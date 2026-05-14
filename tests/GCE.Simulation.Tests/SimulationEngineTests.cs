using GCE.Atmosphere;
using GCE.Core;
using GCE.Electrochemistry;
using GCE.Numerics.Solvers;
using GCE.Simulation;
using System.Linq;

namespace GCE.Simulation.Tests;

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

    // ── Adaptive time-stepping ────────────────────────────────────────────────

    [Fact]
    public void Run_WithAdaptiveTimeStep_ReturnsCorrectStepCount()
    {
        var parameters = SimulationTestFixtures.DefaultParameters(
            durationSeconds: 3600, timeSteps: 50) with { UseAdaptiveTimeStep = true };

        var result = Engine.Run(parameters);

        Assert.Equal(51, result.TimePoints.Count);
        Assert.Equal(51, result.MixedPotentials.Count);
        Assert.Equal(51, result.CorrosionRates.Count);
    }

    [Fact]
    public void Run_WithAdaptiveTimeStep_ConvergenceHistoryIsPopulated()
    {
        var parameters = SimulationTestFixtures.DefaultParameters(
            durationSeconds: 360, timeSteps: 10) with { UseAdaptiveTimeStep = true };

        var result = Engine.Run(parameters);

        Assert.NotEmpty(result.ConvergenceHistory);
    }

    [Fact]
    public void Run_WithFixedTimeStep_ConvergenceHistoryIsEmpty()
    {
        var parameters = SimulationTestFixtures.DefaultParameters(
            durationSeconds: 360, timeSteps: 10);   // UseAdaptiveTimeStep defaults to false

        var result = Engine.Run(parameters);

        Assert.Empty(result.ConvergenceHistory);
    }

    [Fact]
    public void Run_WithAdaptiveTimeStep_CorrosionRates_ArePositive()
    {
        var parameters = SimulationTestFixtures.DefaultParameters() with
        {
            UseAdaptiveTimeStep = true,
        };

        var result = Engine.Run(parameters);

        Assert.All(result.CorrosionRates, r => Assert.True(r >= 0.0));
    }

    [Fact]
    public void Run_WithAdaptiveTimeStep_MixedPotential_LiesBetweenElectrodeStandardPotentials()
    {
        var parameters = SimulationTestFixtures.DefaultParameters() with
        {
            UseAdaptiveTimeStep = true,
        };

        var result = Engine.Run(parameters);

        double anodeE   = MaterialRegistry.Zinc.StandardPotential;
        double cathodeE = MaterialRegistry.Copper.StandardPotential;

        foreach (double e in result.MixedPotentials)
        {
            Assert.True(e >= anodeE,   $"Potential {e} is below anode OCP {anodeE}.");
            Assert.True(e <= cathodeE, $"Potential {e} is above cathode OCP {cathodeE}.");
        }
    }

    [Fact]
    public async Task RunAsync_WithAdaptiveTimeStep_ReturnsResultAndPopulatesConvergenceHistory()
    {
        var parameters = SimulationTestFixtures.DefaultParameters(
            durationSeconds: 360, timeSteps: 10) with { UseAdaptiveTimeStep = true };

        var result = await Engine.RunAsync(parameters, progress: null, out _, CancellationToken.None);

        Assert.Equal(11, result.TimePoints.Count);
        Assert.NotEmpty(result.ConvergenceHistory);
    }
}

// ── OhmicDropTests ────────────────────────────────────────────────────────────

public class OhmicDropTests
{
    private static readonly SimulationEngine Engine = new();

    [Fact]
    public void Run_WithZeroPathLength_ProducesSameStepCountAsBaseline()
    {
        var baseline = SimulationTestFixtures.DefaultParameters(durationSeconds: 360, timeSteps: 10);
        var withOhmic = baseline with { PathLength = 0.0 };

        var r1 = Engine.Run(baseline);
        var r2 = Engine.Run(withOhmic);

        Assert.Equal(r1.TimePoints.Count, r2.TimePoints.Count);
    }

