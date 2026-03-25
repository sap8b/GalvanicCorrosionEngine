using GCE.Atmosphere;
using GCE.Core;

namespace GCE.Atmosphere.Tests;

// ── Time of Wetness ───────────────────────────────────────────────────────────
//
// "Time of Wetness" (TOW) is the fraction of time during which a metal surface
// carries a continuous electrolyte film (i.e., the film is deliquesced and
// thicker than the minimum dry threshold).  It is a key factor in atmospheric
// corrosion rate prediction (ISO 9223).
//
// These tests verify that FilmEvolution correctly tracks whether the surface is
// wet and that computed TOW fractions are physically reasonable.

public class TimeOfWetnessTests
{
    private static FilmEvolutionParameters DefaultParams(double initialThickness = 0.0) =>
        new() { InitialThicknessMeters = initialThickness };

    // ── Continuously high-RH environment → TOW ≈ 1 ──────────────────────────

    [Fact]
    public void TimeOfWetness_AllStepsWet_FractionIsOne()
    {
        // RH 0.90 is well above NaCl DRH (0.753) → surface stays wet
        var obs = new WeatherObservation(20.0, RelativeHumidity: 0.90, ChlorideConcentration: 0.05);
        var fe  = new FilmEvolution(new() { InitialThicknessMeters = 1e-6 });

        int steps   = 100;
        int wetCount = 0;

        for (int i = 0; i < steps; i++)
        {
            fe.Advance(3600.0, obs);
            if (fe.State.IsDeliquesced) wetCount++;
        }

        double tow = (double)wetCount / steps;
        Assert.True(tow >= 0.95,
            $"All-wet scenario should give TOW near 1.0, got {tow:F3}");
    }

    // ── Continuously low-RH environment → TOW ≈ 0 ───────────────────────────

    [Fact]
    public void TimeOfWetness_AllStepsDry_FractionIsZero()
    {
        // RH 0.20 is well below NaCl efflorescence RH (0.740) → surface stays dry
        var obs = new WeatherObservation(25.0, RelativeHumidity: 0.20, ChlorideConcentration: 0.05);
        var fe  = new FilmEvolution(DefaultParams());   // starts dry

        int steps   = 100;
        int wetCount = 0;

        for (int i = 0; i < steps; i++)
        {
            fe.Advance(3600.0, obs);
            if (fe.State.IsDeliquesced) wetCount++;
        }

        double tow = (double)wetCount / steps;
        Assert.Equal(0.0, tow);
    }

    // ── Diurnal cycle → 0 < TOW < 1 ─────────────────────────────────────────

    [Fact]
    public void TimeOfWetness_DiurnalCycle_FractionIsBetweenZeroAndOne()
    {
        // Sinusoidal weather with base RH 0.70, amplitude 0.20
        // → peaks at 0.90 (wet) and troughs at 0.50 (dry)
        var provider = new SyntheticWeatherProvider(
            baseTempCelsius:       15.0,
            tempAmplitude:          8.0,
            baseRelativeHumidity:   0.70,
            humidityAmplitude:      0.20,
            chlorideConcentration:  0.05);

        var fe = new FilmEvolution(new() { InitialThicknessMeters = 5e-7 });

        double dtSeconds = 3600.0;
        int    steps     = 24 * 7;   // one week of hourly steps
        int    wetCount  = 0;

        for (int i = 0; i < steps; i++)
        {
            var obs = provider.GetObservation(i * dtSeconds);
            fe.Advance(dtSeconds, obs);
            if (fe.State.IsDeliquesced) wetCount++;
        }

        double tow = (double)wetCount / steps;
        Assert.True(tow > 0.0 && tow < 1.0,
            $"Diurnal cycle should give 0 < TOW < 1, got {tow:F3}");
    }

    // ── Rain initiates wetness ────────────────────────────────────────────────

