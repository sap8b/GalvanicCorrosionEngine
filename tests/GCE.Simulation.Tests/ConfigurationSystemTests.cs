using GCE.Atmosphere;
using GCE.Configuration;
using GCE.Core;
using GCE.Simulation;

namespace GCE.Simulation.Tests;

// ── SimulationConfig defaults ─────────────────────────────────────────────────

public class SimulationConfigTests
{
    [Fact]
    public void SimulationConfig_Defaults_AreReasonable()
    {
        var config = new SimulationConfig();

        Assert.Equal("Zinc",       config.Anode.Name);
        Assert.Equal("Mild Steel", config.Cathode.Name);
        Assert.Equal(25.0,  config.Environment.TemperatureCelsius);
        Assert.Equal(0.80,  config.Environment.RelativeHumidity);
        Assert.Equal(0.5,   config.Environment.ChlorideConcentration);
        Assert.Equal(3600.0, config.DurationSeconds);
        Assert.Equal(1000,  config.TimeSteps);
        Assert.Null(config.Weather);
    }

    [Fact]
    public void MaterialConfig_IsRegistryLookup_TrueWhenOnlyNameSet()
    {
        var cfg = new MaterialConfig { Name = "Zinc" };
        Assert.True(cfg.IsRegistryLookup);
    }

    [Fact]
    public void MaterialConfig_IsRegistryLookup_FalseWhenCustomFieldsSet()
    {
        var cfg = new MaterialConfig
        {
            Name = "Custom",
            StandardPotential = -0.50,
            ExchangeCurrentDensity = 1e-4,
            MolarMass = 0.058,
            ElectronsTransferred = 2,
            Density = 7800.0,
        };
        Assert.False(cfg.IsRegistryLookup);
    }

    [Fact]
    public void MaterialConfig_IsRegistryLookup_FalseWhenNameIsNull()
    {
        var cfg = new MaterialConfig();
        Assert.False(cfg.IsRegistryLookup);
    }
}

// ── SimulationConfigValidator ─────────────────────────────────────────────────

public class SimulationConfigValidatorTests
{
    private static readonly SimulationConfigValidator Validator = new();