    [Fact]
    public void Run_WithNonZeroPathLength_SucceedsAndHasCorrectStepCount()
    {
        var parameters = SimulationTestFixtures.DefaultParameters(
            durationSeconds: 360, timeSteps: 10) with { PathLength = 0.005 };

        var result = Engine.Run(parameters);

        // 10 steps + initial point
        Assert.Equal(11, result.TimePoints.Count);
        Assert.Equal(11, result.MixedPotentials.Count);
        Assert.Equal(11, result.CorrosionRates.Count);
    }

    [Fact]
    public void Run_WithPathLength_CorrosionRatesAreNonNegative()
    {
        var parameters = SimulationTestFixtures.DefaultParameters(
            durationSeconds: 3600, timeSteps: 50) with { PathLength = 0.010 };

        var result = Engine.Run(parameters);

        Assert.All(result.CorrosionRates, r => Assert.True(r >= 0.0));
    }

    [Fact]
    public void Run_WithPathLength_MixedPotentialLiesBetweenStandardPotentials()
    {
        var parameters = SimulationTestFixtures.DefaultParameters(
            durationSeconds: 1800, timeSteps: 30) with { PathLength = 0.010 };

        var result = Engine.Run(parameters);

        double anodeE   = MaterialRegistry.Zinc.StandardPotential;
        double cathodeE = MaterialRegistry.Copper.StandardPotential;

        foreach (double e in result.MixedPotentials)
        {
            Assert.True(e >= anodeE,   $"Potential {e:G4} is below anode OCP.");
            Assert.True(e <= cathodeE, $"Potential {e:G4} is above cathode OCP.");
        }
    }

    [Fact]
    public void Run_HighPathLength_ReducesAverageCorrosionRateVsLowPathLength()
    {
        // A longer electrolyte path adds more ohmic resistance, which should
        // reduce the net driving potential and therefore the corrosion rate.
        var low  = SimulationTestFixtures.DefaultParameters(durationSeconds: 3600, timeSteps: 50)
                        with { PathLength = 1e-6 };
        var high = SimulationTestFixtures.DefaultParameters(durationSeconds: 3600, timeSteps: 50)
                        with { PathLength = 1.0 };

        double avgLow  = Engine.Run(low).AverageCorrosionRate;
        double avgHigh = Engine.Run(high).AverageCorrosionRate;

        Assert.True(avgHigh <= avgLow,
            $"High-resistance run ({avgHigh:G4}) should not exceed low-resistance run ({avgLow:G4}).");
    }
}

// ── SpatialDistributionTests ──────────────────────────────────────────────────

public class SpatialDistributionTests
{
    private static readonly SimulationEngine Engine = new();

    private static GeometryMesh TinyMesh()
    {
        var regions = new NodePhase[5, 5];
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
                regions[i, j] = i < 3 ? NodePhase.Anode : NodePhase.Cathode; // left = anode, right = cathode
        return new GeometryMesh(
            XCoordinates: [0.0, 0.025, 0.050, 0.075, 0.100],
            YCoordinates: [0.0, 0.025, 0.050, 0.075, 0.100],
            Regions: regions);
    }

    [Fact]
    public void Run_WithMesh_PopulatesNodalPotentials()
    {
        var parameters = SimulationTestFixtures.DefaultParameters(
            durationSeconds: 360, timeSteps: 10) with { Mesh = TinyMesh() };

        var result = Engine.Run(parameters);

        Assert.NotNull(result.NodalPotentials);
        Assert.Equal(25, result.NodalPotentials!.Length); // 5×5 = 25 nodes
    }

    [Fact]
    public void Run_WithMesh_PopulatesNodalCorrosionRates()
    {
        var parameters = SimulationTestFixtures.DefaultParameters(
            durationSeconds: 360, timeSteps: 10) with { Mesh = TinyMesh() };

        var result = Engine.Run(parameters);

        Assert.NotNull(result.NodalCorrosionRates);
        Assert.Equal(25, result.NodalCorrosionRates!.Length);
    }

