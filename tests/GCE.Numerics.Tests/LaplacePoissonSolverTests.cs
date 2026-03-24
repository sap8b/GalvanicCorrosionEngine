using GCE.Numerics.Solvers;

namespace GCE.Numerics.Tests;

// ════════════════════════════════════════════════════════════════════════════
// PoissonSolver2D – construction tests
// ════════════════════════════════════════════════════════════════════════════

public class PoissonSolver2DConstructionTests
{
    [Fact]
    public void Constructor_ValidArguments_DoesNotThrow()
    {
        var solver = MakeSolver();
        Assert.NotNull(solver);
    }

    [Fact]
    public void Constructor_WithInitialGuess_DoesNotThrow()
    {
        int n = 5;
        var ig = new double[n * n];
        var solver = new PoissonSolver2D(
            n, n, 1.0, 1.0,
            new DirichletBC(0.0), new DirichletBC(1.0),
            new DirichletBC(0.0), new DirichletBC(0.0),
            (_, _) => 0.0,
            initialGuess: ig);
        Assert.NotNull(solver);
    }

    [Fact]
    public void Constructor_WithSorOmega_DoesNotThrow()
    {
        var solver = new PoissonSolver2D(
            5, 5, 1.0, 1.0,
            new DirichletBC(0.0), new DirichletBC(1.0),
            new DirichletBC(0.0), new DirichletBC(0.0),
            (_, _) => 0.0,
            omega: 1.5);
        Assert.NotNull(solver);
    }

    [Fact]
    public void Constructor_NxLessThan2_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PoissonSolver2D(1, 5, 1.0, 1.0,
                new DirichletBC(0.0), new DirichletBC(0.0),
                new DirichletBC(0.0), new DirichletBC(0.0),
                (_, _) => 0.0));
    }

    [Fact]
    public void Constructor_NyLessThan2_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PoissonSolver2D(5, 1, 1.0, 1.0,
                new DirichletBC(0.0), new DirichletBC(0.0),
                new DirichletBC(0.0), new DirichletBC(0.0),
                (_, _) => 0.0));
    }

    [Fact]
    public void Constructor_NegativeDomainLengthX_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PoissonSolver2D(5, 5, -1.0, 1.0,
                new DirichletBC(0.0), new DirichletBC(0.0),
                new DirichletBC(0.0), new DirichletBC(0.0),
                (_, _) => 0.0));
    }

    [Fact]
    public void Constructor_NegativeDomainLengthY_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PoissonSolver2D(5, 5, 1.0, -1.0,
                new DirichletBC(0.0), new DirichletBC(0.0),
                new DirichletBC(0.0), new DirichletBC(0.0),
                (_, _) => 0.0));
    }

    [Fact]
    public void Constructor_NullLeftBC_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PoissonSolver2D(5, 5, 1.0, 1.0,
                null!,
                new DirichletBC(0.0), new DirichletBC(0.0), new DirichletBC(0.0),
                (_, _) => 0.0));
    }

    [Fact]
    public void Constructor_NullRightBC_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PoissonSolver2D(5, 5, 1.0, 1.0,
                new DirichletBC(0.0), null!,
                new DirichletBC(0.0), new DirichletBC(0.0),
                (_, _) => 0.0));
    }

    [Fact]
    public void Constructor_NullBottomBC_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PoissonSolver2D(5, 5, 1.0, 1.0,
                new DirichletBC(0.0), new DirichletBC(0.0), null!, new DirichletBC(0.0),
                (_, _) => 0.0));
    }

    [Fact]
    public void Constructor_NullTopBC_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PoissonSolver2D(5, 5, 1.0, 1.0,
                new DirichletBC(0.0), new DirichletBC(0.0), new DirichletBC(0.0), null!,
                (_, _) => 0.0));
    }

    [Fact]
    public void Constructor_NullSource_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PoissonSolver2D(5, 5, 1.0, 1.0,
                new DirichletBC(0.0), new DirichletBC(0.0),
                new DirichletBC(0.0), new DirichletBC(0.0),
                null!));
    }

    [Fact]
    public void Constructor_OmegaLessThan1_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PoissonSolver2D(5, 5, 1.0, 1.0,
                new DirichletBC(0.0), new DirichletBC(0.0),
                new DirichletBC(0.0), new DirichletBC(0.0),
                (_, _) => 0.0,
                omega: 0.5));
    }

    [Fact]
    public void Constructor_OmegaGreaterThan2_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PoissonSolver2D(5, 5, 1.0, 1.0,
                new DirichletBC(0.0), new DirichletBC(0.0),
                new DirichletBC(0.0), new DirichletBC(0.0),
                (_, _) => 0.0,
                omega: 2.5));
    }

    [Fact]
    public void Constructor_WrongInitialGuessLength_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new PoissonSolver2D(5, 5, 1.0, 1.0,
                new DirichletBC(0.0), new DirichletBC(0.0),
                new DirichletBC(0.0), new DirichletBC(0.0),
                (_, _) => 0.0,
                initialGuess: new double[10])); // wrong: 10 ≠ 25
    }

    private static PoissonSolver2D MakeSolver(int nx = 5, int ny = 5)
        => new(nx, ny, 1.0, 1.0,
               new DirichletBC(0.0), new DirichletBC(1.0),
               new DirichletBC(0.0), new DirichletBC(0.0),
               (_, _) => 0.0);
}

