using GCE.Atmosphere;
using GCE.Core;
using GCE.Electrochemistry;
using GCE.Numerics;

namespace GCE.Simulation;

/// <summary>
/// Orchestrates a galvanic corrosion simulation over time using
/// Butler–Volmer kinetics and a Runge–Kutta time integrator.
/// </summary>
/// <remarks>
/// When <see cref="SimulationParameters.WeatherProvider"/> is provided the
/// electrochemical environment is updated at every integration step from the
/// weather data, enabling time-varying simulations driven by realistic or
/// synthetic atmospheric conditions.
/// </remarks>
public sealed class SimulationEngine
{
    /// <summary>
    /// Runs a galvanic corrosion simulation according to the given parameters.
    /// </summary>
    /// <param name="parameters">Simulation configuration.</param>
    /// <returns>A <see cref="SimulationResult"/> containing time-series data.</returns>
    public SimulationResult Run(SimulationParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        // Resolves the environment for a given simulation time.
        // When a weather provider is present the environment tracks the weather;
        // otherwise the static environment from the parameters is used.
        IEnvironment GetEnvironmentAt(double t) =>
            parameters.WeatherProvider is not null
                ? new WeatherDrivenAtmosphericConditions(parameters.WeatherProvider.GetObservation(t))
                : parameters.Environment;

        // Find the mixed potential at each step by zero-crossing of net current.
        // The environment (and therefore kinetic parameters) may vary with time.
        double FindMixedPotential(double t, double potential)
        {
            var env = GetEnvironmentAt(t);
            var anodeModel = new ButlerVolmerModel(parameters.Pair.Anode, env);
            var cathodeModel = new ButlerVolmerModel(parameters.Pair.Cathode, env);

            // dE/dt proportional to net current (simple relaxation model)
            double netCurrent = anodeModel.ComputeCurrentDensity(potential)
                              + cathodeModel.ComputeCurrentDensity(potential);
            return -netCurrent * 0.01;
        }

        double initialPotential =
            (parameters.Pair.Anode.StandardPotential +
             parameters.Pair.Cathode.StandardPotential) / 2.0;

        var solver = new RungeKuttaSolver(FindMixedPotential);
        var trajectory = solver.Integrate(
            0, parameters.DurationSeconds, initialPotential, parameters.TimeSteps);

        var times = trajectory.Select(p => p.T).ToList();
        var potentials = trajectory.Select(p => p.Y).ToList();

        // Compute corrosion rate at each time step using the environment applicable
        // at that instant so weather-driven variations are fully reflected.
        var rates = times
            .Zip(potentials, (t, e) => (t, e))
            .Select(pair =>
            {
                var env = GetEnvironmentAt(pair.t);
                return new ButlerVolmerModel(parameters.Pair.Anode, env)
                    .ComputeCorrosionRate(pair.e);
            })
            .ToList();

        return new SimulationResult
        {
            TimePoints = times,
            MixedPotentials = potentials,
            CorrosionRates = rates,
        };
    }
}
