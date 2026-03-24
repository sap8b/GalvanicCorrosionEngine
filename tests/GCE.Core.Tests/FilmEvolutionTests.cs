using GCE.Atmosphere;
using GCE.Core;

namespace GCE.Core.Tests;

// ── DeliquescenceData ─────────────────────────────────────────────────────────

public class DeliquescenceDataTests
{
    [Theory]
    [InlineData(CommonSalt.NaCl,            0.753)]
    [InlineData(CommonSalt.MgCl2,           0.330)]
    [InlineData(CommonSalt.CaCl2,           0.290)]
    [InlineData(CommonSalt.AmmoniumSulfate, 0.799)]
    public void GetDeliquescenceRH_At25C_ReturnsReferenceValue(CommonSalt salt, double expectedDrh)
    {
        double drh = DeliquescenceData.GetDeliquescenceRH(salt, 25.0);
        Assert.Equal(expectedDrh, drh, precision: 3);
    }

    [Theory]
    [InlineData(CommonSalt.NaCl,            0.740)]
    [InlineData(CommonSalt.MgCl2,           0.277)]
    [InlineData(CommonSalt.CaCl2,           0.200)]
    [InlineData(CommonSalt.AmmoniumSulfate, 0.370)]
    public void GetEfflorescenceRH_At25C_ReturnsReferenceValue(CommonSalt salt, double expectedErh)
    {
        double erh = DeliquescenceData.GetEfflorescenceRH(salt, 25.0);
        Assert.Equal(expectedErh, erh, precision: 3);
    }

    [Fact]
    public void GetDeliquescenceRH_AllSalts_DrhAboveErh()
    {
        foreach (CommonSalt salt in Enum.GetValues<CommonSalt>())
        {
            double drh = DeliquescenceData.GetDeliquescenceRH(salt, 25.0);
            double erh = DeliquescenceData.GetEfflorescenceRH(salt, 25.0);
            Assert.True(drh > erh,
                $"{salt}: expected DRH ({drh:F3}) > ERH ({erh:F3})");
        }
    }

    [Fact]
    public void GetDeliquescenceRH_NaCl_IncreasesWithTemperature()
    {
        // NaCl DRH has a small positive temperature slope
        double drh20 = DeliquescenceData.GetDeliquescenceRH(CommonSalt.NaCl, 20.0);
        double drh35 = DeliquescenceData.GetDeliquescenceRH(CommonSalt.NaCl, 35.0);
        Assert.True(drh35 > drh20,
            $"NaCl DRH should increase with temperature: {drh20:F4} → {drh35:F4}");
    }

    [Fact]
    public void GetDeliquescenceRH_MgCl2_DecreasesWithTemperature()
    {
        // MgCl2 DRH has a negative temperature slope
        double drh20 = DeliquescenceData.GetDeliquescenceRH(CommonSalt.MgCl2, 20.0);
        double drh35 = DeliquescenceData.GetDeliquescenceRH(CommonSalt.MgCl2, 35.0);
        Assert.True(drh35 < drh20,
            $"MgCl2 DRH should decrease with temperature: {drh20:F4} → {drh35:F4}");
    }

    [Fact]
    public void GetDeliquescenceRH_ResultClampedBetweenZeroAndOne()
    {
        // Extreme temperatures should not produce out-of-range values
        foreach (CommonSalt salt in Enum.GetValues<CommonSalt>())
        {
            double drhLow  = DeliquescenceData.GetDeliquescenceRH(salt, -50.0);
            double drhHigh = DeliquescenceData.GetDeliquescenceRH(salt, 200.0);
            Assert.InRange(drhLow,  0.0, 1.0);
            Assert.InRange(drhHigh, 0.0, 1.0);
        }
    }

    [Fact]
    public void GetEfflorescenceRH_ResultClampedBetweenZeroAndOne()
    {
        foreach (CommonSalt salt in Enum.GetValues<CommonSalt>())
        {
            double erhLow  = DeliquescenceData.GetEfflorescenceRH(salt, -50.0);
            double erhHigh = DeliquescenceData.GetEfflorescenceRH(salt, 200.0);
            Assert.InRange(erhLow,  0.0, 1.0);
            Assert.InRange(erhHigh, 0.0, 1.0);
        }
    }
}

// ── FilmState ─────────────────────────────────────────────────────────────────

