using GCE.Core;

namespace GCE.Electrochemistry;

/// <summary>
/// Models a thin electrolyte film such as those encountered on metal surfaces
/// in atmospheric corrosion.
/// </summary>
/// <remarks>
/// <para>
/// The thin-film geometry concentrates ionic species as the film thins, which
/// increases the effective ionic conductivity and lowers the buffering capacity.
/// </para>
/// <para>
/// Ionic conductivity uses the Kohlrausch empirical model:
/// <code>κ = Σ |z_i| · λ_i · c_i · (1 − B·√c_total)</code>
/// where λ_i is the limiting molar conductivity and B is an empirical constant.
/// When no individual species are registered the total concentration
/// (<see cref="TotalConcentration"/>) is used with a default limiting molar
/// conductivity of 76 S·cm²/mol (typical for NaCl-like electrolytes).
/// </para>
/// <para>
/// pH is calculated directly from the H⁺ species concentration.  If no H⁺
/// species has been registered, a neutral pH of 7 is assumed.
/// </para>
/// </remarks>
public sealed class ThinFilmElectrolyte : IElectrolyte
{
    private readonly List<Species> _species = new();

    // ── Constructors ──────────────────────────────────────────────────────────

    /// <param name="filmThickness">
    /// Film thickness in metres (must be positive).
    /// </param>
    /// <param name="temperatureCelsius">
    /// Temperature in °C (default 25 °C).
    /// </param>
    /// <param name="totalConcentration">
    /// Fallback total ionic concentration (mol/m³) used when no species are
    /// registered.  Must be non-negative (default 100 mol/m³ ≈ 0.1 mol/L).
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="filmThickness"/> is not positive, or when
    /// <paramref name="totalConcentration"/> is negative.
    /// </exception>
    public ThinFilmElectrolyte(
        double filmThickness,
        double temperatureCelsius = 25.0,
        double totalConcentration = 100.0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(filmThickness, nameof(filmThickness));
        ArgumentOutOfRangeException.ThrowIfNegative(totalConcentration, nameof(totalConcentration));

        FilmThickness = filmThickness;
        TemperatureCelsius = temperatureCelsius;
        TotalConcentration = totalConcentration;
    }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>Gets the film thickness in metres.</summary>
    public double FilmThickness { get; }

    /// <summary>Gets the temperature in degrees Celsius.</summary>
    public double TemperatureCelsius { get; }

    /// <summary>
    /// Gets the fallback total ionic concentration (mol/m³) used for
    /// conductivity calculations when no species are registered.
    /// </summary>
    public double TotalConcentration { get; }

    /// <summary>Gets the registered species in this film.</summary>
    public IReadOnlyList<Species> Species => _species;

    // ── IEnvironment / IElectrolyte ───────────────────────────────────────────

    /// <inheritdoc/>
    public double TemperatureKelvin => TemperatureCelsius + 273.15;

    /// <inheritdoc/>
    /// <remarks>
    /// Calculated as −log₁₀([H⁺] / 1000) where [H⁺] is in mol/m³.
    /// (Dividing by 1000 converts mol/m³ to mol/L for the standard pH definition.)
    /// If no H⁺ species is registered, returns 7.0.
    /// </remarks>
    public double pH => ElectrolyteCalculations.ComputePh(_species);

    /// <inheritdoc/>
    /// <remarks>
    /// Computed from registered species using the Kohlrausch approximation,
    /// scaled by the film-thickness correction factor (thinner films are more
    /// concentrated, raising conductivity).  Falls back to a
    /// total-concentration-based estimate when no species are registered.
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
    /// Adds an ionic or dissolved-gas species to this electrolyte film.
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
