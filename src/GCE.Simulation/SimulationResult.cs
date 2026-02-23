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
}
