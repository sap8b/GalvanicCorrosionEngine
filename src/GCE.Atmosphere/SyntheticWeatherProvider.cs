using GCE.Core;

namespace GCE.Atmosphere;

/// <summary>
/// Generates synthetic weather observations using an idealised diurnal (day/night) cycle.
/// </summary>
/// <remarks>
/// Temperature and relative humidity follow sinusoidal profiles over a 24-hour period.
/// Temperature peaks at 14:00 (solar time) and reaches its minimum near 04:00.
/// Humidity is inversely correlated with temperature.
/// Chloride concentration, precipitation, and wind speed remain constant throughout
/// the cycle unless overridden by a subclass.
/// </remarks>
public sealed class SyntheticWeatherProvider : IWeatherProvider
{
    private const double DaySeconds = 86_400.0;

    // Peak temperature at 14:00 (50 400 s) → sin(phase) = 1 at t = 50 400 s
    // 2π·t_peak/T + offset = π/2  ⟹  offset = π/2 − 2π·(50400/86400) = π/2 − 7π/6 = −2π/3
    private const double PhaseOffsetRadians = -2.0 * Math.PI / 3.0;

    private readonly double _baseTempCelsius;
    private readonly double _tempAmplitude;
    private readonly double _baseRelativeHumidity;
    private readonly double _humidityAmplitude;
    private readonly double _chlorideConcentration;
    private readonly double _precipitation;
    private readonly double _windSpeed;

    /// <summary>
    /// Initialises a new <see cref="SyntheticWeatherProvider"/> with the given diurnal parameters.
    /// </summary>
    /// <param name="baseTempCelsius">Daily-mean temperature in °C (default 15 °C).</param>
    /// <param name="tempAmplitude">Half-range of the diurnal temperature swing in °C (default 8 °C). Zero produces a constant temperature throughout the day.</param>
    /// <param name="baseRelativeHumidity">Daily-mean relative humidity as a fraction (default 0.70).</param>
    /// <param name="humidityAmplitude">Half-range of the diurnal humidity swing (default 0.15).</param>
    /// <param name="chlorideConcentration">Constant chloride ion concentration in mol/L (default 0.05).</param>
    /// <param name="precipitation">Constant precipitation rate in mm/h (default 0 — dry).</param>
    /// <param name="windSpeed">Constant wind speed in m/s (default 2.0).</param>
    public SyntheticWeatherProvider(
        double baseTempCelsius = 15.0,
        double tempAmplitude = 8.0,
        double baseRelativeHumidity = 0.70,
        double humidityAmplitude = 0.15,
        double chlorideConcentration = 0.05,
        double precipitation = 0.0,
        double windSpeed = 2.0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tempAmplitude);
        ArgumentOutOfRangeException.ThrowIfLessThan(baseRelativeHumidity, 0.0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(baseRelativeHumidity, 1.0);
        ArgumentOutOfRangeException.ThrowIfNegative(humidityAmplitude);
        ArgumentOutOfRangeException.ThrowIfNegative(chlorideConcentration);
        ArgumentOutOfRangeException.ThrowIfNegative(precipitation);
        ArgumentOutOfRangeException.ThrowIfNegative(windSpeed);

        _baseTempCelsius = baseTempCelsius;
        _tempAmplitude = tempAmplitude;
        _baseRelativeHumidity = baseRelativeHumidity;
        _humidityAmplitude = humidityAmplitude;
        _chlorideConcentration = chlorideConcentration;
        _precipitation = precipitation;
        _windSpeed = windSpeed;
    }

    /// <inheritdoc/>
    public IWeatherObservation GetObservation(double timeSeconds)
    {
        double phase = 2.0 * Math.PI * timeSeconds / DaySeconds + PhaseOffsetRadians;

        double temp = _baseTempCelsius + _tempAmplitude * Math.Sin(phase);

        // Humidity is inversely correlated with temperature
        double humidity = _baseRelativeHumidity - _humidityAmplitude * Math.Sin(phase);
        humidity = Math.Clamp(humidity, 0.0, 1.0);

        return new WeatherObservation(temp, humidity, _chlorideConcentration, _precipitation, _windSpeed);
    }
}
