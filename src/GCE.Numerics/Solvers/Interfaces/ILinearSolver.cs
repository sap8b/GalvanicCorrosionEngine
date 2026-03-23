namespace GCE.Numerics.Solvers;

/// <summary>
/// Defines a solver for the dense linear system Ax = b.
/// </summary>
/// <remarks>
/// Implementations may use direct methods (e.g., Gaussian elimination, LU factorisation)
/// or iterative methods (e.g., Conjugate Gradient, GMRES).  The caller selects the
/// desired behaviour by choosing a concrete implementation; the interface contract is
/// identical for both categories.
/// </remarks>
public interface ILinearSolver
{
    /// <summary>
    /// Solves the system Ax = b and returns the solution together with convergence
    /// diagnostics.
    /// </summary>
    /// <param name="a">
    /// The square coefficient matrix A of size n × n stored in row-major order.
    /// </param>
    /// <param name="b">The right-hand-side vector b of length n.</param>
    /// <param name="options">
    /// Optional solver configuration.  When <see langword="null"/>, implementation
    /// defaults apply.
    /// </param>
    /// <returns>
    /// A <see cref="LinearSolverResult"/> containing the solution x, convergence
    /// flag, iteration count, and final residual.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="a"/> is not square, or its dimension does not
    /// match the length of <paramref name="b"/>.
    /// </exception>
    LinearSolverResult Solve(double[,] a, double[] b, LinearSolverOptions? options = null);
}
