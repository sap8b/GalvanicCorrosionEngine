using GCE.Atmosphere;
using GCE.Core;
using GCE.Electrochemistry;

namespace GCE.Core.Tests;

// ── Helpers ───────────────────────────────────────────────────────────────────

file static class TestFixtures
{
    public static readonly IElectrolyte Electrolyte = new AtmosphericConditions(25.0, 0.75, 0.1);

    public static Anode ZincAnode(double area = 0.01) =>
        new(MaterialRegistry.Zinc, area, Electrolyte);

    public static Cathode CopperCathode(double area = 0.01) =>
        new(MaterialRegistry.Copper, area, Electrolyte);
}

/// <summary>
/// A constant-current-density stub reaction, useful for testing AddReaction.
/// </summary>
file sealed class ConstantReaction(double currentDensity) : IElectrochemicalReaction
{
    public double CurrentDensity(double potential) => currentDensity;
}

// ── Anode tests ───────────────────────────────────────────────────────────────

public class AnodeTests
{
    private readonly IElectrolyte _env = TestFixtures.Electrolyte;

    [Fact]
    public void Anode_StoresMaterialAndArea()
    {
        var anode = TestFixtures.ZincAnode(0.05);
        Assert.Equal(MaterialRegistry.Zinc, anode.Material);
        Assert.Equal(0.05, anode.Area);
    }

    [Fact]
    public void Anode_ImplementsIElectrode()
    {
        IElectrode electrode = TestFixtures.ZincAnode();
        Assert.Equal(MaterialRegistry.Zinc, electrode.Material);
    }

    [Fact]
    public void Anode_OpenCircuitPotential_EqualsStandardPotential()
    {
        var anode = TestFixtures.ZincAnode();
        Assert.Equal(MaterialRegistry.Zinc.StandardPotential, anode.OpenCircuitPotential);
    }

    [Fact]
    public void Anode_CurrentDensityAtOcp_IsApproximatelyZero()
    {
        var anode = TestFixtures.ZincAnode();
        double i = anode.ComputeCurrentDensity(anode.OpenCircuitPotential);
        Assert.Equal(0.0, i, precision: 12);
    }

    [Fact]
    public void Anode_CurrentDensity_IsPositiveAboveOcp()
    {
        var anode = TestFixtures.ZincAnode();
        double i = anode.ComputeCurrentDensity(anode.OpenCircuitPotential + 0.1);
        Assert.True(i > 0.0, $"Expected positive anodic current above OCP, got {i}");
    }

    [Fact]
    public void Anode_CurrentDensity_IsNegativeBelowOcp()
    {
        var anode = TestFixtures.ZincAnode();
        double i = anode.ComputeCurrentDensity(anode.OpenCircuitPotential - 0.1);
        Assert.True(i < 0.0, $"Expected negative cathodic current below OCP, got {i}");
    }

    [Fact]
    public void Anode_AddReaction_SumsIntoCurrentDensity()
    {
        var anode = TestFixtures.ZincAnode();
        double baseI = anode.ComputeCurrentDensity(0.0);

        const double extra = 5.0;
        anode.AddReaction(new ConstantReaction(extra));

        double totalI = anode.ComputeCurrentDensity(0.0);
        Assert.Equal(baseI + extra, totalI, precision: 12);
    }

    [Fact]
    public void Anode_AdditionalReactions_ReflectsAddedReaction()
    {
        var anode = TestFixtures.ZincAnode();
        Assert.Empty(anode.AdditionalReactions);

        var rxn = new ConstantReaction(1.0);
        anode.AddReaction(rxn);
        Assert.Single(anode.AdditionalReactions);
        Assert.Same(rxn, anode.AdditionalReactions[0]);
    }