public class FilmStateTests
{
    [Fact]
    public void FilmState_StoresAllProperties()
    {
        var state = new FilmState(
            ThicknessMeters:           1e-6,
            SaltConcentrationMolPerL:  0.5,
            SurfaceTemperatureCelsius: 30.0,
            IsDeliquesced:             true,
            CoverageFraction:          0.8);

        Assert.Equal(1e-6, state.ThicknessMeters);
        Assert.Equal(0.5,  state.SaltConcentrationMolPerL);
        Assert.Equal(30.0, state.SurfaceTemperatureCelsius);
        Assert.True(state.IsDeliquesced);
        Assert.Equal(0.8,  state.CoverageFraction);
    }

    [Fact]
    public void FilmState_DefaultCoverageFraction_IsOne()
    {
        var state = new FilmState(1e-6, 0.1, 25.0, true);
        Assert.Equal(1.0, state.CoverageFraction);
    }

    [Fact]
    public void FilmState_EqualityBasedOnAllFields()
    {
        var a = new FilmState(1e-6, 0.1, 25.0, true, 0.9);
        var b = new FilmState(1e-6, 0.1, 25.0, true, 0.9);
        Assert.Equal(a, b);
    }

    [Fact]
    public void FilmState_InequalityWhenThicknessDiffers()
    {
        var a = new FilmState(1e-6, 0.1, 25.0, true);
        var b = new FilmState(2e-6, 0.1, 25.0, true);
        Assert.NotEqual(a, b);
    }
}

// ── FilmEvolutionParameters ───────────────────────────────────────────────────

public class FilmEvolutionParametersTests
{
    [Fact]
    public void DefaultParameters_HaveExpectedValues()
    {
        var p = new FilmEvolutionParameters();

        Assert.Equal(CommonSalt.NaCl, p.Salt);
        Assert.Equal(0.0,   p.InitialThicknessMeters);
        Assert.Equal(0.1,   p.InitialSaltConcentrationMolPerL);
        Assert.Equal(1e-8,  p.EvaporationCoefficient);
        Assert.Equal(1e-8,  p.CondensationCoefficient);
        Assert.Equal(0.5,   p.SolarAbsorptivity);
        Assert.Equal(10.0,  p.HeatTransferCoefficient);
        Assert.Equal(1e-9,  p.MinFilmThicknessMeters);
        Assert.Equal(0.1,   p.WindEvaporationFactor);
        Assert.Equal(30.0,  p.EquilibriumContactAngleDegrees);
    }

    [Fact]
    public void WithExpression_OverridesIndividualField()
    {
        var p = new FilmEvolutionParameters() with { Salt = CommonSalt.MgCl2 };
        Assert.Equal(CommonSalt.MgCl2, p.Salt);
        Assert.Equal(0.1, p.InitialSaltConcentrationMolPerL);
    }
}

// ── FilmEvolution – surface temperature ──────────────────────────────────────

public class FilmEvolutionSurfaceTemperatureTests
{
    private static readonly WeatherObservation BaseObs = new(20.0, 0.70, 0.05);

    [Fact]
    public void CalculateSurfaceTemperature_NoInsolation_EqualsAmbient()
    {
        var fe = new FilmEvolution();
        double tSurface = fe.CalculateSurfaceTemperature(BaseObs, insolationWm2: 0.0);
        Assert.Equal(20.0, tSurface, precision: 9);
    }

    [Fact]
    public void CalculateSurfaceTemperature_WithInsolation_AboveAmbient()
    {
        var fe = new FilmEvolution();
        double tSurface = fe.CalculateSurfaceTemperature(BaseObs, insolationWm2: 500.0);
        Assert.True(tSurface > 20.0, $"Expected surface temp > 20 °C, got {tSurface:F2} °C");
    }

    [Fact]
    public void CalculateSurfaceTemperature_SteadyStateBalance_Correct()
    {
        // ΔT = α · G / HTC = 0.5 × 600 / 10 = 30 °C → T_surface = 50 °C
        var p  = new FilmEvolutionParameters() with
        {
            SolarAbsorptivity     = 0.5,
            HeatTransferCoefficient = 10.0,
        };
        var fe = new FilmEvolution(p);
        double tSurface = fe.CalculateSurfaceTemperature(new WeatherObservation(20.0, 0.70, 0.05), 600.0);
        Assert.Equal(50.0, tSurface, precision: 6);
    }

    [Fact]
    public void CalculateSurfaceTemperature_NullObservation_Throws()
    {
        var fe = new FilmEvolution();
        Assert.Throws<ArgumentNullException>(() => fe.CalculateSurfaceTemperature(null!));
    }
}

