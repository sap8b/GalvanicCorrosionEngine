using GCE.Atmosphere;
using GCE.Core;
using GCE.Electrochemistry;
using GCE.Simulation;

namespace GCE.Simulation.Tests;

/// <summary>
/// Unit tests for <see cref="GeometryEvolver"/>.
/// </summary>
public class GeometryEvolverTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>Creates a 5×5 mesh: columns 0–2 Anode, columns 3–4 Cathode.</summary>
    private static GeometryMesh StandardMesh()
    {
        var regions = new NodePhase[5, 5];
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
                regions[i, j] = i <= 2 ? NodePhase.Anode : NodePhase.Cathode;

        return new GeometryMesh(
            XCoordinates: [0.00, 0.025, 0.050, 0.075, 0.100],
            YCoordinates: [0.00, 0.025, 0.050, 0.075, 0.100],
            Regions:      regions);
    }

    /// <summary>
    /// Builds a flat nodal corrosion rate array where every node has the same rate.
    /// </summary>
    private static double[] UniformRates(int nodeCount, double ratesMmPerYear) =>
        Enumerable.Repeat(ratesMmPerYear, nodeCount).ToArray();

    // ── Constructor validation ─────────────────────────────────────────────────

    [Fact]
    public void Constructor_Throws_WhenMaxNodesPerStepIsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeometryEvolver(0));
    }

    [Fact]
    public void Constructor_Throws_WhenMaxNodesPerStepIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeometryEvolver(-1));
    }

    [Fact]
    public void Constructor_DefaultsToOneNodePerStep()
    {
        var evolver = new GeometryEvolver();
        Assert.Equal(1, evolver.MaxNodesPerStep);
    }

    [Fact]
    public void TotalGeoSteps_StartsAtZero()
    {
        Assert.Equal(0, new GeometryEvolver().TotalGeoSteps);
    }

    // ── ComputeGeometricTimestep ───────────────────────────────────────────────

    [Fact]
    public void ComputeGeometricTimestep_ThrowsOnNullMesh()
    {
        var evolver = new GeometryEvolver();
        Assert.Throws<ArgumentNullException>(
            () => evolver.ComputeGeometricTimestep(
                null!, new double[4], new double[4], MaterialRegistry.Zinc));
    }

    [Fact]
    public void ComputeGeometricTimestep_ThrowsOnNullNodeMassLoss()
    {
        var evolver = new GeometryEvolver();
        Assert.Throws<ArgumentNullException>(
            () => evolver.ComputeGeometricTimestep(
                StandardMesh(), null!, new double[25], MaterialRegistry.Zinc));
    }

    [Fact]
    public void ComputeGeometricTimestep_ThrowsOnNullNodalCorrosionRates()
    {
        var evolver = new GeometryEvolver();
        Assert.Throws<ArgumentNullException>(
            () => evolver.ComputeGeometricTimestep(
                StandardMesh(), new double[25], null!, MaterialRegistry.Zinc));
    }

    [Fact]
    public void ComputeGeometricTimestep_ThrowsOnNullAnodeMaterial()
    {
        var evolver = new GeometryEvolver();
        Assert.Throws<ArgumentNullException>(
            () => evolver.ComputeGeometricTimestep(
                StandardMesh(), new double[25], new double[25], null!));
    }

    [Fact]
    public void ComputeGeometricTimestep_ReturnsMaxDt_WhenNoActiveAnodeNodes()
    {
        // All nodes are cathode — no dissolution possible.
        var regions = new NodePhase[3, 3];
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                regions[i, j] = NodePhase.Cathode;

        var mesh    = new GeometryMesh([0.0, 0.05, 0.1], [0.0, 0.05, 0.1], regions);
        var evolver = new GeometryEvolver();

        double dt = evolver.ComputeGeometricTimestep(
            mesh, new double[9], new double[9], MaterialRegistry.Zinc,
            minDt: 1.0, maxDt: 3600.0);

        Assert.Equal(3600.0, dt);
    }

    [Fact]
    public void ComputeGeometricTimestep_ReturnsMaxDt_WhenRatesAreZero()
    {
        var mesh    = StandardMesh();
        var evolver = new GeometryEvolver();

        // Zero corrosion rates → no dissolution → should return maxDt.
        double dt = evolver.ComputeGeometricTimestep(
            mesh, new double[25], UniformRates(25, 0.0), MaterialRegistry.Zinc,
            minDt: 1.0, maxDt: 7200.0);

        Assert.Equal(7200.0, dt);
    }

    [Fact]
    public void ComputeGeometricTimestep_IsClamped_ToMaxDt()
    {
        var mesh    = StandardMesh();
        var evolver = new GeometryEvolver();

        // Very slow corrosion rate → huge dissolution time → must be clamped to maxDt.
        double dt = evolver.ComputeGeometricTimestep(
            mesh, new double[25], UniformRates(25, 1e-6), MaterialRegistry.Zinc,
            minDt: 1.0, maxDt: 86_400.0);

        Assert.Equal(86_400.0, dt);
    }

    [Fact]
    public void ComputeGeometricTimestep_IsClamped_ToMinDt()
    {
        var mesh    = StandardMesh();
        var evolver = new GeometryEvolver();

        // Very fast corrosion rate → tiny dissolution time → must be clamped to minDt.
        double dt = evolver.ComputeGeometricTimestep(
            mesh, new double[25], UniformRates(25, 1e12), MaterialRegistry.Zinc,
            minDt: 5.0, maxDt: 86_400.0);

        Assert.Equal(5.0, dt);
    }

    [Fact]
    public void ComputeGeometricTimestep_IsPositive_ForTypicalInput()
    {
        var mesh    = StandardMesh();
        var evolver = new GeometryEvolver();

        double dt = evolver.ComputeGeometricTimestep(
            mesh, new double[25], UniformRates(25, 0.1), MaterialRegistry.Zinc,
            minDt: 1.0, maxDt: 86_400.0);

        Assert.True(dt > 0.0, $"Expected positive dt, got {dt}.");
    }

    [Fact]
    public void ComputeGeometricTimestep_DoesNotCountAlreadyDissolved()
    {
        // A mesh where all anode nodes have already exceeded their threshold should
        // be treated as having no active nodes → return maxDt.
        var mesh      = StandardMesh();
        var massLoss  = new double[25];
        int ny        = mesh.NodesY;

        // Zinc threshold per node: ρ × dx × dy = 7133 × 0.025 × 0.025 ≈ 4.46 kg/m.
        double dx        = 0.025;
        double dy        = 0.025;
        double threshold = MaterialRegistry.Zinc.Density * dx * dy;

        for (int i = 0; i <= 2; i++)
            for (int j = 0; j < 5; j++)
                massLoss[i * ny + j] = threshold + 1.0; // already dissolved

        var evolver = new GeometryEvolver();
        double dt   = evolver.ComputeGeometricTimestep(
            mesh, massLoss, UniformRates(25, 1.0), MaterialRegistry.Zinc,
            minDt: 1.0, maxDt: 3600.0);

        Assert.Equal(3600.0, dt);
    }

    [Fact]
    public void ComputeGeometricTimestep_WithMaxNodesPerStep2_ReturnsSecondSmallestTime()
    {
        // 3×1 mesh: two anode columns with different corrosion rates.
        // Column 0: faster rate (shorter remaining time).
        // Column 1: slower rate (longer remaining time).
        // MaxNodesPerStep = 2 should return the 2nd-shortest time (column 1's).
        var regions = new NodePhase[3, 1];
        regions[0, 0] = NodePhase.Anode;
        regions[1, 0] = NodePhase.Anode;
        regions[2, 0] = NodePhase.Cathode;
        var mesh = new GeometryMesh([0.0, 0.05, 0.1], [0.0], regions);

        var    rates    = new double[3];
        rates[0]        = 10.0;  // fast — dissolves first
        rates[1]        = 1.0;   // slow — dissolves second
        rates[2]        = 0.0;   // cathode, ignored

        var evolver1 = new GeometryEvolver(maxNodesPerStep: 1);
        var evolver2 = new GeometryEvolver(maxNodesPerStep: 2);

        double dt1 = evolver1.ComputeGeometricTimestep(
            mesh, new double[3], rates, MaterialRegistry.Zinc, minDt: 1.0, maxDt: 1e10);
        double dt2 = evolver2.ComputeGeometricTimestep(
            mesh, new double[3], rates, MaterialRegistry.Zinc, minDt: 1.0, maxDt: 1e10);

        // dt1 is the time for the fastest node; dt2 should be 10× longer.
        Assert.True(dt2 > dt1,
            $"Expected dt2 ({dt2}) > dt1 ({dt1}) for maxNodesPerStep=2.");
    }

    // ── Advance ────────────────────────────────────────────────────────────────

    [Fact]
    public void Advance_ThrowsOnNullMesh()
    {
        var evolver = new GeometryEvolver();
        Assert.Throws<ArgumentNullException>(
            () => evolver.Advance(null!, new double[4], new double[4], 1.0, MaterialRegistry.Zinc));
    }

    [Fact]
    public void Advance_ThrowsOnNullNodeMassLoss()
    {
        var evolver = new GeometryEvolver();
        Assert.Throws<ArgumentNullException>(
            () => evolver.Advance(StandardMesh(), null!, new double[25], 1.0, MaterialRegistry.Zinc));
    }

    [Fact]
    public void Advance_ThrowsOnNullNodalCorrosionRates()
    {
        var evolver = new GeometryEvolver();
        Assert.Throws<ArgumentNullException>(
            () => evolver.Advance(StandardMesh(), new double[25], null!, 1.0, MaterialRegistry.Zinc));
    }

    [Fact]
    public void Advance_ThrowsOnNullAnodeMaterial()
    {
        var evolver = new GeometryEvolver();
        Assert.Throws<ArgumentNullException>(
            () => evolver.Advance(StandardMesh(), new double[25], new double[25], 1.0, null!));
    }

    [Fact]
    public void Advance_ThrowsOnNonPositiveDt()
    {
        var evolver = new GeometryEvolver();
        Assert.Throws<ArgumentOutOfRangeException>(
            () => evolver.Advance(StandardMesh(), new double[25], new double[25], 0.0, MaterialRegistry.Zinc));
    }

    [Fact]
    public void Advance_IncrementsTotalGeoSteps()
    {
        var evolver = new GeometryEvolver();
        evolver.Advance(StandardMesh(), new double[25], new double[25], 1.0, MaterialRegistry.Zinc);
        Assert.Equal(1, evolver.TotalGeoSteps);

        evolver.Advance(StandardMesh(), new double[25], new double[25], 1.0, MaterialRegistry.Zinc);
        Assert.Equal(2, evolver.TotalGeoSteps);
    }

    [Fact]
    public void Advance_AccumulatesMassForAnodeNodes()
    {
        var    mesh     = StandardMesh();
        var    massLoss = new double[25];
        var    rates    = UniformRates(25, 1.0); // 1 mm/year
        var    evolver  = new GeometryEvolver();

        evolver.Advance(mesh, massLoss, rates, 3600.0, MaterialRegistry.Zinc);

        // At least some anode nodes should have accumulated positive mass.
        bool anyPositive = false;
        int  ny          = mesh.NodesY;
        for (int i = 0; i <= 2; i++)
            for (int j = 0; j < 5; j++)
                if (massLoss[i * ny + j] > 0.0)
                    anyPositive = true;

        Assert.True(anyPositive, "Expected positive mass loss in at least one anode node.");
    }

    [Fact]
    public void Advance_DoesNotAccumulateMassForCathodeNodes()
    {
        var mesh     = StandardMesh();
        var massLoss = new double[25];
        var evolver  = new GeometryEvolver();

        evolver.Advance(mesh, massLoss, UniformRates(25, 1.0), 3600.0, MaterialRegistry.Zinc);

        int ny = mesh.NodesY;
        for (int i = 3; i <= 4; i++)
            for (int j = 0; j < 5; j++)
                Assert.Equal(0.0, massLoss[i * ny + j]);
    }

    [Fact]
    public void Advance_ReturnsZero_WhenNoNodesDissolved()
    {
        var evolver = new GeometryEvolver();
        int count   = evolver.Advance(
            StandardMesh(), new double[25], UniformRates(25, 1e-12), 1.0, MaterialRegistry.Zinc);
        Assert.Equal(0, count);
    }

    [Fact]
    public void Advance_ReturnsPositiveCount_WhenNodesDissolved()
    {
        // TinyAnodeMesh-style setup: tiny mesh, huge corrosion rate, one step.
        var regions = new NodePhase[3, 1];
        regions[0, 0] = NodePhase.Anode;
        regions[1, 0] = NodePhase.Anode;
        regions[2, 0] = NodePhase.Cathode;
        var mesh    = new GeometryMesh([0.0, 5e-6, 1e-5], [0.0], regions);
        var massLoss = new double[3];

        // Corrosion rate high enough to exceed threshold in one step.
        // threshold = ρ × dx × dy = 7133 × 5e-6 × 1 (dy = 1 for single row) ≈ 0.0357 kg/m
        // But dy here is from YCoordinates: only one Y node, so dy falls back to 1.0.
        // Let's use a known-dissolving rate: from SecondsPerYear = 3.156e7,
        //   deltaMass = rate / (3.156e7 × 1000) × density × aNode × dt
        // For rate = 1e10 mm/yr, density = 7133, aNode = 5e-6 × 1.0, dt = 1:
        //   deltaMass ≈ 1e10 / 3.156e10 × 7133 × 5e-6 × 1 ≈ 0.317 × 7133 × 5e-6 ≈ 1.13e-2
        // threshold = 7133 × 5e-6 = 0.0357  → dissolves (0.0113 < 0.0357) — not enough.
        // Use dt = 10 and rate = 1e12 to ensure dissolution:
        //   deltaMass ≈ 1e12 / 3.156e10 × 7133 × 5e-6 × 10 ≈ 31.7 × 7133 × 5e-5 ≈ 11.3 >> threshold.
        var rates = new double[] { 1e12, 1e12, 0.0 };

        var evolver = new GeometryEvolver();
        int dissolved = evolver.Advance(mesh, massLoss, rates, 10.0, MaterialRegistry.Zinc);

        Assert.True(dissolved > 0, $"Expected at least one dissolved node, got {dissolved}.");
    }

    [Fact]
    public void Advance_TransitionsDissolvedNodes_ToElectrolyte()
    {
        // Same setup as above.
        var regions = new NodePhase[3, 1];
        regions[0, 0] = NodePhase.Anode;
        regions[1, 0] = NodePhase.Anode;
        regions[2, 0] = NodePhase.Cathode;
        var mesh    = new GeometryMesh([0.0, 5e-6, 1e-5], [0.0], regions);
        var massLoss = new double[3];
        var rates    = new double[] { 1e12, 1e12, 0.0 };

        var evolver = new GeometryEvolver();
        evolver.Advance(mesh, massLoss, rates, 10.0, MaterialRegistry.Zinc);

        Assert.Equal(NodePhase.Electrolyte, mesh.Regions[0, 0]);
        Assert.Equal(NodePhase.Electrolyte, mesh.Regions[1, 0]);
        Assert.Equal(NodePhase.Cathode,     mesh.Regions[2, 0]); // unchanged
    }

    // ── Operator-splitting integration via SimulationEngine ───────────────────

    [Fact]
    public void SimulationEngine_OperatorSplitting_GeoStepCountIsPositive_WhenMeshProvided()
    {
        var regions = new NodePhase[5, 5];
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
                regions[i, j] = i <= 2 ? NodePhase.Anode : NodePhase.Cathode;

        var mesh = new GeometryMesh(
            XCoordinates: [0.00, 0.025, 0.050, 0.075, 0.100],
            YCoordinates: [0.00, 0.025, 0.050, 0.075, 0.100],
            Regions: regions);

        var parameters = new SimulationParameters(
            new GalvanicPair(MaterialRegistry.Zinc, MaterialRegistry.Copper),
            new AtmosphericConditions(25.0, 0.75, 0.1),
            DurationSeconds: 360.0,
            TimeSteps: 5,
            Mesh: mesh,
            UseOperatorSplitting: true,
            MaxNodesPerGeoStep: 1);

        var result = new SimulationEngine().Run(parameters);

        Assert.True(result.GeoStepCount > 0,
            $"Expected GeoStepCount > 0 with operator splitting, got {result.GeoStepCount}.");
    }

    [Fact]
    public void SimulationEngine_GeoStepCountIsZero_WithoutOperatorSplitting()
    {
        var regions = new NodePhase[5, 5];
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
                regions[i, j] = i <= 2 ? NodePhase.Anode : NodePhase.Cathode;

        var mesh = new GeometryMesh(
            XCoordinates: [0.00, 0.025, 0.050, 0.075, 0.100],
            YCoordinates: [0.00, 0.025, 0.050, 0.075, 0.100],
            Regions: regions);

        var parameters = new SimulationParameters(
            new GalvanicPair(MaterialRegistry.Zinc, MaterialRegistry.Copper),
            new AtmosphericConditions(25.0, 0.75, 0.1),
            DurationSeconds: 360.0,
            TimeSteps: 5,
            Mesh: mesh,
            UseOperatorSplitting: false);

        var result = new SimulationEngine().Run(parameters);

        Assert.Equal(0, result.GeoStepCount);
    }

    [Fact]
    public void SimulationEngine_OperatorSplitting_ProducesNodeMassLoss()
    {
        var regions = new NodePhase[5, 5];
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
                regions[i, j] = i <= 2 ? NodePhase.Anode : NodePhase.Cathode;

        var mesh = new GeometryMesh(
            XCoordinates: [0.00, 0.025, 0.050, 0.075, 0.100],
            YCoordinates: [0.00, 0.025, 0.050, 0.075, 0.100],
            Regions: regions);

        var parameters = new SimulationParameters(
            new GalvanicPair(MaterialRegistry.Zinc, MaterialRegistry.Copper),
            new AtmosphericConditions(25.0, 0.75, 0.1),
            DurationSeconds: 360.0,
            TimeSteps: 5,
            Mesh: mesh,
            UseOperatorSplitting: true);

        var result = new SimulationEngine().Run(parameters);

        Assert.NotNull(result.NodeMassLoss);
        Assert.All(result.NodeMassLoss!, m => Assert.True(m >= 0.0));
    }

    [Fact]
    public void SimulationEngine_OperatorSplitting_WithMaxNodesPerGeoStep2_ProducesLargerTimesteps()
    {
        // With MaxNodesPerGeoStep=2, the geometric timestep can be larger than with 1,
        // so fewer outer steps should cover the same simulation duration.
        var makeParams = (int maxNodes) =>
        {
            var regions = new NodePhase[5, 5];
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                    regions[i, j] = i <= 2 ? NodePhase.Anode : NodePhase.Cathode;

            var mesh = new GeometryMesh(
                XCoordinates: [0.00, 0.025, 0.050, 0.075, 0.100],
                YCoordinates: [0.00, 0.025, 0.050, 0.075, 0.100],
                Regions: regions);

            return new SimulationParameters(
                new GalvanicPair(MaterialRegistry.Zinc, MaterialRegistry.Copper),
                new AtmosphericConditions(25.0, 0.75, 0.1),
                DurationSeconds: 360.0,
                TimeSteps: 10,
                Mesh: mesh,
                UseOperatorSplitting: true,
                MaxNodesPerGeoStep: maxNodes);
        };

        var result1 = new SimulationEngine().Run(makeParams(1));
        var result2 = new SimulationEngine().Run(makeParams(2));

        // With MaxNodesPerGeoStep=2, the dt_geo step is at least as large so
        // GeoStepCount should be ≤ that of MaxNodesPerGeoStep=1.
        Assert.True(result2.GeoStepCount <= result1.GeoStepCount,
            $"Expected GeoStepCount(2)={result2.GeoStepCount} ≤ GeoStepCount(1)={result1.GeoStepCount}.");
    }
}
