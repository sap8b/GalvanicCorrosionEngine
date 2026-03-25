# GalvanicCorrosionEngine

> A modular .NET library for the numerical simulation of galvanic corrosion between
> dissimilar metals under atmospheric and electrolytic conditions.

---

## Introduction

Galvanic corrosion arises when two electrochemically dissimilar metals are placed in
electrical contact within a common electrolyte. The difference in standard electrode
potentials between the two materials drives a net ionic current through the electrolyte
and a corresponding electron current through the metallic junction. The less noble metal
(the *anode*) undergoes accelerated oxidative dissolution, while the more noble metal
(the *cathode*) is cathodically protected. This mechanism underlies a broad class of
engineering failures — from fastener corrosion in airframe panels to pipeline degradation
at bimetallic joints — and accurate prediction of corrosion rates requires resolving both
the electrochemical kinetics at each electrode surface and the transport of ionic species
through the intervening electrolyte.

The governing electrode kinetics are described by the Butler–Volmer equation:

$$i = i_0 \left[ \exp\!\left(\frac{\alpha_a F \eta}{RT}\right) - \exp\!\left(-\frac{\alpha_c F \eta}{RT}\right) \right]$$

where *i* is the current density, *i*₀ is the exchange current density, α*ₐ* and α*꜀*
are the anodic and cathodic transfer coefficients, *F* is Faraday's constant, *η* is the
overpotential (the deviation from equilibrium potential), *R* is the universal gas
constant, and *T* is temperature in kelvin. The *mixed potential* (or *corrosion
potential*) is the electrode potential at which the anodic and cathodic partial currents
are equal and opposite; it is found iteratively as a root-finding problem at each time
step.

Corrosion rates are sensitive to environmental conditions — temperature, relative
humidity, and electrolyte chloride concentration — all of which vary in service.
Atmospheric corrosion in particular involves a thin adsorbed electrolyte film whose
conductivity and thickness are functions of the ambient humidity and temperature.
GalvanicCorrosionEngine captures these dependencies through empirical correlations
incorporated into the `AtmosphericConditions` electrolyte model, and supports
time-varying weather drivers via the `IWeatherProvider` interface.

The simulation engine integrates the coupled electrode-potential and corrosion-rate
ODEs using a 4th-order Runge–Kutta scheme, with an optional adaptive time-stepper
(`TimeEvolver`) that varies the step size according to a local truncation-error
estimate. Spatial field quantities (current density, potential distribution) can be
resolved on a 2D rectilinear mesh using the included PDE solvers — a Gauss–Seidel SOR
Poisson solver and a Peaceman–Rachford ADI diffusion solver — and exported to VTK
format for visualization in ParaView or VisIt.

The library supersedes an earlier single-file MATLAB script (`CorrosionModel7`) by
providing a structured, extensible .NET architecture, higher-order time integration,
spatial PDE capability, and weather coupling, while achieving ≥ 24× faster wall time
for equivalent fixed-step scenarios.

---

## Table of Contents

