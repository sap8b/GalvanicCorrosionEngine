// SideBySide - demonstrates the GalvanicCorrosionEngine side-by-side geometry.
//
// This sample models a zinc sheet placed alongside a copper sheet, sharing a
// marine electrolyte.  Zinc is the anode and corrodes preferentially while copper
// acts as the cathode.  The large cathode-to-anode area ratio accelerates the
// zinc corrosion rate.

using GCE.Atmosphere;
using GCE.Core;
using GCE.IO;
using GCE.Simulation;
using GCE.Simulation.Geometry;

// -- Geometry --
// A 20 mm wide zinc sheet alongside a 50 mm wide copper sheet, both 30 mm long.
var geometry = new SideBySideGeometry(
    anodeMaterial:   MaterialRegistry.Zinc,
    cathodeMaterial: MaterialRegistry.Copper,
    anodeWidth:      0.020,  // 20 mm
    cathodeWidth:    0.050,  // 50 mm
    length:          0.030); // 30 mm

Console.WriteLine("=== SideBySide Example ===");
Console.WriteLine();
Console.WriteLine($"Anode material  : {geometry.AnodeMaterial.Name}");
Console.WriteLine($"Cathode material: {geometry.CathodeMaterial.Name}");
Console.WriteLine($"Anode width     : {geometry.AnodeWidth * 1e3:F0} mm");
Console.WriteLine($"Cathode width   : {geometry.CathodeWidth * 1e3:F0} mm");
Console.WriteLine($"Length          : {geometry.Length * 1e3:F0} mm");
Console.WriteLine();

// -- Environment (marine atmosphere) --
var env = new AtmosphericConditions(
    TemperatureCelsius:    25.0,
    RelativeHumidity:      0.85,
    ChlorideConcentration: 0.6);

// Build the galvanic cell from the geometry and the electrolyte
var cell = geometry.Build(env);
Console.WriteLine($"Galvanic cell   : {cell.Anode.Material.Name} (anode) / {cell.Cathode.Material.Name} (cathode)");
Console.WriteLine($"Anode area      : {cell.Anode.Area * 1e4:F2} cm²");
Console.WriteLine($"Cathode area    : {cell.Cathode.Area * 1e4:F2} cm²");
Console.WriteLine($"Area ratio      : {cell.Cathode.Area / cell.Anode.Area:F1} (cathode/anode)");
Console.WriteLine();

// -- Simulation (1 hour, 360 steps) --
var pair = new GCE.Electrochemistry.GalvanicPair(
    anode:   geometry.AnodeMaterial,
    cathode: geometry.CathodeMaterial);

Console.WriteLine($"ΔE (OCV)        : {pair.GalvanicVoltage:F3} V");
Console.WriteLine();

var parameters = new SimulationParameters(pair, env, DurationSeconds: 3600, TimeSteps: 360);
var result = new SimulationEngine().Run(parameters);

Console.WriteLine($"Simulation complete : {result.TimePoints.Count} time steps over {parameters.DurationSeconds} s");
Console.WriteLine($"Average corrosion rate : {result.AverageCorrosionRate:F4} mm/year");
Console.WriteLine($"Min corrosion rate     : {result.CorrosionRates.Min():F4} mm/year");
Console.WriteLine($"Max corrosion rate     : {result.CorrosionRates.Max():F4} mm/year");
Console.WriteLine();

// -- Build spatial mesh and export VTK --
var mesh = geometry.BuildMesh(nodesX: 35, nodesY: 20);
Console.WriteLine($"Mesh nodes: {mesh.NodesX} × {mesh.NodesY}");
Console.WriteLine();

// Export results
string csvPath = Path.Combine(AppContext.BaseDirectory, "side_by_side_output.csv");
new CsvExporter().ExportToFile(result, csvPath);
Console.WriteLine($"CSV exported to : {csvPath}");

string vtkPath = Path.Combine(AppContext.BaseDirectory, "side_by_side_output.vtr");
new VtkResultWriter(mesh).WriteToFile(result, vtkPath);
Console.WriteLine($"VTK exported to : {vtkPath}");
