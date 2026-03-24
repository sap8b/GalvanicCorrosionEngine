namespace GCE.Core;

/// <summary>
/// Represents a snapshot of atmospheric weather conditions at a single point in time.
/// </summary>
public interface IWeatherObservation
{
    /// <summary>Gets the ambient air temperature in degrees Celsius.</summary>
    double TemperatureCelsius { get; }

    /// <summary>Gets the relative humidity as a fraction (0–1).</summary>
    double RelativeHumidity { get; }

    /// <summary>Gets the chloride ion concentration (mol/L) in the surface electrolyte layer.</summary>
    double ChlorideConcentration { get; }

    /// <summary>Gets the precipitation rate in mm/h. Zero indicates dry conditions.</summary>
    double Precipitation { get; }

    /// <summary>Gets the wind speed in m/s.</summary>
    double WindSpeed { get; }
}
