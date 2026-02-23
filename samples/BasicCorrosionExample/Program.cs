// BasicCorrosionExample - demonstrates the GalvanicCorrosionEngine API.
//
// This sample models zinc sacrificially protecting mild steel in a humid, chloride-rich atmosphere.

using GCE.Atmosphere;
using GCE.Core;
using GCE.Electrochemistry;
using GCE.IO;
using GCE.Simulation;

// -- Materials --
IMaterial zinc = new Material("Zinc", StandardPotential: -0.76, ExchangeCurrentDensity: 1e-3);
IMaterial steel = new Material("Mild Steel", StandardPotential: -0.44, ExchangeCurrentDensity: 1e-4);

// -- Environment (marine atmosphere) --
var env = new AtmosphericConditions(
    TemperatureCelsius: 25.0,
    RelativeHumidity: 0.85,
    ChlorideConcentration: 0.6);

// -- Galvanic pair --
var pair = new GalvanicPair(anode: zinc, cathode: steel);

Console.WriteLine($"Pair       : {pair.Anode.Name} / {pair.Cathode.Name}");
Console.WriteLine($"ΔE (OCV)   : {pair.GalvanicVoltage:F3} V");

// -- Simulation (1 hour, 360 steps) --
var parameters = new SimulationParameters(pair, env, DurationSeconds: 3600, TimeSteps: 360);
var result = new SimulationEngine().Run(parameters);

Console.WriteLine($"Avg corrosion rate: {result.AverageCorrosionRate:F4} mm/year");

// -- Export --
string csv = Path.Combine(AppContext.BaseDirectory, "example_output.csv");
new CsvExporter().ExportToFile(result, csv);
Console.WriteLine($"CSV exported to: {csv}");
