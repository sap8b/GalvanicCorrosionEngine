namespace GCE.Numerics.Solvers;

/// <summary>
/// Configuration options shared by all <see cref="IPDESolver"/> implementations.
/// </summary>
/// <remarks>
/// Options govern convergence behaviour for iterative PDE solvers and time-stepping
/// bounds for transient (parabolic/hyperbolic) solvers.  Elliptic solvers that use a
/// single direct solve may ignore the iteration and time-step settings.
/// </remarks>
public sealed class PdeSolverOptions
{
    /// <summary>
    /// Gets the maximum number of solver iterations (outer iterations for non-linear
    /// problems, or inner iterations for iterative linear sub-solvers).
    /// </summary>
    public int MaxIterations { get; init; } = 1_000;

    /// <summary>
    /// Gets the absolute residual tolerance at which the solver is considered converged.
    /// </summary>
    public double Tolerance { get; init; } = 1e-10;

    /// <summary>
    /// Gets the maximum number of time steps for transient (time-dependent) PDE solvers.
    /// Steady-state solvers ignore this value.
    /// </summary>
    public int MaxTimeSteps { get; init; } = 1_000;
}