    [Fact]
    public void Anode_MultipleAdditionalReactions_AllSummed()
    {
        var anode = TestFixtures.ZincAnode();
        anode.AddReaction(new ConstantReaction(3.0));
        anode.AddReaction(new ConstantReaction(-1.0));

        double baseI = new Anode(MaterialRegistry.Zinc, 0.01, _env).ComputeCurrentDensity(0.0);
        Assert.Equal(baseI + 2.0, anode.ComputeCurrentDensity(0.0), precision: 12);
    }

    [Fact]
    public void Anode_PolarizationCurve_HasCorrectPointCount()
    {
        var anode = TestFixtures.ZincAnode();
        var curve = anode.PolarizationCurve(-1.0, 0.0, 51);
        Assert.Equal(51, curve.Count);
    }

    [Fact]
    public void Anode_PolarizationCurve_BoundsAreCorrect()
    {
        var anode = TestFixtures.ZincAnode();
        double eStart = -1.0;
        double eEnd = 0.0;
        var curve = anode.PolarizationCurve(eStart, eEnd, 11);
        Assert.Equal(eStart, curve[0].Potential, precision: 12);
        Assert.Equal(eEnd, curve[^1].Potential, precision: 12);
    }

    [Fact]
    public void Anode_Constructor_ThrowsOnNullMaterial()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Anode(null!, 0.01, _env));
    }

    [Fact]
    public void Anode_Constructor_ThrowsOnNullEnvironment()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Anode(MaterialRegistry.Zinc, 0.01, null!));
    }

    [Fact]
    public void Anode_Constructor_ThrowsOnNonPositiveArea()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Anode(MaterialRegistry.Zinc, 0.0, _env));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Anode(MaterialRegistry.Zinc, -1.0, _env));
    }

    [Fact]
    public void Anode_AddReaction_ThrowsOnNull()
    {
        var anode = TestFixtures.ZincAnode();
        Assert.Throws<ArgumentNullException>(() => anode.AddReaction(null!));
    }

    [Fact]
    public void Anode_PolarizationCurve_ThrowsWhenPointsBelowTwo()
    {
        var anode = TestFixtures.ZincAnode();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            anode.PolarizationCurve(-1.0, 0.0, 1));
    }
}

// ── Cathode tests ─────────────────────────────────────────────────────────────

public class CathodeTests
{
    private readonly IElectrolyte _env = TestFixtures.Electrolyte;

    [Fact]
    public void Cathode_StoresMaterialAndArea()
    {
        var cathode = TestFixtures.CopperCathode(0.05);
        Assert.Equal(MaterialRegistry.Copper, cathode.Material);
        Assert.Equal(0.05, cathode.Area);
    }

    [Fact]
    public void Cathode_ImplementsIElectrode()
    {
        IElectrode electrode = TestFixtures.CopperCathode();
        Assert.Equal(MaterialRegistry.Copper, electrode.Material);
    }

    [Fact]
    public void Cathode_OpenCircuitPotential_EqualsStandardPotential()
    {
        var cathode = TestFixtures.CopperCathode();
        Assert.Equal(MaterialRegistry.Copper.StandardPotential, cathode.OpenCircuitPotential);
    }

    [Fact]
    public void Cathode_CurrentDensityAtOcp_IsApproximatelyZero()
    {
        var cathode = TestFixtures.CopperCathode();
        double i = cathode.ComputeCurrentDensity(cathode.OpenCircuitPotential);
        Assert.Equal(0.0, i, precision: 12);
    }

    [Fact]
    public void Cathode_CurrentDensity_IsNegativeBelowOcp()
    {
        var cathode = TestFixtures.CopperCathode();
        double i = cathode.ComputeCurrentDensity(cathode.OpenCircuitPotential - 0.1);
        Assert.True(i < 0.0, $"Expected negative cathodic current below OCP, got {i}");
    }

    [Fact]
    public void Cathode_CurrentDensity_IsPositiveAboveOcp()
    {
        var cathode = TestFixtures.CopperCathode();
        double i = cathode.ComputeCurrentDensity(cathode.OpenCircuitPotential + 0.1);
        Assert.True(i > 0.0, $"Expected positive anodic current above OCP, got {i}");
    }