// ── FilmEvolution – deliquescence / efflorescence transitions ─────────────────

public class FilmEvolutionPhaseTransitionTests
{
    // NaCl DRH at 25 °C ≈ 0.753; ERH ≈ 0.740
    private const double NaClDrhAt25 = 0.753;
    private const double NaClErhAt25 = 0.740;

    [Fact]
    public void Advance_DrySurface_RhAboveDrh_BecomesWet()
    {
        var fe  = new FilmEvolution(); // starts dry with NaCl
        var obs = new WeatherObservation(25.0, NaClDrhAt25 + 0.05, 0.05);

        var state = fe.Advance(3600.0, obs);

        Assert.True(state.IsDeliquesced, "Surface should be deliquesced when RH > DRH");
    }

    [Fact]
    public void Advance_DrySurface_RhBelowDrh_Remains干()
    {
        var fe  = new FilmEvolution();
        var obs = new WeatherObservation(25.0, NaClDrhAt25 - 0.05, 0.05);

        var state = fe.Advance(3600.0, obs);

        Assert.False(state.IsDeliquesced);
        Assert.Equal(0.0, state.ThicknessMeters, precision: 12);
    }

    [Fact]
    public void Advance_WetSurface_RhBelowErh_EventuallyDries()
    {
        // Start with a very thin wet film at ERH - 0.1 to force drying
        var p = new FilmEvolutionParameters() with
        {
            InitialThicknessMeters         = 1e-9 * 0.5, // just above zero but below min
            InitialSaltConcentrationMolPerL = 0.1,
            EvaporationCoefficient          = 1e-6, // fast evaporation
        };
        var fe  = new FilmEvolution(p with { InitialThicknessMeters = 5e-10 });
        var obs = new WeatherObservation(25.0, NaClErhAt25 - 0.05, 0.05);

        // Start manually with a wet, nearly-zero film via one big evaporation step
        var pWet = new FilmEvolutionParameters() with
        {
            InitialThicknessMeters          = 5e-10,
            EvaporationCoefficient          = 1e-3,
        };
        var feWet = new FilmEvolution(pWet);
        // Manually set state by calling Advance from a high-humidity observation first
        feWet.Advance(1.0, new WeatherObservation(25.0, 0.90, 0.05));
        // Now lower humidity below ERH for an extended step
        var finalState = feWet.Advance(7200.0, obs);

        // After drying the film should be gone or surface should be dry
        Assert.True(!finalState.IsDeliquesced || finalState.ThicknessMeters <= pWet.MinFilmThicknessMeters);
    }
}

// ── FilmEvolution – evaporation and condensation ──────────────────────────────

public class FilmEvolutionThicknessTests
{
    [Fact]
    public void Advance_HighHumidity_FilmThickens()
    {
        // Start with a thin wet film; RH much higher than DRH → condensation dominates
        var p = new FilmEvolutionParameters() with
        {
            InitialThicknessMeters          = 1e-7,
            CondensationCoefficient          = 1e-6, // fast condensation
            EvaporationCoefficient           = 1e-10, // slow evaporation
        };
        var fe  = new FilmEvolution(p);
        // Start wet
        var wetObs = new WeatherObservation(25.0, 0.90, 0.05);
        fe.Advance(1.0, wetObs); // initiate wet surface

        // Now condense at very high RH
        double before = fe.State.ThicknessMeters;
        fe.Advance(3600.0, new WeatherObservation(25.0, 0.95, 0.05));
        double after = fe.State.ThicknessMeters;

        Assert.True(after > before,
            $"Film should thicken during condensation: {before:E3} → {after:E3}");
    }

    [Fact]
    public void Advance_LowHumidity_WetFilm_Thins()
    {
        // Start with a reasonably thick wet film; drive RH well below DRH
        var p = new FilmEvolutionParameters() with
        {
            InitialThicknessMeters          = 1e-5,
            EvaporationCoefficient          = 1e-6,
        };
        var fe  = new FilmEvolution(p);
        fe.Advance(1.0, new WeatherObservation(25.0, 0.90, 0.05)); // deliquescence trigger

        var state = fe.State;
        double before = state.ThicknessMeters;
        var dryObs = new WeatherObservation(25.0, 0.30, 0.05); // well below DRH
        fe.Advance(3600.0, dryObs);
        double after = fe.State.ThicknessMeters;

        Assert.True(after < before,
            $"Film should thin during evaporation: {before:E3} → {after:E3}");
    }

