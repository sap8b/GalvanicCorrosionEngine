using BenchmarkDotNet.Attributes;
using GCE.Atmosphere;
using GCE.Core;
using GCE.Electrochemistry;
using GCE.Simulation;

namespace GCE.Benchmarks;

/// <summary>
/// Benchmarks for <see cref="SimulationEngine"/> at varying time-step counts.
/// These measure the full ODE integration path (both fixed-step and adaptive).
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class SimulationEngineBenchmarks
{
    private SimulationParameters _params100  = null!;
    private SimulationParameters _params500  = null!;
    private SimulationParameters _params1000 = null!;
    private SimulationParameters _paramsAdaptive = null!;

    private readonly SimulationEngine _engine = new();

    [GlobalSetup]
    public void Setup()
    {
        var pair = new GalvanicPair(MaterialRegistry.Zinc, MaterialRegistry.Copper);
        var env  = new AtmosphericConditions(TemperatureCelsius: 25.0, RelativeHumidity: 0.75, ChlorideConcentration: 0.1);

        _params100  = new SimulationParameters(pair, env, DurationSeconds: 3600.0, TimeSteps: 100);
        _params500  = new SimulationParameters(pair, env, DurationSeconds: 3600.0, TimeSteps: 500);
        _params1000 = new SimulationParameters(pair, env, DurationSeconds: 3600.0, TimeSteps: 1000);
        _paramsAdaptive = new SimulationParameters(pair, env, DurationSeconds: 3600.0, TimeSteps: 200,
            UseAdaptiveTimeStep: true);
    }

    [Benchmark(Baseline = true)]
    public SimulationResult Run_100Steps() => _engine.Run(_params100);

    [Benchmark]
    public SimulationResult Run_500Steps() => _engine.Run(_params500);

    [Benchmark]
    public SimulationResult Run_1000Steps() => _engine.Run(_params1000);

    [Benchmark]
    public SimulationResult Run_AdaptiveTimeStep() => _engine.Run(_paramsAdaptive);
}
