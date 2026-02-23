namespace GCE.Core;

/// <summary>
/// Represents the environmental conditions surrounding a corrosion scenario.
/// </summary>
public interface IEnvironment
{
    /// <summary>Gets the ambient temperature in Kelvin.</summary>
    double TemperatureKelvin { get; }

    /// <summary>Gets the pH of the electrolyte.</summary>
    double pH { get; }

    /// <summary>Gets the ionic conductivity of the electrolyte (S/m).</summary>
    double IonicConductivity { get; }
}