// ════════════════════════════════════════════════════════════════════════════
// LaplaceSolver2D – construction tests
// ════════════════════════════════════════════════════════════════════════════

public class LaplaceSolver2DConstructionTests
{
    [Fact]
    public void Constructor_ValidArguments_DoesNotThrow()
    {
        var solver = new LaplaceSolver2D(
            5, 5, 1.0, 1.0,
            new DirichletBC(0.0), new DirichletBC(1.0),
            new DirichletBC(0.0), new DirichletBC(0.0));
        Assert.NotNull(solver);
    }

    [Fact]
    public void Constructor_NxLessThan2_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LaplaceSolver2D(1, 5, 1.0, 1.0,
                new DirichletBC(0.0), new DirichletBC(0.0),
                new DirichletBC(0.0), new DirichletBC(0.0)));
    }

    [Fact]
    public void Constructor_NullLeftBC_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LaplaceSolver2D(5, 5, 1.0, 1.0,
                null!,
                new DirichletBC(0.0), new DirichletBC(0.0), new DirichletBC(0.0)));
    }

    [Fact]
    public void Constructor_WithInitialGuess_DoesNotThrow()
    {
        var ig = new double[25];
        var solver = new LaplaceSolver2D(
            5, 5, 1.0, 1.0,
            new DirichletBC(0.0), new DirichletBC(1.0),
            new DirichletBC(0.0), new DirichletBC(0.0),
            initialGuess: ig);
        Assert.NotNull(solver);
    }

    [Fact]
    public void Constructor_InvalidOmega_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LaplaceSolver2D(5, 5, 1.0, 1.0,
                new DirichletBC(0.0), new DirichletBC(0.0),
                new DirichletBC(0.0), new DirichletBC(0.0),
                omega: 2.5));
    }
}

// ════════════════════════════════════════════════════════════════════════════
// PoissonSolver2D – IPDESolver interface contract
// ════════════════════════════════════════════════════════════════════════════

public class PoissonSolver2DInterfaceTests
{
    [Fact]
    public void Solve_ImplementsIPDESolver()
    {
        IPDESolver solver = MakeSolver();
        Assert.NotNull(solver);
    }

    [Fact]
    public void Solve_ReturnsResultWithCorrectSolutionLength()
    {
        const int nx = 6, ny = 8;
        IPDESolver solver = MakeSolver(nx: nx, ny: ny);
        var result = solver.Solve();

        Assert.Equal(nx * ny, result.Solution.Length);
    }

    [Fact]
    public void Solve_NullOptions_UsesDefaults()
    {
        IPDESolver solver = MakeSolver();
        var result = solver.Solve(null);

        Assert.NotNull(result.Solution);
        Assert.True(result.Iterations > 0);
    }

    [Fact]
    public void Solve_WithCustomMaxIterations_RespectsLimit()
    {
        // Force early exit at exactly 3 iterations using tight tolerance.
        IPDESolver solver = MakeSolverNontrivial();
        var opts   = new PdeSolverOptions { MaxIterations = 3, Tolerance = 1e-20 };
        var result = solver.Solve(opts);

        Assert.Equal(3, result.Iterations);
    }

    [Fact]
    public void Solve_Converged_FlagSetWhenResidualBelowTolerance()
    {
        IPDESolver solver = MakeSolver();
        var opts   = new PdeSolverOptions { MaxIterations = 5_000, Tolerance = 1e-10 };
        var result = solver.Solve(opts);

        Assert.True(result.Converged);
        Assert.True(result.Residual < 1e-10);
    }

