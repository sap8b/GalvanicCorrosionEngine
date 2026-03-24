using GCE.Core;
using GCE.Electrochemistry;

namespace GCE.Simulation.Geometry;

/// <summary>
/// Geometry builder for a side-by-side configuration: two flat metal sheets of
/// dissimilar materials placed in parallel contact, sharing a common electrolyte.
/// </summary>
/// <remarks>
/// <para>
/// The two sheets are modelled as lying side by side in the XY plane.  The anode
/// occupies x ∈ [0, <see cref="AnodeWidth"/>] and the cathode occupies
/// x ∈ (<see cref="AnodeWidth"/>, <see cref="AnodeWidth"/> + <see cref="CathodeWidth"/>].
/// Both sheets share the same <see cref="Length"/> (y-direction).
/// </para>
/// <para>
/// Electrode areas are computed as top-surface areas:
/// <list type="bullet">
///   <item>
///     <description>Anode area = <see cref="AnodeWidth"/> × <see cref="Length"/>.</description>
///   </item>
///   <item>
///     <description>Cathode area = <see cref="CathodeWidth"/> × <see cref="Length"/>.</description>
///   </item>
/// </list>
/// </para>
/// </remarks>
public sealed class SideBySideGeometry : IGeometryBuilder
{
    /// <summary>Gets the width of the anode sheet (m).</summary>
    public double AnodeWidth { get; }

    /// <summary>Gets the width of the cathode sheet (m).</summary>
    public double CathodeWidth { get; }

    /// <summary>Gets the shared length of both sheets in the y-direction (m).</summary>
    public double Length { get; }

    /// <inheritdoc/>
    public IMaterial AnodeMaterial { get; }

    /// <inheritdoc/>
    public IMaterial CathodeMaterial { get; }

    /// <param name="anodeMaterial">Material of the anode sheet (lower standard potential).</param>
    /// <param name="cathodeMaterial">Material of the cathode sheet (higher standard potential).</param>
    /// <param name="anodeWidth">Width of the anode sheet (m); must be positive.</param>
    /// <param name="cathodeWidth">Width of the cathode sheet (m); must be positive.</param>
    /// <param name="length">Length of both sheets (m); must be positive.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="anodeMaterial"/> or <paramref name="cathodeMaterial"/> is
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="anodeWidth"/>, <paramref name="cathodeWidth"/>, or
    /// <paramref name="length"/> is not positive.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="cathodeMaterial"/> does not have a strictly higher standard
    /// potential than <paramref name="anodeMaterial"/>.
    /// </exception>
    public SideBySideGeometry(
        IMaterial anodeMaterial,
        IMaterial cathodeMaterial,
        double anodeWidth,
        double cathodeWidth,
        double length)
    {
        ArgumentNullException.ThrowIfNull(anodeMaterial);
        ArgumentNullException.ThrowIfNull(cathodeMaterial);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(anodeWidth, 0.0, nameof(anodeWidth));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(cathodeWidth, 0.0, nameof(cathodeWidth));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(length, 0.0, nameof(length));

        if (cathodeMaterial.StandardPotential <= anodeMaterial.StandardPotential)
            throw new ArgumentException(
                "The cathode material must have a strictly higher standard potential than the anode material.");

        AnodeMaterial = anodeMaterial;
        CathodeMaterial = cathodeMaterial;
        AnodeWidth = anodeWidth;
        CathodeWidth = cathodeWidth;
        Length = length;
    }

    /// <inheritdoc/>
    public IGalvanicCell Build(IElectrolyte electrolyte)
    {
        ArgumentNullException.ThrowIfNull(electrolyte);

        var anode = new Electrode(AnodeMaterial, AnodeWidth * Length);
        var cathode = new Electrode(CathodeMaterial, CathodeWidth * Length);
        return new GalvanicCell(anode, cathode, electrolyte);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Generates a top-down view of the two sheets.  Nodes at x ≤ <see cref="AnodeWidth"/>
    /// are in region 0 (anode); nodes at x &gt; <see cref="AnodeWidth"/> are in region 1
    /// (cathode).
    /// </remarks>
    public GeometryMesh BuildMesh(int nodesX = 20, int nodesY = 20)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(nodesX, 2, nameof(nodesX));
        ArgumentOutOfRangeException.ThrowIfLessThan(nodesY, 2, nameof(nodesY));

        double totalWidth = AnodeWidth + CathodeWidth;
        double xStep = totalWidth / (nodesX - 1);
        double yStep = Length / (nodesY - 1);

        var xs = new double[nodesX];
        var ys = new double[nodesY];

        for (int i = 0; i < nodesX; i++)
            xs[i] = i * xStep;
        for (int j = 0; j < nodesY; j++)
            ys[j] = j * yStep;

        var regions = new int[nodesX, nodesY];
        for (int i = 0; i < nodesX; i++)
        {
            int region = xs[i] <= AnodeWidth ? 0 : 1;
            for (int j = 0; j < nodesY; j++)
                regions[i, j] = region;
        }

        return new GeometryMesh(xs, ys, regions);
    }
}
