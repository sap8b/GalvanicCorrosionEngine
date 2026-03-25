using GCE.Atmosphere;
using GCE.Core;
using GCE.Electrochemistry;

namespace GCE.Electrochemistry.Tests;

// ── Charge conservation in coupled galvanic systems ───────────────────────────
//
// At the mixed potential of a galvanic couple the total current flowing out of
// the anodic electrode (dissolution) must equal the total current flowing into
// the cathodic electrode (reduction), scaled by each electrode's area.
// That is:  A_anode · i_anode(E_m) + A_cathode · i_cathode(E_m) = 0
//
// These tests verify that charge is conserved for a variety of electrode
// geometries, electrolytes and coupled-reaction configurations.

public class ChargeConservationTests
{
    private static readonly IElectrolyte Electrolyte = new AtmosphericConditions(25.0, 0.75, 0.1);

    // ── Equal-area couple ─────────────────────────────────────────────────────

    [Fact]
    public void GalvanicCouple_EqualArea_TotalCurrentAtMixedPotential_IsZero()
    {
        var anode   = new Anode(MaterialRegistry.Zinc,   0.01, Electrolyte);
        var cathode = new Cathode(MaterialRegistry.Copper, 0.01, Electrolyte);
        var couple  = new GalvanicCouple(anode, cathode, Electrolyte);

        double em = couple.MixedPotential;
        double totalCurrent =
            anode.Area * anode.ComputeCurrentDensity(em) +
            cathode.Area * cathode.ComputeCurrentDensity(em);

        Assert.Equal(0.0, totalCurrent, precision: 6);
    }

    // ── Unequal-area couple ───────────────────────────────────────────────────

    [Theory]
    [InlineData(0.01, 0.02)]
    [InlineData(0.05, 0.01)]
    [InlineData(0.10, 0.10)]
    [InlineData(0.001, 0.10)]
    public void GalvanicCouple_UnequalArea_TotalCurrentAtMixedPotential_IsZero(
        double anodeArea, double cathodeArea)
    {
        var anode   = new Anode(MaterialRegistry.Zinc,   anodeArea,   Electrolyte);
        var cathode = new Cathode(MaterialRegistry.Copper, cathodeArea, Electrolyte);
        var couple  = new GalvanicCouple(anode, cathode, Electrolyte);

        double em = couple.MixedPotential;
        double totalCurrent =
            anode.Area * anode.ComputeCurrentDensity(em) +
            cathode.Area * cathode.ComputeCurrentDensity(em);

        Assert.Equal(0.0, totalCurrent, precision: 6);
    }

    // ── Mixed potential lies between electrode open-circuit potentials ────────

    [Fact]
    public void GalvanicCouple_MixedPotential_LiesBetweenOpenCircuitPotentials()
    {
        var anode   = new Anode(MaterialRegistry.Zinc,   0.01, Electrolyte);
        var cathode = new Cathode(MaterialRegistry.Copper, 0.01, Electrolyte);
        var couple  = new GalvanicCouple(anode, cathode, Electrolyte);

        double em = couple.MixedPotential;

        Assert.True(em > anode.OpenCircuitPotential,
            $"E_m {em:F4} should be above anode OCP {anode.OpenCircuitPotential:F4}");
        Assert.True(em < cathode.OpenCircuitPotential,
            $"E_m {em:F4} should be below cathode OCP {cathode.OpenCircuitPotential:F4}");
    }

    // ── Corrosion current density is positive ─────────────────────────────────

    [Fact]
    public void GalvanicCouple_CorrosionCurrentDensity_IsPositive()
    {
        var anode   = new Anode(MaterialRegistry.Zinc,   0.01, Electrolyte);
        var cathode = new Cathode(MaterialRegistry.Copper, 0.01, Electrolyte);
        var couple  = new GalvanicCouple(anode, cathode, Electrolyte);

        Assert.True(couple.CorrosionCurrentDensity > 0.0,
            "Corrosion current density at the anode must be positive.");
    }

    // ── Conservation across multiple coupled reactions ─────────────────────────

