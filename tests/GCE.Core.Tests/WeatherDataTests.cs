using GCE.Atmosphere;
using GCE.Core;

namespace GCE.Core.Tests;

// ── IWeatherObservation / WeatherObservation ──────────────────────────────────

public class WeatherObservationTests
{
    [Fact]
    public void WeatherObservation_StoresAllProperties()
    {
        var obs = new WeatherObservation(
            TemperatureCelsius: 22.5,
            RelativeHumidity: 0.65,
            ChlorideConcentration: 0.10,
            Precipitation: 2.0,
            WindSpeed: 3.5);

        Assert.Equal(22.5, obs.TemperatureCelsius);
        Assert.Equal(0.65, obs.RelativeHumidity);
        Assert.Equal(0.10, obs.ChlorideConcentration);
        Assert.Equal(2.0,  obs.Precipitation);
        Assert.Equal(3.5,  obs.WindSpeed);
    }

    [Fact]
    public void WeatherObservation_DefaultsForOptionalParameters()
    {
        var obs = new WeatherObservation(20.0, 0.70, 0.05);

        Assert.Equal(0.0, obs.Precipitation);
        Assert.Equal(0.0, obs.WindSpeed);
    }

    [Fact]
    public void WeatherObservation_IsAssignableToIWeatherObservation()
    {
        IWeatherObservation obs = new WeatherObservation(15.0, 0.80, 0.02);
        Assert.NotNull(obs);
        Assert.Equal(15.0, obs.TemperatureCelsius);
    }

    [Fact]
    public void WeatherObservation_EqualityBasedOnAllFields()
    {
        var a = new WeatherObservation(20.0, 0.75, 0.10, 1.0, 2.0);
        var b = new WeatherObservation(20.0, 0.75, 0.10, 1.0, 2.0);
        Assert.Equal(a, b);
    }

    [Fact]
    public void WeatherObservation_InequalityWhenTemperatureDiffers()
    {
        var a = new WeatherObservation(20.0, 0.75, 0.10);
        var b = new WeatherObservation(21.0, 0.75, 0.10);
        Assert.NotEqual(a, b);
    }
}

// ── SyntheticWeatherProvider ──────────────────────────────────────────────────

public class SyntheticWeatherProviderTests
{
    private static readonly SyntheticWeatherProvider DefaultProvider =
        new SyntheticWeatherProvider(
            baseTempCelsius: 15.0,
            tempAmplitude: 8.0,
            baseRelativeHumidity: 0.70,
            humidityAmplitude: 0.15,
            chlorideConcentration: 0.05,
            precipitation: 0.0,
            windSpeed: 2.0);

    [Fact]
    public void GetObservation_ReturnsIWeatherObservation()
    {
        IWeatherObservation obs = DefaultProvider.GetObservation(0.0);
        Assert.NotNull(obs);
    }

    [Fact]
    public void GetObservation_TemperatureWithinExpectedRange()
    {
        // Temperature should stay within [base - amplitude, base + amplitude]
        for (double t = 0; t < 86_400; t += 3600)
        {
            var obs = DefaultProvider.GetObservation(t);
            Assert.InRange(obs.TemperatureCelsius, 15.0 - 8.0, 15.0 + 8.0);
        }
    }

    [Fact]
    public void GetObservation_HumidityWithinZeroToOne()
    {
        for (double t = 0; t < 86_400; t += 3600)
        {
            var obs = DefaultProvider.GetObservation(t);
            Assert.InRange(obs.RelativeHumidity, 0.0, 1.0);
        }
    }

    [Fact]
    public void GetObservation_ChlorideConstantThroughoutDay()
    {
        var obs0  = DefaultProvider.GetObservation(0);
        var obs12 = DefaultProvider.GetObservation(43_200);

        Assert.Equal(obs0.ChlorideConcentration, obs12.ChlorideConcentration, precision: 9);
    }

    [Fact]
    public void GetObservation_PrecipitationAndWindConstant()
    {
        var obs = DefaultProvider.GetObservation(0);
        Assert.Equal(0.0, obs.Precipitation);
        Assert.Equal(2.0, obs.WindSpeed);
    }

    [Fact]
    public void GetObservation_TemperatureAtNoon_HigherThanAtMidnight()
    {
        // Noon (43200 s) should be warmer than midnight (0 s) with the default phase
        var tempNoon     = DefaultProvider.GetObservation(43_200).TemperatureCelsius;
        var tempMidnight = DefaultProvider.GetObservation(0).TemperatureCelsius;

        Assert.True(tempNoon > tempMidnight,
            $"Expected noon temp ({tempNoon:F2} °C) > midnight temp ({tempMidnight:F2} °C)");
    }

