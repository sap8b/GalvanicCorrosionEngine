namespace GCE.Core;

/// <summary>
/// Abstract base class for materials that participate in galvanic corrosion.
/// </summary>
/// <remarks>
/// <para>
/// Provides a conventional, property-based implementation of <see cref="IMaterial"/>
/// with constructor validation.  Subclass this to create named alloy families or to
/// attach domain-specific metadata (e.g., alloy designation, heat-treatment condition)
/// without modifying existing code — in accordance with the Open/Closed Principle.
/// </para>
/// <para>
/// For lightweight, data-only materials the existing <see cref="Material"/> record
/// remains the simplest choice.  <see cref="MaterialBase"/> is intended for richer
/// material hierarchies such as <see cref="Alloy"/>.
/// </para>
/// </remarks>
public abstract class MaterialBase : IMaterial
{
    /// <summary>
    /// Initialises a new instance of <see cref="MaterialBase"/>.
    /// </summary>
    /// <param name="name">Display name of the material (must not be null or whitespace).</param>
    /// <param name="standardPotential">Standard electrochemical potential (V vs. SHE).</param>
    /// <param name="exchangeCurrentDensity">Exchange current density (A/m²); must be positive.</param>
    /// <param name="molarMass">Molar mass (kg/mol); must be positive.</param>
    /// <param name="electronsTransferred">Electrons per formula unit in anodic dissolution; must be positive.</param>
    /// <param name="density">Density (kg/m³); must be positive.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is null or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="exchangeCurrentDensity"/>, <paramref name="molarMass"/>,
    /// <paramref name="electronsTransferred"/>, or <paramref name="density"/> is not positive.
    /// </exception>
    protected MaterialBase(
        string name,
        double standardPotential,
        double exchangeCurrentDensity,
        double molarMass,
        int    electronsTransferred,
        double density)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(exchangeCurrentDensity, 0.0, nameof(exchangeCurrentDensity));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(molarMass,              0.0, nameof(molarMass));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(electronsTransferred,   0,   nameof(electronsTransferred));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(density,                0.0, nameof(density));

        Name                   = name;
        StandardPotential      = standardPotential;
        ExchangeCurrentDensity = exchangeCurrentDensity;
        MolarMass              = molarMass;
        ElectronsTransferred   = electronsTransferred;
        Density                = density;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public double StandardPotential { get; }

    /// <inheritdoc/>
    public double ExchangeCurrentDensity { get; }

    /// <inheritdoc/>
    public double MolarMass { get; }

    /// <inheritdoc/>
    public int ElectronsTransferred { get; }

    /// <inheritdoc/>
    public double Density { get; }

    /// <inheritdoc/>
    public override string ToString() => Name;
}