    private static PoissonSolver2D MakeSolver(int nx = 5, int ny = 5)
        => new(nx, ny, 1.0, 1.0,
               new DirichletBC(0.0), new DirichletBC(1.0),
               new DirichletBC(0.0), new DirichletBC(0.0),
               (_, _) => 0.0);

    private static PoissonSolver2D MakeSolverNontrivial(int nx = 10, int ny = 10)
        => new(nx, ny, 1.0, 1.0,
               new DirichletBC(0.0), new DirichletBC(0.0),
               new DirichletBC(0.0), new DirichletBC(1.0),
               (x, y) => -2.0 * Math.PI * Math.PI * Math.Sin(Math.PI * x) * Math.Sin(Math.PI * y));
}

// ════════════════════════════════════════════════════════════════════════════
// LaplaceSolver2D – IPDESolver interface contract
// ════════════════════════════════════════════════════════════════════════════

public class LaplaceSolver2DInterfaceTests
{
    [Fact]
    public void Solve_ImplementsIPDESolver()
    {
        IPDESolver solver = MakeSolver();
        Assert.NotNull(solver);
    }

    [Fact]
    public void Solve_ReturnsResultWithCorrectSolutionLength()
    {
        const int nx = 6, ny = 7;
        IPDESolver solver = new LaplaceSolver2D(
            nx, ny, 1.0, 1.0,
            new DirichletBC(0.0), new DirichletBC(1.0),
            new DirichletBC(0.0), new DirichletBC(0.0));
        var result = solver.Solve();

        Assert.Equal(nx * ny, result.Solution.Length);
    }

    [Fact]
    public void Solve_NullOptions_UsesDefaults()
    {
        IPDESolver solver = MakeSolver();
        var result = solver.Solve(null);

        Assert.NotNull(result.Solution);
    }

    [Fact]
    public void CurrentSolution_ReflectsLatestSolveResult()
    {
        var solver = new LaplaceSolver2D(
            5, 5, 1.0, 1.0,
            new DirichletBC(0.0), new DirichletBC(1.0),
            new DirichletBC(0.0), new DirichletBC(0.0));

        var result = solver.Solve(new PdeSolverOptions { MaxIterations = 100 });

        // CurrentSolution should match the returned Solution array.
        var current = solver.CurrentSolution;
        Assert.Equal(current.Length, result.Solution.Length);
        for (int k = 0; k < result.Solution.Length; k++)
            Assert.Equal(current[k], result.Solution[k]);
    }

    private static LaplaceSolver2D MakeSolver()
        => new(5, 5, 1.0, 1.0,
               new DirichletBC(0.0), new DirichletBC(1.0),
               new DirichletBC(0.0), new DirichletBC(0.0));
}

// ════════════════════════════════════════════════════════════════════════════
// LaplaceSolver2D – analytical solution tests
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Verifies <see cref="LaplaceSolver2D"/> against two known analytical solutions
/// of the 2-D Laplace equation on the unit square.
/// </summary>
public class LaplaceSolver2DAnalyticalTests
{
    // ── Test case 1: linear solution u = x ───────────────────────────────────
    //
    // ∇²u = 0  on [0,1]×[0,1]
    // BCs: left Dirichlet u=0, right Dirichlet u=1,
    //      bottom & top Neumann q=0 (no y-variation in exact solution)
    // Exact: u(x,y) = x

    [Fact]
    public void Solve_LinearSolution_MatchesExactSolution()
    {
        const int nx = 21, ny = 21;
        double    dx = 1.0 / (nx - 1);

        var solver = new LaplaceSolver2D(
            nx, ny, 1.0, 1.0,
            leftBC:   new DirichletBC(0.0),
            rightBC:  new DirichletBC(1.0),
            bottomBC: new NeumannBC(0.0),
            topBC:    new NeumannBC(0.0));

        var result = solver.Solve(new PdeSolverOptions
        {
            MaxIterations = 10_000,
            Tolerance     = 1e-12,
        });

        Assert.True(result.Converged,
            $"Solver did not converge: residual = {result.Residual}");

        // Check every node against the exact solution u = x.
        for (int i = 0; i < nx; i++)
        for (int j = 0; j < ny; j++)
        {
            double exact = i * dx;
            double got   = result.Solution[i * ny + j];
            Assert.Equal(exact, got, precision: 6);
        }
    }

