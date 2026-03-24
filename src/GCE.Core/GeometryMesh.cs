namespace GCE.Core;

/// <summary>
/// A 2-D spatial mesh produced by an <see cref="IGeometryBuilder"/>.
/// </summary>
/// <remarks>
/// The mesh is defined by a rectilinear grid of node positions.  Each cell in the
/// <see cref="Regions"/> array identifies which material region contains that grid node:
/// <list type="bullet">
///   <item><description><c>0</c> — anode region</description></item>
///   <item><description><c>1</c> — cathode region</description></item>
///   <item><description><c>-1</c> — electrolyte / gap region</description></item>
/// </list>
/// </remarks>
/// <param name="XCoordinates">
/// Node positions along the x-axis (m).  Length equals <see cref="NodesX"/>.
/// </param>
/// <param name="YCoordinates">
/// Node positions along the y-axis (m).  Length equals <see cref="NodesY"/>.
/// </param>
/// <param name="Regions">
/// A <c>NodesX × NodesY</c> array of region identifiers (see remarks).
/// </param>
public sealed record GeometryMesh(double[] XCoordinates, double[] YCoordinates, int[,] Regions)
{
    /// <summary>Gets the number of nodes in the x-direction.</summary>
    public int NodesX => XCoordinates.Length;

    /// <summary>Gets the number of nodes in the y-direction.</summary>
    public int NodesY => YCoordinates.Length;
}
