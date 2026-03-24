namespace GCE.Numerics.Solvers;

/// <summary>
/// Solves the 2-D Poisson equation
///   ∇²u = f(x, y)
/// on a uniform rectangular grid [0, Lx] × [0, Ly] using a
/// Gauss–Seidel successive over-relaxation (SOR) iterative method.
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
/// The Gauss–Seidel update of the 5-point stencil at each non-Dirichlet node
/// uses the most recently computed neighbours, giving faster convergence than
/// Jacobi.  The SOR relaxation factor ω ∈ [1, 2] accelerates convergence
/// further: ω = 1 is standard Gauss–Seidel; values closer to 2 apply
/// successive over-relaxation.  The optimal ω for a square grid of n interior
/// nodes per side is approximately 2 / (1 + sin(π / (n + 1))).
/// </para>
/// <para>
/// All three boundary condition types — Dirichlet, Neumann, and Robin — are
/// supported on each side of the rectangle.  Left and right BCs own the full
/// column including corners; bottom and top BCs own only the interior x-nodes
/// (i = 1 … nx − 2).  Conflicting corner values (e.g., different Dirichlet
/// values on two adjacent sides) are resolved in favour of the left / right BC.
/// </para>
/// <para>
/// The solver is a steady-state solver; <see cref="PdeSolverOptions.MaxIterations"/>
/// controls the iteration budget and <see cref="PdeSolverOptions.Tolerance"/>
/// is the L∞ change per sweep that signals convergence.
/// <see cref="PdeSolverOptions.MaxTimeSteps"/> is unused.
/// </para>
/// </remarks>
public sealed class PoissonSolver2D : IPDESolver
{
    // ── Grid ──────────────────────────────────────────────────────────────────

    private readonly int    _nx;
    private readonly int    _ny;
    private readonly double _dx;
    private readonly double _dy;
    private readonly double _dx2; // Δx²
    private readonly double _dy2; // Δy²

    // ── Boundary conditions ───────────────────────────────────────────────────

    private readonly IBoundaryCondition _leftBC;   // x = 0,  owns full column
    private readonly IBoundaryCondition _rightBC;  // x = Lx, owns full column
    private readonly IBoundaryCondition _bottomBC; // y = 0,  interior x only
    private readonly IBoundaryCondition _topBC;    // y = Ly, interior x only

    // ── SOR parameter ─────────────────────────────────────────────────────────

    private readonly double _omega;

    // ── State (row-major: _u[i*_ny + j]) ─────────────────────────────────────

    private readonly double[] _u;

    // ── Precomputed source term (same layout as _u) ───────────────────────────