    [Fact]
    public void Validate_DefaultConfig_ReturnsNoErrors()
    {
        var errors = Validator.Validate(new SimulationConfig());
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_KnownMaterials_ReturnsNoErrors()
    {
        var config = new SimulationConfig
        {
            Anode   = new MaterialConfig { Name = "Aluminium" },
            Cathode = new MaterialConfig { Name = "Copper" },
        };
        Assert.Empty(Validator.Validate(config));
    }

    [Fact]
    public void Validate_UnknownAnodeName_ReturnsError()
    {
        var config = new SimulationConfig
        {
            Anode = new MaterialConfig { Name = "Unobtainium" },
        };
        var errors = Validator.Validate(config);
        Assert.Contains(errors, e => e.Contains("anode") && e.Contains("Unobtainium"));
    }

    [Fact]
    public void Validate_NullAnode_ReturnsError()
    {
        var config = new SimulationConfig { Anode = null! };
        var errors = Validator.Validate(config);
        Assert.Contains(errors, e => e.Contains("anode"));
    }

    [Fact]
    public void Validate_CustomMaterial_AllFieldsSupplied_ReturnsNoErrors()
    {
        var config = new SimulationConfig
        {
            Anode = new MaterialConfig
            {
                Name = "Custom Alloy",
                StandardPotential = -0.55,
                ExchangeCurrentDensity = 1e-4,
                MolarMass = 0.058,
                ElectronsTransferred = 2,
                Density = 7800.0,
            },
        };
        Assert.Empty(Validator.Validate(config));
    }

    [Fact]
    public void Validate_CustomMaterial_MissingDensity_ReturnsError()
    {
        var config = new SimulationConfig
        {
            Anode = new MaterialConfig
            {
                Name = "Custom",
                StandardPotential = -0.55,
                ExchangeCurrentDensity = 1e-4,
                MolarMass = 0.058,
                ElectronsTransferred = 2,
                // Density missing
            },
        };
        var errors = Validator.Validate(config);
        Assert.Contains(errors, e => e.Contains("density"));
    }

    [Fact]
    public void Validate_NegativeExchangeCurrentDensity_ReturnsError()
    {
        var config = new SimulationConfig
        {
            Anode = new MaterialConfig
            {
                Name = "Custom",
                StandardPotential = -0.55,
                ExchangeCurrentDensity = -1e-4,
                MolarMass = 0.058,
                ElectronsTransferred = 2,
                Density = 7800.0,
            },
        };
        var errors = Validator.Validate(config);
        Assert.Contains(errors, e => e.Contains("exchangeCurrentDensity"));
    }

    [Fact]
    public void Validate_RelativeHumidityAboveOne_ReturnsError()
    {
        var config = new SimulationConfig
        {
            Environment = new EnvironmentConfig { RelativeHumidity = 1.5 },
        };
        var errors = Validator.Validate(config);
        Assert.Contains(errors, e => e.Contains("relativeHumidity"));
    }

    [Fact]
    public void Validate_NegativeChloride_ReturnsError()
    {
        var config = new SimulationConfig
        {
            Environment = new EnvironmentConfig { ChlorideConcentration = -0.1 },
        };
        var errors = Validator.Validate(config);
        Assert.Contains(errors, e => e.Contains("chlorideConcentration"));
    }

    [Fact]
    public void Validate_ZeroDuration_ReturnsError()
    {
        var config = new SimulationConfig { DurationSeconds = 0 };
        var errors = Validator.Validate(config);
        Assert.Contains(errors, e => e.Contains("durationSeconds"));
    }

    [Fact]
    public void Validate_ZeroTimeSteps_ReturnsError()
    {
        var config = new SimulationConfig { TimeSteps = 0 };
        var errors = Validator.Validate(config);
        Assert.Contains(errors, e => e.Contains("timeSteps"));
    }

    [Fact]
    public void Validate_SyntheticWeather_ValidConfig_ReturnsNoErrors()
    {
        var config = new SimulationConfig
        {
            Weather = new WeatherConfig { Type = WeatherProviderType.Synthetic },
        };
        Assert.Empty(Validator.Validate(config));
    }

    [Fact]
    public void Validate_SyntheticWeather_NegativeTempAmplitude_ReturnsError()
    {
        var config = new SimulationConfig
        {
            Weather = new WeatherConfig
            {
                Type = WeatherProviderType.Synthetic,
                TempAmplitude = -5.0,
            },
        };
        var errors = Validator.Validate(config);
        Assert.Contains(errors, e => e.Contains("tempAmplitude"));
    }

    [Fact]
    public void Validate_CsvWeather_MissingPath_ReturnsError()
    {
        var config = new SimulationConfig
        {
            Weather = new WeatherConfig { Type = WeatherProviderType.Csv },
        };
        var errors = Validator.Validate(config);
        Assert.Contains(errors, e => e.Contains("csvPath"));
    }

    [Fact]
    public void ValidateOrThrow_InvalidConfig_ThrowsConfigurationValidationException()
    {
        var config = new SimulationConfig { DurationSeconds = -1 };
        Assert.Throws<ConfigurationValidationException>(() => Validator.ValidateOrThrow(config));
    }

    [Fact]
    public void ValidateOrThrow_ValidConfig_DoesNotThrow()
    {
        var config = new SimulationConfig();
        Validator.ValidateOrThrow(config); // should not throw
    }
}

// ── ConfigurationValidationException ─────────────────────────────────────────

public class ConfigurationValidationExceptionTests
{
    [Fact]
    public void Exception_StoresErrors()
    {
        var errors = new[] { "Error A", "Error B" };
        var ex = new ConfigurationValidationException(errors);

        Assert.Equal(2, ex.Errors.Count);
        Assert.Contains("Error A", ex.Errors);
        Assert.Contains("Error B", ex.Errors);
    }

    [Fact]
    public void Exception_MessageContainsAllErrors()
    {
        var errors = new[] { "first problem", "second problem" };
        var ex = new ConfigurationValidationException(errors);

        Assert.Contains("first problem",  ex.Message);
        Assert.Contains("second problem", ex.Message);
    }

    [Fact]
    public void Exception_IsAssignableToException()
    {
        Exception ex = new ConfigurationValidationException(new[] { "x" });
        Assert.NotNull(ex.Message);
    }
}

// ── SimulationConfigReader (JSON) ─────────────────────────────────────────────

public class SimulationConfigReaderJsonTests
{
    private static readonly SimulationConfigReader Reader = new();

