using GCE.Core;
using GCE.Electrochemistry;
using GCE.Numerics;

namespace GCE.Simulation;

/// <summary>
/// Parameters controlling a galvanic corrosion simulation run.
/// </summary>
public sealed record SimulationParameters(
    GalvanicPair Pair,
    IEnvironment Environment,
    double DurationSeconds,
    int TimeSteps = 1000);
