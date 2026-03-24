using GCE.Core;
using GCE.Electrochemistry;

namespace GCE.Core.Tests;

// ── OxygenReductionReaction tests ─────────────────────────────────────────────

public class OxygenReductionReactionTests
{
    private const double I0 = 1e-4;   // exchange current density (A/m²)
    private const double T = 298.15;  // room temperature (K)

    private static OxygenReductionReaction MakeDefault(
        double pH = 7.0, OrrPathway pathway = OrrPathway.FourElectron,
        double? limitingCurrent = null) =>
        new(I0, pH, oxygenPartialPressure: 0.21, pathway: pathway,
            temperatureKelvin: T, limitingCurrentDensity: limitingCurrent);

    // ── Stored properties ─────────────────────────────────────────────────────

    [Fact]
    public void Orr_Properties_StoredCorrectly()
    {
        var orr = new OxygenReductionReaction(I0, pH: 4.0, oxygenPartialPressure: 0.5,
            pathway: OrrPathway.TwoElectron, temperatureKelvin: 310.0);
        Assert.Equal(OrrPathway.TwoElectron, orr.Pathway);
        Assert.Equal(4.0, orr.pH);
        Assert.Equal(0.5, orr.OxygenPartialPressure);
    }

    [Fact]
    public void Orr_ImplementsIElectrochemicalReaction()
    {
        IElectrochemicalReaction rxn = MakeDefault();
        // Current at a potential far below E_eq should be negative (cathodic)
        Assert.True(rxn.CurrentDensity(-0.5) < 0.0);
    }

    // ── Equilibrium potential ─────────────────────────────────────────────────

    [Fact]
    public void Orr_CurrentDensity_IsZeroAtEquilibriumPotential()
    {
        var orr = MakeDefault(pH: 7.0);
        Assert.Equal(0.0, orr.CurrentDensity(orr.EquilibriumPotential), precision: 12);
    }

    [Fact]
    public void Orr_FourElectron_EquilibriumPotential_AtStandardConditions()
    {
        // pH 0, pO2 = 1 atm → E_eq should equal E° = 1.229 V
        var orr = new OxygenReductionReaction(I0, pH: 0.0, oxygenPartialPressure: 1.0,
            pathway: OrrPathway.FourElectron, temperatureKelvin: T);
        Assert.Equal(1.229, orr.EquilibriumPotential, precision: 3);
    }

    [Fact]
    public void Orr_TwoElectron_EquilibriumPotential_AtStandardConditions()
    {
        // pH 0, pO2 = 1 atm → E_eq should equal E° = 0.695 V
        var orr = new OxygenReductionReaction(I0, pH: 0.0, oxygenPartialPressure: 1.0,
            pathway: OrrPathway.TwoElectron, temperatureKelvin: T);
        Assert.Equal(0.695, orr.EquilibriumPotential, precision: 3);
    }

    [Fact]
    public void Orr_FourElectron_EquilibriumPotential_IsHigherThan_TwoElectron()
    {
        var orr4 = MakeDefault(pathway: OrrPathway.FourElectron);
        var orr2 = MakeDefault(pathway: OrrPathway.TwoElectron);
        Assert.True(orr4.EquilibriumPotential > orr2.EquilibriumPotential,
            "4-electron pathway must have a higher equilibrium potential than 2-electron.");
    }

    [Fact]
    public void Orr_HigherPh_LowersEquilibriumPotential()
    {
        var orrLowPh = MakeDefault(pH: 0.0);
        var orrHighPh = MakeDefault(pH: 7.0);
        Assert.True(orrLowPh.EquilibriumPotential > orrHighPh.EquilibriumPotential,
            "Higher pH should give a lower equilibrium potential (Nernst).");
    }

    [Fact]
    public void Orr_HigherPO2_RaisesEquilibriumPotential()
    {
        var orrLowO2 = new OxygenReductionReaction(I0, pH: 7.0, oxygenPartialPressure: 0.05, temperatureKelvin: T);
        var orrHighO2 = new OxygenReductionReaction(I0, pH: 7.0, oxygenPartialPressure: 1.0, temperatureKelvin: T);
        Assert.True(orrHighO2.EquilibriumPotential > orrLowO2.EquilibriumPotential,
            "Higher pO₂ should give a higher equilibrium potential (Nernst).");
    }

    // ── Sign convention ───────────────────────────────────────────────────────

    [Fact]
    public void Orr_CurrentDensity_IsNegativeBelowEquilibriumPotential()
    {
        var orr = MakeDefault();
        double i = orr.CurrentDensity(orr.EquilibriumPotential - 0.2);
        Assert.True(i < 0.0, $"Expected cathodic (negative) current below E_eq, got {i}");
    }

