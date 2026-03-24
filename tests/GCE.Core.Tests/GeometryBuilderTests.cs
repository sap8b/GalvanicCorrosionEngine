using GCE.Atmosphere;
using GCE.Core;
using GCE.Simulation.Geometry;

namespace GCE.Core.Tests;

// ── Helpers ───────────────────────────────────────────────────────────────────

file static class GeometryFixtures
{
    // A simple electrolyte for all geometry tests (25 °C, 75 % RH, 0.1 mol/L Cl⁻).
    public static readonly IElectrolyte Electrolyte = new AtmosphericConditions(25.0, 0.75, 0.1);

    // Zinc is the anode (−0.76 V), copper is the cathode (+0.34 V).
    public static IMaterial Anode => MaterialRegistry.Zinc;
    public static IMaterial Cathode => MaterialRegistry.Copper;
}

// ── BoltInPlateGeometry tests ─────────────────────────────────────────────────

public class BoltInPlateGeometryTests
{
    // r = 5 mm, t = 10 mm, w = 50 mm — bolt is zinc (anode), plate is copper (cathode).
    private static BoltInPlateGeometry CreateDefault() =>
        new(GeometryFixtures.Anode, GeometryFixtures.Cathode,
            boltRadius: 0.005, plateThickness: 0.010, plateWidth: 0.050);

    [Fact]
    public void Constructor_StoresProperties()
    {
        var g = CreateDefault();
        Assert.Equal(GeometryFixtures.Anode, g.BoltMaterial);
        Assert.Equal(GeometryFixtures.Cathode, g.PlateMaterial);
        Assert.Equal(0.005, g.BoltRadius);
        Assert.Equal(0.010, g.PlateThickness);
        Assert.Equal(0.050, g.PlateWidth);
    }

    [Fact]
    public void Constructor_ZincBolt_IsAnode()
    {
        var g = CreateDefault();
        Assert.Equal(MaterialRegistry.Zinc, g.AnodeMaterial);
        Assert.Equal(MaterialRegistry.Copper, g.CathodeMaterial);
    }

    [Fact]
    public void Constructor_NobleBolt_BecomesCathode()
    {
        // When bolt is copper (more noble) and plate is zinc (more active), bolt is cathode.
        var g = new BoltInPlateGeometry(
            MaterialRegistry.Copper, MaterialRegistry.Zinc,
            boltRadius: 0.005, plateThickness: 0.010, plateWidth: 0.050);

        Assert.Equal(MaterialRegistry.Zinc, g.AnodeMaterial);
        Assert.Equal(MaterialRegistry.Copper, g.CathodeMaterial);
    }