    private readonly double[] _f;

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the Poisson solver.
    /// </summary>
    /// <param name="nx">Number of grid nodes in the x-direction (must be ≥ 2).</param>
    /// <param name="ny">Number of grid nodes in the y-direction (must be ≥ 2).</param>
    /// <param name="domainLengthX">Physical width Lx (must be &gt; 0).</param>
    /// <param name="domainLengthY">Physical height Ly (must be &gt; 0).</param>
    /// <param name="leftBC">Boundary condition on the x = 0 face.  For Dirichlet conditions the BC is evaluated with the y-coordinate of each node as the argument, allowing spatially-varying prescribed values.</param>
    /// <param name="rightBC">Boundary condition on the x = Lx face.  Same coordinate convention as <paramref name="leftBC"/>.</param>
    /// <param name="bottomBC">Boundary condition on the y = 0 face.  For Dirichlet conditions the BC is evaluated with the x-coordinate of each node as the argument.</param>
    /// <param name="topBC">Boundary condition on the y = Ly face.  Same coordinate convention as <paramref name="bottomBC"/>.</param>
    /// <param name="source">
    /// The source function f(x, y) evaluated at each grid node at construction time.
    /// Pass <c>(_, _) =&gt; 0.0</c> to obtain the Laplace equation.
    /// </param>
    /// <param name="initialGuess">
    /// Optional flat array (row-major, length nx·ny) used as the initial guess for
    /// the iterative solver.  When <see langword="null"/> the solution is initialised
    /// to zero everywhere (Dirichlet nodes are overwritten immediately).
    /// </param>
    /// <param name="omega">
    /// SOR relaxation parameter ω ∈ [1, 2].  ω = 1 gives standard Gauss–Seidel.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any dimension, domain length, or ω is out of range.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any boundary condition or <paramref name="source"/> is
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="initialGuess"/> has the wrong length.
    /// </exception>
    public PoissonSolver2D(
        int                           nx,
        int                           ny,
        double                        domainLengthX,
        double                        domainLengthY,
        IBoundaryCondition            leftBC,
        IBoundaryCondition            rightBC,
        IBoundaryCondition            bottomBC,
        IBoundaryCondition            topBC,
        Func<double, double, double>  source,
        double[]?                     initialGuess = null,
        double                        omega        = 1.0)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(nx, 2, nameof(nx));
        ArgumentOutOfRangeException.ThrowIfLessThan(ny, 2, nameof(ny));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(domainLengthX, nameof(domainLengthX));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(domainLengthY, nameof(domainLengthY));
        ArgumentNullException.ThrowIfNull(leftBC,   nameof(leftBC));
        ArgumentNullException.ThrowIfNull(rightBC,  nameof(rightBC));
        ArgumentNullException.ThrowIfNull(bottomBC, nameof(bottomBC));
        ArgumentNullException.ThrowIfNull(topBC,    nameof(topBC));
        ArgumentNullException.ThrowIfNull(source,   nameof(source));
        if (omega is < 1.0 or > 2.0)
            throw new ArgumentOutOfRangeException(
                nameof(omega), "SOR parameter ω must satisfy 1 ≤ ω ≤ 2.");
        if (initialGuess is not null && initialGuess.Length != nx * ny)
            throw new ArgumentException(
                $"initialGuess must have length {nx * ny} (nx × ny).",
                nameof(initialGuess));

        _nx      = nx;
        _ny      = ny;
        _dx      = domainLengthX / (nx - 1.0);
        _dy      = domainLengthY / (ny - 1.0);
        _dx2     = _dx * _dx;
        _dy2     = _dy * _dy;
        _leftBC  = leftBC;
        _rightBC = rightBC;
        _bottomBC = bottomBC;
        _topBC   = topBC;
        _omega   = omega;

        _u = initialGuess is not null
            ? (double[])initialGuess.Clone()
            : new double[nx * ny];

        // Precompute source at every grid node (source is time-independent).
        _f = new double[nx * ny];
        for (int i = 0; i < nx; i++)
        {
            double x = i * _dx;
            for (int j = 0; j < ny; j++)
                _f[i * ny + j] = source(x, j * _dy);
        }