    [Fact]
    public void GalvanicCouple_WithAdditionalReactions_TotalCurrentAtMixedPotential_IsZero()
    {
        var anode   = new Anode(MaterialRegistry.Zinc,   0.01, Electrolyte);
        var cathode = new Cathode(MaterialRegistry.Copper, 0.01, Electrolyte);

        // Add an ORR to the cathode (additional reduction reaction)
        var orr = new OxygenReductionReaction(
            exchangeCurrentDensity: 1e-5,
            pH: Electrolyte.pH,
            oxygenPartialPressure: 0.21,
            temperatureKelvin: Electrolyte.TemperatureKelvin);
        cathode.AddReaction(orr);

        var couple = new GalvanicCouple(anode, cathode, Electrolyte);

        double em = couple.MixedPotential;
        double totalCurrent =
            anode.Area * anode.ComputeCurrentDensity(em) +
            cathode.Area * cathode.ComputeCurrentDensity(em);

        Assert.Equal(0.0, totalCurrent, precision: 6);
    }

    // ── Conservation with metal dissolution on anode ──────────────────────────

    [Fact]
    public void GalvanicCouple_WithMetalDissolution_TotalCurrentAtMixedPotential_IsZero()
    {
        var anode   = new Anode(MaterialRegistry.Zinc,   0.01, Electrolyte);
        var cathode = new Cathode(MaterialRegistry.Copper, 0.01, Electrolyte);

        var dissolution = new MetalDissolutionReaction(
            exchangeCurrentDensity: 1e-4,
            standardPotential: MaterialRegistry.Zinc.StandardPotential,
            electronsTransferred: 2,
            metalIonConcentration: 1e-6,
            temperatureKelvin: Electrolyte.TemperatureKelvin);
        anode.AddReaction(dissolution);

        var couple = new GalvanicCouple(anode, cathode, Electrolyte);

        double em = couple.MixedPotential;
        double totalCurrent =
            anode.Area * anode.ComputeCurrentDensity(em) +
            cathode.Area * cathode.ComputeCurrentDensity(em);

        Assert.Equal(0.0, totalCurrent, precision: 6);
    }

    // ── Polarization curve crosses zero at the mixed potential ────────────────

    [Fact]
    public void GalvanicCouple_PolarizationCurve_CrossesZeroAtMixedPotential()
    {
        var anode   = new Anode(MaterialRegistry.Zinc,   0.01, Electrolyte);
        var cathode = new Cathode(MaterialRegistry.Copper, 0.01, Electrolyte);
        var couple  = new GalvanicCouple(anode, cathode, Electrolyte);

        double em    = couple.MixedPotential;
        double start = anode.OpenCircuitPotential - 0.05;
        double end   = cathode.OpenCircuitPotential + 0.05;
        var curve    = couple.PolarizationCurve(start, end, 200);

        // Find the two points that bracket the mixed potential
        (double Potential, double TotalCurrentDensity) prev = curve[0];
        bool foundCrossing = false;
        foreach (var point in curve.Skip(1))
        {
            if (prev.TotalCurrentDensity * point.TotalCurrentDensity <= 0.0 &&
                prev.Potential <= em + 0.01 && point.Potential >= em - 0.01)
            {
                foundCrossing = true;
                break;
            }
            prev = point;
        }

        Assert.True(foundCrossing, "Polarization curve should cross zero near the mixed potential.");
    }

    // ── Galvanic voltage is positive ──────────────────────────────────────────

    [Fact]
    public void GalvanicCouple_GalvanicVoltage_IsPositive()
    {
        var anode   = new Anode(MaterialRegistry.Zinc,   0.01, Electrolyte);
        var cathode = new Cathode(MaterialRegistry.Copper, 0.01, Electrolyte);
        var couple  = new GalvanicCouple(anode, cathode, Electrolyte);

        Assert.True(couple.GalvanicVoltage > 0.0,
            $"Galvanic voltage should be positive, got {couple.GalvanicVoltage}");
    }

    // ── Larger cathode → higher mixed potential ───────────────────────────────

    [Fact]
    public void GalvanicCouple_LargerCathodeArea_RaisesMixedPotential()
    {
        var anodeSmall = new Anode(MaterialRegistry.Zinc,   0.01, Electrolyte);
        var cathodeSm  = new Cathode(MaterialRegistry.Copper, 0.01, Electrolyte);
        var coupleSm   = new GalvanicCouple(anodeSmall, cathodeSm, Electrolyte);

        var anodeLarge = new Anode(MaterialRegistry.Zinc,   0.01, Electrolyte);
        var cathodeLg  = new Cathode(MaterialRegistry.Copper, 0.10, Electrolyte);
        var coupleLg   = new GalvanicCouple(anodeLarge, cathodeLg, Electrolyte);

        // A larger cathode-to-anode area ratio shifts E_m toward the cathode OCP
        Assert.True(coupleLg.MixedPotential > coupleSm.MixedPotential,
            "Larger cathode area should raise the mixed potential.");
    }
}