    [Fact]
    public void Constructor_BoltDiameterEqualsPlateWidth_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new BoltInPlateGeometry(
                GeometryFixtures.Anode, GeometryFixtures.Cathode,
                boltRadius: 0.025, plateThickness: 0.010, plateWidth: 0.050));
    }

    [Fact]
    public void Constructor_SamePotentialMaterials_Throws()
    {
        var mat = MaterialRegistry.Zinc; // same material → same potential
        Assert.Throws<ArgumentException>(() =>
            new BoltInPlateGeometry(mat, mat,
                boltRadius: 0.005, plateThickness: 0.010, plateWidth: 0.050));
    }

    [Fact]
    public void Constructor_NonPositiveBoltRadius_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BoltInPlateGeometry(
                GeometryFixtures.Anode, GeometryFixtures.Cathode,
                boltRadius: 0.0, plateThickness: 0.010, plateWidth: 0.050));

    [Fact]
    public void Build_ReturnsGalvanicCell_WithCorrectMaterials()
    {
        var g = CreateDefault();
        var cell = g.Build(GeometryFixtures.Electrolyte);

        Assert.Equal(g.AnodeMaterial, cell.Anode.Material);
        Assert.Equal(g.CathodeMaterial, cell.Cathode.Material);
    }

    [Fact]
    public void Build_AnodeArea_EqualsBoltLateralArea()
    {
        // Bolt lateral area = 2π × r × t
        var g = CreateDefault();
        double expected = 2.0 * Math.PI * g.BoltRadius * g.PlateThickness;
        var cell = g.Build(GeometryFixtures.Electrolyte);
        Assert.Equal(expected, cell.Anode.Area, precision: 10);
    }

    [Fact]
    public void Build_CathodeArea_EqualsPlateAreaMinusHole()
    {
        // Plate area = w² − π·r²
        var g = CreateDefault();
        double expected = g.PlateWidth * g.PlateWidth - Math.PI * g.BoltRadius * g.BoltRadius;
        var cell = g.Build(GeometryFixtures.Electrolyte);
        Assert.Equal(expected, cell.Cathode.Area, precision: 10);
    }

    [Fact]
    public void Build_NullElectrolyte_Throws() =>
        Assert.Throws<ArgumentNullException>(() =>
            CreateDefault().Build(null!));

    [Fact]
    public void BuildMesh_DefaultSize_HasCorrectDimensions()
    {
        var g = CreateDefault();
        var mesh = g.BuildMesh(10, 8);

        Assert.Equal(10, mesh.NodesX);
        Assert.Equal(8, mesh.NodesY);
        Assert.Equal(10, mesh.XCoordinates.Length);
        Assert.Equal(8, mesh.YCoordinates.Length);
        Assert.Equal(10, mesh.Regions.GetLength(0));
        Assert.Equal(8, mesh.Regions.GetLength(1));
    }

    [Fact]
    public void BuildMesh_CentreNode_IsInBoltRegion()
    {
        var g = new BoltInPlateGeometry(
            GeometryFixtures.Anode, GeometryFixtures.Cathode,
            boltRadius: 0.010, plateThickness: 0.010, plateWidth: 0.050);

        // Use odd number of nodes so there is an exact centre node.
        var mesh = g.BuildMesh(nodesX: 11, nodesY: 11);
        int ci = 5, cj = 5; // centre node (0-indexed)
        Assert.Equal(0, mesh.Regions[ci, cj]); // anode region (bolt = zinc = anode)
    }

    [Fact]
    public void BuildMesh_CornerNode_IsInPlateRegion()
    {
        var g = CreateDefault();
        var mesh = g.BuildMesh(20, 20);
        // Corners are far from centre — should be in plate (cathode = 1) region.
        Assert.Equal(1, mesh.Regions[0, 0]);
        Assert.Equal(1, mesh.Regions[19, 19]);
    }

    [Fact]
    public void BuildMesh_XCoordinates_SpanFullPlateWidth()
    {
        var g = CreateDefault();
        var mesh = g.BuildMesh(20, 20);
        double half = g.PlateWidth / 2.0;
        Assert.Equal(-half, mesh.XCoordinates[0], precision: 10);
        Assert.Equal(+half, mesh.XCoordinates[19], precision: 10);
    }

    [Fact]
    public void BuildMesh_TooFewNodes_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateDefault().BuildMesh(nodesX: 1));
}

// ── SideBySideGeometry tests ──────────────────────────────────────────────────

public class SideBySideGeometryTests
{
    // Anode: zinc (0.02 m wide), cathode: copper (0.02 m wide), length: 0.1 m.
    private static SideBySideGeometry CreateDefault() =>
        new(GeometryFixtures.Anode, GeometryFixtures.Cathode,
            anodeWidth: 0.020, cathodeWidth: 0.020, length: 0.100);

    [Fact]
    public void Constructor_StoresProperties()
    {
        var g = CreateDefault();
        Assert.Equal(GeometryFixtures.Anode, g.AnodeMaterial);
        Assert.Equal(GeometryFixtures.Cathode, g.CathodeMaterial);
        Assert.Equal(0.020, g.AnodeWidth);
        Assert.Equal(0.020, g.CathodeWidth);
        Assert.Equal(0.100, g.Length);
    }

