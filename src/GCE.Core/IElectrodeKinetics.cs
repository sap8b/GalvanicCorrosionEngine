namespace GCE.Core;

/// <summary>
/// Defines electrode kinetics that compute current density from overpotential.
/// </summary>
/// <remarks>
/// Implementations model the relationship between electrode overpotential
/// (η = E − E_eq) and current density, including separate anodic and cathodic branches.
/// </remarks>
public interface IElectrodeKinetics
{
    /// <summary>Gets the exchange current density i₀ (A/m²).</summary>
    double ExchangeCurrentDensity { get; }

    /// <summary>Gets the anodic charge-transfer coefficient α_a (dimensionless, 0–1).</summary>
    double AnodicTransferCoefficient { get; }

    /// <summary>Gets the cathodic charge-transfer coefficient α_c (dimensionless, 0–1).</summary>
    double CathodicTransferCoefficient { get; }

    /// <summary>
    /// Computes the net current density (A/m²) for a given overpotential.
    /// Positive values represent net anodic (oxidation) current;
    /// negative values represent net cathodic (reduction) current.
    /// </summary>
    /// <param name="overpotential">Overpotential η = E − E_eq in V.</param>
    /// <returns>Net current density in A/m².</returns>
    double CurrentDensity(double overpotential);

    /// <summary>
    /// Computes the anodic partial current density (A/m²) for a given overpotential.
    /// Always returns a non-negative value representing the oxidation branch.
    /// </summary>
    /// <param name="overpotential">Overpotential η = E − E_eq in V.</param>
    /// <returns>Anodic partial current density in A/m².</returns>
    double AnodicCurrentDensity(double overpotential);

    /// <summary>
    /// Computes the cathodic partial current density (A/m²) for a given overpotential.
    /// Always returns a non-positive value representing the reduction branch.
    /// </summary>
    /// <param name="overpotential">Overpotential η = E − E_eq in V.</param>
    /// <returns>Cathodic partial current density in A/m².</returns>
    double CathodicCurrentDensity(double overpotential);
}
