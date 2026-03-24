namespace GCE.Core;

/// <summary>
/// Represents an individual electrochemical reaction that contributes a partial current
/// density to an electrode at a given potential.
/// </summary>
public interface IElectrochemicalReaction
{
    /// <summary>
    /// Computes the partial current density (A/m²) at the given electrode potential.
    /// Positive values represent anodic (oxidation) currents;
    /// negative values represent cathodic (reduction) currents.
    /// </summary>
    /// <param name="potential">Electrode potential in V vs. SHE.</param>
    /// <returns>Partial current density in A/m².</returns>
    double CurrentDensity(double potential);
}
