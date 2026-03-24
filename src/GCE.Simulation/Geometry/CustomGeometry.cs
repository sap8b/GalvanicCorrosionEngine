using GCE.Core;
using GCE.Electrochemistry;

namespace GCE.Simulation.Geometry;

/// <summary>
/// Geometry builder for a fully user-defined configuration.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CustomGeometry"/> accepts explicit electrode areas and materials, making it
/// suitable for irregular geometries or configurations loaded from external data (e.g.
/// JSON or YAML).  An optional pre-built <see cref="GeometryMesh"/> may be supplied; if
/// omitted, a default unit-square mesh is generated when <see cref="BuildMesh"/> is called.
/// </para>
/// <para>
/// The default mesh (no custom mesh supplied) uses a 1 m × 1 m square domain divided at the
/// midpoint: the left half (x ≤ 0.5 m) is assigned to the anode region and the right half
/// (x &gt; 0.5 m) to the cathode region.
/// </para>
/// </remarks>
public sealed class CustomGeometry : IGeometryBuilder
{
    private static readonly double DefaultDomainSize = 1.0;

    private readonly GeometryMesh? _customMesh;

    /// <inheritdoc/>
    public IMaterial AnodeMaterial { get; }

    /// <inheritdoc/>
    public IMaterial CathodeMaterial { get; }

    /// <summary>Gets the electrochemically active area of the anode electrode (m²).</summary>
    public double AnodeArea { get; }

    /// <summary>Gets the electrochemically active area of the cathode electrode (m²).</summary>
    public double CathodeArea { get; }

    /// <param name="anodeMaterial">Material of the anode (lower standard potential).</param>
    /// <param name="cathodeMaterial">Material of the cathode (higher standard potential).</param>
    /// <param name="anodeArea">Electrochemically active anode area (m²); must be positive.</param>
    /// <param name="cathodeArea">Electrochemically active cathode area (m²); must be positive.</param>
    /// <param name="customMesh">
    /// Optional pre-built mesh.  When <see langword="null"/>, <see cref="BuildMesh"/> generates
    /// a default unit-square mesh.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="anodeMaterial"/> or <paramref name="cathodeMaterial"/> is
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="anodeArea"/> or <paramref name="cathodeArea"/> is not positive.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="cathodeMaterial"/> does not have a strictly higher standard
    /// potential than <paramref name="anodeMaterial"/>.
    /// </exception>
    public CustomGeometry(
        IMaterial anodeMaterial,
        IMaterial cathodeMaterial,
        double anodeArea,
        double cathodeArea,
        GeometryMesh? customMesh = null)
    {
        ArgumentNullException.ThrowIfNull(anodeMaterial);
        ArgumentNullException.ThrowIfNull(cathodeMaterial);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(anodeArea, 0.0, nameof(anodeArea));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(cathodeArea, 0.0, nameof(cathodeArea));

        if (cathodeMaterial.StandardPotential <= anodeMaterial.StandardPotential)
            throw new ArgumentException(
                "The cathode material must have a strictly higher standard potential than the anode material.");

        AnodeMaterial = anodeMaterial;
        CathodeMaterial = cathodeMaterial;
        AnodeArea = anodeArea;
        CathodeArea = cathodeArea;
        _customMesh = customMesh;
    }

    /// <inheritdoc/>
    public IGalvanicCell Build(IElectrolyte electrolyte)
    {
        ArgumentNullException.ThrowIfNull(electrolyte);

        var anode = new Electrode(AnodeMaterial, AnodeArea);
        var cathode = new Electrode(CathodeMaterial, CathodeArea);
        return new GalvanicCell(anode, cathode, electrolyte);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Returns the custom mesh supplied at construction time, if any.  Otherwise generates
    /// a default 1 m × 1 m unit-square mesh split at x = 0.5 m (left half = anode, right
    /// half = cathode).  The <paramref name="nodesX"/> and <paramref name="nodesY"/>
    /// parameters are ignored when a custom mesh has been provided.
    /// </remarks>
    public GeometryMesh BuildMesh(int nodesX = 20, int nodesY = 20)
    {
        if (_customMesh is not null)
            return _customMesh;

        ArgumentOutOfRangeException.ThrowIfLessThan(nodesX, 2, nameof(nodesX));
        ArgumentOutOfRangeException.ThrowIfLessThan(nodesY, 2, nameof(nodesY));

        double xStep = DefaultDomainSize / (nodesX - 1);
        double yStep = DefaultDomainSize / (nodesY - 1);

        var xs = new double[nodesX];
        var ys = new double[nodesY];

        for (int i = 0; i < nodesX; i++)
            xs[i] = i * xStep;
        for (int j = 0; j < nodesY; j++)
            ys[j] = j * yStep;

        double midpoint = DefaultDomainSize / 2.0;
        var regions = new int[nodesX, nodesY];
        for (int i = 0; i < nodesX; i++)
        {
            int region = xs[i] <= midpoint ? 0 : 1;
            for (int j = 0; j < nodesY; j++)
                regions[i, j] = region;
        }

        return new GeometryMesh(xs, ys, regions);
    }
}
