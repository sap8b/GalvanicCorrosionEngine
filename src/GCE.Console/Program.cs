using GCE.Atmosphere;
using GCE.Core;
using GCE.Electrochemistry;
using GCE.IO;
using GCE.Simulation;

Console.WriteLine("=== GalvanicCorrosionEngine ===");
Console.WriteLine();

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
