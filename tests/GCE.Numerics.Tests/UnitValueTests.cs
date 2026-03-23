using GCE.Numerics;

namespace GCE.Numerics.Tests;

public class UnitValueTests
{
    // ── Construction and basic properties ────────────────────────────────────

    [Fact]
    public void Constructor_StoresValueAndUnit()
    {
        var uv = new UnitValue(25.0, UnitType.Celsius);
        Assert.Equal(25.0, uv.Value);
        Assert.Equal(UnitType.Celsius, uv.Unit);
    }

    // ── ConvertTo ────────────────────────────────────────────────────────────

    [Fact]
    public void ConvertTo_CelsiusToKelvin_CorrectValue()
    {
        var celsius = new UnitValue(100.0, UnitType.Celsius);
        var kelvin  = celsius.ConvertTo(UnitType.Kelvin);

        Assert.Equal(UnitType.Kelvin, kelvin.Unit);
        Assert.Equal(373.15, kelvin.Value, precision: 9);
    }

    [Fact]
    public void ConvertTo_MillimetersToMeters()
    {
        var mm     = new UnitValue(1000.0, UnitType.Millimeters);
        var meters = mm.ConvertTo(UnitType.Meters);

        Assert.Equal(UnitType.Meters, meters.Unit);
        Assert.Equal(1.0, meters.Value, precision: 9);
    }

    [Fact]
    public void ConvertTo_IncompatibleUnit_Throws()
    {
        var temp = new UnitValue(25.0, UnitType.Celsius);
        Assert.Throws<InvalidOperationException>(() => temp.ConvertTo(UnitType.Meters));
    }

    // ── Addition ─────────────────────────────────────────────────────────────

    [Fact]
    public void Addition_SameUnit_SumsValues()
    {
        var a = new UnitValue(1.0, UnitType.Meters);
        var b = new UnitValue(2.0, UnitType.Meters);
        var result = a + b;

        Assert.Equal(UnitType.Meters, result.Unit);
        Assert.Equal(3.0, result.Value, precision: 9);
    }

    [Fact]
    public void Addition_CompatibleUnits_ConvertsBeforeAdding()
    {
        // 1 km + 500 m = 1500 m (result in left-hand unit: km)
        var km  = new UnitValue(1.0, UnitType.Kilometers);
        var m   = new UnitValue(500.0, UnitType.Meters);
        var sum = km + m;

        Assert.Equal(UnitType.Kilometers, sum.Unit);
        Assert.Equal(1.5, sum.Value, precision: 9);
    }

    [Fact]
    public void Addition_IncompatibleUnits_Throws()
    {
        var length = new UnitValue(1.0, UnitType.Meters);
        var mass   = new UnitValue(1.0, UnitType.Kilograms);
        Assert.Throws<InvalidOperationException>(() => _ = length + mass);
    }

    // ── Subtraction ──────────────────────────────────────────────────────────

    [Fact]
    public void Subtraction_SameUnit_DifferencesValues()
    {
        var a = new UnitValue(5.0, UnitType.Seconds);
        var b = new UnitValue(3.0, UnitType.Seconds);
        var result = a - b;

        Assert.Equal(UnitType.Seconds, result.Unit);
        Assert.Equal(2.0, result.Value, precision: 9);
    }

    [Fact]
    public void Subtraction_CompatibleUnits_ConvertsBeforeSubtracting()
    {
        // 1 hour − 30 min = 0.5 hours
        var hours   = new UnitValue(1.0, UnitType.Hours);
        var minutes = new UnitValue(30.0, UnitType.Minutes);
        var result  = hours - minutes;

        Assert.Equal(UnitType.Hours, result.Unit);
        Assert.Equal(0.5, result.Value, precision: 9);
    }

    // ── Negation ─────────────────────────────────────────────────────────────

    [Fact]
    public void Negation_NegatesValue()
    {
        var v = new UnitValue(3.0, UnitType.Volts);
        Assert.Equal(-3.0, (-v).Value);
        Assert.Equal(UnitType.Volts, (-v).Unit);
    }

    // ── Scalar multiplication ─────────────────────────────────────────────────

    [Fact]
    public void Multiplication_ByScalar_ScalesValue()
    {
        var v = new UnitValue(2.0, UnitType.MillimetersPerYear);
        Assert.Equal(6.0, (v * 3.0).Value, precision: 9);
        Assert.Equal(6.0, (3.0 * v).Value, precision: 9);
    }

    // ── Division ─────────────────────────────────────────────────────────────

    [Fact]
    public void Division_ByScalar_ScalesValue()
    {
        var v = new UnitValue(10.0, UnitType.Kilograms);
        Assert.Equal(5.0, (v / 2.0).Value, precision: 9);
    }

    [Fact]
    public void Division_ByZero_Throws()
    {
        var v = new UnitValue(1.0, UnitType.Meters);
        Assert.Throws<DivideByZeroException>(() => _ = v / 0.0);
    }

    // ── Equality ─────────────────────────────────────────────────────────────

    [Fact]
    public void Equality_SameUnitSameValue_IsEqual()
    {
        var a = new UnitValue(100.0, UnitType.Pascals);
        var b = new UnitValue(100.0, UnitType.Pascals);
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void Equality_EquivalentValuesInDifferentUnits_IsEqual()
    {
        // 1000 m == 1 km
        var meters    = new UnitValue(1000.0, UnitType.Meters);
        var kilometers = new UnitValue(1.0,   UnitType.Kilometers);
        Assert.True(meters == kilometers);
    }

    [Fact]
    public void Equality_DifferentValues_IsNotEqual()
    {
        var a = new UnitValue(1.0, UnitType.Meters);
        var b = new UnitValue(2.0, UnitType.Meters);
        Assert.True(a != b);
    }

    // ── Comparison ───────────────────────────────────────────────────────────

    [Fact]
    public void Comparison_LessThan_Works()
    {
        var small = new UnitValue(1.0, UnitType.Meters);
        var large = new UnitValue(2.0, UnitType.Meters);
        Assert.True(small < large);
        Assert.False(large < small);
    }

    [Fact]
    public void Comparison_AcrossCompatibleUnits_Works()
    {
        // 500 m < 1 km
        var m  = new UnitValue(500.0, UnitType.Meters);
        var km = new UnitValue(1.0,   UnitType.Kilometers);
        Assert.True(m < km);
        Assert.True(km > m);
    }

    [Fact]
    public void Comparison_IncompatibleUnits_Throws()
    {
        var length = new UnitValue(1.0, UnitType.Meters);
        var time   = new UnitValue(1.0, UnitType.Seconds);
        Assert.Throws<InvalidOperationException>(() => _ = length < time);
    }

    [Fact]
    public void CompareTo_EqualValues_ReturnsZero()
    {
        var a = new UnitValue(1.0, UnitType.Kilometers);
        var b = new UnitValue(1000.0, UnitType.Meters);
        Assert.Equal(0, a.CompareTo(b));
    }

    // ── GetHashCode ───────────────────────────────────────────────────────────

    [Fact]
    public void GetHashCode_EquivalentValues_SameHash()
    {
        var meters    = new UnitValue(1000.0, UnitType.Meters);
        var kilometers = new UnitValue(1.0,   UnitType.Kilometers);
        Assert.Equal(meters.GetHashCode(), kilometers.GetHashCode());
    }

    // ── ToString ─────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var v = new UnitValue(25.0, UnitType.Celsius);
        Assert.Equal("25 Celsius", v.ToString());
    }
}
