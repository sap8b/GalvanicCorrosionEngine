namespace GCE.Configuration;

/// <summary>
/// Identifies the kind of weather provider to use in a simulation.
/// </summary>
public enum WeatherProviderType
{
    /// <summary>No weather provider; the static <see cref="EnvironmentConfig"/> is used throughout.</summary>
    None,

    /// <summary>A synthetic diurnal (day/night) cycle generated from parametric values.</summary>
    Synthetic,

    /// <summary>Observations loaded from a CSV file.</summary>
    Csv,
}

/// <summary>
/// Configuration for the weather provider used in a time-varying simulation.
/// </summary>
/// <remarks>
/// Set <see cref="Type"/> to <see cref="WeatherProviderType.Synthetic"/> and supply
/// the diurnal parameters, or set it to <see cref="WeatherProviderType.Csv"/> and
/// supply <see cref="CsvPath"/>.
/// </remarks>
public sealed class WeatherConfig
{
    /// <summary>Gets or sets the type of weather provider. Default is <see cref="WeatherProviderType.None"/>.</summary>
    public WeatherProviderType Type { get; set; } = WeatherProviderType.None;

    // ── Synthetic weather parameters ──────────────────────────────────────────

    /// <summary>Daily-mean temperature in °C. Default is 15 °C.</summary>
    public double BaseTempCelsius { get; set; } = 15.0;

    /// <summary>Half-range of the diurnal temperature swing in °C. Default is 8 °C.</summary>
    public double TempAmplitude { get; set; } = 8.0;

    /// <summary>Daily-mean relative humidity as a fraction [0, 1]. Default is 0.70.</summary>
    public double BaseRelativeHumidity { get; set; } = 0.70;

    /// <summary>Half-range of the diurnal humidity swing. Default is 0.15.</summary>
    public double HumidityAmplitude { get; set; } = 0.15;

    /// <summary>Chloride concentration (mol/L) used by the synthetic provider. Default is 0.05 mol/L.</summary>
    public double ChlorideConcentration { get; set; } = 0.05;

    /// <summary>Constant precipitation rate (mm/h). Default is 0 (dry).</summary>
    public double Precipitation { get; set; } = 0.0;

    /// <summary>Constant wind speed (m/s). Default is 2.0 m/s.</summary>
    public double WindSpeed { get; set; } = 2.0;

    // ── CSV weather parameters ─────────────────────────────────────────────────

    /// <summary>
    /// Path to the CSV weather data file. Required when <see cref="Type"/> is
    /// <see cref="WeatherProviderType.Csv"/>.
    /// </summary>
    public string? CsvPath { get; set; }
}
