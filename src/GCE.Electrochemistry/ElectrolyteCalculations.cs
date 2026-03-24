namespace GCE.Electrochemistry;

/// <summary>
/// Internal helpers for electrolyte property calculations shared between
/// <see cref="BulkElectrolyte"/> and <see cref="ThinFilmElectrolyte"/>.
/// </summary>
internal static class ElectrolyteCalculations
{
    // ── Kohlrausch empirical constant (mol/L)^{-1/2}
    internal const double KohlrauschB = 0.2289;
    // ── Default limiting molar conductivity (S·m²/mol) — NaCl-like
    internal const double DefaultLambda = 76e-4; // 76 S·cm²/mol = 76e-4 S·m²/mol

    /// <summary>
    /// Computes ionic conductivity (S/m) using the Kohlrausch approximation from
    /// a list of registered species.
    /// </summary>
    internal static double ConductivityFromSpecies(IEnumerable<Species> species)
    {
        double cTotalL = species.Sum(s => s.Concentration) / 1000.0;
        double sqrtC   = Math.Sqrt(Math.Max(cTotalL, 0.0));
        double factor  = Math.Max(1.0 - KohlrauschB * sqrtC, 0.01);

        double kappa = 0.0;
        foreach (var s in species)
        {
            double cL = s.Concentration / 1000.0;
            kappa += Math.Abs(s.Charge) * DefaultLambda * cL * factor;
        }

        return kappa;
    }

    /// <summary>
    /// Computes ionic conductivity (S/m) from a single total concentration value (mol/m³).
    /// </summary>
    internal static double ConductivityFromTotal(double cMolPerM3)
    {
        double cL   = cMolPerM3 / 1000.0;
        double sqrt = Math.Sqrt(Math.Max(cL, 0.0));
        return DefaultLambda * cL * Math.Max(1.0 - KohlrauschB * sqrt, 0.01);
    }

    /// <summary>
    /// Finds the H⁺ species in the collection, or returns null if none is registered.
    /// </summary>
    internal static Species? FindHydrogenIon(IEnumerable<Species> species) =>
        species.FirstOrDefault(s => s.Charge == 1 &&
            (s.Name.Equals("H+", StringComparison.OrdinalIgnoreCase) ||
             s.Name.Equals("H", StringComparison.OrdinalIgnoreCase)));

    /// <summary>
    /// Computes pH from the H⁺ species concentration (mol/m³).
    /// Returns 7.0 when no H⁺ species is registered or its concentration is zero.
    /// </summary>
    internal static double ComputePh(IEnumerable<Species> species)
    {
        var hPlus = FindHydrogenIon(species);
        if (hPlus is null || hPlus.Concentration <= 0.0)
            return 7.0;

        // [H+] in mol/m³; divide by 1000 to get mol/L for standard pH definition
        return -Math.Log10(hPlus.Concentration / 1000.0);
    }
}