        // Impose Dirichlet values so the initial state is consistent.
        ApplyDirichletInit();
    }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the current nodal solution as a read-only span in row-major order
    /// (index = i·ny + j).
    /// </summary>
    public ReadOnlySpan<double> CurrentSolution => _u.AsSpan();

    // ── IPDESolver ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// Runs up to <see cref="PdeSolverOptions.MaxIterations"/> Gauss–Seidel SOR
    /// sweeps.  Convergence is declared when the per-sweep L∞ change falls below
    /// <see cref="PdeSolverOptions.Tolerance"/>.
    /// <para>
    /// <see cref="PdeSolverResult.Iterations"/> is the number of sweeps taken;
    /// <see cref="PdeSolverResult.Residual"/> is the L∞ change in the last sweep.
    /// </para>
    /// </remarks>
    public PdeSolverResult Solve(PdeSolverOptions? options = null)
    {
        var    opts         = options ?? new PdeSolverOptions();
        double lastResidual = 0.0;
        int    iterCount    = 0;

        for (int iter = 0; iter < opts.MaxIterations; iter++)
        {
            lastResidual = 0.0;

            // Left column (i = 0, all j, including corners)
            SweepColumnLeft(ref lastResidual);

            // Interior nodes (i = 1 … nx−2, j = 1 … ny−2)
            for (int i = 1; i < _nx - 1; i++)
                for (int j = 1; j < _ny - 1; j++)
                    SweepInterior(i, j, ref lastResidual);

            // Right column (i = nx−1, all j, including corners)
            SweepColumnRight(ref lastResidual);

            // Bottom row (j = 0, i = 1 … nx−2)
            SweepRowBottom(ref lastResidual);

            // Top row (j = ny−1, i = 1 … nx−2)
            SweepRowTop(ref lastResidual);

            iterCount++;

            if (lastResidual < opts.Tolerance)
                break;
        }

        return new PdeSolverResult
        {
            Solution   = (double[])_u.Clone(),
            Converged  = lastResidual < opts.Tolerance,
            Iterations = iterCount,
            Residual   = lastResidual,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private int Idx(int i, int j) => i * _ny + j;

    /// <summary>
    /// Sets Dirichlet boundary values in the initial solution.
    /// Left/right BCs own the full column; bottom/top BCs own interior x-nodes.
    /// <para>
    /// The boundary condition is evaluated with the spatial coordinate along the
    /// edge as its argument (y for left/right, x for bottom/top).  For uniform
    /// Dirichlet values this makes no difference; for spatially-varying conditions
    /// the lambda receives the position of each node along the edge.
    /// </para>
    /// </summary>
    private void ApplyDirichletInit()
    {
        // Left column (i=0): spatial coord along edge = y_j
        if (_leftBC.Type == BoundaryConditionType.Dirichlet)
        {
            for (int j = 0; j < _ny; j++)
                _u[Idx(0, j)] = _leftBC.Evaluate(j * _dy);
        }

        // Right column (i=nx-1): spatial coord along edge = y_j
        if (_rightBC.Type == BoundaryConditionType.Dirichlet)
        {
            for (int j = 0; j < _ny; j++)
                _u[Idx(_nx - 1, j)] = _rightBC.Evaluate(j * _dy);
        }

        // Bottom row (j=0): spatial coord along edge = x_i, interior x only
        if (_bottomBC.Type == BoundaryConditionType.Dirichlet)
        {
            for (int i = 1; i < _nx - 1; i++)
                _u[Idx(i, 0)] = _bottomBC.Evaluate(i * _dx);
        }

        // Top row (j=ny-1): spatial coord along edge = x_i, interior x only
        if (_topBC.Type == BoundaryConditionType.Dirichlet)
        {
            for (int i = 1; i < _nx - 1; i++)
                _u[Idx(i, _ny - 1)] = _topBC.Evaluate(i * _dx);
        }
    }

    // ── GS SOR application ────────────────────────────────────────────────────

    /// <summary>
    /// Applies the SOR update to node (i, j) and accumulates the L∞ residual.
    /// </summary>
    private void ApplySOR(int i, int j, double uGS, ref double residual)
    {
        double old  = _u[Idx(i, j)];
        double uNew = (1.0 - _omega) * old + _omega * uGS;
        _u[Idx(i, j)] = uNew;
        double change = Math.Abs(uNew - old);
        if (change > residual) residual = change;
    }

    // ── Interior update ───────────────────────────────────────────────────────

    private void SweepInterior(int i, int j, ref double residual)
    {
        double uGS = GaussSeidelFormula(
            uW: _u[Idx(i - 1, j)],
            uE: _u[Idx(i + 1, j)],
            uS: _u[Idx(i, j - 1)],
            uN: _u[Idx(i, j + 1)],
            f:  _f[Idx(i, j)]);
        ApplySOR(i, j, uGS, ref residual);
    }

    /// <summary>
    /// Standard 5-point Gauss–Seidel formula for the Poisson equation:
    ///   u_{i,j} = ( Δy²(uW+uE) + Δx²(uS+uN) − Δx²Δy²·f ) / (2(Δx²+Δy²))
    /// </summary>
    private double GaussSeidelFormula(
        double uW, double uE, double uS, double uN, double f) =>
        (_dy2 * (uW + uE) + _dx2 * (uS + uN) - _dx2 * _dy2 * f)
        / (2.0 * (_dx2 + _dy2));

    // ── Left column (i = 0) ───────────────────────────────────────────────────

    private void SweepColumnLeft(ref double residual)
    {
        if (_leftBC.Type == BoundaryConditionType.Dirichlet)
            return; // Fixed by Dirichlet; no iteration update.

        for (int j = 0; j < _ny; j++)
        {
            double coord = j * _dy; // spatial position along left edge
            double uE = _u[Idx(1, j)];
            double uS = j > 0         ? _u[Idx(0, j - 1)] : EdgeGhost(_bottomBC, _u[Idx(0, 1)],     _dy, _u[Idx(0, 0)],        0.0);
            double uN = j < _ny - 1   ? _u[Idx(0, j + 1)] : EdgeGhost(_topBC,    _u[Idx(0, _ny-2)], _dy, _u[Idx(0, _ny-1)], 0.0);

            double uGS = LeftBoundaryGS(j, coord, uE, uS, uN);
            ApplySOR(0, j, uGS, ref residual);
        }
    }

    /// <summary>
    /// Computes the Gauss–Seidel update for a node on the left boundary (i = 0)
    /// with a Neumann or Robin left BC.
    /// <paramref name="coord"/> is the spatial position y_j along the left edge.
    /// </summary>
    private double LeftBoundaryGS(int j, double coord, double uE, double uS, double uN)
    {
        double f = _f[Idx(0, j)];

        if (_leftBC.Type == BoundaryConditionType.Neumann)
        {
            // Outward normal at x = 0 is −x; ∂u/∂n = −∂u/∂x = q
            // Ghost: u[−1,j] = u[1,j] + 2·Δx·q
            // ⟹ u[0,j] = (Δy²·(2·uE + 2·Δx·q) + Δx²·(uS+uN) − Δx²Δy²·f)
            //              / (2·(Δx²+Δy²))
            double q = _leftBC.Evaluate(coord);
            return (_dy2 * (2.0 * uE + 2.0 * _dx * q) + _dx2 * (uS + uN) - _dx2 * _dy2 * f)
                   / (2.0 * (_dx2 + _dy2));
        }
        else // Robin
        {
            var    robin = (RobinBC)_leftBC;
            double alpha = robin.Alpha;
            double beta  = robin.Beta;
            double gamma = robin.Evaluate(coord);

            if (Math.Abs(beta) < 1e-15)
                return Math.Abs(alpha) < 1e-15 ? 0.0 : gamma / alpha;

            // k = 2·Δx/β; denominator = 2·(Δx²+Δy²) + k·α·Δy²
            double k     = 2.0 * _dx / beta;
            double denom = 2.0 * (_dx2 + _dy2) + k * alpha * _dy2;
            double rhs   = 2.0 * _dy2 * uE + k * gamma * _dy2
                         + _dx2 * (uS + uN) - _dx2 * _dy2 * f;
            return rhs / denom;
        }
    }

    // ── Right column (i = nx−1) ───────────────────────────────────────────────

    private void SweepColumnRight(ref double residual)
    {
        if (_rightBC.Type == BoundaryConditionType.Dirichlet)
            return;

        int last = _nx - 1;
        for (int j = 0; j < _ny; j++)
        {
            double coord = j * _dy; // spatial position along right edge
            double uW = _u[Idx(last - 1, j)];
            double uS = j > 0         ? _u[Idx(last, j - 1)] : EdgeGhost(_bottomBC, _u[Idx(last, 1)],     _dy, _u[Idx(last, 0)],        last * _dx);
            double uN = j < _ny - 1   ? _u[Idx(last, j + 1)] : EdgeGhost(_topBC,    _u[Idx(last, _ny-2)], _dy, _u[Idx(last, _ny-1)], last * _dx);

            double uGS = RightBoundaryGS(j, coord, uW, uS, uN);
            ApplySOR(last, j, uGS, ref residual);
        }
    }

    /// <summary>
    /// Computes the Gauss–Seidel update for a node on the right boundary (i = nx−1).
    /// Outward normal is +x; ghost symmetry is mirrored from the left formula.
    /// <paramref name="coord"/> is the spatial position y_j along the right edge.
    /// </summary>
    private double RightBoundaryGS(int j, double coord, double uW, double uS, double uN)
    {
        double f = _f[Idx(_nx - 1, j)];

        if (_rightBC.Type == BoundaryConditionType.Neumann)
        {
            // ∂u/∂n = q at x = Lx; ∂u/∂n = ∂u/∂x = q
            // Ghost: u[nx,j] = u[nx−2,j] + 2·Δx·q
            double q = _rightBC.Evaluate(coord);
            return (_dy2 * (2.0 * uW + 2.0 * _dx * q) + _dx2 * (uS + uN) - _dx2 * _dy2 * f)
                   / (2.0 * (_dx2 + _dy2));
        }
        else // Robin
        {
            var    robin = (RobinBC)_rightBC;
            double alpha = robin.Alpha;
            double beta  = robin.Beta;
            double gamma = robin.Evaluate(coord);

            if (Math.Abs(beta) < 1e-15)
                return Math.Abs(alpha) < 1e-15 ? 0.0 : gamma / alpha;

            double k     = 2.0 * _dx / beta;
            double denom = 2.0 * (_dx2 + _dy2) + k * alpha * _dy2;
            double rhs   = 2.0 * _dy2 * uW + k * gamma * _dy2
                         + _dx2 * (uS + uN) - _dx2 * _dy2 * f;
            return rhs / denom;
        }
    }

    // ── Bottom row (j = 0, i = 1 … nx−2) ─────────────────────────────────────

    private void SweepRowBottom(ref double residual)
    {
        if (_bottomBC.Type == BoundaryConditionType.Dirichlet)
            return;

        for (int i = 1; i < _nx - 1; i++)
        {
            double coord = i * _dx; // spatial position along bottom edge
            double uW = _u[Idx(i - 1, 0)];
            double uE = _u[Idx(i + 1, 0)];
            double uN = _u[Idx(i, 1)];

            double uGS = BottomBoundaryGS(i, coord, uW, uE, uN);
            ApplySOR(i, 0, uGS, ref residual);
        }
    }

    /// <summary>
    /// Computes the Gauss–Seidel update for a node on the bottom boundary (j = 0).
    /// Outward normal at y = 0 is −y; ∂u/∂n = −∂u/∂y.
    /// <paramref name="coord"/> is the spatial position x_i along the bottom edge.
    /// </summary>
    private double BottomBoundaryGS(int i, double coord, double uW, double uE, double uN)
    {
        double f = _f[Idx(i, 0)];

        if (_bottomBC.Type == BoundaryConditionType.Neumann)
        {
            // Ghost: u[i,−1] = u[i,1] + 2·Δy·q
            double q = _bottomBC.Evaluate(coord);
            return (_dy2 * (uW + uE) + _dx2 * (2.0 * uN + 2.0 * _dy * q) - _dx2 * _dy2 * f)
                   / (2.0 * (_dx2 + _dy2));
        }
        else // Robin
        {
            var    robin = (RobinBC)_bottomBC;
            double alpha = robin.Alpha;
            double beta  = robin.Beta;
            double gamma = robin.Evaluate(coord);

            if (Math.Abs(beta) < 1e-15)
                return Math.Abs(alpha) < 1e-15 ? 0.0 : gamma / alpha;

            // k = 2·Δy/β; denominator = 2·(Δx²+Δy²) + k·α·Δx²
            double k     = 2.0 * _dy / beta;
            double denom = 2.0 * (_dx2 + _dy2) + k * alpha * _dx2;
            double rhs   = _dy2 * (uW + uE) + 2.0 * _dx2 * uN
                         + k * gamma * _dx2 - _dx2 * _dy2 * f;
            return rhs / denom;
        }
    }

    // ── Top row (j = ny−1, i = 1 … nx−2) ─────────────────────────────────────

    private void SweepRowTop(ref double residual)
    {
        if (_topBC.Type == BoundaryConditionType.Dirichlet)
            return;

        int last = _ny - 1;
        for (int i = 1; i < _nx - 1; i++)
        {
            double coord = i * _dx; // spatial position along top edge
            double uW = _u[Idx(i - 1, last)];
            double uE = _u[Idx(i + 1, last)];
            double uS = _u[Idx(i, last - 1)];

            double uGS = TopBoundaryGS(i, coord, uW, uE, uS);
            ApplySOR(i, last, uGS, ref residual);
        }
    }

    /// <summary>
    /// Computes the Gauss–Seidel update for a node on the top boundary (j = ny−1).
    /// Outward normal at y = Ly is +y; ghost symmetry mirrors the bottom formula.
    /// <paramref name="coord"/> is the spatial position x_i along the top edge.
    /// </summary>
    private double TopBoundaryGS(int i, double coord, double uW, double uE, double uS)
    {
        double f = _f[Idx(i, _ny - 1)];

        if (_topBC.Type == BoundaryConditionType.Neumann)
        {
            // Ghost: u[i,ny] = u[i,ny−2] + 2·Δy·q
            double q = _topBC.Evaluate(coord);
            return (_dy2 * (uW + uE) + _dx2 * (2.0 * uS + 2.0 * _dy * q) - _dx2 * _dy2 * f)
                   / (2.0 * (_dx2 + _dy2));
        }
        else // Robin
        {
            var    robin = (RobinBC)_topBC;
            double alpha = robin.Alpha;
            double beta  = robin.Beta;
            double gamma = robin.Evaluate(coord);

            if (Math.Abs(beta) < 1e-15)
                return Math.Abs(alpha) < 1e-15 ? 0.0 : gamma / alpha;

            double k     = 2.0 * _dy / beta;
            double denom = 2.0 * (_dx2 + _dy2) + k * alpha * _dx2;
            double rhs   = _dy2 * (uW + uE) + 2.0 * _dx2 * uS
                         + k * gamma * _dx2 - _dx2 * _dy2 * f;
            return rhs / denom;
        }
    }

    // ── Corner ghost helper ────────────────────────────────────────────────────

    /// <summary>
    /// Returns the ghost-node value for an edge BC when a column/row boundary
    /// node's perpendicular neighbour lies outside the domain.
    /// </summary>
    /// <param name="edgeBC">The BC on the perpendicular edge.</param>
    /// <param name="innerNode">The first interior node in the perpendicular direction.</param>
    /// <param name="step">Grid spacing in the perpendicular direction (Δx or Δy).</param>
    /// <param name="boundaryNode">Current value at the corner node.</param>
    /// <param name="coord">
    /// Spatial coordinate along the perpendicular edge (x for bottom/top,
    /// y for left/right) used when evaluating the BC.
    /// </param>
    /// <returns>Ghost node value.</returns>
    private static double EdgeGhost(
        IBoundaryCondition edgeBC,
        double             innerNode,
        double             step,
        double             boundaryNode,
        double             coord)
    {
        return edgeBC.Type switch
        {
            BoundaryConditionType.Dirichlet => edgeBC.Evaluate(coord),
            BoundaryConditionType.Neumann   => innerNode + 2.0 * step * edgeBC.Evaluate(coord),
            BoundaryConditionType.Robin     => RobinGhost((RobinBC)edgeBC, innerNode, step, boundaryNode, coord),
            _                               => edgeBC.Evaluate(coord),
        };
    }

    private static double RobinGhost(
        RobinBC robin, double innerNode, double step, double boundaryNode, double coord)
    {
        double beta = robin.Beta;
        if (Math.Abs(beta) < 1e-15)
            return Math.Abs(robin.Alpha) < 1e-15
                ? 0.0
                : robin.Evaluate(coord) / robin.Alpha;
        return innerNode
             + 2.0 * step * (robin.Evaluate(coord) - robin.Alpha * boundaryNode) / beta;
    }
}
