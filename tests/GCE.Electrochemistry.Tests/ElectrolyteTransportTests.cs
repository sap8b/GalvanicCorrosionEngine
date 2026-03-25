using GCE.Core;
using GCE.Electrochemistry;
using GCE.Numerics.Solvers;

namespace GCE.Electrochemistry.Tests;

// ── Species tests ──────────────────────────────────────────────────────────────

public class SpeciesTests
{
    // ── Constructor – stored properties ───────────────────────────────────────

    [Fact]
    public void Species_Properties_StoredCorrectly()
    {
        var s = new Species("Na+", charge: 1, diffusionCoefficient: 1.33e-9, concentration: 100.0);
        Assert.Equal("Na+", s.Name);
        Assert.Equal(1, s.Charge);
        Assert.Equal(1.33e-9, s.DiffusionCoefficient);
        Assert.Equal(100.0, s.Concentration);
    }

    [Fact]
    public void Species_DefaultConcentration_IsZero()
    {
        var s = new Species("Cl-", charge: -1, diffusionCoefficient: 2.03e-9);
        Assert.Equal(0.0, s.Concentration);
    }

    [Fact]
    public void Species_ZeroCharge_AllowedForNeutralGas()
    {
        var s = new Species("O2", charge: 0, diffusionCoefficient: 1.97e-9, concentration: 0.25);
        Assert.Equal(0, s.Charge);
        Assert.Equal(0.25, s.Concentration);
    }

    // ── Concentration setter ───────────────────────────────────────────────────

    [Fact]
    public void Species_SetConcentration_UpdatesValue()
    {
        var s = new Species("H+", charge: 1, diffusionCoefficient: 9.31e-9, concentration: 10.0);
        s.Concentration = 50.0;
        Assert.Equal(50.0, s.Concentration);
    }

    [Fact]
    public void Species_SetConcentration_Zero_IsAllowed()
    {
        var s = new Species("H+", charge: 1, diffusionCoefficient: 9.31e-9, concentration: 10.0);
        s.Concentration = 0.0;
        Assert.Equal(0.0, s.Concentration);
    }

    // ── WithConcentration factory ──────────────────────────────────────────────

    [Fact]
    public void Species_WithConcentration_ReturnsNewInstance()
    {
        var original = new Species("Na+", charge: 1, diffusionCoefficient: 1.33e-9, concentration: 50.0);
        var clone    = original.WithConcentration(200.0);

        Assert.NotSame(original, clone);
        Assert.Equal(original.Name,                clone.Name);
        Assert.Equal(original.Charge,              clone.Charge);
        Assert.Equal(original.DiffusionCoefficient, clone.DiffusionCoefficient);
        Assert.Equal(200.0,                         clone.Concentration);
        // Original unchanged
        Assert.Equal(50.0, original.Concentration);
    }

    // ── Input validation ───────────────────────────────────────────────────────

    [Fact]
    public void Species_Constructor_ThrowsOnNullOrWhitespaceName()
    {
        Assert.Throws<ArgumentException>(() => new Species(null!, 1, 1e-9));
        Assert.Throws<ArgumentException>(() => new Species("",   1, 1e-9));
        Assert.Throws<ArgumentException>(() => new Species("  ", 1, 1e-9));
    }

    [Fact]
    public void Species_Constructor_ThrowsOnNegativeDiffusionCoefficient()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Species("Na+", 1, -1e-9));
    }

    [Fact]
    public void Species_Constructor_ThrowsOnNegativeConcentration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Species("Na+", 1, 1e-9, -1.0));
    }

    [Fact]
    public void Species_SetConcentration_ThrowsOnNegativeValue()
    {
        var s = new Species("Na+", 1, 1e-9, 10.0);
        Assert.Throws<ArgumentOutOfRangeException>(() => s.Concentration = -0.001);
    }
}

// ── BulkElectrolyte tests ─────────────────────────────────────────────────────

public class BulkElectrolyteTests
{
    // ── Default construction ───────────────────────────────────────────────────

    [Fact]
    public void BulkElectrolyte_DefaultPh_IsNeutral_WhenNoSpecies()
    {
        var elec = new BulkElectrolyte(temperatureCelsius: 25.0);
        Assert.Equal(7.0, elec.pH);
    }

    [Fact]
    public void BulkElectrolyte_ImplementsIElectrolyte()
    {
        IElectrolyte e = new BulkElectrolyte();
        Assert.True(e.IonicConductivity >= 0.0);
    }

