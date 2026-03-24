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
/// </remarks>
public sealed record SimulationParameters(
    GalvanicPair Pair,
    IEnvironment Environment,
    double DurationSeconds,
    int TimeSteps = 1000,
    IWeatherProvider? WeatherProvider = null);
