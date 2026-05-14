namespace GCE.Core;

/// <summary>
/// Concrete corrosion-product material with electrochemical transport and solubility properties.
/// </summary>
public sealed class CorrosionProductMaterial : ICorrosionProductMaterial
{
    /// <summary>
    /// Initialises a new <see cref="CorrosionProductMaterial"/>.
    /// </summary>
    public CorrosionProductMaterial(
        string name,
        double ionicConductivity,
        double molarVolume,
        double solubilityProduct,
        double barrierResistanceFactor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ionicConductivity, 0.0, nameof(ionicConductivity));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(molarVolume, 0.0, nameof(molarVolume));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(solubilityProduct, 0.0, nameof(solubilityProduct));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(barrierResistanceFactor, 0.0, nameof(barrierResistanceFactor));

        Name = name;
        IonicConductivity = ionicConductivity;
        MolarVolume = molarVolume;
        SolubilityProduct = solubilityProduct;
        BarrierResistanceFactor = barrierResistanceFactor;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public double IonicConductivity { get; }

    /// <inheritdoc />
    public double MolarVolume { get; }

    /// <inheritdoc />
    public double SolubilityProduct { get; }

    /// <inheritdoc />
    public double BarrierResistanceFactor { get; }

    /// <inheritdoc />
    public override string ToString() => Name;
}