    // ── Test case 2: sinusoidal-harmonic solution ─────────────────────────────
    //
    // ∇²u = 0  on [0,1]×[0,1]
    // BCs: u=0 on left, right, and bottom; u=sin(π·x) on top
    // Exact: u(x,y) = sin(π·x) · sinh(π·y) / sinh(π)
    // (Separation-of-variables solution, first Fourier mode)

    [Fact]
    public void Solve_SinusoidalDirichletTop_MatchesAnalyticalSolution()
    {
        const int    nx  = 31, ny = 31;
        const double L   = 1.0;
        double       dx  = L / (nx - 1);
        double       dy  = L / (ny - 1);

        static double Exact(double x, double y) =>
            Math.Sin(Math.PI * x) * Math.Sinh(Math.PI * y) / Math.Sinh(Math.PI);

        var solver = new LaplaceSolver2D(
            nx, ny, L, L,
            leftBC:   new DirichletBC(0.0),
            rightBC:  new DirichletBC(0.0),
            bottomBC: new DirichletBC(0.0),
            topBC:    new DirichletBC(x => Math.Sin(Math.PI * x)));

        // Warm-start with the known solution to ensure the BC function is
        // consistent (topBC is evaluated at x, not at (x,y); we must check
        // that the DirichletBC is set correctly).
        var result = solver.Solve(new PdeSolverOptions
        {
            MaxIterations = 20_000,
            Tolerance     = 1e-8,
        });

        Assert.True(result.Converged,
            $"Solver did not converge: residual = {result.Residual}");

        // Check a sample of interior nodes; allow 1 % relative tolerance for
        // the coarse 31×31 grid.
        for (int i = 1; i < nx - 1; i += 3)
        for (int j = 1; j < ny - 1; j += 3)
        {
            double x     = i * dx;
            double y     = j * dy;
            double exact = Exact(x, y);
            double got   = result.Solution[i * ny + j];
            double tol   = Math.Max(1e-3, 0.01 * Math.Abs(exact));
            Assert.True(Math.Abs(got - exact) < tol,
                $"Node ({i},{j}): exact={exact:G6}, got={got:G6}, diff={got-exact:G3}");
        }
    }
}

// ════════════════════════════════════════════════════════════════════════════
// PoissonSolver2D – analytical solution tests
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Verifies <see cref="PoissonSolver2D"/> against known analytical solutions of
/// the 2-D Poisson equation ∇²u = f on the unit square.
/// </summary>
public class PoissonSolver2DAnalyticalTests
{
    // ── Test case 1: sinusoidal manufactured solution ─────────────────────────
    //
    // Exact: u(x,y) = sin(π·x) · sin(π·y)
    // ∇²u = −2π² · sin(π·x) · sin(π·y) = f(x,y)
    // BCs: Dirichlet u = 0 on all four sides (since sin vanishes at 0 and 1)

    [Fact]
    public void Solve_SinusoidalManufacturedSolution_MatchesAnalytical()
    {
        const int    nx  = 31, ny = 31;
        const double L   = 1.0;
        double       dx  = L / (nx - 1);
        double       dy  = L / (ny - 1);

        static double Exact(double x, double y) =>
            Math.Sin(Math.PI * x) * Math.Sin(Math.PI * y);

        static double Source(double x, double y) =>
            -2.0 * Math.PI * Math.PI * Math.Sin(Math.PI * x) * Math.Sin(Math.PI * y);

        var solver = new PoissonSolver2D(
            nx, ny, L, L,
            leftBC:   new DirichletBC(0.0),
            rightBC:  new DirichletBC(0.0),
            bottomBC: new DirichletBC(0.0),
            topBC:    new DirichletBC(0.0),
            source:   Source);

        var result = solver.Solve(new PdeSolverOptions
        {
            MaxIterations = 20_000,
            Tolerance     = 1e-8,
        });

        Assert.True(result.Converged,
            $"Solver did not converge: residual = {result.Residual}");

        for (int i = 1; i < nx - 1; i += 3)
        for (int j = 1; j < ny - 1; j += 3)
        {
            double x     = i * dx;
            double y     = j * dy;
            double exact = Exact(x, y);
            double got   = result.Solution[i * ny + j];
            double tol   = Math.Max(1e-3, 0.01 * Math.Abs(exact));
            Assert.True(Math.Abs(got - exact) < tol,
                $"Node ({i},{j}): exact={exact:G6}, got={got:G6}, diff={got-exact:G3}");
        }
    }

