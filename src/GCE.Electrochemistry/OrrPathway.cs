namespace GCE.Electrochemistry;

/// <summary>
/// Specifies the electron-transfer pathway for the Oxygen Reduction Reaction (ORR).
/// </summary>
public enum OrrPathway
{
    /// <summary>
    /// 4-electron pathway: O₂ + 4H⁺ + 4e⁻ → 2H₂O (E° = +1.229 V vs. SHE).
    /// The predominant pathway on platinum and noble metals; produces water directly.
    /// </summary>
    FourElectron,

    /// <summary>
    /// 2-electron pathway: O₂ + 2H⁺ + 2e⁻ → H₂O₂ (E° = +0.695 V vs. SHE).
    /// Common on carbon and some transition metal surfaces; produces hydrogen peroxide.
    /// </summary>
    TwoElectron
}
