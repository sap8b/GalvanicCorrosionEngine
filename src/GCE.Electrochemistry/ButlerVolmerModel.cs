using GCE.Core;

namespace GCE.Electrochemistry;

/// <summary>
/// Computes anodic and cathodic current densities using the Butler–Volmer equation.
/// </summary>
/// <remarks>
/// i = i₀ · ( exp(α·F·η / R·T) − exp(−(1−α)·F·η / R·T) )
/// where η is the overpotential (E − E_eq).
/// </remarks>
public sealed class ButlerVolmerModel : ICorrosionModel
{
    private readonly IMaterial _material;
    private readonly IEnvironment _environment;

    /// <summary>Gets the anodic charge-transfer coefficient (dimensionless, 0–1).</summary>
    public double Alpha { get; }

    /// <param name="material">The material whose kinetics are modelled.</param>
    /// <param name="environment">The electrochemical environment.</param>
    /// <param name="alpha">Anodic charge-transfer coefficient (default 0.5).</param>
    public ButlerVolmerModel(
        IMaterial material,
        IEnvironment environment,
        double alpha = 0.5)
    {
        _material = material;
        _environment = environment;
        Alpha = alpha;
    }

    /// <inheritdoc/>
    public double ComputeCorrosionRate(double potential)
    {
        double currentDensity = ComputeCurrentDensity(potential);
        // Convert A/m² → mm/year using Faraday's law:
        // corrosion rate (mm/yr) = i (A/m²) × M / (n × F × ρ) × seconds_per_year × 1000
        const double secondsPerYear = 3.156e7;

        return Math.Abs(currentDensity) * _material.MolarMass
               / (_material.ElectronsTransferred * PhysicalConstants.Faraday * _material.Density)
               * secondsPerYear * 1000.0;
    }

    /// <summary>
    /// Computes the net current density (A/m²) at the given electrode potential.
    /// </summary>
    public double ComputeCurrentDensity(double potential)
    {
        double eta = potential - _material.StandardPotential;
        double factor = PhysicalConstants.Faraday / (PhysicalConstants.GasConstant * _environment.TemperatureKelvin);

        return _material.ExchangeCurrentDensity
               * (Math.Exp(Alpha * factor * eta)
               - Math.Exp(-(1.0 - Alpha) * factor * eta));
    }
}
