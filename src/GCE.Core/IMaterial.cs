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

    /// <summary>
    /// Gets the molar mass of the material (kg/mol), used for Faraday's Law corrosion-rate conversion.
    /// </summary>
    double MolarMass { get; }

    /// <summary>
    /// Gets the number of electrons transferred per formula unit in the anodic dissolution reaction.
    /// </summary>
    int ElectronsTransferred { get; }

    /// <summary>
    /// Gets the density of the material (kg/m³), used for volumetric corrosion-rate conversion.
    /// </summary>
    double Density { get; }
}
