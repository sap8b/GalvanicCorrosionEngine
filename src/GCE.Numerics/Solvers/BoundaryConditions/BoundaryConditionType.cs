namespace GCE.Numerics.Solvers;

/// <summary>
/// Identifies the mathematical type of a boundary condition applied to a PDE problem.
/// </summary>
public enum BoundaryConditionType
{
    /// <summary>
    /// A Dirichlet (essential) boundary condition prescribes the solution value
    /// directly on the boundary: u = g(t).
    /// </summary>
    Dirichlet,

    /// <summary>
    /// A Neumann (natural) boundary condition prescribes the outward normal
    /// derivative (flux) on the boundary: ∂u/∂n = q(t).
    /// </summary>
    Neumann,

    /// <summary>
    /// A Robin (mixed or convective) boundary condition is a linear combination of
    /// the solution value and its normal derivative on the boundary:
    /// α·u + β·(∂u/∂n) = γ(t).
    /// </summary>
    Robin,
}
