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
}