    [Fact]
    public void Advance_Rain_ThickensFilm()
    {
        var p = new FilmEvolutionParameters() with
        {
            InitialThicknessMeters = 1e-7,
        };
        var fe = new FilmEvolution(p);
        fe.Advance(1.0, new WeatherObservation(25.0, 0.90, 0.05)); // start wet

        double before = fe.State.ThicknessMeters;
        // Heavy rain: 10 mm/h, no evaporation (RH at DRH)
        var rainObs = new WeatherObservation(
            TemperatureCelsius: 25.0,
            RelativeHumidity:   0.753, // at DRH → no evap or cond from humidity
            ChlorideConcentration: 0.05,
            Precipitation: 10.0,
            WindSpeed: 0.0);
        fe.Advance(3600.0, rainObs);
        double after = fe.State.ThicknessMeters;

        Assert.True(after > before,
            $"Rain should increase film thickness: {before:E3} → {after:E3}");
    }

    [Fact]
    public void Advance_Rain_OnDrySurface_InitiatesWetFilm()
    {
        var fe = new FilmEvolution(); // starts dry
        var rainObs = new WeatherObservation(25.0, 0.40, 0.05, Precipitation: 5.0);

        var state = fe.Advance(3600.0, rainObs);

        Assert.True(state.IsDeliquesced, "Rain on dry surface should initiate a wet film");
        Assert.True(state.ThicknessMeters > 0.0);
    }

    [Fact]
    public void Advance_SaltConservation_DuringEvaporation()
    {
        // As film evaporates, salt concentration should increase
        var p = new FilmEvolutionParameters() with
        {
            InitialThicknessMeters          = 1e-5,
            EvaporationCoefficient          = 1e-7,
        };
        var fe = new FilmEvolution(p);
        fe.Advance(1.0, new WeatherObservation(25.0, 0.90, 0.05)); // wet start

        double concBefore = fe.State.SaltConcentrationMolPerL;
        double thickBefore = fe.State.ThicknessMeters;
        fe.Advance(3600.0, new WeatherObservation(25.0, 0.30, 0.05));
        double concAfter  = fe.State.SaltConcentrationMolPerL;
        double thickAfter  = fe.State.ThicknessMeters;

        // If the film thinned, concentration should have increased
        if (thickAfter < thickBefore && thickAfter > p.MinFilmThicknessMeters)
        {
            Assert.True(concAfter > concBefore,
                $"Salt concentration should increase as film evaporates: {concBefore:F4} → {concAfter:F4}");
        }
    }

    [Fact]
    public void Advance_SaltConservation_MassPreserved()
    {
        // Total salt per unit area (mol/m²) = conc × thickness should be preserved
        // (neglecting rain dilution)
        var p = new FilmEvolutionParameters() with
        {
            InitialThicknessMeters          = 1e-5,
            EvaporationCoefficient          = 1e-8,
        };
        var fe = new FilmEvolution(p);
        fe.Advance(1.0, new WeatherObservation(25.0, 0.90, 0.05)); // wet start

        double saltMassBefore = fe.State.SaltConcentrationMolPerL * fe.State.ThicknessMeters;
        fe.Advance(1800.0, new WeatherObservation(25.0, 0.50, 0.05));
        double saltMassAfter  = fe.State.SaltConcentrationMolPerL * fe.State.ThicknessMeters;

        if (fe.State.ThicknessMeters > p.MinFilmThicknessMeters)
        {
            Assert.Equal(saltMassBefore, saltMassAfter, precision: 10);
        }
    }
}

// ── FilmEvolution – solar heating effects ─────────────────────────────────────

public class FilmEvolutionSolarHeatingTests
{
    [Fact]
    public void Advance_HighInsolation_StateReflectsElevatedSurfaceTemp()
    {
        var fe  = new FilmEvolution();
        var obs = new WeatherObservation(25.0, 0.30, 0.05);

        var state = fe.Advance(3600.0, obs, insolationWm2: 800.0);

        // Surface temperature should be above ambient
        Assert.True(state.SurfaceTemperatureCelsius > 25.0,
            $"Expected surface temp above 25 °C; got {state.SurfaceTemperatureCelsius:F2} °C");
    }

    [Fact]
    public void Advance_ZeroInsolation_SurfaceTempEqualsAmbient()
    {
        var fe  = new FilmEvolution();
        var obs = new WeatherObservation(22.0, 0.60, 0.05);

        var state = fe.Advance(3600.0, obs, insolationWm2: 0.0);

        Assert.Equal(22.0, state.SurfaceTemperatureCelsius, precision: 9);
    }