    [Fact]
    public void BulkElectrolyte_TemperatureKelvin_CorrectConversion()
    {
        var elec = new BulkElectrolyte(temperatureCelsius: 25.0);
        Assert.Equal(298.15, elec.TemperatureKelvin, precision: 5);
    }

    // ── pH from H+ species ────────────────────────────────────────────────────

    [Fact]
    public void BulkElectrolyte_Ph_CalculatedFromHydrogenIon()
    {
        var elec  = new BulkElectrolyte();
        // [H+] = 1e-4 mol/L = 0.1 mol/m³ → pH = 4.0
        var hPlus = new Species("H+", charge: 1, diffusionCoefficient: 9.31e-9, concentration: 0.1);
        elec.AddSpecies(hPlus);

        Assert.Equal(4.0, elec.pH, precision: 5);
    }

    [Fact]
    public void BulkElectrolyte_Ph_NeutralAt1e7_MolPerLiter()
    {
        var elec  = new BulkElectrolyte();
        // [H+] = 1e-7 mol/L = 1e-4 mol/m³ → pH = 7.0
        var hPlus = new Species("H+", charge: 1, diffusionCoefficient: 9.31e-9, concentration: 1e-4);
        elec.AddSpecies(hPlus);

        Assert.Equal(7.0, elec.pH, precision: 5);
    }

    // ── Conductivity ──────────────────────────────────────────────────────────

    [Fact]
    public void BulkElectrolyte_Conductivity_IsPositive_WithFallbackConcentration()
    {
        var elec = new BulkElectrolyte(totalConcentration: 1000.0);
        Assert.True(elec.IonicConductivity > 0.0);
    }

    [Fact]
    public void BulkElectrolyte_Conductivity_IsZero_WhenConcentrationIsZero()
    {
        var elec = new BulkElectrolyte(totalConcentration: 0.0);
        Assert.Equal(0.0, elec.IonicConductivity, precision: 10);
    }

    [Fact]
    public void BulkElectrolyte_Conductivity_IncreasesWithConcentration()
    {
        var low  = new BulkElectrolyte(totalConcentration: 10.0);
        var high = new BulkElectrolyte(totalConcentration: 1000.0);
        Assert.True(high.IonicConductivity > low.IonicConductivity);
    }

    [Fact]
    public void BulkElectrolyte_Conductivity_WithSpecies_IsPositive()
    {
        var elec = new BulkElectrolyte();
        elec.AddSpecies(new Species("Na+", 1, 1.33e-9, 500.0));
        elec.AddSpecies(new Species("Cl-", -1, 2.03e-9, 500.0));

        Assert.True(elec.IonicConductivity > 0.0);
    }

    // ── Species registration ──────────────────────────────────────────────────

    [Fact]
    public void BulkElectrolyte_AddSpecies_AppearsInList()
    {
        var elec = new BulkElectrolyte();
        var s    = new Species("Na+", 1, 1.33e-9, 100.0);
        elec.AddSpecies(s);

        Assert.Single(elec.Species);
        Assert.Same(s, elec.Species[0]);
    }

    [Fact]
    public void BulkElectrolyte_Concentration_ReturnsCationSum_WhenSpeciesPresent()
    {
        var elec = new BulkElectrolyte();
        elec.AddSpecies(new Species("Na+", 1, 1.33e-9, 300.0));
        elec.AddSpecies(new Species("Cl-", -1, 2.03e-9, 300.0));

        // Concentration should return cation (Na+) sum = 300
        Assert.Equal(300.0, elec.Concentration, precision: 6);
    }

    // ── Input validation ───────────────────────────────────────────────────────

    [Fact]
    public void BulkElectrolyte_Constructor_ThrowsOnNegativeConcentration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BulkElectrolyte(totalConcentration: -1.0));
    }

    [Fact]
    public void BulkElectrolyte_AddSpecies_ThrowsOnNull()
    {
        var elec = new BulkElectrolyte();
        Assert.Throws<ArgumentNullException>(() => elec.AddSpecies(null!));
    }
}

// ── ThinFilmElectrolyte tests ─────────────────────────────────────────────────

public class ThinFilmElectrolyteTests
{
    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void ThinFilm_Properties_StoredCorrectly()
    {
        var film = new ThinFilmElectrolyte(filmThickness: 1e-5, temperatureCelsius: 20.0,
                                           totalConcentration: 500.0);
        Assert.Equal(1e-5, film.FilmThickness);
        Assert.Equal(20.0, film.TemperatureCelsius);
        Assert.Equal(500.0, film.TotalConcentration);
    }

