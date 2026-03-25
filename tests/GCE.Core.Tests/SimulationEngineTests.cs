using GCE.Atmosphere;
using GCE.Core;
using GCE.Electrochemistry;
using GCE.Simulation;

namespace GCE.Core.Tests;

// ── Helpers ───────────────────────────────────────────────────────────────────

file static class SimulationTestFixtures
{
    // Zinc (-0.76 V) as anode, Copper (+0.34 V) as cathode.
    public static SimulationParameters DefaultParameters(
        double durationSeconds = 3600.0,
        int    timeSteps       = 100) =>
        new(
            new GalvanicPair(MaterialRegistry.Zinc, MaterialRegistry.Copper),
            new AtmosphericConditions(25.0, 0.75, 0.1),
            durationSeconds,
            timeSteps);
}

// ── SimulationProgress ────────────────────────────────────────────────────────

public class SimulationProgressTests
{
    [Fact]
    public void Fraction_IsZero_WhenNoSteps()
    {
        var p = new SimulationProgress(0, 0, 0, 3600, -0.5, 0.01);
        Assert.Equal(0.0, p.Fraction);
    }

    [Fact]
    public void Fraction_IsOne_WhenAllStepsCompleted()
    {
        var p = new SimulationProgress(100, 100, 3600, 3600, -0.5, 0.01);
        Assert.Equal(1.0, p.Fraction);
    }

    [Fact]
    public void Fraction_IsHalf_WhenHalfwayThrough()
    {
        var p = new SimulationProgress(50, 100, 1800, 3600, -0.5, 0.01);
        Assert.Equal(0.5, p.Fraction);
    }

    [Fact]
    public void Properties_ArePropagated()
    {
        var p = new SimulationProgress(10, 100, 360, 3600, -0.55, 0.07);
        Assert.Equal(10,     p.CurrentStep);
        Assert.Equal(100,    p.TotalSteps);
        Assert.Equal(360,    p.CurrentTime);
        Assert.Equal(3600,   p.TotalTime);
        Assert.Equal(-0.55,  p.MixedPotential);
        Assert.Equal(0.07,   p.CorrosionRate);
    }
}

// ── SimulationState ───────────────────────────────────────────────────────────

public class SimulationStateTests
{
    [Fact]
    public void DefaultState_HasEmptyLists()
    {
        var state = new SimulationState();
        Assert.Empty(state.TimePoints);
        Assert.Empty(state.MixedPotentials);
        Assert.Empty(state.CorrosionRates);
        Assert.Equal(0, state.CompletedSteps);
        Assert.Equal(0.0, state.CurrentTime);
        Assert.Equal(0.0, state.CurrentPotential);
    }

    [Fact]
    public void State_CanBeInitialisedViaInit()
    {
        var times = new List<double> { 0.0, 1.0 }.AsReadOnly();
        var state = new SimulationState
        {
            CompletedSteps   = 2,
            CurrentTime      = 1.0,
            CurrentPotential = -0.55,
            TimePoints       = times,
            MixedPotentials  = new List<double> { -0.60, -0.55 }.AsReadOnly(),
            CorrosionRates   = new List<double> { 0.10, 0.09 }.AsReadOnly(),
        };

        Assert.Equal(2,     state.CompletedSteps);
        Assert.Equal(1.0,   state.CurrentTime);
        Assert.Equal(-0.55, state.CurrentPotential);
        Assert.Equal(2,     state.TimePoints.Count);
    }
}

// ── ConvergenceChecker ────────────────────────────────────────────────────────

public class ConvergenceCheckerTests
{
    [Fact]
    public void Check_ReturnsFalse_WhenResidualAboveTolerance()
    {
        var checker = new ConvergenceChecker(residualTolerance: 1e-4, changeTolerance: 1e-6);
        bool converged = checker.Check(0, 1e-3, 1e-8);
        Assert.False(converged);
    }

    [Fact]
    public void Check_ReturnsFalse_WhenChangeAboveTolerance()
    {
        var checker = new ConvergenceChecker(residualTolerance: 1e-4, changeTolerance: 1e-6);
        bool converged = checker.Check(0, 1e-6, 1e-5);
        Assert.False(converged);
    }