    private const string MinimalJson = """
        {
            "anode": { "name": "Zinc" },
            "cathode": { "name": "Mild Steel" }
        }
        """;

    [Fact]
    public void ReadJson_MinimalConfig_ReturnsConfig()
    {
        var config = Reader.ReadJson(MinimalJson);
        Assert.Equal("Zinc",       config.Anode.Name);
        Assert.Equal("Mild Steel", config.Cathode.Name);
    }

    [Fact]
    public void ReadJson_UnsetPropertiesUseDefaults()
    {
        var config = Reader.ReadJson(MinimalJson);
        Assert.Equal(3600.0, config.DurationSeconds);
        Assert.Equal(1000,   config.TimeSteps);
        Assert.Null(config.Weather);
    }

    [Fact]
    public void ReadJson_FullStaticConfig_ParsesAllFields()
    {
        const string json = """
            {
                "anode": { "name": "Aluminium" },
                "cathode": { "name": "Copper" },
                "environment": {
                    "temperatureCelsius": 20.0,
                    "relativeHumidity": 0.65,
                    "chlorideConcentration": 0.2
                },
                "durationSeconds": 7200,
                "timeSteps": 720
            }
            """;

        var config = Reader.ReadJson(json);
        Assert.Equal("Aluminium", config.Anode.Name);
        Assert.Equal("Copper",    config.Cathode.Name);
        Assert.Equal(20.0,  config.Environment.TemperatureCelsius,  precision: 9);
        Assert.Equal(0.65,  config.Environment.RelativeHumidity,    precision: 9);
        Assert.Equal(0.2,   config.Environment.ChlorideConcentration, precision: 9);
        Assert.Equal(7200.0, config.DurationSeconds, precision: 9);
        Assert.Equal(720,   config.TimeSteps);
    }

    [Fact]
    public void ReadJson_SyntheticWeather_ParsesWeatherSection()
    {
        const string json = """
            {
                "weather": {
                    "type": "synthetic",
                    "baseTempCelsius": 18.0,
                    "tempAmplitude": 10.0,
                    "baseRelativeHumidity": 0.72,
                    "humidityAmplitude": 0.18,
                    "chlorideConcentration": 0.5
                }
            }
            """;

        var config = Reader.ReadJson(json);
        Assert.NotNull(config.Weather);
        Assert.Equal(WeatherProviderType.Synthetic, config.Weather!.Type);
        Assert.Equal(18.0, config.Weather.BaseTempCelsius, precision: 9);
        Assert.Equal(10.0, config.Weather.TempAmplitude,   precision: 9);
    }

    [Fact]
    public void ReadJson_CsvWeather_ParsesCsvPath()
    {
        const string json = """
            {
                "weather": {
                    "type": "csv",
                    "csvPath": "data/weather.csv"
                }
            }
            """;

        var config = Reader.ReadJson(json);
        Assert.Equal(WeatherProviderType.Csv, config.Weather!.Type);
        Assert.Equal("data/weather.csv", config.Weather.CsvPath);
    }

    [Fact]
    public void ReadJson_CaseInsensitivePropertyNames()
    {
        const string json = """
            {
                "DurationSeconds": 5400,
                "TimeSteps": 540
            }
            """;

        var config = Reader.ReadJson(json);
        Assert.Equal(5400, config.DurationSeconds, precision: 9);
        Assert.Equal(540,  config.TimeSteps);
    }

    [Fact]
    public void ReadJson_CommentsAndTrailingCommas_Tolerated()
    {
        const string json = """
            {
                // This is a simulation config
                "durationSeconds": 1800, /* half hour */
                "timeSteps": 180,
            }
            """;

        var config = Reader.ReadJson(json);
        Assert.Equal(1800, config.DurationSeconds, precision: 9);
    }

