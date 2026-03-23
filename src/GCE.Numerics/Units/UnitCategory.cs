namespace GCE.Numerics;

/// <summary>
/// Identifies the physical dimension (measurement category) of a <see cref="UnitType"/>.
/// Only units within the same category can be converted to one another.
/// </summary>
public enum UnitCategory
{
    /// <summary>Dimensionless ratios and percentages.</summary>
    Dimensionless,

    /// <summary>Thermodynamic temperature (Celsius, Fahrenheit, Kelvin).</summary>
    Temperature,

    /// <summary>Spatial length or distance (m, mm, μm, …).</summary>
    Length,

    /// <summary>Mass (kg, g, mg).</summary>
    Mass,

    /// <summary>Duration (s, min, h, days, years).</summary>
    Time,

    /// <summary>Mechanical pressure (Pa, bar, atm, …).</summary>
    Pressure,

    /// <summary>Electric potential / voltage (V, mV).</summary>
    ElectricPotential,

    /// <summary>Electric current per unit area (A/m², mA/cm², …).</summary>
    CurrentDensity,

    /// <summary>Molar concentration (mol/L, mmol/L).</summary>
    MolarConcentration,

    /// <summary>Mass-based concentration (g/L, ppm by mass).</summary>
    MassConcentration,

    /// <summary>Linear corrosion rate (mm/year, μm/year, mils/year).</summary>
    CorrosionRate,
}
