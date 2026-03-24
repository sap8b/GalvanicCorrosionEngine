namespace GCE.Atmosphere;

/// <summary>
/// Reference deliquescence and efflorescence relative-humidity data for common salts,
/// with empirical temperature corrections.
/// </summary>
/// <remarks>
/// <para>
/// The <b>deliquescence relative humidity</b> (DRH) is the RH above which a dry salt
/// particle absorbs atmospheric moisture and dissolves into a liquid electrolyte film.
/// </para>
/// <para>
/// The <b>efflorescence relative humidity</b> (ERH) is the RH below which a saturated
/// salt solution crystallises.  ERH is always lower than DRH (hysteresis), so a wet
/// surface can remain liquid at humidities below the DRH threshold.
/// </para>
/// <para>
/// Reference values at 25 °C and linear temperature-correction slopes are taken from
/// published thermodynamic data (Tang &amp; Munkelwitz 1994; Greenspan 1977).
/// </para>
/// </remarks>
public static class DeliquescenceData
{
    // Reference DRH at 25 °C as a fraction (0–1)
    private static readonly Dictionary<CommonSalt, double> s_referenceDrh = new()
    {
        [CommonSalt.NaCl]            = 0.753,
        [CommonSalt.MgCl2]           = 0.330,
        [CommonSalt.CaCl2]           = 0.290,
        [CommonSalt.AmmoniumSulfate] = 0.799,
    };

    // Reference ERH at 25 °C as a fraction (0–1)
    private static readonly Dictionary<CommonSalt, double> s_referenceErh = new()
    {
        [CommonSalt.NaCl]            = 0.740,
        [CommonSalt.MgCl2]           = 0.277,
        [CommonSalt.CaCl2]           = 0.200,
        [CommonSalt.AmmoniumSulfate] = 0.370,
    };

    // Linear temperature-correction slope dDRH/dT (fraction per °C)
    // Positive value: DRH increases with temperature; negative: DRH decreases.
    private static readonly Dictionary<CommonSalt, double> s_drhTempSlope = new()
    {
        [CommonSalt.NaCl]            =  0.00017,
        [CommonSalt.MgCl2]           = -0.0010,
        [CommonSalt.CaCl2]           = -0.0007,
        [CommonSalt.AmmoniumSulfate] =  0.00065,
    };

    private const double ReferenceTemperatureCelsius = 25.0;

    /// <summary>
    /// Returns the deliquescence relative humidity for the specified salt
    /// at the given temperature.
    /// </summary>
    /// <param name="salt">The salt species.</param>
    /// <param name="temperatureCelsius">Surface temperature in °C (default 25 °C).</param>
    /// <returns>Deliquescence RH as a fraction (0–1).</returns>
    public static double GetDeliquescenceRH(CommonSalt salt, double temperatureCelsius = 25.0)
    {
        double drh25 = s_referenceDrh[salt];
        double slope = s_drhTempSlope[salt];
        return Math.Clamp(drh25 + slope * (temperatureCelsius - ReferenceTemperatureCelsius), 0.0, 1.0);
    }

    /// <summary>
    /// Returns the efflorescence relative humidity for the specified salt
    /// at the given temperature.
    /// </summary>
    /// <param name="salt">The salt species.</param>
    /// <param name="temperatureCelsius">Surface temperature in °C (default 25 °C).</param>
    /// <returns>Efflorescence RH as a fraction (0–1).</returns>
    public static double GetEfflorescenceRH(CommonSalt salt, double temperatureCelsius = 25.0)
    {
        double erh25 = s_referenceErh[salt];
        double slope = s_drhTempSlope[salt];
        return Math.Clamp(erh25 + slope * (temperatureCelsius - ReferenceTemperatureCelsius), 0.0, 1.0);
    }
}
