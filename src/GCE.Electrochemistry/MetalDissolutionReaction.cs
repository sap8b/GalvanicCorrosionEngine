using GCE.Core;

namespace GCE.Electrochemistry;

/// <summary>
/// Models a generic anodic metal dissolution reaction (M → Mⁿ⁺ + ne⁻) as an
/// <see cref="IElectrochemicalReaction"/> using Butler–Volmer kinetics with a
/// Nernst-corrected equilibrium potential.
/// </summary>
/// <remarks>
/// <para>
/// The equilibrium potential is calculated via the Nernst equation:
/// <code>E_eq = E° + (RT/nF)·ln([Mⁿ⁺])</code>
/// where E° is the standard electrode potential, n is the number of electrons
/// transferred, and [Mⁿ⁺] is the dissolved metal-ion activity (mol/L).
/// </para>
/// <para>
/// The current density is the net Butler–Volmer response to the overpotential
/// η = E − E_eq.  Positive values represent anodic (dissolution) current;
/// negative values represent cathodic (deposition) current.
/// </para>
/// </remarks>
public sealed class MetalDissolutionReaction : IElectrochemicalReaction
{
    private readonly ButlerVolmerKinetics _kinetics;

    /// <summary>Gets the standard electrode potential E° (V vs. SHE).</summary>
    public double StandardPotential { get; }

    /// <summary>Gets the number of electrons transferred per formula unit (n).</summary>
    public int ElectronsTransferred { get; }

    /// <summary>Gets the dissolved metal-ion activity/concentration (mol/L).</summary>
    public double MetalIonConcentration { get; }

    /// <summary>
    /// Gets the equilibrium (Nernst) potential of the dissolution reaction at the
    /// specified local conditions (V vs. SHE).
    /// </summary>
    public double EquilibriumPotential { get; }

    /// <param name="exchangeCurrentDensity">Exchange current density i₀ (A/m²); must be positive.</param>
    /// <param name="standardPotential">Standard electrode potential E° (V vs. SHE).</param>
    /// <param name="electronsTransferred">
    /// Number of electrons transferred per formula unit n (default 2); must be at least 1.
    /// </param>
    /// <param name="metalIonConcentration">
    /// Dissolved metal-ion activity/concentration (mol/L, default 1×10⁻⁶); must be positive.
    /// </param>
    /// <param name="anodicTransferCoefficient">Anodic charge-transfer coefficient α_a (0–1, default 0.5).</param>
    /// <param name="cathodicTransferCoefficient">Cathodic charge-transfer coefficient α_c (0–1, default 0.5).</param>
    /// <param name="temperatureKelvin">Absolute temperature in Kelvin (default 298.15).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any numeric parameter is outside its valid range.
    /// </exception>
    public MetalDissolutionReaction(
        double exchangeCurrentDensity,
        double standardPotential,
        int electronsTransferred = 2,
        double metalIonConcentration = 1e-6,
        double anodicTransferCoefficient = 0.5,
        double cathodicTransferCoefficient = 0.5,
        double temperatureKelvin = 298.15)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(electronsTransferred, 1, nameof(electronsTransferred));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(metalIonConcentration, 0.0, nameof(metalIonConcentration));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(temperatureKelvin, 0.0, nameof(temperatureKelvin));

        StandardPotential = standardPotential;
        ElectronsTransferred = electronsTransferred;
        MetalIonConcentration = metalIonConcentration;

        // Nernst: E_eq = E° + (RT/nF)·ln([Mⁿ⁺])
        double rtNF = PhysicalConstants.GasConstant * temperatureKelvin
                      / (electronsTransferred * PhysicalConstants.Faraday);
        EquilibriumPotential = standardPotential + rtNF * Math.Log(metalIonConcentration);

        _kinetics = new ButlerVolmerKinetics(
            exchangeCurrentDensity,
            anodicTransferCoefficient,
            cathodicTransferCoefficient,
            temperatureKelvin);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Returns the net Butler–Volmer current density at the given electrode potential.
    /// Positive values represent anodic (metal dissolution) current.
    /// </remarks>
    public double CurrentDensity(double potential)
    {
        double eta = potential - EquilibriumPotential;
        return _kinetics.CurrentDensity(eta);
    }
}
