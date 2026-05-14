namespace GCE.Simulation;

/// <summary>
/// Holds the output of a completed galvanic corrosion simulation.
/// </summary>
public sealed class SimulationResult
{
    /// <summary>Gets the time points (seconds) of the simulation.</summary>
    public IReadOnlyList<double> TimePoints { get; init; } = [];

    /// <summary>Gets the corrosion rate (mm/year) at each time point.</summary>
    public IReadOnlyList<double> CorrosionRates { get; init; } = [];

    /// <summary>Gets the mixed potential (V vs. SHE) at each time point.</summary>
    public IReadOnlyList<double> MixedPotentials { get; init; } = [];

    /// <summary>Gets the average corrosion rate over the simulation (mm/year).</summary>
    public double AverageCorrosionRate =>
        CorrosionRates.Count == 0 ? 0.0 : CorrosionRates.Average();

    /// <summary>
    /// Gets the convergence history recorded during the simulation.
    /// Populated only when <see cref="SimulationParameters.UseAdaptiveTimeStep"/> is
    /// <see langword="true"/>; otherwise empty.
    /// </summary>
    public IReadOnlyList<ConvergenceInfo> ConvergenceHistory { get; init; } = [];

    /// <summary>
    /// Gets the per-node electrolyte potential field (V vs. SHE), flattened in
    /// row-major order (index = i*NodesY + j). Populated only when a
    /// <see cref="GeometryMesh"/> is provided via
    /// <see cref="SimulationParameters.Mesh"/>.
    /// </summary>
    public double[]? NodalPotentials { get; init; }

    /// <summary>
    /// Gets the per-node corrosion rate (mm/year), flattened in row-major order.
    /// Populated only when a <see cref="GeometryMesh"/> is provided.
    /// </summary>
    public double[]? NodalCorrosionRates { get; init; }

    /// <summary>
    /// Gets the per-step average concentration (mol/m³) for each tracked species,
    /// keyed by species name. Each value list has one entry per simulation time
    /// point. <see langword="null"/> when no species transport is configured.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<double>>? SpeciesConcentrationHistory { get; init; }

    /// <summary>
    /// Gets the pH at each simulation time point. Populated only when
    /// <see cref="SimulationParameters.TrackpH"/> is <see langword="true"/>.
    /// <see langword="null"/> otherwise.
    /// </summary>
    public IReadOnlyList<double>? pHHistory { get; init; }

    /// <summary>
    /// Gets the per-node cumulative dissolved mass (kg per unit depth) at the end of the
    /// simulation, flattened in row-major order (index = i*NodesY + j).
    /// <see langword="null"/> when no <see cref="GeometryMesh"/> was provided.
    /// </summary>
    public double[]? NodeMassLoss { get; init; }

    /// <summary>
    /// Gets the per-time-step 2-D phase maps.
    /// Each entry is a <c>NodesX × NodesY</c> phase snapshot aligned with
    /// <see cref="TimePoints"/>. <see langword="null"/> when no
    /// <see cref="GeometryMesh"/> was provided.
    /// </summary>
    public IReadOnlyList<NodePhase[,]>? PhaseSnapshots { get; init; }

    /// <summary>
    /// Gets the per-time-step cumulative dissolved-mass maps (kg per unit depth),
    /// flattened in row-major order (index = i*NodesY + j) and aligned with
    /// <see cref="TimePoints"/>. <see langword="null"/> when no
    /// <see cref="GeometryMesh"/> was provided.
    /// </summary>
    public IReadOnlyList<double[]>? NodeMassLossSnapshots { get; init; }

    /// <summary>
    /// Gets the per-time-step recession-depth maps (m), flattened in row-major order
    /// (index = i*NodesY + j) and aligned with <see cref="TimePoints"/>.
    /// <see langword="null"/> when no <see cref="GeometryMesh"/> was provided.
    /// </summary>
    public IReadOnlyList<double[]>? RecessionDepthSnapshots { get; init; }

    /// <summary>
    /// Gets the per-time-step surface recession profiles (m) as a function of x-position.
    /// Each entry contains one depth value per x-node and is aligned with
    /// <see cref="TimePoints"/>. <see langword="null"/> when no
    /// <see cref="GeometryMesh"/> was provided.
    /// </summary>
    public IReadOnlyList<double[]>? SurfaceProfileHistory { get; init; }

    /// <summary>
    /// Gets the number of outer geometric steps taken during the simulation.
    /// Populated only when <see cref="SimulationParameters.UseOperatorSplitting"/> is
    /// <see langword="true"/> and a <see cref="GeometryMesh"/> was provided;
    /// otherwise 0.
    /// </summary>
    public int GeoStepCount { get; init; }
}
