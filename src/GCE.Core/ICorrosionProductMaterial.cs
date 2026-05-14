namespace GCE.Core;

/// <summary>
/// Defines electrochemical properties for a corrosion-product layer.
/// </summary>
public interface ICorrosionProductMaterial
{
    /// <summary>Gets the display name of the corrosion product.</summary>
    string Name { get; }

    /// <summary>Gets the ionic conductivity of the corrosion product (S/m).</summary>
    double IonicConductivity { get; }

    /// <summary>Gets the molar volume of the solid corrosion product (m³/mol).</summary>
    double MolarVolume { get; }

    /// <summary>Gets the solubility product Ksp for dissolution/re-precipitation.</summary>
    double SolubilityProduct { get; }

    /// <summary>
    /// Gets a dimensionless multiplier describing added polarization resistance through the layer.
    /// </summary>
    double BarrierResistanceFactor { get; }
}
