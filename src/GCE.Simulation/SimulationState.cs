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
}
