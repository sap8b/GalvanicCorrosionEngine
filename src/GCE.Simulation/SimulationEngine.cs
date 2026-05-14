using GCE.Atmosphere;
using GCE.Core;
using GCE.Electrochemistry;
using GCE.Numerics;
using GCE.Numerics.Solvers;

namespace GCE.Simulation;

/// <summary>
/// Orchestrates a galvanic corrosion simulation over time using
/// Butler–Volmer kinetics and a Runge–Kutta time integrator.
/// </summary>
/// <remarks>
/// <para>
/// When <see cref="SimulationParameters.WeatherProvider"/> is provided the
/// electrochemical environment is updated at every integration step from the
/// weather data, enabling time-varying simulations driven by realistic or
/// synthetic atmospheric conditions.
/// </para>
/// <para>
/// The engine implements <see cref="ISimulationRunner"/>: use <see cref="Run"/>
/// for synchronous execution, <see cref="RunAsync"/> for asynchronous execution
/// with progress callbacks and cooperative pause/cancellation support, and
/// <see cref="Resume"/> to continue a previously paused run from a
/// <see cref="SimulationState"/> checkpoint.
/// </para>
/// </remarks>
public sealed class SimulationEngine : ISimulationRunner
{
    /// <summary>
    /// Maximum ratio by which the adaptive time step may exceed the nominal step size.
    /// An adaptive step is clamped to at most <c>nominalDt × MaxAdaptiveStepMultiplier</c>.
    /// </summary>
    private const double MaxAdaptiveStepMultiplier = 4.0;