    [Fact]
    public void Advance_HighInsolation_ShiftsDrhAndMayPreventWetting()
    {
        // Solar heating can raise surface temperature enough that the DRH (for MgCl2,
        // which has a negative temperature slope) drops, making the surface easier
        // to wet at the same ambient RH.  Conversely, for NaCl (positive slope)
        // heating raises DRH, which can keep the surface dry at ambient RH near DRH.

        // NaCl DRH at 25°C ≈ 0.753; at elevated surface temp it should be higher.
        // With G=1000 W/m², α=0.5, HTC=10 → ΔT=50°C → T_surface=75°C
        // DRH(NaCl, 75°C) = 0.753 + 0.00017*(75-25) = 0.753 + 0.0085 = 0.7615
        var p   = new FilmEvolutionParameters() with { SolarAbsorptivity = 0.5, HeatTransferCoefficient = 10.0 };
        var fe1 = new FilmEvolution(p);
        var fe2 = new FilmEvolution(p);

        // RH = 0.76 – slightly above DRH at 25°C but below DRH at 75°C
        var obs = new WeatherObservation(25.0, 0.760, 0.05);

        var stateNoSun  = fe1.Advance(3600.0, obs, insolationWm2: 0.0);
        var stateSunny  = fe2.Advance(3600.0, obs, insolationWm2: 1000.0);

        // Without solar heating: RH 0.76 > NaCl DRH 0.753 → should deliquesce
        Assert.True(stateNoSun.IsDeliquesced, "Without solar heating the surface should deliquesce at RH=0.76");

        // With strong solar heating: DRH rises above 0.76 → surface stays dry
        Assert.False(stateSunny.IsDeliquesced, "Strong solar heating should raise DRH above ambient RH, keeping surface dry");
    }
}

// ── FilmEvolution – droplet spreading and coalescence ─────────────────────────

public class FilmEvolutionDropletTests
{
    [Fact]
    public void Advance_ThinFilm_CoverageLessThanOne()
    {
        // A very thin wet film should have coverage < 1 (droplet regime)
        var p = new FilmEvolutionParameters() with
        {
            InitialThicknessMeters = 1e-7, // well below coalescence threshold
        };
        var fe = new FilmEvolution(p);
        fe.Advance(1.0, new WeatherObservation(25.0, 0.90, 0.05)); // initiate wet state

        Assert.True(fe.State.CoverageFraction < 1.0,
            $"Thin film should be in droplet regime (coverage < 1), got {fe.State.CoverageFraction:F4}");
    }

    [Fact]
    public void Advance_FilmAboveCoalescenceThreshold_CoverageIsOne()
    {
        // A thick film (≥ 10 µm) should be fully coalesced
        var p = new FilmEvolutionParameters() with
        {
            InitialThicknessMeters = FilmEvolution.DropletCoalescenceThicknessMeters * 2,
        };
        var fe = new FilmEvolution(p);
        fe.Advance(1.0, new WeatherObservation(25.0, 0.90, 0.05)); // initiate wet state

        Assert.Equal(1.0, fe.State.CoverageFraction, precision: 9);
    }

    [Fact]
    public void Advance_CoverageRelaxesToEquilibrium_OverTime()
    {
        // A film starting with zero coverage should gradually increase coverage
        var p = new FilmEvolutionParameters() with
        {
            InitialThicknessMeters = 1e-6, // thin but visible film
        };
        var fe = new FilmEvolution(p);
        fe.Advance(1.0, new WeatherObservation(25.0, 0.90, 0.05)); // trigger wetting

        double coverageAfterShortStep = fe.State.CoverageFraction;

        // After a long spreading time, coverage should be closer to equilibrium
        fe.Advance(3600.0, new WeatherObservation(25.0, 0.90, 0.05));
        double coverageAfterLongStep = fe.State.CoverageFraction;

        Assert.True(coverageAfterLongStep >= coverageAfterShortStep,
            $"Coverage should not decrease as droplets spread: {coverageAfterShortStep:F4} → {coverageAfterLongStep:F4}");
    }

