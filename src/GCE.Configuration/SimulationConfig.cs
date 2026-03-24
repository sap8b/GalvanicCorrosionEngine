namespace GCE.Configuration;

/// <summary>
/// Top-level configuration for a galvanic corrosion simulation.
/// </summary>
/// <remarks>
/// A <see cref="SimulationConfig"/> can be loaded from a JSON or YAML file using
/// <see cref="SimulationConfigReader"/>, validated with
/// <see cref="SimulationConfigValidator"/>, and converted to a
/// <c>SimulationParameters</c> instance using <see cref="SimulationConfigMapper"/>.
/// </remarks>
public sealed class SimulationConfig
{
    /// <summary>
    /// Gets or sets the anode material. Defaults to Zinc from the built-in registry.
    /// </summary>
    public MaterialConfig Anode { get; set; } = new() { Name = "Zinc" };

    /// <summary>
    /// Gets or sets the cathode material. Defaults to Mild Steel from the built-in registry.
    /// </summary>
    public MaterialConfig Cathode { get; set; } = new() { Name = "Mild Steel" };

    /// <summary>
    /// Gets or sets the static atmospheric environment. Used when no weather provider is configured.
    /// </summary>
    public EnvironmentConfig Environment { get; set; } = new();

    /// <summary>
    /// Gets or sets the simulation duration in seconds. Default is 3600 s (1 hour).
    /// </summary>
    public double DurationSeconds { get; set; } = 3600.0;

    /// <summary>
    /// Gets or sets the number of time integration steps. Default is 1000.
    /// </summary>
    public int TimeSteps { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the optional weather provider configuration.
    /// When <see langword="null"/> (or <see cref="WeatherConfig.Type"/> is
    /// <see cref="WeatherProviderType.None"/>), the static <see cref="Environment"/> is used.
    /// </summary>
    public WeatherConfig? Weather { get; set; }
}
