using GCE.Core;

namespace GCE.Atmosphere;

/// <summary>
/// Immutable record representing a single atmospheric weather observation.
/// </summary>
/// <param name="TemperatureCelsius">Ambient air temperature in °C.</param>
/// <param name="RelativeHumidity">Relative humidity as a fraction (0–1).</param>
/// <param name="ChlorideConcentration">Chloride ion concentration (mol/L) in the surface electrolyte layer.</param>
/// <param name="Precipitation">Precipitation rate in mm/h (default 0 — dry conditions).</param>
/// <param name="WindSpeed">Wind speed in m/s (default 0).</param>
public sealed record WeatherObservation(
    double TemperatureCelsius,
    double RelativeHumidity,
    double ChlorideConcentration,
    double Precipitation = 0.0,
    double WindSpeed = 0.0) : IWeatherObservation;