    [Fact]
    public void GetObservation_HumidityAtNoon_LowerThanAtMidnight()
    {
        // Humidity is inversely correlated with temperature
        var rhNoon     = DefaultProvider.GetObservation(43_200).RelativeHumidity;
        var rhMidnight = DefaultProvider.GetObservation(0).RelativeHumidity;

        Assert.True(rhNoon < rhMidnight,
            $"Expected noon RH ({rhNoon:F3}) < midnight RH ({rhMidnight:F3})");
    }

    [Fact]
    public void GetObservation_PeriodOneDay_ReturnsIdenticalValues()
    {
        // Diurnal cycle should repeat every 86400 s
        for (double t = 0; t < 86_400; t += 7200)
        {
            var obs1 = DefaultProvider.GetObservation(t);
            var obs2 = DefaultProvider.GetObservation(t + 86_400);

            Assert.Equal(obs1.TemperatureCelsius, obs2.TemperatureCelsius, precision: 6);
            Assert.Equal(obs1.RelativeHumidity, obs2.RelativeHumidity, precision: 6);
        }
    }

    [Fact]
    public void Constructor_ZeroTempAmplitude_IsValid()
    {
        // Zero amplitude means constant temperature throughout the day
        var provider = new SyntheticWeatherProvider(baseTempCelsius: 20.0, tempAmplitude: 0.0);
        var obs0  = provider.GetObservation(0);
        var obs12 = provider.GetObservation(43_200);

        Assert.Equal(20.0, obs0.TemperatureCelsius,  precision: 9);
        Assert.Equal(20.0, obs12.TemperatureCelsius, precision: 9);
    }

    [Fact]
    public void Constructor_NegativeTempAmplitude_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SyntheticWeatherProvider(tempAmplitude: -1.0));
    }

    [Fact]
    public void Constructor_HumidityOutOfRange_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SyntheticWeatherProvider(baseRelativeHumidity: 1.5));
    }

    [Fact]
    public void Constructor_NegativeChloride_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SyntheticWeatherProvider(chlorideConcentration: -0.01));
    }
}

// ── CsvWeatherProvider ────────────────────────────────────────────────────────

public class CsvWeatherProviderTests
{
    private const string MinimalCsv =
        "TimeSeconds,TemperatureCelsius,RelativeHumidity,ChlorideConcentration,Precipitation,WindSpeed\n" +
        "0,20.0,0.75,0.10,0.0,2.0\n" +
        "3600,24.0,0.65,0.12,0.5,3.0\n" +
        "7200,22.0,0.70,0.11,0.0,2.5\n";

    private static CsvWeatherProvider MakeProvider(string csv) =>
        new CsvWeatherProvider(new StringReader(csv));

    [Fact]
    public void GetObservation_AtFirstTimePoint_ReturnsExactValues()
    {
        var provider = MakeProvider(MinimalCsv);
        var obs = provider.GetObservation(0.0);

        Assert.Equal(20.0, obs.TemperatureCelsius, precision: 9);
        Assert.Equal(0.75, obs.RelativeHumidity,   precision: 9);
        Assert.Equal(0.10, obs.ChlorideConcentration, precision: 9);
    }

    [Fact]
    public void GetObservation_AtLastTimePoint_ReturnsExactValues()
    {
        var provider = MakeProvider(MinimalCsv);
        var obs = provider.GetObservation(7200.0);

        Assert.Equal(22.0, obs.TemperatureCelsius, precision: 9);
    }

    [Fact]
    public void GetObservation_BeforeFirstTimePoint_ClampedToFirst()
    {
        var provider = MakeProvider(MinimalCsv);
        var obs = provider.GetObservation(-100.0);

        Assert.Equal(20.0, obs.TemperatureCelsius, precision: 9);
    }

    [Fact]
    public void GetObservation_AfterLastTimePoint_ClampedToLast()
    {
        var provider = MakeProvider(MinimalCsv);
        var obs = provider.GetObservation(100_000.0);

        Assert.Equal(22.0, obs.TemperatureCelsius, precision: 9);
    }

    [Fact]
    public void GetObservation_MidpointBetweenRows_Interpolated()
    {
        var provider = MakeProvider(MinimalCsv);
        // Midpoint between t=0 (20.0°C) and t=3600 (24.0°C) should be 22.0°C
        var obs = provider.GetObservation(1800.0);

        Assert.Equal(22.0, obs.TemperatureCelsius, precision: 9);
        Assert.Equal(0.70, obs.RelativeHumidity,   precision: 9);
    }

    [Fact]
    public void GetObservation_InterpolatesAllFields()
    {
        var provider = MakeProvider(MinimalCsv);
        // Midpoint between t=0 and t=3600
        var obs = provider.GetObservation(1800.0);

        Assert.Equal(0.25,  obs.Precipitation, precision: 9);
        Assert.Equal(2.5,   obs.WindSpeed,     precision: 9);
    }

