namespace GCE.Core;

/// <summary>
/// Material phase associated with a node in a <see cref="GeometryMesh"/>.
/// </summary>
public enum NodePhase
{
    /// <summary>The node belongs to the anodic metal phase.</summary>
    Anode = 0,

    /// <summary>The node belongs to the cathodic metal phase.</summary>
    Cathode = 1,

    /// <summary>The node belongs to the electrolyte (gap) phase.</summary>
    Electrolyte = -1,

    /// <summary>The node belongs to a corrosion-product phase.</summary>
    CorrosionProduct = 2,
}

/// <summary>
/// A 2-D spatial mesh produced by an <see cref="IGeometryBuilder"/>.
/// </summary>
/// <remarks>
/// The mesh is defined by a rectilinear grid of node positions.  Each cell in the
/// <see cref="Regions"/> array identifies which material region contains that grid node:
/// <list type="bullet">
///   <item><description><see cref="NodePhase.Anode"/> — anode region</description></item>
///   <item><description><see cref="NodePhase.Cathode"/> — cathode region</description></item>
///   <item><description><see cref="NodePhase.Electrolyte"/> — electrolyte / gap region</description></item>
///   <item><description><see cref="NodePhase.CorrosionProduct"/> — corrosion-product region</description></item>
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
public sealed class GeometryMesh
{
    /// <summary>
    /// Creates a new <see cref="GeometryMesh"/>.
    /// </summary>
    /// <param name="XCoordinates">Node positions along the x-axis (m).</param>
    /// <param name="YCoordinates">Node positions along the y-axis (m).</param>
    /// <param name="Regions">Node phases on the mesh grid.</param>
    public GeometryMesh(double[] XCoordinates, double[] YCoordinates, NodePhase[,] Regions)
    {
        this.XCoordinates = XCoordinates;
        this.YCoordinates = YCoordinates;
        this.Regions = Regions;
    }

    /// <summary>Gets node positions along the x-axis (m).</summary>
    public double[] XCoordinates { get; }

    /// <summary>Gets node positions along the y-axis (m).</summary>
    public double[] YCoordinates { get; }

    /// <summary>Gets or sets node phases on the mesh grid.</summary>
    public NodePhase[,] Regions { get; set; }

    /// <summary>Gets the number of nodes in the x-direction.</summary>
    public int NodesX => XCoordinates.Length;

    /// <summary>Gets the number of nodes in the y-direction.</summary>
    public int NodesY => YCoordinates.Length;
}
