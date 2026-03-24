using GCE.Core;

namespace GCE.Electrochemistry;

/// <summary>
/// Models a bulk liquid electrolyte such as a seawater bath or a laboratory
/// NaCl solution.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="ThinFilmElectrolyte"/>, this class assumes infinite
/// volume (no film-thickness correction).  Conductivity is computed from
/// registered ionic species using the Kohlrausch approximation, or from
/// a single total-concentration value when no species have been registered.
/// </para>
/// <para>
/// pH is calculated from the H⁺ species concentration when available;
/// otherwise a neutral value of 7 is returned.
/// </para>
/// </remarks>
public sealed class BulkElectrolyte : IElectrolyte
{
    private readonly List<Species> _species = new();

    // ── Constructors ──────────────────────────────────────────────────────────

    /// <param name="temperatureCelsius">Temperature in °C (default 25 °C).</param>
    /// <param name="totalConcentration">
    /// Fallback total ionic concentration (mol/m³) used when no species are
    /// registered.  Must be non-negative (default 1000 mol/m³ ≈ 1 mol/L).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="totalConcentration"/> is negative.
    /// </exception>
    public BulkElectrolyte(double temperatureCelsius = 25.0, double totalConcentration = 1000.0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalConcentration, nameof(totalConcentration));

        TemperatureCelsius = temperatureCelsius;
        TotalConcentration = totalConcentration;
    }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>Gets the temperature in degrees Celsius.</summary>
    public double TemperatureCelsius { get; }

    /// <summary>
    /// Gets the fallback total ionic concentration (mol/m³) used for
    /// conductivity calculations when no species are registered.
    /// </summary>
    public double TotalConcentration { get; }

    /// <summary>Gets the registered species in this electrolyte.</summary>
    public IReadOnlyList<Species> Species => _species;

    // ── IEnvironment / IElectrolyte ───────────────────────────────────────────

    /// <inheritdoc/>
    public double TemperatureKelvin => TemperatureCelsius + 273.15;

    /// <inheritdoc/>
    /// <remarks>
    /// Calculated as −log₁₀([H⁺] / 1000) where [H⁺] is in mol/m³.
    /// If no H⁺ species is registered, returns 7.0.
    /// </remarks>
    public double pH => ElectrolyteCalculations.ComputePh(_species);

    /// <inheritdoc/>
    /// <remarks>
    /// Computed from registered species using the Kohlrausch approximation,
    /// or from the total concentration when no species are registered.
    /// </remarks>
    public double IonicConductivity =>
        _species.Count > 0
            ? ElectrolyteCalculations.ConductivityFromSpecies(_species)
            : ElectrolyteCalculations.ConductivityFromTotal(TotalConcentration);

    /// <inheritdoc/>
    /// <remarks>
    /// Returns the sum of all positive-ion concentrations in mol/m³,
    /// or <see cref="TotalConcentration"/> when no species are registered.
    /// </remarks>
    public double Concentration => ComputeConcentration();

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds an ionic or dissolved-gas species to this electrolyte.
    /// </summary>
    /// <param name="species">The species to add; must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="species"/> is null.
    /// </exception>
    public void AddSpecies(Species species)
    {
        ArgumentNullException.ThrowIfNull(species);
        _species.Add(species);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private double ComputeConcentration()
    {
        if (_species.Count == 0)
            return TotalConcentration;

        double sum = _species.Where(s => s.Charge > 0).Sum(s => s.Concentration);
        return sum > 0.0 ? sum : _species.Sum(s => s.Concentration);
    }
}