    [Fact]
    public void ReadJson_CustomMaterial_ParsesAllFields()
    {
        const string json = """
            {
                "anode": {
                    "name": "Custom Alloy",
                    "standardPotential": -0.55,
                    "exchangeCurrentDensity": 0.0001,
                    "molarMass": 0.058,
                    "electronsTransferred": 2,
                    "density": 7800.0
                }
            }
            """;

        var config = Reader.ReadJson(json);
        Assert.False(config.Anode.IsRegistryLookup);
        Assert.Equal(-0.55, config.Anode.StandardPotential!.Value, precision: 9);
        Assert.Equal(0.058, config.Anode.MolarMass!.Value, precision: 9);
    }

    [Fact]
    public void ReadJson_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Reader.ReadJson(null!));
    }

    [Fact]
    public void ReadJsonFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            Reader.ReadJsonFile("/nonexistent/path/config.json"));
    }

    [Fact]
    public void ReadFile_UnsupportedExtension_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            Reader.ReadFile("/path/config.toml"));
    }
}

// ── SimulationConfigReader (YAML) ─────────────────────────────────────────────

public class SimulationConfigReaderYamlTests
{
    private static readonly SimulationConfigReader Reader = new();

    private const string MinimalYaml = """
        anode:
          name: "Zinc"
        cathode:
          name: "Mild Steel"
        """;

    [Fact]
    public void ReadYaml_MinimalConfig_ReturnsConfig()
    {
        var config = Reader.ReadYaml(MinimalYaml);
        Assert.Equal("Zinc",       config.Anode.Name);
        Assert.Equal("Mild Steel", config.Cathode.Name);
    }

    [Fact]
    public void ReadYaml_SyntheticWeather_ParsesWeatherSection()
    {
        const string yaml = """
            weather:
              type: "synthetic"
              baseTempCelsius: 18.0
              tempAmplitude: 10.0
              baseRelativeHumidity: 0.72
              humidityAmplitude: 0.18
              chlorideConcentration: 0.5
            """;

        var config = Reader.ReadYaml(yaml);
        Assert.NotNull(config.Weather);
        Assert.Equal(WeatherProviderType.Synthetic, config.Weather!.Type);
        Assert.Equal(18.0, config.Weather.BaseTempCelsius, precision: 9);
    }

    [Fact]
    public void ReadYaml_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Reader.ReadYaml(null!));
    }

    [Fact]
    public void ReadYamlFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() =>
            Reader.ReadYamlFile("/nonexistent/path/config.yaml"));
    }
}

// ── SimulationConfigMapper ────────────────────────────────────────────────────

public class SimulationConfigMapperTests
{
    private static readonly SimulationConfigMapper Mapper = new();

    [Fact]
    public void Map_DefaultConfig_ReturnsValidSimulationParameters()
    {
        var parameters = Mapper.Map(new SimulationConfig());

        Assert.NotNull(parameters);
        Assert.Equal("Zinc",       parameters.Pair.Anode.Name);
        Assert.Equal("Mild Steel", parameters.Pair.Cathode.Name);
        Assert.Equal(3600.0, parameters.DurationSeconds, precision: 9);
        Assert.Equal(1000,   parameters.TimeSteps);
        Assert.Null(parameters.WeatherProvider);
    }

    [Fact]
    public void Map_RegistryMaterials_ResolvesCorrectly()
    {
        var config = new SimulationConfig
        {
            Anode   = new MaterialConfig { Name = "Aluminium" },
            Cathode = new MaterialConfig { Name = "Copper" },
        };

        var parameters = Mapper.Map(config);
        Assert.Equal("Aluminium", parameters.Pair.Anode.Name);
        Assert.Equal("Copper",    parameters.Pair.Cathode.Name);
    }

    [Fact]
    public void Map_CustomMaterial_CreatesCorrectMaterial()
    {
        var config = new SimulationConfig
        {
            Anode = new MaterialConfig
            {
                Name = "Test Alloy",
                StandardPotential = -0.60,
                ExchangeCurrentDensity = 2e-4,
                MolarMass = 0.055,
                ElectronsTransferred = 2,
                Density = 7500.0,
            },
        };

        var parameters = Mapper.Map(config);
        Assert.Equal("Test Alloy", parameters.Pair.Anode.Name);
        Assert.Equal(-0.60, parameters.Pair.Anode.StandardPotential, precision: 9);
        Assert.Equal(7500.0, parameters.Pair.Anode.Density, precision: 9);
    }

