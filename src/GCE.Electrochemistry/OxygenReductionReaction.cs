using GCE.Core;

namespace GCE.Electrochemistry;

/// <summary>
/// Models the Oxygen Reduction Reaction (ORR) as an <see cref="IElectrochemicalReaction"/>
/// using Butler–Volmer kinetics with a Nernst-corrected equilibrium potential.
/// </summary>
/// <remarks>
/// <para>
/// The ORR proceeds via two pathways:
/// <list type="bullet">
///   <item><description>
///     4-electron: O₂ + 4H⁺ + 4e⁻ → 2H₂O (E° = +1.229 V vs. SHE)
///   </description></item>
///   <item><description>
///     2-electron: O₂ + 2H⁺ + 2e⁻ → H₂O₂ (E° = +0.695 V vs. SHE)
///   </description></item>
/// </list>
/// </para>
/// <para>
/// The equilibrium potential is calculated via the Nernst equation:
/// <code>E_eq = E° − (RT/F)·ln(10)·pH + (RT/nF)·ln(pO₂)</code>
/// where n is the number of electrons transferred (4 or 2) and pO₂ is the oxygen
/// partial pressure in atm.
/// </para>
/// <para>
/// The current density is the net Butler–Volmer response to the overpotential
/// η = E − E_eq.  Negative values represent cathodic (O₂ reduction) current;
/// positive values represent the reverse (anodic) current.  An optional limiting
/// current density caps the cathodic branch to model oxygen mass-transport limitation.
/// </para>
/// </remarks>
public sealed class OxygenReductionReaction : IElectrochemicalReaction
{
    private readonly ButlerVolmerKinetics _kinetics;

    /// <summary>Gets the electron-transfer pathway (4-electron or 2-electron).</summary>
    public OrrPathway Pathway { get; }

    /// <summary>Gets the pH used to calculate the equilibrium potential.</summary>
    public double pH { get; }

    /// <summary>Gets the oxygen partial pressure (atm) used to calculate the equilibrium potential.</summary>
    public double OxygenPartialPressure { get; }

    /// <summary>
    /// Gets the equilibrium (Nernst) potential of the ORR at the specified local conditions (V vs. SHE).
    /// </summary>
    public double EquilibriumPotential { get; }

    /// <param name="exchangeCurrentDensity">Exchange current density i₀ (A/m²); must be positive.</param>
    /// <param name="pH">pH of the electrolyte (default 7.0).</param>
    /// <param name="oxygenPartialPressure">
    /// Oxygen partial pressure in atm (default 0.21 for air); must be positive.
    /// </param>
    /// <param name="pathway">
    /// Reaction pathway: <see cref="OrrPathway.FourElectron"/> (default) or
    /// <see cref="OrrPathway.TwoElectron"/>.
    /// </param>
    /// <param name="anodicTransferCoefficient">Anodic charge-transfer coefficient α_a (0–1, default 0.5).</param>
    /// <param name="cathodicTransferCoefficient">Cathodic charge-transfer coefficient α_c (0–1, default 0.5).</param>
    /// <param name="temperatureKelvin">Absolute temperature in Kelvin (default 298.15).</param>
    /// <param name="limitingCurrentDensity">
    /// Optional oxygen mass-transport limiting current density (A/m²); must be positive if provided.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any numeric parameter is outside its valid range.
    /// </exception>
    public OxygenReductionReaction(
        double exchangeCurrentDensity,
        double pH = 7.0,
        double oxygenPartialPressure = 0.21,
        OrrPathway pathway = OrrPathway.FourElectron,
        double anodicTransferCoefficient = 0.5,
        double cathodicTransferCoefficient = 0.5,
        double temperatureKelvin = 298.15,
        double? limitingCurrentDensity = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(oxygenPartialPressure, 0.0, nameof(oxygenPartialPressure));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(temperatureKelvin, 0.0, nameof(temperatureKelvin));

        Pathway = pathway;
        this.pH = pH;
        OxygenPartialPressure = oxygenPartialPressure;

        // Standard electrode potentials
        double e0 = pathway == OrrPathway.FourElectron ? 1.229 : 0.695;
        int n = pathway == OrrPathway.FourElectron ? 4 : 2;

        // Nernst: E_eq = E° + (RT/nF)·ln([H⁺]ⁿ · pO₂)
        //              = E° − (RT/F)·ln(10)·pH + (RT/nF)·ln(pO₂)
        double rtF = PhysicalConstants.GasConstant * temperatureKelvin / PhysicalConstants.Faraday;
        EquilibriumPotential = e0
            - rtF * Math.Log(10) * pH
            + (rtF / n) * Math.Log(oxygenPartialPressure);

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
    /// Negative values represent cathodic ORR current (O₂ reduction).
    /// </remarks>
    public double CurrentDensity(double potential)
    {
        double eta = potential - EquilibriumPotential;
        return _kinetics.CurrentDensity(eta);
    }
}
