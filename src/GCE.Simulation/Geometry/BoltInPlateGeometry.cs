using GCE.Core;
using GCE.Electrochemistry;

namespace GCE.Simulation.Geometry;

/// <summary>
/// Geometry builder for a bolt-in-plate configuration: a cylindrical fastener of one
/// material inserted through a flat plate of a dissimilar metal.
/// </summary>
/// <remarks>
/// <para>
/// The cross-sectional geometry (top-down view) consists of a circular bolt hole of
/// radius <see cref="BoltRadius"/> centred within a square plate of side
/// <see cref="PlateWidth"/>.
/// </para>
/// <para>
/// Electrode areas are computed as follows:
/// <list type="bullet">
///   <item>
///     <description>
///       Bolt lateral area = 2π × <see cref="BoltRadius"/> × <see cref="PlateThickness"/>
///       (cylindrical surface of the bolt through the plate).
///     </description>
///   </item>
///   <item>
///     <description>
///       Plate area = <see cref="PlateWidth"/>² − π × <see cref="BoltRadius"/>²
///       (square plate face minus the circular bolt hole).
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// The material with the lower standard potential is automatically assigned as the
/// anode; the other becomes the cathode.
/// </para>
/// </remarks>
public sealed class BoltInPlateGeometry : IGeometryBuilder
{
    /// <summary>Gets the radius of the bolt (m).</summary>
    public double BoltRadius { get; }

    /// <summary>Gets the thickness of the plate (and insertion depth of the bolt) (m).</summary>
    public double PlateThickness { get; }

    /// <summary>Gets the total side length of the square plate (m).</summary>
    public double PlateWidth { get; }

    /// <summary>Gets the material of the bolt.</summary>
    public IMaterial BoltMaterial { get; }

    /// <summary>Gets the material of the plate.</summary>
    public IMaterial PlateMaterial { get; }

    /// <inheritdoc/>
    public IMaterial AnodeMaterial { get; }

    /// <inheritdoc/>
    public IMaterial CathodeMaterial { get; }

    private readonly double _boltArea;
    private readonly double _plateArea;
    private readonly bool _boltIsAnode;

    /// <param name="boltMaterial">Material of the bolt.</param>
    /// <param name="plateMaterial">Material of the plate.</param>
    /// <param name="boltRadius">Radius of the bolt (m); must be positive.</param>
    /// <param name="plateThickness">Thickness of the plate (m); must be positive.</param>
    /// <param name="plateWidth">Side length of the square plate (m); must exceed bolt diameter.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="boltMaterial"/> or <paramref name="plateMaterial"/> is
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="boltRadius"/>, <paramref name="plateThickness"/>, or
    /// <paramref name="plateWidth"/> is not positive.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the bolt diameter equals or exceeds <paramref name="plateWidth"/>, or when
    /// both materials share the same standard potential (no galvanic couple).
    /// </exception>
    public BoltInPlateGeometry(
        IMaterial boltMaterial,
        IMaterial plateMaterial,
        double boltRadius,
        double plateThickness,
        double plateWidth)
    {
        ArgumentNullException.ThrowIfNull(boltMaterial);
        ArgumentNullException.ThrowIfNull(plateMaterial);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(boltRadius, 0.0, nameof(boltRadius));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(plateThickness, 0.0, nameof(plateThickness));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(plateWidth, 0.0, nameof(plateWidth));

        if (2.0 * boltRadius >= plateWidth)
            throw new ArgumentException(
                "Bolt diameter must be strictly less than the plate width.", nameof(boltRadius));

        if (boltMaterial.StandardPotential == plateMaterial.StandardPotential)
            throw new ArgumentException(
                "The bolt and plate materials must have different standard potentials " +
                "to form a galvanic couple.");

        BoltMaterial = boltMaterial;
        PlateMaterial = plateMaterial;
        BoltRadius = boltRadius;
        PlateThickness = plateThickness;
        PlateWidth = plateWidth;

        _boltArea = 2.0 * Math.PI * boltRadius * plateThickness;
        _plateArea = plateWidth * plateWidth - Math.PI * boltRadius * boltRadius;

        _boltIsAnode = boltMaterial.StandardPotential < plateMaterial.StandardPotential;
        AnodeMaterial = _boltIsAnode ? boltMaterial : plateMaterial;
        CathodeMaterial = _boltIsAnode ? plateMaterial : boltMaterial;
    }

    /// <inheritdoc/>
    public IGalvanicCell Build(IElectrolyte electrolyte)
    {
        ArgumentNullException.ThrowIfNull(electrolyte);

        double anodeArea = _boltIsAnode ? _boltArea : _plateArea;
        double cathodeArea = _boltIsAnode ? _plateArea : _boltArea;

        var anode = new Electrode(AnodeMaterial, anodeArea);
        var cathode = new Electrode(CathodeMaterial, cathodeArea);
        return new GalvanicCell(anode, cathode, electrolyte);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Generates a top-down (XY cross-section) view of the plate.  Nodes within
    /// <see cref="BoltRadius"/> of the origin are assigned to the bolt region; all
    /// other nodes are assigned to the plate region.
    /// </remarks>
    public GeometryMesh BuildMesh(int nodesX = 20, int nodesY = 20)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(nodesX, 2, nameof(nodesX));
        ArgumentOutOfRangeException.ThrowIfLessThan(nodesY, 2, nameof(nodesY));

        int boltRegion = _boltIsAnode ? 0 : 1;
        int plateRegion = _boltIsAnode ? 1 : 0;

        double half = PlateWidth / 2.0;
        double xStep = PlateWidth / (nodesX - 1);
        double yStep = PlateWidth / (nodesY - 1);

        var xs = new double[nodesX];
        var ys = new double[nodesY];

        for (int i = 0; i < nodesX; i++)
            xs[i] = -half + i * xStep;
        for (int j = 0; j < nodesY; j++)
            ys[j] = -half + j * yStep;

        var regions = new int[nodesX, nodesY];
        double r2 = BoltRadius * BoltRadius;
        for (int i = 0; i < nodesX; i++)
        {
            for (int j = 0; j < nodesY; j++)
            {
                double dist2 = xs[i] * xs[i] + ys[j] * ys[j];
                regions[i, j] = dist2 <= r2 ? boltRegion : plateRegion;
            }
        }

        return new GeometryMesh(xs, ys, regions);
    }
}