    [Fact]
    public void Run_WithMesh_NodalCorrosionRatesAreNonNegative()
    {
        var parameters = SimulationTestFixtures.DefaultParameters(
            durationSeconds: 360, timeSteps: 10) with { Mesh = TinyMesh() };

        var result = Engine.Run(parameters);

        Assert.All(result.NodalCorrosionRates!, r => Assert.True(r >= 0.0));
    }

    [Fact]
    public void Run_WithMesh_NodalPotentialsContainFiniteValues()
    {
        var parameters = SimulationTestFixtures.DefaultParameters(
            durationSeconds: 360, timeSteps: 10) with { Mesh = TinyMesh() };

        var result = Engine.Run(parameters);

        Assert.All(result.NodalPotentials!, v => Assert.True(double.IsFinite(v)));
    }

    [Fact]
    public void Run_WithoutMesh_NodalPropertiesAreNull()
    {
        var parameters = SimulationTestFixtures.DefaultParameters();

        var result = Engine.Run(parameters);

        Assert.Null(result.NodalPotentials);
        Assert.Null(result.NodalCorrosionRates);
    }
}

// ── SpeciesTransportSimulationTests ──────────────────────────────────────────

public class SpeciesTransportSimulationTests
{
    private static readonly SimulationEngine Engine = new();

    private static SpeciesTransport CreateChlorideTransport()
    {
        var cl = new Species("Cl-", -1, diffusionCoefficient: 2.03e-9, concentration: 100.0);
        var leftBC  = new DirichletBC(100.0);
        var rightBC = new DirichletBC(100.0);
        return new SpeciesTransport(cl, domainLength: 1e-4, gridPoints: 5,
            initialProfile: [100.0, 100.0, 100.0, 100.0, 100.0],
            leftBC: leftBC, rightBC: rightBC, timeStep: 1.0);
    }

    private static GeometryMesh AnodeElectrolyteMesh()
    {
        var regions = new NodePhase[5, 5];
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                regions[i, j] = i switch
                {
                    <= 1 => NodePhase.Anode,
                    >= 4 => NodePhase.Cathode,
                    _ => NodePhase.Electrolyte,
                };
            }
        }

        return new GeometryMesh(
            XCoordinates: [0.0, 0.025, 0.050, 0.075, 0.100],
            YCoordinates: [0.0, 0.025, 0.050, 0.075, 0.100],
            Regions: regions);
    }

    private static SpeciesTransport CreateZincIonTransportForMesh()
    {
        var zn2 = new Species("Zn2+", charge: 2, diffusionCoefficient: 0.72e-9, concentration: 1.0);
        var leftBC = new DirichletBC(1.0);
        var rightBC = new DirichletBC(1.0);
        return new SpeciesTransport(zn2, domainLength: 1e-4, gridPoints: 25,
            initialProfile: Enumerable.Repeat(1.0, 25).ToArray(),
            leftBC: leftBC, rightBC: rightBC, timeStep: 1.0);
    }

    [Fact]
    public void Run_WithTrackedSpecies_PopulatesSpeciesConcentrationHistory()
    {
        var st = CreateChlorideTransport();
        var parameters = SimulationTestFixtures.DefaultParameters(
            durationSeconds: 360, timeSteps: 10)
            with { TrackedSpecies = [st] };

        var result = Engine.Run(parameters);

        Assert.NotNull(result.SpeciesConcentrationHistory);
        Assert.True(result.SpeciesConcentrationHistory!.ContainsKey("Cl-"));
    }

    [Fact]
    public void Run_WithTrackedSpecies_HistoryHasCorrectEntryCount()
    {
        var st = CreateChlorideTransport();
        var parameters = SimulationTestFixtures.DefaultParameters(
            durationSeconds: 360, timeSteps: 10)
            with { TrackedSpecies = [st] };

        var result = Engine.Run(parameters);

        // Initial point + 10 steps = 11 entries.
        Assert.Equal(11, result.SpeciesConcentrationHistory!["Cl-"].Count);
    }

    [Fact]
    public void Run_WithTrackedSpecies_ConcentrationsAreNonNegative()
    {
        var st = CreateChlorideTransport();
        var parameters = SimulationTestFixtures.DefaultParameters(
            durationSeconds: 360, timeSteps: 10)
            with { TrackedSpecies = [st] };

        var result = Engine.Run(parameters);

        Assert.All(result.SpeciesConcentrationHistory!["Cl-"], c => Assert.True(c >= 0.0));
    }

    [Fact]
    public void Run_WithoutTrackedSpecies_SpeciesConcentrationHistoryIsNull()
    {
        var result = Engine.Run(SimulationTestFixtures.DefaultParameters());

        Assert.Null(result.SpeciesConcentrationHistory);
    }

    [Fact]
    public void Run_WithTrackedMetalIonAndCorrosionProductMaterial_DepositsCorrosionProductNodes()
    {
        var mesh = AnodeElectrolyteMesh();
        var znTransport = CreateZincIonTransportForMesh();

        var parameters = SimulationTestFixtures.DefaultParameters(
            durationSeconds: 60, timeSteps: 5)
            with
            {
                Mesh = mesh,
                TrackedSpecies = [znTransport],
                CorrosionProductMaterial = CorrosionProductBehavior.ZincOxide,
            };

        var result = Engine.Run(parameters);

        int corrosionProductCount = 0;
        for (int i = 0; i < mesh.NodesX; i++)
            for (int j = 0; j < mesh.NodesY; j++)
                if (mesh.Regions[i, j] == NodePhase.CorrosionProduct)
                    corrosionProductCount++;

        Assert.True(corrosionProductCount > 0);
        Assert.NotNull(result.SpeciesConcentrationHistory);
        Assert.True(result.SpeciesConcentrationHistory!.ContainsKey("Zn2+"));
        Assert.All(result.SpeciesConcentrationHistory["Zn2+"], c => Assert.True(c >= 0.0));
    }
}