    [Fact]
    public void GetObservation_OptionalColumnsDefaultToZero()
    {
        const string csvNoOptional =
            "TimeSeconds,TemperatureCelsius,RelativeHumidity,ChlorideConcentration\n" +
            "0,20.0,0.75,0.10\n" +
            "3600,24.0,0.65,0.12\n";

        var provider = MakeProvider(csvNoOptional);
        var obs = provider.GetObservation(1800.0);

        Assert.Equal(0.0, obs.Precipitation);
        Assert.Equal(0.0, obs.WindSpeed);
    }

    [Fact]
    public void Constructor_EmptyData_Throws()
    {
        const string csvHeaderOnly =
            "TimeSeconds,TemperatureCelsius,RelativeHumidity,ChlorideConcentration\n";

        Assert.Throws<InvalidOperationException>(() => MakeProvider(csvHeaderOnly));
    }

    [Fact]
    public void Constructor_MalformedRow_Throws()
    {
        const string badCsv =
            "TimeSeconds,TemperatureCelsius,RelativeHumidity,ChlorideConcentration\n" +
            "0,20.0,0.75\n"; // only 3 columns, need 4

        Assert.Throws<FormatException>(() => MakeProvider(badCsv));
    }
}

// ── WeatherDrivenAtmosphericConditions ────────────────────────────────────────

public class WeatherDrivenAtmosphericConditionsTests
{
    private static readonly WeatherObservation TestObservation =
        new WeatherObservation(25.0, 0.80, 0.50);

    [Fact]
    public void WeatherDrivenAtmosphericConditions_IsAssignableToIElectrolyte()
    {
        IElectrolyte electrolyte = new WeatherDrivenAtmosphericConditions(TestObservation);
        Assert.NotNull(electrolyte);
    }

    [Fact]
    public void TemperatureKelvin_ConvertsCorrectly()
    {
        var conditions = new WeatherDrivenAtmosphericConditions(TestObservation);
        Assert.Equal(298.15, conditions.TemperatureKelvin, precision: 9);
    }

    [Fact]
    public void pH_WithZeroChloride_ApproximatelyNeutral()
    {
        var obs = new WeatherObservation(20.0, 0.70, 0.0);
        var conditions = new WeatherDrivenAtmosphericConditions(obs);

        // log10(1 + 0) = 0, so pH should be 7.0
        Assert.Equal(7.0, conditions.pH, precision: 9);
    }

    [Fact]
    public void pH_WithHighChloride_LowerThanNeutral()
    {
        var conditions = new WeatherDrivenAtmosphericConditions(TestObservation);
        Assert.True(conditions.pH < 7.0);
    }

    [Fact]
    public void IonicConductivity_PositiveForNonZeroHumidityAndChloride()
    {
        var conditions = new WeatherDrivenAtmosphericConditions(TestObservation);
        Assert.True(conditions.IonicConductivity > 0.0);
    }

    [Fact]
    public void Concentration_EqualsChlorideConcentration()
    {
        var conditions = new WeatherDrivenAtmosphericConditions(TestObservation);
        Assert.Equal(0.50, conditions.Concentration, precision: 9);
    }

    [Fact]
    public void Observation_ReturnsOriginalSnapshot()
    {
        var conditions = new WeatherDrivenAtmosphericConditions(TestObservation);
        Assert.Same(TestObservation, conditions.Observation);
    }

    [Fact]
    public void Constructor_NullObservation_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new WeatherDrivenAtmosphericConditions(null!));
    }

    [Fact]
    public void MatchesAtmosphericConditions_ForEquivalentInputs()
    {
        // WeatherDrivenAtmosphericConditions should produce the same electrochemical
        // properties as AtmosphericConditions when given identical inputs
        var staticConditions = new AtmosphericConditions(25.0, 0.80, 0.50);
        var weatherConditions = new WeatherDrivenAtmosphericConditions(
            new WeatherObservation(25.0, 0.80, 0.50));

        Assert.Equal(staticConditions.TemperatureKelvin, weatherConditions.TemperatureKelvin, precision: 9);
        Assert.Equal(staticConditions.pH,                weatherConditions.pH,                precision: 9);
        Assert.Equal(staticConditions.IonicConductivity, weatherConditions.IonicConductivity, precision: 9);
        Assert.Equal(staticConditions.Concentration,     weatherConditions.Concentration,     precision: 9);
    }
}

// ── IWeatherProvider contract tests ──────────────────────────────────────────

public class WeatherProviderContractTests
{
    [Fact]
    public void SyntheticWeatherProvider_ImplementsIWeatherProvider()
    {
        IWeatherProvider provider = new SyntheticWeatherProvider();
        Assert.NotNull(provider.GetObservation(0.0));
    }

    [Fact]
    public void CsvWeatherProvider_ImplementsIWeatherProvider()
    {
        const string csv =
            "TimeSeconds,TemperatureCelsius,RelativeHumidity,ChlorideConcentration\n" +
            "0,20.0,0.75,0.10\n";

        IWeatherProvider provider = new CsvWeatherProvider(new StringReader(csv));
        Assert.NotNull(provider.GetObservation(0.0));
    }
}
