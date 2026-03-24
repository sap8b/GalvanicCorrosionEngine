using GCE.Core;

namespace GCE.Configuration;

/// <summary>
/// Validates a <see cref="SimulationConfig"/> and returns a list of error messages.
/// </summary>
public sealed class SimulationConfigValidator
{
    /// <summary>
    /// Validates the given <paramref name="config"/> and returns all error messages found.
    /// An empty list indicates a valid configuration.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <returns>A read-only list of human-readable error messages (empty if valid).</returns>
    public IReadOnlyList<string> Validate(SimulationConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var errors = new List<string>();

        ValidateMaterial(config.Anode,   "anode",   errors);
        ValidateMaterial(config.Cathode, "cathode", errors);
        ValidateEnvironment(config.Environment, errors);
        ValidateDuration(config.DurationSeconds, errors);
        ValidateTimeSteps(config.TimeSteps, errors);

        if (config.Weather is not null && config.Weather.Type != WeatherProviderType.None)
            ValidateWeather(config.Weather, errors);

        return errors;
    }

    /// <summary>
    /// Validates the given <paramref name="config"/> and throws
    /// <see cref="ConfigurationValidationException"/> if any errors are found.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <exception cref="ConfigurationValidationException">Thrown when the configuration is invalid.</exception>
    public void ValidateOrThrow(SimulationConfig config)
    {
        var errors = Validate(config);
        if (errors.Count > 0)
            throw new ConfigurationValidationException(errors);
    }

    // ── Material ──────────────────────────────────────────────────────────────

    private static void ValidateMaterial(MaterialConfig? cfg, string role, List<string> errors)
    {
        if (cfg is null)
        {
            errors.Add($"The '{role}' material configuration is required.");
            return;
        }

        if (cfg.IsRegistryLookup)
        {
            // Name-only lookup: check the registry
            if (!MaterialRegistry.TryGet(cfg.Name!, out _))
                errors.Add($"The '{role}' material '{cfg.Name}' is not found in the registry. " +
                           $"Known materials: {string.Join(", ", MaterialRegistry.RegisteredNames)}.");
        }
        else
        {
            // Custom material: all electrochemical properties must be supplied
            if (string.IsNullOrWhiteSpace(cfg.Name))
                errors.Add($"The '{role}' custom material must have a non-empty name.");

            if (cfg.StandardPotential is null)
                errors.Add($"The '{role}' custom material must specify 'standardPotential'.");

            if (cfg.ExchangeCurrentDensity is null)
                errors.Add($"The '{role}' custom material must specify 'exchangeCurrentDensity'.");
            else if (cfg.ExchangeCurrentDensity.Value <= 0)
                errors.Add($"The '{role}' custom material 'exchangeCurrentDensity' must be positive.");

            if (cfg.MolarMass is null)
                errors.Add($"The '{role}' custom material must specify 'molarMass'.");
            else if (cfg.MolarMass.Value <= 0)
                errors.Add($"The '{role}' custom material 'molarMass' must be positive.");

            if (cfg.ElectronsTransferred is null)
                errors.Add($"The '{role}' custom material must specify 'electronsTransferred'.");
            else if (cfg.ElectronsTransferred.Value <= 0)
                errors.Add($"The '{role}' custom material 'electronsTransferred' must be a positive integer.");

            if (cfg.Density is null)
                errors.Add($"The '{role}' custom material must specify 'density'.");
            else if (cfg.Density.Value <= 0)
                errors.Add($"The '{role}' custom material 'density' must be positive.");
        }
    }

    // ── Environment ───────────────────────────────────────────────────────────

    private static void ValidateEnvironment(EnvironmentConfig? cfg, List<string> errors)
    {
        if (cfg is null)
        {
            errors.Add("The 'environment' section is required.");
            return;
        }

        if (cfg.RelativeHumidity < 0.0 || cfg.RelativeHumidity > 1.0)
            errors.Add($"'environment.relativeHumidity' must be between 0 and 1 (got {cfg.RelativeHumidity}).");

        if (cfg.ChlorideConcentration < 0.0)
            errors.Add($"'environment.chlorideConcentration' must be non-negative (got {cfg.ChlorideConcentration}).");
    }

    // ── Duration / time steps ─────────────────────────────────────────────────

    private static void ValidateDuration(double durationSeconds, List<string> errors)
    {
        if (durationSeconds <= 0)
            errors.Add($"'durationSeconds' must be positive (got {durationSeconds}).");
    }

    private static void ValidateTimeSteps(int timeSteps, List<string> errors)
    {
        if (timeSteps <= 0)
            errors.Add($"'timeSteps' must be a positive integer (got {timeSteps}).");
    }

    // ── Weather ───────────────────────────────────────────────────────────────

    private static void ValidateWeather(WeatherConfig cfg, List<string> errors)
    {
        switch (cfg.Type)
        {
            case WeatherProviderType.Synthetic:
                if (cfg.TempAmplitude < 0)
                    errors.Add($"'weather.tempAmplitude' must be non-negative (got {cfg.TempAmplitude}).");
                if (cfg.BaseRelativeHumidity < 0.0 || cfg.BaseRelativeHumidity > 1.0)
                    errors.Add($"'weather.baseRelativeHumidity' must be between 0 and 1 (got {cfg.BaseRelativeHumidity}).");
                if (cfg.HumidityAmplitude < 0)
                    errors.Add($"'weather.humidityAmplitude' must be non-negative (got {cfg.HumidityAmplitude}).");
                if (cfg.ChlorideConcentration < 0)
                    errors.Add($"'weather.chlorideConcentration' must be non-negative (got {cfg.ChlorideConcentration}).");
                if (cfg.Precipitation < 0)
                    errors.Add($"'weather.precipitation' must be non-negative (got {cfg.Precipitation}).");
                if (cfg.WindSpeed < 0)
                    errors.Add($"'weather.windSpeed' must be non-negative (got {cfg.WindSpeed}).");
                break;

            case WeatherProviderType.Csv:
                if (string.IsNullOrWhiteSpace(cfg.CsvPath))
                    errors.Add("'weather.csvPath' is required when weather type is 'csv'.");
                break;

            default:
                errors.Add($"Unknown weather provider type '{cfg.Type}'. " +
                           "Valid values are: 'none', 'synthetic', 'csv'.");
                break;
        }
    }
}
