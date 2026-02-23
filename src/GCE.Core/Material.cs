namespace GCE.Core;

/// <summary>
/// Concrete implementation of <see cref="IMaterial"/> describing a metal or alloy.
/// </summary>
/// <param name="Name">Display name of the material (e.g. "Zinc", "Mild Steel").</param>
/// <param name="StandardPotential">Standard electrochemical potential in V vs. SHE.</param>
/// <param name="ExchangeCurrentDensity">Exchange current density in A/m².</param>
public sealed record Material(
    string Name,
    double StandardPotential,
    double ExchangeCurrentDensity) : IMaterial;
