namespace GCE.Core;

/// <summary>
/// Defines a corrosion model that can compute a corrosion rate.
/// </summary>
public interface ICorrosionModel
{
    /// <summary>
    /// Computes the corrosion rate (mm/year) given the applied potential (V).
    /// </summary>
    /// <param name="potential">The mixed potential in volts (V vs. SHE).</param>
    /// <returns>Corrosion rate in mm/year.</returns>
    double ComputeCorrosionRate(double potential);
}
