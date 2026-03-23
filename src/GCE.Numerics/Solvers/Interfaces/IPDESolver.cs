namespace GCE.Numerics.Solvers;

/// <summary>
/// Defines a solver for a partial differential equation (PDE) on a discretised domain.
/// </summary>
/// <remarks>
/// <para>
/// Implementations cover steady-state (elliptic) problems such as the Laplace and
/// Poisson equations, as well as transient (parabolic) problems such as the diffusion
/// equation.  The interface is intentionally solver-agnostic: concrete types may use
/// direct linear algebra (e.g., Gaussian elimination), classic iterative schemes
/// (e.g., Gauss–Seidel, SOR), or Krylov methods (e.g., Conjugate Gradient).
/// </para>
/// <para>
/// Domain geometry, grid resolution, boundary conditions, and source terms are
/// supplied to the concrete implementation — typically through its constructor —
/// and are not part of this interface.  <see cref="PdeSolverOptions"/> provides
/// run-time knobs that control convergence behaviour and are common to all
/// implementations.
/// </para>
/// </remarks>
public interface IPDESolver
{
    /// <summary>
    /// Solves the configured PDE problem and returns the solution together with
    /// convergence diagnostics.
    /// </summary>
    /// <param name="options">
    /// Optional solver configuration.  When <see langword="null"/>, implementation
    /// defaults apply.
    /// </param>
    /// <returns>
    /// A <see cref="PdeSolverResult"/> containing the nodal solution array,
    /// convergence flag, iteration count, and final residual.
    /// </returns>
    PdeSolverResult Solve(PdeSolverOptions? options = null);
}
