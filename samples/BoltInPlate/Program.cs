// BoltInPlate - demonstrates the GalvanicCorrosionEngine bolt-in-plate geometry.
//
// This sample models a zinc bolt inserted through a mild-steel plate in a marine
// atmosphere.  Zinc is the anode (more active metal) and corrodes preferentially,
// sacrificially protecting the steel plate.

using GCE.Atmosphere;
using GCE.Core;
using GCE.IO;
using GCE.Simulation;
using GCE.Simulation.Geometry;

// -- Geometry --
// A 6 mm radius zinc bolt through a 100 mm × 100 mm mild-steel plate that is 5 mm thick.
var geometry = new BoltInPlateGeometry(
    boltMaterial:    MaterialRegistry.Zinc,
    plateMaterial:   MaterialRegistry.MildSteel,
    boltRadius:      0.006,   // 6 mm
    plateThickness:  0.005,   // 5 mm
    plateWidth:      0.100);  // 100 mm

Console.WriteLine("=== BoltInPlate Example ===");
Console.WriteLine();
Console.WriteLine($"Bolt material  : {geometry.BoltMaterial.Name}");
Console.WriteLine($"Plate material : {geometry.PlateMaterial.Name}");
Console.WriteLine($"Anode          : {geometry.AnodeMaterial.Name}");
Console.WriteLine($"Cathode        : {geometry.CathodeMaterial.Name}");
Console.WriteLine($"Bolt radius    : {geometry.BoltRadius * 1e3:F1} mm");
Console.WriteLine($"Plate width    : {geometry.PlateWidth * 1e3:F0} mm");
Console.WriteLine($"Plate thickness: {geometry.PlateThickness * 1e3:F1} mm");
Console.WriteLine();

// -- Environment (marine atmosphere) --
var env = new AtmosphericConditions(
    TemperatureCelsius:      25.0,
    RelativeHumidity:        0.85,
    ChlorideConcentration:   0.6);

// Build the galvanic cell from the geometry and the electrolyte
var cell = geometry.Build(env);
Console.WriteLine($"Galvanic cell  : {cell.Anode.Material.Name} (anode) / {cell.Cathode.Material.Name} (cathode)");
Console.WriteLine($"Anode area     : {cell.Anode.Area * 1e4:F2} cm²");
Console.WriteLine($"Cathode area   : {cell.Cathode.Area * 1e4:F2} cm²");
Console.WriteLine($"Area ratio     : {cell.Cathode.Area / cell.Anode.Area:F1} (cathode/anode)");
Console.WriteLine();

// -- Simulation (1 hour, 360 steps) --
var pair = new GCE.Electrochemistry.GalvanicPair(
    anode:   geometry.AnodeMaterial,
    cathode: geometry.CathodeMaterial);

Console.WriteLine($"ΔE (OCV)       : {pair.GalvanicVoltage:F3} V");
Console.WriteLine();

var parameters = new SimulationParameters(pair, env, DurationSeconds: 3600, TimeSteps: 360);
var result = new SimulationEngine().Run(parameters);

Console.WriteLine($"Simulation complete : {result.TimePoints.Count} time steps over {parameters.DurationSeconds} s");
Console.WriteLine($"Average corrosion rate : {result.AverageCorrosionRate:F4} mm/year");
Console.WriteLine($"Min corrosion rate     : {result.CorrosionRates.Min():F4} mm/year");
Console.WriteLine($"Max corrosion rate     : {result.CorrosionRates.Max():F4} mm/year");
Console.WriteLine();

// -- Build spatial mesh and export VTK --
var mesh = geometry.BuildMesh(nodesX: 40, nodesY: 40);
Console.WriteLine($"Mesh nodes: {mesh.NodesX} × {mesh.NodesY}");
Console.WriteLine();

// Export results
string csvPath = Path.Combine(AppContext.BaseDirectory, "bolt_in_plate_output.csv");
new CsvExporter().ExportToFile(result, csvPath);
Console.WriteLine($"CSV exported to : {csvPath}");

string vtkPath = Path.Combine(AppContext.BaseDirectory, "bolt_in_plate_output.vtr");
new VtkResultWriter(mesh).WriteToFile(result, vtkPath);
Console.WriteLine($"VTK exported to : {vtkPath}");
