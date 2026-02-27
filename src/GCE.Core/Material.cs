namespace GCE.Core;

/// <summary>
/// Concrete implementation of <see cref="IMaterial"/> describing a metal or alloy.
/// </summary>
/// <param name="Name">Display name of the material (e.g. "Zinc", "Mild Steel").</param>
/// <param name="StandardPotential">Standard electrochemical potential in V vs. SHE.</param>
/// <param name="ExchangeCurrentDensity">Exchange current density in A/m².</param>
/// <param name="MolarMass">Molar mass in kg/mol, used for Faraday's Law corrosion-rate conversion.</param>
/// <param name="ElectronsTransferred">Electrons transferred per formula unit in the anodic dissolution reaction.</param>
/// <param name="Density">Density in kg/m³, used for volumetric corrosion-rate conversion.</param>
public sealed record Material(
    string Name,
    double StandardPotential,
    double ExchangeCurrentDensity,
    double MolarMass,
    int ElectronsTransferred,
    double Density) : IMaterial;