    // ── Test case 2: polynomial manufactured solution ─────────────────────────
    //
    // Exact: u(x,y) = x·(1−x)·y·(1−y)
    // ∇²u = −2·y·(1−y) − 2·x·(1−x) = f(x,y)
    // BCs: Dirichlet u = 0 on all four sides

    [Fact]
    public void Solve_PolynomialManufacturedSolution_MatchesAnalytical()
    {
        const int    nx = 21, ny = 21;
        const double L  = 1.0;
        double       dx = L / (nx - 1);
        double       dy = L / (ny - 1);

        static double Exact(double x, double y)  => x * (1 - x) * y * (1 - y);
        static double Source(double x, double y) => -2.0 * y * (1 - y) - 2.0 * x * (1 - x);

        var solver = new PoissonSolver2D(
            nx, ny, L, L,
            leftBC:   new DirichletBC(0.0),
            rightBC:  new DirichletBC(0.0),
            bottomBC: new DirichletBC(0.0),
            topBC:    new DirichletBC(0.0),
            source:   Source);

        var result = solver.Solve(new PdeSolverOptions
        {
            MaxIterations = 10_000,
            Tolerance     = 1e-10,
        });

        Assert.True(result.Converged,
            $"Solver did not converge: residual = {result.Residual}");

        for (int i = 1; i < nx - 1; i++)
        for (int j = 1; j < ny - 1; j++)
        {
            double x     = i * dx;
            double y     = j * dy;
            double exact = Exact(x, y);
            double got   = result.Solution[i * ny + j];
            Assert.Equal(exact, got, precision: 4);
        }
    }

    // ── Test case 3: non-trivial source on non-square domain ─────────────────
    //
    // Exact: u(x,y) = sin(π·x/Lx) · sin(π·y/Ly)
    // ∇²u = −π²·(1/Lx² + 1/Ly²)·sin(π·x/Lx)·sin(π·y/Ly)
    // BCs: Dirichlet u = 0 on all four sides

    [Fact]
    public void Solve_NonSquareDomain_MatchesAnalyticalSolution()
    {
        const int    nx = 21, ny = 31;
        const double Lx = 1.0, Ly = 2.0;
        double       dx = Lx / (nx - 1);
        double       dy = Ly / (ny - 1);

        double Exact(double x, double y) =>
            Math.Sin(Math.PI * x / Lx) * Math.Sin(Math.PI * y / Ly);

        double Source(double x, double y) =>
            -Math.PI * Math.PI * (1.0 / (Lx * Lx) + 1.0 / (Ly * Ly))
            * Math.Sin(Math.PI * x / Lx) * Math.Sin(Math.PI * y / Ly);

        var solver = new PoissonSolver2D(
            nx, ny, Lx, Ly,
            leftBC:   new DirichletBC(0.0),
            rightBC:  new DirichletBC(0.0),
            bottomBC: new DirichletBC(0.0),
            topBC:    new DirichletBC(0.0),
            source:   Source);

        var result = solver.Solve(new PdeSolverOptions
        {
            MaxIterations = 20_000,
            Tolerance     = 1e-8,
        });

        Assert.True(result.Converged,
            $"Solver did not converge: residual = {result.Residual}");

        for (int i = 1; i < nx - 1; i += 3)
        for (int j = 1; j < ny - 1; j += 3)
        {
            double x     = i * dx;
            double y     = j * dy;
            double exact = Exact(x, y);
            double got   = result.Solution[i * ny + j];
            double tol   = Math.Max(1e-3, 0.01 * Math.Abs(exact));
            Assert.True(Math.Abs(got - exact) < tol,
                $"Node ({i},{j}): exact={exact:G6}, got={got:G6}, diff={got-exact:G3}");
        }
    }
}

// ════════════════════════════════════════════════════════════════════════════
// PoissonSolver2D – Neumann and Robin boundary condition tests
// ════════════════════════════════════════════════════════════════════════════

public class PoissonSolver2DNeumannRobinTests
{
    // ── Neumann: u = x with zero-flux top/bottom ──────────────────────────────
    //
    // Exact: u(x,y) = x   (linear in x, uniform in y)
    // ∇²u = 0;   f = 0
    // BCs: left Dirichlet u=0, right Dirichlet u=1,
    //      bottom & top Neumann ∂u/∂n = 0

