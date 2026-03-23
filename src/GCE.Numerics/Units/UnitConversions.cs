namespace GCE.Numerics;

/// <summary>
/// Provides unit-category classification and value conversion for all
/// <see cref="UnitType"/> values supported by <see cref="UnitValue"/>.
/// </summary>
/// <remarks>
/// <para>
/// Linear conversions are performed via a two-step round-trip through each
/// category's SI base unit:
/// <c>result = ToBaseUnit(value, from) * FromBaseUnitFactor(to)</c>.
/// </para>
/// <para>
/// Temperature is the sole affine (non-linear) category and is handled
/// through a dedicated Kelvin pivot.
/// </para>
/// </remarks>
public static class UnitConversions
{
    // ── Category lookup ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns the <see cref="UnitCategory"/> that the given <paramref name="unit"/> belongs to.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="unit"/> is not a recognised <see cref="UnitType"/> value.
    /// </exception>
    public static UnitCategory GetCategory(UnitType unit) => unit switch
    {
        UnitType.Dimensionless or UnitType.Percent
            => UnitCategory.Dimensionless,

        UnitType.Celsius or UnitType.Fahrenheit or UnitType.Kelvin
            => UnitCategory.Temperature,

        UnitType.Meters or UnitType.Centimeters or UnitType.Millimeters
            or UnitType.Micrometers or UnitType.Kilometers
            => UnitCategory.Length,

        UnitType.Kilograms or UnitType.Grams or UnitType.Milligrams
            => UnitCategory.Mass,

        UnitType.Seconds or UnitType.Minutes or UnitType.Hours
            or UnitType.Days or UnitType.Years
            => UnitCategory.Time,

        UnitType.Pascals or UnitType.KiloPascals or UnitType.MegaPascals
            or UnitType.Bar or UnitType.Atmospheres
            => UnitCategory.Pressure,

        UnitType.Volts or UnitType.Millivolts
            => UnitCategory.ElectricPotential,

        UnitType.AmperesPerSquareMeter or UnitType.MilliAmperesPerSquareMeter
            or UnitType.MilliAmperesPerSquareCentimeter
            => UnitCategory.CurrentDensity,

        UnitType.MolesPerLiter or UnitType.MillimolesPerLiter
            => UnitCategory.MolarConcentration,

        UnitType.GramsPerLiter or UnitType.PartsPerMillion
            => UnitCategory.MassConcentration,

        UnitType.MillimetersPerYear or UnitType.MicrometersPerYear or UnitType.MilsPerYear
            => UnitCategory.CorrosionRate,

        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit,
                 $"Unrecognised UnitType value '{unit}'.")
    };

    // ── Public conversion entry-point ────────────────────────────────────────

    /// <summary>
    /// Converts <paramref name="value"/> expressed in <paramref name="from"/> units
    /// to the equivalent value in <paramref name="to"/> units.
    /// </summary>
    /// <param name="value">The numeric magnitude to convert.</param>
    /// <param name="from">The source unit.</param>
    /// <param name="to">The target unit.</param>
    /// <returns>The converted magnitude in <paramref name="to"/> units.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="from"/> and <paramref name="to"/> belong to
    /// different <see cref="UnitCategory"/> values and therefore cannot be converted.
    /// </exception>
    public static double Convert(double value, UnitType from, UnitType to)
    {
        if (from == to)
            return value;

        UnitCategory fromCategory = GetCategory(from);
        UnitCategory toCategory   = GetCategory(to);

        if (fromCategory != toCategory)
            throw new InvalidOperationException(
                $"Cannot convert from {from} ({fromCategory}) to {to} ({toCategory}): " +
                "incompatible unit categories.");

        return fromCategory == UnitCategory.Temperature
            ? ConvertTemperature(value, from, to)
            : ToBaseUnit(value, from) / ToBaseUnit(1.0, to);
    }

    /// <summary>
    /// Converts <paramref name="value"/> from <paramref name="unit"/> to the SI base unit
    /// for its category.  For temperature the base unit is <see cref="UnitType.Kelvin"/>.
    /// </summary>
    public static double ToBaseUnit(double value, UnitType unit) => unit switch
    {
        // Dimensionless
        UnitType.Dimensionless => value,
        UnitType.Percent       => value * 0.01,

        // Temperature — affine: delegate to helper
        UnitType.Celsius or UnitType.Fahrenheit or UnitType.Kelvin
            => ConvertTemperature(value, unit, UnitType.Kelvin),

        // Length (base: metres)
        UnitType.Meters      => value,
        UnitType.Centimeters => value * 1e-2,
        UnitType.Millimeters => value * 1e-3,
        UnitType.Micrometers => value * 1e-6,
        UnitType.Kilometers  => value * 1e3,

        // Mass (base: kilograms)
        UnitType.Kilograms  => value,
        UnitType.Grams      => value * 1e-3,
        UnitType.Milligrams => value * 1e-6,

        // Time (base: seconds)
        UnitType.Seconds => value,
        UnitType.Minutes => value * 60.0,
        UnitType.Hours   => value * 3_600.0,
        UnitType.Days    => value * 86_400.0,
        UnitType.Years   => value * 31_557_600.0,   // Julian year = 365.25 days

        // Pressure (base: pascals)
        UnitType.Pascals      => value,
        UnitType.KiloPascals  => value * 1e3,
        UnitType.MegaPascals  => value * 1e6,
        UnitType.Bar          => value * 1e5,
        UnitType.Atmospheres  => value * 101_325.0,

        // Electric potential (base: volts)
        UnitType.Volts      => value,
        UnitType.Millivolts => value * 1e-3,

        // Current density (base: A/m²)
        UnitType.AmperesPerSquareMeter          => value,
        UnitType.MilliAmperesPerSquareMeter     => value * 1e-3,
        UnitType.MilliAmperesPerSquareCentimeter => value * 10.0,  // 1 mA/cm² = 10 A/m²

        // Molar concentration (base: mol/L)
        UnitType.MolesPerLiter      => value,
        UnitType.MillimolesPerLiter => value * 1e-3,

        // Mass concentration (base: g/L)
        UnitType.GramsPerLiter  => value,
        UnitType.PartsPerMillion => value * 1e-3,   // 1 ppm ≈ 1 mg/L = 0.001 g/L

        // Corrosion rate (base: mm/year)
        UnitType.MillimetersPerYear  => value,
        UnitType.MicrometersPerYear  => value * 1e-3,
        UnitType.MilsPerYear         => value * 0.0254,   // 1 mil = 0.0254 mm

        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit,
                 $"Unrecognised UnitType value '{unit}'.")
    };

    /// <summary>
    /// Converts <paramref name="value"/> expressed in the SI base unit for its category
    /// to <paramref name="unit"/>.
    /// </summary>
    public static double FromBaseUnit(double value, UnitType unit)
    {
        if (GetCategory(unit) == UnitCategory.Temperature)
            return ConvertTemperature(value, UnitType.Kelvin, unit);

        double factor = ToBaseUnit(1.0, unit);
        return value / factor;
    }

    // ── Temperature helper ───────────────────────────────────────────────────

    private static double ConvertTemperature(double value, UnitType from, UnitType to)
    {
        // Step 1: convert to Kelvin
        double kelvin = from switch
        {
            UnitType.Kelvin     => value,
            UnitType.Celsius    => value + 273.15,
            UnitType.Fahrenheit => (value + 459.67) * (5.0 / 9.0),
            _ => throw new ArgumentOutOfRangeException(nameof(from))
        };

        // Step 2: convert from Kelvin to target
        return to switch
        {
            UnitType.Kelvin     => kelvin,
            UnitType.Celsius    => kelvin - 273.15,
            UnitType.Fahrenheit => kelvin * (9.0 / 5.0) - 459.67,
            _ => throw new ArgumentOutOfRangeException(nameof(to))
        };
    }
}
