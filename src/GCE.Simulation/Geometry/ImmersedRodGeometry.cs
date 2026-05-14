using GCE.Core;
using GCE.Electrochemistry;

namespace GCE.Simulation.Geometry;

/// <summary>
/// Geometry builder for an immersed-rod configuration: a cylindrical rod of one
/// material immersed vertically in a rectangular bath of a dissimilar metal.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="BoltInPlateGeometry"/>, the rod is fully immersed in the bath
/// (no plate hole required). The bath is modelled as a flat plate with the rod's
/// cross-sectional footprint subtracted from it.
/// </para>
/// <para>
/// Electrode areas:
/// <list type="bullet">
///   <item>
///     <description>Rod lateral area = 2π × <see cref="RodRadius"/> × <see cref="RodLength"/>.</description>
///   </item>
///   <item>
///     <description>Bath area = <see cref="BathWidth"/> × <see cref="BathHeight"/> − π × <see cref="RodRadius"/>².</description>
///   </item>
/// </list>
/// </para>
/// <para>
/// The material with the lower standard potential is assigned as the anode.
/// </para>
/// </remarks>
public sealed class ImmersedRodGeometry : IGeometryBuilder
{
    /// <summary>Gets the radius of the rod (m).</summary>
    public double RodRadius { get; }

    /// <summary>Gets the length of the immersed rod (m).</summary>
    public double RodLength { get; }

    /// <summary>Gets the width of the rectangular bath (m).</summary>
    public double BathWidth { get; }

    /// <summary>Gets the height of the rectangular bath (m).</summary>
    public double BathHeight { get; }

    /// <summary>Gets the material of the rod.</summary>
    public IMaterial RodMaterial { get; }

    /// <summary>Gets the material of the bath.</summary>
    public IMaterial BathMaterial { get; }

    /// <inheritdoc/>
    public IMaterial AnodeMaterial { get; }

    /// <inheritdoc/>
    public IMaterial CathodeMaterial { get; }

    private readonly double _rodArea;
    private readonly double _bathArea;
    private readonly bool   _rodIsAnode;

    /// <param name="rodMaterial">Material of the cylindrical rod.</param>
    /// <param name="bathMaterial">Material of the bath plate.</param>
    /// <param name="rodRadius">Rod radius (m); must be positive.</param>
    /// <param name="rodLength">Immersed rod length (m); must be positive.</param>
    /// <param name="bathWidth">Bath width (m); must be positive.</param>
    /// <param name="bathHeight">Bath height (m); must be positive.</param>
    /// <exception cref="ArgumentNullException">Thrown when either material is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any dimension is not positive.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when both materials have the same standard potential, or when the rod footprint
    /// area (π·r²) is not strictly less than the bath area (W·H).
    /// </exception>
    public ImmersedRodGeometry(
        IMaterial rodMaterial,
        IMaterial bathMaterial,
        double    rodRadius,
        double    rodLength,
        double    bathWidth,
        double    bathHeight)
    {
        ArgumentNullException.ThrowIfNull(rodMaterial);
        ArgumentNullException.ThrowIfNull(bathMaterial);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(rodRadius, 0.0, nameof(rodRadius));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(rodLength, 0.0, nameof(rodLength));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(bathWidth, 0.0, nameof(bathWidth));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(bathHeight, 0.0, nameof(bathHeight));

        double bathArea = bathWidth * bathHeight - Math.PI * rodRadius * rodRadius;
        if (bathArea <= 0.0)
            throw new ArgumentException(
                "The rod cross-sectional area must be strictly less than the bath area.",
                nameof(rodRadius));

        if (rodMaterial.StandardPotential == bathMaterial.StandardPotential)
            throw new ArgumentException(
                "The rod and bath materials must have different standard potentials to form a galvanic couple.");

        RodMaterial  = rodMaterial;
        BathMaterial = bathMaterial;
        RodRadius    = rodRadius;
        RodLength    = rodLength;
        BathWidth    = bathWidth;
        BathHeight   = bathHeight;

        _rodArea  = 2.0 * Math.PI * rodRadius * rodLength;
        _bathArea = bathArea;

        _rodIsAnode    = rodMaterial.StandardPotential < bathMaterial.StandardPotential;
        AnodeMaterial  = _rodIsAnode ? rodMaterial  : bathMaterial;
        CathodeMaterial = _rodIsAnode ? bathMaterial : rodMaterial;
    }

    /// <inheritdoc/>
    public IGalvanicCell Build(IElectrolyte electrolyte)
    {
        ArgumentNullException.ThrowIfNull(electrolyte);

        double anodeArea   = _rodIsAnode ? _rodArea  : _bathArea;
        double cathodeArea = _rodIsAnode ? _bathArea : _rodArea;

        var anode   = new Electrode(AnodeMaterial,   anodeArea);
        var cathode = new Electrode(CathodeMaterial, cathodeArea);
        return new GalvanicCell(anode, cathode, electrolyte);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Generates a top-down (XY cross-section) view spanning
    /// [−BathWidth/2, BathWidth/2] × [−BathHeight/2, BathHeight/2].
    /// Nodes within <see cref="RodRadius"/> of the origin are assigned to the
    /// rod region; all other nodes are assigned to the bath region.
    /// </remarks>
    public GeometryMesh BuildMesh(int nodesX = 20, int nodesY = 20)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(nodesX, 2, nameof(nodesX));
        ArgumentOutOfRangeException.ThrowIfLessThan(nodesY, 2, nameof(nodesY));

        double halfW = BathWidth  / 2.0;
        double halfH = BathHeight / 2.0;
        double xStep = BathWidth  / (nodesX - 1);
        double yStep = BathHeight / (nodesY - 1);

        var xs = new double[nodesX];
        var ys = new double[nodesY];
        for (int i = 0; i < nodesX; i++) xs[i] = -halfW + i * xStep;
        for (int j = 0; j < nodesY; j++) ys[j] = -halfH + j * yStep;

        return BuildMesh(xs, ys);
    }

    /// <inheritdoc/>
    public GeometryMesh BuildMesh(double[] xCoordinates, double[] yCoordinates)
    {
        ArgumentNullException.ThrowIfNull(xCoordinates);
        ArgumentNullException.ThrowIfNull(yCoordinates);
        GeometryCoordinateValidation.ValidateStrictlyIncreasing(xCoordinates, nameof(xCoordinates));
        GeometryCoordinateValidation.ValidateStrictlyIncreasing(yCoordinates, nameof(yCoordinates));

        var xs = (double[])xCoordinates.Clone();
        var ys = (double[])yCoordinates.Clone();

        NodePhase rodRegion  = _rodIsAnode ? NodePhase.Anode : NodePhase.Cathode;
        NodePhase bathRegion = _rodIsAnode ? NodePhase.Cathode : NodePhase.Anode;
        double r2 = RodRadius * RodRadius;
        var regions = new NodePhase[xs.Length, ys.Length];
        for (int i = 0; i < xs.Length; i++)
            for (int j = 0; j < ys.Length; j++)
                regions[i, j] = xs[i] * xs[i] + ys[j] * ys[j] <= r2 ? rodRegion : bathRegion;

        return new GeometryMesh(xs, ys, regions);
    }
}