    [Fact]
    public void Map_Environment_MapsCorrectly()
    {
        var config = new SimulationConfig
        {
            Environment = new EnvironmentConfig
            {
                TemperatureCelsius = 30.0,
                RelativeHumidity = 0.90,
                ChlorideConcentration = 0.8,
            },
        };

        var parameters = Mapper.Map(config);
        var env = (AtmosphericConditions)parameters.Environment;
        Assert.Equal(30.0, env.TemperatureCelsius,       precision: 9);
        Assert.Equal(0.90, env.RelativeHumidity,          precision: 9);
        Assert.Equal(0.8,  env.ChlorideConcentration,     precision: 9);
    }

    [Fact]
    public void Map_NoWeather_WeatherProviderIsNull()
    {
        var parameters = Mapper.Map(new SimulationConfig());
        Assert.Null(parameters.WeatherProvider);
    }

    [Fact]
    public void Map_WeatherTypeNone_WeatherProviderIsNull()
    {
        var config = new SimulationConfig
        {
            Weather = new WeatherConfig { Type = WeatherProviderType.None },
        };
        var parameters = Mapper.Map(config);
        Assert.Null(parameters.WeatherProvider);
    }

    [Fact]
    public void Map_SyntheticWeather_CreatesWeatherProvider()
    {
        var config = new SimulationConfig
        {
            Weather = new WeatherConfig
            {
                Type = WeatherProviderType.Synthetic,
                BaseTempCelsius = 20.0,
            },
        };

        var parameters = Mapper.Map(config);
        Assert.NotNull(parameters.WeatherProvider);
    }

    [Fact]
    public void Map_NullConfig_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Mapper.Map(null!));
    }
}

// ── End-to-end: JSON → validate → map → simulate ─────────────────────────────

public class ConfigurationSystemIntegrationTests
{
    [Fact]
    public void EndToEnd_StaticSimulation_ProducesResults()
    {
        const string json = """
            {
                "anode": { "name": "Zinc" },
                "cathode": { "name": "Mild Steel" },
                "environment": {
                    "temperatureCelsius": 25.0,
                    "relativeHumidity": 0.80,
                    "chlorideConcentration": 0.5
                },
                "durationSeconds": 360,
                "timeSteps": 36
            }
            """;

        var reader    = new SimulationConfigReader();
        var validator = new SimulationConfigValidator();
        var mapper    = new SimulationConfigMapper();

        var config = reader.ReadJson(json);
        validator.ValidateOrThrow(config);

        var parameters = mapper.Map(config);
        var result = new SimulationEngine().Run(parameters);

        Assert.Equal(37, result.TimePoints.Count); // timeSteps + 1 initial point
        Assert.True(result.AverageCorrosionRate > 0);
    }

    [Fact]
    public void EndToEnd_WeatherDrivenSimulation_ProducesResults()
    {
        const string json = """
            {
                "durationSeconds": 3600,
                "timeSteps": 36,
                "weather": {
                    "type": "synthetic",
                    "baseTempCelsius": 15.0,
                    "tempAmplitude": 8.0,
                    "baseRelativeHumidity": 0.70,
                    "humidityAmplitude": 0.15,
                    "chlorideConcentration": 0.05
                }
            }
            """;

        var reader    = new SimulationConfigReader();
        var validator = new SimulationConfigValidator();
        var mapper    = new SimulationConfigMapper();

        var config = reader.ReadJson(json);
        validator.ValidateOrThrow(config);

        var parameters = mapper.Map(config);
        Assert.NotNull(parameters.WeatherProvider);

        var result = new SimulationEngine().Run(parameters);
        Assert.True(result.AverageCorrosionRate > 0);
    }

    [Fact]
    public void EndToEnd_YamlConfig_ProducesResults()
    {
        const string yaml = """
            anode:
              name: "Zinc"
            cathode:
              name: "Mild Steel"
            durationSeconds: 360
            timeSteps: 36
            """;

        var reader    = new SimulationConfigReader();
        var validator = new SimulationConfigValidator();
        var mapper    = new SimulationConfigMapper();

        var config = reader.ReadYaml(yaml);
        validator.ValidateOrThrow(config);

        var result = new SimulationEngine().Run(mapper.Map(config));
        Assert.True(result.AverageCorrosionRate > 0);
    }
}
