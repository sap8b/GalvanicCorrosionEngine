# GalvanicCorrosionEngine

A modular .NET library for simulating galvanic corrosion between dissimilar metals under atmospheric and electrolytic conditions.

## Projects

| Project | Description |
|---------|-------------|
| `GCE.Core` | Core interfaces (`IMaterial`, `IEnvironment`, `ICorrosionModel`) and domain types |
| `GCE.Numerics` | Numerical solvers (Runge–Kutta, Brent root-finder) |
| `GCE.Electrochemistry` | Galvanic pair model and Butler–Volmer kinetics |
| `GCE.Atmosphere` | Atmospheric condition models (`AtmosphericConditions`) |
| `GCE.Simulation` | High-level simulation engine and result types |
| `GCE.IO` | Result export utilities (CSV) |
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