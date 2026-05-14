using GCE.Core;
using GCE.Electrochemistry;

namespace GCE.Simulation.Geometry;

/// <summary>
/// Geometry builder for an overlap-joint (lap-joint) configuration: two flat metal
/// sheets of dissimilar materials that overlap along a rectangular interface zone.
/// </summary>
/// <remarks>
/// <para>
/// The overlap zone is the electrochemically active interface where galvanic
/// corrosion occurs.  Both sheets contribute their overlap area to the galvanic
/// couple.
/// </para>
/// <para>
/// Electrode areas:
/// <list type="bullet">
///   <item>
///     <description>Anode area = <see cref="OverlapWidth"/> × <see cref="OverlapLength"/>.</description>
///   </item>
///   <item>
///     <description>Cathode area = <see cref="OverlapWidth"/> × <see cref="OverlapLength"/>.</description>
///   </item>
/// </list>
/// Both areas are equal; the anode/cathode assignment is governed by the standard
/// potentials of the materials.
/// </para>
/// <para>
/// The 2-D mesh represents the overlap zone as seen from above.  The left half
/// (x ≤ OverlapWidth/2) is assigned to one sheet and the right half to the other.
/// </para>
/// </remarks>
public sealed class OverlapJointGeometry : IGeometryBuilder
{
    /// <summary>Gets the width of the overlap zone in the x-direction (m).</summary>
    public double OverlapWidth { get; }

    /// <summary>Gets the length of the overlap zone in the y-direction (m).</summary>
    public double OverlapLength { get; }

    /// <summary>Gets the material of the first (left) sheet.</summary>
    public IMaterial Material1 { get; }

    /// <summary>Gets the material of the second (right) sheet.</summary>
    public IMaterial Material2 { get; }

    /// <inheritdoc/>
    public IMaterial AnodeMaterial { get; }

    /// <inheritdoc/>
    public IMaterial CathodeMaterial { get; }

    private readonly bool   _mat1IsAnode;
    private readonly double _overlapArea;

    /// <param name="material1">Material of the first sheet.</param>
    /// <param name="material2">Material of the second sheet.</param>
    /// <param name="overlapWidth">Width of the overlap zone (m); must be positive.</param>
    /// <param name="overlapLength">Length of the overlap zone (m); must be positive.</param>
    /// <exception cref="ArgumentNullException">Thrown when either material is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any dimension is not positive.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when both materials have the same standard potential (no galvanic couple).
    /// </exception>
    public OverlapJointGeometry(
        IMaterial material1,
        IMaterial material2,
        double    overlapWidth,
        double    overlapLength)
    {
        ArgumentNullException.ThrowIfNull(material1);
        ArgumentNullException.ThrowIfNull(material2);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(overlapWidth,  0.0, nameof(overlapWidth));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(overlapLength, 0.0, nameof(overlapLength));

        if (material1.StandardPotential == material2.StandardPotential)
            throw new ArgumentException(
                "The two sheet materials must have different standard potentials to form a galvanic couple.");

        Material1 = material1;
        Material2 = material2;
        OverlapWidth  = overlapWidth;
        OverlapLength = overlapLength;
        _overlapArea  = overlapWidth * overlapLength;

        _mat1IsAnode   = material1.StandardPotential < material2.StandardPotential;
        AnodeMaterial  = _mat1IsAnode ? material1 : material2;
        CathodeMaterial = _mat1IsAnode ? material2 : material1;
    }

    /// <inheritdoc/>
    public IGalvanicCell Build(IElectrolyte electrolyte)
    {
        ArgumentNullException.ThrowIfNull(electrolyte);

        var anode   = new Electrode(AnodeMaterial,   _overlapArea);
        var cathode = new Electrode(CathodeMaterial, _overlapArea);
        return new GalvanicCell(anode, cathode, electrolyte);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Generates a top-down (XY) view of the overlap zone.
    /// Nodes in the left half (x ≤ OverlapWidth/2) are assigned to the sheet-1 region;
    /// nodes in the right half (x &gt; OverlapWidth/2) are assigned to the sheet-2 region.
    /// </remarks>
    public GeometryMesh BuildMesh(int nodesX = 20, int nodesY = 20)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(nodesX, 2, nameof(nodesX));
        ArgumentOutOfRangeException.ThrowIfLessThan(nodesY, 2, nameof(nodesY));

        NodePhase region1 = _mat1IsAnode ? NodePhase.Anode : NodePhase.Cathode;
        NodePhase region2 = _mat1IsAnode ? NodePhase.Cathode : NodePhase.Anode;

        double xStep = OverlapWidth  / (nodesX - 1);
        double yStep = OverlapLength / (nodesY - 1);

        var xs = new double[nodesX];
        var ys = new double[nodesY];
        for (int i = 0; i < nodesX; i++) xs[i] = i * xStep;
        for (int j = 0; j < nodesY; j++) ys[j] = j * yStep;

        return BuildMesh(xs, ys);
    }

    /// <inheritdoc/>
    public GeometryMesh BuildMesh(double[] xCoordinates, double[] yCoordinates)
    {
        ArgumentNullException.ThrowIfNull(xCoordinates);
        ArgumentNullException.ThrowIfNull(yCoordinates);
        ValidateCoordinates(xCoordinates, nameof(xCoordinates));
        ValidateCoordinates(yCoordinates, nameof(yCoordinates));

        var xs = (double[])xCoordinates.Clone();
        var ys = (double[])yCoordinates.Clone();

        NodePhase region1 = _mat1IsAnode ? NodePhase.Anode : NodePhase.Cathode;
        NodePhase region2 = _mat1IsAnode ? NodePhase.Cathode : NodePhase.Anode;
        double mid = OverlapWidth / 2.0;
        var regions = new NodePhase[xs.Length, ys.Length];
        for (int i = 0; i < xs.Length; i++)
        {
            NodePhase region = xs[i] <= mid ? region1 : region2;
            for (int j = 0; j < ys.Length; j++)
                regions[i, j] = region;
        }

        return new GeometryMesh(xs, ys, regions);
    }

    private static void ValidateCoordinates(double[] coordinates, string paramName)
    {
        if (coordinates.Length < 2)
            throw new ArgumentException("Coordinate array must contain at least two points.", paramName);

        for (int i = 1; i < coordinates.Length; i++)
        {
            if (coordinates[i] <= coordinates[i - 1])
                throw new ArgumentException("Coordinate array must be strictly increasing.", paramName);
        }
    }
}
