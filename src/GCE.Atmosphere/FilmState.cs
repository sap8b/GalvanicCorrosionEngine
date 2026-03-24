namespace GCE.Atmosphere;

/// <summary>
/// Represents the instantaneous state of an electrolyte film or droplet field
/// on a corroding metal surface.
/// </summary>
/// <param name="ThicknessMeters">
/// Equivalent film thickness in metres (volume of liquid per unit surface area).
/// Zero indicates a fully dry surface.
/// </param>
/// <param name="SaltConcentrationMolPerL">
/// Dissolved salt concentration in mol/L within the electrolyte film.
/// </param>
/// <param name="SurfaceTemperatureCelsius">
/// Metal surface temperature in °C.  May exceed the ambient air temperature
/// when solar insolation is present.
/// </param>
/// <param name="IsDeliquesced">
/// <see langword="true"/> when the salt is dissolved and the film is liquid;
/// <see langword="false"/> when the salt is crystalline and the surface is dry.
/// </param>
/// <param name="CoverageFraction">
/// Fraction of the total surface area covered by liquid electrolyte (0–1).
/// A value less than 1 indicates a droplet regime where discrete droplets
/// have not yet coalesced into a continuous film.
/// </param>
public sealed record FilmState(
    double ThicknessMeters,
    double SaltConcentrationMolPerL,
    double SurfaceTemperatureCelsius,
    bool IsDeliquesced,
    double CoverageFraction = 1.0);
