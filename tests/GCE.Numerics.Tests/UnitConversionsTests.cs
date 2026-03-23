using GCE.Numerics;

namespace GCE.Numerics.Tests;

public class UnitConversionsTests
{
    // ── GetCategory ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(UnitType.Dimensionless, UnitCategory.Dimensionless)]
    [InlineData(UnitType.Percent,       UnitCategory.Dimensionless)]
    [InlineData(UnitType.Celsius,       UnitCategory.Temperature)]
    [InlineData(UnitType.Fahrenheit,    UnitCategory.Temperature)]
    [InlineData(UnitType.Kelvin,        UnitCategory.Temperature)]
    [InlineData(UnitType.Meters,        UnitCategory.Length)]
    [InlineData(UnitType.Millimeters,   UnitCategory.Length)]
    [InlineData(UnitType.Micrometers,   UnitCategory.Length)]
    [InlineData(UnitType.Kilometers,    UnitCategory.Length)]
    [InlineData(UnitType.Kilograms,     UnitCategory.Mass)]
    [InlineData(UnitType.Grams,         UnitCategory.Mass)]
    [InlineData(UnitType.Milligrams,    UnitCategory.Mass)]
    [InlineData(UnitType.Seconds,       UnitCategory.Time)]
    [InlineData(UnitType.Minutes,       UnitCategory.Time)]
    [InlineData(UnitType.Hours,         UnitCategory.Time)]
    [InlineData(UnitType.Days,          UnitCategory.Time)]
    [InlineData(UnitType.Years,         UnitCategory.Time)]
    [InlineData(UnitType.Pascals,       UnitCategory.Pressure)]
    [InlineData(UnitType.KiloPascals,   UnitCategory.Pressure)]
    [InlineData(UnitType.Bar,           UnitCategory.Pressure)]
    [InlineData(UnitType.Atmospheres,   UnitCategory.Pressure)]
    [InlineData(UnitType.Volts,                           UnitCategory.ElectricPotential)]
    [InlineData(UnitType.Millivolts,                      UnitCategory.ElectricPotential)]
    [InlineData(UnitType.AmperesPerSquareMeter,           UnitCategory.CurrentDensity)]
    [InlineData(UnitType.MilliAmperesPerSquareMeter,      UnitCategory.CurrentDensity)]
    [InlineData(UnitType.MilliAmperesPerSquareCentimeter, UnitCategory.CurrentDensity)]
    [InlineData(UnitType.MolesPerLiter,                   UnitCategory.MolarConcentration)]
    [InlineData(UnitType.MillimolesPerLiter,              UnitCategory.MolarConcentration)]
    [InlineData(UnitType.GramsPerLiter,                   UnitCategory.MassConcentration)]
    [InlineData(UnitType.PartsPerMillion,                 UnitCategory.MassConcentration)]
    [InlineData(UnitType.MillimetersPerYear,              UnitCategory.CorrosionRate)]
    [InlineData(UnitType.MicrometersPerYear,              UnitCategory.CorrosionRate)]
    [InlineData(UnitType.MilsPerYear,                     UnitCategory.CorrosionRate)]
    public void GetCategory_ReturnsCorrectCategory(UnitType unit, UnitCategory expected)
    {
        Assert.Equal(expected, UnitConversions.GetCategory(unit));
    }

    // ── Identity conversion ───────────────────────────────────────────────────

    [Theory]
    [InlineData(UnitType.Meters,     42.0)]
    [InlineData(UnitType.Kelvin,    300.0)]
    [InlineData(UnitType.Seconds,   3600.0)]
    [InlineData(UnitType.Pascals,   101325.0)]
    public void Convert_SameUnit_ReturnsSameValue(UnitType unit, double value)
    {
        Assert.Equal(value, UnitConversions.Convert(value, unit, unit));
    }

    // ── Temperature conversions ──────────────────────────────────────────────

    [Theory]
    [InlineData(0.0,    273.15)]   // 0 °C = 273.15 K
    [InlineData(100.0,  373.15)]   // 100 °C = 373.15 K
    [InlineData(-273.15, 0.0)]     // absolute zero
    public void Convert_CelsiusToKelvin(double celsius, double expectedKelvin)
    {
        double result = UnitConversions.Convert(celsius, UnitType.Celsius, UnitType.Kelvin);
        Assert.Equal(expectedKelvin, result, precision: 9);
    }

    [Theory]
    [InlineData(32.0,   0.0)]       // 32 °F = 0 °C
    [InlineData(212.0,  100.0)]     // 212 °F = 100 °C
    [InlineData(-40.0,  -40.0)]     // −40 is the same in both scales
    public void Convert_FahrenheitToCelsius(double fahrenheit, double expectedCelsius)
    {
        double result = UnitConversions.Convert(fahrenheit, UnitType.Fahrenheit, UnitType.Celsius);
        Assert.Equal(expectedCelsius, result, precision: 9);
    }

    [Fact]
    public void Convert_KelvinToFahrenheit_BoilingPoint()
    {
        // 373.15 K = 212 °F
        double result = UnitConversions.Convert(373.15, UnitType.Kelvin, UnitType.Fahrenheit);
        Assert.Equal(212.0, result, precision: 9);
    }

