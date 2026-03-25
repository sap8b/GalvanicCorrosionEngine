# GalvanicCorrosionEngine

A modular .NET library for simulating galvanic corrosion between dissimilar metals under atmospheric and electrolytic conditions.

## Projects

| Project | Description |
|---------|-------------|
| `GCE.Core` | Core interfaces (`IMaterial`, `IElectrode`, `IElectrolyte`, `IGalvanicCell`, `IEnvironment`, `ICorrosionModel`) and domain types |
| `GCE.Numerics` | Numerical solvers (Runge–Kutta, Brent root-finder) |
| `GCE.Electrochemistry` | Galvanic pair model and Butler–Volmer kinetics |
| `GCE.Atmosphere` | Atmospheric condition models (`AtmosphericConditions`) |
| `GCE.Simulation` | High-level simulation engine and result types |
| `GCE.IO` | Result export utilities (CSV, JSON, VTK) |
| `GCE.Console` | Command-line entry point demonstrating the full pipeline |

## Quick Start

```bash
# Build the solution
dotnet build

# Run the console demo
dotnet run --project src/GCE.Console

# Run the basic corrosion example
dotnet run --project samples/BasicCorrosionExample
```

## Solution Structure

```
GalvanicCorrosionEngine/
├── src/
│   ├── GCE.Core/
│   ├── GCE.Numerics/
│   ├── GCE.Electrochemistry/
│   ├── GCE.Atmosphere/
│   ├── GCE.Simulation/
│   ├── GCE.IO/
│   └── GCE.Console/
├── samples/
│   └── BasicCorrosionExample/
├── ARCHITECTURE.md
└── GalvanicCorrosionEngine.sln
```

## Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) for a detailed description of the design principles and module dependencies.

## Exporting Results

`GCE.IO` exposes three result writers, all implementing `IResultWriter`:

| Class | Output format | File extension |
|---|---|---|
| `CsvResultWriter` | Comma-separated values | `.csv` |
| `JsonResultWriter` | JSON object with parallel arrays | `.json` |
| `VtkResultWriter` | VTK XML RectilinearGrid | `.vtr` |

### Using VtkResultWriter

`VtkResultWriter` produces a VTK XML RectilinearGrid file (`.vtr`) that can be opened directly in [ParaView](https://www.paraview.org/) or [VisIt](https://visit-dav.github.io/visit-website/).

**Time-series only** (no spatial mesh):

```csharp
using GCE.IO;

SimulationResult result = /* … run your simulation … */;

var writer = new VtkResultWriter();
writer.WriteToFile(result, "output.vtr");
```

The file will contain three VTK `FieldData` arrays:

| Array name | Unit |
|---|---|
| `Time_s` | seconds |
| `MixedPotential_V` | volts vs. SHE |
| `CorrosionRate_mmPerYear` | mm/year |

**With a geometry mesh** (spatial point data + time-series field data):

```csharp
using GCE.Core;
using GCE.IO;
using GCE.Simulation.Geometry;

// Build or obtain a geometry mesh.
var geometry = new SideBySideGeometry(
    anodeMaterial:   MaterialRegistry.Zinc,
    cathodeMaterial: MaterialRegistry.Copper,
    anodeWidth:      0.02, cathodeWidth: 0.05, height: 0.03);

GeometryMesh mesh = geometry.BuildMesh(nodesX: 40, nodesY: 20);

SimulationResult result = /* … */;

var writer = new VtkResultWriter(mesh);
writer.WriteToFile(result, "output.vtr");
```

When a mesh is provided the file additionally contains a `RegionId` point-data array whose values identify each grid node:

| Value | Meaning |
|---|---|
| `0` | Anode region |
| `1` | Cathode region |
| `-1` | Electrolyte / gap |

You can also write to any `TextWriter` (e.g. a `MemoryStream` or network stream):

```csharp
using var stream = new StreamWriter("output.vtr");
writer.Write(result, stream);
```