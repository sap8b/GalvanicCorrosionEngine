using GCE.Atmosphere;
using GCE.Core;
using GCE.Electrochemistry;
using GCE.Simulation;
using GCE.Simulation.Geometry;

namespace GCE.Simulation.Tests;

/// <summary>
/// Tests for the spatial potential solver exercised through
/// <see cref="SimulationEngine"/> with a <see cref="GeometryMesh"/>.
/// </summary>
/// <remarks>
/// <see cref="SpatialSolver"/> is an <c>internal</c> helper; its behaviour is
/// verified indirectly via the public <see cref="SimulationEngine"/> API.
/// </remarks>
public class SpatialSolverTests
{
    private static readonly SimulationEngine Engine = new();

    // 5×5 mesh: nodes 0–2 in x are anode, nodes 3–4 are cathode.
    private static GeometryMesh FiveFiveMesh()
    {
        var regions = new NodePhase[5, 5];
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
                regions[i, j] = i <= 2 ? NodePhase.Anode : NodePhase.Cathode;

        return new GeometryMesh(
            XCoordinates: [0.00, 0.025, 0.050, 0.075, 0.100],
            YCoordinates: [0.00, 0.025, 0.050, 0.075, 0.100],
            Regions: regions);
    }

    private static GeometryMesh PhaseLayeredMesh()
    {
        var regions = new NodePhase[5, 5];
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
                regions[i, j] = j < 3 ? NodePhase.Electrolyte : NodePhase.CorrosionProduct;
        }

