using GCE.Core;

namespace GCE.Electrochemistry;

/// <summary>
/// Computes electrode current densities using the full Butler–Volmer equation,
/// with optional mass-transport limiting current correction.
/// </summary>
/// <remarks>
/// <para>
/// Net current density:
/// <code>i = i₀ · ( exp(α_a·F·η / R·T) − exp(−α_c·F·η / R·T) )</code>
/// where η = E − E_eq is the overpotential.
/// </para>
/// <para>
/// When a <see cref="LimitingCurrentDensity"/> is supplied, a mass-transport correction
/// is applied to cap the net current magnitude at the limiting value:
/// <code>i_net = i_lim · i_BV / (i_lim + |i_BV|)</code>
/// This saturates smoothly to ±i_lim as |η| → ∞.
/// </para>
/// </remarks>
public sealed class ButlerVolmerKinetics : IElectrodeKinetics
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

    /// <summary>
    /// Gets the limiting current density i_lim (A/m²) for mass-transport correction,
    /// or <see langword="null"/> if no limiting current is applied.
    /// </summary>
    public double? LimitingCurrentDensity { get; }

    /// <param name="exchangeCurrentDensity">Exchange current density i₀ (A/m²); must be positive.</param>
    /// <param name="anodicTransferCoefficient">Anodic charge-transfer coefficient α_a (0–1).</param>
    /// <param name="cathodicTransferCoefficient">Cathodic charge-transfer coefficient α_c (0–1).</param>
    /// <param name="temperatureKelvin">Absolute temperature in Kelvin; must be positive.</param>
    /// <param name="limitingCurrentDensity">
    /// Optional limiting current density i_lim (A/m²) for mass-transport correction; must be positive if provided.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any numeric parameter is outside its valid range.
    /// </exception>
    public ButlerVolmerKinetics(
        double exchangeCurrentDensity,
        double anodicTransferCoefficient = 0.5,
        double cathodicTransferCoefficient = 0.5,
        double temperatureKelvin = 298.15,
        double? limitingCurrentDensity = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(exchangeCurrentDensity, 0.0, nameof(exchangeCurrentDensity));
        ArgumentOutOfRangeException.ThrowIfLessThan(anodicTransferCoefficient, 0.0, nameof(anodicTransferCoefficient));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(anodicTransferCoefficient, 1.0, nameof(anodicTransferCoefficient));
        ArgumentOutOfRangeException.ThrowIfLessThan(cathodicTransferCoefficient, 0.0, nameof(cathodicTransferCoefficient));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(cathodicTransferCoefficient, 1.0, nameof(cathodicTransferCoefficient));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(temperatureKelvin, 0.0, nameof(temperatureKelvin));

        if (limitingCurrentDensity.HasValue)
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(limitingCurrentDensity.Value, 0.0, nameof(limitingCurrentDensity));

        ExchangeCurrentDensity = exchangeCurrentDensity;
        AnodicTransferCoefficient = anodicTransferCoefficient;
        CathodicTransferCoefficient = cathodicTransferCoefficient;
        TemperatureKelvin = temperatureKelvin;
        LimitingCurrentDensity = limitingCurrentDensity;

        _thermalFactor = PhysicalConstants.Faraday / (PhysicalConstants.GasConstant * temperatureKelvin);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// When <see cref="LimitingCurrentDensity"/> is set, the result is capped via
    /// <c>i_lim · i_BV / (i_lim + |i_BV|)</c>.
    /// </remarks>
    public double CurrentDensity(double overpotential)
    {
        double iBV = AnodicCurrentDensity(overpotential) + CathodicCurrentDensity(overpotential);

        if (LimitingCurrentDensity.HasValue)
        {
            double iLim = LimitingCurrentDensity.Value;
            return iLim * iBV / (iLim + Math.Abs(iBV));
        }

        return iBV;
    }

    /// <inheritdoc/>
    public double AnodicCurrentDensity(double overpotential) =>
        ExchangeCurrentDensity * Math.Exp(AnodicTransferCoefficient * _thermalFactor * overpotential);

    /// <inheritdoc/>
    public double CathodicCurrentDensity(double overpotential) =>
        -ExchangeCurrentDensity * Math.Exp(-CathodicTransferCoefficient * _thermalFactor * overpotential);
}