    [Fact]
    public void Orr_CurrentDensity_IsPositiveAboveEquilibriumPotential()
    {
        var orr = MakeDefault();
        double i = orr.CurrentDensity(orr.EquilibriumPotential + 0.2);
        Assert.True(i > 0.0, $"Expected anodic (positive) current above E_eq, got {i}");
    }

    // ── Limiting current ──────────────────────────────────────────────────────

    [Fact]
    public void Orr_WithLimitingCurrent_CathodicCurrentIsBounded()
    {
        double iLim = 5.0; // A/m²
        var orr = MakeDefault(limitingCurrent: iLim);

        // At a very negative potential the cathodic current should not exceed iLim
        double i = orr.CurrentDensity(orr.EquilibriumPotential - 5.0);
        Assert.True(i >= -iLim, $"Cathodic current {i} exceeded limiting current {-iLim}");
    }

    // ── Integration with Cathode ───────────────────────────────────────────────

    [Fact]
    public void Orr_CanBeRegisteredOnCathode()
    {
        var env = new GCE.Atmosphere.AtmosphericConditions(25.0, 0.75, 0.01);
        var cathode = new Cathode(MaterialRegistry.Copper, 0.01, env);
        var orr = MakeDefault(pH: env.pH);
        cathode.AddReaction(orr);
        Assert.Single(cathode.AdditionalReactions);
    }

    // ── Input validation ──────────────────────────────────────────────────────

    [Fact]
    public void Orr_Constructor_ThrowsOnNonPositiveOxygenPressure()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OxygenReductionReaction(I0, oxygenPartialPressure: 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OxygenReductionReaction(I0, oxygenPartialPressure: -0.1));
    }

    [Fact]
    public void Orr_Constructor_ThrowsOnNonPositiveTemperature()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OxygenReductionReaction(I0, temperatureKelvin: 0.0));
    }

    [Fact]
    public void Orr_Constructor_ThrowsOnNonPositiveExchangeCurrent()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OxygenReductionReaction(0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OxygenReductionReaction(-1e-4));
    }
}

// ── HydrogenEvolutionReaction tests ──────────────────────────────────────────

public class HydrogenEvolutionReactionTests
{
    private const double I0 = 1e-4;
    private const double T = 298.15;

    private static HydrogenEvolutionReaction MakeDefault(double pH = 0.0) =>
        new(I0, pH: pH, temperatureKelvin: T);

    // ── Stored properties ─────────────────────────────────────────────────────

    [Fact]
    public void Her_Properties_StoredCorrectly()
    {
        var her = new HydrogenEvolutionReaction(I0, pH: 3.0, hydrogenPartialPressure: 0.5, temperatureKelvin: 310.0);
        Assert.Equal(3.0, her.pH);
        Assert.Equal(0.5, her.HydrogenPartialPressure);
    }

    [Fact]
    public void Her_ImplementsIElectrochemicalReaction()
    {
        IElectrochemicalReaction rxn = MakeDefault();
        // Potential below E_eq (0 V at pH 0) should produce cathodic (negative) current
        Assert.True(rxn.CurrentDensity(-0.5) < 0.0);
    }

    // ── Equilibrium potential ─────────────────────────────────────────────────

    [Fact]
    public void Her_CurrentDensity_IsZeroAtEquilibriumPotential()
    {
        var her = MakeDefault(pH: 7.0);
        Assert.Equal(0.0, her.CurrentDensity(her.EquilibriumPotential), precision: 12);
    }

    [Fact]
    public void Her_AtPh0_EquilibriumPotential_IsZero()
    {
        // pH=0, pH2=1atm → E_eq = 0.000 V (definition of SHE)
        var her = new HydrogenEvolutionReaction(I0, pH: 0.0, hydrogenPartialPressure: 1.0, temperatureKelvin: T);
        Assert.Equal(0.0, her.EquilibriumPotential, precision: 10);
    }

    [Fact]
    public void Her_HigherPh_LowersEquilibriumPotential()
    {
        var herLow = MakeDefault(pH: 0.0);
        var herHigh = MakeDefault(pH: 7.0);
        Assert.True(herLow.EquilibriumPotential > herHigh.EquilibriumPotential,
            "Higher pH should shift E_eq more negative (Nernst).");
    }

    [Fact]
    public void Her_EquilibriumPotential_NernstSlope_IsApproximately_59mV_PerPh()
    {
        // At 25 °C: ΔE_eq / ΔpH ≈ −59.16 mV/pH unit
        var her0 = new HydrogenEvolutionReaction(I0, pH: 0.0, temperatureKelvin: T);
        var her1 = new HydrogenEvolutionReaction(I0, pH: 1.0, temperatureKelvin: T);
        double slope = (her1.EquilibriumPotential - her0.EquilibriumPotential) / 1.0;
        double expectedSlope = -(PhysicalConstants.GasConstant * T / PhysicalConstants.Faraday) * Math.Log(10);
        Assert.Equal(expectedSlope, slope, precision: 8);
    }

