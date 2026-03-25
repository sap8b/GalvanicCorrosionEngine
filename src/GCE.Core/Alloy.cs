namespace GCE.Core;

/// <summary>
/// Represents a specific engineering alloy (e.g., AA7075-T6, SS316L, AZ31B)
/// that can participate in galvanic corrosion.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Alloy"/> extends <see cref="MaterialBase"/> with an optional
/// industry <see cref="Designation"/> (e.g., <c>"AA7075-T6"</c>,
/// <c>"UNS S31603"</c>, <c>"ASTM AZ31B"</c>).
/// </para>
/// <para>
/// Register custom alloys at runtime via <see cref="MaterialRegistry.Register"/>:
/// <code>
/// MaterialRegistry.Register(new Alloy(
///     "My Alloy", standardPotential: -0.60, exchangeCurrentDensity: 5e-6,
///     molarMass: 0.02698, electronsTransferred: 3, density: 2810.0,
///     designation: "AA7075-T6"));
/// </code>
/// </para>
/// </remarks>
public sealed class Alloy : MaterialBase
{
    /// <summary>
    /// Initialises a new <see cref="Alloy"/> instance.
    /// </summary>
    /// <param name="name">Display name (e.g., <c>"AA7075"</c>, <c>"SS316L"</c>).</param>
    /// <param name="standardPotential">Standard electrochemical potential (V vs. SHE).</param>
    /// <param name="exchangeCurrentDensity">Exchange current density (A/m²); must be positive.</param>
    /// <param name="molarMass">Molar mass (kg/mol); must be positive.</param>
    /// <param name="electronsTransferred">Electrons per formula unit in anodic dissolution; must be positive.</param>
    /// <param name="density">Density (kg/m³); must be positive.</param>
    /// <param name="designation">
    /// Optional industry designation (e.g., <c>"AA7075-T6"</c>, <c>"UNS S31603"</c>).
    /// </param>
    public Alloy(
        string  name,
        double  standardPotential,
        double  exchangeCurrentDensity,
        double  molarMass,
        int     electronsTransferred,
        double  density,
        string? designation = null)
        : base(name, standardPotential, exchangeCurrentDensity, molarMass, electronsTransferred, density)
    {
        Designation = designation;
    }

    /// <summary>
    /// Gets the optional industry designation for the alloy
    /// (e.g., <c>"AA7075-T6"</c>, <c>"UNS S31603"</c>, <c>"ASTM AZ31B"</c>).
    /// </summary>
    public string? Designation { get; }
}