    [Fact]
    public void Solve_NeumannTopBottom_ZeroFlux_LinearSolution()
    {
        const int nx = 21, ny = 21;
        double    dx = 1.0 / (nx - 1);

        var solver = new PoissonSolver2D(
            nx, ny, 1.0, 1.0,
            leftBC:   new DirichletBC(0.0),
            rightBC:  new DirichletBC(1.0),
            bottomBC: new NeumannBC(0.0),
            topBC:    new NeumannBC(0.0),
            source:   (_, _) => 0.0);

        var result = solver.Solve(new PdeSolverOptions
        {
            MaxIterations = 10_000,
            Tolerance     = 1e-12,
        });

        Assert.True(result.Converged);

        for (int i = 0; i < nx; i++)
        for (int j = 0; j < ny; j++)
        {
            double exact = i * dx;
            double got   = result.Solution[i * ny + j];
            Assert.Equal(exact, got, precision: 6);
        }
    }

    // ── Robin on all four sides (degenerate α≠0, β=0 ⟹ Dirichlet) ───────────

    [Fact]
    public void Solve_RobinBCDegenerateWithBetaZero_ActsAsDirichlet()
    {
        // Robin with β=0 degenerates to Dirichlet: u = γ/α on the boundary.
        // We use u = 0 on all faces (α=1, β=0, γ=0) and source = 0.
        // Expected solution: u = 0 everywhere.
        const int nx = 5, ny = 5;

        var bc = new RobinBC(alpha: 1.0, beta: 0.0, gamma: 0.0);
        var solver = new PoissonSolver2D(
            nx, ny, 1.0, 1.0,
            leftBC: bc, rightBC: bc, bottomBC: bc, topBC: bc,
            source: (_, _) => 0.0);

        var result = solver.Solve(new PdeSolverOptions
        {
            MaxIterations = 1_000,
            Tolerance     = 1e-12,
        });

        foreach (double v in result.Solution)
            Assert.Equal(0.0, v, precision: 10);
    }
}

// ════════════════════════════════════════════════════════════════════════════
// PoissonSolver2D – SOR acceleration test
// ════════════════════════════════════════════════════════════════════════════

public class PoissonSolver2DSorTests
{
    // Verify that ω > 1 requires fewer iterations to reach the same tolerance.

    [Fact]
    public void Solve_SorOmegaAbove1_ConvergesFasterThanGaussSeidel()
    {
        var opts = new PdeSolverOptions { MaxIterations = 10_000, Tolerance = 1e-6 };

        int iterGS  = SolveWithOmega(1.0,  opts);
        int iterSOR = SolveWithOmega(1.5,  opts);

        Assert.True(iterSOR < iterGS,
            $"SOR (ω=1.5, iters={iterSOR}) should converge in fewer sweeps than Gauss-Seidel (ω=1, iters={iterGS}).");
    }

    private static int SolveWithOmega(double omega, PdeSolverOptions opts)
    {
        const int nx = 21, ny = 21;
        var solver = new PoissonSolver2D(
            nx, ny, 1.0, 1.0,
            leftBC:   new DirichletBC(0.0),
            rightBC:  new DirichletBC(0.0),
            bottomBC: new DirichletBC(0.0),
            topBC:    new DirichletBC(x => Math.Sin(Math.PI * x)),
            source:   (_, _) => 0.0,
            omega:    omega);

        return solver.Solve(opts).Iterations;
    }
}

// ════════════════════════════════════════════════════════════════════════════
// LaplaceSolver2D acts as Poisson with zero source
// ════════════════════════════════════════════════════════════════════════════

public class LaplaceSolver2DAsPoissonTests
{
    [Fact]
    public void LaplaceSolver_ProducesSameResultAsPoissonWithZeroSource()
    {
        const int nx = 11, ny = 11;
        IBoundaryCondition left   = new DirichletBC(0.0);
        IBoundaryCondition right  = new DirichletBC(1.0);
        IBoundaryCondition bottom = new NeumannBC(0.0);
        IBoundaryCondition top    = new NeumannBC(0.0);

        var opts = new PdeSolverOptions { MaxIterations = 5_000, Tolerance = 1e-12 };

        var laplace = new LaplaceSolver2D(nx, ny, 1.0, 1.0, left, right, bottom, top);
        var poisson = new PoissonSolver2D(nx, ny, 1.0, 1.0, left, right, bottom, top,
                                          source: (_, _) => 0.0);

        double[] rL = laplace.Solve(opts).Solution;
        double[] rP = poisson.Solve(opts).Solution;

        Assert.Equal(rL.Length, rP.Length);
        for (int k = 0; k < rL.Length; k++)
            Assert.Equal(rL[k], rP[k], precision: 10);
    }
}