    // ── Length conversions ───────────────────────────────────────────────────

    [Theory]
    [InlineData(1.0,    UnitType.Meters,      UnitType.Centimeters, 100.0)]
    [InlineData(1.0,    UnitType.Meters,      UnitType.Millimeters, 1000.0)]
    [InlineData(1.0,    UnitType.Kilometers,  UnitType.Meters,      1000.0)]
    [InlineData(1000.0, UnitType.Micrometers, UnitType.Millimeters, 1.0)]
    public void Convert_Length(double value, UnitType from, UnitType to, double expected)
    {
        Assert.Equal(expected, UnitConversions.Convert(value, from, to), precision: 9);
    }

    // ── Mass conversions ─────────────────────────────────────────────────────

    [Fact]
    public void Convert_KilogramsToGrams()
    {
        Assert.Equal(1000.0, UnitConversions.Convert(1.0, UnitType.Kilograms, UnitType.Grams), precision: 9);
    }

    [Fact]
    public void Convert_GramsToMilligrams()
    {
        Assert.Equal(1000.0, UnitConversions.Convert(1.0, UnitType.Grams, UnitType.Milligrams), precision: 9);
    }

    // ── Time conversions ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(1.0,  UnitType.Minutes, UnitType.Seconds, 60.0)]
    [InlineData(1.0,  UnitType.Hours,   UnitType.Minutes, 60.0)]
    [InlineData(1.0,  UnitType.Days,    UnitType.Hours,   24.0)]
    [InlineData(365.25, UnitType.Days,  UnitType.Years,   1.0)]
    public void Convert_Time(double value, UnitType from, UnitType to, double expected)
    {
        Assert.Equal(expected, UnitConversions.Convert(value, from, to), precision: 6);
    }

    // ── Pressure conversions ─────────────────────────────────────────────────

    [Theory]
    [InlineData(1.0,       UnitType.Atmospheres, UnitType.Pascals,     101325.0)]
    [InlineData(1.0,       UnitType.Bar,         UnitType.Pascals,     100000.0)]
    [InlineData(1.0,       UnitType.KiloPascals, UnitType.Pascals,     1000.0)]
    [InlineData(1_000_000, UnitType.Pascals,     UnitType.MegaPascals, 1.0)]
    public void Convert_Pressure(double value, UnitType from, UnitType to, double expected)
    {
        Assert.Equal(expected, UnitConversions.Convert(value, from, to), precision: 6);
    }

    // ── Electric potential conversions ───────────────────────────────────────

    [Fact]
    public void Convert_VoltsToMillivolts()
    {
        Assert.Equal(1000.0, UnitConversions.Convert(1.0, UnitType.Volts, UnitType.Millivolts), precision: 9);
    }

    // ── Current density conversions ──────────────────────────────────────────

    [Fact]
    public void Convert_MilliAmperesPerSquareCentimeterToAmperesPerSquareMeter()
    {
        // 1 mA/cm² = 10 A/m²
        Assert.Equal(10.0,
            UnitConversions.Convert(1.0,
                UnitType.MilliAmperesPerSquareCentimeter,
                UnitType.AmperesPerSquareMeter),
            precision: 9);
    }

    // ── Molar concentration conversions ──────────────────────────────────────

    [Fact]
    public void Convert_MolesToMillimoles()
    {
        Assert.Equal(1000.0,
            UnitConversions.Convert(1.0, UnitType.MolesPerLiter, UnitType.MillimolesPerLiter),
            precision: 9);
    }

    // ── Mass concentration conversions ───────────────────────────────────────

    [Fact]
    public void Convert_PartsPerMillionToGramsPerLiter()
    {
        // 1 ppm ≈ 0.001 g/L
        Assert.Equal(0.001,
            UnitConversions.Convert(1.0, UnitType.PartsPerMillion, UnitType.GramsPerLiter),
            precision: 9);
    }

    // ── Corrosion rate conversions ───────────────────────────────────────────

    [Theory]
    [InlineData(1.0, UnitType.MillimetersPerYear, UnitType.MicrometersPerYear, 1000.0)]
    [InlineData(1.0, UnitType.MilsPerYear,        UnitType.MillimetersPerYear, 0.0254)]
    public void Convert_CorrosionRate(double value, UnitType from, UnitType to, double expected)
    {
        Assert.Equal(expected, UnitConversions.Convert(value, from, to), precision: 9);
    }

    // ── Dimensionless conversions ────────────────────────────────────────────

    [Fact]
    public void Convert_PercentToDimensionless()
    {
        Assert.Equal(0.5, UnitConversions.Convert(50.0, UnitType.Percent, UnitType.Dimensionless), precision: 9);
    }

    // ── Error cases ──────────────────────────────────────────────────────────

    [Fact]
    public void Convert_IncompatibleCategories_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            UnitConversions.Convert(100.0, UnitType.Celsius, UnitType.Meters));
    }

    [Fact]
    public void GetCategory_UnrecognisedValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UnitConversions.GetCategory((UnitType)9999));
    }
}
