using GCE.Core;

namespace GCE.Core.Tests;

// ── MaterialBase ──────────────────────────────────────────────────────────────

/// <summary>Minimal concrete subclass used only inside this test file.</summary>
file sealed class ConcreteMaterial(
    string name,
    double standardPotential,
    double exchangeCurrentDensity,
    double molarMass,
    int    electronsTransferred,
    double density)
    : MaterialBase(name, standardPotential, exchangeCurrentDensity, molarMass, electronsTransferred, density);

public class MaterialBaseTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var m = new ConcreteMaterial("TestMetal", -0.50, 1e-4, 0.05585, 2, 7874.0);

        Assert.Equal("TestMetal", m.Name);
        Assert.Equal(-0.50,   m.StandardPotential,       precision: 10);
        Assert.Equal(1e-4,    m.ExchangeCurrentDensity,  precision: 10);
        Assert.Equal(0.05585, m.MolarMass,                precision: 10);
        Assert.Equal(2,       m.ElectronsTransferred);
        Assert.Equal(7874.0,  m.Density,                  precision: 6);
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        var m = new ConcreteMaterial("TestMetal", 0.0, 1e-4, 0.05585, 2, 7874.0);
        Assert.Equal("TestMetal", m.ToString());
    }

    [Fact]
    public void IsAssignableToIMaterial()
    {
        IMaterial m = new ConcreteMaterial("TestMetal", 0.0, 1e-4, 0.05585, 2, 7874.0);
        Assert.NotNull(m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_WhenNameIsNullOrWhiteSpace(string name)
    {
        Assert.Throws<ArgumentException>(
            () => new ConcreteMaterial(name, 0.0, 1e-4, 0.05585, 2, 7874.0));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void Constructor_Throws_WhenExchangeCurrentDensityNotPositive(double i0)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ConcreteMaterial("M", 0.0, i0, 0.05585, 2, 7874.0));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void Constructor_Throws_WhenMolarMassNotPositive(double mm)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ConcreteMaterial("M", 0.0, 1e-4, mm, 2, 7874.0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_Throws_WhenElectronsTransferredNotPositive(int n)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ConcreteMaterial("M", 0.0, 1e-4, 0.05585, n, 7874.0));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void Constructor_Throws_WhenDensityNotPositive(double rho)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ConcreteMaterial("M", 0.0, 1e-4, 0.05585, 2, rho));
    }
}

// ── Alloy ─────────────────────────────────────────────────────────────────────

public class AlloyTests
{
    [Fact]
    public void Constructor_SetsAllProperties_IncludingDesignation()
    {
        var alloy = new Alloy("AA7075", -0.59, 1e-6, 0.02698, 3, 2810.0, "AA7075-T6");

        Assert.Equal("AA7075",    alloy.Name);
        Assert.Equal(-0.59,       alloy.StandardPotential,      precision: 10);
        Assert.Equal(1e-6,        alloy.ExchangeCurrentDensity, precision: 10);
        Assert.Equal(0.02698,     alloy.MolarMass,               precision: 10);
        Assert.Equal(3,           alloy.ElectronsTransferred);
        Assert.Equal(2810.0,      alloy.Density,                 precision: 6);
        Assert.Equal("AA7075-T6", alloy.Designation);
    }

    [Fact]
    public void Constructor_AllowsNullDesignation()
    {
        var alloy = new Alloy("GenericAlloy", -0.50, 1e-5, 0.05585, 2, 7800.0);
        Assert.Null(alloy.Designation);
    }

    [Fact]
    public void Alloy_IsAssignableToIMaterial()
    {
        IMaterial m = new Alloy("GenericAlloy", -0.50, 1e-5, 0.05585, 2, 7800.0);
        Assert.NotNull(m);
    }

    [Fact]
    public void Alloy_IsAssignableToMaterialBase()
    {
        MaterialBase m = new Alloy("GenericAlloy", -0.50, 1e-5, 0.05585, 2, 7800.0);
        Assert.NotNull(m);
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        var alloy = new Alloy("AZ31B", -1.67, 5e-6, 0.02430, 2, 1770.0, "ASTM AZ31B");
        Assert.Equal("AZ31B", alloy.ToString());
    }
}

// ── MaterialRegistry ──────────────────────────────────────────────────────────

public class MaterialRegistryTests
{
    // ── Pre-configured materials ───────────────────────────────────────────────