- [Introduction](#introduction)
- [Module Overview](#module-overview)
  - [GCE.Core — Domain Contracts](#gcecore--domain-contracts)
  - [GCE.Numerics — Numerical Solvers](#gcenumerics--numerical-solvers)
  - [GCE.Electrochemistry — Electrode Kinetics](#gceelectrochemistry--electrode-kinetics)
  - [GCE.Atmosphere — Environmental Models](#gceatmosphere--environmental-models)
  - [GCE.Simulation — Simulation Engine & Geometry](#gcesimulation--simulation-engine--geometry)
  - [GCE.Configuration — JSON/YAML Configuration](#gceconfiguration--jsonyaml-configuration)
  - [GCE.IO — Result Export & Visualization Output](#gceio--result-export--visualization-output)
- [Setting Up and Running Simulations](#setting-up-and-running-simulations)
  - [Prerequisites](#prerequisites)
  - [Building the Solution](#building-the-solution)
  - [Configuration File Format](#configuration-file-format)
  - [Sample Setup JSON](#sample-setup-json)
  - [Running via the CLI](#running-via-the-cli)
  - [Running the Bundled Samples](#running-the-bundled-samples)
  - [Running Programmatically](#running-programmatically)
- [Visualizing Results](#visualizing-results)
  - [Opening VTK Output in ParaView](#opening-vtk-output-in-paraview)
  - [Working with Time-Series CSV/JSON Output](#working-with-time-series-csvjson-output)
- [Further Documentation](#further-documentation)

---

## Module Overview

GalvanicCorrosionEngine is organized as a layered solution in which dependencies
always point **inward** — outer modules depend on inner modules, never the reverse.
Each module is described briefly below, with a link to the detailed documentation.

### GCE.Core — Domain Contracts

`GCE.Core` is the innermost layer and carries no dependencies on any other GCE
project. It defines all domain interfaces (`IMaterial`, `IElectrode`, `IElectrolyte`,
`IGalvanicCell`, `IEnvironment`, `ICorrosionModel`, `IGeometryBuilder`,
`IWeatherObservation`, `IWeatherProvider`, `ISimulationRunner`), the immutable value
types that implement them (`Material`, `Electrode`, `GeometryMesh`), the
`MaterialRegistry` of pre-configured metals, and the shared `PhysicalConstants`.

→ See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the full interface table.

---

### GCE.Numerics — Numerical Solvers

`GCE.Numerics` provides general-purpose numerical methods with no electrochemical
domain knowledge. It includes:

- **ODE integration** — `RungeKuttaSolver` (4th-order Runge–Kutta).
- **Root finding** — `BrentSolver` (robust bracketed bisection).
- **PDE solvers** — `LaplaceSolver2D`, `PoissonSolver2D` (Gauss–Seidel SOR),
  `DiffusionSolver1D` (Crank–Nicolson), `DiffusionSolver2D` (Peaceman–Rachford ADI).
- **Linear algebra** — `Vector`, `Matrix`, `SparseMatrix` with SIMD-vectorized
  dot products via `System.Numerics.Vector<double>`.
- **Boundary conditions** — `DirichletBC`, `NeumannBC`, `RobinBC`.
- **Unit arithmetic** — `UnitValue` and `UnitConversions`.

The ADI diffusion solver sweeps are parallelized with `Parallel.For`, yielding
≈ 3.2× speedup on a 4-core host for 128 × 128 grids.

→ See [`PERFORMANCE.md`](PERFORMANCE.md) for solver scaling tables and benchmark results.

---

### GCE.Electrochemistry — Electrode Kinetics

`GCE.Electrochemistry` implements the electrochemical domain logic:

- `GalvanicPair` — pairs an anode and cathode and enforces thermodynamic ordering
  (lower standard potential = anode).
- `GalvanicCell` / `GalvanicCouple` — couples two `IElectrode` objects through an
  `IElectrolyte`; `GalvanicCouple` uses bisection to locate the mixed potential.
- `ButlerVolmerModel` / `ButlerVolmerKinetics` / `TafelKinetics` — current-density
  and corrosion-rate computation via the Butler–Volmer equation.
- `MetalDissolutionReaction`, `OxygenReductionReaction`, `HydrogenEvolutionReaction`
  — individual Faradaic reaction models with Nernst-corrected equilibrium potentials.
- `ThinFilmElectrolyte`, `BulkElectrolyte`, `SpeciesTransport` — electrolyte and
  ion-transport models using Kohlrausch conductivity.

→ See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md#gceelectrochemistry) for class details.

---

### GCE.Atmosphere — Environmental Models

`GCE.Atmosphere` models the atmospheric and electrolytic environment:

- `AtmosphericConditions` — derives electrolyte conductivity from temperature, relative
  humidity, and chloride concentration using empirical correlations.
- `WeatherDrivenAtmosphericConditions` — constructs an `IEnvironment` from a live
  `IWeatherObservation`.
- `SyntheticWeatherProvider` — generates synthetic diurnal weather cycles.
- `CsvWeatherProvider` — reads observed weather data from a CSV file.
- `FilmEvolution` — thin-film electrolyte growth/evaporation model.
- `TimeOfWetnessCalculator` — computes ISO 9223 time-of-wetness metrics.

→ See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md#gceatmosphere) and
[`docs/README.md`](docs/README.md#time-varying-weather-simulations).

---

### GCE.Simulation — Simulation Engine & Geometry

`GCE.Simulation` is the high-level orchestration layer:

- `SimulationEngine` — drives the time-integration loop; exposes synchronous `Run`,
  asynchronous `RunAsync`, and `Resume` (checkpoint restart) methods.
- `SimulationParameters` / `SimulationResult` / `SimulationState` — input DTO,
  output DTO, and checkpoint record.
- `TimeEvolver` / `ConvergenceChecker` — adaptive time-stepping with local error
  control.
- **Geometry builders** (`GCE.Simulation.Geometry` namespace):
  - `BoltInPlateGeometry` — bolt-in-plate cross-section.
  - `SideBySideGeometry` — two sheets sharing a common electrolyte.
  - `CustomGeometry` — arbitrary electrode areas with a user-supplied mesh.

→ See [`docs/README.md`](docs/README.md#using-a-geometry) for geometry code examples.

---

### GCE.Configuration — JSON/YAML Configuration

`GCE.Configuration` provides a file-driven setup path:

- `SimulationConfigReader` — reads `SimulationConfig` from JSON (`System.Text.Json`)
  or YAML (`YamlDotNet`).
- `SimulationConfigValidator` — validates the config and returns structured error
  messages.
- `SimulationConfigMapper` — maps a validated config to `SimulationParameters`.

The JSON schema is at [`schemas/simulation-config.schema.json`](schemas/simulation-config.schema.json).
Example config files are in [`samples/config/`](samples/config/).

→ See [Setting Up and Running Simulations](#setting-up-and-running-simulations) below.

---

### GCE.IO — Result Export & Visualization Output

`GCE.IO` exposes three result writers, all implementing `IResultWriter`:

| Class | Output format | File extension |
|---|---|---|
| `CsvResultWriter` | Comma-separated values | `.csv` |
| `JsonResultWriter` | JSON object with parallel arrays | `.json` |
| `VtkResultWriter` | VTK XML RectilinearGrid | `.vtr` |

`VtkResultWriter` can be instantiated with or without a `GeometryMesh`. Without a
mesh it writes three VTK `FieldData` arrays (`Time_s`, `MixedPotential_V`,
`CorrosionRate_mmPerYear`). With a mesh it additionally writes a `RegionId` point-data
array marking each grid node as anode (0), cathode (1), or electrolyte/gap (−1).

→ See [Visualizing Results](#visualizing-results) below for ParaView workflow.

---

## Setting Up and Running Simulations

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later.
- A terminal / command prompt.
- *(For visualization)* [ParaView](https://www.paraview.org/) ≥ 5.11 or
  [VisIt](https://visit-dav.github.io/visit-website/).

### Building the Solution

```bash
git clone https://github.com/sap8b/GalvanicCorrosionEngine.git
cd GalvanicCorrosionEngine
dotnet build
dotnet test          # optional — verify all tests pass
```

---

### Configuration File Format

Simulations can be driven entirely by a JSON (or YAML) configuration file. The schema
is defined in [`schemas/simulation-config.schema.json`](schemas/simulation-config.schema.json).
Top-level keys are:

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `anode` | `material` | — | Less-noble electrode material. |
| `cathode` | `material` | — | More-noble electrode material. |
| `environment` | `environment` | — | Static atmospheric conditions. |
| `durationSeconds` | number | 3600 | Total simulation time in seconds. |
| `timeSteps` | integer | 1000 | Number of time-integration steps. |
| `weather` | `weather` \| null | null | Optional time-varying weather driver. |

A **`material`** object accepts either a `name` string for a registry lookup
(`"Zinc"`, `"Mild Steel"`, `"Aluminium"`, `"Copper"`, `"Nickel"`, `"Magnesium"`) or
the full set of electrochemical properties for a custom metal
(`standardPotential`, `exchangeCurrentDensity`, `molarMass`, `electronsTransferred`,
`density`).

A **`weather`** object requires a `type` field of `"none"`, `"synthetic"`, or `"csv"`.
For `"synthetic"`, the diurnal cycle is parameterized by `baseTempCelsius`,
`tempAmplitude`, `baseRelativeHumidity`, `humidityAmplitude`, and
`chlorideConcentration`. For `"csv"`, provide a `csvPath` to a weather observation
file.

---

### Sample Setup JSON

The following configuration models a zinc anode protecting a mild-steel cathode in a
coastal atmospheric environment over a 24-hour period with a synthetic diurnal weather
cycle. It is a ready-to-run starting point for new simulations.

```json
{
  "$schema": "https://github.com/sap8b/GalvanicCorrosionEngine/schemas/simulation-config.schema.json",

  "anode": {
    "name": "Zinc"
  },

  "cathode": {
    "name": "Mild Steel"
  },

  "environment": {
    "temperatureCelsius":    20.0,
    "relativeHumidity":      0.80,
    "chlorideConcentration": 0.5
  },

  "durationSeconds": 86400,
  "timeSteps":       1440,

  "weather": {
    "type":                 "synthetic",
    "baseTempCelsius":      18.0,
    "tempAmplitude":        8.0,
    "baseRelativeHumidity": 0.72,
    "humidityAmplitude":    0.18,
    "chlorideConcentration": 0.5,
    "precipitation":        0.0,
    "windSpeed":            3.5
  }
}
```

Save this as (e.g.) `my-simulation.json`, then run:

```bash
dotnet run --project src/GCE.Console -- --config my-simulation.json
```

The engine will validate the configuration, run the 24-hour simulation (1 440 time
steps with the synthetic weather driver), and write the results to the working
directory. By default `GCE.Console` emits a `.csv` file; edit the console entry point
or call the writers directly in code to obtain JSON or VTK output.

Additional example configurations are provided in [`samples/config/`](samples/config/):

| File | Description |
|------|-------------|
| [`static-simulation.json`](samples/config/static-simulation.json) | 1-hour static atmosphere, zinc vs. mild steel |
| [`static-simulation.yaml`](samples/config/static-simulation.yaml) | Same as above in YAML format |
| [`weather-simulation.json`](samples/config/weather-simulation.json) | 24-hour synthetic diurnal weather |
| [`weather-simulation.yaml`](samples/config/weather-simulation.yaml) | Same as above in YAML format |

---

### Running via the CLI

```bash
# Static simulation from a JSON config
dotnet run --project src/GCE.Console -- --config samples/config/static-simulation.json

# Weather-driven simulation from a YAML config
dotnet run --project src/GCE.Console -- --config samples/config/weather-simulation.yaml

# No config file — built-in defaults (zinc / mild steel, 1 h, static)
dotnet run --project src/GCE.Console
```

---

### Running the Bundled Samples

Four sample programs in `samples/` cover the most common usage patterns:

```bash
# Core API: 1-hour zinc/steel simulation → example_output.csv
dotnet run --project samples/BasicCorrosionExample

# Bolt-in-plate geometry → bolt_in_plate_output.csv + .vtr
dotnet run --project samples/BoltInPlate

# Side-by-side geometry → side_by_side_output.csv + .vtr
dotnet run --project samples/SideBySide
```

See [`docs/README.md`](docs/README.md#running-the-samples) for a detailed walkthrough
of each sample.

---

### Running Programmatically

```csharp
using GCE.Atmosphere;
using GCE.Core;
using GCE.Electrochemistry;
using GCE.IO;
using GCE.Simulation;

// 1. Select materials
IMaterial zinc  = MaterialRegistry.Zinc;
IMaterial steel = MaterialRegistry.MildSteel;

// 2. Define the static environment
var env = new AtmosphericConditions(
    TemperatureCelsius:    20.0,
    RelativeHumidity:      0.80,
    ChlorideConcentration: 0.5);

// 3. Build a galvanic pair (lower standard potential → anode)
var pair = new GalvanicPair(anode: zinc, cathode: steel);

// 4. Optionally attach a synthetic weather driver
var weather = new SyntheticWeatherProvider(
    baseTempCelsius:       18.0,
    tempAmplitude:         8.0,
    baseRelativeHumidity:  0.72,
    humidityAmplitude:     0.18,
    chlorideConcentration: 0.5);

// 5. Configure and run
var parameters = new SimulationParameters(
    pair, env,
    DurationSeconds: 86_400,
    TimeSteps:       1440,
    WeatherProvider: weather);

SimulationResult result = new SimulationEngine().Run(parameters);

Console.WriteLine($"Average corrosion rate: {result.AverageCorrosionRate:F4} mm/year");

// 6. Export
new CsvResultWriter().WriteToFile(result, "output.csv");
new JsonResultWriter().WriteToFile(result, "output.json");
new VtkResultWriter().WriteToFile(result,  "output.vtr");
```

To export a spatially resolved mesh alongside the time-series data, use a geometry
builder:

```csharp
using GCE.Simulation.Geometry;

var geometry = new SideBySideGeometry(
    anodeMaterial:   MaterialRegistry.Zinc,
    cathodeMaterial: MaterialRegistry.MildSteel,
    anodeWidth:      0.020,   // 20 mm
    cathodeWidth:    0.100,   // 100 mm
    length:          0.030);  // 30 mm

var cell = geometry.Build(env);
GeometryMesh mesh = geometry.BuildMesh(nodesX: 40, nodesY: 20);

new VtkResultWriter(mesh).WriteToFile(result, "output_spatial.vtr");
```

See [`docs/README.md`](docs/README.md) for the complete getting-started guide,
available materials, and custom-material registration.

---

## Visualizing Results

### Opening VTK Output in ParaView

The `VtkResultWriter` produces a **VTK XML RectilinearGrid** (`.vtr`) file readable by
[ParaView](https://www.paraview.org/) ≥ 5.11 and
[VisIt](https://visit-dav.github.io/visit-website/).

**Step-by-step workflow in ParaView:**

1. **Open the file** — *File → Open*, select `output.vtr`, click **Apply**.
2. **Inspect FieldData arrays** — In the *Information* tab, the three time-series
   arrays (`Time_s`, `MixedPotential_V`, `CorrosionRate_mmPerYear`) are listed under
   *Field Data*.
3. **Plot time series** — *Filters → Data Analysis → Plot Data* (or *Plot Selection
   Over Time*). Set the X axis to `Time_s` and the Y axis to `CorrosionRate_mmPerYear`
   or `MixedPotential_V`.
4. **View the spatial mesh** *(geometry runs only)* — Set the coloring dropdown to
   `RegionId` to confirm anode/cathode/electrolyte regions, or switch to another point
   array for field visualization. Use *Filters → Warp By Scalar* or *Surface With
   Edges* for additional context.
5. **Animate** — With a time-series VTK collection or a multi-block dataset, use the
   *VCR controls* in the toolbar to step through time.

> **Tip:** To obtain per-timestep spatial snapshots rather than a single file, run the
> simulation in a loop, calling `VtkResultWriter` for each checkpoint and saving files
> as `output_t0000.vtr`, `output_t0001.vtr`, etc. Then use *File → Open* with the
> wildcard pattern to load them as a time series in ParaView.

**Screenshot — corrosion rate time series (ParaView Plot Data view):**

<!-- TODO: Replace with actual screenshot after running a simulation -->
![Corrosion rate time series in ParaView](docs/images/paraview_corrosion_rate_timeseries.png)

---

**Screenshot — spatial RegionId mesh rendered in ParaView (side-by-side geometry):**

<!-- TODO: Replace with actual screenshot after running the SideBySide sample -->
![Spatial mesh RegionId in ParaView](docs/images/paraview_spatial_regionid.png)

---

### Working with Time-Series CSV/JSON Output

`CsvResultWriter` produces a two-column file with headers
`Time_s,MixedPotential_V,CorrosionRate_mmPerYear`. It can be opened directly in
Excel, MATLAB, Python (pandas/matplotlib), or any plotting tool.

**Python quick-plot example:**

```python
import pandas as pd
import matplotlib.pyplot as plt

df = pd.read_csv("output.csv")

fig, ax1 = plt.subplots()
ax1.plot(df["Time_s"] / 3600, df["CorrosionRate_mmPerYear"], color="tab:red")
ax1.set_xlabel("Time (h)")
ax1.set_ylabel("Corrosion Rate (mm/year)", color="tab:red")

ax2 = ax1.twinx()
ax2.plot(df["Time_s"] / 3600, df["MixedPotential_V"], color="tab:blue", linestyle="--")
ax2.set_ylabel("Mixed Potential (V vs. SHE)", color="tab:blue")

plt.title("Galvanic Corrosion — Zinc / Mild Steel, Coastal Atmosphere")
plt.tight_layout()
plt.savefig("corrosion_timeseries.png", dpi=150)
```

`JsonResultWriter` produces a JSON object with three parallel arrays under keys
`time_s`, `mixed_potential_V`, and `corrosion_rate_mm_per_year`, suitable for web
dashboards or downstream processing with `System.Text.Json` / `Newtonsoft.Json`.

---

## Further Documentation

| Document | Contents |
|----------|----------|
| [`docs/README.md`](docs/README.md) | Getting-started guide: build, samples, code examples, available materials |
| [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) | Layered architecture, module descriptions, key abstractions, data-flow diagram |
| [`docs/CONTRIBUTING.md`](docs/CONTRIBUTING.md) | Development setup, coding conventions, adding materials/modules, submitting PRs |
| [`PERFORMANCE.md`](PERFORMANCE.md) | Benchmark methodology, profiling findings, optimizations, scaling tables |
| [`schemas/simulation-config.schema.json`](schemas/simulation-config.schema.json) | JSON Schema for simulation configuration files |
| [`samples/config/`](samples/config/) | Ready-to-run JSON and YAML configuration examples |
