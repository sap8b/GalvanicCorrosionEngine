using GCE.Core;

namespace GCE.Electrochemistry;

/// <summary>
/// Helpers and defaults for electrochemically active corrosion-product layers.
/// </summary>
public static class CorrosionProductBehavior
{
    /// <summary>Ferrous hydroxide corrosion product, Fe(OH)₂.</summary>
    public static ICorrosionProductMaterial FerrousHydroxide { get; } =
        new CorrosionProductMaterial(
            name: "Fe(OH)2",
            ionicConductivity: 2.5e-5,
            molarVolume: 2.65e-5,
            solubilityProduct: 8.0e-16,
            barrierResistanceFactor: 4.0);

    /// <summary>Zinc oxide corrosion product, ZnO.</summary>
    public static ICorrosionProductMaterial ZincOxide { get; } =
        new CorrosionProductMaterial(
            name: "ZnO",
            ionicConductivity: 1.0e-6,
            molarVolume: 1.45e-5,
            solubilityProduct: 3.0e-17,
            barrierResistanceFactor: 8.0);

    /// <summary>Aluminium oxide corrosion product, Al₂O₃.</summary>
    public static ICorrosionProductMaterial AluminiumOxide { get; } =
        new CorrosionProductMaterial(
            name: "Al2O3",
            ionicConductivity: 1.0e-8,
            molarVolume: 2.56e-5,
            solubilityProduct: 1.0e-33,
            barrierResistanceFactor: 25.0);

    /// <summary>
    /// Returns the effective ionic conductivity to assign to corrosion-product nodes.
    /// </summary>
    public static double GetEffectiveConductivity(ICorrosionProductMaterial material)
    {
        ArgumentNullException.ThrowIfNull(material);
        return material.IonicConductivity;
    }

    /// <summary>
    /// Applies the corrosion-product barrier factor to a current density.
    /// </summary>
    public static double ApplyBarrierResistance(double currentDensity, ICorrosionProductMaterial? material)
    {
        if (material is null)
            return currentDensity;

        return currentDensity / Math.Max(material.BarrierResistanceFactor, 1.0);
    }

    /// <summary>
    /// Converts precipitated moles into a solid corrosion-product volume.
    /// </summary>
    public static double ComputeSolidVolume(double precipitatedMoles, ICorrosionProductMaterial material)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(precipitatedMoles, 0.0, nameof(precipitatedMoles));
        ArgumentNullException.ThrowIfNull(material);

        return precipitatedMoles * material.MolarVolume;
    }

    /// <summary>
    /// Computes the saturation index Ω = IAP / Ksp for the corrosion product.
    /// </summary>
    public static double ComputeSaturationIndex(double ionActivityProduct, ICorrosionProductMaterial material)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ionActivityProduct, 0.0, nameof(ionActivityProduct));
        ArgumentNullException.ThrowIfNull(material);

        return ionActivityProduct / material.SolubilityProduct;
    }

    /// <summary>
    /// Returns whether the layer is undersaturated and therefore thermodynamically favoured to re-dissolve.
    /// </summary>
    public static bool ShouldRedissolve(double ionActivityProduct, ICorrosionProductMaterial material) =>
        ComputeSaturationIndex(ionActivityProduct, material) < 1.0;
}