    [Fact]
    public void Check_ReturnsTrue_WhenBothCriteriaMet()
    {
        var checker = new ConvergenceChecker(residualTolerance: 1e-4, changeTolerance: 1e-6);
        bool converged = checker.Check(0, 1e-5, 1e-7);
        Assert.True(converged);
    }

    [Fact]
    public void History_GrowsWithEachCheck()
    {
        var checker = new ConvergenceChecker();
        checker.Check(0, 1e-3, 1e-3);
        checker.Check(1, 1e-4, 1e-5);
        Assert.Equal(2, checker.History.Count);
    }

    [Fact]
    public void Reset_ClearsHistory()
    {
        var checker = new ConvergenceChecker();
        checker.Check(0, 1e-3, 1e-3);
        checker.Reset();
        Assert.Empty(checker.History);
    }

    [Fact]
    public void LastConverged_IsFalse_WhenHistoryIsEmpty()
    {
        var checker = new ConvergenceChecker();
        Assert.False(checker.LastConverged);
    }

    [Fact]
    public void LastConverged_ReflectsMostRecentCheck()
    {
        var checker = new ConvergenceChecker(residualTolerance: 1e-4, changeTolerance: 1e-6);
        checker.Check(0, 1e-3, 1e-3);   // not converged
        checker.Check(1, 1e-5, 1e-7);   // converged
        Assert.True(checker.LastConverged);
    }

    [Fact]
    public void AdaptTimeStep_DoublesWhenChangeIsNearlyZero()
    {
        var checker = new ConvergenceChecker();
        double newDt = checker.AdaptTimeStep(0.01, 0.0);
        Assert.Equal(0.02, newDt, precision: 10);
    }

    [Fact]
    public void AdaptTimeStep_ClampsToMaxDt()
    {
        var checker = new ConvergenceChecker();
        // Small-but-nonzero change → ratio drives dt to 1e4, clamped to maxDt = 0.5.
        // newDt = 0.01 × (1e-4 / 1e-10) = 1e4, clamped to 0.5.
        double newDt = checker.AdaptTimeStep(0.01, 1e-10, targetChange: 1e-4, maxDt: 0.5);
        Assert.Equal(0.5, newDt, precision: 10);
    }

    [Fact]
    public void AdaptTimeStep_ClampsToMinDt()
    {
        var checker = new ConvergenceChecker();
        // Large change → wants tiny dt, but minDt = 0.001.
        double newDt = checker.AdaptTimeStep(0.01, 100.0, targetChange: 1e-4, minDt: 0.001);
        Assert.Equal(0.001, newDt, precision: 10);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void Constructor_Throws_WhenResidualToleranceNotPositive(double tol)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ConvergenceChecker(residualTolerance: tol));
    }
}

// ── TimeEvolver ───────────────────────────────────────────────────────────────

public class TimeEvolverTests
{
    private static ConvergenceChecker DefaultChecker() =>
        new(residualTolerance: 1e-4, changeTolerance: 1e-4, maxIterations: 20);

    [Fact]
    public void Advance_ReturnsCorrectValue_ForSimpleLinearOde()
    {
        // dy/dt = 0 → y stays constant.
        var evolver = new TimeEvolver(DefaultChecker());
        double next = evolver.Advance(0, 1.0, 0.1, (t, y) => 0.0);
        Assert.Equal(1.0, next, precision: 12);
    }

    [Fact]
    public void Advance_ThrowsOnNegativeDt()
    {
        var evolver = new TimeEvolver(DefaultChecker());
        Assert.Throws<ArgumentOutOfRangeException>(
            () => evolver.Advance(0, 0, -0.1, (t, y) => 0.0));
    }

    [Fact]
    public void AdvanceAdaptive_ReturnsNewPotentialAndDt()
    {
        var evolver = new TimeEvolver(DefaultChecker());
        var (next, actualDt) = evolver.AdvanceAdaptive(0, 1.0, 0.1, (t, y) => 0.0);
        Assert.Equal(1.0, next, precision: 12);
        Assert.True(actualDt > 0);
    }

