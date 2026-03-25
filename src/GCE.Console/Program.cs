using GCE.Atmosphere;
using GCE.Core;
using GCE.Electrochemistry;
using GCE.IO;
using GCE.Simulation;
using GCE.Configuration;

Console.WriteLine("=== GalvanicCorrosionEngine ===");
Console.WriteLine();

// ── Config-file mode ──────────────────────────────────────────────────────────
// Usage: dotnet run --project src/GCE.Console -- --config path/to/simulation.json
//        dotnet run --project src/GCE.Console -- --config path/to/simulation.yaml

string? configPath = null;
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] is "--config" or "-c")
    {
        configPath = args[i + 1];
        break;
    }
}

if (configPath is not null)
{
    Console.WriteLine($"Loading configuration from: {configPath}");
    Console.WriteLine();

    var reader    = new SimulationConfigReader();
    var validator = new SimulationConfigValidator();
    var mapper    = new SimulationConfigMapper();

    SimulationConfig config;
    try
    {
        config = reader.ReadFile(configPath);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error reading configuration file: {ex.Message}");
        return 1;
    }

    var errors = validator.Validate(config);
    if (errors.Count > 0)
    {
        Console.Error.WriteLine("Configuration validation failed:");
        foreach (var error in errors)
            Console.Error.WriteLine($"  • {error}");
        return 1;
    }

    SimulationParameters parameters = mapper.Map(config);

    Console.WriteLine($"Anode           : {parameters.Pair.Anode.Name}");
    Console.WriteLine($"Cathode         : {parameters.Pair.Cathode.Name}");
    Console.WriteLine($"Galvanic voltage: {parameters.Pair.GalvanicVoltage:F3} V");
    Console.WriteLine($"Duration        : {parameters.DurationSeconds} s  ({parameters.TimeSteps} steps)");
    if (parameters.WeatherProvider is not null)
        Console.WriteLine("Weather         : time-varying");
    Console.WriteLine();

    var result = new SimulationEngine().Run(parameters);

    Console.WriteLine($"Simulation complete : {result.TimePoints.Count} time steps");
    Console.WriteLine($"Average corrosion rate : {result.AverageCorrosionRate:F4} mm/year");
    Console.WriteLine();

    string csvPath = Path.Combine(AppContext.BaseDirectory, "corrosion_results_config.csv");
    new CsvExporter().ExportToFile(result, csvPath);
    Console.WriteLine($"Results exported to: {csvPath}");
    return 0;
}


// Retrieve materials from the registry
IMaterial zinc = MaterialRegistry.Zinc;
IMaterial steel = MaterialRegistry.MildSteel;

// ── Static-environment simulation ────────────────────────────────────────────

Console.WriteLine("-- Static Environment --");

var staticConditions = new AtmosphericConditions(
    TemperatureCelsius: 25.0,
    RelativeHumidity: 0.80,
    ChlorideConcentration: 0.5);

var pair = new GalvanicPair(anode: zinc, cathode: steel);
var staticParameters = new SimulationParameters(pair, staticConditions, DurationSeconds: 3600, TimeSteps: 360);

Console.WriteLine($"Galvanic pair  : {pair.Anode.Name} (anode) / {pair.Cathode.Name} (cathode)");
Console.WriteLine($"Galvanic voltage : {pair.GalvanicVoltage:F3} V");
Console.WriteLine($"Environment    : T={staticConditions.TemperatureCelsius}°C, RH={staticConditions.RelativeHumidity * 100}%, [Cl⁻]={staticConditions.ChlorideConcentration} mol/L");
Console.WriteLine();

var engine = new SimulationEngine();
var staticResult = engine.Run(staticParameters);

Console.WriteLine($"Simulation complete : {staticResult.TimePoints.Count} time steps over {staticParameters.DurationSeconds} s");
Console.WriteLine($"Average corrosion rate : {staticResult.AverageCorrosionRate:F4} mm/year");
Console.WriteLine();

string staticCsvPath = Path.Combine(AppContext.BaseDirectory, "corrosion_results_static.csv");
new CsvExporter().ExportToFile(staticResult, staticCsvPath);
Console.WriteLine($"Results exported to : {staticCsvPath}");
Console.WriteLine();

// ── Weather-driven simulation (synthetic diurnal cycle) ───────────────────────

Console.WriteLine("-- Weather-Driven Environment (Synthetic Diurnal Cycle) --");

var weatherProvider = new SyntheticWeatherProvider(
    baseTempCelsius: 18.0,
    tempAmplitude: 10.0,
    baseRelativeHumidity: 0.72,
    humidityAmplitude: 0.18,
    chlorideConcentration: 0.5);

var weatherParameters = new SimulationParameters(
    pair,
    staticConditions,         // fallback environment (unused when WeatherProvider is set)
    DurationSeconds: 86_400,  // simulate one full day
    TimeSteps: 1440,
    WeatherProvider: weatherProvider);

Console.WriteLine($"Galvanic pair  : {pair.Anode.Name} (anode) / {pair.Cathode.Name} (cathode)");
var peakObs = weatherProvider.GetObservation(50_400); // 14:00 solar time — daily peak
Console.WriteLine($"Weather        : synthetic diurnal — peak {peakObs.TemperatureCelsius:F1}°C, [Cl⁻]={peakObs.ChlorideConcentration} mol/L");
Console.WriteLine();

var weatherResult = engine.Run(weatherParameters);

Console.WriteLine($"Simulation complete : {weatherResult.TimePoints.Count} time steps over {weatherParameters.DurationSeconds} s");
Console.WriteLine($"Average corrosion rate : {weatherResult.AverageCorrosionRate:F4} mm/year");
Console.WriteLine($"Min corrosion rate     : {weatherResult.CorrosionRates.Min():F4} mm/year");
Console.WriteLine($"Max corrosion rate     : {weatherResult.CorrosionRates.Max():F4} mm/year");
Console.WriteLine();

string weatherCsvPath = Path.Combine(AppContext.BaseDirectory, "corrosion_results_weather.csv");
new CsvExporter().ExportToFile(weatherResult, weatherCsvPath);
Console.WriteLine($"Results exported to : {weatherCsvPath}");
return 0;
