using GCE.Core;

namespace GCE.Atmosphere;

/// <summary>
/// Models atmospheric environmental conditions relevant to galvanic corrosion.
/// </summary>
/// <param name="TemperatureCelsius">Ambient temperature in °C.</param>
/// <param name="RelativeHumidity">Relative humidity as a fraction (0–1).</param>
/// <param name="ChlorideConcentration">Chloride ion concentration (mol/L).</param>
public sealed record AtmosphericConditions(
    double TemperatureCelsius,
    double RelativeHumidity,
    double ChlorideConcentration = 0.0) : IEnvironment
{
    private const double AbsoluteZero = 273.15;

    /// <inheritdoc/>
    public double TemperatureKelvin => TemperatureCelsius + AbsoluteZero;

    /// <inheritdoc/>
    public double pH =>
        // Approximate pH: pure water baseline shifted by chloride concentration
        7.0 - Math.Log10(1.0 + ChlorideConcentration);

    /// <inheritdoc/>
    public double IonicConductivity =>
        // Empirical approximation: conductivity (S/m) driven by RH and chloride
        RelativeHumidity * (0.01 + 0.5 * ChlorideConcentration);
}