    [Fact]
    public void Constructor_CathodeNotMoreNoble_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            new SideBySideGeometry(
                MaterialRegistry.Copper, MaterialRegistry.Zinc,
                anodeWidth: 0.020, cathodeWidth: 0.020, length: 0.100));

    [Fact]
    public void Constructor_NonPositiveWidth_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SideBySideGeometry(
                GeometryFixtures.Anode, GeometryFixtures.Cathode,
                anodeWidth: 0.0, cathodeWidth: 0.020, length: 0.100));

    [Fact]
    public void Build_AnodeArea_EqualsWidthTimesLength()
    {
        var g = CreateDefault();
        var cell = g.Build(GeometryFixtures.Electrolyte);
        Assert.Equal(g.AnodeWidth * g.Length, cell.Anode.Area, precision: 10);
    }

    [Fact]
    public void Build_CathodeArea_EqualsWidthTimesLength()
    {
        var g = CreateDefault();
        var cell = g.Build(GeometryFixtures.Electrolyte);
        Assert.Equal(g.CathodeWidth * g.Length, cell.Cathode.Area, precision: 10);
    }

    [Fact]
    public void Build_ImplementsIGeometryBuilder()
    {
        IGeometryBuilder builder = CreateDefault();
        var cell = builder.Build(GeometryFixtures.Electrolyte);
        Assert.NotNull(cell);
    }

    [Fact]
    public void Build_NullElectrolyte_Throws() =>
        Assert.Throws<ArgumentNullException>(() =>
            CreateDefault().Build(null!));

    [Fact]
    public void BuildMesh_HasCorrectNodeCounts()
    {
        var mesh = CreateDefault().BuildMesh(12, 8);
        Assert.Equal(12, mesh.NodesX);
        Assert.Equal(8, mesh.NodesY);
    }

    [Fact]
    public void BuildMesh_LeftSide_IsAnodeRegion()
    {
        var mesh = CreateDefault().BuildMesh(20, 10);
        // Node [0, *] is at x = 0, which is on the anode side.
        for (int j = 0; j < 10; j++)
            Assert.Equal(0, mesh.Regions[0, j]);
    }

    [Fact]
    public void BuildMesh_RightSide_IsCathodeRegion()
    {
        var mesh = CreateDefault().BuildMesh(20, 10);
        // Node [19, *] is at x = AnodeWidth + CathodeWidth (far right).
        for (int j = 0; j < 10; j++)
            Assert.Equal(1, mesh.Regions[19, j]);
    }

    [Fact]
    public void BuildMesh_XCoordinates_SpanTotalWidth()
    {
        var g = CreateDefault();
        var mesh = g.BuildMesh(20, 10);
        Assert.Equal(0.0, mesh.XCoordinates[0], precision: 10);
        Assert.Equal(g.AnodeWidth + g.CathodeWidth, mesh.XCoordinates[19], precision: 10);
    }

    [Fact]
    public void BuildMesh_AsymmetricWidths_RegionBoundaryCorrect()
    {
        // Anode: 0.01 m, cathode: 0.03 m — junction at x = 0.01 m.
        var g = new SideBySideGeometry(
            GeometryFixtures.Anode, GeometryFixtures.Cathode,
            anodeWidth: 0.010, cathodeWidth: 0.030, length: 0.100);

        var mesh = g.BuildMesh(nodesX: 5, nodesY: 2);
        // x values: 0, 0.01, 0.02, 0.03, 0.04  (step = 0.04/4 = 0.01)
        Assert.Equal(0, mesh.Regions[0, 0]); // x = 0.00 ≤ 0.01 → anode
        Assert.Equal(0, mesh.Regions[1, 0]); // x = 0.01 ≤ 0.01 → anode
        Assert.Equal(1, mesh.Regions[2, 0]); // x = 0.02 > 0.01 → cathode
    }

    [Fact]
    public void BuildMesh_TooFewNodes_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateDefault().BuildMesh(nodesY: 1));
}

// ── CustomGeometry tests ──────────────────────────────────────────────────────

public class CustomGeometryTests
{
    private static CustomGeometry CreateDefault(GeometryMesh? mesh = null) =>
        new(GeometryFixtures.Anode, GeometryFixtures.Cathode,
            anodeArea: 0.01, cathodeArea: 0.05, customMesh: mesh);

    [Fact]
    public void Constructor_StoresProperties()
    {
        var g = CreateDefault();
        Assert.Equal(GeometryFixtures.Anode, g.AnodeMaterial);
        Assert.Equal(GeometryFixtures.Cathode, g.CathodeMaterial);
        Assert.Equal(0.01, g.AnodeArea);
        Assert.Equal(0.05, g.CathodeArea);
    }

