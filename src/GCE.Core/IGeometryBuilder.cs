namespace GCE.Core;

/// <summary>
/// Builder that constructs a <see cref="IGalvanicCell"/> and an optional 2-D spatial
/// mesh from a concrete physical geometry configuration.
/// </summary>
/// <remarks>
/// Implementations encode the geometry-specific area calculations and spatial
/// layout, and expose both the electrochemical cell (via <see cref="Build"/>) and
/// a discretised mesh (via <see cref="BuildMesh"/>) for downstream PDE solvers.
/// </remarks>
public interface IGeometryBuilder
{
    /// <summary>Gets the material assigned to the anodic region of the geometry.</summary>
    IMaterial AnodeMaterial { get; }

    /// <summary>Gets the material assigned to the cathodic region of the geometry.</summary>
    IMaterial CathodeMaterial { get; }

    /// <summary>
    /// Constructs a <see cref="IGalvanicCell"/> using the given electrolyte, with
    /// electrode areas derived from the geometry.
    /// </summary>
    /// <param name="electrolyte">Electrolyte connecting the two electrodes.</param>
    /// <returns>A fully configured galvanic cell.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="electrolyte"/> is <see langword="null"/>.
    /// </exception>
    IGalvanicCell Build(IElectrolyte electrolyte);

    /// <summary>
    /// Generates a 2-D mesh representing the spatial layout of the geometry.
    /// </summary>
    /// <param name="nodesX">Number of nodes along the x-axis (minimum 2).</param>
    /// <param name="nodesY">Number of nodes along the y-axis (minimum 2).</param>
    /// <returns>A <see cref="GeometryMesh"/> describing the grid and region assignments.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="nodesX"/> or <paramref name="nodesY"/> is less than 2.
    /// </exception>
    GeometryMesh BuildMesh(int nodesX = 20, int nodesY = 20);

    /// <summary>
    /// Generates a 2-D mesh using explicitly supplied coordinates.
    /// </summary>
    /// <remarks>
    /// This overload supports non-uniform rectilinear grids.  The minimum resolvable
    /// interface recession/deposition along x is one local node pitch (Δx).
    /// </remarks>
    /// <param name="xCoordinates">Strictly increasing x-axis node coordinates (m), minimum length 2.</param>
    /// <param name="yCoordinates">Strictly increasing y-axis node coordinates (m), minimum length 2.</param>
    /// <returns>A <see cref="GeometryMesh"/> describing the grid and region assignments.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="xCoordinates"/> or <paramref name="yCoordinates"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when either coordinate array has fewer than 2 points or is not strictly increasing.
    /// </exception>
    GeometryMesh BuildMesh(double[] xCoordinates, double[] yCoordinates);
}
