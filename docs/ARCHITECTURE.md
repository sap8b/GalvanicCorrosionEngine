# Architecture

GalvanicCorrosionEngine follows a layered, SOLID-oriented architecture.
Dependencies always point **inward** — outer layers depend on inner layers, never the reverse.

## Dependency Graph

```
GCE.Console
    ├── GCE.Simulation
    │       ├── GCE.Electrochemistry
    │       │       ├── GCE.Core
    │       │       └── GCE.Numerics
    │       │               └── GCE.Core
    │       ├── GCE.Atmosphere
    │       │       ├── GCE.Core
    │       │       └── GCE.Numerics
    │       └── GCE.Core
    ├── GCE.IO
    │       └── GCE.Core
    └── GCE.Core
```

`GCE.Configuration` is an optional peer of `GCE.Simulation` used only by `GCE.Console`
to read JSON/YAML configuration files.

## Modules

### GCE.Core

The innermost layer. Contains only:

- **Interfaces** — `IMaterial`, `IElectrode`, `IElectrolyte`, `IGalvanicCell`,
  `IEnvironment`, `ICorrosionModel`, `IGeometryBuilder`, `IWeatherObservation`,
  `IWeatherProvider` — define all domain contracts.
- **Value types** — `Material`, `Electrode`, `GeometryMesh` records for zero-cost
  domain objects.
- **Registry** — `MaterialRegistry` provides pre-configured common metals and
  supports runtime registration of custom materials.
- **Physical constants** — `PhysicalConstants` shared across the solution.

No dependencies on other GCE projects.

### GCE.Numerics

General-purpose numerical methods with no domain knowledge:

- **Linear algebra** — `Vector`, `Matrix`, `SparseMatrix` in `GCE.Numerics.LinearAlgebra`.
- **ODE solvers** — `RungeKuttaSolver` (4th-order Runge–Kutta).
- **Root finding** — `BrentSolver` (robust bracketed root-finding).
- **PDE solvers** — `DiffusionSolver1D` (Crank–Nicolson), `DiffusionSolver2D`
  (Peaceman–Rachford ADI), `LaplaceSolver2D`, `PoissonSolver2D` (Gauss–Seidel SOR).
- **Boundary conditions** — `DirichletBC`, `NeumannBC`, `RobinBC`.
- **Units** — `UnitValue`, `UnitConversions` for explicit physical-unit arithmetic.

Depends only on `GCE.Core`.

### GCE.Electrochemistry

Electrochemical domain logic:

- `GalvanicPair` — Encapsulates an anode/cathode pair and enforces thermodynamic ordering.
- `GalvanicCell` / `GalvanicCouple` — Implements `IGalvanicCell`; couples two
  `IElectrode` objects through an `IElectrolyte`. `GalvanicCouple` uses bisection
  for the mixed potential.
- `ButlerVolmerModel` / `ButlerVolmerKinetics` / `TafelKinetics` — Implements
  `ICorrosionModel` using the Butler–Volmer equation.
- `Anode`, `Cathode` — Typed electrode wrappers with area scaling.
- `MetalDissolutionReaction`, `OxygenReductionReaction`, `HydrogenEvolutionReaction` —
  Faradaic reaction models with Nernst-corrected equilibrium potentials.
- `ThinFilmElectrolyte`, `BulkElectrolyte`, `SpeciesTransport` — Electrolyte and
  ion transport models using Kohlrausch conductivity.

Depends on `GCE.Core` and `GCE.Numerics`.

### GCE.Atmosphere

Atmospheric and electrolytic environment modelling:

- `AtmosphericConditions` — Implements `IElectrolyte` / `IEnvironment` from
  temperature, relative humidity, and chloride concentration using empirical correlations.
- `WeatherDrivenAtmosphericConditions` — Derives `IEnvironment` from an
  `IWeatherObservation`.
- `WeatherObservation` — Concrete `IWeatherObservation` record.
- `SyntheticWeatherProvider` — Generates synthetic diurnal weather observations.
- `CsvWeatherProvider` — Reads weather observations from CSV files.
- `FilmEvolution` — Thin-film electrolyte growth/evaporation model driven by weather.
- `TimeOfWetnessCalculator` — Computes time-of-wetness metrics for corrosion rate
  estimation.

Depends on `GCE.Core` and `GCE.Numerics`.

### GCE.Simulation

High-level orchestration:

- `SimulationEngine` — Drives the time integration loop, coupling electrochemical and
  atmospheric models. Implements `ISimulationRunner` with synchronous `Run`,
  asynchronous `RunAsync`, and `Resume` (checkpoint restart) methods.
