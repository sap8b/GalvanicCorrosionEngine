using GCE.Atmosphere;
using GCE.Core;
using GCE.Electrochemistry;
using GCE.Numerics;

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

        // Include the initial point when starting fresh (startStep == 0).
        if (startStep == 0)
        {
            double rate0 = ComputeCorrosionRate(parameters, t, potential);
            times.Add(t);
            potentials.Add(potential);
            rates.Add(rate0);
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
                };
                break;
            }

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
                (potential, currentDt) = timeEvolver.AdvanceAdaptive(t, potential, trialDt, ode,
                    maxDt: nominalDt * MaxAdaptiveStepMultiplier);
            }
            else
            {
                // Fixed-step path.
                potential = solver.Step(t, potential, nominalDt);
                currentDt = nominalDt;
            }

            t += currentDt;

            double rate = ComputeCorrosionRate(parameters, t, potential);
            times.Add(t);
            potentials.Add(potential);
            rates.Add(rate);

            progress?.Report(new SimulationProgress(
                CurrentStep:      step + 1,
                TotalSteps:       parameters.TimeSteps,
                CurrentTime:      t,
                TotalTime:        parameters.DurationSeconds,
                MixedPotential:   potential,
                CorrosionRate:    rate));
        }

        checkpoint = capturedCheckpoint;

        var result = new SimulationResult
        {
            TimePoints        = times.AsReadOnly(),
            MixedPotentials   = potentials.AsReadOnly(),
            CorrosionRates    = rates.AsReadOnly(),
            ConvergenceHistory = timeEvolver?.ConvergenceHistory
                                     ?? (IReadOnlyList<ConvergenceInfo>)[],
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

            // dE/dt proportional to net current (relaxation model):
            // drives potential toward the mixed-potential equilibrium.
            double netCurrent = anodeModel.ComputeCurrentDensity(potential)
                              + cathodeModel.ComputeCurrentDensity(potential);
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
    private static IEnvironment GetEnvironmentAt(SimulationParameters parameters, double t) =>
        parameters.WeatherProvider is not null
            ? new WeatherDrivenAtmosphericConditions(parameters.WeatherProvider.GetObservation(t))
            : parameters.Environment;
}
