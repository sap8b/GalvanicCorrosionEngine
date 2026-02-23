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

## Modules

### GCE.Core
The innermost layer.  Contains only:
- **Interfaces** (`IMaterial`, `IEnvironment`, `ICorrosionModel`) that define the domain contracts.
- **Value types** (`Material` record) for zero-cost domain objects.
- **Physical constants** (`PhysicalConstants`) shared across the solution.

No dependencies on other GCE projects.

### GCE.Numerics
General-purpose numerical methods with no domain knowledge:
- `RungeKuttaSolver` — 4th-order Runge–Kutta ODE integrator.
- `BrentSolver` — Robust bracketed root-finding.

Depends only on `GCE.Core` (for shared constants).

### GCE.Electrochemistry
Electrochemical domain logic:
- `GalvanicPair` — Encapsulates an anode/cathode pair and enforces thermodynamic ordering.
- `ButlerVolmerModel` — Implements `ICorrosionModel` using the Butler–Volmer equation.

Depends on `GCE.Core` and `GCE.Numerics`.

### GCE.Atmosphere
Atmospheric and electrolytic environment modelling:
- `AtmosphericConditions` — Implements `IEnvironment` from temperature, relative humidity, and chloride concentration using empirical correlations.

Depends on `GCE.Core`.

### GCE.Simulation
High-level orchestration:
- `SimulationEngine` — Drives the time integration loop, coupling electrochemical and atmospheric models.
- `SimulationParameters` / `SimulationResult` — Input/output data transfer objects.

Depends on `GCE.Core`, `GCE.Numerics`, `GCE.Electrochemistry`, and `GCE.Atmosphere`.

### GCE.IO
Output adapters:
- `CsvExporter` — Serialises a `SimulationResult` to CSV (file or arbitrary `TextWriter`).

Depends on `GCE.Core` and `GCE.Simulation`.

### GCE.Console
Executable entry point that wires all components together and demonstrates an end-to-end simulation.

## Design Principles

| Principle | Application |
|-----------|-------------|
| **Single Responsibility** | Each project has exactly one cohesive concern. |
| **Open/Closed** | New material models or solvers can be added without modifying existing code. |
| **Liskov Substitution** | Any `ICorrosionModel` / `IEnvironment` implementation can be substituted freely. |
| **Interface Segregation** | `IMaterial`, `IEnvironment`, and `ICorrosionModel` are narrow and client-specific. |
| **Dependency Inversion** | Outer layers reference abstractions (interfaces) defined in `GCE.Core`. |
