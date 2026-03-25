namespace GCE.Simulation;

/// <summary>
/// Describes the progress of a running simulation, suitable for display or logging.
/// </summary>
/// <param name="CurrentStep">The index of the most-recently completed integration step (0-based).</param>
/// <param name="TotalSteps">The total number of integration steps requested.</param>
/// <param name="CurrentTime">The simulation time at the current step (s).</param>
/// <param name="TotalTime">The total simulation duration (s).</param>
/// <param name="MixedPotential">The mixed (corrosion) potential at the current step (V vs. SHE).</param>
/// <param name="CorrosionRate">The corrosion rate at the current step (mm/year).</param>
public sealed record SimulationProgress(
    int CurrentStep,
    int TotalSteps,
    double CurrentTime,
    double TotalTime,
    double MixedPotential,
    double CorrosionRate)
{
    /// <summary>
    /// Gets the fraction of the simulation that has been completed (0 to 1).
    /// </summary>
    public double Fraction =>
        TotalSteps > 0 ? (double)CurrentStep / TotalSteps : 0.0;
}
