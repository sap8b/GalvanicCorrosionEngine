using GCE.Core;
using GCE.Electrochemistry;

namespace GCE.Simulation;

/// <summary>
/// Parameters controlling a galvanic corrosion simulation run.
/// </summary>
/// <remarks>
/// When <see cref="WeatherProvider"/> is supplied the simulation uses time-varying
/// conditions derived from that provider at each integration step, and
/// <see cref="Environment"/> serves only as a fallback for static runs.
/// When <see cref="WeatherProvider"/> is <see langword="null"/>,
/// <see cref="Environment"/> is used throughout.
/// <para>
/// When <see cref="UseAdaptiveTimeStep"/> is <see langword="true"/> the engine uses
/// <see cref="TimeEvolver.AdvanceAdaptive"/> to adapt the step size at each integration
/// step based on the observed solution change, and records the convergence history in
/// <see cref="SimulationResult.ConvergenceHistory"/>.
/// </para>
/// </remarks>
public sealed record SimulationParameters(
    GalvanicPair Pair,
    IEnvironment Environment,
    double DurationSeconds,
    int TimeSteps = 1000,
    IWeatherProvider? WeatherProvider = null,
    bool UseAdaptiveTimeStep = false);
