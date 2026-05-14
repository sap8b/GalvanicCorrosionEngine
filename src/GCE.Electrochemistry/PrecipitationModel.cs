namespace GCE.Electrochemistry;

/// <summary>
/// Models supersaturation-driven precipitation of dissolved corrosion ions.
/// </summary>
public sealed class PrecipitationModel
{
    /// <summary>
    /// Initialises a new <see cref="PrecipitationModel"/>.
    /// </summary>
    /// <param name="solubilityProduct">Solubility product Ksp (must be &gt; 0).</param>
    /// <param name="supersaturationThreshold">
    /// Supersaturation threshold Ω* above which precipitation is allowed (must be ≥ 1).
    /// </param>
    /// <param name="precipitationFraction">
    /// Fraction of the supersaturation driving force applied per update (0..1).
    /// </param>
    public PrecipitationModel(
        double solubilityProduct,
        double supersaturationThreshold = 1.0,
        double precipitationFraction = 1.0)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(solubilityProduct, 0.0, nameof(solubilityProduct));
        ArgumentOutOfRangeException.ThrowIfLessThan(supersaturationThreshold, 1.0, nameof(supersaturationThreshold));
        ArgumentOutOfRangeException.ThrowIfLessThan(precipitationFraction, 0.0, nameof(precipitationFraction));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(precipitationFraction, 1.0, nameof(precipitationFraction));

        SolubilityProduct = solubilityProduct;
        SupersaturationThreshold = supersaturationThreshold;
        PrecipitationFraction = precipitationFraction;
    }

    /// <summary>Gets the solubility product Ksp.</summary>
    public double SolubilityProduct { get; }

    /// <summary>Gets the supersaturation threshold Ω*.</summary>
    public double SupersaturationThreshold { get; }

    /// <summary>Gets the per-step precipitation fraction (0..1).</summary>
    public double PrecipitationFraction { get; }

    /// <summary>
    /// Computes ionic activity product IAP from a metal ion activity and a counter-ion activity term.
    /// </summary>
    public double ComputeIonActivityProduct(
        double metalIonActivity,
        double counterIonActivity,
        int counterIonStoichiometricExponent = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(metalIonActivity, nameof(metalIonActivity));
        ArgumentOutOfRangeException.ThrowIfNegative(counterIonActivity, nameof(counterIonActivity));
        ArgumentOutOfRangeException.ThrowIfLessThan(counterIonStoichiometricExponent, 1, nameof(counterIonStoichiometricExponent));

        return metalIonActivity * Math.Pow(counterIonActivity, counterIonStoichiometricExponent);
    }

    /// <summary>
    /// Returns whether the local state is supersaturated and precipitation is thermodynamically allowed.
    /// </summary>
    public bool IsSupersaturated(double ionActivityProduct)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ionActivityProduct, nameof(ionActivityProduct));
        return ionActivityProduct >= SolubilityProduct * SupersaturationThreshold;
    }

    /// <summary>
    /// Computes how much dissolved concentration should precipitate during one update.
    /// </summary>
    public double ComputePrecipitatedConcentration(
        double localConcentration,
        double ionActivityProduct)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(localConcentration, nameof(localConcentration));
        ArgumentOutOfRangeException.ThrowIfNegative(ionActivityProduct, nameof(ionActivityProduct));

        if (!IsSupersaturated(ionActivityProduct) || localConcentration <= 0.0)
            return 0.0;

        double saturationTarget = SolubilityProduct * SupersaturationThreshold;
        double supersaturationRatio = ionActivityProduct / saturationTarget;
        double drivingForceFraction = (supersaturationRatio - 1.0) / supersaturationRatio;
        double precipitated = localConcentration * drivingForceFraction * PrecipitationFraction;

        return Math.Clamp(precipitated, 0.0, localConcentration);
    }
}
