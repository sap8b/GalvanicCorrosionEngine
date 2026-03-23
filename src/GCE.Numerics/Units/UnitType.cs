namespace GCE.Numerics;

/// <summary>
/// Enumerates every physical unit supported by <see cref="UnitValue"/>.
/// Units are organised by measurement category; use
/// <see cref="UnitConversions.GetCategory"/> to retrieve the category for a given value.
/// </summary>
public enum UnitType
{
    // ── Dimensionless ────────────────────────────────────────────────────────
    /// <summary>Dimensionless scalar (ratio, coefficient, etc.).</summary>
    Dimensionless,

    /// <summary>Percentage (1 % = 0.01 dimensionless).</summary>
    Percent,

    // ── Temperature ──────────────────────────────────────────────────────────
    /// <summary>Degrees Celsius (°C).</summary>
    Celsius,

    /// <summary>Degrees Fahrenheit (°F).</summary>
    Fahrenheit,

    /// <summary>Kelvin (K) — SI base unit for temperature.</summary>
    Kelvin,

    // ── Length ───────────────────────────────────────────────────────────────
    /// <summary>Metres (m) — SI base unit for length.</summary>
    Meters,

    /// <summary>Centimetres (cm).</summary>
    Centimeters,

    /// <summary>Millimetres (mm).</summary>
    Millimeters,

    /// <summary>Micrometres / microns (μm).</summary>
    Micrometers,

    /// <summary>Kilometres (km).</summary>
    Kilometers,

    // ── Mass ─────────────────────────────────────────────────────────────────
    /// <summary>Kilograms (kg) — SI base unit for mass.</summary>
    Kilograms,

    /// <summary>Grams (g).</summary>
    Grams,

    /// <summary>Milligrams (mg).</summary>
    Milligrams,

    // ── Time ─────────────────────────────────────────────────────────────────
    /// <summary>Seconds (s) — SI base unit for time.</summary>
    Seconds,

    /// <summary>Minutes (min).</summary>
    Minutes,

    /// <summary>Hours (h).</summary>
    Hours,

    /// <summary>Days (d).</summary>
    Days,

    /// <summary>Julian years (a) — 365.25 days.</summary>
    Years,

    // ── Pressure ─────────────────────────────────────────────────────────────
    /// <summary>Pascals (Pa) — SI base unit for pressure.</summary>
    Pascals,

    /// <summary>Kilopascals (kPa).</summary>
    KiloPascals,

    /// <summary>Megapascals (MPa).</summary>
    MegaPascals,

    /// <summary>Bar (bar).</summary>
    Bar,

    /// <summary>Standard atmospheres (atm).</summary>
    Atmospheres,

    // ── Electric Potential ───────────────────────────────────────────────────
    /// <summary>Volts (V) — SI base unit for electric potential.</summary>
    Volts,

    /// <summary>Millivolts (mV).</summary>
    Millivolts,

    // ── Current Density ──────────────────────────────────────────────────────
    /// <summary>Amperes per square metre (A/m²) — SI base unit for current density.</summary>
    AmperesPerSquareMeter,

    /// <summary>Milliamperes per square metre (mA/m²).</summary>
    MilliAmperesPerSquareMeter,

    /// <summary>Milliamperes per square centimetre (mA/cm²).</summary>
    MilliAmperesPerSquareCentimeter,

    // ── Molar Concentration ──────────────────────────────────────────────────
    /// <summary>Moles per litre (mol/L = mol/dm³) — base unit for molar concentration.</summary>
    MolesPerLiter,

    /// <summary>Millimoles per litre (mmol/L).</summary>
    MillimolesPerLiter,

    // ── Mass Concentration ───────────────────────────────────────────────────
    /// <summary>Grams per litre (g/L) — base unit for mass concentration.</summary>
    GramsPerLiter,

    /// <summary>Parts per million by mass (mg/kg ≈ mg/L for dilute aqueous solutions).</summary>
    PartsPerMillion,

    // ── Corrosion Rate ───────────────────────────────────────────────────────
    /// <summary>Millimetres per year (mm/a) — conventional base unit for corrosion rate.</summary>
    MillimetersPerYear,

    /// <summary>Micrometres per year (μm/a).</summary>
    MicrometersPerYear,

    /// <summary>Mils per year (mpy) — 1 mil = 0.0254 mm.</summary>
    MilsPerYear,
}
