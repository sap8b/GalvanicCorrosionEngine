using GCE.Core;

namespace GCE.Electrochemistry;

/// <summary>
/// Computes electrode current densities using the Tafel approximation — a simplified
/// form of Butler–Volmer kinetics valid at high overpotentials.
/// </summary>
/// <remarks>
/// <para>
/// At large overpotentials the back-reaction term in the full Butler–Volmer equation
/// becomes negligible and can be dropped:
/// <list type="bullet">
///   <item><description>
///     Anodic (η &gt; 0): <c>i ≈ i₀ · exp(α_a · F · η / R·T)</c>
///   </description></item>
///   <item><description>
///     Cathodic (η &lt; 0): <c>i ≈ −i₀ · exp(α_c · F · |η| / R·T)</c>
///   </description></item>
/// </list>
/// </para>
/// <para>
/// At η = 0 the model returns zero (no net current at equilibrium).
/// </para>
/// </remarks>
public sealed class TafelKinetics : IElectrodeKinetics
{
    private readonly double _thermalFactor; // F / (R·T)

    /// <inheritdoc/>
    public double ExchangeCurrentDensity { get; }

    /// <inheritdoc/>
    public double AnodicTransferCoefficient { get; }

    /// <inheritdoc/>
    public double CathodicTransferCoefficient { get; }

    /// <summary>Gets the temperature used for the kinetics calculation (K).</summary>
    public double TemperatureKelvin { get; }

    /// <param name="exchangeCurrentDensity">Exchange current density i₀ (A/m²); must be positive.</param>
    /// <param name="anodicTransferCoefficient">Anodic charge-transfer coefficient α_a (0–1).</param>
    /// <param name="cathodicTransferCoefficient">Cathodic charge-transfer coefficient α_c (0–1).</param>
    /// <param name="temperatureKelvin">Absolute temperature in Kelvin; must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any numeric parameter is outside its valid range.
    /// </exception>
    public TafelKinetics(
        double exchangeCurrentDensity,
        double anodicTransferCoefficient = 0.5,
        double cathodicTransferCoefficient = 0.5,
        double temperatureKelvin = 298.15)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(exchangeCurrentDensity, 0.0, nameof(exchangeCurrentDensity));
        ArgumentOutOfRangeException.ThrowIfLessThan(anodicTransferCoefficient, 0.0, nameof(anodicTransferCoefficient));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(anodicTransferCoefficient, 1.0, nameof(anodicTransferCoefficient));
        ArgumentOutOfRangeException.ThrowIfLessThan(cathodicTransferCoefficient, 0.0, nameof(cathodicTransferCoefficient));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(cathodicTransferCoefficient, 1.0, nameof(cathodicTransferCoefficient));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(temperatureKelvin, 0.0, nameof(temperatureKelvin));

        ExchangeCurrentDensity = exchangeCurrentDensity;
        AnodicTransferCoefficient = anodicTransferCoefficient;
        CathodicTransferCoefficient = cathodicTransferCoefficient;
        TemperatureKelvin = temperatureKelvin;

        _thermalFactor = PhysicalConstants.Faraday / (PhysicalConstants.GasConstant * temperatureKelvin);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Uses only the dominant exponential branch: anodic when η ≥ 0, cathodic when η &lt; 0.
    /// Returns exactly zero at η = 0.
    /// </remarks>
    public double CurrentDensity(double overpotential)
    {
        if (overpotential == 0.0)
            return 0.0;

        return overpotential > 0.0
            ? AnodicCurrentDensity(overpotential)
            : CathodicCurrentDensity(overpotential);
    }

    /// <inheritdoc/>
    public double AnodicCurrentDensity(double overpotential) =>
        ExchangeCurrentDensity * Math.Exp(AnodicTransferCoefficient * _thermalFactor * overpotential);

    /// <inheritdoc/>
    public double CathodicCurrentDensity(double overpotential) =>
        -ExchangeCurrentDensity * Math.Exp(-CathodicTransferCoefficient * _thermalFactor * overpotential);
}
