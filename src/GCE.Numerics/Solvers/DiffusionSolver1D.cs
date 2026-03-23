namespace GCE.Numerics.Solvers;

/// <summary>
/// Solves the 1-D diffusion (heat/mass-transfer) equation
///   ∂u/∂t = ∂/∂x ( D(x) ∂u/∂x )
/// on a uniform grid [0, L] using the Crank–Nicolson implicit scheme,
/// which is unconditionally stable and second-order accurate in both space
/// and time.
/// </summary>
/// <remarks>
/// <para>
/// Grid layout: <c>nx</c> nodes at positions x_i = i · Δx,
/// i = 0 … nx − 1, where Δx = L / (nx − 1).
/// The flat solution array stored in <see cref="PdeSolverResult.Solution"/>
/// is ordered in increasing x.
/// </para>
/// <para>
/// Boundary conditions are specified via <see cref="IBoundaryCondition"/>
/// instances (Dirichlet, Neumann, or Robin).  Time-varying conditions are
/// evaluated at each step through <see cref="IBoundaryCondition.Evaluate"/>.
/// For Robin conditions the concrete type must be <see cref="RobinBC"/>.
/// </para>
/// <para>
/// When <c>useAdaptiveDt</c> is <see langword="true"/> the solver
/// applies a simple heuristic: the step is halved when the maximum nodal
/// change per step exceeds <c>targetChangeMax</c>, and doubled (up to
/// <c>maxDt</c>) when it falls below <c>targetChangeMin</c>.
/// </para>
/// <para>
/// Call <see cref="Solve"/> repeatedly to continue time evolution; the
/// internal state is preserved between calls.  Inspect
/// <see cref="CurrentSolution"/> to read the state without advancing it.
/// </para>
/// </remarks>
public sealed class DiffusionSolver1D : IPDESolver
{
    // ── Grid ──────────────────────────────────────────────────────────────────

    private readonly int    _nx;
    private readonly double _dx;

    // ── Diffusivity ───────────────────────────────────────────────────────────

    private readonly double[] _D;           // nodal values, length nx

    // ── Boundary conditions ───────────────────────────────────────────────────

    private readonly IBoundaryCondition _leftBC;
    private readonly IBoundaryCondition _rightBC;

    // ── Time stepping ─────────────────────────────────────────────────────────

    private double _dt;
    private readonly bool   _useAdaptiveDt;
    private readonly double _minDt;
    private readonly double _maxDt;
    private readonly double _targetChangeMin;
    private readonly double _targetChangeMax;

    // ── State ─────────────────────────────────────────────────────────────────

    private readonly double[] _u;           // current nodal solution, length nx
    private double _currentTime;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the solver with a spatially <em>constant</em> diffusivity.
    /// </summary>
    /// <param name="nx">Number of grid nodes (must be ≥ 2).</param>
    /// <param name="domainLength">Physical length of the domain L (must be &gt; 0).</param>
    /// <param name="diffusivity">Constant diffusion coefficient D (must be ≥ 0; use 0 for a pure-transport or no-diffusion case).</param>
    /// <param name="initialCondition">
    /// Nodal values at t = 0.  Must have length <paramref name="nx"/>.
    /// </param>
    /// <param name="leftBC">Boundary condition at x = 0.</param>
    /// <param name="rightBC">Boundary condition at x = <paramref name="domainLength"/>.</param>
    /// <param name="dt">Initial (or constant) time-step size (must be &gt; 0).</param>
    /// <param name="useAdaptiveDt">
    /// When <see langword="true"/>, the time step is adjusted automatically at each step.
    /// </param>
    /// <param name="minDt">Minimum allowed time step when adaptive mode is active.</param>
    /// <param name="maxDt">Maximum allowed time step when adaptive mode is active.</param>
    public DiffusionSolver1D(
        int                  nx,
        double               domainLength,
        double               diffusivity,
        double[]             initialCondition,
        IBoundaryCondition   leftBC,
        IBoundaryCondition   rightBC,
        double               dt,
        bool                 useAdaptiveDt    = false,
        double               minDt            = 1e-9,
        double               maxDt            = double.MaxValue)
        : this(nx, domainLength,
               CreateUniform(nx, diffusivity),
               initialCondition, leftBC, rightBC, dt,
               useAdaptiveDt, minDt, maxDt) { }

