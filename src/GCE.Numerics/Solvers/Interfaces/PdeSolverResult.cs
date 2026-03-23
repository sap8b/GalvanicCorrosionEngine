namespace GCE.Numerics.Solvers;

/// <summary>
/// Encapsulates the outcome of a PDE solve performed by an <see cref="IPDESolver"/>.
/// </summary>
public sealed class PdeSolverResult
{
    /// <summary>
    /// Gets the computed solution as a flat array of nodal values.
    /// The ordering of entries is defined by the concrete solver implementation
    /// (e.g., row-major for a 2-D Cartesian grid).
    /// </summary>
    public required double[] Solution { get; init; }

    /// <summary>
    /// Gets a value indicating whether the solver converged within the requested
    /// tolerance and iteration budget.
    /// </summary>
    public bool Converged { get; init; }

    /// <summary>
    /// Gets the number of iterations performed (outer iterations for non-linear
    /// problems; time steps for transient solvers).
    /// </summary>
    public int Iterations { get; init; }

    /// <summary>
    /// Gets the final absolute residual achieved by the solver.
    /// </summary>
    public double Residual { get; init; }
}