// ── pHTrackingTests ───────────────────────────────────────────────────────────

public class pHTrackingTests
{
    private static readonly SimulationEngine Engine = new();

    [Fact]
    public void Run_WithTrackpH_PopulatespHHistory()
    {
        var parameters = SimulationTestFixtures.DefaultParameters(
            durationSeconds: 360, timeSteps: 10) with { TrackpH = true };

        var result = Engine.Run(parameters);

        Assert.NotNull(result.pHHistory);
    }

    [Fact]
    public void Run_WithTrackpH_HistoryHasCorrectEntryCount()
    {
        int steps = 10;
        var parameters = SimulationTestFixtures.DefaultParameters(
            durationSeconds: 360, timeSteps: steps) with { TrackpH = true };

        var result = Engine.Run(parameters);

        // Initial pH + one per step = steps + 1 total
        Assert.Equal(steps + 1, result.pHHistory!.Count);
    }

    [Fact]
    public void Run_WithTrackpH_AllValuesAreFinite()
    {
        var parameters = SimulationTestFixtures.DefaultParameters(
            durationSeconds: 360, timeSteps: 10) with { TrackpH = true };

        var result = Engine.Run(parameters);

        Assert.All(result.pHHistory!, v => Assert.True(double.IsFinite(v)));
    }

    [Fact]
    public void Run_WithoutTrackpH_pHHistoryIsNull()
    {
        var result = Engine.Run(SimulationTestFixtures.DefaultParameters());

        Assert.Null(result.pHHistory);
    }

    [Fact]
    public void Run_WithTrackpH_InitialValueMatchesEnvironmentpH()
    {
        var env = new AtmosphericConditions(25.0, 0.75, 0.1);
        var parameters = SimulationTestFixtures.DefaultParameters(
            durationSeconds: 360, timeSteps: 10) with { TrackpH = true };

        var result = Engine.Run(parameters);

        // First entry should be the initial pH of the environment.
        Assert.Equal(env.pH, result.pHHistory![0], precision: 6);
    }
}
