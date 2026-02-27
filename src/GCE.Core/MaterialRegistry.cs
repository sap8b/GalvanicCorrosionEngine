namespace GCE.Core;

/// <summary>
/// Registry of <see cref="IMaterial"/> instances using the factory pattern.
/// Pre-configured common metals are available as static properties; custom
/// materials can be registered at runtime with <see cref="Register"/>.
/// </summary>
public static class MaterialRegistry
{
    private static readonly Dictionary<string, IMaterial> _registry =
        new(StringComparer.OrdinalIgnoreCase);

    // -----------------------------------------------------------------------
    // Pre-configured common metals
    // Electrochemical data: standard potentials vs. SHE, exchange current
    // densities, molar masses, dissolution valence, and bulk densities.
    // -----------------------------------------------------------------------

    /// <summary>Zinc (Zn²⁺/Zn, −0.76 V vs. SHE).</summary>
    public static IMaterial Zinc { get; } =
        Register(new Material("Zinc", StandardPotential: -0.76, ExchangeCurrentDensity: 1e-3,
            MolarMass: 0.06538, ElectronsTransferred: 2, Density: 7133.0));

    /// <summary>Mild steel / iron (Fe²⁺/Fe, −0.44 V vs. SHE).</summary>
    public static IMaterial MildSteel { get; } =
        Register(new Material("Mild Steel", StandardPotential: -0.44, ExchangeCurrentDensity: 1e-4,
            MolarMass: 0.05585, ElectronsTransferred: 2, Density: 7874.0));

    /// <summary>Aluminium (Al³⁺/Al, −1.66 V vs. SHE).</summary>
    public static IMaterial Aluminium { get; } =
        Register(new Material("Aluminium", StandardPotential: -1.66, ExchangeCurrentDensity: 1e-6,
            MolarMass: 0.02698, ElectronsTransferred: 3, Density: 2700.0));

    /// <summary>Copper (Cu²⁺/Cu, +0.34 V vs. SHE).</summary>
    public static IMaterial Copper { get; } =
        Register(new Material("Copper", StandardPotential: 0.34, ExchangeCurrentDensity: 1e-3,
            MolarMass: 0.06355, ElectronsTransferred: 2, Density: 8960.0));

    /// <summary>Nickel (Ni²⁺/Ni, −0.25 V vs. SHE).</summary>
    public static IMaterial Nickel { get; } =
        Register(new Material("Nickel", StandardPotential: -0.25, ExchangeCurrentDensity: 1e-5,
            MolarMass: 0.05869, ElectronsTransferred: 2, Density: 8908.0));

    /// <summary>Magnesium (Mg²⁺/Mg, −2.37 V vs. SHE).</summary>
    public static IMaterial Magnesium { get; } =
        Register(new Material("Magnesium", StandardPotential: -2.37, ExchangeCurrentDensity: 1e-5,
            MolarMass: 0.02430, ElectronsTransferred: 2, Density: 1738.0));

    // -----------------------------------------------------------------------
    // Registry operations
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the material registered under <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The material name (case-insensitive).</param>
    /// <returns>The registered <see cref="IMaterial"/>.</returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no material with the given name has been registered.
    /// </exception>
    public static IMaterial Get(string name)
    {
        if (_registry.TryGetValue(name, out IMaterial? material))
            return material;

        throw new KeyNotFoundException($"No material named '{name}' is registered.");
    }

    /// <summary>
    /// Attempts to retrieve a material by <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The material name (case-insensitive).</param>
    /// <param name="material">
    /// When this method returns <see langword="true"/>, contains the material; otherwise <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if found; otherwise <see langword="false"/>.</returns>
    public static bool TryGet(string name, out IMaterial? material) =>
        _registry.TryGetValue(name, out material);

    /// <summary>
    /// Registers a custom <paramref name="material"/>, making it retrievable by name.
    /// If a material with the same name is already registered it is replaced.
    /// </summary>
    /// <param name="material">The material to register.</param>
    /// <returns>The same <paramref name="material"/> instance, for fluent use.</returns>
    public static IMaterial Register(IMaterial material)
    {
        _registry[material.Name] = material;
        return material;
    }

    /// <summary>
    /// Returns the names of all currently registered materials.
    /// </summary>
    public static IReadOnlyCollection<string> RegisteredNames =>
        _registry.Keys;
}
