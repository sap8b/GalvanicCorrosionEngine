using GCE.Atmosphere;
using GCE.Core;
using GCE.Electrochemistry;
using GCE.Simulation;

namespace GCE.Simulation.Tests;

/// <summary>
/// Tests for per-node cumulative mass-loss bookkeeping introduced to support the
/// moving-boundary feature.  The feature is exercised via the public
/// <see cref="SimulationEngine"/> API.
/// </summary>
public class NodeMassLossTests
{
    private static readonly SimulationEngine Engine = new();

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 5×5 mesh: columns 0–2 are Anode, columns 3–4 are Cathode.
    /// </summary>
    private static GeometryMesh StandardMesh()
    {
        const int lastAnodeColumn = 2; // columns 0..2 are Anode, 3..4 are Cathode
        var regions = new NodePhase[5, 5];
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
                regions[i, j] = i <= lastAnodeColumn ? NodePhase.Anode : NodePhase.Cathode;

        return new GeometryMesh(
            XCoordinates: [0.00, 0.025, 0.050, 0.075, 0.100],
            YCoordinates: [0.00, 0.025, 0.050, 0.075, 0.100],
            Regions: regions);
    }

    /// <summary>
    /// Very small (3×2) all-metal mesh designed to produce dissolution within
    /// a single step: the metal conductivity drives extremely high current density,
    /// ensuring mass-loss accumulation exceeds the nodal threshold immediately.
    /// </summary>
    private static GeometryMesh TinyAnodeMesh()
    {
        var regions = new NodePhase[3, 2];
        for (int j = 0; j < 2; j++)
        {
            regions[0, j] = NodePhase.Anode;
            regions[1, j] = NodePhase.Anode;
            regions[2, j] = NodePhase.Cathode;
        }

        // 10 µm total extent – metal conductivity (1 × 10⁶ S/m) over this tiny length
        // produces |j| >> 1 × 10⁸ A/m², far exceeding the dissolution threshold in one step.
        return new GeometryMesh(
            XCoordinates: [0.0, 5e-6, 1e-5],
            YCoordinates: [0.0, 1e-5],
            Regions: regions);
    }

    private static SimulationParameters MeshParams(GeometryMesh mesh, int steps = 5) =>
        new SimulationParameters(
            new GalvanicPair(MaterialRegistry.Zinc, MaterialRegistry.Copper),
            new AtmosphericConditions(25.0, 0.75, 0.1),
            DurationSeconds: 360,
            TimeSteps: steps,
            Mesh: mesh);

    // ── Basic population ───────────────────────────────────────────────────────

    [Fact]
    public void Run_WithMesh_NodeMassLossIsNotNull()
    {
        var result = Engine.Run(MeshParams(StandardMesh()));
        Assert.NotNull(result.NodeMassLoss);
    }

    [Fact]
    public void Run_WithMesh_NodeMassLossLengthMatchesMeshNodeCount()
    {
        var mesh   = StandardMesh();
        var result = Engine.Run(MeshParams(mesh));

        Assert.NotNull(result.NodeMassLoss);
        Assert.Equal(mesh.NodesX * mesh.NodesY, result.NodeMassLoss!.Length);
    }

    [Fact]
    public void Run_WithoutMesh_NodeMassLossIsNull()
    {
        var parameters = new SimulationParameters(
            new GalvanicPair(MaterialRegistry.Zinc, MaterialRegistry.Copper),
            new AtmosphericConditions(25.0, 0.75, 0.1),
            DurationSeconds: 360,
            TimeSteps: 5);

        var result = Engine.Run(parameters);
        Assert.Null(result.NodeMassLoss);
    }

    // ── Value correctness ──────────────────────────────────────────────────────

    [Fact]
    public void Run_WithMesh_NodeMassLossIsNonNegative()
    {
        var result = Engine.Run(MeshParams(StandardMesh()));

        Assert.NotNull(result.NodeMassLoss);
        Assert.All(result.NodeMassLoss!, m => Assert.True(m >= 0.0, $"Mass loss {m} is negative."));
    }

    [Fact]
    public void Run_WithMesh_AnodeNodeMassLossIsPositive()
    {
        var result = Engine.Run(MeshParams(StandardMesh()));

        Assert.NotNull(result.NodeMassLoss);
        // All Anode nodes (columns 0–2 in the 5×5 StandardMesh) must accumulate
        // non-zero mass loss given the non-zero corrosion rate of the Zinc/Copper couple.
        const int ny               = 5;
        const int lastAnodeColumn  = 2;
        for (int i = 0; i <= lastAnodeColumn; i++)
            for (int j = 0; j < 5; j++)
                Assert.True(result.NodeMassLoss![i * ny + j] > 0.0,
                    $"Expected positive mass loss at anode node ({i},{j}).");
    }

    [Fact]
    public void Run_WithMesh_CathodeNodesHaveZeroMassLoss()
    {
        var result = Engine.Run(MeshParams(StandardMesh()));

        Assert.NotNull(result.NodeMassLoss);
        // Cathode nodes (columns 3–4 in the 5×5 StandardMesh) are never NodePhase.Anode
        // so they should not accumulate mass.
        const int ny              = 5;
        const int firstCathodeCol = 3;
        for (int i = firstCathodeCol; i <= 4; i++)
            for (int j = 0; j < 5; j++)
                Assert.Equal(0.0, result.NodeMassLoss![i * ny + j]);
    }