    [Fact]
    public void Her_HigherHydrogenPressure_LowersEquilibriumPotential()
    {
        var herLowP = new HydrogenEvolutionReaction(I0, pH: 0.0, hydrogenPartialPressure: 0.1, temperatureKelvin: T);
        var herHighP = new HydrogenEvolutionReaction(I0, pH: 0.0, hydrogenPartialPressure: 10.0, temperatureKelvin: T);
        Assert.True(herLowP.EquilibriumPotential > herHighP.EquilibriumPotential,
            "Higher pH₂ pressure should shift E_eq more negative (Nernst).");
    }

    // ── Sign convention ───────────────────────────────────────────────────────

    [Fact]
    public void Her_CurrentDensity_IsNegativeBelowEquilibriumPotential()
    {
        var her = MakeDefault();
        double i = her.CurrentDensity(her.EquilibriumPotential - 0.2);
        Assert.True(i < 0.0, $"Expected cathodic (negative) current below E_eq, got {i}");
    }

    [Fact]
    public void Her_CurrentDensity_IsPositiveAboveEquilibriumPotential()
    {
        var her = MakeDefault();
        double i = her.CurrentDensity(her.EquilibriumPotential + 0.2);
        Assert.True(i > 0.0, $"Expected anodic (positive) current above E_eq, got {i}");
    }

    // ── Limiting current ──────────────────────────────────────────────────────

    [Fact]
    public void Her_WithLimitingCurrent_CathodicCurrentIsBounded()
    {
        double iLim = 2.0;
        var her = new HydrogenEvolutionReaction(I0, pH: 0.0, limitingCurrentDensity: iLim, temperatureKelvin: T);
        double i = her.CurrentDensity(her.EquilibriumPotential - 5.0);
        Assert.True(i >= -iLim, $"Cathodic current {i} exceeded limiting current {-iLim}");
    }

    // ── Integration with Cathode ───────────────────────────────────────────────

    [Fact]
    public void Her_CanBeRegisteredOnCathode()
    {
        var env = new GCE.Atmosphere.AtmosphericConditions(25.0, 0.75, 0.1);
        var cathode = new Cathode(MaterialRegistry.Copper, 0.01, env);
        cathode.AddReaction(MakeDefault(pH: env.pH));
        Assert.Single(cathode.AdditionalReactions);
    }

    // ── Input validation ──────────────────────────────────────────────────────

    [Fact]
    public void Her_Constructor_ThrowsOnNonPositiveHydrogenPressure()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new HydrogenEvolutionReaction(I0, hydrogenPartialPressure: 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new HydrogenEvolutionReaction(I0, hydrogenPartialPressure: -1.0));
    }

    [Fact]
    public void Her_Constructor_ThrowsOnNonPositiveTemperature()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new HydrogenEvolutionReaction(I0, temperatureKelvin: 0.0));
    }

    [Fact]
    public void Her_Constructor_ThrowsOnNonPositiveExchangeCurrent()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HydrogenEvolutionReaction(0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HydrogenEvolutionReaction(-1e-4));
    }
}

// ── MetalDissolutionReaction tests ────────────────────────────────────────────

public class MetalDissolutionReactionTests
{
    private const double I0 = 1e-3;
    private const double T = 298.15;
    private const double E0Zn = -0.76; // Zn²⁺/Zn standard potential

    private static MetalDissolutionReaction MakeZinc(double concentration = 1e-6) =>
        new(I0, E0Zn, electronsTransferred: 2, metalIonConcentration: concentration,
            temperatureKelvin: T);

    // ── Stored properties ─────────────────────────────────────────────────────

    [Fact]
    public void MetalDissolution_Properties_StoredCorrectly()
    {
        var rxn = new MetalDissolutionReaction(I0, E0Zn, electronsTransferred: 2,
            metalIonConcentration: 1e-3, temperatureKelvin: 310.0);
        Assert.Equal(E0Zn, rxn.StandardPotential);
        Assert.Equal(2, rxn.ElectronsTransferred);
        Assert.Equal(1e-3, rxn.MetalIonConcentration);
    }

    [Fact]
    public void MetalDissolution_ImplementsIElectrochemicalReaction()
    {
        IElectrochemicalReaction rxn = MakeZinc();
        // Above standard potential (–0.76 V) dissolution current should be positive
        Assert.True(rxn.CurrentDensity(-0.5) > 0.0);
    }

    // ── Equilibrium potential ─────────────────────────────────────────────────

    [Fact]
    public void MetalDissolution_CurrentDensity_IsZeroAtEquilibriumPotential()
    {
        var rxn = MakeZinc();
        Assert.Equal(0.0, rxn.CurrentDensity(rxn.EquilibriumPotential), precision: 12);
    }