    [Fact]
    public void Constructor_CathodeNotMoreNoble_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            new CustomGeometry(
                MaterialRegistry.Copper, MaterialRegistry.Zinc,
                anodeArea: 0.01, cathodeArea: 0.05));

    [Fact]
    public void Constructor_NonPositiveAnodeArea_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CustomGeometry(
                GeometryFixtures.Anode, GeometryFixtures.Cathode,
                anodeArea: 0.0, cathodeArea: 0.05));

    [Fact]
    public void Constructor_NonPositiveCathodeArea_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CustomGeometry(
                GeometryFixtures.Anode, GeometryFixtures.Cathode,
                anodeArea: 0.01, cathodeArea: -1.0));

    [Fact]
    public void Build_ReturnsCell_WithSpecifiedAreas()
    {
        var g = CreateDefault();
        var cell = g.Build(GeometryFixtures.Electrolyte);

        Assert.Equal(0.01, cell.Anode.Area, precision: 10);
        Assert.Equal(0.05, cell.Cathode.Area, precision: 10);
    }

    [Fact]
    public void Build_NullElectrolyte_Throws() =>
        Assert.Throws<ArgumentNullException>(() =>
            CreateDefault().Build(null!));

    [Fact]
    public void BuildMesh_NoCustomMesh_ReturnsDefaultUnitSquare()
    {
        var g = CreateDefault();
        var mesh = g.BuildMesh(10, 10);

        Assert.Equal(10, mesh.NodesX);
        Assert.Equal(10, mesh.NodesY);
        Assert.Equal(0.0, mesh.XCoordinates[0], precision: 10);
        Assert.Equal(1.0, mesh.XCoordinates[9], precision: 10);
    }

    [Fact]
    public void BuildMesh_NoCustomMesh_LeftHalfIsAnode()
    {
        var g = CreateDefault();
        var mesh = g.BuildMesh(10, 4);
        // Nodes at x ≤ 0.5 → region 0 (anode)
        Assert.Equal(0, mesh.Regions[0, 0]);
        Assert.Equal(0, mesh.Regions[4, 0]); // x = 4/9 ≈ 0.444 ≤ 0.5
    }

    [Fact]
    public void BuildMesh_NoCustomMesh_RightHalfIsCathode()
    {
        var g = CreateDefault();
        var mesh = g.BuildMesh(10, 4);
        // Node at index 9: x = 1.0 > 0.5 → region 1 (cathode)
        Assert.Equal(1, mesh.Regions[9, 0]);
    }

    [Fact]
    public void BuildMesh_WithCustomMesh_ReturnsCustomMesh()
    {
        var xs = new double[] { 0.0, 1.0 };
        var ys = new double[] { 0.0, 1.0 };
        var regions = new int[,] { { 0, 0 }, { 1, 1 } };
        var customMesh = new GeometryMesh(xs, ys, regions);

        var g = CreateDefault(customMesh);
        var mesh = g.BuildMesh(50, 50); // parameters ignored when custom mesh provided

        Assert.Same(customMesh, mesh);
        Assert.Equal(2, mesh.NodesX);
        Assert.Equal(2, mesh.NodesY);
    }

    [Fact]
    public void BuildMesh_TooFewNodes_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateDefault().BuildMesh(nodesX: 1));
}

// ── IGeometryBuilder interface contract tests ─────────────────────────────────

public class IGeometryBuilderContractTests
{
    private static IElectrolyte Env => GeometryFixtures.Electrolyte;

    public static IEnumerable<object[]> AllBuilders()
    {
        yield return new object[]
        {
            new BoltInPlateGeometry(
                MaterialRegistry.Zinc, MaterialRegistry.Copper,
                boltRadius: 0.005, plateThickness: 0.010, plateWidth: 0.050)
        };
        yield return new object[]
        {
            new SideBySideGeometry(
                MaterialRegistry.Zinc, MaterialRegistry.Copper,
                anodeWidth: 0.020, cathodeWidth: 0.020, length: 0.100)
        };
        yield return new object[]
        {
            new CustomGeometry(
                MaterialRegistry.Zinc, MaterialRegistry.Copper,
                anodeArea: 0.01, cathodeArea: 0.05)
        };
    }

    [Theory]
    [MemberData(nameof(AllBuilders))]
    public void Build_AnodePotential_LowerThanCathodePotential(IGeometryBuilder builder)
    {
        Assert.True(
            builder.AnodeMaterial.StandardPotential < builder.CathodeMaterial.StandardPotential,
            "Anode standard potential must be lower than cathode standard potential.");
    }

    [Theory]
    [MemberData(nameof(AllBuilders))]
    public void Build_ReturnsCellWithPositiveAreas(IGeometryBuilder builder)
    {
        var cell = builder.Build(Env);
        Assert.True(cell.Anode.Area > 0);
        Assert.True(cell.Cathode.Area > 0);
    }

    [Theory]
    [MemberData(nameof(AllBuilders))]
    public void Build_GalvanicVoltage_IsPositive(IGeometryBuilder builder)
    {
        var cell = builder.Build(Env);
        Assert.True(cell.GalvanicVoltage > 0);
    }

    [Theory]
    [MemberData(nameof(AllBuilders))]
    public void BuildMesh_RegionsContainOnlyValidValues(IGeometryBuilder builder)
    {
        var mesh = builder.BuildMesh(10, 10);
        for (int i = 0; i < mesh.NodesX; i++)
            for (int j = 0; j < mesh.NodesY; j++)
                Assert.True(
                    mesh.Regions[i, j] is -1 or 0 or 1,
                    $"Unexpected region id {mesh.Regions[i, j]} at [{i},{j}]");
    }

    [Theory]
    [MemberData(nameof(AllBuilders))]
    public void BuildMesh_CoordinateArraysMatchRegionDimensions(IGeometryBuilder builder)
    {
        var mesh = builder.BuildMesh(8, 6);
        Assert.Equal(mesh.NodesX, mesh.Regions.GetLength(0));
        Assert.Equal(mesh.NodesY, mesh.Regions.GetLength(1));
    }
}
