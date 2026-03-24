namespace GCE.Electrochemistry;

/// <summary>
/// Represents an ionic species or dissolved gas present in an electrolyte.
/// </summary>
/// <remarks>
/// <para>
/// A species is characterised by its name, ionic charge (zero for neutral dissolved gases),
/// aqueous diffusion coefficient, and current molar concentration.
/// </para>
/// <para>
/// The diffusion coefficient governs how quickly the species migrates through the
/// electrolyte during transport calculations (see <see cref="SpeciesTransport"/>).
/// </para>
/// </remarks>
public sealed class Species
{
    /// <summary>Gets the chemical name or symbol of the species (e.g. "Na+", "Cl-", "O2").</summary>
    public string Name { get; }

    /// <summary>Gets the ionic charge number (e.g. +1 for Na⁺, −1 for Cl⁻, 0 for O₂).</summary>
    public int Charge { get; }

    /// <summary>
    /// Gets the aqueous diffusion coefficient D (m²/s) at the reference temperature.
    /// Must be non-negative (use 0 for an immobile species).
    /// </summary>
    public double DiffusionCoefficient { get; }

    /// <summary>
    /// Gets or sets the molar concentration (mol/m³).
    /// Must be non-negative.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is negative.
    /// </exception>
    public double Concentration
    {
        get => _concentration;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(value));
            _concentration = value;
        }
    }

    private double _concentration;

    /// <param name="name">Chemical name or symbol; must not be null or whitespace.</param>
    /// <param name="charge">Ionic charge number (may be negative, zero, or positive).</param>
    /// <param name="diffusionCoefficient">Aqueous diffusion coefficient D (m²/s); must be non-negative.</param>
    /// <param name="concentration">Initial molar concentration (mol/m³); must be non-negative.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="diffusionCoefficient"/> or <paramref name="concentration"/> is negative.
    /// </exception>
    public Species(string name, int charge, double diffusionCoefficient, double concentration = 0.0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Species name must not be null or whitespace.", nameof(name));
        ArgumentOutOfRangeException.ThrowIfNegative(diffusionCoefficient, nameof(diffusionCoefficient));
        ArgumentOutOfRangeException.ThrowIfNegative(concentration, nameof(concentration));

        Name = name;
        Charge = charge;
        DiffusionCoefficient = diffusionCoefficient;
        _concentration = concentration;
    }

    /// <summary>
    /// Creates a clone of this species with an optionally overridden concentration.
    /// </summary>
    /// <param name="concentration">New concentration (mol/m³); defaults to the current value.</param>
    /// <returns>A new <see cref="Species"/> with the same name, charge, and diffusion coefficient.</returns>
    public Species WithConcentration(double concentration) =>
        new(Name, Charge, DiffusionCoefficient, concentration);

    /// <inheritdoc/>
    public override string ToString() =>
        $"{Name} (z={Charge:+0;-0;0}, D={DiffusionCoefficient:G3} m²/s, c={_concentration:G3} mol/m³)";
}
