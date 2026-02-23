using GCE.Electrochemistry;
using GCE.Numerics;

namespace GCE.Simulation;

/// <summary>
/// Orchestrates a galvanic corrosion simulation over time using
/// Butler–Volmer kinetics and a Runge–Kutta time integrator.
/// </summary>
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

        var anodeModel = new ButlerVolmerModel(
            parameters.Pair.Anode, parameters.Environment);
        var cathodeModel = new ButlerVolmerModel(
            parameters.Pair.Cathode, parameters.Environment);

        // Find the mixed potential at each step by zero-crossing of net current
        double FindMixedPotential(double _t, double potential)
        {
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
        var rates = potentials
            .Select(e => anodeModel.ComputeCorrosionRate(e))
            .ToList();

        return new SimulationResult
        {
            TimePoints = times,
            MixedPotentials = potentials,
            CorrosionRates = rates,
        };
    }
}
