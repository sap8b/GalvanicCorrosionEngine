using GCE.Core;

namespace GCE.Electrochemistry;

/// <summary>
/// Models the Hydrogen Evolution Reaction (HER) as an <see cref="IElectrochemicalReaction"/>
/// using Butler–Volmer kinetics with a pH-corrected equilibrium potential.
/// </summary>
/// <remarks>
/// <para>
/// The HER in acidic conditions: 2H⁺ + 2e⁻ → H₂ (E° = 0.000 V vs. SHE).
/// </para>
/// <para>
/// The equilibrium potential is calculated via the Nernst equation:
/// <code>E_eq = −(RT/F)·ln(10)·pH − (RT/2F)·ln(pH₂)</code>
/// At standard hydrogen pressure (pH₂ = 1 atm) this simplifies to:
/// <code>E_eq = −(RT/F)·ln(10)·pH ≈ −0.05916·pH</code> at 25 °C.
/// </para>
/// <para>
/// The current density is the net Butler–Volmer response to the overpotential
/// η = E − E_eq.  Negative values represent cathodic (H₂ evolution) current;
/// positive values represent the reverse (H₂ oxidation) current.  An optional limiting
/// current density caps the cathodic branch to model proton mass-transport limitation.
/// </para>
/// </remarks>
public sealed class HydrogenEvolutionReaction : IElectrochemicalReaction
{
    private readonly ButlerVolmerKinetics _kinetics;

    /// <summary>Gets the pH used to calculate the equilibrium potential.</summary>
    public double pH { get; }

    /// <summary>Gets the hydrogen partial pressure (atm) used to calculate the equilibrium potential.</summary>
    public double HydrogenPartialPressure { get; }

    /// <summary>
    /// Gets the equilibrium (Nernst) potential of the HER at the specified local conditions (V vs. SHE).
    /// </summary>
    public double EquilibriumPotential { get; }

    /// <param name="exchangeCurrentDensity">Exchange current density i₀ (A/m²); must be positive.</param>
    /// <param name="pH">pH of the electrolyte (default 0.0 for strongly acidic).</param>
    /// <param name="hydrogenPartialPressure">
    /// Hydrogen partial pressure in atm (default 1.0, standard conditions); must be positive.
    /// </param>
    /// <param name="anodicTransferCoefficient">Anodic charge-transfer coefficient α_a (0–1, default 0.5).</param>
    /// <param name="cathodicTransferCoefficient">Cathodic charge-transfer coefficient α_c (0–1, default 0.5).</param>
    /// <param name="temperatureKelvin">Absolute temperature in Kelvin (default 298.15).</param>
    /// <param name="limitingCurrentDensity">
    /// Optional proton mass-transport limiting current density (A/m²); must be positive if provided.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any numeric parameter is outside its valid range.
    /// </exception>
    public HydrogenEvolutionReaction(
        double exchangeCurrentDensity,
        double pH = 0.0,
        double hydrogenPartialPressure = 1.0,
        double anodicTransferCoefficient = 0.5,
        double cathodicTransferCoefficient = 0.5,
        double temperatureKelvin = 298.15,
        double? limitingCurrentDensity = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(hydrogenPartialPressure, 0.0, nameof(hydrogenPartialPressure));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(temperatureKelvin, 0.0, nameof(temperatureKelvin));

        this.pH = pH;
        HydrogenPartialPressure = hydrogenPartialPressure;

        // Nernst: E_eq = 0 + (RT/2F)·ln([H⁺]² / pH₂)
        //              = −(RT/F)·ln(10)·pH − (RT/2F)·ln(pH₂)
        double rtF = PhysicalConstants.GasConstant * temperatureKelvin / PhysicalConstants.Faraday;
        EquilibriumPotential = -rtF * Math.Log(10) * pH
                               - (rtF / 2.0) * Math.Log(hydrogenPartialPressure);

        _kinetics = new ButlerVolmerKinetics(
            exchangeCurrentDensity,
            anodicTransferCoefficient,
            cathodicTransferCoefficient,
            temperatureKelvin,
            limitingCurrentDensity);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Returns the net Butler–Volmer current density at the given electrode potential.
    /// Negative values represent cathodic HER current (H₂ evolution).
    /// </remarks>
    public double CurrentDensity(double potential)
    {
        double eta = potential - EquilibriumPotential;
        return _kinetics.CurrentDensity(eta);
    }
}
