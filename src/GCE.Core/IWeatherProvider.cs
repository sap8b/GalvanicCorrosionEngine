namespace GCE.Core;

/// <summary>
/// Provides weather observations at arbitrary points in simulation time.
/// </summary>
/// <remarks>
/// Implementations may generate synthetic data, interpolate from historical records,
/// or query an external weather service.
/// </remarks>
public interface IWeatherProvider
{
    /// <summary>
    /// Returns the weather observation applicable at the given simulation time.
    /// </summary>
    /// <param name="timeSeconds">Elapsed simulation time in seconds.</param>
    /// <returns>The <see cref="IWeatherObservation"/> at that instant.</returns>
    IWeatherObservation GetObservation(double timeSeconds);
}