    [Fact]
    public void ThinFilm_ImplementsIElectrolyte()
    {
        IElectrolyte e = new ThinFilmElectrolyte(1e-6);
        Assert.True(e.IonicConductivity >= 0.0);
    }

    [Fact]
    public void ThinFilm_TemperatureKelvin_CorrectConversion()
    {
        var film = new ThinFilmElectrolyte(1e-6, temperatureCelsius: 25.0);
        Assert.Equal(298.15, film.TemperatureKelvin, precision: 5);
    }

    // ── pH from H+ species ────────────────────────────────────────────────────

    [Fact]
    public void ThinFilm_DefaultPh_IsNeutral_WhenNoSpecies()
    {
        var film = new ThinFilmElectrolyte(1e-6);
        Assert.Equal(7.0, film.pH);
    }

    [Fact]
    public void ThinFilm_Ph_CalculatedFromHydrogenIon()
    {
        var film  = new ThinFilmElectrolyte(1e-6);
        // [H+] = 1e-3 mol/L = 1 mol/m³ → pH = 3.0
        var hPlus = new Species("H+", charge: 1, diffusionCoefficient: 9.31e-9, concentration: 1.0);
        film.AddSpecies(hPlus);

        Assert.Equal(3.0, film.pH, precision: 5);
    }

    // ── Conductivity ──────────────────────────────────────────────────────────

    [Fact]
    public void ThinFilm_Conductivity_IsPositive_WithFallbackConcentration()
    {
        var film = new ThinFilmElectrolyte(1e-6, totalConcentration: 100.0);
        Assert.True(film.IonicConductivity > 0.0);
    }

    [Fact]
    public void ThinFilm_Conductivity_IsZero_WhenConcentrationIsZero()
    {
        var film = new ThinFilmElectrolyte(1e-6, totalConcentration: 0.0);
        Assert.Equal(0.0, film.IonicConductivity, precision: 10);
    }

    [Fact]
    public void ThinFilm_Conductivity_IncreasesWithConcentration()
    {
        var low  = new ThinFilmElectrolyte(1e-6, totalConcentration: 10.0);
        var high = new ThinFilmElectrolyte(1e-6, totalConcentration: 1000.0);
        Assert.True(high.IonicConductivity > low.IonicConductivity);
    }

    [Fact]
    public void ThinFilm_Conductivity_WithNaClSpecies_IsPositive()
    {
        var film = new ThinFilmElectrolyte(1e-6);
        film.AddSpecies(new Species("Na+", 1, 1.33e-9, 300.0));
        film.AddSpecies(new Species("Cl-", -1, 2.03e-9, 300.0));

        Assert.True(film.IonicConductivity > 0.0);
    }

    // ── Species registration ──────────────────────────────────────────────────

    [Fact]
    public void ThinFilm_AddSpecies_AppearsInList()
    {
        var film = new ThinFilmElectrolyte(1e-6);
        var s    = new Species("Cl-", -1, 2.03e-9, 50.0);
        film.AddSpecies(s);

        Assert.Single(film.Species);
        Assert.Same(s, film.Species[0]);
    }

    // ── Input validation ───────────────────────────────────────────────────────

    [Fact]
    public void ThinFilm_Constructor_ThrowsOnNonPositiveFilmThickness()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ThinFilmElectrolyte(0.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ThinFilmElectrolyte(-1e-6));
    }

    [Fact]
    public void ThinFilm_Constructor_ThrowsOnNegativeConcentration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ThinFilmElectrolyte(1e-6, totalConcentration: -1.0));
    }

    [Fact]
    public void ThinFilm_AddSpecies_ThrowsOnNull()
    {
        var film = new ThinFilmElectrolyte(1e-6);
        Assert.Throws<ArgumentNullException>(() => film.AddSpecies(null!));
    }
}

// ── SpeciesTransport tests ────────────────────────────────────────────────────

public class SpeciesTransportTests
{
    private const int    Nx  = 20;
    private const double L   = 1e-4;   // 100 µm domain
    private const double D   = 1.33e-9; // Na+ diffusion coefficient
    private const double Dt  = 0.01;   // time step (s)

    private static Species MakeSodium(double c = 100.0) =>
        new("Na+", charge: 1, diffusionCoefficient: D, concentration: c);