    [Fact]
    public void MetalDissolution_EquilibriumPotential_AtUnitActivity_EqualsStandardPotential()
    {
        // [Mⁿ⁺] = 1 mol/L → ln(1) = 0 → E_eq = E°
        var rxn = new MetalDissolutionReaction(I0, E0Zn, electronsTransferred: 2,
            metalIonConcentration: 1.0, temperatureKelvin: T);
        Assert.Equal(E0Zn, rxn.EquilibriumPotential, precision: 10);
    }

    [Fact]
    public void MetalDissolution_HigherIonConcentration_RaisesEquilibriumPotential()
    {
        var rxnDilute = MakeZinc(concentration: 1e-6);
        var rxnConcentrated = MakeZinc(concentration: 1e-1);
        Assert.True(rxnConcentrated.EquilibriumPotential > rxnDilute.EquilibriumPotential,
            "Higher metal-ion concentration should give a higher equilibrium potential (Nernst).");
    }

    [Fact]
    public void MetalDissolution_NernstShift_IsCorrect()
    {
        // For n=2 at 25 °C: ΔE_eq per decade of [M²⁺] = RT/(2F)·ln(10) ≈ 29.58 mV
        var rxn1 = new MetalDissolutionReaction(I0, E0Zn, electronsTransferred: 2,
            metalIonConcentration: 1e-2, temperatureKelvin: T);
        var rxn2 = new MetalDissolutionReaction(I0, E0Zn, electronsTransferred: 2,
            metalIonConcentration: 1e-3, temperatureKelvin: T);
        double deltaE = rxn1.EquilibriumPotential - rxn2.EquilibriumPotential;
        double expected = PhysicalConstants.GasConstant * T
                          / (2 * PhysicalConstants.Faraday) * Math.Log(10);
        Assert.Equal(expected, deltaE, precision: 8);
    }

    // ── Sign convention ───────────────────────────────────────────────────────

    [Fact]
    public void MetalDissolution_CurrentDensity_IsPositiveAboveEquilibriumPotential()
    {
        var rxn = MakeZinc();
        double i = rxn.CurrentDensity(rxn.EquilibriumPotential + 0.1);
        Assert.True(i > 0.0, $"Expected anodic (positive) dissolution current above E_eq, got {i}");
    }

    [Fact]
    public void MetalDissolution_CurrentDensity_IsNegativeBelowEquilibriumPotential()
    {
        var rxn = MakeZinc();
        double i = rxn.CurrentDensity(rxn.EquilibriumPotential - 0.1);
        Assert.True(i < 0.0, $"Expected cathodic (negative) deposition current below E_eq, got {i}");
    }

    // ── Integration with Anode ────────────────────────────────────────────────

    [Fact]
    public void MetalDissolution_CanBeRegisteredOnAnode()
    {
        var env = new GCE.Atmosphere.AtmosphericConditions(25.0, 0.75, 0.1);
        var anode = new Anode(MaterialRegistry.Zinc, 0.01, env);
        var rxn = MakeZinc();
        anode.AddReaction(rxn);
        Assert.Single(anode.AdditionalReactions);
    }

    [Fact]
    public void MetalDissolution_AddsToAnodeCurrentDensity()
    {
        var env = new GCE.Atmosphere.AtmosphericConditions(25.0, 0.75, 0.1);
        var anode = new Anode(MaterialRegistry.Zinc, 0.01, env);
        double baseI = anode.ComputeCurrentDensity(0.0);

        var rxn = MakeZinc();
        double rxnI = rxn.CurrentDensity(0.0);
        anode.AddReaction(rxn);

        Assert.Equal(baseI + rxnI, anode.ComputeCurrentDensity(0.0), precision: 10);
    }

    // ── Input validation ──────────────────────────────────────────────────────

    [Fact]
    public void MetalDissolution_Constructor_ThrowsOnInvalidElectronsTransferred()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MetalDissolutionReaction(I0, E0Zn, electronsTransferred: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MetalDissolutionReaction(I0, E0Zn, electronsTransferred: -1));
    }

    [Fact]
    public void MetalDissolution_Constructor_ThrowsOnNonPositiveConcentration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MetalDissolutionReaction(I0, E0Zn, metalIonConcentration: 0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MetalDissolutionReaction(I0, E0Zn, metalIonConcentration: -1e-6));
    }

    [Fact]
    public void MetalDissolution_Constructor_ThrowsOnNonPositiveTemperature()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MetalDissolutionReaction(I0, E0Zn, temperatureKelvin: 0.0));
    }

    [Fact]
    public void MetalDissolution_Constructor_ThrowsOnNonPositiveExchangeCurrent()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MetalDissolutionReaction(0.0, E0Zn));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MetalDissolutionReaction(-1e-3, E0Zn));
    }
}