        return new GeometryMesh(
            XCoordinates: [0.00, 0.025, 0.050, 0.075, 0.100],
            YCoordinates: [0.00, 0.025, 0.050, 0.075, 0.100],
            Regions: regions);
    }

    private static GeometryMesh DynamicInterfaceMesh()
    {
        var regions = new NodePhase[5, 5];
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                regions[i, j] = i switch
                {
                    <= 1 => NodePhase.Anode,
                    2    => NodePhase.Electrolyte,
                    _    => NodePhase.Cathode,
                };
            }
        }

        return new GeometryMesh(
            XCoordinates: [0.00, 0.025, 0.050, 0.075, 0.100],
            YCoordinates: [0.00, 0.025, 0.050, 0.075, 0.100],
            Regions: regions);
    }

    private static SimulationParameters MakeParams(
        GeometryMesh mesh,
        ICorrosionProductMaterial? corrosionProductMaterial = null) =>
        new SimulationParameters(
            new GalvanicPair(MaterialRegistry.Zinc, MaterialRegistry.Copper),
            new AtmosphericConditions(25.0, 0.75, 0.1),
            DurationSeconds: 360,
            TimeSteps: 10,
            Mesh: mesh,
            CorrosionProductMaterial: corrosionProductMaterial);

    [Fact]
    public void Run_WithMesh_NodalPotentialsLengthMatchesMeshNodeCount()
    {
        var mesh = FiveFiveMesh();
        var result = Engine.Run(MakeParams(mesh));

        Assert.NotNull(result.NodalPotentials);
        Assert.Equal(mesh.NodesX * mesh.NodesY, result.NodalPotentials!.Length);
    }

    [Fact]
    public void Run_WithMesh_NodalCorrosionRatesLengthMatchesMeshNodeCount()
    {
        var mesh = FiveFiveMesh();
        var result = Engine.Run(MakeParams(mesh));

        Assert.NotNull(result.NodalCorrosionRates);
        Assert.Equal(mesh.NodesX * mesh.NodesY, result.NodalCorrosionRates!.Length);
    }

    [Fact]
    public void Run_WithMesh_NodalPotentialsAreFinite()
    {
        var result = Engine.Run(MakeParams(FiveFiveMesh()));

        Assert.All(result.NodalPotentials!, v => Assert.True(double.IsFinite(v)));
    }

    [Fact]
    public void Run_WithMesh_NodalCorrosionRatesAreNonNegative()
    {
        var result = Engine.Run(MakeParams(FiveFiveMesh()));

        Assert.All(result.NodalCorrosionRates!, r => Assert.True(r >= 0.0, $"Rate {r} is negative."));
    }

    [Fact]
    public void Run_WithGeometryBuilder_MeshProducesNodalResults()
    {
        // Build a mesh from SideBySideGeometry and pass it to the engine.
        var g = new SideBySideGeometry(
            MaterialRegistry.Zinc, MaterialRegistry.Copper,
            anodeWidth: 0.020, cathodeWidth: 0.020, length: 0.050);
        var mesh = g.BuildMesh(6, 4);

        var parameters = new SimulationParameters(
            new GalvanicPair(MaterialRegistry.Zinc, MaterialRegistry.Copper),
            new AtmosphericConditions(25.0, 0.75, 0.1),
            DurationSeconds: 360,
            TimeSteps: 5,
            Mesh: mesh);

        var result = Engine.Run(parameters);

        Assert.NotNull(result.NodalPotentials);
        Assert.Equal(mesh.NodesX * mesh.NodesY, result.NodalPotentials!.Length);
    }

    [Fact]
    public void Run_WithoutMesh_NodalResultsAreNull()
    {
        var parameters = new SimulationParameters(
            new GalvanicPair(MaterialRegistry.Zinc, MaterialRegistry.Copper),
            new AtmosphericConditions(25.0, 0.75, 0.1),
            DurationSeconds: 360,
            TimeSteps: 5);

        var result = Engine.Run(parameters);

        Assert.Null(result.NodalPotentials);
        Assert.Null(result.NodalCorrosionRates);
    }

    [Fact]
    public void Run_LargerMesh_HasExpectedNodeCount()
    {
        var g = new BoltInPlateGeometry(
            MaterialRegistry.Zinc, MaterialRegistry.Copper,
            boltRadius: 0.005, plateThickness: 0.010, plateWidth: 0.050);
        var mesh = g.BuildMesh(10, 10);

        var parameters = new SimulationParameters(
            new GalvanicPair(MaterialRegistry.Zinc, MaterialRegistry.Copper),
            new AtmosphericConditions(25.0, 0.75, 0.1),
            DurationSeconds: 360,
            TimeSteps: 5,
            Mesh: mesh);

        var result = Engine.Run(parameters);

        Assert.Equal(100, result.NodalPotentials!.Length); // 10×10
        Assert.Equal(100, result.NodalCorrosionRates!.Length);
    }

    [Fact]
    public void Run_WithCorrosionProductPhase_LowersLocalCorrosionRate()
    {
        var result = Engine.Run(MakeParams(PhaseLayeredMesh()));

        Assert.NotNull(result.NodalCorrosionRates);

        const int ny = 5;
        double electrolyteRate       = result.NodalCorrosionRates![2 * ny + 1];
        double corrosionProductRate  = result.NodalCorrosionRates[2 * ny + 4];

        Assert.True(electrolyteRate > corrosionProductRate,
            $"Expected electrolyte rate {electrolyteRate} to exceed corrosion-product rate {corrosionProductRate}.");
    }

    [Fact]
    public void Run_WithExplicitCorrosionProductMaterial_FurtherLowersCorrosionProductRate()
    {
        var baseline = Engine.Run(MakeParams(PhaseLayeredMesh()));
        var withBarrier = Engine.Run(MakeParams(PhaseLayeredMesh(), CorrosionProductBehavior.AluminiumOxide));

        Assert.NotNull(baseline.NodalCorrosionRates);
        Assert.NotNull(withBarrier.NodalCorrosionRates);

        const int ny = 5;
        double baselineRate = baseline.NodalCorrosionRates![2 * ny + 4];
        double barrierRate = withBarrier.NodalCorrosionRates![2 * ny + 4];

        Assert.True(barrierRate < baselineRate,
            $"Expected explicit corrosion-product rate {barrierRate} to be below baseline rate {baselineRate}.");
    }

    [Fact]
    public void Run_WithDynamicMetalElectrolyteInterfaces_AppliesElectrodePotentialsAtInterfaces()
    {
        var mesh = DynamicInterfaceMesh();
        var result = Engine.Run(MakeParams(mesh));

        Assert.NotNull(result.NodalPotentials);

        int ny = mesh.NodesY;
        const int anodeInterfaceColumn = 1;
        const int cathodeInterfaceColumn = 3;
        double anodePotential = MaterialRegistry.Zinc.StandardPotential;
        double cathodePotential = MaterialRegistry.Copper.StandardPotential;

        for (int j = 0; j < ny; j++)
        {
            int anodeInterfaceIdx = anodeInterfaceColumn * ny + j;
            int cathodeInterfaceIdx = cathodeInterfaceColumn * ny + j;
            Assert.Equal(anodePotential, result.NodalPotentials![anodeInterfaceIdx], precision: 6);
            Assert.Equal(cathodePotential, result.NodalPotentials[cathodeInterfaceIdx], precision: 6);
        }
    }
}