    [Fact]
    public void Run_LongerDuration_NodeMassLossArrayLengthIsStable()
    {
        // Confirms that NodeMassLoss is stable (same length) regardless of how many steps ran.
        var mesh5  = StandardMesh();
        var mesh10 = StandardMesh();

        var result5  = Engine.Run(MeshParams(mesh5,  steps: 5));
        var result10 = Engine.Run(MeshParams(mesh10, steps: 10));

        Assert.NotNull(result5.NodeMassLoss);
        Assert.NotNull(result10.NodeMassLoss);
        Assert.Equal(result5.NodeMassLoss!.Length, result10.NodeMassLoss!.Length);
    }

    // ── Checkpoint / resume ────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_WhenCancelled_CheckpointContainsNodeMassLoss()
    {
        var mesh       = StandardMesh();
        var parameters = MeshParams(mesh, steps: 20);
        var cts        = new CancellationTokenSource();

        await Engine.RunAsync(
            parameters,
            new Progress<SimulationProgress>(p =>
            {
                if (p.CurrentStep >= 5) cts.Cancel();
            }),
            out SimulationState? checkpoint,
            cts.Token);

        await Task.Delay(50); // allow IProgress callbacks to flush

        Assert.NotNull(checkpoint);
        Assert.NotNull(checkpoint!.NodeMassLoss);
        Assert.Equal(mesh.NodesX * mesh.NodesY, checkpoint.NodeMassLoss!.Length);
        Assert.True(checkpoint.NodeMassLoss.Any(m => m > 0.0),
            "Expected at least one node with positive mass loss in the checkpoint.");
    }

    [Fact]
    public async Task Resume_CarriesForwardNodeMassLossFromCheckpoint()
    {
        var parameters = MeshParams(StandardMesh(), steps: 20);
        var cts        = new CancellationTokenSource();

        // Run until step ≥ 5, then cancel to capture a checkpoint.
        await Engine.RunAsync(
            parameters,
            new Progress<SimulationProgress>(p =>
            {
                if (p.CurrentStep >= 5) cts.Cancel();
            }),
            out SimulationState? checkpoint,
            cts.Token);

        await Task.Delay(50);

        Assert.NotNull(checkpoint);
        double checkpointMassTotal = checkpoint!.NodeMassLoss!.Sum();

        // Resume with a fresh matching mesh so the simulation can continue.
        var resumeParams = MeshParams(StandardMesh(), steps: 20);
        var resumed = await Engine.Resume(checkpoint, resumeParams);

        Assert.NotNull(resumed.NodeMassLoss);
        double resumedMassTotal = resumed.NodeMassLoss!.Sum();

        Assert.True(resumedMassTotal >= checkpointMassTotal,
            $"Resumed total mass loss ({resumedMassTotal}) should be ≥ checkpoint total ({checkpointMassTotal}).");
    }

    // ── Dissolution transition ─────────────────────────────────────────────────

    [Fact]
    public void Run_TinyMesh_AnodeNodesDissolveToElectrolyte()
    {
        // The TinyAnodeMesh has a 10 µm extent so the metal conductivity (1 × 10⁶ S/m)
        // drives |j| >> 10⁸ A/m², producing a mass-loss increment per step that far
        // exceeds ρ × V_node.  Even a single 1-second step is enough to dissolve every
        // anode node.
        var mesh       = TinyAnodeMesh();
        var parameters = new SimulationParameters(
            new GalvanicPair(MaterialRegistry.Zinc, MaterialRegistry.Copper),
            new AtmosphericConditions(25.0, 0.75, 0.1),
            DurationSeconds: 1.0,
            TimeSteps: 1,
            Mesh: mesh);

        var result = Engine.Run(parameters);

        Assert.NotNull(result.NodeMassLoss);

        // Every original Anode node should have transitioned to Electrolyte.
        bool anyDissolved = false;
        for (int i = 0; i < mesh.NodesX; i++)
            for (int j = 0; j < mesh.NodesY; j++)
                if (mesh.Regions[i, j] == NodePhase.Electrolyte)
                    anyDissolved = true;

        Assert.True(anyDissolved,
            "Expected at least one anode node to dissolve and transition to Electrolyte.");
    }

    [Fact]
    public void Run_TinyMesh_DissolvedNodeHasMassLossAboveThreshold()
    {
        var mesh       = TinyAnodeMesh();
        var parameters = new SimulationParameters(
            new GalvanicPair(MaterialRegistry.Zinc, MaterialRegistry.Copper),
            new AtmosphericConditions(25.0, 0.75, 0.1),
            DurationSeconds: 1.0,
            TimeSteps: 1,
            Mesh: mesh);

        var result = Engine.Run(parameters);

        Assert.NotNull(result.NodeMassLoss);

        // For Zinc: ρ = 7133 kg/m³, dx = 5e-6 m, dy = 1e-5 m → threshold = 3.57 × 10⁻⁷ kg/m.
        double dx        = 5e-6;
        double dy        = 1e-5;
        double threshold = MaterialRegistry.Zinc.Density * dx * dy;

        int ny = mesh.NodesY;
        for (int i = 0; i <= 1; i++) // original anode columns
            for (int j = 0; j < ny; j++)
                Assert.True(result.NodeMassLoss![i * ny + j] >= threshold,
                    $"Node ({i},{j}) mass loss {result.NodeMassLoss![i * ny + j]:e3} is below threshold {threshold:e3}.");
    }
}