- `SimulationParameters` — Input DTO: galvanic pair, environment, duration, time
  steps, optional weather provider, and adaptive time-stepping flag.
- `SimulationResult` — Output DTO: time-series of mixed potentials and corrosion rates,
  plus optional convergence history.
- `SimulationState` — Checkpoint record enabling pause and resume.
- `TimeEvolver` / `ConvergenceChecker` — Adaptive time-stepping with convergence
  tracking.
- **Geometry builders** (`GCE.Simulation.Geometry` namespace):
  - `BoltInPlateGeometry` — Bolt-in-plate cross-section geometry.
  - `SideBySideGeometry` — Two sheets placed side-by-side geometry.
  - `CustomGeometry` — Arbitrary electrode areas with a custom mesh.

Depends on `GCE.Core`, `GCE.Numerics`, `GCE.Electrochemistry`, and `GCE.Atmosphere`.

### GCE.Configuration

JSON/YAML configuration system:

- `SimulationConfigReader` — Reads `SimulationConfig` from JSON (via `System.Text.Json`)
  or YAML (via `YamlDotNet`) files.
- `SimulationConfigValidator` — Validates a `SimulationConfig` and returns a list of
  error messages.
- `SimulationConfigMapper` — Maps a validated `SimulationConfig` to `SimulationParameters`.
- Data classes: `SimulationConfig`, `MaterialConfig`, `EnvironmentConfig`, `WeatherConfig`.

Depends on `GCE.Core`, `GCE.Atmosphere`, `GCE.Electrochemistry`, and `GCE.Simulation`.

### GCE.IO

Output adapters:

- `IResultWriter` — Common interface: `Write(SimulationResult, TextWriter)` and
  `WriteToFile(SimulationResult, string)`.
- `CsvResultWriter` / `CsvExporter` — Serialise a `SimulationResult` to CSV.
- `JsonResultWriter` — Serialises a `SimulationResult` to JSON (pretty-printed by
  default; configurable via `Indented`).
- `VtkResultWriter` — Writes a VTK XML RectilinearGrid (`.vtr`) file; optionally
  embeds a `GeometryMesh` as spatial point data alongside time-series field data.

Depends on `GCE.Core` and `GCE.Simulation`.

### GCE.Console

Executable entry point that wires all components together and demonstrates an
end-to-end simulation. Accepts an optional `--config <file>` argument to drive the
simulation from a JSON or YAML file.

## Key Abstractions

| Interface | Responsibility |
|-----------|----------------|
| `IMaterial` | Physical and electrochemical properties of a metal |
| `IElectrode` | An electrode with material and exposed area |
| `IElectrolyte` | Ionic medium connecting anode and cathode |
| `IGalvanicCell` | Paired anode/cathode through a shared electrolyte |
| `IEnvironment` | Observable environment conditions (temperature, humidity, chlorides) |
| `ICorrosionModel` | Computes current density and corrosion rate at a given potential |
| `IGeometryBuilder` | Constructs an `IGalvanicCell` and a `GeometryMesh` from physical dimensions |
| `IWeatherObservation` | A snapshot of atmospheric conditions at one instant |
| `IWeatherProvider` | Produces `IWeatherObservation` values on demand for time-varying simulations |
| `ISimulationRunner` | Runs, pauses, and resumes a simulation |

## Design Principles

| Principle | Application |
|-----------|-------------|
| **Single Responsibility** | Each project has exactly one cohesive concern. |
| **Open/Closed** | New material models, geometries, or solvers can be added without modifying existing code. |
| **Liskov Substitution** | Any `ICorrosionModel` / `IEnvironment` implementation can be substituted freely. |
| **Interface Segregation** | All domain interfaces in `GCE.Core` are narrow and client-specific. |
| **Dependency Inversion** | Outer layers reference abstractions defined in `GCE.Core`; implementations live in outer modules. |

## Data Flow

```
Configuration file
      │
      ▼
SimulationConfigReader → SimulationConfigValidator → SimulationConfigMapper
                                                              │
                                                              ▼
                                                   SimulationParameters
                                                              │
                                                              ▼
                                                    SimulationEngine.Run()
                                                              │
               ┌──────────────────────────────────────────────┤
               │                                              │
               ▼                                              ▼
    GalvanicPair + ButlerVolmerModel           WeatherProvider (optional)
               │                                              │
               └──────────────────────────────────────────────┘
                                    │
                                    ▼
                             SimulationResult
                                    │
              ┌─────────────────────┼──────────────────────┐
              ▼                     ▼                       ▼
        CsvExporter          JsonResultWriter        VtkResultWriter
```
