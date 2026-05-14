using GCE.Core;
using GCE.Electrochemistry;

namespace GCE.Simulation.Geometry;

/// <summary>
/// Geometry builder for a coaxial cylinder configuration: an inner cylinder of one
/// material enclosed within an outer coaxial cylinder of a dissimilar material,
/// with the annular gap filled by electrolyte.
/// </summary>
/// <remarks>
/// <para>
/// Electrode areas are computed as lateral cylindrical surfaces:
/// <list type="bullet">
///   <item>
///     <description>Inner area = 2π × <see cref="InnerRadius"/> × <see cref="Length"/>.</description>
///   </item>
///   <item>
///     <description>Outer area = 2π × <see cref="OuterRadius"/> × <see cref="Length"/>.</description>
///   </item>
/// </list>
/// </para>
/// <para>
/// The material with the lower standard potential is assigned as the anode; the
/// other becomes the cathode.
/// </para>
/// <para>
/// The 2-D mesh is a top-down (XY cross-section) view of the annular domain.
/// Nodes with radius ≤ <see cref="InnerRadius"/> are assigned to the inner-electrode
/// region; nodes with radius &gt; <see cref="InnerRadius"/> are assigned to the
/// outer-electrode region.
/// </para>
/// </remarks>
public sealed class CoaxialCylinderGeometry : IGeometryBuilder
{
    /// <summary>Gets the radius of the inner cylinder (m).</summary>
    public double InnerRadius { get; }

    /// <summary>Gets the radius of the outer cylinder (m).</summary>
    public double OuterRadius { get; }

    /// <summary>Gets the axial length of both cylinders (m).</summary>
    public double Length { get; }

    /// <summary>Gets the material of the inner cylinder.</summary>
    public IMaterial InnerMaterial { get; }

    /// <summary>Gets the material of the outer cylinder.</summary>
    public IMaterial OuterMaterial { get; }

    /// <inheritdoc/>
    public IMaterial AnodeMaterial { get; }

    /// <inheritdoc/>
    public IMaterial CathodeMaterial { get; }

    private readonly double _innerArea;
    private readonly double _outerArea;
    private readonly bool   _innerIsAnode;

    /// <param name="innerMaterial">Material of the inner cylinder.</param>
    /// <param name="outerMaterial">Material of the outer cylinder.</param>
    /// <param name="innerRadius">Inner cylinder radius (m); must be positive.</param>
    /// <param name="outerRadius">Outer cylinder radius (m); must exceed inner radius.</param>
    /// <param name="length">Axial length (m); must be positive.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when either material is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any dimension is invalid.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="outerRadius"/> ≤ <paramref name="innerRadius"/>, or when
    /// both materials have the same standard potential.
    /// </exception>
    public CoaxialCylinderGeometry(
        IMaterial innerMaterial,
        IMaterial outerMaterial,
        double    innerRadius,
        double    outerRadius,
        double    length)
    {
        ArgumentNullException.ThrowIfNull(innerMaterial);
        ArgumentNullException.ThrowIfNull(outerMaterial);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(innerRadius, 0.0, nameof(innerRadius));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(outerRadius, 0.0, nameof(outerRadius));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(length, 0.0, nameof(length));

        if (outerRadius <= innerRadius)
            throw new ArgumentException(
                "Outer radius must be strictly greater than the inner radius.", nameof(outerRadius));

        if (innerMaterial.StandardPotential == outerMaterial.StandardPotential)
            throw new ArgumentException(
                "The inner and outer materials must have different standard potentials to form a galvanic couple.");

        InnerMaterial = innerMaterial;
        OuterMaterial = outerMaterial;
        InnerRadius   = innerRadius;
        OuterRadius   = outerRadius;
        Length        = length;

        _innerArea    = 2.0 * Math.PI * innerRadius * length;
        _outerArea    = 2.0 * Math.PI * outerRadius * length;

        _innerIsAnode  = innerMaterial.StandardPotential < outerMaterial.StandardPotential;
        AnodeMaterial  = _innerIsAnode ? innerMaterial : outerMaterial;
        CathodeMaterial = _innerIsAnode ? outerMaterial : innerMaterial;
    }

    /// <inheritdoc/>
    public IGalvanicCell Build(IElectrolyte electrolyte)
    {
        ArgumentNullException.ThrowIfNull(electrolyte);

        double anodeArea   = _innerIsAnode ? _innerArea : _outerArea;
        double cathodeArea = _innerIsAnode ? _outerArea : _innerArea;

        var anode   = new Electrode(AnodeMaterial,   anodeArea);
        var cathode = new Electrode(CathodeMaterial, cathodeArea);
        return new GalvanicCell(anode, cathode, electrolyte);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Generates a top-down (XY cross-section) view spanning
    /// [−OuterRadius, OuterRadius] × [−OuterRadius, OuterRadius].
    /// Nodes with √(x² + y²) ≤ InnerRadius are assigned to the inner-electrode
    /// region; all others are assigned to the outer-electrode region.
    /// </remarks>
    public GeometryMesh BuildMesh(int nodesX = 20, int nodesY = 20)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(nodesX, 2, nameof(nodesX));
        ArgumentOutOfRangeException.ThrowIfLessThan(nodesY, 2, nameof(nodesY));

        double half  = OuterRadius;
        double xStep = 2.0 * OuterRadius / (nodesX - 1);
        double yStep = 2.0 * OuterRadius / (nodesY - 1);

        var xs = new double[nodesX];
        var ys = new double[nodesY];
        for (int i = 0; i < nodesX; i++) xs[i] = -half + i * xStep;
        for (int j = 0; j < nodesY; j++) ys[j] = -half + j * yStep;

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

        NodePhase innerRegion = _innerIsAnode ? NodePhase.Anode : NodePhase.Cathode;
        NodePhase outerRegion = _innerIsAnode ? NodePhase.Cathode : NodePhase.Anode;
        double r2 = InnerRadius * InnerRadius;
        var regions = new NodePhase[xs.Length, ys.Length];
        for (int i = 0; i < xs.Length; i++)
            for (int j = 0; j < ys.Length; j++)
                regions[i, j] = xs[i] * xs[i] + ys[j] * ys[j] <= r2 ? innerRegion : outerRegion;

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