    [Fact]
    public void AdvanceAdaptive_AccumulatesConvergenceHistory()
    {
        var checker = DefaultChecker();
        var evolver = new TimeEvolver(checker);
        evolver.AdvanceAdaptive(0, -0.5, 0.01, (t, y) => -y * 0.01);
        Assert.NotEmpty(evolver.ConvergenceHistory);
    }

    [Fact]
    public void Advance_ThrowsOnNullOde()
    {
        var evolver = new TimeEvolver(DefaultChecker());
        Assert.Throws<ArgumentNullException>(
            () => evolver.Advance(0, 0, 0.1, null!));
    }
}

// ── SimulationEngine (ISimulationRunner) ─────────────────────────────────────

public class SimulationEngineTests
{
    private static readonly SimulationEngine Engine = new();

    // ── Run (synchronous) ─────────────────────────────────────────────────────

    [Fact]
    public void Run_ReturnsResult_WithCorrectStepCount()
    {
        var parameters = SimulationTestFixtures.DefaultParameters(durationSeconds: 3600, timeSteps: 50);
        var result = Engine.Run(parameters);

        // Integrate returns t₀…tN → 51 points for 50 steps.
        Assert.Equal(51, result.TimePoints.Count);
        Assert.Equal(51, result.MixedPotentials.Count);
        Assert.Equal(51, result.CorrosionRates.Count);
    }

    [Fact]
    public void Run_ThrowsOnNullParameters()
    {
        Assert.Throws<ArgumentNullException>(() => Engine.Run(null!));
    }

    [Fact]
    public void Run_FirstTimePoint_IsZero()
    {
        var result = Engine.Run(SimulationTestFixtures.DefaultParameters());
        Assert.Equal(0.0, result.TimePoints[0], precision: 10);
    }

    [Fact]
    public void Run_LastTimePoint_EqualsDuration()
    {
        var p      = SimulationTestFixtures.DefaultParameters(durationSeconds: 7200);
        var result = Engine.Run(p);
        Assert.Equal(7200.0, result.TimePoints[^1], precision: 6);
    }

    [Fact]
    public void Run_MixedPotential_LiesBetweenElectrodeStandardPotentials()
    {
        var p      = SimulationTestFixtures.DefaultParameters();
        var result = Engine.Run(p);

        double anodeE   = MaterialRegistry.Zinc.StandardPotential;    // -0.76 V
        double cathodeE = MaterialRegistry.Copper.StandardPotential;  // +0.34 V

        foreach (double e in result.MixedPotentials)
        {
            Assert.True(e >= anodeE,   $"Potential {e} is below anode OCP {anodeE}.");
            Assert.True(e <= cathodeE, $"Potential {e} is above cathode OCP {cathodeE}.");
        }
    }

    [Fact]
    public void Run_CorrosionRates_ArePositive()
    {
        var result = Engine.Run(SimulationTestFixtures.DefaultParameters());
        Assert.All(result.CorrosionRates, r => Assert.True(r >= 0.0));
    }

    [Fact]
    public void Run_AverageCorrosionRate_IsPositive()
    {
        var result = Engine.Run(SimulationTestFixtures.DefaultParameters());
        Assert.True(result.AverageCorrosionRate > 0.0);
    }

    // ── SimulationEngine implements ISimulationRunner ─────────────────────────

    [Fact]
    public void SimulationEngine_ImplementsISimulationRunner()
    {
        ISimulationRunner runner = new SimulationEngine();
        Assert.NotNull(runner);
    }

    // ── RunAsync (async with progress and cancellation) ───────────────────────

    [Fact]
    public async Task RunAsync_ReturnsResult_WithExpectedStepCount()
    {
        var parameters = SimulationTestFixtures.DefaultParameters(timeSteps: 50);
        var result = await Engine.RunAsync(parameters, progress: null, out _, CancellationToken.None);

        // RunAsync emits t₀ + 50 steps = 51 points.
        Assert.Equal(51, result.TimePoints.Count);
    }

