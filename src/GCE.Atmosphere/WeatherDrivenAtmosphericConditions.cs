using GCE.Core;

namespace GCE.Atmosphere;

/// <summary>
/// Implements <see cref="IElectrolyte"/> by deriving electrochemical properties
/// directly from a <see cref="IWeatherObservation"/>.
/// </summary>
/// <remarks>
/// This class bridges the weather data layer and the electrochemistry layer:
/// temperature and humidity from a weather observation are translated into the
/// ionic conductivity and pH that the corrosion models require.
/// The same empirical correlations used by <see cref="AtmosphericConditions"/> are applied.
/// </remarks>
public sealed class WeatherDrivenAtmosphericConditions : IElectrolyte
{
    private const double AbsoluteZero = 273.15;

    private readonly IWeatherObservation _observation;

    /// <summary>
    /// Initialises a new instance backed by the given weather observation.
    /// </summary>
    /// <param name="observation">The weather snapshot to derive conditions from.</param>
    public WeatherDrivenAtmosphericConditions(IWeatherObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        _observation = observation;
    }

    /// <summary>Gets the underlying weather observation.</summary>
    public IWeatherObservation Observation => _observation;

    /// <inheritdoc/>
    public double TemperatureKelvin => _observation.TemperatureCelsius + AbsoluteZero;

    /// <inheritdoc/>
    public double pH =>
        // Approximate pH: pure water baseline shifted by chloride concentration
        7.0 - Math.Log10(1.0 + _observation.ChlorideConcentration);

    /// <inheritdoc/>
    public double IonicConductivity =>
        // Empirical approximation: conductivity (S/m) driven by RH and chloride
        _observation.RelativeHumidity * (0.01 + 0.5 * _observation.ChlorideConcentration);

    /// <inheritdoc/>
    public double Concentration => _observation.ChlorideConcentration;
}
