using System.Threading.Tasks;

namespace GCE.Numerics.Solvers;


/// <summary>
/// Solves the 2-D diffusion equation
///   ∂u/∂t = D · (∂²u/∂x² + ∂²u/∂y²)
/// on a uniform rectangular grid [0, Lx] × [0, Ly] using the
/// Peaceman–Rachford Alternating Direction Implicit (ADI) method.
/// </summary>
/// <remarks>
/// <para>
/// Grid layout: <c>nx × ny</c> nodes at positions (x_i, y_j) = (i·Δx, j·Δy),
/// i = 0 … nx − 1, j = 0 … ny − 1.
/// The flat solution array in <see cref="PdeSolverResult.Solution"/> uses
/// <em>row-major</em> order: index = i·ny + j (x varies in the outer loop,
/// y in the inner loop).
/// </para>
/// <para>
/// Each full time step is split into two half-steps of size dt/2:
/// </para>
/// <list type="number">
///   <item>
///     <description>
///       <b>x-implicit sweep:</b> for each row j, solve a tridiagonal system
///       in x using the fully explicit y-diffusion from the current level.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>y-implicit sweep:</b> for each column i, solve a tridiagonal system
///       in y using the x-diffusion from the intermediate level.
///     </description>
///   </item>
/// </list>
/// <para>
/// Variable diffusivity is not supported in the 2-D solver; use
/// <see cref="DiffusionSolver1D"/> for 1-D problems with variable D.
/// Only Dirichlet and Neumann boundary conditions are supported on each side.
/// A <see cref="RobinBC"/> will throw <see cref="NotSupportedException"/>.
/// </para>
/// <para>
/// When <c>useAdaptiveDt</c> is <see langword="true"/>, the same heuristic as
/// <see cref="DiffusionSolver1D"/> is used: halve dt when the L∞ change
/// exceeds <c>targetChangeMax</c>, double it when below <c>targetChangeMin</c>.
/// </para>
/// <para>
/// Call <see cref="Solve"/> repeatedly to continue time evolution; the
/// internal state is preserved between calls.
/// </para>
/// </remarks>
public sealed class DiffusionSolver2D : IPDESolver
{
    // ── Grid ──────────────────────────────────────────────────────────────────

    private readonly int    _nx;
    private readonly int    _ny;
    private readonly double _dx;
    private readonly double _dy;

    // ── Physics ───────────────────────────────────────────────────────────────

    private readonly double _D;

    // ── Boundary conditions ───────────────────────────────────────────────────

    private readonly IBoundaryCondition _leftBC;    // x = 0
    private readonly IBoundaryCondition _rightBC;   // x = Lx
    private readonly IBoundaryCondition _bottomBC;  // y = 0
    private readonly IBoundaryCondition _topBC;     // y = Ly

    // ── Time stepping ─────────────────────────────────────────────────────────

    private double _dt;
    private readonly bool   _useAdaptiveDt;
    private readonly double _minDt;
    private readonly double _maxDt;
    private readonly double _targetChangeMin;
    private readonly double _targetChangeMax;

    // ── State (row-major: u[i*ny + j]) ────────────────────────────────────────

