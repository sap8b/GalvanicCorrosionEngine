using GCE.Atmosphere;
using GCE.Core;
using GCE.Electrochemistry;
using GCE.Simulation;

namespace GCE.Configuration;

/// <summary>
/// Maps a validated <see cref="SimulationConfig"/> to a <see cref="SimulationParameters"/>
/// instance that can be passed directly to <c>SimulationEngine.Run</c>.
/// </summary>
public sealed class SimulationConfigMapper
{
    /// <summary>
    /// Converts the given <paramref name="config"/> to a <see cref="SimulationParameters"/>.
    /// </summary>
    /// <param name="config">A previously validated simulation configuration.</param>
    /// <returns>A fully constructed <see cref="SimulationParameters"/> ready for simulation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when a registry-referenced material name is not found.
    /// </exception>
    public SimulationParameters Map(SimulationConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        IMaterial anode   = ResolveMaterial(config.Anode,   "anode");
        IMaterial cathode = ResolveMaterial(config.Cathode, "cathode");
        var pair = new GalvanicPair(anode, cathode);

        var environment = new AtmosphericConditions(
            config.Environment.TemperatureCelsius,
            config.Environment.RelativeHumidity,
            config.Environment.ChlorideConcentration);

        IWeatherProvider? weatherProvider = ResolveWeatherProvider(config.Weather);

        return new SimulationParameters(
            pair,
            environment,
            config.DurationSeconds,
            config.TimeSteps,
            weatherProvider);
    }

    // ── Material resolution ───────────────────────────────────────────────────

    private static IMaterial ResolveMaterial(MaterialConfig cfg, string role)
    {
        if (cfg.IsRegistryLookup)
            return MaterialRegistry.Get(cfg.Name!);

        // Custom material — all properties are guaranteed non-null after validation
        return new Material(
            Name:                  cfg.Name ?? role,
            StandardPotential:     cfg.StandardPotential!.Value,
            ExchangeCurrentDensity: cfg.ExchangeCurrentDensity!.Value,
            MolarMass:             cfg.MolarMass!.Value,
            ElectronsTransferred:  cfg.ElectronsTransferred!.Value,
            Density:               cfg.Density!.Value);
    }

    // ── Weather provider resolution ───────────────────────────────────────────

    private static IWeatherProvider? ResolveWeatherProvider(WeatherConfig? cfg)
    {
        if (cfg is null || cfg.Type == WeatherProviderType.None)
            return null;

        return cfg.Type switch
        {
            WeatherProviderType.Synthetic => new SyntheticWeatherProvider(
                baseTempCelsius:      cfg.BaseTempCelsius,
                tempAmplitude:        cfg.TempAmplitude,
                baseRelativeHumidity: cfg.BaseRelativeHumidity,
                humidityAmplitude:    cfg.HumidityAmplitude,
                chlorideConcentration: cfg.ChlorideConcentration,
                precipitation:        cfg.Precipitation,
                windSpeed:            cfg.WindSpeed),

            WeatherProviderType.Csv => new CsvWeatherProvider(
                new StreamReader(cfg.CsvPath!)),

            _ => throw new NotSupportedException(
                $"Weather provider type '{cfg.Type}' is not supported."),
        };
    }
}
