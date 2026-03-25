# Contributing to GalvanicCorrosionEngine

Thank you for your interest in contributing! This guide explains how to set up
your development environment, the conventions used throughout the codebase, and
the process for submitting changes.

## Table of Contents

- [Development Setup](#development-setup)
- [Project Structure](#project-structure)
- [Coding Conventions](#coding-conventions)
- [Adding a New Module](#adding-a-new-module)
- [Adding a New Material](#adding-a-new-material)
- [Writing Tests](#writing-tests)
- [Submitting a Pull Request](#submitting-a-pull-request)

## Development Setup

1. **Install prerequisites**
   - [.NET 10 SDK](https://dotnet.microsoft.com/download) (or later)
   - Any IDE with C# support (Visual Studio, Rider, VS Code + C# Dev Kit)

2. **Clone and build**
   ```bash
   git clone https://github.com/sap8b/GalvanicCorrosionEngine.git
   cd GalvanicCorrosionEngine
   dotnet build
   ```

3. **Run all tests**
   ```bash
   dotnet test
   ```

4. **Run a specific sample to verify end-to-end behaviour**
   ```bash
   dotnet run --project samples/BasicCorrosionExample
   dotnet run --project samples/BoltInPlate
   dotnet run --project samples/SideBySide
   ```

## Project Structure

```
GalvanicCorrosionEngine/
├── src/
│   ├── GCE.Core/             ← Domain interfaces and value types (no outward deps)
│   ├── GCE.Numerics/         ← Numerical solvers and linear algebra
│   ├── GCE.Electrochemistry/ ← Electrochemical models
│   ├── GCE.Atmosphere/       ← Atmospheric/weather models
│   ├── GCE.Simulation/       ← High-level simulation engine and geometry builders
│   ├── GCE.Configuration/    ← JSON/YAML configuration system
│   ├── GCE.IO/               ← Result exporters (CSV, JSON, VTK)
│   └── GCE.Console/          ← CLI entry point
├── tests/
│   ├── GCE.Atmosphere.Tests/
│   ├── GCE.Core.Tests/
│   ├── GCE.Electrochemistry.Tests/
│   ├── GCE.Numerics.Tests/
│   └── GCE.Simulation.Tests/
├── samples/
│   ├── BasicCorrosionExample/
│   ├── BoltInPlate/
│   └── SideBySide/
├── benchmarks/
│   └── GCE.Benchmarks/
├── schemas/
│   └── simulation-config.schema.json
├── docs/
│   ├── README.md             ← Getting started guide (this folder)
│   ├── ARCHITECTURE.md       ← Design and module overview
│   └── CONTRIBUTING.md       ← This file
├── README.md
└── ARCHITECTURE.md
```

See [ARCHITECTURE.md](ARCHITECTURE.md) for a detailed description of each module
and the dependency graph.

## Coding Conventions

### Language and Target Framework

- C# 13, `net10.0`, nullable reference types enabled, implicit usings enabled.
- Use `record` types for value objects, `sealed class` for concrete implementations
  that are not intended for inheritance.

### Naming

- Follow standard .NET naming conventions (PascalCase for types and members,
  camelCase for locals).
- Interfaces are prefixed with `I` (e.g., `ICorrosionModel`).
- No abbreviations in public API names.

### XML Documentation

All public types and members must have XML doc comments. Use the standard
`<summary>`, `<param>`, `<returns>`, `<exception>`, and `<remarks>` tags.

### Dependency Rules

Dependencies must always point **inward**:

```
GCE.Console → GCE.Simulation → GCE.Electrochemistry → GCE.Core
                             → GCE.Atmosphere        → GCE.Core
                             → GCE.Numerics          → GCE.Core
GCE.Console → GCE.IO        → GCE.Core
```

Never add a reference from an inner project to an outer one. When a new
abstraction is needed, add the interface to `GCE.Core` and the implementation
to the appropriate outer project.

### Null Safety

- Enable `<Nullable>enable</Nullable>` in every project file.
- Use `ArgumentNullException.ThrowIfNull` for public method parameters.
- Annotate reference-type return values and parameters explicitly.

### Error Handling

- Throw `ArgumentNullException`, `ArgumentOutOfRangeException`, or
  `ArgumentException` for invalid caller-supplied arguments.
- Use `ConfigurationValidationException` (in `GCE.Configuration`) for
  configuration validation errors.
- Avoid catching and re-throwing exceptions without adding information.

## Adding a New Module

1. Create a new `src/GCE.<Name>/` directory.
2. Add a `GCE.<Name>.csproj` with `net10.0`, nullable and implicit usings enabled.
3. Only add `<ProjectReference>` entries that respect the dependency rules above.
4. Add the project to `GalvanicCorrosionEngine.slnx` under the `/src/` folder.
5. Create a matching `tests/GCE.<Name>.Tests/` project using xUnit and add it
   under the `/tests/` folder in the solution file.

## Adding a New Material

Add a new static property to `MaterialRegistry` in `src/GCE.Core/MaterialRegistry.cs`:

```csharp
/// <summary>Titanium (Ti⁴⁺/Ti, −1.63 V vs. SHE).</summary>
public static IMaterial Titanium { get; } =
    Register(new Material("Titanium", StandardPotential: -1.63, ExchangeCurrentDensity: 1e-7,
        MolarMass: 0.04787, ElectronsTransferred: 4, Density: 4506.0));
```

Add a corresponding test in `tests/GCE.Core.Tests/MaterialRegistryTests.cs` that
verifies the new material is retrievable by name.

## Writing Tests

- Tests use **xUnit** (namespace `Xunit`).
- Test projects target `net10.0` and reference `xunit` and `xunit.runner.visualstudio`.
- Follow the existing file naming convention: `<Subject>Tests.cs`.
- Use `Assert.Equal`, `Assert.True`, `Assert.Throws`, and `Assert.InRange` from xUnit.
- One `[Fact]` or `[Theory]` per distinct behaviour; keep test methods focused and
  independent.

Example test structure:

```csharp
public class MyNewFeatureTests
{
    [Fact]
    public void MyMethod_WithValidInput_ReturnsExpectedResult()
    {
        // Arrange
        var sut = new MyClass();

        // Act
        double result = sut.MyMethod(42.0);

        // Assert
        Assert.Equal(expected: 84.0, actual: result, precision: 6);
    }
}
```

## Submitting a Pull Request

1. **Fork** the repository and create a feature branch:
   ```bash
   git checkout -b feature/my-feature
   ```

2. **Make your changes** following the coding conventions above.

3. **Add or update tests** for every changed behaviour.

4. **Build and test** locally:
   ```bash
   dotnet build
   dotnet test
   ```

5. **Commit** with a clear, imperative message:
   ```
   Add TitaniumAluminium galvanic couple example
   ```

6. **Open a pull request** against the `main` branch. In the PR description:
   - Summarise what changed and why.
   - Reference any related issues (e.g. `Closes #5`).
   - List any new public API surface introduced.

A maintainer will review your PR, leave comments if needed, and merge once all
checks pass.
