namespace GCE.Numerics.Solvers;

/// <summary>
/// Configuration options shared by all <see cref="ILinearSolver"/> implementations.
/// </summary>
/// <remarks>
/// Options are used to control convergence behaviour for iterative solvers and are
/// ignored (or treated as upper bounds) by direct solvers that do not iterate.
/// </remarks>
public sealed class LinearSolverOptions
{
    /// <summary>
    /// Gets the maximum number of iterations allowed before the solver declares failure.
    /// Applicable to iterative methods; direct methods typically complete in one step.
    /// </summary>
    public int MaxIterations { get; init; } = 1_000;

    /// <summary>
    /// Gets the absolute residual tolerance at which an iterative solver is considered
    /// converged.  The solver stops when ‖Ax − b‖ ≤ <see cref="Tolerance"/>.
    /// </summary>
    public double Tolerance { get; init; } = 1e-10;
}