    [Fact]
    public void Advance_SmallContactAngle_HigherCoverage_ThanLargeContactAngle()
    {
        // Hydrophilic surface (small θ) → droplets spread more → higher coverage
        // at the same equivalent film thickness.
        double thickness = 1e-7; // 100 nm
        var pHydrophilic = new FilmEvolutionParameters() with
        {
            InitialThicknessMeters              = thickness,
            EquilibriumContactAngleDegrees       = 10.0,
        };
        var pHydrophobic = new FilmEvolutionParameters() with
        {
            InitialThicknessMeters              = thickness,
            EquilibriumContactAngleDegrees       = 80.0,
        };

        var feHydrophilic = new FilmEvolution(pHydrophilic);
        var feHydrophobic = new FilmEvolution(pHydrophobic);

        var obs = new WeatherObservation(25.0, 0.90, 0.05);

        // Allow the spreading model to take a long time step so coverage approaches equilibrium
        feHydrophilic.Advance(3600.0, obs);
        feHydrophobic.Advance(3600.0, obs);

        Assert.True(feHydrophilic.State.CoverageFraction > feHydrophobic.State.CoverageFraction,
            $"Hydrophilic (θ=10°) coverage {feHydrophilic.State.CoverageFraction:F4} should exceed " +
            $"hydrophobic (θ=80°) coverage {feHydrophobic.State.CoverageFraction:F4}");
    }

    [Fact]
    public void Advance_DrySurface_CoverageFraction_IsZero()
    {
        var fe  = new FilmEvolution();
        var obs = new WeatherObservation(25.0, 0.30, 0.05); // well below DRH

        var state = fe.Advance(3600.0, obs);

        Assert.Equal(0.0, state.CoverageFraction, precision: 9);
    }
}

// ── FilmEvolution – wind enhancement ──────────────────────────────────────────

public class FilmEvolutionWindTests
{
    [Fact]
    public void Advance_HighWind_EvaporatesFilmFaster()
    {
        // Prepare two identical thick wet films; advance one with high wind, one calm.
        // Use a 1 mm film so neither completely evaporates within the test step,
        // allowing a measurable thickness difference.
        double initialThickness = 1e-3; // 1 mm – thick enough to survive the step
        var obsCalm  = new WeatherObservation(25.0, 0.40, 0.05, WindSpeed: 0.0);
        var obsWindy = new WeatherObservation(25.0, 0.40, 0.05, WindSpeed: 10.0);

        var p = new FilmEvolutionParameters() with { InitialThicknessMeters = initialThickness };
        var feCalm  = new FilmEvolution(p);
        var feWindy = new FilmEvolution(p);

        // Both films already start wet (InitialThicknessMeters > MinFilmThicknessMeters).
        // One short step keeps them comparable before the wind comparison.
        feCalm.Advance(1.0,  new WeatherObservation(25.0, 0.90, 0.05));
        feWindy.Advance(1.0, new WeatherObservation(25.0, 0.90, 0.05));

        feCalm.Advance(3600.0,  obsCalm);
        feWindy.Advance(3600.0, obsWindy);

        Assert.True(feWindy.State.ThicknessMeters < feCalm.State.ThicknessMeters,
            "Windy conditions should evaporate the film faster than calm conditions");
    }
}

// ── FilmEvolution – argument validation ───────────────────────────────────────

public class FilmEvolutionValidationTests
{
    [Fact]
    public void Advance_NullObservation_Throws()
    {
        var fe = new FilmEvolution();
        Assert.Throws<ArgumentNullException>(() => fe.Advance(60.0, null!));
    }

    [Fact]
    public void Advance_NegativeOrZeroDt_Throws()
    {
        var fe  = new FilmEvolution();
        var obs = new WeatherObservation(25.0, 0.70, 0.05);

        Assert.Throws<ArgumentOutOfRangeException>(() => fe.Advance(0.0, obs));
        Assert.Throws<ArgumentOutOfRangeException>(() => fe.Advance(-1.0, obs));
    }

    [Fact]
    public void Advance_NegativeInsolation_Throws()
    {
        var fe  = new FilmEvolution();
        var obs = new WeatherObservation(25.0, 0.70, 0.05);

        Assert.Throws<ArgumentOutOfRangeException>(() => fe.Advance(60.0, obs, insolationWm2: -1.0));
    }

    [Fact]
    public void InitialState_ReflectsParameters()
    {
        var p = new FilmEvolutionParameters() with
        {
            InitialThicknessMeters          = 1e-6,
            InitialSaltConcentrationMolPerL = 0.5,
        };
        var fe = new FilmEvolution(p);

        Assert.Equal(1e-6, fe.State.ThicknessMeters);
        Assert.Equal(0.5,  fe.State.SaltConcentrationMolPerL);
        Assert.True(fe.State.IsDeliquesced, "Non-zero initial thickness should start in deliquesced state");
    }
}
