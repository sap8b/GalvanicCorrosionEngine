namespace GCE.Atmosphere;

/// <summary>
/// Common salt species relevant to atmospheric corrosion environments.
/// </summary>
public enum CommonSalt
{
    /// <summary>Sodium chloride (sea salt); dominant in marine and coastal environments.</summary>
    NaCl,

    /// <summary>Magnesium chloride; present in marine spray and de-icing applications.</summary>
    MgCl2,

    /// <summary>Calcium chloride; common in de-icing and desiccant applications.</summary>
    CaCl2,

    /// <summary>Ammonium sulfate; typical in inland atmospheres from agricultural and industrial sources.</summary>
    AmmoniumSulfate,
}
