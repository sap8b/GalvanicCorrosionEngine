namespace GCE.Atmosphere;

/// <summary>
/// Tunable physical parameters that govern a <see cref="FilmEvolution"/> simulation.
/// </summary>
/// <remarks>
/// All parameters have physically motivated defaults that are appropriate for an
/// NaCl-contaminated steel surface in a moderate marine atmosphere.  Override
/// individual values using C# record <c>with</c> expressions.
/// </remarks>
public sealed record FilmEvolutionParameters
{
    /// <summary>
    /// Salt species deposited on the surface.  Determines the deliquescence and
    /// efflorescence thresholds.  Default: <see cref="CommonSalt.NaCl"/>.
    /// </summary>
    public CommonSalt Salt { get; init; } = CommonSalt.NaCl;

    /// <summary>
    /// Initial equivalent film thickness in metres.
    /// Zero represents a fully dry surface.  Default: 0.0 m.
    /// </summary>
    public double InitialThicknessMeters { get; init; } = 0.0;

    /// <summary>
    /// Initial dissolved salt concentration in mol/L.
    /// This value is also used to re-initialise the film after it dries out.
    /// Default: 0.1 mol/L.
    /// </summary>
    public double InitialSaltConcentrationMolPerL { get; init; } = 0.1;

    /// <summary>
    /// Evaporation mass-transfer coefficient (m/s per unit RH deficit).
    /// Controls how quickly the film thins when the ambient RH is below the
    /// equilibrium RH of the salt solution.  Default: 1 × 10⁻⁸ m/s.
    /// </summary>
    public double EvaporationCoefficient { get; init; } = 1e-8;

    /// <summary>
    /// Condensation mass-transfer coefficient (m/s per unit RH excess).
    /// Controls how quickly the film thickens when the ambient RH exceeds
    /// the deliquescence threshold.  Default: 1 × 10⁻⁸ m/s.
    /// </summary>
    public double CondensationCoefficient { get; init; } = 1e-8;

    /// <summary>
    /// Solar absorptivity of the metal surface (dimensionless, 0–1).
    /// Fraction of incident solar radiation converted to surface heat.
    /// Default: 0.5.
    /// </summary>
    public double SolarAbsorptivity { get; init; } = 0.5;

    /// <summary>
    /// Convective heat-transfer coefficient between the surface and the ambient
    /// air (W m⁻² K⁻¹).  Used to compute the steady-state surface temperature
    /// rise due to solar heating.  Default: 10.0 W m⁻² K⁻¹.
    /// </summary>
    public double HeatTransferCoefficient { get; init; } = 10.0;

    /// <summary>
    /// Minimum film thickness in metres below which the film is considered to have
    /// fully dried out and efflorescence occurs.  Default: 1 × 10⁻⁹ m (1 nm).
    /// </summary>
    public double MinFilmThicknessMeters { get; init; } = 1e-9;

    /// <summary>
    /// Wind-enhancement factor for evaporation (s/m).
    /// The evaporation rate is multiplied by <c>1 + WindEvaporationFactor × windSpeed</c>.
    /// Default: 0.1 s/m.
    /// </summary>
    public double WindEvaporationFactor { get; init; } = 0.1;

    /// <summary>
    /// Equilibrium contact angle of droplets on the surface in degrees.
    /// Smaller angles indicate more hydrophilic (wetting) behaviour, leading to
    /// greater surface coverage at the same film thickness.  Default: 30°.
    /// </summary>
    public double EquilibriumContactAngleDegrees { get; init; } = 30.0;
}
