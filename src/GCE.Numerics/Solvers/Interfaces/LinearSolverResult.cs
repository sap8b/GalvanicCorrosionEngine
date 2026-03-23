namespace GCE.Numerics.Solvers;

/// <summary>
/// Encapsulates the outcome of a linear-system solve Ax = b.
/// </summary>
public sealed class LinearSolverResult
{
    /// <summary>
    /// Gets the computed solution vector x such that Ax ≈ b.
    /// </summary>
    public required double[] Solution { get; init; }

    /// <summary>
    /// Gets a value indicating whether the solver converged within the requested
    /// tolerance and iteration budget.
    /// </summary>
    public bool Converged { get; init; }

    /// <summary>
    /// Gets the number of iterations performed.  Direct solvers report <c>1</c>;
    /// iterative solvers report the actual iteration count.
    /// </summary>
    public int Iterations { get; init; }

    /// <summary>
    /// Gets the final absolute residual ‖Ax − b‖ achieved by the solver.
    /// </summary>
    public double Residual { get; init; }
}
