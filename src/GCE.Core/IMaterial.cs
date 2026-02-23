namespace GCE.Core;

/// <summary>
/// Represents a material that can participate in galvanic corrosion.
/// </summary>
public interface IMaterial
{
    /// <summary>Gets the display name of the material.</summary>
    string Name { get; }

    /// <summary>
    /// Gets the standard electrochemical potential (V vs. SHE) for the material.
    /// </summary>
    double StandardPotential { get; }

    /// <summary>
    /// Gets the exchange current density (A/m²) for the anodic reaction.
    /// </summary>
    double ExchangeCurrentDensity { get; }
}
