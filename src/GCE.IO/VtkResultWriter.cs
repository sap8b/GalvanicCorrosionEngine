using System.Globalization;
using System.Text;
using GCE.Core;
using GCE.Simulation;

namespace GCE.IO;

/// <summary>
/// Writes simulation results to VTK XML RectilinearGrid format (<c>.vtr</c>).
/// </summary>
/// <remarks>
/// <para>
/// When a <see cref="GeometryMesh"/> is supplied the writer produces a 2-D
/// rectilinear grid whose node coordinates come from the mesh and whose point-data
/// arrays carry:
/// <list type="bullet">
///   <item><description><c>RegionId</c> — the region identifier from <see cref="GeometryMesh.Regions"/> (0 = anode, 1 = cathode, −1 = electrolyte).</description></item>
/// </list>
/// Time-series field data (<c>Time_s</c>, <c>MixedPotential_V</c>,
/// <c>CorrosionRate_mmPerYear</c>) is always written as VTK field-data arrays so
/// that a single file captures the complete simulation output.
/// </para>
/// <para>
/// When no mesh is provided a degenerate 1×1×1 grid is used as a placeholder, and
/// only the field-data arrays are written.
/// </para>
/// <para>
/// The output is a valid <em>VTK XML RectilinearGrid</em> document that can be
/// opened directly in ParaView or VisIt.
/// </para>
/// </remarks>
public sealed class VtkResultWriter : IResultWriter
{
    private readonly GeometryMesh? _mesh;

    /// <summary>
    /// Initialises a <see cref="VtkResultWriter"/> without spatial geometry.
    /// Only time-series field data will be written.
    /// </summary>
    public VtkResultWriter() { }

    /// <summary>
    /// Initialises a <see cref="VtkResultWriter"/> with the supplied
    /// <paramref name="mesh"/>. Both spatial point data and time-series field
    /// data will be written.
    /// </summary>
    /// <param name="mesh">The geometry mesh to embed in the VTK output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="mesh"/> is <see langword="null"/>.
    /// </exception>
    public VtkResultWriter(GeometryMesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        _mesh = mesh;
    }

    /// <inheritdoc />
    public void Write(SimulationResult result, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        // Determine grid extents.
        int nx = _mesh?.NodesX ?? 1;
        int ny = _mesh?.NodesY ?? 1;
        writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        writer.WriteLine("<VTKFile type=\"RectilinearGrid\" version=\"0.1\" byte_order=\"LittleEndian\">");
        writer.WriteLine($"  <RectilinearGrid WholeExtent=\"0 {nx - 1} 0 {ny - 1} 0 0\">");

        // ── FieldData — time-series arrays ───────────────────────────────────
        writer.WriteLine("    <FieldData>");
        WriteDataArray(writer, "Time_s",                  result.TimePoints,      indent: 6);
        WriteDataArray(writer, "MixedPotential_V",        result.MixedPotentials, indent: 6);
        WriteDataArray(writer, "CorrosionRate_mmPerYear", result.CorrosionRates,  indent: 6);
        writer.WriteLine("    </FieldData>");

        // ── Piece ─────────────────────────────────────────────────────────────
        writer.WriteLine($"    <Piece Extent=\"0 {nx - 1} 0 {ny - 1} 0 0\">");

        // ── Coordinates ───────────────────────────────────────────────────────
        writer.WriteLine("      <Coordinates>");
        WriteDataArray(writer, "x_m", _mesh?.XCoordinates ?? [0.0], indent: 8);
        WriteDataArray(writer, "y_m", _mesh?.YCoordinates ?? [0.0], indent: 8);
        WriteDataArray(writer, "z_m", [0.0],                        indent: 8);
        writer.WriteLine("      </Coordinates>");

        // ── PointData — spatial arrays ────────────────────────────────────────
        if (_mesh != null)
        {
            writer.WriteLine("      <PointData>");
            WriteRegionArray(writer, _mesh, indent: 8);
            writer.WriteLine("      </PointData>");
        }

        writer.WriteLine("    </Piece>");
        writer.WriteLine("  </RectilinearGrid>");
        writer.WriteLine("</VTKFile>");
    }

    /// <inheritdoc />
    public void WriteToFile(SimulationResult result, string filePath)
    {
        using var writer = new StreamWriter(filePath, append: false, encoding: Encoding.UTF8);
        Write(result, writer);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void WriteDataArray(
        TextWriter writer,
        string name,
        IReadOnlyList<double> values,
        int indent)
    {
        string pad = new(' ', indent);
        writer.WriteLine(
            $"{pad}<DataArray type=\"Float64\" Name=\"{name}\" NumberOfTuples=\"{values.Count}\" format=\"ascii\">");
        writer.Write(new string(' ', indent + 2));
        for (int i = 0; i < values.Count; i++)
        {
            if (i > 0) writer.Write(' ');
            writer.Write(values[i].ToString("G6", CultureInfo.InvariantCulture));
        }
        writer.WriteLine();
        writer.WriteLine($"{pad}</DataArray>");
    }

    private static void WriteDataArray(
        TextWriter writer,
        string name,
        double[] values,
        int indent)
    {
        WriteDataArray(writer, name, (IReadOnlyList<double>)values, indent);
    }

    private static void WriteRegionArray(TextWriter writer, GeometryMesh mesh, int indent)
    {
        int totalNodes = mesh.NodesX * mesh.NodesY;
        string pad = new(' ', indent);
        writer.WriteLine(
            $"{pad}<DataArray type=\"Int32\" Name=\"RegionId\" NumberOfTuples=\"{totalNodes}\" format=\"ascii\">");
        writer.Write(new string(' ', indent + 2));
        bool first = true;
        // VTK rectilinear grid node ordering: x varies fastest, then y.
        for (int j = 0; j < mesh.NodesY; j++)
        {
            for (int i = 0; i < mesh.NodesX; i++)
            {
                if (!first) writer.Write(' ');
                writer.Write(mesh.Regions[i, j]);
                first = false;
            }
        }
        writer.WriteLine();
        writer.WriteLine($"{pad}</DataArray>");
    }
}
