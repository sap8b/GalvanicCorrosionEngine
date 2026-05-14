namespace GCE.Simulation;

/// <summary>
/// A snapshot of a simulation at a particular point in time, usable as a
/// checkpoint for pausing and later resuming or restarting the simulation.
/// </summary>
/// <remarks>
/// Obtain an instance either from a <see cref="SimulationResult"/> at the end
/// of a run, or from the checkpoint callback during a run. Pass it to
/// <see cref="ISimulationRunner.Resume"/> to continue from where the simulation
/// was paused.
/// </remarks>
public sealed class SimulationState
{
    /// <summary>
    /// Gets the number of integration steps that have been completed.
    /// </summary>
    public int CompletedSteps { get; init; }

    /// <summary>
    /// Gets the simulation time (s) at which this snapshot was captured.
    /// </summary>
    public double CurrentTime { get; init; }

    /// <summary>
    /// Gets the mixed (corrosion) potential (V vs. SHE) at the snapshot time.
    /// </summary>
    public double CurrentPotential { get; init; }

    /// <summary>
    /// Gets all time points (s) recorded up to and including the snapshot.
    /// </summary>
    public IReadOnlyList<double> TimePoints { get; init; } = [];

    /// <summary>
    /// Gets the mixed potentials (V vs. SHE) at each recorded time point.
    /// </summary>
    public IReadOnlyList<double> MixedPotentials { get; init; } = [];

    /// <summary>
    /// Gets the corrosion rates (mm/year) at each recorded time point.
    /// </summary>
    public IReadOnlyList<double> CorrosionRates { get; init; } = [];

    /// <summary>
    /// Gets the per-node cumulative dissolved mass (kg per unit depth) accumulated up to and
    /// including the snapshot, flattened in row-major order (index = i*NodesY + j).
    /// <see langword="null"/> when no <see cref="GeometryMesh"/> was provided.
    /// </summary>
    public double[]? NodeMassLoss { get; init; }

    /// <summary>
    /// Gets the current <c>NodesX × NodesY</c> material-phase map at the checkpoint time.
    /// <see langword="null"/> when no <see cref="GeometryMesh"/> was provided.
    /// </summary>
    public NodePhase[,]? Regions { get; init; }

    /// <summary>
    /// Gets the per-time-step 2-D phase maps recorded up to and including this checkpoint.
    /// </summary>
    public IReadOnlyList<NodePhase[,]>? PhaseSnapshots { get; init; }

    /// <summary>
    /// Gets the per-time-step cumulative dissolved-mass maps (kg per unit depth),
    /// flattened in row-major order (index = i*NodesY + j), recorded up to and including
    /// this checkpoint.
    /// </summary>
    public IReadOnlyList<double[]>? NodeMassLossSnapshots { get; init; }

    /// <summary>
    /// Gets the per-time-step recession-depth maps (m), flattened in row-major order
    /// (index = i*NodesY + j), recorded up to and including this checkpoint.
    /// </summary>
    public IReadOnlyList<double[]>? RecessionDepthSnapshots { get; init; }

    /// <summary>
    /// Gets the per-time-step surface recession profiles (m) as a function of x-position,
    /// recorded up to and including this checkpoint.
    /// </summary>
    public IReadOnlyList<double[]>? SurfaceProfileHistory { get; init; }
}
