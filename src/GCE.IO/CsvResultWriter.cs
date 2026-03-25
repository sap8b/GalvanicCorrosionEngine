using GCE.Simulation;

namespace GCE.IO;

/// <summary>
/// Writes a <see cref="SimulationResult"/> to CSV format.
/// </summary>
/// <remarks>
/// Output columns: <c>Time_s</c>, <c>MixedPotential_V</c>, <c>CorrosionRate_mmPerYear</c>.
/// </remarks>
public sealed class CsvResultWriter : IResultWriter
{
    /// <inheritdoc />
    public void Write(SimulationResult result, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteLine("Time_s,MixedPotential_V,CorrosionRate_mmPerYear");

        for (int i = 0; i < result.TimePoints.Count; i++)
        {
            writer.WriteLine(
                $"{result.TimePoints[i]:G6}," +
                $"{result.MixedPotentials[i]:G6}," +
                $"{result.CorrosionRates[i]:G6}");
        }
    }

    /// <inheritdoc />
    public void WriteToFile(SimulationResult result, string filePath)
    {
        using var writer = new StreamWriter(filePath, append: false);
        Write(result, writer);
    }
}