    /// <summary>
    /// Initialises the solver with a spatially <em>varying</em> diffusivity.
    /// </summary>
    /// <param name="diffusivity">
    /// Nodal diffusion coefficients D(x_i).  Must have length <paramref name="nx"/>
    /// and all values must be non-negative.
    /// </param>
    /// <inheritdoc cref="DiffusionSolver1D(int,double,double,double[],IBoundaryCondition,IBoundaryCondition,double,bool,double,double)"/>
    public DiffusionSolver1D(
        int                  nx,
        double               domainLength,
        double[]             diffusivity,
        double[]             initialCondition,
        IBoundaryCondition   leftBC,
        IBoundaryCondition   rightBC,
        double               dt,
        bool                 useAdaptiveDt    = false,
        double               minDt            = 1e-9,
        double               maxDt            = double.MaxValue)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(nx, 2, nameof(nx));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(domainLength, nameof(domainLength));
        ArgumentNullException.ThrowIfNull(diffusivity, nameof(diffusivity));
        if (diffusivity.Length != nx)
            throw new ArgumentException(
                $"diffusivity must have length {nx}.", nameof(diffusivity));
        foreach (double d in diffusivity)
            if (d < 0.0)
                throw new ArgumentOutOfRangeException(
                    nameof(diffusivity), "All diffusivity values must be non-negative.");
        ArgumentNullException.ThrowIfNull(initialCondition, nameof(initialCondition));
        if (initialCondition.Length != nx)
            throw new ArgumentException(
                $"initialCondition must have length {nx}.", nameof(initialCondition));
        ArgumentNullException.ThrowIfNull(leftBC,  nameof(leftBC));
        ArgumentNullException.ThrowIfNull(rightBC, nameof(rightBC));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dt, nameof(dt));

        _nx              = nx;
        _dx              = domainLength / (nx - 1.0);
        _D               = (double[])diffusivity.Clone();
        _u               = (double[])initialCondition.Clone();
        _leftBC          = leftBC;
        _rightBC         = rightBC;
        _dt              = dt;
        _useAdaptiveDt   = useAdaptiveDt;
        _minDt           = minDt;
        _maxDt           = maxDt;
        _targetChangeMin = 1e-6;
        _targetChangeMax = 0.1;
        _currentTime     = 0.0;
    }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the current simulation time, i.e. the time reached after the most
    /// recent <see cref="Solve"/> call (or 0 if <see cref="Solve"/> has not
    /// been called yet).
    /// </summary>
    public double CurrentTime => _currentTime;

    /// <summary>
    /// Gets the current nodal solution as a read-only span in increasing-x order.
    /// </summary>
    public ReadOnlySpan<double> CurrentSolution => _u.AsSpan();

    // ── IPDESolver ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// Advances the solution by at most <see cref="PdeSolverOptions.MaxTimeSteps"/>
    /// steps (or until the per-step L∞ change drops below
    /// <see cref="PdeSolverOptions.Tolerance"/>, treating a near-steady state as
    /// "converged").
    /// <para>
    /// <see cref="PdeSolverResult.Iterations"/> reports the number of time steps
    /// actually taken; <see cref="PdeSolverResult.Residual"/> is the maximum
    /// nodal change in the final step.
    /// </para>
    /// </remarks>
    public PdeSolverResult Solve(PdeSolverOptions? options = null)
    {
        var opts = options ?? new PdeSolverOptions();

        double lastResidual = 0.0;
        int    stepCount    = 0;
        var    uNew         = new double[_nx];

        for (int step = 0; step < opts.MaxTimeSteps; step++)
        {
            double dt = _useAdaptiveDt ? ClampDt(_dt) : _dt;

            StepCrankNicolson(_currentTime, dt, uNew);

            // L∞ change per step (used as residual and for adaptive-dt)
            lastResidual = 0.0;
            for (int i = 0; i < _nx; i++)
                lastResidual = Math.Max(lastResidual, Math.Abs(uNew[i] - _u[i]));

            Array.Copy(uNew, _u, _nx);
            _currentTime += dt;
            stepCount++;

            if (_useAdaptiveDt)
                _dt = AdjustDt(dt, lastResidual);

            if (lastResidual < opts.Tolerance)
                break;
        }

        return new PdeSolverResult
        {
            Solution   = (double[])_u.Clone(),
            Converged  = lastResidual < opts.Tolerance,
            Iterations = stepCount,
            Residual   = lastResidual,
        };
    }

    // ── Private – one Crank–Nicolson step ─────────────────────────────────────

    private void StepCrankNicolson(double time, double dt, double[] uNew)
    {
        double dx2 = _dx * _dx;

        var a   = new double[_nx];
        var b   = new double[_nx];
        var c   = new double[_nx];
        var rhs = new double[_nx];

        // ── Interior nodes (i = 1 … nx−2) ─────────────────────────────────────
        for (int i = 1; i < _nx - 1; i++)
        {
            double Dl = 0.5 * (_D[i - 1] + _D[i]);     // D_{i-1/2}
            double Dr = 0.5 * (_D[i]     + _D[i + 1]); // D_{i+1/2}
            double rl = dt * Dl / (2.0 * dx2);
            double rr = dt * Dr / (2.0 * dx2);

            a[i]   = -rl;
            b[i]   = 1.0 + rl + rr;
            c[i]   = -rr;
            rhs[i] = rl * _u[i - 1] + (1.0 - rl - rr) * _u[i] + rr * _u[i + 1];
        }

        // ── Left boundary ──────────────────────────────────────────────────────
        double tNew = time + dt;
        ApplyLeftBC(time, tNew, dt, dx2, a, b, c, rhs);

        // ── Right boundary ─────────────────────────────────────────────────────
        ApplyRightBC(time, tNew, dt, dx2, a, b, c, rhs);

        // ── Thomas algorithm ───────────────────────────────────────────────────
        SolveTridiagonal(a, b, c, rhs, uNew);
    }

    // ── Boundary condition application ────────────────────────────────────────

    private void ApplyLeftBC(double tOld, double tNew, double dt, double dx2,
                             double[] a, double[] b, double[] c, double[] rhs)
    {
        // Face diffusivities: ghost face uses D_0, first interior face = (D_0+D_1)/2
        double Dg = _D[0];                           // ghost face: D_{-1/2} ≈ D_0
        double Dn = (_nx > 1) ? 0.5 * (_D[0] + _D[1]) : _D[0]; // D_{+1/2}
        double rg = dt * Dg / (2.0 * dx2);
        double rn = dt * Dn / (2.0 * dx2);

        switch (_leftBC.Type)
        {
            case BoundaryConditionType.Dirichlet:
                a[0]   = 0.0;
                b[0]   = 1.0;
                c[0]   = 0.0;
                rhs[0] = _leftBC.Evaluate(tNew);
                break;

            case BoundaryConditionType.Neumann:
            {
                // ∂u/∂n = q at x=0; outward normal is −x, so ∂u/∂n = −∂u/∂x = q
                // Central-difference ghost: u_{-1} = u_1 + 2·dx·q
                // After ghost substitution into CN row 0:
                //   (1+rg+rn)·u_0^{n+1} + (−rg−rn)·u_1^{n+1}
                //   = (1−rg−rn)·u_0^n + (rg+rn)·u_1^n + 2·dx·rg·(q_old−q_new)
                double qOld = _leftBC.Evaluate(tOld);
                double qNew = _leftBC.Evaluate(tNew);
                a[0]   = 0.0;
                b[0]   = 1.0 + rg + rn;
                c[0]   = -(rg + rn);
                rhs[0] = (1.0 - rg - rn) * _u[0]
                       + (rg + rn)        * _u[1]
                       + 2.0 * _dx * rg * (qOld - qNew);
                break;
            }

            case BoundaryConditionType.Robin:
            {
                // α·u + β·(∂u/∂n) = γ(t);  ∂u/∂n = −∂u/∂x at x=0
                // Ghost: u_{-1} = u_1 + 2·dx/β·(γ − α·u_0)  (β≠0)
                // After substitution into CN row 0 and rearranging:
                //   (1+rg+rn − k·α)·u_0^{n+1} + (−rg−rn)·u_1^{n+1}
                //   = (1−rg−rn + k·α)·u_0^n + (rg+rn)·u_1^n − k·(γ_old + γ_new)
                // where k = 2·dx·rg/β
                var    robin    = (RobinBC)_leftBC;
                double alpha    = robin.Alpha;
                double beta     = robin.Beta;
                double gammaOld = robin.Evaluate(tOld);
                double gammaNew = robin.Evaluate(tNew);

                if (Math.Abs(beta) < 1e-15)
                {
                    // Degenerate to Dirichlet: u_0 = γ/α
                    a[0]   = 0.0;
                    b[0]   = 1.0;
                    c[0]   = 0.0;
                    rhs[0] = Math.Abs(alpha) < 1e-15 ? 0.0 : gammaNew / alpha;
                }
                else
                {
                    double k = 2.0 * _dx * rg / beta;
                    a[0]   = 0.0;
                    b[0]   = 1.0 + rg + rn - k * alpha;
                    c[0]   = -(rg + rn);
                    rhs[0] = (1.0 - rg - rn + k * alpha) * _u[0]
                           + (rg + rn)                   * _u[1]
                           - k * (gammaOld + gammaNew);
                }
                break;
            }
        }
    }

    private void ApplyRightBC(double tOld, double tNew, double dt, double dx2,
                              double[] a, double[] b, double[] c, double[] rhs)
    {
        int last = _nx - 1;
        // Face diffusivities: ghost face uses D_{n-1}, inner face = (D_{n-2}+D_{n-1})/2
        double Dg = _D[last];
        double Dn = (last > 0) ? 0.5 * (_D[last - 1] + _D[last]) : _D[last];
        double rg = dt * Dg / (2.0 * dx2);
        double rn = dt * Dn / (2.0 * dx2);

        switch (_rightBC.Type)
        {
            case BoundaryConditionType.Dirichlet:
                a[last]   = 0.0;
                b[last]   = 1.0;
                c[last]   = 0.0;
                rhs[last] = _rightBC.Evaluate(tNew);
                break;

            case BoundaryConditionType.Neumann:
            {
                // ∂u/∂n = q at x=L; outward normal is +x, so ∂u/∂n = ∂u/∂x = q
                // Ghost: u_n = u_{n-2} + 2·dx·q
                // After substitution into CN row (n-1):
                //   (−rn−rg)·u_{n-2}^{n+1} + (1+rn+rg)·u_{n-1}^{n+1}
                //   = (rn+rg)·u_{n-2}^n + (1−rn−rg)·u_{n-1}^n + 2·dx·rg·(q_old−q_new)
                double qOld = _rightBC.Evaluate(tOld);
                double qNew = _rightBC.Evaluate(tNew);
                a[last]   = -(rn + rg);
                b[last]   = 1.0 + rn + rg;
                c[last]   = 0.0;
                rhs[last] = (rn + rg)        * _u[last - 1]
                          + (1.0 - rn - rg)  * _u[last]
                          + 2.0 * _dx * rg * (qOld - qNew);
                break;
            }

            case BoundaryConditionType.Robin:
            {
                // α·u + β·(∂u/∂n) = γ(t);  ∂u/∂n = ∂u/∂x at x=L
                // Ghost: u_n = u_{n-2} + 2·dx/β·(γ − α·u_{n-1})  (β≠0)
                // After substitution:
                //   (−rn−rg)·u_{n-2}^{n+1} + (1+rn+rg + k·α)·u_{n-1}^{n+1}
                //   = (rn+rg)·u_{n-2}^n + (1−rn−rg − k·α)·u_{n-1}^n + k·(γ_old + γ_new)
                // where k = 2·dx·rg/β
                var    robin    = (RobinBC)_rightBC;
                double alpha    = robin.Alpha;
                double beta     = robin.Beta;
                double gammaOld = robin.Evaluate(tOld);
                double gammaNew = robin.Evaluate(tNew);

                if (Math.Abs(beta) < 1e-15)
                {
                    a[last]   = 0.0;
                    b[last]   = 1.0;
                    c[last]   = 0.0;
                    rhs[last] = Math.Abs(alpha) < 1e-15 ? 0.0 : gammaNew / alpha;
                }
                else
                {
                    double k = 2.0 * _dx * rg / beta;
                    a[last]   = -(rn + rg);
                    b[last]   = 1.0 + rn + rg + k * alpha;
                    c[last]   = 0.0;
                    rhs[last] = (rn + rg)                    * _u[last - 1]
                              + (1.0 - rn - rg - k * alpha)  * _u[last]
                              + k * (gammaOld + gammaNew);
                }
                break;
            }
        }
    }

    // ── Thomas (tridiagonal) algorithm ────────────────────────────────────────

    private static void SolveTridiagonal(
        double[] a, double[] b, double[] c, double[] d, double[] x)
    {
        int n = a.Length;

        // Work on copies so the caller's arrays are unmodified.
        var cMod = (double[])c.Clone();
        var dMod = (double[])d.Clone();

        // Forward sweep
        for (int i = 1; i < n; i++)
        {
            double w = a[i] / b[i - 1];
            b[i]    -= w * cMod[i - 1];
            dMod[i] -= w * dMod[i - 1];
        }

        // Back substitution
        x[n - 1] = dMod[n - 1] / b[n - 1];
        for (int i = n - 2; i >= 0; i--)
            x[i] = (dMod[i] - cMod[i] * x[i + 1]) / b[i];
    }

    // ── Adaptive time-step helpers ────────────────────────────────────────────

    private double ClampDt(double dt) =>
        Math.Clamp(dt, _minDt, _maxDt);

    private double AdjustDt(double dt, double maxChange)
    {
        if (maxChange > _targetChangeMax)
            dt *= 0.5;
        else if (maxChange < _targetChangeMin && maxChange > 0.0)
            dt *= 2.0;

        return ClampDt(dt);
    }

    // ── Factory helper ────────────────────────────────────────────────────────

    private static double[] CreateUniform(int n, double value)
    {
        var arr = new double[n];
        Array.Fill(arr, value);
        return arr;
    }
}