    private static double[] UniformProfile(int n, double val)
    {
        var arr = new double[n];
        Array.Fill(arr, val);
        return arr;
    }

    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void SpeciesTransport_Properties_StoredCorrectly()
    {
        var species = MakeSodium();
        var left    = new DirichletBC(_ => 100.0);
        var right   = new DirichletBC(_ => 100.0);
        var init    = UniformProfile(Nx, 100.0);

        var transport = new SpeciesTransport(species, L, Nx, init, left, right, Dt);

        Assert.Same(species, transport.Species);
        Assert.Equal(L,  transport.DomainLength);
        Assert.Equal(Nx, transport.GridPoints);
        Assert.Equal(0.0, transport.CurrentTime);
        Assert.Equal(Nx, transport.ConcentrationProfile.Count);
    }

    // ── Advance / time evolution ───────────────────────────────────────────────

    [Fact]
    public void SpeciesTransport_Advance_IncreasesCurrentTime()
    {
        var species   = MakeSodium();
        var left      = new DirichletBC(_ => 100.0);
        var right     = new DirichletBC(_ => 100.0);
        var transport = new SpeciesTransport(species, L, Nx, UniformProfile(Nx, 100.0), left, right, Dt);

        transport.Advance(10);

        Assert.True(transport.CurrentTime > 0.0, "CurrentTime should advance after Advance().");
    }

    [Fact]
    public void SpeciesTransport_Advance_UpdatesSpeciesConcentration()
    {
        // Gradient: left = 200, right = 0 → average will be somewhere between 0 and 200
        var species   = MakeSodium(0.0);
        var left      = new DirichletBC(_ => 200.0);
        var right     = new DirichletBC(_ => 0.0);
        var init      = UniformProfile(Nx, 0.0);
        var transport = new SpeciesTransport(species, L, Nx, init, left, right, Dt);

        transport.Advance(100);

        // After many steps the profile should have evolved from all-zero
        Assert.True(species.Concentration > 0.0,
            "Species concentration should increase when left BC is higher.");
    }

    [Fact]
    public void SpeciesTransport_SteadyState_UniformProfile_IsStable()
    {
        // Uniform initial profile with equal Dirichlet BCs → should stay uniform
        var species   = MakeSodium(50.0);
        var left      = new DirichletBC(_ => 50.0);
        var right     = new DirichletBC(_ => 50.0);
        var transport = new SpeciesTransport(species, L, Nx, UniformProfile(Nx, 50.0), left, right, Dt);

        transport.Advance(50);

        // All nodal values should remain 50 (steady state)
        foreach (double v in transport.ConcentrationProfile)
            Assert.Equal(50.0, v, precision: 6);
    }

    [Fact]
    public void SpeciesTransport_ConcentrationProfile_HasCorrectLength()
    {
        var species   = MakeSodium();
        var left      = new DirichletBC(_ => 100.0);
        var right     = new DirichletBC(_ => 0.0);
        var transport = new SpeciesTransport(species, L, Nx, UniformProfile(Nx, 100.0), left, right, Dt);

        transport.Advance();

        Assert.Equal(Nx, transport.ConcentrationProfile.Count);
    }

    // ── Input validation ───────────────────────────────────────────────────────

    [Fact]
    public void SpeciesTransport_Constructor_ThrowsOnNullSpecies()
    {
        var left  = new DirichletBC(_ => 0.0);
        var right = new DirichletBC(_ => 0.0);
        Assert.Throws<ArgumentNullException>(() =>
            new SpeciesTransport(null!, L, Nx, UniformProfile(Nx, 0.0), left, right, Dt));
    }

    [Fact]
    public void SpeciesTransport_Constructor_ThrowsOnMismatchedProfileLength()
    {
        var species = MakeSodium();
        var left    = new DirichletBC(_ => 0.0);
        var right   = new DirichletBC(_ => 0.0);
        Assert.Throws<ArgumentException>(() =>
            new SpeciesTransport(species, L, Nx, new double[Nx + 1], left, right, Dt));
    }

    [Fact]
    public void SpeciesTransport_Advance_ThrowsOnZeroSteps()
    {
        var species   = MakeSodium();
        var left      = new DirichletBC(_ => 100.0);
        var right     = new DirichletBC(_ => 0.0);
        var transport = new SpeciesTransport(species, L, Nx, UniformProfile(Nx, 50.0), left, right, Dt);

        Assert.Throws<ArgumentOutOfRangeException>(() => transport.Advance(0));
    }
}