    [Theory]
    [InlineData("Zinc")]
    [InlineData("Mild Steel")]
    [InlineData("Aluminium")]
    [InlineData("Copper")]
    [InlineData("Nickel")]
    [InlineData("Magnesium")]
    [InlineData("AA7075")]
    [InlineData("SS316L")]
    [InlineData("AZ31B")]
    public void Get_ReturnsMaterial_ForAllPreConfiguredNames(string name)
    {
        var material = MaterialRegistry.Get(name);
        Assert.Equal(name, material.Name, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Get_IsCaseInsensitive()
    {
        var lower = MaterialRegistry.Get("zinc");
        var upper = MaterialRegistry.Get("ZINC");
        Assert.Same(lower, upper);
    }

    [Fact]
    public void Get_Throws_ForUnknownName()
    {
        Assert.Throws<KeyNotFoundException>(
            () => MaterialRegistry.Get("__no_such_material__"));
    }

    [Fact]
    public void TryGet_ReturnsTrue_ForKnownMaterial()
    {
        bool found = MaterialRegistry.TryGet("Copper", out IMaterial? m);
        Assert.True(found);
        Assert.NotNull(m);
    }

    [Fact]
    public void TryGet_ReturnsFalse_ForUnknownMaterial()
    {
        bool found = MaterialRegistry.TryGet("__unknown__", out IMaterial? m);
        Assert.False(found);
        Assert.Null(m);
    }

    [Fact]
    public void TryGet_IsCaseInsensitive()
    {
        bool found = MaterialRegistry.TryGet("aluminium", out IMaterial? m);
        Assert.True(found);
        Assert.NotNull(m);
    }

    // ── Runtime registration ───────────────────────────────────────────────────

    [Fact]
    public void Register_MakesNewMaterialRetrievable()
    {
        const string name = "__TestMaterial_Register__";
        var custom = new Material(name, -0.30, 1e-5, 0.05585, 2, 7800.0);

        MaterialRegistry.Register(custom);

        var retrieved = MaterialRegistry.Get(name);
        Assert.Equal(name, retrieved.Name);
    }

    [Fact]
    public void Register_ReplacesExistingMaterialWithSameName()
    {
        const string name = "__TestMaterial_Replace__";
        var first  = new Material(name, -0.30, 1e-5, 0.05585, 2, 7800.0);
        var second = new Material(name, -0.40, 2e-5, 0.05585, 2, 7800.0);

        MaterialRegistry.Register(first);
        MaterialRegistry.Register(second);

        var retrieved = MaterialRegistry.Get(name);
        Assert.Equal(-0.40, retrieved.StandardPotential, precision: 10);
    }

    [Fact]
    public void Register_ReturnsSameMaterialInstance()
    {
        var custom = new Material("__TestMaterial_Return__", -0.30, 1e-5, 0.05585, 2, 7800.0);
        var result = MaterialRegistry.Register(custom);
        Assert.Same(custom, result);
    }

    [Fact]
    public void Register_AllowsAlloySubclass()
    {
        const string name = "__TestAlloy_Register__";
        var alloy = new Alloy(name, -0.59, 1e-6, 0.02698, 3, 2810.0, "Custom-T6");

        MaterialRegistry.Register(alloy);

        var retrieved = MaterialRegistry.Get(name);
        Assert.IsType<Alloy>(retrieved);
        Assert.Equal("Custom-T6", ((Alloy)retrieved).Designation);
    }

    // ── RegisteredNames ────────────────────────────────────────────────────────

    [Fact]
    public void RegisteredNames_ContainsAllPreConfiguredMaterials()
    {
        var names = MaterialRegistry.RegisteredNames;

        Assert.Contains("Zinc",       names, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Mild Steel", names, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Aluminium",  names, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Copper",     names, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Nickel",     names, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Magnesium",  names, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("AA7075",     names, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("SS316L",     names, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("AZ31B",      names, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegisteredNames_IsUpdatedAfterRegistration()
    {
        const string name = "__TestMaterial_Names__";
        MaterialRegistry.Register(new Material(name, 0.0, 1e-5, 0.05585, 2, 7800.0));

        Assert.Contains(name, MaterialRegistry.RegisteredNames, StringComparer.OrdinalIgnoreCase);
    }

    // ── Built-in alloy properties ──────────────────────────────────────────────

    [Fact]
    public void AA7075_IsAlloy_WithCorrectDesignation()
    {
        Assert.IsType<Alloy>(MaterialRegistry.AA7075);
        Assert.Equal("AA7075-T6", ((Alloy)MaterialRegistry.AA7075).Designation);
    }

    [Fact]
    public void SS316L_IsAlloy_WithCorrectDesignation()
    {
        Assert.IsType<Alloy>(MaterialRegistry.SS316L);
        Assert.Equal("UNS S31603", ((Alloy)MaterialRegistry.SS316L).Designation);
    }

    [Fact]
    public void AZ31B_IsAlloy_WithCorrectDesignation()
    {
        Assert.IsType<Alloy>(MaterialRegistry.AZ31B);
        Assert.Equal("ASTM AZ31B", ((Alloy)MaterialRegistry.AZ31B).Designation);
    }

    [Fact]
    public void AA7075_StandardPotential_IsMoreNegativeThanCopper()
    {
        // AA7075 (≈ −0.59 V) should be anodic relative to copper (+0.34 V).
        Assert.True(MaterialRegistry.AA7075.StandardPotential <
                    MaterialRegistry.Copper.StandardPotential);
    }

    [Fact]
    public void AZ31B_StandardPotential_IsMoreNegativeThanZinc()
    {
        // AZ31B (≈ −1.67 V) should be more anodic than zinc (−0.76 V).
        Assert.True(MaterialRegistry.AZ31B.StandardPotential <
                    MaterialRegistry.Zinc.StandardPotential);
    }

    [Fact]
    public void SS316L_StandardPotential_IsMorePositiveThanMildSteel()
    {
        // SS316L (≈ +0.10 V passive) should be cathodic relative to mild steel (−0.44 V).
        Assert.True(MaterialRegistry.SS316L.StandardPotential >
                    MaterialRegistry.MildSteel.StandardPotential);
    }
}