    private readonly double[] _u;
    private double _currentTime;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the 2-D diffusion solver.
    /// </summary>
    /// <param name="nx">Number of grid nodes in the x-direction (must be ≥ 2).</param>
    /// <param name="ny">Number of grid nodes in the y-direction (must be ≥ 2).</param>
    /// <param name="domainLengthX">Physical width of the domain Lx (must be &gt; 0).</param>
    /// <param name="domainLengthY">Physical height of the domain Ly (must be &gt; 0).</param>
    /// <param name="diffusivity">Constant diffusion coefficient D (must be ≥ 0; use 0 for a pure-transport or no-diffusion case).</param>
    /// <param name="initialCondition">
    /// Flat array of initial nodal values, row-major (index = i·ny + j).
    /// Must have length <paramref name="nx"/> · <paramref name="ny"/>.
    /// </param>
    /// <param name="leftBC">Boundary condition on the x = 0 face.</param>
    /// <param name="rightBC">Boundary condition on the x = Lx face.</param>
    /// <param name="bottomBC">Boundary condition on the y = 0 face.</param>
    /// <param name="topBC">Boundary condition on the y = Ly face.</param>
    /// <param name="dt">Initial (or constant) time-step size (must be &gt; 0).</param>
    /// <param name="useAdaptiveDt">
    /// When <see langword="true"/>, the time step is adjusted automatically.
    /// </param>
    /// <param name="minDt">Minimum allowed time step (adaptive mode only).</param>
    /// <param name="maxDt">Maximum allowed time step (adaptive mode only).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any dimension, length, or time-step parameter is out of range.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any boundary condition or array is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="initialCondition"/> has the wrong length.
    /// </exception>
    public DiffusionSolver2D(
        int                  nx,
        int                  ny,
        double               domainLengthX,
        double               domainLengthY,
        double               diffusivity,
        double[]             initialCondition,
        IBoundaryCondition   leftBC,
        IBoundaryCondition   rightBC,
        IBoundaryCondition   bottomBC,
        IBoundaryCondition   topBC,
        double               dt,
        bool                 useAdaptiveDt    = false,
        double               minDt            = 1e-9,
        double               maxDt            = double.MaxValue)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(nx, 2, nameof(nx));
        ArgumentOutOfRangeException.ThrowIfLessThan(ny, 2, nameof(ny));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(domainLengthX, nameof(domainLengthX));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(domainLengthY, nameof(domainLengthY));
        ArgumentOutOfRangeException.ThrowIfNegative(diffusivity, nameof(diffusivity));
        ArgumentNullException.ThrowIfNull(initialCondition, nameof(initialCondition));
        if (initialCondition.Length != nx * ny)
            throw new ArgumentException(
                $"initialCondition must have length {nx * ny} (nx × ny).",
                nameof(initialCondition));
        ArgumentNullException.ThrowIfNull(leftBC,   nameof(leftBC));
        ArgumentNullException.ThrowIfNull(rightBC,  nameof(rightBC));
        ArgumentNullException.ThrowIfNull(bottomBC, nameof(bottomBC));
        ArgumentNullException.ThrowIfNull(topBC,    nameof(topBC));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dt, nameof(dt));

        _nx              = nx;
        _ny              = ny;
        _dx              = domainLengthX / (nx - 1.0);
        _dy              = domainLengthY / (ny - 1.0);
        _D               = diffusivity;
        _u               = (double[])initialCondition.Clone();
        _leftBC          = leftBC;
        _rightBC         = rightBC;
        _bottomBC        = bottomBC;
        _topBC           = topBC;
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
    /// Gets the current simulation time.
    /// </summary>
    public double CurrentTime => _currentTime;

    /// <summary>
    /// Gets the current nodal solution as a read-only span in row-major order
    /// (index = i·ny + j).
    /// </summary>
    public ReadOnlySpan<double> CurrentSolution => _u.AsSpan();

    // ── IPDESolver ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// Advances the solution by at most <see cref="PdeSolverOptions.MaxTimeSteps"/>
    /// steps.  Convergence is declared when the per-step L∞ change drops below
    /// <see cref="PdeSolverOptions.Tolerance"/>.
    /// <para>
    /// <see cref="PdeSolverResult.Solution"/> is in row-major order:
    /// index = i·ny + j.
    /// </para>
    /// </remarks>
    public PdeSolverResult Solve(PdeSolverOptions? options = null)
    {
        var opts = options ?? new PdeSolverOptions();

        ValidateBoundaryConditions();

        double lastResidual = 0.0;
        int    stepCount    = 0;
        var    uNew         = new double[_nx * _ny];

        for (int step = 0; step < opts.MaxTimeSteps; step++)
        {
            double dt = _useAdaptiveDt ? ClampDt(_dt) : _dt;

            StepADI(_currentTime, dt, uNew);

            lastResidual = 0.0;
            for (int k = 0; k < _nx * _ny; k++)
                lastResidual = Math.Max(lastResidual, Math.Abs(uNew[k] - _u[k]));

            Array.Copy(uNew, _u, _nx * _ny);
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

    // ── Private – one ADI step ─────────────────────────────────────────────────

    private void StepADI(double time, double dt, double[] uNew)
    {
        double halfDt = dt * 0.5;
        double tHalf  = time + halfDt;
        double tNew   = time + dt;

        double rx = _D * halfDt / (_dx * _dx); // Fourier number in x (for half-step)
        double ry = _D * halfDt / (_dy * _dy); // Fourier number in y (for half-step)

        var uHalf = new double[_nx * _ny];

        // ── Half-step 1: x-implicit, y-explicit ─────────────────────────────
        // For each row j, solve a tridiagonal system in x.
        // Rows are mutually independent: different j values write to distinct
        // uHalf slots (Idx(i,j) = i*ny+j), so the sweep is safely parallelised.
        Parallel.For(0, _ny, j =>
        {
            // Enforce Dirichlet y-BCs at the intermediate level without a solve.
            if (j == 0 && _bottomBC.Type == BoundaryConditionType.Dirichlet)
            {
                double val = _bottomBC.Evaluate(tHalf);
                for (int i = 0; i < _nx; i++) uHalf[Idx(i, 0)] = val;
                return;
            }
            if (j == _ny - 1 && _topBC.Type == BoundaryConditionType.Dirichlet)
            {
                double val = _topBC.Evaluate(tHalf);
                for (int i = 0; i < _nx; i++) uHalf[Idx(i, _ny - 1)] = val;
                return;
            }

            var a   = new double[_nx];
            var b   = new double[_nx];
            var c   = new double[_nx];
            var rhs = new double[_nx];

            // Interior x-nodes: LHS is x-implicit; RHS is the y-explicit part.
            for (int i = 1; i < _nx - 1; i++)
            {
                a[i]   = -rx;
                b[i]   = 1.0 + 2.0 * rx;
                c[i]   = -rx;
                rhs[i] = YExplicitRhs(i, j, ry, time);
            }

            // x-boundary nodes: set LHS and overwrite RHS.
            ApplyXLeftBC(j, time, tHalf, rx, ry, a, b, c, rhs);
            ApplyXRightBC(j, time, tHalf, rx, ry, a, b, c, rhs);

            var row = new double[_nx];
            SolveTridiagonal(a, b, c, rhs, row);
            for (int i = 0; i < _nx; i++)
                uHalf[Idx(i, j)] = row[i];
        });

        // ── Half-step 2: y-implicit, x-explicit ─────────────────────────────
        // For each column i, solve a tridiagonal system in y.
        // Columns are mutually independent: different i values write to distinct
        // uNew slots (Idx(i,j) = i*ny+j), so the sweep is safely parallelised.
        Parallel.For(0, _nx, i =>
        {
            // Enforce Dirichlet x-BCs at the new level without a solve.
            if (i == 0 && _leftBC.Type == BoundaryConditionType.Dirichlet)
            {
                double val = _leftBC.Evaluate(tNew);
                for (int j = 0; j < _ny; j++) uNew[Idx(0, j)] = val;
                return;
            }
            if (i == _nx - 1 && _rightBC.Type == BoundaryConditionType.Dirichlet)
            {
                double val = _rightBC.Evaluate(tNew);
                for (int j = 0; j < _ny; j++) uNew[Idx(_nx - 1, j)] = val;
                return;
            }

            var a   = new double[_ny];
            var b   = new double[_ny];
            var c   = new double[_ny];
            var rhs = new double[_ny];

            // Interior y-nodes: LHS is y-implicit; RHS is the x-explicit part.
            for (int j = 1; j < _ny - 1; j++)
            {
                a[j]   = -ry;
                b[j]   = 1.0 + 2.0 * ry;
                c[j]   = -ry;
                rhs[j] = XExplicitRhs(i, j, rx, uHalf, tHalf);
            }

            // y-boundary nodes: set LHS and overwrite RHS.
            ApplyYBottomBC(i, tHalf, tNew, ry, rx, uHalf, a, b, c, rhs);
            ApplyYTopBC(i, tHalf, tNew, ry, rx, uHalf, a, b, c, rhs);

            var col = new double[_ny];
            SolveTridiagonal(a, b, c, rhs, col);
            for (int j = 0; j < _ny; j++)
                uNew[Idx(i, j)] = col[j];
        });
    }

    // ── Explicit right-hand sides ──────────────────────────────────────────────

    /// <summary>
    /// y-direction explicit contribution at node (i, j) for half-step 1,
    /// using the solution at <paramref name="tOld"/>.
    /// Ghost nodes at y-boundaries are incorporated using the BC at tOld.
    /// </summary>
    private double YExplicitRhs(int i, int j, double ry, double tOld)
    {
        double uC = _u[Idx(i, j)];

        // South (j−1) neighbour or ghost node.
        double uS;
        if (j > 0)
            uS = _u[Idx(i, j - 1)];
        else
            uS = YSouthGhost(i, tOld);

        // North (j+1) neighbour or ghost node.
        double uN;
        if (j < _ny - 1)
            uN = _u[Idx(i, j + 1)];
        else
            uN = YNorthGhost(i, tOld);

        return ry * uS + (1.0 - 2.0 * ry) * uC + ry * uN;
    }

    /// <summary>
    /// x-direction explicit contribution at node (i, j) for half-step 2,
    /// using u^{1/2} (uHalf) evaluated at <paramref name="tHalf"/>.
    /// Ghost nodes at x-boundaries are incorporated using the BC at tHalf.
    /// </summary>
    private double XExplicitRhs(int i, int j, double rx, double[] uHalf, double tHalf)
    {
        double uC = uHalf[Idx(i, j)];

        // West (i−1) neighbour or ghost node.
        double uW;
        if (i > 0)
            uW = uHalf[Idx(i - 1, j)];
        else
            uW = XWestGhost(j, uHalf, tHalf);

        // East (i+1) neighbour or ghost node.
        double uE;
        if (i < _nx - 1)
            uE = uHalf[Idx(i + 1, j)];
        else
            uE = XEastGhost(j, uHalf, tHalf);

        return rx * uW + (1.0 - 2.0 * rx) * uC + rx * uE;
    }

    // ── Ghost-node helpers ────────────────────────────────────────────────────

    // For Neumann at y=0 (bottom, outward normal −y): ∂u/∂n = −∂u/∂y = q
    //   ⟹ (u_{i,1} − u_{i,−1})/(2·dy) = −q  ⟹  ghost = u_{i,1} + 2·dy·q
    private double YSouthGhost(int i, double t)
    {
        if (_bottomBC.Type == BoundaryConditionType.Dirichlet)
            return _bottomBC.Evaluate(t);   // treat as if mirrored at Dirichlet value
        return _u[Idx(i, 1)] + 2.0 * _dy * _bottomBC.Evaluate(t);
    }

    // For Neumann at y=Ly (top, outward normal +y): ∂u/∂n = ∂u/∂y = q
    //   ⟹ (u_{i,ny} − u_{i,ny−2})/(2·dy) = q  ⟹  ghost = u_{i,ny−2} + 2·dy·q
    private double YNorthGhost(int i, double t)
    {
        if (_topBC.Type == BoundaryConditionType.Dirichlet)
            return _topBC.Evaluate(t);
        return _u[Idx(i, _ny - 2)] + 2.0 * _dy * _topBC.Evaluate(t);
    }

    // For Neumann at x=0 (left, outward normal −x): ∂u/∂n = −∂u/∂x = q
    //   ⟹ (uH_{1,j} − uH_{−1,j})/(2·dx) = −q  ⟹  ghost = uH_{1,j} + 2·dx·q
    private double XWestGhost(int j, double[] uHalf, double t)
    {
        if (_leftBC.Type == BoundaryConditionType.Dirichlet)
            return _leftBC.Evaluate(t);
        return uHalf[Idx(1, j)] + 2.0 * _dx * _leftBC.Evaluate(t);
    }

    // For Neumann at x=Lx (right, outward normal +x): ∂u/∂n = ∂u/∂x = q
    //   ⟹ ghost = uH_{nx−2,j} + 2·dx·q
    private double XEastGhost(int j, double[] uHalf, double t)
    {
        if (_rightBC.Type == BoundaryConditionType.Dirichlet)
            return _rightBC.Evaluate(t);
        return uHalf[Idx(_nx - 2, j)] + 2.0 * _dx * _rightBC.Evaluate(t);
    }

    // ── Boundary condition helpers for x-sweeps (half-step 1) ─────────────────

    private void ApplyXLeftBC(int j, double tOld, double tHalf, double rx, double ry,
                              double[] a, double[] b, double[] c, double[] rhs)
    {
        // Left boundary (i=0), x-implicit half-step.
        // For Neumann: ghost u_{−1,j}^{1/2} = u_{1,j}^{1/2} + 2·dx·q^{1/2}
        // After substitution in the LHS:
        //   b[0] = 1+2rx, c[0] = −2rx
        //   rhs[0] = YExplicitRhs(0, j, ry) + 2·dx·rx·q^{1/2}
        switch (_leftBC.Type)
        {
            case BoundaryConditionType.Dirichlet:
                a[0]   = 0.0;
                b[0]   = 1.0;
                c[0]   = 0.0;
                rhs[0] = _leftBC.Evaluate(tHalf);
                break;

            case BoundaryConditionType.Neumann:
                a[0]   = 0.0;
                b[0]   = 1.0 + 2.0 * rx;
                c[0]   = -2.0 * rx;
                rhs[0] = YExplicitRhs(0, j, ry, tOld)
                       + 2.0 * _dx * rx * _leftBC.Evaluate(tHalf);
                break;

            default:
                throw new NotSupportedException(
                    $"Boundary condition type {_leftBC.Type} is not supported in DiffusionSolver2D.");
        }
    }

    private void ApplyXRightBC(int j, double tOld, double tHalf, double rx, double ry,
                               double[] a, double[] b, double[] c, double[] rhs)
    {
        int last = _nx - 1;
        // Right boundary (i=nx−1), x-implicit half-step.
        // For Neumann: ghost u_{nx,j}^{1/2} = u_{nx−2,j}^{1/2} + 2·dx·q^{1/2}
        // After substitution: a[last]=−2rx, b[last]=1+2rx
        //   rhs[last] = YExplicitRhs(last, j, ry) + 2·dx·rx·q^{1/2}
        switch (_rightBC.Type)
        {
            case BoundaryConditionType.Dirichlet:
                a[last]   = 0.0;
                b[last]   = 1.0;
                c[last]   = 0.0;
                rhs[last] = _rightBC.Evaluate(tHalf);
                break;

            case BoundaryConditionType.Neumann:
                a[last]   = -2.0 * rx;
                b[last]   = 1.0 + 2.0 * rx;
                c[last]   = 0.0;
                rhs[last] = YExplicitRhs(last, j, ry, tOld)
                          + 2.0 * _dx * rx * _rightBC.Evaluate(tHalf);
                break;

            default:
                throw new NotSupportedException(
                    $"Boundary condition type {_rightBC.Type} is not supported in DiffusionSolver2D.");
        }
    }

    // ── Boundary condition helpers for y-sweeps (half-step 2) ─────────────────

    private void ApplyYBottomBC(int i, double tHalf, double tNew, double ry, double rx,
                                double[] uHalf,
                                double[] a, double[] b, double[] c, double[] rhs)
    {
        // Bottom boundary (j=0), y-implicit half-step.
        // For Neumann (∂u/∂n=−∂u/∂y=q at y=0):
        //   ghost u_{i,−1}^{n+1} = u_{i,1}^{n+1} + 2·dy·q^{n+1}
        //   After substitution: b[0]=1+2ry, c[0]=−2ry
        //   rhs[0] = XExplicitRhs(i, 0) + 2·dy·ry·q^{n+1}
        switch (_bottomBC.Type)
        {
            case BoundaryConditionType.Dirichlet:
                a[0]   = 0.0;
                b[0]   = 1.0;
                c[0]   = 0.0;
                rhs[0] = _bottomBC.Evaluate(tNew);
                break;

            case BoundaryConditionType.Neumann:
                a[0]   = 0.0;
                b[0]   = 1.0 + 2.0 * ry;
                c[0]   = -2.0 * ry;
                rhs[0] = XExplicitRhs(i, 0, rx, uHalf, tHalf)
                       + 2.0 * _dy * ry * _bottomBC.Evaluate(tNew);
                break;

            default:
                throw new NotSupportedException(
                    $"Boundary condition type {_bottomBC.Type} is not supported in DiffusionSolver2D.");
        }
    }

    private void ApplyYTopBC(int i, double tHalf, double tNew, double ry, double rx,
                             double[] uHalf,
                             double[] a, double[] b, double[] c, double[] rhs)
    {
        int last = _ny - 1;
        // Top boundary (j=ny−1), y-implicit half-step.
        // For Neumann (∂u/∂n=∂u/∂y=q at y=Ly):
        //   ghost u_{i,ny}^{n+1} = u_{i,ny−2}^{n+1} + 2·dy·q^{n+1}
        //   After substitution: a[last]=−2ry, b[last]=1+2ry
        //   rhs[last] = XExplicitRhs(i, last) + 2·dy·ry·q^{n+1}
        switch (_topBC.Type)
        {
            case BoundaryConditionType.Dirichlet:
                a[last]   = 0.0;
                b[last]   = 1.0;
                c[last]   = 0.0;
                rhs[last] = _topBC.Evaluate(tNew);
                break;

            case BoundaryConditionType.Neumann:
                a[last]   = -2.0 * ry;
                b[last]   = 1.0 + 2.0 * ry;
                c[last]   = 0.0;
                rhs[last] = XExplicitRhs(i, last, rx, uHalf, tHalf)
                          + 2.0 * _dy * ry * _topBC.Evaluate(tNew);
                break;

            default:
                throw new NotSupportedException(
                    $"Boundary condition type {_topBC.Type} is not supported in DiffusionSolver2D.");
        }
    }

    // ── Thomas (tridiagonal) algorithm ────────────────────────────────────────

    private static void SolveTridiagonal(
        double[] a, double[] b, double[] c, double[] d, double[] x)
    {
        int n = a.Length;

        var cMod = (double[])c.Clone();
        var dMod = (double[])d.Clone();

        for (int i = 1; i < n; i++)
        {
            double w = a[i] / b[i - 1];
            b[i]    -= w * cMod[i - 1];
            dMod[i] -= w * dMod[i - 1];
        }

        x[n - 1] = dMod[n - 1] / b[n - 1];
        for (int i = n - 2; i >= 0; i--)
            x[i] = (dMod[i] - cMod[i] * x[i + 1]) / b[i];
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Row-major index: i is x, j is y.</summary>
    private int Idx(int i, int j) => i * _ny + j;

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

    private void ValidateBoundaryConditions()
    {
        if (_leftBC.Type   == BoundaryConditionType.Robin ||
            _rightBC.Type  == BoundaryConditionType.Robin ||
            _bottomBC.Type == BoundaryConditionType.Robin ||
            _topBC.Type    == BoundaryConditionType.Robin)
        {
            throw new NotSupportedException(
                "Robin boundary conditions are not supported by DiffusionSolver2D.");
        }
    }
}
