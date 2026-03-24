namespace GCE.Configuration;

/// <summary>
/// Specifies a material for a galvanic pair member.
/// </summary>
/// <remarks>
/// <para>
/// To reference a material from the built-in registry, set only <see cref="Name"/>
/// (e.g. <c>"Zinc"</c>, <c>"Mild Steel"</c>, <c>"Aluminium"</c>, <c>"Copper"</c>,
/// <c>"Nickel"</c>, <c>"Magnesium"</c>).
/// </para>
/// <para>
/// To define a custom material, supply <see cref="Name"/> together with all of
/// <see cref="StandardPotential"/>, <see cref="ExchangeCurrentDensity"/>,
/// <see cref="MolarMass"/>, <see cref="ElectronsTransferred"/>, and
/// <see cref="Density"/>.
/// </para>
/// </remarks>
public sealed class MaterialConfig
{
    /// <summary>
    /// Gets or sets the material name.
    /// Used for registry lookup when no electrochemical properties are provided,
    /// or as the display name when defining a custom material.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the standard electrochemical potential (V vs. SHE).</summary>
    public double? StandardPotential { get; set; }

    /// <summary>Gets or sets the exchange current density (A/m²).</summary>
    public double? ExchangeCurrentDensity { get; set; }

    /// <summary>Gets or sets the molar mass (kg/mol).</summary>
    public double? MolarMass { get; set; }

    /// <summary>Gets or sets the electrons transferred per formula unit in anodic dissolution.</summary>
    public int? ElectronsTransferred { get; set; }

    /// <summary>Gets or sets the density (kg/m³).</summary>
    public double? Density { get; set; }

    /// <summary>
    /// Returns <see langword="true"/> when this entry should be resolved from
    /// the material registry (i.e. only <see cref="Name"/> is specified and no
    /// electrochemical properties are set).
    /// </summary>
    public bool IsRegistryLookup =>
        Name is not null
        && StandardPotential is null
        && ExchangeCurrentDensity is null
        && MolarMass is null
        && ElectronsTransferred is null
        && Density is null;
}