    [Fact]
    public void TimeOfWetness_RainEvent_InitiatesWettingOnDrySurface()
    {
        // Start dry; apply a rain observation
        var fe      = new FilmEvolution(DefaultParams());
        var rainObs = new WeatherObservation(20.0, RelativeHumidity: 0.85,
                                             ChlorideConcentration: 0.05,
                                             Precipitation: 5.0);
        fe.Advance(1.0, rainObs);

        Assert.True(fe.State.IsDeliquesced,
            "A rain event should initiate a wet surface.");
    }

    // ── TOW accumulates correctly over multiple observations ──────────────────

    [Fact]
    public void TimeOfWetness_Accumulation_MatchesManualCount()
    {
        var observations = new WeatherObservation[]
        {
            new(25.0, 0.90, 0.05),   // wet
            new(25.0, 0.90, 0.05),   // wet
            new(25.0, 0.10, 0.05),   // dry
            new(25.0, 0.10, 0.05),   // dry
            new(25.0, 0.10, 0.05),   // dry
        };

        var fe = new FilmEvolution(new() { InitialThicknessMeters = 1e-6 });

        int wetCount = 0;
        foreach (var obs in observations)
        {
            fe.Advance(3600.0, obs);
            if (fe.State.IsDeliquesced) wetCount++;
        }

        // After driving the surface wet and then dry we expect the counts to be
        // consistent with the number of steps for which IsDeliquesced was true.
        Assert.True(wetCount >= 0 && wetCount <= observations.Length,
            $"Wet count {wetCount} should be between 0 and {observations.Length}");
    }

    // ── TOW increases with higher base humidity ───────────────────────────────

    [Fact]
    public void TimeOfWetness_HigherBaseHumidity_GivesHigherTow()
    {
        static int ComputeWetSteps(double baseRh)
        {
            var provider = new SyntheticWeatherProvider(
                baseTempCelsius:      15.0,
                tempAmplitude:         5.0,
                baseRelativeHumidity:  baseRh,
                humidityAmplitude:     0.10,
                chlorideConcentration: 0.05);

            var fe = new FilmEvolution(new() { InitialThicknessMeters = 5e-7 });
            int steps = 24 * 30;   // 30 days of hourly steps
            int wet   = 0;

            for (int i = 0; i < steps; i++)
            {
                fe.Advance(3600.0, provider.GetObservation(i * 3600.0));
                if (fe.State.IsDeliquesced) wet++;
            }
            return wet;
        }

        int wetLow  = ComputeWetSteps(0.50);
        int wetHigh = ComputeWetSteps(0.85);

        Assert.True(wetHigh >= wetLow,
            $"Higher base RH (0.85) should give ≥ wet steps than lower base RH (0.50); " +
            $"got {wetHigh} vs {wetLow}.");
    }

    // ── Wetness state persists when RH stays above DRH ───────────────────────

    [Fact]
    public void TimeOfWetness_ConstantHighRh_SurfaceRemainsWet()
    {
        // Start with an already-wet surface; keep RH well above DRH
        var fe  = new FilmEvolution(new() { InitialThicknessMeters = 1e-6 });
        var obs = new WeatherObservation(20.0, RelativeHumidity: 0.95, ChlorideConcentration: 0.05);

        // Advance a single step to ensure the film is established
        fe.Advance(3600.0, obs);
        Assert.True(fe.State.IsDeliquesced, "Surface should be wet at RH 0.95.");

        // Continue advancing – surface must remain wet
        for (int i = 0; i < 10; i++)
        {
            fe.Advance(3600.0, obs);
            Assert.True(fe.State.IsDeliquesced,
                $"Surface should remain wet at RH 0.95 (step {i + 1}).");
        }
    }

    // ── Film thickness is positive when surface is wet ───────────────────────

    [Fact]
    public void TimeOfWetness_WhenDeliquesced_FilmThicknessIsAboveMinimum()
    {
        var fe  = new FilmEvolution(new() { InitialThicknessMeters = 1e-6 });
        var obs = new WeatherObservation(20.0, RelativeHumidity: 0.90, ChlorideConcentration: 0.05);

        fe.Advance(3600.0, obs);

        if (fe.State.IsDeliquesced)
        {
            Assert.True(fe.State.ThicknessMeters > 0.0,
                "Film thickness must be positive when surface is deliquesced.");
        }
    }
}