    [Fact]
    public async Task RunAsync_ReportsProgress_ForEachStep()
    {
        var parameters = SimulationTestFixtures.DefaultParameters(timeSteps: 20);
        var reports    = new List<SimulationProgress>();
        var progress   = new Progress<SimulationProgress>(reports.Add);

        await Engine.RunAsync(parameters, progress, out _, CancellationToken.None);

        // Allow IProgress<T> callbacks to complete on the thread-pool.
        await Task.Delay(50);

        Assert.Equal(20, reports.Count);
        Assert.Equal(20, reports[^1].CurrentStep);
        Assert.Equal(20, reports[^1].TotalSteps);
    }

    [Fact]
    public async Task RunAsync_WhenCancelled_SetsCheckpoint()
    {
        var cts        = new CancellationTokenSource();
        var parameters = SimulationTestFixtures.DefaultParameters(timeSteps: 1000);
        var reports    = new List<SimulationProgress>();
        var progress   = new Progress<SimulationProgress>(p =>
        {
            reports.Add(p);
            if (p.CurrentStep >= 10) cts.Cancel();
        });

        var result = await Engine.RunAsync(
            parameters, progress, out SimulationState? checkpoint, cts.Token);

        // Allow callbacks to flush.
        await Task.Delay(50);

        Assert.NotNull(checkpoint);
        Assert.True(checkpoint!.CompletedSteps > 0);
        Assert.True(result.TimePoints.Count < parameters.TimeSteps + 1);
    }

    [Fact]
    public async Task RunAsync_WhenCancelled_CheckpointIsMergeable()
    {
        var cts        = new CancellationTokenSource();
        var parameters = SimulationTestFixtures.DefaultParameters(durationSeconds: 3600, timeSteps: 100);

        var firstResult = await Engine.RunAsync(
            parameters,
            new Progress<SimulationProgress>(p => { if (p.CurrentStep >= 30) cts.Cancel(); }),
            out SimulationState? checkpoint,
            cts.Token);

        await Task.Delay(50);

        Assert.NotNull(checkpoint);

        // Resume from the checkpoint.
        var resumed = await Engine.Resume(checkpoint!, parameters);

        // Combined data should cover more than the partial first run.
        Assert.True(resumed.TimePoints.Count > firstResult.TimePoints.Count);
    }

    // ── Resume ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resume_ThrowsOnNullCheckpoint()
    {
        var parameters = SimulationTestFixtures.DefaultParameters();
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Engine.Resume(null!, parameters));
    }

    [Fact]
    public async Task Resume_ThrowsOnNullParameters()
    {
        var state = new SimulationState();
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Engine.Resume(state, null!));
    }

    [Fact]
    public async Task Resume_FromFreshState_ProducesSameResultAsRun()
    {
        var parameters = SimulationTestFixtures.DefaultParameters(durationSeconds: 360, timeSteps: 10);

        // A fresh checkpoint at step 0 — Resume should produce the same result as Run.
        var state = new SimulationState
        {
            CompletedSteps   = 0,
            CurrentTime      = 0.0,
            CurrentPotential = (MaterialRegistry.Zinc.StandardPotential
                                + MaterialRegistry.Copper.StandardPotential) / 2.0,
        };

        var resumed = await Engine.Resume(state, parameters);
        var direct  = Engine.Run(parameters);

        Assert.Equal(direct.TimePoints.Count, resumed.TimePoints.Count);
        Assert.Equal(
            direct.MixedPotentials[^1],
            resumed.MixedPotentials[^1],
            precision: 6);
    }

    // ── Weather-driven run ────────────────────────────────────────────────────

    [Fact]
    public void Run_WithWeatherProvider_SucceedsAndHasPositiveCorrosionRates()
    {
        var weatherProvider = new SyntheticWeatherProvider(
            baseTempCelsius:      20.0,
            tempAmplitude:        5.0,
            baseRelativeHumidity: 0.7,
            humidityAmplitude:    0.1,
            chlorideConcentration: 0.05);

        var parameters = new SimulationParameters(
            new GalvanicPair(MaterialRegistry.Zinc, MaterialRegistry.Copper),
            new AtmosphericConditions(20.0, 0.7, 0.05),
            DurationSeconds:  1800,
            TimeSteps:        50,
            WeatherProvider:  weatherProvider);

        var result = Engine.Run(parameters);

        Assert.Equal(51, result.TimePoints.Count);
        Assert.All(result.CorrosionRates, r => Assert.True(r >= 0.0));
    }
}