    [Fact]
    public void Cathode_AddReaction_SumsIntoCurrentDensity()
    {
        var cathode = TestFixtures.CopperCathode();
        double baseI = cathode.ComputeCurrentDensity(0.0);

        const double extra = -3.0;
        cathode.AddReaction(new ConstantReaction(extra));

        Assert.Equal(baseI + extra, cathode.ComputeCurrentDensity(0.0), precision: 12);
    }

    [Fact]
    public void Cathode_AdditionalReactions_ReflectsAddedReaction()
    {
        var cathode = TestFixtures.CopperCathode();
        Assert.Empty(cathode.AdditionalReactions);

        var rxn = new ConstantReaction(-2.0);
        cathode.AddReaction(rxn);
        Assert.Single(cathode.AdditionalReactions);
    }

    [Fact]
    public void Cathode_PolarizationCurve_HasCorrectPointCount()
    {
        var cathode = TestFixtures.CopperCathode();
        var curve = cathode.PolarizationCurve(-0.5, 1.0, 51);
        Assert.Equal(51, curve.Count);
    }

    [Fact]
    public void Cathode_PolarizationCurve_BoundsAreCorrect()
    {
        var cathode = TestFixtures.CopperCathode();
        var curve = cathode.PolarizationCurve(-0.5, 1.0, 11);
        Assert.Equal(-0.5, curve[0].Potential, precision: 12);
        Assert.Equal(1.0, curve[^1].Potential, precision: 12);
    }

    [Fact]
    public void Cathode_Constructor_ThrowsOnNullMaterial()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Cathode(null!, 0.01, _env));
    }

    [Fact]
    public void Cathode_Constructor_ThrowsOnNullEnvironment()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Cathode(MaterialRegistry.Copper, 0.01, null!));
    }

    [Fact]
    public void Cathode_Constructor_ThrowsOnNonPositiveArea()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Cathode(MaterialRegistry.Copper, 0.0, _env));
    }

    [Fact]
    public void Cathode_PolarizationCurve_ThrowsWhenPointsBelowTwo()
    {
        var cathode = TestFixtures.CopperCathode();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            cathode.PolarizationCurve(0.0, 1.0, 1));
    }
}

// ── GalvanicCouple tests ──────────────────────────────────────────────────────

public class GalvanicCoupleTests
{
    private static readonly IElectrolyte Env = TestFixtures.Electrolyte;

    // Zinc anode (OCP −0.76 V) coupled to Copper cathode (OCP +0.34 V)
    private static Anode ZnAnode(double area = 0.01) => TestFixtures.ZincAnode(area);
    private static Cathode CuCathode(double area = 0.01) => TestFixtures.CopperCathode(area);

    [Fact]
    public void GalvanicCouple_StoresElectrodesAndElectrolyte()
    {
        var couple = new GalvanicCouple(ZnAnode(), CuCathode(), Env);
        Assert.Equal(MaterialRegistry.Zinc, couple.Anode.Material);
        Assert.Equal(MaterialRegistry.Copper, couple.Cathode.Material);
        Assert.Same(Env, couple.Electrolyte);
    }

    [Fact]
    public void GalvanicCouple_GalvanicVoltage_IsCathodeOcpMinusAnodeOcp()
    {
        var couple = new GalvanicCouple(ZnAnode(), CuCathode(), Env);
        double expected = MaterialRegistry.Copper.StandardPotential
                        - MaterialRegistry.Zinc.StandardPotential;
        Assert.Equal(expected, couple.GalvanicVoltage, precision: 9);
    }

