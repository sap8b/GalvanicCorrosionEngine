using GCE.Simulation;

namespace GCE.IO;

/// <summary>
/// Exports a <see cref="SimulationResult"/> to CSV format.
/// </summary>
public sealed class CsvExporter
{
    /// <summary>
    /// Writes the simulation result to <paramref name="writer"/> as CSV.
    /// </summary>
    /// <param name="result">The simulation result to export.</param>
    /// <param name="writer">The text writer to write to.</param>
    public void Export(SimulationResult result, TextWriter writer)
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

    /// <summary>
    /// Writes the simulation result to the file at <paramref name="filePath"/>.
    /// </summary>
    public void ExportToFile(SimulationResult result, string filePath)
    {
        using var writer = new StreamWriter(filePath, append: false);
        Export(result, writer);
    }
}
