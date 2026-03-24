using GCE.Core;

namespace GCE.Atmosphere;

/// <summary>
/// Models the evolution of a thin electrolyte film or droplet field on a metal
/// surface as atmospheric conditions change over time.
/// </summary>
/// <remarks>
/// <para>
/// Call <see cref="Advance"/> once per time step, supplying the current weather
/// observation and (optionally) the incident solar irradiance.  The method returns
/// the updated <see cref="FilmState"/> and also updates <see cref="State"/>.
/// </para>
/// <para><b>Physics summary</b></para>
/// <para>
/// <i>Film thickness</i> (equivalent depth of liquid water per unit area) evolves
/// through three additive processes:
/// <list type="bullet">
///   <item><description>
///     <b>Evaporation</b> — film thinning proportional to the RH deficit below
///     the equilibrium RH of the salt solution, enhanced by wind speed.
///   </description></item>
///   <item><description>
///     <b>Condensation</b> — film thickening proportional to the RH excess above
///     the deliquescence threshold (DRH).
///   </description></item>
///   <item><description>
///     <b>Precipitation</b> — direct addition from rain, converted from mm/h to
///     an equivalent thickness growth rate in m/s.
///   </description></item>
/// </list>
/// </para>
/// <para>
/// <i>Salt concentration</i> is updated via mass conservation: as the film
/// thickens (dilution) or thins (concentration), the total moles of salt per unit
/// area are preserved.  If the film dries out completely the concentration resets
/// to the initial value set in <see cref="FilmEvolutionParameters"/>.
/// </para>
/// <para>
/// <i>Surface temperature</i> may exceed the ambient air temperature when solar
/// radiation is absorbed.  A steady-state energy balance gives
/// <c>ΔT = α · G / h_conv</c>, where α is the solar absorptivity, G is the
/// irradiance (W/m²), and h_conv is the convective heat-transfer coefficient.
/// </para>
/// <para>
/// <i>Deliquescence / efflorescence</i> phase transitions are triggered when
/// the ambient RH crosses the temperature-corrected DRH (dry → wet) or the
/// film thickness falls below the minimum threshold while RH is below the ERH
/// (wet → dry).
/// </para>
/// <para>
/// <i>Droplet spreading and coalescence</i>: when the film is thin the electrolyte
/// exists as discrete droplets characterised by a surface coverage fraction.
/// Coverage relaxes towards an equilibrium value that depends on the
/// equivalent film thickness and the equilibrium contact angle.  Once the
/// film exceeds <see cref="DropletCoalescenceThicknessMeters"/> (10 µm) the
/// droplets are assumed to have fully coalesced into a continuous film
/// (coverage = 1).
/// </para>
/// </remarks>
public sealed class FilmEvolution
{
    /// <summary>
    /// Film thickness at which discrete droplets are assumed to have fully
    /// coalesced into a continuous liquid film.
    /// </summary>
    public const double DropletCoalescenceThicknessMeters = 1e-5; // 10 µm

    // Time constant (seconds) for droplet spreading towards equilibrium coverage.
    private const double SpreadingTimeConstantSeconds = 600.0;

    private readonly FilmEvolutionParameters _params;
    private FilmState _state;

    /// <summary>
    /// Initialises a new <see cref="FilmEvolution"/> instance.
    /// </summary>
    /// <param name="parameters">
    /// Physical parameters for the model.  If <see langword="null"/>, the defaults
    /// from <see cref="FilmEvolutionParameters"/> are used.
    /// </param>
    public FilmEvolution(FilmEvolutionParameters? parameters = null)
    {
        _params = parameters ?? new FilmEvolutionParameters();

        bool initiallyWet = _params.InitialThicknessMeters > _params.MinFilmThicknessMeters;
        double initialCoverage = initiallyWet
            ? EquilibriumCoverage(_params.InitialThicknessMeters)
            : 0.0;

        _state = new FilmState(
            ThicknessMeters:            _params.InitialThicknessMeters,
            SaltConcentrationMolPerL:   _params.InitialSaltConcentrationMolPerL,
            SurfaceTemperatureCelsius:  25.0,
            IsDeliquesced:              initiallyWet,
            CoverageFraction:           initialCoverage);
    }

    /// <summary>Gets the current film state.</summary>
    public FilmState State => _state;

