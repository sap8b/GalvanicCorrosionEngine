using GCE.Numerics;

namespace GCE.Simulation;

/// <summary>
/// Advances the simulation potential in time using Runge–Kutta 4 integration,
/// optionally with convergence-driven adaptive time-stepping.
/// </summary>
/// <remarks>
/// <para>
/// Each call to <see cref="Advance"/> performs a single fixed-size RK4 step.
/// </para>
/// <para>
/// Each call to <see cref="AdvanceAdaptive"/> attempts a step and, if the local
/// solution change is too large, halves the step and retries, clamping the final
/// step to the limits configured in the underlying <see cref="ConvergenceChecker"/>.
/// The <see cref="ConvergenceChecker.History"/> records the convergence data for
/// every attempted step (including retries).
/// </para>
/// </remarks>
public sealed class TimeEvolver
{
    private readonly ConvergenceChecker _convergenceChecker;

    /// <summary>Gets the convergence history accumulated since creation or last reset.</summary>
    public IReadOnlyList<ConvergenceInfo> ConvergenceHistory =>
        _convergenceChecker.History;

    /// <summary>
    /// Initialises a <see cref="TimeEvolver"/> with the supplied convergence checker.
    /// </summary>
    /// <param name="convergenceChecker">
    /// The checker used to evaluate convergence and adapt the time step.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="convergenceChecker"/> is <see langword="null"/>.
    /// </exception>
    public TimeEvolver(ConvergenceChecker convergenceChecker)
    {
        ArgumentNullException.ThrowIfNull(convergenceChecker);
        _convergenceChecker = convergenceChecker;
    }

    /// <summary>
    /// Advances the solution by a single RK4 step of size <paramref name="dt"/>.
    /// </summary>
    /// <param name="t">Current simulation time (s).</param>
    /// <param name="potential">Current mixed potential (V vs. SHE).</param>
    /// <param name="dt">Step size (s). Must be positive.</param>
    /// <param name="ode">
    /// The ODE right-hand side f(t, y) = dy/dt. Called multiple times per step.
    /// </param>
    /// <returns>The potential at time <c>t + dt</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ode"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="dt"/> is not positive.
    /// </exception>
    public double Advance(double t, double potential, double dt, OdeFunction ode)
    {
        ArgumentNullException.ThrowIfNull(ode);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(dt, 0.0, nameof(dt));

        var solver = new RungeKuttaSolver(ode);
        return solver.Step(t, potential, dt);
    }

    /// <summary>
    /// Advances the solution with adaptive step-size control.
    /// </summary>
    /// <remarks>
    /// The method takes a trial step of <paramref name="dt"/>. If the absolute change in
    /// the potential exceeds the convergence-checker's target change, the step is halved
    /// and retried. Up to 10 halvings are attempted; after that the best available
    /// step is accepted.
    /// </remarks>
    /// <param name="t">Current simulation time (s).</param>
    /// <param name="potential">Current mixed potential (V vs. SHE).</param>
    /// <param name="dt">Nominal step size (s). Must be positive.</param>
    /// <param name="ode">The ODE right-hand side f(t, y).</param>
    /// <param name="targetChange">
    /// Target absolute change in potential per step. Default 1 × 10⁻⁴ V.
    /// </param>
    /// <param name="minDt">Minimum allowable step size (s). Default 1 × 10⁻⁶ s.</param>
    /// <param name="maxDt">Maximum allowable step size (s). Default 1.0 s.</param>
    /// <returns>
    /// A tuple <c>(NextPotential, ActualDt)</c> giving the accepted potential value
    /// and the step size that was used.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ode"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="dt"/> is not positive.
    /// </exception>
    public (double NextPotential, double ActualDt) AdvanceAdaptive(
        double      t,
        double      potential,
        double      dt,
        OdeFunction ode,
        double      targetChange = 1e-4,
        double      minDt        = 1e-6,
        double      maxDt        = 1.0)
    {
        ArgumentNullException.ThrowIfNull(ode);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(dt, 0.0, nameof(dt));

        var solver     = new RungeKuttaSolver(ode);
        double trialDt = Math.Clamp(dt, minDt, maxDt);

        const int maxHalvings = 10;
        for (int attempt = 0; attempt <= maxHalvings; attempt++)
        {
            double next     = solver.Step(t, potential, trialDt);
            double change   = Math.Abs(next - potential);
            // Residual: magnitude of the ODE derivative at the accepted point,
            // distinct from the solution change used for step-size control.
            double residual = Math.Abs(ode(t + trialDt, next));

            bool converged = _convergenceChecker.Check(attempt, residual, change);

            if (converged || attempt == maxHalvings)
                return (next, trialDt);

            // Step is too large — halve and retry
            trialDt = Math.Max(trialDt * 0.5, minDt);
        }

        // Unreachable, but satisfies the compiler.
        return (potential, trialDt);
    }
}
