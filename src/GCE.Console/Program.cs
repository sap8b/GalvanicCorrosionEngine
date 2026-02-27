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

// Define environment (25 °C, 80 % RH, seawater-like chloride)
var conditions = new AtmosphericConditions(
    TemperatureCelsius: 25.0,
    RelativeHumidity: 0.80,
    ChlorideConcentration: 0.5);

// Build galvanic pair and simulation parameters
var pair = new GalvanicPair(anode: zinc, cathode: steel);
var parameters = new SimulationParameters(pair, conditions, DurationSeconds: 3600, TimeSteps: 360);

Console.WriteLine($"Galvanic pair : {pair.Anode.Name} (anode) / {pair.Cathode.Name} (cathode)");
Console.WriteLine($"Galvanic voltage : {pair.GalvanicVoltage:F3} V");
Console.WriteLine($"Environment  : T={conditions.TemperatureCelsius}°C, RH={conditions.RelativeHumidity * 100}%, [Cl⁻]={conditions.ChlorideConcentration} mol/L");
Console.WriteLine();

// Run simulation
var engine = new SimulationEngine();
var result = engine.Run(parameters);

Console.WriteLine($"Simulation complete: {result.TimePoints.Count} time steps over {parameters.DurationSeconds} s");
Console.WriteLine($"Average corrosion rate : {result.AverageCorrosionRate:F4} mm/year");
Console.WriteLine();

// Export results
string csvPath = Path.Combine(AppContext.BaseDirectory, "corrosion_results.csv");
new CsvExporter().ExportToFile(result, csvPath);
Console.WriteLine($"Results exported to : {csvPath}");
