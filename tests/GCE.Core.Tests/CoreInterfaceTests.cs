using GCE.Atmosphere;
using GCE.Core;
using GCE.Electrochemistry;

namespace GCE.Core.Tests;

// ── IElectrode / Electrode ────────────────────────────────────────────────────

public class ElectrodeTests
{
    private static readonly IMaterial Zinc   = MaterialRegistry.Zinc;
    private static readonly IMaterial Copper = MaterialRegistry.Copper;

    [Fact]
    public void Electrode_StoresMaterialAndArea()
    {
        var electrode = new Electrode(Zinc, 0.01);
        Assert.Equal(Zinc, electrode.Material);
        Assert.Equal(0.01, electrode.Area);
    }

    [Fact]
    public void Electrode_IsAssignableToIElectrode()
    {
        IElectrode electrode = new Electrode(Zinc, 0.05);
        Assert.Equal(Zinc, electrode.Material);
        Assert.Equal(0.05, electrode.Area);
    }

    [Fact]
    public void Electrode_EqualityBasedOnMaterialAndArea()
    {
        var a = new Electrode(Zinc, 0.01);
        var b = new Electrode(Zinc, 0.01);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Electrode_InequalityWhenAreaDiffers()
    {
        var a = new Electrode(Zinc, 0.01);
        var b = new Electrode(Zinc, 0.02);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Electrode_InequalityWhenMaterialDiffers()
    {
        var a = new Electrode(Zinc,   0.01);
        var b = new Electrode(Copper, 0.01);
        Assert.NotEqual(a, b);
    }
}

// ── IElectrolyte / AtmosphericConditions ─────────────────────────────────────

public class ElectrolyteTests
{
    [Fact]
    public void AtmosphericConditions_IsAssignableToIElectrolyte()
    {
        IElectrolyte electrolyte = new AtmosphericConditions(25.0, 0.75, 0.1);
        Assert.NotNull(electrolyte);
    }

    [Fact]
    public void AtmosphericConditions_Concentration_EqualsChlorideConcentration()
    {
        IElectrolyte electrolyte = new AtmosphericConditions(25.0, 0.75, 0.1);
        Assert.Equal(0.1, electrolyte.Concentration, precision: 9);
    }

    [Fact]
    public void AtmosphericConditions_IElectrolyte_ExposesIEnvironmentProperties()
    {
        IElectrolyte electrolyte = new AtmosphericConditions(25.0, 0.75, 0.1);
        Assert.Equal(298.15, electrolyte.TemperatureKelvin, precision: 9);
        Assert.InRange(electrolyte.pH, 0.0, 14.0);
        Assert.True(electrolyte.IonicConductivity > 0.0);
    }

    [Fact]
    public void AtmosphericConditions_ZeroChloride_ZeroConcentration()
    {
        IElectrolyte electrolyte = new AtmosphericConditions(20.0, 0.6);
        Assert.Equal(0.0, electrolyte.Concentration, precision: 9);
    }
}

// ── IGalvanicCell / GalvanicCell ──────────────────────────────────────────────

public class GalvanicCellTests
{
    private static readonly IElectrode   ZincElectrode   = new Electrode(MaterialRegistry.Zinc,   0.05);
    private static readonly IElectrode   CopperElectrode = new Electrode(MaterialRegistry.Copper, 0.05);
    private static readonly IElectrolyte Env             = new AtmosphericConditions(25.0, 0.75, 0.1);

    [Fact]
    public void GalvanicCell_StoresElectrodesAndElectrolyte()
    {
        var cell = new GalvanicCell(ZincElectrode, CopperElectrode, Env);
        Assert.Equal(ZincElectrode,   cell.Anode);
        Assert.Equal(CopperElectrode, cell.Cathode);
        Assert.Equal(Env,             cell.Electrolyte);
    }

    [Fact]
    public void GalvanicCell_GalvanicVoltage_IsCathodePotentialMinusAnode()
    {
        var cell = new GalvanicCell(ZincElectrode, CopperElectrode, Env);
        double expected =
            MaterialRegistry.Copper.StandardPotential - MaterialRegistry.Zinc.StandardPotential;
        Assert.Equal(expected, cell.GalvanicVoltage, precision: 9);
    }

    [Fact]
    public void GalvanicCell_IsAssignableToIGalvanicCell()
    {
        IGalvanicCell cell = new GalvanicCell(ZincElectrode, CopperElectrode, Env);
        Assert.NotNull(cell.Anode);
        Assert.NotNull(cell.Cathode);
        Assert.NotNull(cell.Electrolyte);
        Assert.True(cell.GalvanicVoltage > 0.0);
    }

    [Fact]
    public void GalvanicCell_ThrowsWhenCathodeNotMoreNoble()
    {
        // Cathode (Zinc) has lower potential than Anode (Copper) — invalid
        Assert.Throws<ArgumentException>(() =>
            new GalvanicCell(CopperElectrode, ZincElectrode, Env));
    }

    [Fact]
    public void GalvanicCell_ThrowsWhenBothElectrodesSameMaterial()
    {
        var zinc2 = new Electrode(MaterialRegistry.Zinc, 0.05);
        Assert.Throws<ArgumentException>(() =>
            new GalvanicCell(ZincElectrode, zinc2, Env));
    }
}