    /// <summary>
    /// Advances the film state by one time step under the given atmospheric conditions.
    /// </summary>
    /// <param name="dtSeconds">
    /// Duration of the time step in seconds.  Must be positive.
    /// </param>
    /// <param name="observation">
    /// Current atmospheric weather observation.  Must not be null.
    /// </param>
    /// <param name="insolationWm2">
    /// Solar irradiance incident on the metal surface (W/m²).
    /// Pass 0 (default) for night-time or shaded conditions.
    /// Must be non-negative.
    /// </param>
    /// <returns>The updated <see cref="FilmState"/> after the time step.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="observation"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="dtSeconds"/> is not positive, or when
    /// <paramref name="insolationWm2"/> is negative.
    /// </exception>
    public FilmState Advance(double dtSeconds, IWeatherObservation observation, double insolationWm2 = 0.0)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dtSeconds);
        ArgumentOutOfRangeException.ThrowIfNegative(insolationWm2);

        double tSurface = CalculateSurfaceTemperature(observation, insolationWm2);
        double drh = DeliquescenceData.GetDeliquescenceRH(_params.Salt, tSurface);
        double erh = DeliquescenceData.GetEfflorescenceRH(_params.Salt, tSurface);

        bool   isDeliquesced = _state.IsDeliquesced;
        double thickness     = _state.ThicknessMeters;
        double saltConc      = _state.SaltConcentrationMolPerL;
        double coverage      = _state.CoverageFraction;

        // ── Phase transitions ──────────────────────────────────────────────
        if (!isDeliquesced && observation.RelativeHumidity >= drh)
            isDeliquesced = true;  // dry  → wet  (deliquescence)
        else if (isDeliquesced && observation.RelativeHumidity <= erh
                               && thickness <= _params.MinFilmThicknessMeters)
            isDeliquesced = false; // wet  → dry  (efflorescence)

        if (isDeliquesced)
        {
            // ── Film thickness mass balance ────────────────────────────────
            double dh = ComputeThicknessChange(dtSeconds, observation, thickness, drh);
            double newThickness = Math.Max(0.0, thickness + dh);

            // ── Salt mass conservation ─────────────────────────────────────
            // mol/m² (per unit area) = concentration (mol/L) × thickness (m) × 1000 L/m³
            double saltMassPerArea = saltConc * thickness;
            double newSaltConc;

            if (newThickness < _params.MinFilmThicknessMeters)
            {
                // Film dried out completely → efflorescence
                newThickness  = 0.0;
                newSaltConc   = _params.InitialSaltConcentrationMolPerL;
                isDeliquesced = false;
                coverage      = 0.0;
            }
            else
            {
                newSaltConc = saltMassPerArea / newThickness;
                coverage    = UpdateCoverage(coverage, newThickness, dtSeconds);
            }

            _state = new FilmState(newThickness, newSaltConc, tSurface, isDeliquesced, coverage);
        }
        else
        {
            // ── Dry surface: only rain can initiate a new film ─────────────
            double rainThickness = RainThicknessAdded(dtSeconds, observation);

            if (rainThickness > _params.MinFilmThicknessMeters)
            {
                // Rain wets the surface; use initial salt concentration as reference
                _state = new FilmState(
                    rainThickness,
                    _params.InitialSaltConcentrationMolPerL,
                    tSurface,
                    IsDeliquesced: true,
                    CoverageFraction: 1.0); // rain produces a continuous film
            }
            else
            {
                _state = new FilmState(0.0, saltConc, tSurface, IsDeliquesced: false, CoverageFraction: 0.0);
            }
        }

        return _state;
    }

    /// <summary>
    /// Calculates the metal surface temperature including solar heating.
    /// </summary>
    /// <param name="observation">Ambient weather conditions.</param>
    /// <param name="insolationWm2">Incident solar irradiance (W/m²).</param>
    /// <returns>Surface temperature in °C.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="observation"/> is null.
    /// </exception>
    public double CalculateSurfaceTemperature(IWeatherObservation observation, double insolationWm2 = 0.0)
    {
        ArgumentNullException.ThrowIfNull(observation);
        // Steady-state energy balance:
        //   Q_absorbed = α · G  (W/m²)
        //   Q_convective_loss = HTC · ΔT
        //   At steady state: ΔT = α · G / HTC
        double deltaT = _params.SolarAbsorptivity * insolationWm2 / _params.HeatTransferCoefficient;
        return observation.TemperatureCelsius + deltaT;
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private double ComputeThicknessChange(
        double dt,
        IWeatherObservation obs,
        double currentThickness,
        double drh)
    {
        // Evaporation: acts when RH < DRH of the solution
        double rhDeficit  = Math.Max(0.0, drh - obs.RelativeHumidity);
        double windFactor = 1.0 + _params.WindEvaporationFactor * obs.WindSpeed;
        double evapRate   = _params.EvaporationCoefficient * rhDeficit * windFactor;

        // Condensation: acts when RH > DRH
        double rhExcess  = Math.Max(0.0, obs.RelativeHumidity - drh);
        double condRate  = _params.CondensationCoefficient * rhExcess;

        // Rain (precipitation in mm/h converted to m/s)
        double rainRate = obs.Precipitation / 3_600_000.0;

        return (condRate + rainRate - evapRate) * dt;
    }

    private static double RainThicknessAdded(double dt, IWeatherObservation obs)
        => obs.Precipitation / 3_600_000.0 * dt;

    /// <summary>
    /// Updates the surface coverage fraction using a relaxation model towards
    /// an equilibrium value derived from the film thickness and contact angle.
    /// </summary>
    /// <remarks>
    /// For a spherical-cap droplet with equilibrium contact angle θ, the base
    /// radius r satisfies h = r · tan(θ/2), so coverage (∝ r²) scales as h².
    /// This gives an equilibrium coverage of:
    /// <code>
    ///   f_eq = min(1, (h / h_coalesce) / tan(θ/2))
    /// </code>
    /// The actual coverage relaxes towards f_eq with a time constant of
    /// <see cref="SpreadingTimeConstantSeconds"/>.
    /// </remarks>
    private double UpdateCoverage(double currentCoverage, double newThickness, double dt)
    {
        double feq = EquilibriumCoverage(newThickness);

        // Exponential relaxation: f(t) = f_eq + (f0 - f_eq)·exp(-dt/τ)
        double newCoverage = feq + (currentCoverage - feq) * Math.Exp(-dt / SpreadingTimeConstantSeconds);
        return Math.Clamp(newCoverage, 0.0, 1.0);
    }

    private double EquilibriumCoverage(double thickness)
    {
        if (thickness >= DropletCoalescenceThicknessMeters)
            return 1.0;

        // Spreading factor from contact angle: f_eq = (h / h_coalesce) / tan(θ/2)
        double thetaRad    = _params.EquilibriumContactAngleDegrees * Math.PI / 180.0;
        double tanHalfTheta = Math.Tan(thetaRad / 2.0);
        double rawCoverage  = (thickness / DropletCoalescenceThicknessMeters) / tanHalfTheta;
        return Math.Clamp(rawCoverage, 0.0, 1.0);
    }
}
