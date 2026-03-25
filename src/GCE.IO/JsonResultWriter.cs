using System.Text.Json;
using System.Text.Json.Serialization;
using GCE.Simulation;

namespace GCE.IO;

/// <summary>
/// Writes a <see cref="SimulationResult"/> to JSON format.
/// </summary>
/// <remarks>
/// The output is a single JSON object with four top-level arrays:
/// <c>timePoints_s</c>, <c>mixedPotentials_V</c>, <c>corrosionRates_mmPerYear</c>,
/// and <c>convergenceHistory</c>.  When <see cref="Indented"/> is <see langword="true"/>
/// (the default) the output is pretty-printed.
/// </remarks>
public sealed class JsonResultWriter : IResultWriter
{
    private static readonly JsonSerializerOptions _compactOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly JsonSerializerOptions _indentedOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Gets or sets a value indicating whether the JSON output is indented
    /// (pretty-printed).  Defaults to <see langword="true"/>.
    /// </summary>
    public bool Indented { get; init; } = true;

    /// <inheritdoc />
    public void Write(SimulationResult result, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        var options = Indented ? _indentedOptions : _compactOptions;
        var dto = new ResultDto(result);
        string json = JsonSerializer.Serialize(dto, options);
        writer.Write(json);
    }

    /// <inheritdoc />
    public void WriteToFile(SimulationResult result, string filePath)
    {
        using var writer = new StreamWriter(filePath, append: false);
        Write(result, writer);
    }

    // ── Private DTO ───────────────────────────────────────────────────────────

    private sealed class ResultDto
    {
        public ResultDto(SimulationResult r)
        {
            TimePoints_s             = [.. r.TimePoints];
            MixedPotentials_V        = [.. r.MixedPotentials];
            CorrosionRates_mmPerYear = [.. r.CorrosionRates];
            ConvergenceHistory       = r.ConvergenceHistory.Count > 0
                ? [.. r.ConvergenceHistory.Select(c => new ConvergenceDto(c))]
                : null;
        }

        [JsonPropertyName("timePoints_s")]
        public double[] TimePoints_s { get; }

        [JsonPropertyName("mixedPotentials_V")]
        public double[] MixedPotentials_V { get; }

        [JsonPropertyName("corrosionRates_mmPerYear")]
        public double[] CorrosionRates_mmPerYear { get; }

        [JsonPropertyName("convergenceHistory")]
        public ConvergenceDto[]? ConvergenceHistory { get; }
    }

    private sealed class ConvergenceDto
    {
        public ConvergenceDto(ConvergenceInfo c)
        {
            Iteration = c.Iteration;
            Residual  = c.Residual;
            Change    = c.Change;
            Converged = c.Converged;
        }

        [JsonPropertyName("iteration")]
        public int Iteration { get; }

        [JsonPropertyName("residual")]
        public double Residual { get; }

        [JsonPropertyName("change")]
        public double Change { get; }

        [JsonPropertyName("converged")]
        public bool Converged { get; }
    }
}
