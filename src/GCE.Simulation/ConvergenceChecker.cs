namespace GCE.Simulation;

/// <summary>
/// Records convergence data for a single iteration of an iterative solver.
/// </summary>
/// <param name="Iteration">The iteration index (0-based).</param>
/// <param name="Residual">The residual norm at this iteration.</param>
/// <param name="Change">The change in the solution between this and the previous iteration.</param>
/// <param name="Converged">
/// <see langword="true"/> when the convergence criteria were satisfied at this iteration.
/// </param>
public sealed record ConvergenceInfo(
    int Iteration,
    double Residual,
    double Change,
    bool Converged);

/// <summary>
/// Checks convergence of an iterative calculation using multiple criteria and
/// maintains a history of convergence data for post-run diagnostics.
/// </summary>
/// <remarks>
/// <para>
/// Convergence is declared when <em>all</em> of the following are satisfied:
/// </para>
/// <list type="bullet">
///   <item><description>the residual is ≤ <see cref="ResidualTolerance"/>, and</description></item>
///   <item><description>the change is ≤ <see cref="ChangeTolerance"/>.</description></item>
/// </list>
/// <para>
/// Convergence is treated as a failure when the iteration count exceeds
/// <see cref="MaxIterations"/>.
/// </para>
/// </remarks>
public sealed class ConvergenceChecker
{
    private readonly List<ConvergenceInfo> _history = [];

    /// <summary>Gets the residual tolerance below which the residual criterion is met.</summary>
    public double ResidualTolerance { get; }

    /// <summary>Gets the change tolerance below which the change criterion is met.</summary>
    public double ChangeTolerance { get; }

    /// <summary>Gets the maximum allowed number of iterations before failure is reported.</summary>
    public int MaxIterations { get; }

    /// <summary>Gets the convergence history since the last <see cref="Reset"/>.</summary>
    public IReadOnlyList<ConvergenceInfo> History => _history;

    /// <summary>
    /// Gets a value indicating whether the most recent call to <see cref="Check"/> reported
    /// convergence.  Returns <see langword="false"/> if <see cref="Check"/> has never been called.
    /// </summary>
    public bool LastConverged =>
        _history.Count > 0 && _history[_history.Count - 1].Converged;

    /// <summary>
    /// Gets a value indicating whether the iteration count exceeded <see cref="MaxIterations"/>
    /// without convergence.
    /// </summary>
    public bool Failed =>
        _history.Count > 0 &&
        !LastConverged &&
        _history[_history.Count - 1].Iteration >= MaxIterations - 1;

    /// <summary>
    /// Initialises a new <see cref="ConvergenceChecker"/> with the given tolerances.
    /// </summary>
    /// <param name="residualTolerance">
    /// Residual norm below which the residual criterion is satisfied.
    /// Must be positive.
    /// </param>
    /// <param name="changeTolerance">
    /// Solution-change norm below which the change criterion is satisfied.
    /// Must be positive.
    /// </param>
    /// <param name="maxIterations">
    /// Maximum number of iterations before the check is considered failed.
    /// Must be at least 1.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any argument is out of range.
    /// </exception>
    public ConvergenceChecker(
        double residualTolerance = 1e-6,
        double changeTolerance   = 1e-8,
        int    maxIterations     = 100)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(residualTolerance, 0.0, nameof(residualTolerance));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(changeTolerance, 0.0, nameof(changeTolerance));
        ArgumentOutOfRangeException.ThrowIfLessThan(maxIterations, 1, nameof(maxIterations));

        ResidualTolerance = residualTolerance;
        ChangeTolerance   = changeTolerance;
        MaxIterations     = maxIterations;
    }

    /// <summary>
    /// Records the convergence data for one iteration and returns whether convergence
    /// has been achieved.
    /// </summary>
    /// <param name="iteration">The current iteration index (0-based).</param>
    /// <param name="residual">The current residual norm.</param>
    /// <param name="change">The current solution-change norm.</param>
    /// <returns>
    /// <see langword="true"/> when both the residual and change criteria are satisfied;
    /// otherwise <see langword="false"/>.
    /// </returns>
    public bool Check(int iteration, double residual, double change)
    {
        bool converged = residual <= ResidualTolerance && change <= ChangeTolerance;
        _history.Add(new ConvergenceInfo(iteration, residual, change, converged));
        return converged;
    }

    /// <summary>
    /// Proposes an adapted time-step size based on the observed change in the solution.
    /// </summary>
    /// <remarks>
    /// The new step is selected so that the expected change at the next step equals
    /// <paramref name="targetChange"/>:
    /// <c>newDt = currentDt × targetChange / change</c>, clamped to
    /// [<paramref name="minDt"/>, <paramref name="maxDt"/>].
    /// When <paramref name="change"/> is zero or negligibly small the step is doubled
    /// (subject to the maximum).
    /// </remarks>
    /// <param name="currentDt">The current time-step size (s). Must be positive.</param>
    /// <param name="change">The observed solution change at this step.</param>
    /// <param name="targetChange">
    /// The desired solution change per step. Defaults to 1 × 10⁻⁴.
    /// </param>
    /// <param name="minDt">Minimum allowable time step (s). Defaults to 1 × 10⁻⁶.</param>
    /// <param name="maxDt">Maximum allowable time step (s). Defaults to 1.0.</param>
    /// <returns>A new time-step size within [<paramref name="minDt"/>, <paramref name="maxDt"/>].</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="currentDt"/>, <paramref name="minDt"/>, or
    /// <paramref name="maxDt"/> is not positive.
    /// </exception>
    public double AdaptTimeStep(
        double currentDt,
        double change,
        double targetChange = 1e-4,
        double minDt        = 1e-6,
        double maxDt        = 1.0)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(currentDt, 0.0, nameof(currentDt));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(minDt, 0.0, nameof(minDt));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxDt, 0.0, nameof(maxDt));

        double newDt = Math.Abs(change) < 1e-15
            ? currentDt * 2.0
            : currentDt * (targetChange / Math.Abs(change));

        return Math.Clamp(newDt, minDt, maxDt);
    }

    /// <summary>
    /// Clears the convergence history, resetting the checker to its initial state.
    /// </summary>
    public void Reset() => _history.Clear();
}