    /// <summary>Approximate seconds per Julian year (365.25 d × 86 400 s/d), used for mass-loss calculations.</summary>
    private const double SecondsPerYear = 3.156e7;
    /// <summary>
    /// Runs a galvanic corrosion simulation synchronously and returns the full result.
    /// </summary>
    /// <param name="parameters">Simulation configuration.</param>
    /// <returns>A <see cref="SimulationResult"/> containing time-series data.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="parameters"/> is <see langword="null"/>.
    /// </exception>
    public SimulationResult Run(SimulationParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        // Delegate to RunCoreAsync (synchronous completion is guaranteed here).
        return RunCoreAsync(
            startStep:        0,
            startTime:        0.0,
            startPotential:   InitialPotential(parameters),
            priorTimes:       [],
            priorPotentials:  [],
            priorRates:       [],
            priorNodeMassLoss: null,
            parameters:       parameters,
            progress:         null,
            checkpoint:       out _,
            cancellationToken: CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Runs the simulation asynchronously, reporting progress after every step and
    /// honouring cooperative cancellation.
    /// </summary>
    /// <remarks>
    /// When the <paramref name="cancellationToken"/> is cancelled a
    /// <see cref="SimulationState"/> checkpoint is stored in <paramref name="checkpoint"/>
    /// so the caller can later pass it to <see cref="Resume"/>.
    /// </remarks>
    /// <inheritdoc cref="ISimulationRunner.RunAsync"/>
    public Task<SimulationResult> RunAsync(
        SimulationParameters           parameters,
        IProgress<SimulationProgress>? progress,
        out SimulationState?           checkpoint,
        CancellationToken              cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        return RunCoreAsync(
            startStep:        0,
            startTime:        0.0,
            startPotential:   InitialPotential(parameters),
            priorTimes:       [],
            priorPotentials:  [],
            priorRates:       [],
            priorNodeMassLoss: null,
            parameters:       parameters,
            progress:         progress,
            checkpoint:       out checkpoint,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Resumes a previously paused simulation from a <see cref="SimulationState"/>
    /// checkpoint, appending new results to the data already in the checkpoint.
    /// </summary>
    /// <inheritdoc cref="ISimulationRunner.Resume"/>
    public Task<SimulationResult> Resume(
        SimulationState                checkpoint,
        SimulationParameters           parameters,
        IProgress<SimulationProgress>? progress          = null,
        CancellationToken              cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        ArgumentNullException.ThrowIfNull(parameters);

        return RunCoreAsync(
            startStep:        checkpoint.CompletedSteps,
            startTime:        checkpoint.CurrentTime,
            startPotential:   checkpoint.CurrentPotential,
            priorTimes:       checkpoint.TimePoints,
            priorPotentials:  checkpoint.MixedPotentials,
            priorRates:       checkpoint.CorrosionRates,
            priorNodeMassLoss: checkpoint.NodeMassLoss,
            parameters:       parameters,
            progress:         progress,
            checkpoint:       out _,
            cancellationToken: cancellationToken);
    }

    // ── Internal helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Core async integration loop, shared by <see cref="RunAsync"/> and
    /// <see cref="Resume"/>.
    /// </summary>
    private Task<SimulationResult> RunCoreAsync(
        int                            startStep,
        double                         startTime,
        double                         startPotential,
        IReadOnlyList<double>          priorTimes,
        IReadOnlyList<double>          priorPotentials,
        IReadOnlyList<double>          priorRates,
        double[]?                      priorNodeMassLoss,
        SimulationParameters           parameters,
        IProgress<SimulationProgress>? progress,
        out SimulationState?           checkpoint,
        CancellationToken              cancellationToken)
    {
        // Capture a local null ref so we can assign it inside the closure below.
        SimulationState? capturedCheckpoint = null;

        var times      = new List<double>(priorTimes);
        var potentials = new List<double>(priorPotentials);
        var rates      = new List<double>(priorRates);

        var ode           = BuildOde(parameters);
        double nominalDt  = parameters.DurationSeconds / parameters.TimeSteps;
        double t          = startTime;
        double potential  = startPotential;

        // Adaptive time-stepping components (created only when needed).
        TimeEvolver? timeEvolver = null;
        if (parameters.UseAdaptiveTimeStep)
            timeEvolver = new TimeEvolver(new ConvergenceChecker());

        var solver = new RungeKuttaSolver(ode);

        // Operator-splitting: geometry evolver for the slow (geometric) timescale.
        // Created only when both the flag is set and a mesh is provided.
        GeometryEvolver? geoEvolver = null;
        if (parameters.UseOperatorSplitting && parameters.Mesh is not null)
            geoEvolver = new GeometryEvolver(parameters.MaxNodesPerGeoStep);

        // Species transport tracking.
        Dictionary<string, List<double>>? speciesHistory = null;
        if (parameters.TrackedSpecies is { Count: > 0 })
        {
            speciesHistory = new Dictionary<string, List<double>>();
            foreach (var st in parameters.TrackedSpecies)
                speciesHistory[st.Species.Name] = new List<double>();
        }

        // pH tracking
        List<double>? pHList = null;
        double currentpH = parameters.Environment.pH;
        if (parameters.TrackpH)
        {
            pHList = new List<double>();
        }

        // Per-node cumulative mass-loss bookkeeping (only when a mesh is provided).
        double[]? nodeMassLoss = null;
        double[]? lastNodalPotentials     = null;
        double[]? lastNodalCorrosionRates = null;
        if (parameters.Mesh is not null)
        {
            int nodeCount = parameters.Mesh.NodesX * parameters.Mesh.NodesY;
            nodeMassLoss = priorNodeMassLoss?.Length == nodeCount
                ? (double[])priorNodeMassLoss.Clone()
                : new double[nodeCount];
        }

        // Include the initial point when starting fresh (startStep == 0).
        if (startStep == 0)
        {
            double rate0 = ComputeCorrosionRate(parameters, t, potential);
            times.Add(t);
            potentials.Add(potential);
            rates.Add(rate0);

            if (speciesHistory is not null && parameters.TrackedSpecies is not null)
            {
                foreach (var st in parameters.TrackedSpecies)
                    speciesHistory[st.Species.Name].Add(st.Species.Concentration);
            }

            pHList?.Add(currentpH);
        }

        double currentDt = nominalDt;

        for (int step = startStep; step < parameters.TimeSteps; step++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                capturedCheckpoint = new SimulationState
                {
                    CompletedSteps   = step,
                    CurrentTime      = t,
                    CurrentPotential = potential,
                    TimePoints       = times.AsReadOnly(),
                    MixedPotentials  = potentials.AsReadOnly(),
                    CorrosionRates   = rates.AsReadOnly(),
                    NodeMassLoss     = nodeMassLoss is not null ? (double[])nodeMassLoss.Clone() : null,
                };
                break;
            }

            // Advance species transport.
            if (parameters.TrackedSpecies is not null)
            {
                foreach (var st in parameters.TrackedSpecies)
                {
                    st.Advance(1);
                    speciesHistory?[st.Species.Name].Add(st.Species.Concentration);
                }
            }

            // Operator-splitting: when the simulation has already consumed all
            // remaining time (due to variable Δt_geo steps), exit the loop early
            // rather than attempting a zero-sized step.
            if (geoEvolver is not null && t >= parameters.DurationSeconds)
                break;

            if (timeEvolver is not null)
            {
                // Adaptive path: let TimeEvolver choose an appropriate dt.
                // The loop still iterates exactly TimeSteps times so the result
                // always contains TimeSteps + 1 data points; individual dt values
                // adapt to the solution dynamics.  The trial dt is clamped to the
                // remaining simulation time so that the final point never overshoots
                // DurationSeconds.
                double remainingTime = parameters.DurationSeconds - t;
                double trialDt       = Math.Min(currentDt, remainingTime);

                // When operator splitting is active, use the geometric timestep as
                // the upper bound so that both the potential ODE and the geometry
                // advance on the same (slow) timescale.
                double maxDtForStep;
                if (geoEvolver is not null && lastNodalCorrosionRates is not null)
                {
                    // Cap the geometric timestep to the remaining simulation time so
                    // the time axis never significantly overshoots DurationSeconds.
                    double maxAllowed = Math.Max(remainingTime, 0.0);
                    maxDtForStep = geoEvolver.ComputeGeometricTimestep(
                        parameters.Mesh!, nodeMassLoss!, lastNodalCorrosionRates,
                        parameters.Pair.Anode,
                        minDt: 1.0,
                        maxDt: maxAllowed);
                }
                else
                {
                    maxDtForStep = nominalDt * MaxAdaptiveStepMultiplier;
                }

                trialDt = Math.Min(trialDt, maxDtForStep);
                (potential, currentDt) = timeEvolver.AdvanceAdaptive(t, potential, trialDt, ode,
                    maxDt: maxDtForStep);
            }
            else if (geoEvolver is not null && lastNodalCorrosionRates is not null)
            {
                // Operator-splitting (non-adaptive potential) path: the ODE step
                // uses the adaptive geometric timestep Δt_geo so that the time axis
                // advances on the slow (geometric) timescale.
                double remainingTime = parameters.DurationSeconds - t;
                // Cap the geometric timestep to the remaining simulation time so
                // the time axis never significantly overshoots DurationSeconds.
                double maxAllowed    = Math.Max(remainingTime, 0.0);
                double dtGeo = geoEvolver.ComputeGeometricTimestep(
                    parameters.Mesh!, nodeMassLoss!, lastNodalCorrosionRates,
                    parameters.Pair.Anode,
                    minDt: 1.0,
                    maxDt: maxAllowed);
                potential = solver.Step(t, potential, dtGeo);
                currentDt = dtGeo;
            }
            else
            {
                // Fixed-step path.
                potential = solver.Step(t, potential, nominalDt);
                currentDt = nominalDt;
            }

            t += currentDt;

            // Per-step spatial solve: update nodal potentials, corrosion rates, and
            // accumulate dissolved mass for each Anode node.
            if (parameters.Mesh is not null && nodeMassLoss is not null)
            {
                var   envSpatial    = GetEnvironmentAt(parameters, t);
                double kappaSpatial = envSpatial.IonicConductivity > 0 ? envSpatial.IonicConductivity : 1e-3;

                (lastNodalPotentials, lastNodalCorrosionRates) = SpatialSolver.Solve(
                    parameters.Mesh,
                    parameters.Pair.Anode,
                    parameters.Pair.Cathode,
                    kappaSpatial,
                    parameters.CorrosionProductMaterial,
                    envSpatial.TemperatureKelvin);

                if (geoEvolver is not null)
                {
                    // Operator-splitting (slow) path: use the GeometryEvolver to
                    // accumulate mass with the already-computed currentDt (= Δt_geo).
                    geoEvolver.Advance(
                        parameters.Mesh,
                        nodeMassLoss,
                        lastNodalCorrosionRates,
                        currentDt,
                        parameters.Pair.Anode);
                }
                else
                {
                    AccumulateNodeMassLoss(
                        parameters.Mesh,
                        nodeMassLoss,
                        lastNodalCorrosionRates,
                        currentDt,
                        parameters.Pair.Anode);
                }

                if (parameters.CorrosionProductMaterial is not null
                    && parameters.TrackedSpecies is { Count: > 0 })
                {
                    int hydroxideExponent = InferHydroxideStoichiometry(parameters.CorrosionProductMaterial.Name);
                    double hydroxideActivity = ComputeHydroxideActivity(GetEnvironmentAt(parameters, t).pH);
                    var precipitationModel = new PrecipitationModel(
                        parameters.CorrosionProductMaterial.SolubilityProduct);

                    foreach (var st in parameters.TrackedSpecies)
                    {
                        if (st.GridPoints != parameters.Mesh.NodesX * parameters.Mesh.NodesY)
                            continue;
                        if (!IsLikelyAnodicMetalIon(st.Species, parameters.Pair.Anode))
                            continue;

                        st.ApplyPrecipitationAndDeposition(
                            parameters.Mesh,
                            lastNodalCorrosionRates,
                            currentDt,
                            parameters.Pair.Anode,
                            precipitationModel,
                            parameters.CorrosionProductMaterial,
                            hydroxideActivity,
                            hydroxideExponent);
                    }
                }
            }

            double rate = ComputeCorrosionRate(parameters, t, potential);
            times.Add(t);
            potentials.Add(potential);
            rates.Add(rate);

            if (pHList is not null)
            {
                // Update pH: net anodic current per unit area × dt produces H⁺
                // (simplified: corrosion dissolves metal, not directly H⁺, but
                //  for pH tracking we use the net current as a proxy)
                var envForPh  = GetEnvironmentAt(parameters, t);
                var anodeForPh = new ButlerVolmerModel(parameters.Pair.Anode, envForPh);
                double iAnodePh = anodeForPh.ComputeCurrentDensity(potential);
                // Assume 1 L effective volume, Faraday's law for H⁺ change
                double deltaH = iAnodePh * currentDt / (PhysicalConstants.Faraday * 1.0);
                double hConc  = Math.Pow(10.0, -currentpH);          // mol/L
                hConc = Math.Max(hConc + deltaH * 1000.0, 1e-14);    // convert mol/m³ → mol/L
                currentpH = -Math.Log10(hConc);
                pHList.Add(currentpH);
            }

            progress?.Report(new SimulationProgress(
                CurrentStep:      step + 1,
                TotalSteps:       parameters.TimeSteps,
                CurrentTime:      t,
                TotalTime:        parameters.DurationSeconds,
                MixedPotential:   potential,
                CorrosionRate:    rate));
        }

        checkpoint = capturedCheckpoint;

        // Use the last per-step spatial-solve result; fall back to a single solve when
        // no loop iterations ran (e.g. TimeSteps == 0) but a mesh was supplied.
        double[]? nodalPotentials    = lastNodalPotentials;
        double[]? nodalCorrosionRates = lastNodalCorrosionRates;
        if (nodalPotentials is null && parameters.Mesh is not null && potentials.Count > 0)
        {
            var    lastEnv  = GetEnvironmentAt(parameters, times.Count > 0 ? times[^1] : 0.0);
            double kappa    = lastEnv.IonicConductivity > 0 ? lastEnv.IonicConductivity : 1e-3;

            (nodalPotentials, nodalCorrosionRates) = SpatialSolver.Solve(
                parameters.Mesh,
                parameters.Pair.Anode,
                parameters.Pair.Cathode,
                kappa,
                parameters.CorrosionProductMaterial,
                lastEnv.TemperatureKelvin);
        }

        IReadOnlyDictionary<string, IReadOnlyList<double>>? speciesConcentrationHistory = null;
        if (speciesHistory is not null)
        {
            var dict = new Dictionary<string, IReadOnlyList<double>>();
            foreach (var kvp in speciesHistory)
                dict[kvp.Key] = kvp.Value.AsReadOnly();
            speciesConcentrationHistory = dict;
        }

        var result = new SimulationResult
        {
            TimePoints           = times.AsReadOnly(),
            MixedPotentials      = potentials.AsReadOnly(),
            CorrosionRates       = rates.AsReadOnly(),
            ConvergenceHistory   = timeEvolver?.ConvergenceHistory
                                       ?? (IReadOnlyList<ConvergenceInfo>)[],
            NodalPotentials      = nodalPotentials,
            NodalCorrosionRates  = nodalCorrosionRates,
            SpeciesConcentrationHistory = speciesConcentrationHistory,
            pHHistory            = pHList?.AsReadOnly(),
            NodeMassLoss         = nodeMassLoss,
            GeoStepCount         = geoEvolver?.TotalGeoSteps ?? 0,
        };

        return Task.FromResult(result);
    }

    // ── Private helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns an ODE function f(t, y) = dE/dt for the given parameters.
    /// The ODE models the relaxation of the mixed potential to the steady-state
    /// galvanic corrosion potential.
    /// </summary>
    private static OdeFunction BuildOde(SimulationParameters parameters)
    {
        return (t, potential) =>
        {
            var env          = GetEnvironmentAt(parameters, t);
            var anodeModel   = new ButlerVolmerModel(parameters.Pair.Anode,   env);
            var cathodeModel = new ButlerVolmerModel(parameters.Pair.Cathode, env);

            // Compute solution resistance Rs = PathLength / IonicConductivity (Ω·m²).
            double rs = 0.0;
            if (parameters.PathLength > 0.0 && env.IonicConductivity > 0.0)
                rs = parameters.PathLength / env.IonicConductivity;

            double iAnode   = anodeModel.ComputeCurrentDensity(potential);
            double iCathode = cathodeModel.ComputeCurrentDensity(potential);

            if (rs > 0.0)
            {
                // Ohmic IR-drop correction: the net current flowing through the
                // solution resistance causes a voltage drop V_IR = iNet × Rs.
                // Both electrode reactions see a shifted effective potential.
                double iNet  = iAnode + iCathode;
                double vDrop = Math.Clamp(iNet * rs, -1.0, 1.0);
                iAnode   = anodeModel.ComputeCurrentDensity(potential - vDrop);
                iCathode = cathodeModel.ComputeCurrentDensity(potential - vDrop);
            }

            double netCurrent = iAnode + iCathode;
            return -netCurrent * 0.01;
        };
    }

    /// <summary>
    /// Returns the initial mixed potential as the arithmetic mean of the
    /// standard potentials of both electrode materials.
    /// </summary>
    private static double InitialPotential(SimulationParameters parameters) =>
        (parameters.Pair.Anode.StandardPotential +
         parameters.Pair.Cathode.StandardPotential) / 2.0;

    /// <summary>
    /// Returns the corrosion rate (mm/year) at the given time and potential,
    /// using the environment applicable at that instant.
    /// </summary>
    private static double ComputeCorrosionRate(
        SimulationParameters parameters, double t, double potential)
    {
        var env = GetEnvironmentAt(parameters, t);
        return new ButlerVolmerModel(parameters.Pair.Anode, env)
            .ComputeCorrosionRate(potential);
    }

    /// <summary>
    /// Resolves the <see cref="IEnvironment"/> at time <paramref name="t"/>.
    /// When a weather provider is configured it is queried; otherwise the static
    /// environment from the parameters is returned.
    /// </summary>
    private static IEnvironment GetEnvironmentAt(SimulationParameters parameters, double t)
    {
        if (parameters.WeatherProvider is not null)
        {
            var obs = parameters.WeatherProvider.GetObservation(t);
            if (parameters.FilmEvolution is not null)
            {
                // Advance the film by a small nominal step using the current observation.
                // dt is approximated as DurationSeconds/TimeSteps.
                double dt = parameters.DurationSeconds / parameters.TimeSteps;
                parameters.FilmEvolution.Advance(dt, obs);
                // Return an AtmosphericConditions derived from the updated film state.
                var state = parameters.FilmEvolution.State;
                return new AtmosphericConditions(
                    state.SurfaceTemperatureCelsius,
                    obs.RelativeHumidity,
                    state.SaltConcentrationMolPerL);
            }
            return new WeatherDrivenAtmosphericConditions(obs);
        }
        return parameters.Environment;
    }

    /// <summary>
    /// Accumulates per-node dissolved mass for each <see cref="NodePhase.Anode"/> node and
    /// transitions fully dissolved nodes to <see cref="NodePhase.Electrolyte"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The incremental mass loss for node (i, j) over time step <paramref name="dt"/> is:
    /// <code>
    /// Δmass = rate_mm_yr / (1000 × s_per_yr) × ρ × A_node × Δt
    /// </code>
    /// where <c>rate_mm_yr</c> is the nodal corrosion rate from <see cref="SpatialSolver"/>,
    /// <c>ρ</c> is the anode material density, and <c>A_node = dx × dy</c> is the nodal
    /// cell area (per unit depth).
    /// </para>
    /// <para>
    /// A node is considered fully dissolved when its cumulative mass loss reaches or
    /// exceeds <c>ρ × V_node = ρ × dx × dy</c> (per unit depth), at which point its
    /// phase is set to <see cref="NodePhase.Electrolyte"/>.
    /// </para>
    /// </remarks>
    private static void AccumulateNodeMassLoss(
        GeometryMesh mesh,
        double[]     nodeMassLoss,
        double[]     nodalCorrosionRates,
        double       dt,
        IMaterial    anodeMaterial)
    {
        int nx = mesh.NodesX;
        int ny = mesh.NodesY;

        // Assume a uniform rectilinear grid: node spacing is the total span divided by
        // the number of intervals.  Non-uniform meshes are not currently supported and
        // would require per-node area calculations using adjacent coordinate differences.
        double dx = nx > 1 ? (mesh.XCoordinates[nx - 1] - mesh.XCoordinates[0]) / (nx - 1) : 1.0;
        double dy = ny > 1 ? (mesh.YCoordinates[ny - 1] - mesh.YCoordinates[0]) / (ny - 1) : 1.0;
        double aNode     = dx * dy;                        // nodal cell area (m²)
        double threshold = anodeMaterial.Density * aNode;  // ρ × V_node per unit depth (kg/m)

        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                if (mesh.Regions[i, j] != NodePhase.Anode)
                    continue;

                int    idx       = i * ny + j;
                // Δmass = rate_m/s × ρ × A_node × Δt  (derived from Faraday's law via the
                // corrosion-rate formula in SpatialSolver, cancelling n, F, and M).
                double deltaMass = nodalCorrosionRates[idx] / (SecondsPerYear * 1000.0)
                                   * anodeMaterial.Density * aNode * dt;
                nodeMassLoss[idx] += deltaMass;

                if (nodeMassLoss[idx] >= threshold)
                    mesh.Regions[i, j] = NodePhase.Electrolyte;
            }
        }
    }

    private static bool IsLikelyAnodicMetalIon(Species species, IMaterial anodeMaterial)
    {
        if (species.Charge <= 0)
            return false;

        if (species.Name.Equals("H+", StringComparison.OrdinalIgnoreCase))
            return false;

        string prefix = GetAnodeIonPrefix(anodeMaterial.Name);
        if (prefix.Length == 0)
            return true;

        return species.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetAnodeIonPrefix(string materialName)
    {
        if (materialName.Contains("zinc", StringComparison.OrdinalIgnoreCase))
            return "Zn";
        if (materialName.Contains("steel", StringComparison.OrdinalIgnoreCase)
            || materialName.Contains("iron", StringComparison.OrdinalIgnoreCase))
            return "Fe";
        if (materialName.Contains("aluminium", StringComparison.OrdinalIgnoreCase)
            || materialName.Contains("aluminum", StringComparison.OrdinalIgnoreCase))
            return "Al";
        if (materialName.Contains("copper", StringComparison.OrdinalIgnoreCase))
            return "Cu";
        if (materialName.Contains("nickel", StringComparison.OrdinalIgnoreCase))
            return "Ni";
        if (materialName.Contains("magnesium", StringComparison.OrdinalIgnoreCase))
            return "Mg";

        return string.Empty;
    }

    private static int InferHydroxideStoichiometry(string corrosionProductName)
    {
        // Heuristic fallback based on formula-like names (e.g., Fe(OH)2).
        int marker = corrosionProductName.IndexOf("(OH)", StringComparison.OrdinalIgnoreCase);
        if (marker < 0)
            return 1;

        int idx = marker + "(OH)".Length;
        int value = 0;
        while (idx < corrosionProductName.Length && char.IsDigit(corrosionProductName[idx]))
        {
            value = value * 10 + (corrosionProductName[idx] - '0');
            idx++;
        }

        return value > 0 ? value : 1;
    }

    private static double ComputeHydroxideActivity(double pH)
    {
        // [OH-] = 10^(pH-14) mol/L; multiply by 1000 because 1 L = 1e-3 m³, so mol/L = 1000·mol/m³.
        double hydroxideMolPerLiter = Math.Pow(10.0, pH - 14.0);
        return Math.Max(hydroxideMolPerLiter * 1000.0, 0.0);
    }
}