    [Fact]
    public void GalvanicCouple_MixedPotential_IsBetweenOcpValues()
    {
        var couple = new GalvanicCouple(ZnAnode(), CuCathode(), Env);
        double em = couple.MixedPotential;
        Assert.True(em > MaterialRegistry.Zinc.StandardPotential,
            $"Mixed potential {em} should be above anode OCP {MaterialRegistry.Zinc.StandardPotential}");
        Assert.True(em < MaterialRegistry.Copper.StandardPotential,
            $"Mixed potential {em} should be below cathode OCP {MaterialRegistry.Copper.StandardPotential}");
    }

    [Fact]
    public void GalvanicCouple_MixedPotential_NetCurrentIsNearZero()
    {
        var anode = ZnAnode(0.02);
        var cathode = CuCathode(0.01);
        var couple = new GalvanicCouple(anode, cathode, Env);
        double em = couple.MixedPotential;

        double netCurrent =
            anode.Area * anode.ComputeCurrentDensity(em) +
            cathode.Area * cathode.ComputeCurrentDensity(em);

        Assert.Equal(0.0, netCurrent, precision: 6);
    }

    [Fact]
    public void GalvanicCouple_CorrosionCurrentDensity_IsPositive()
    {
        var couple = new GalvanicCouple(ZnAnode(), CuCathode(), Env);
        Assert.True(couple.CorrosionCurrentDensity > 0.0,
            "Corrosion current density at anode should be positive at the mixed potential.");
    }

    [Fact]
    public void GalvanicCouple_AreaRatio_ShiftsMixedPotential()
    {
        // Larger cathode area → more cathodic current → mixed potential shifts more anodic
        var coupleSmallCathode = new GalvanicCouple(ZnAnode(0.01), CuCathode(0.001), Env);
        var coupleLargeCathode = new GalvanicCouple(ZnAnode(0.01), CuCathode(0.10), Env);

        Assert.True(coupleLargeCathode.MixedPotential > coupleSmallCathode.MixedPotential,
            "Larger cathode area should drive mixed potential more anodic.");
    }

    [Fact]
    public void GalvanicCouple_PolarizationCurve_HasCorrectPointCount()
    {
        var couple = new GalvanicCouple(ZnAnode(), CuCathode(), Env);
        var curve = couple.PolarizationCurve(-1.0, 0.5, 51);
        Assert.Equal(51, curve.Count);
    }

    [Fact]
    public void GalvanicCouple_PolarizationCurve_BoundsAreCorrect()
    {
        var couple = new GalvanicCouple(ZnAnode(), CuCathode(), Env);
        var curve = couple.PolarizationCurve(-1.0, 0.5, 11);
        Assert.Equal(-1.0, curve[0].Potential, precision: 12);
        Assert.Equal(0.5, curve[^1].Potential, precision: 12);
    }

    [Fact]
    public void GalvanicCouple_Constructor_ThrowsOnNullAnode()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GalvanicCouple(null!, CuCathode(), Env));
    }

    [Fact]
    public void GalvanicCouple_Constructor_ThrowsOnNullCathode()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GalvanicCouple(ZnAnode(), null!, Env));
    }

    [Fact]
    public void GalvanicCouple_Constructor_ThrowsOnNullElectrolyte()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GalvanicCouple(ZnAnode(), CuCathode(), null!));
    }

    [Fact]
    public void GalvanicCouple_Constructor_ThrowsWhenCathodeNotMoreNoble()
    {
        // Zinc (−0.76 V) used as cathode, Copper (+0.34 V) used as anode — invalid
        var anodeCu = new Anode(MaterialRegistry.Copper, 0.01, Env);
        var cathodeZn = new Cathode(MaterialRegistry.Zinc, 0.01, Env);
        Assert.Throws<ArgumentException>(() =>
            new GalvanicCouple(anodeCu, cathodeZn, Env));
    }

    [Fact]
    public void GalvanicCouple_PolarizationCurve_ThrowsWhenPointsBelowTwo()
    {
        var couple = new GalvanicCouple(ZnAnode(), CuCathode(), Env);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            couple.PolarizationCurve(-1.0, 0.5, 1));
    }
}
