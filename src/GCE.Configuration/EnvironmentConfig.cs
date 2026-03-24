namespace GCE.Configuration;

/// <summary>
/// Configuration for the static atmospheric environment used in a simulation.
/// </summary>
public sealed class EnvironmentConfig
{
    /// <summary>Gets or sets the ambient temperature in °C. Default is 25 °C.</summary>
    public double TemperatureCelsius { get; set; } = 25.0;

    /// <summary>
    /// Gets or sets the relative humidity as a fraction in [0, 1]. Default is 0.80.
    /// </summary>
    public double RelativeHumidity { get; set; } = 0.80;

    /// <summary>
    /// Gets or sets the chloride ion concentration (mol/L). Default is 0.5 mol/L.
    /// </summary>
    public double ChlorideConcentration { get; set; } = 0.5;
}
