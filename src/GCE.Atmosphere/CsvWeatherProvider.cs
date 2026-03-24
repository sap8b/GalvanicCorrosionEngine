using GCE.Core;

namespace GCE.Atmosphere;

/// <summary>
/// Loads weather observations from a CSV file and serves them by linear
/// interpolation between recorded time points.
/// </summary>
/// <remarks>
/// <para>Expected CSV format (header row required):</para>
/// <code>
/// TimeSeconds,TemperatureCelsius,RelativeHumidity,ChlorideConcentration,Precipitation,WindSpeed
/// 0,20.0,0.75,0.10,0.0,2.0
/// 3600,21.5,0.72,0.10,0.0,2.3
/// </code>
/// <para>
/// The <c>Precipitation</c> and <c>WindSpeed</c> columns are optional; they default
/// to 0 if absent.  Time values must be non-negative and in ascending order.
/// </para>
/// <para>
/// For times before the first row the first row's values are used.
/// For times after the last row the last row's values are used (constant extrapolation).
/// </para>
/// </remarks>
public sealed class CsvWeatherProvider : IWeatherProvider
{
    private readonly IReadOnlyList<(double Time, WeatherObservation Observation)> _records;

    /// <summary>
    /// Creates a <see cref="CsvWeatherProvider"/> by reading records from the given reader.
    /// </summary>
    /// <param name="reader">A <see cref="TextReader"/> positioned at the beginning of CSV data.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reader"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the CSV contains no data rows.</exception>
    public CsvWeatherProvider(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        _records = ParseRecords(reader);

        if (_records.Count == 0)
            throw new InvalidOperationException("CSV weather data contains no data rows.");
    }

    /// <summary>
    /// Creates a <see cref="CsvWeatherProvider"/> by reading records from a file.
    /// </summary>
    /// <param name="path">Path to the CSV file.</param>
    public CsvWeatherProvider(string path)
        : this(new StringReader(File.ReadAllText(path)))
    {
    }

    /// <inheritdoc/>
    public IWeatherObservation GetObservation(double timeSeconds)
    {
        if (timeSeconds <= _records[0].Time)
            return _records[0].Observation;

        if (timeSeconds >= _records[^1].Time)
            return _records[^1].Observation;

        // Binary search for the surrounding interval
        int lo = 0, hi = _records.Count - 1;
        while (hi - lo > 1)
        {
            int mid = (lo + hi) / 2;
            if (_records[mid].Time <= timeSeconds)
                lo = mid;
            else
                hi = mid;
        }

        var (t0, obs0) = _records[lo];
        var (t1, obs1) = _records[hi];
        double fraction = (timeSeconds - t0) / (t1 - t0);

        return Lerp(obs0, obs1, fraction);
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static IReadOnlyList<(double, WeatherObservation)> ParseRecords(TextReader reader)
    {
        var records = new List<(double, WeatherObservation)>();
        bool headerSkipped = false;

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            if (!headerSkipped)
            {
                headerSkipped = true;
                continue; // skip header row
            }

            var parts = line.Split(',');
            if (parts.Length < 4)
                throw new FormatException(
                    $"CSV row has fewer than 4 columns: '{line}'");

            double time = double.Parse(parts[0].Trim());
            double temp = double.Parse(parts[1].Trim());
            double rh = double.Parse(parts[2].Trim());
            double cl = double.Parse(parts[3].Trim());
            double precip = parts.Length > 4 ? double.Parse(parts[4].Trim()) : 0.0;
            double wind = parts.Length > 5 ? double.Parse(parts[5].Trim()) : 0.0;

            records.Add((time, new WeatherObservation(temp, rh, cl, precip, wind)));
        }

        return records.AsReadOnly();
    }

    private static WeatherObservation Lerp(WeatherObservation a, WeatherObservation b, double t) =>
        new(
            TemperatureCelsius:    a.TemperatureCelsius    + t * (b.TemperatureCelsius    - a.TemperatureCelsius),
            RelativeHumidity:      a.RelativeHumidity      + t * (b.RelativeHumidity      - a.RelativeHumidity),
            ChlorideConcentration: a.ChlorideConcentration + t * (b.ChlorideConcentration - a.ChlorideConcentration),
            Precipitation:         a.Precipitation         + t * (b.Precipitation         - a.Precipitation),
            WindSpeed:             a.WindSpeed             + t * (b.WindSpeed             - a.WindSpeed));
}
