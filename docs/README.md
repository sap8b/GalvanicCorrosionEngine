# Getting Started

This guide walks you through installing, building, and running GalvanicCorrosionEngine,
then explains the core concepts through progressively more detailed examples.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- A terminal or command prompt

## Building the Project

Clone the repository and build the full solution:

```bash
git clone https://github.com/sap8b/GalvanicCorrosionEngine.git
cd GalvanicCorrosionEngine
dotnet build
```

Run the test suite to verify everything is working:

```bash
dotnet test
```

## Running the Samples

Four ready-to-run sample programs are provided under `samples/`.

### BasicCorrosionExample

Demonstrates the core API: select materials, define an environment, run a simulation,
and export results to CSV.

```bash
dotnet run --project samples/BasicCorrosionExample
```

**What it does:**
- Models zinc sacrificially protecting mild steel in a marine atmosphere
- Runs a 1-hour simulation with 360 time steps
- Exports time-series results to `example_output.csv`

### BoltInPlate

Models a zinc bolt inserted through a mild-steel plate.
Demonstrates `BoltInPlateGeometry` and VTK spatial mesh export.

```bash
dotnet run --project samples/BoltInPlate
```

**What it does:**
- Constructs a bolt-in-plate geometry (6 mm radius zinc bolt, 100 mm steel plate)
- Computes electrode areas from the geometry and the large cathode/anode area ratio
- Runs a 1-hour simulation
- Exports `bolt_in_plate_output.csv` and a `bolt_in_plate_output.vtr` spatial mesh file

### SideBySide

Models a zinc sheet placed alongside a copper sheet sharing a common electrolyte.
Demonstrates `SideBySideGeometry` and VTK spatial mesh export.

```bash
dotnet run --project samples/SideBySide
```

**What it does:**
- Constructs a side-by-side geometry (20 mm zinc sheet, 50 mm copper sheet)
- Runs a 1-hour simulation
- Exports `side_by_side_output.csv` and a `side_by_side_output.vtr` spatial mesh file

### Using a Configuration File

The `GCE.Console` app accepts a JSON or YAML configuration file via `--config`:

```bash
dotnet run --project src/GCE.Console -- --config samples/config/static-simulation.json
dotnet run --project src/GCE.Console -- --config samples/config/weather-simulation.yaml
```

See `samples/config/` for example configuration files and `schemas/simulation-config.schema.json`
for the full JSON schema.

## Quick Code Example

```csharp
using GCE.Atmosphere;
using GCE.Core;
using GCE.Electrochemistry;
using GCE.IO;
using GCE.Simulation;

// 1. Select materials from the built-in registry
IMaterial zinc  = MaterialRegistry.Zinc;
IMaterial steel = MaterialRegistry.MildSteel;

// 2. Define the environment
var env = new AtmosphericConditions(
    TemperatureCelsius:    25.0,
    RelativeHumidity:      0.80,
    ChlorideConcentration: 0.5);

// 3. Build a galvanic pair (lower standard potential = anode)
var pair = new GalvanicPair(anode: zinc, cathode: steel);

// 4. Configure and run the simulation
var parameters = new SimulationParameters(pair, env, DurationSeconds: 3600, TimeSteps: 360);
var result = new SimulationEngine().Run(parameters);

Console.WriteLine($"Average corrosion rate: {result.AverageCorrosionRate:F4} mm/year");

// 5. Export results
new CsvExporter().ExportToFile(result, "output.csv");
```

## Using a Geometry

Geometry builders (`BoltInPlateGeometry`, `SideBySideGeometry`) compute electrode areas
from physical dimensions and produce a spatial mesh for VTK export:

```csharp
using GCE.Simulation.Geometry;
using GCE.IO;

var geometry = new SideBySideGeometry(
    anodeMaterial:   MaterialRegistry.Zinc,
    cathodeMaterial: MaterialRegistry.Copper,
    anodeWidth:      0.020,
    cathodeWidth:    0.050,
    length:          0.030);

// Build the galvanic cell with area weighting from the geometry
var cell = geometry.Build(env);

// Build a mesh for spatial VTK export (35 × 20 nodes)
var mesh = geometry.BuildMesh(nodesX: 35, nodesY: 20);
new VtkResultWriter(mesh).WriteToFile(result, "output.vtr");
```

Open the resulting `.vtr` file in [ParaView](https://www.paraview.org/) or
[VisIt](https://visit-dav.github.io/visit-website/) to visualise the spatial mesh
alongside the corrosion time series.

## Time-Varying Weather Simulations

Pass a weather provider to drive the simulation with a time-varying atmosphere:

```csharp
using GCE.Atmosphere;

var weatherProvider = new SyntheticWeatherProvider(
    baseTempCelsius:      18.0,
    tempAmplitude:        10.0,
    baseRelativeHumidity: 0.72,
    humidityAmplitude:    0.18,
    chlorideConcentration: 0.5);

var parameters = new SimulationParameters(
    pair,
    env,                      // fallback (unused when WeatherProvider is set)
    DurationSeconds: 86_400,  // 24-hour simulation
    TimeSteps: 1440,
    WeatherProvider: weatherProvider);

var result = new SimulationEngine().Run(parameters);
```

## Available Materials

`MaterialRegistry` ships with these pre-configured metals:

| Property | Name | Standard Potential (V vs. SHE) |
|----------|------|-------------------------------|
| `Zinc` | Zinc | −0.76 |
| `MildSteel` | Mild Steel | −0.44 |
| `Aluminium` | Aluminium | −1.66 |
| `Copper` | Copper | +0.34 |
| `Nickel` | Nickel | −0.25 |
| `Magnesium` | Magnesium | −2.37 |

Custom materials can be added via `MaterialRegistry.Register(material)`.

## Next Steps

- Read [ARCHITECTURE.md](ARCHITECTURE.md) for an overview of the library design
- Read [CONTRIBUTING.md](CONTRIBUTING.md) to learn how to contribute
- Explore `src/` for detailed XML doc comments on every public API
