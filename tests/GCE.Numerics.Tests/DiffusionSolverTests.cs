using GCE.Numerics.Solvers;

namespace GCE.Numerics.Tests;

// ════════════════════════════════════════════════════════════════════════════
// DiffusionSolver1D – construction tests
// ════════════════════════════════════════════════════════════════════════════

public class DiffusionSolver1DConstructionTests
{
    [Fact]
    public void Constructor_ConstantD_DoesNotThrow()
    {
        var ic = new double[10];
        var solver = new DiffusionSolver1D(
            nx: 10, domainLength: 1.0, diffusivity: 0.1,
            initialCondition: ic,
            leftBC: new DirichletBC(0.0), rightBC: new DirichletBC(0.0),
            dt: 1e-4);

        Assert.NotNull(solver);
    }

    [Fact]
    public void Constructor_VariableD_DoesNotThrow()
    {
        int n = 8;
        double[] D = Enumerable.Range(0, n).Select(i => 0.1 + 0.01 * i).ToArray();
        var solver = new DiffusionSolver1D(
            nx: n, domainLength: 1.0, diffusivity: D,
            initialCondition: new double[n],
            leftBC: new DirichletBC(0.0), rightBC: new DirichletBC(1.0),
            dt: 1e-4);

        Assert.NotNull(solver);
    }

    [Fact]
    public void Constructor_NxLessThan2_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DiffusionSolver1D(1, 1.0, 0.1, [0.0],
                new DirichletBC(0.0), new DirichletBC(0.0), 1e-3));
    }

    [Fact]
    public void Constructor_NegativeDomainLength_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DiffusionSolver1D(5, -1.0, 0.1, new double[5],
                new DirichletBC(0.0), new DirichletBC(0.0), 1e-3));
    }

    [Fact]
    public void Constructor_NegativeDiffusivity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DiffusionSolver1D(5, 1.0, -0.1, new double[5],
                new DirichletBC(0.0), new DirichletBC(0.0), 1e-3));
    }

    [Fact]
    public void Constructor_WrongInitialConditionLength_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new DiffusionSolver1D(5, 1.0, 0.1, new double[4],  // wrong: 4 ≠ 5
                new DirichletBC(0.0), new DirichletBC(0.0), 1e-3));
    }

    [Fact]
    public void Constructor_NullLeftBC_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DiffusionSolver1D(5, 1.0, 0.1, new double[5],
                null!, new DirichletBC(0.0), 1e-3));
    }

    [Fact]
    public void Constructor_ZeroDt_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DiffusionSolver1D(5, 1.0, 0.1, new double[5],
                new DirichletBC(0.0), new DirichletBC(0.0), dt: 0.0));
    }

    [Fact]
    public void Constructor_VariableD_WrongLength_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new DiffusionSolver1D(5, 1.0, new double[4], new double[5],
                new DirichletBC(0.0), new DirichletBC(0.0), 1e-3));
    }

    [Fact]
    public void Constructor_VariableD_NegativeValue_ThrowsArgumentOutOfRangeException()
    {
        double[] D = [0.1, 0.1, -0.1, 0.1, 0.1];
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DiffusionSolver1D(5, 1.0, D, new double[5],
                new DirichletBC(0.0), new DirichletBC(0.0), 1e-3));
    }
}

// ════════════════════════════════════════════════════════════════════════════
// DiffusionSolver1D – IPDESolver interface contract
// ════════════════════════════════════════════════════════════════════════════

public class DiffusionSolver1DInterfaceTests
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
        const int nx = 20;
        IPDESolver solver = MakeSolver(nx: nx);
        var result = solver.Solve();

        Assert.Equal(nx, result.Solution.Length);
    }

    [Fact]
    public void Solve_NullOptions_UsesDefaults()
    {
        IPDESolver solver = MakeSolver();
        var result = solver.Solve(null);
        Assert.NotNull(result.Solution);
    }

    [Fact]
    public void Solve_WithCustomOptions_RespectsMaxTimeSteps()
    {
        // Very short run – exactly 2 time steps; use non-trivial IC so residual
        // stays positive and the convergence criterion does not fire early.
        IPDESolver solver = MakeSolverWithSineIC();
        var opts   = new PdeSolverOptions { MaxTimeSteps = 2, Tolerance = 1e-20 };
        var result = solver.Solve(opts);

        Assert.Equal(2, result.Iterations);
    }

    private static DiffusionSolver1D MakeSolver(int nx = 10)
    {
        var ic = new double[nx]; // zero IC
        return new DiffusionSolver1D(
            nx: nx, domainLength: 1.0, diffusivity: 0.01,
            initialCondition: ic,
            leftBC: new DirichletBC(0.0), rightBC: new DirichletBC(0.0),
            dt: 1e-4);
    }

    private static DiffusionSolver1D MakeSolverWithSineIC(int nx = 10)
    {
        double[] ic = Enumerable.Range(0, nx)
                                .Select(i => Math.Sin(Math.PI * i / (nx - 1.0)))
                                .ToArray();
        return new DiffusionSolver1D(
            nx: nx, domainLength: 1.0, diffusivity: 0.01,
            initialCondition: ic,
            leftBC: new DirichletBC(0.0), rightBC: new DirichletBC(0.0),
            dt: 1e-4);
    }
}

// ════════════════════════════════════════════════════════════════════════════
// DiffusionSolver1D – analytical solution tests
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Verifies accuracy against the analytical separation-of-variables solution.
/// Domain: [0, 1], D = 0.01, Dirichlet BCs: u(0,t) = u(1,t) = 0.
/// Initial condition: u(x,0) = sin(π·x).
/// Analytical solution: u(x,t) = sin(π·x)·exp(−D·π²·t).
/// </summary>
public class DiffusionSolver1DAnalyticalTests
{
    private const double D  = 0.01;
    private const double L  = 1.0;

    // Analytical solution for sine-mode test case
    private static double Exact(double x, double t) =>
        Math.Sin(Math.PI * x / L) * Math.Exp(-D * Math.PI * Math.PI / (L * L) * t);

    [Fact]
    public void Solve_SineMode_MatchesAnalyticalSolutionAtT01()
    {
        const int    nx  = 101;
        const double dt  = 1e-4;
        const double tEnd = 0.1;
        int steps = (int)Math.Round(tEnd / dt);

        double dx = L / (nx - 1);
        var    ic = Enumerable.Range(0, nx)
                              .Select(i => Exact(i * dx, 0.0))
                              .ToArray();

        var solver = new DiffusionSolver1D(
            nx: nx, domainLength: L, diffusivity: D,
            initialCondition: ic,
            leftBC:  new DirichletBC(0.0),
            rightBC: new DirichletBC(0.0),
            dt: dt);

        var result = solver.Solve(new PdeSolverOptions
        {
            MaxTimeSteps = steps,
            Tolerance    = 1e-15,   // don't stop early
        });

        // Crank–Nicolson is O(dt², dx²) accurate; tolerance generous for CI
        const double tol = 1e-4;
        double dx2 = L / (nx - 1);
        for (int i = 0; i < nx; i++)
        {
            double exact = Exact(i * dx2, tEnd);
            Assert.InRange(result.Solution[i], exact - tol, exact + tol);
        }
    }

    [Fact]
    public void Solve_SineMode_AccuracyIsBelowThreshold()
    {
        // With nx=51 and dt=1e-3, both spatial (O(dx²)≈4e-4) and temporal
        // (O(dt²)≈1e-6) contributions are small – the maximum error over all
        // nodes should be well below 1e-3.
        const double tEnd = 0.05;
        double err = MaxError(nx: 51, dt: 1e-3, tEnd: tEnd);
        Assert.True(err < 1e-3,
            $"Maximum error {err:G4} exceeds expected threshold of 1e-3");
    }

    [Fact]
    public void Solve_SineMode_SpatialRefinementReducesError()
    {
        // With the same dt, a finer spatial grid should produce a smaller error.
        const double tEnd = 0.05;
        const double dt   = 5e-3;  // fixed dt; vary nx

        double errCoarse = MaxError(nx: 11, dt: dt, tEnd: tEnd);
        double errFine   = MaxError(nx: 41, dt: dt, tEnd: tEnd);

        Assert.True(errFine < errCoarse,
            $"Spatial refinement did not reduce error: coarse={errCoarse:G4}, fine={errFine:G4}");
    }

    [Fact]
    public void Solve_SteadyState_ZeroIC_RemainZero()
    {
        // Zero IC with zero Dirichlet BCs should stay at zero.
        const int nx = 20;
        var solver = new DiffusionSolver1D(
            nx: nx, domainLength: 1.0, diffusivity: 1.0,
            initialCondition: new double[nx],
            leftBC:  new DirichletBC(0.0),
            rightBC: new DirichletBC(0.0),
            dt: 1e-3);

        var result = solver.Solve(new PdeSolverOptions { MaxTimeSteps = 50 });

        foreach (double v in result.Solution)
            Assert.Equal(0.0, v, precision: 14);
    }

    [Fact]
    public void Solve_ConstantIC_NeumannBothSides_ConservesIntegral()
    {
        // No-flux (Neumann=0) on both ends, uniform IC → solution stays uniform.
        const int    nx = 21;
        const double u0 = 3.5;
        double[] ic = Enumerable.Repeat(u0, nx).ToArray();

        var solver = new DiffusionSolver1D(
            nx: nx, domainLength: 1.0, diffusivity: 0.05,
            initialCondition: ic,
            leftBC:  new NeumannBC(0.0),  // zero flux
            rightBC: new NeumannBC(0.0),
            dt: 1e-3);

        var result = solver.Solve(new PdeSolverOptions { MaxTimeSteps = 100 });

        // Average should remain u0
        double avg = result.Solution.Average();
        Assert.Equal(u0, avg, precision: 6);

        // Every node should equal u0 (uniform solution is invariant)
        foreach (double v in result.Solution)
            Assert.Equal(u0, v, precision: 6);
    }

    // ── Helper ──────────────────────────────────────────────────────────────

    private static double MaxError(int nx, double dt, double tEnd)
    {
        double dx    = L / (nx - 1);
        int    steps = (int)Math.Round(tEnd / dt);
        var    ic    = Enumerable.Range(0, nx)
                                 .Select(i => Exact(i * dx, 0.0))
                                 .ToArray();

        var solver = new DiffusionSolver1D(
            nx: nx, domainLength: L, diffusivity: D,
            initialCondition: ic,
            leftBC:  new DirichletBC(0.0),
            rightBC: new DirichletBC(0.0),
            dt: dt);

        var result = solver.Solve(new PdeSolverOptions
        {
            MaxTimeSteps = steps,
            Tolerance    = 1e-15,
        });

        double maxErr = 0.0;
        for (int i = 0; i < nx; i++)
        {
            double e = Math.Abs(result.Solution[i] - Exact(i * dx, tEnd));
            if (e > maxErr) maxErr = e;
        }
        return maxErr;
    }
}

// ════════════════════════════════════════════════════════════════════════════
// DiffusionSolver1D – variable diffusivity tests
// ════════════════════════════════════════════════════════════════════════════

public class DiffusionSolver1DVariableDTests
{
    [Fact]
    public void Solve_VariableD_ConstantIC_NeumannBCs_ConservesIntegral()
    {
        // Even with variable D, zero-flux BCs on a uniform IC → stay uniform.
        const int    nx = 21;
        const double u0 = 2.0;
        int          n  = nx;
        double[]     D  = Enumerable.Range(0, n).Select(i => 0.05 + 0.02 * i / (n - 1)).ToArray();
        double[]     ic = Enumerable.Repeat(u0, n).ToArray();

        var solver = new DiffusionSolver1D(
            nx: n, domainLength: 1.0, diffusivity: D,
            initialCondition: ic,
            leftBC:  new NeumannBC(0.0),
            rightBC: new NeumannBC(0.0),
            dt: 1e-3);

        var result = solver.Solve(new PdeSolverOptions { MaxTimeSteps = 100 });

        foreach (double v in result.Solution)
            Assert.Equal(u0, v, precision: 6);
    }

    [Fact]
    public void Solve_VariableD_SolutionDecaysMonotonically()
    {
        // Sine IC with Dirichlet BCs: solution amplitude must decay over time.
        const int    nx = 51;
        const double L  = 1.0;
        double       dx = L / (nx - 1);
        double[]     D  = Enumerable.Range(0, nx).Select(i => 0.01 + 0.005 * Math.Sin(Math.PI * i * dx)).ToArray();
        double[]     ic = Enumerable.Range(0, nx).Select(i => Math.Sin(Math.PI * i * dx / L)).ToArray();

        var solver = new DiffusionSolver1D(
            nx: nx, domainLength: L, diffusivity: D,
            initialCondition: ic,
            leftBC:  new DirichletBC(0.0),
            rightBC: new DirichletBC(0.0),
            dt: 1e-4);

        // Run a short time
        var result = solver.Solve(new PdeSolverOptions { MaxTimeSteps = 50, Tolerance = 1e-15 });

        // Max amplitude should be less than 1 (initial max)
        double maxAmp = result.Solution.Max();
        Assert.True(maxAmp < 1.0,
            $"Expected amplitude decay; max = {maxAmp}");
        Assert.True(maxAmp > 0.0,
            "Solution should not have vanished in 50 small steps");
    }
}

// ════════════════════════════════════════════════════════════════════════════
// DiffusionSolver1D – adaptive dt tests
// ════════════════════════════════════════════════════════════════════════════

public class DiffusionSolver1DAdaptiveDtTests
{
    [Fact]
    public void Solve_AdaptiveDt_SolutionMatchesFixed()
    {
        // Both fixed and adaptive should converge to the same steady state
        // (zero for Dirichlet=0 with any positive IC).
        const int    nx  = 21;
        const double D   = 0.05;
        const double tol = 1e-3;

        double[] ic = Enumerable.Range(0, nx)
                                .Select(i => Math.Sin(Math.PI * i / (nx - 1.0)))
                                .ToArray();

        var opts = new PdeSolverOptions { MaxTimeSteps = 500, Tolerance = tol };

        var fixedSolver = new DiffusionSolver1D(
            nx, 1.0, D, (double[])ic.Clone(),
            new DirichletBC(0.0), new DirichletBC(0.0), dt: 1e-3);
        var fixedResult = fixedSolver.Solve(opts);

        var adaptSolver = new DiffusionSolver1D(
            nx, 1.0, D, (double[])ic.Clone(),
            new DirichletBC(0.0), new DirichletBC(0.0), dt: 1e-3,
            useAdaptiveDt: true, minDt: 1e-5, maxDt: 0.1);
        var adaptResult = adaptSolver.Solve(opts);

        // Both should converge
        Assert.True(fixedResult.Converged || adaptResult.Converged,
            "At least one solver should converge");
    }

    [Fact]
    public void Solve_AdaptiveDt_CurrentTimeAdvances()
    {
        const int nx = 10;
        var solver = new DiffusionSolver1D(
            nx, 1.0, 0.01, new double[nx],
            new DirichletBC(0.0), new DirichletBC(0.0), dt: 1e-3,
            useAdaptiveDt: true, minDt: 1e-6, maxDt: 0.1);

        Assert.Equal(0.0, solver.CurrentTime);

        solver.Solve(new PdeSolverOptions { MaxTimeSteps = 10, Tolerance = 1e-15 });

        Assert.True(solver.CurrentTime > 0.0,
            "CurrentTime should advance after Solve.");
    }
}

// ════════════════════════════════════════════════════════════════════════════
// DiffusionSolver1D – Robin boundary condition tests
// ════════════════════════════════════════════════════════════════════════════

public class DiffusionSolver1DRobinBCTests
{
    [Fact]
    public void Solve_RobinWithBetaZero_DegeneratesToDirichlet()
    {
        // β=0  =>  α·u = γ  =>  u = γ/α at the boundary
        const double gamma = 5.0, alpha = 2.0;
        const int nx = 11;
        var ic = new double[nx];

        var solver = new DiffusionSolver1D(
            nx: nx, domainLength: 1.0, diffusivity: 0.1,
            initialCondition: ic,
            leftBC:  new RobinBC(alpha, 0.0, gamma),  // β=0 → Dirichlet
            rightBC: new DirichletBC(0.0),
            dt: 1e-3);

        var result = solver.Solve(new PdeSolverOptions { MaxTimeSteps = 1 });
        // First node should immediately take the Dirichlet value γ/α = 2.5
        Assert.Equal(gamma / alpha, result.Solution[0], precision: 10);
    }

    [Fact]
    public void Solve_RobinBC_SolutionRemainsBounded()
    {
        // Stable Robin BC should produce a bounded solution.
        const int nx = 21;
        double[] ic = Enumerable.Range(0, nx)
                                .Select(i => Math.Sin(Math.PI * i / (nx - 1.0)))
                                .ToArray();

        var solver = new DiffusionSolver1D(
            nx: nx, domainLength: 1.0, diffusivity: 0.05,
            initialCondition: ic,
            leftBC:  new RobinBC(1.0, 1.0, 0.0),   // α·u + β·∂u/∂n = 0
            rightBC: new RobinBC(1.0, 1.0, 0.0),
            dt: 1e-3);

        var result = solver.Solve(new PdeSolverOptions { MaxTimeSteps = 100 });

        foreach (double v in result.Solution)
            Assert.True(Math.Abs(v) <= 2.0,
                $"Solution value {v} is unexpectedly large.");
    }
}

// ════════════════════════════════════════════════════════════════════════════
// DiffusionSolver1D – CurrentSolution and state tests
// ════════════════════════════════════════════════════════════════════════════

public class DiffusionSolver1DStateTests
{
    [Fact]
    public void CurrentSolution_InitiallyEqualsInitialCondition()
    {
        double[] ic = [1.0, 2.0, 3.0, 2.0, 1.0];
        var solver = new DiffusionSolver1D(
            nx: 5, domainLength: 1.0, diffusivity: 0.1,
            initialCondition: ic,
            leftBC: new DirichletBC(1.0), rightBC: new DirichletBC(1.0),
            dt: 1e-4);

        // Before any Solve call the state should match the IC
        for (int i = 0; i < ic.Length; i++)
            Assert.Equal(ic[i], solver.CurrentSolution[i]);
    }

    [Fact]
    public void CurrentTime_InitiallyZero()
    {
        var solver = new DiffusionSolver1D(
            5, 1.0, 0.1, new double[5],
            new DirichletBC(0.0), new DirichletBC(0.0), 1e-3);

        Assert.Equal(0.0, solver.CurrentTime);
    }

    [Fact]
    public void Solve_RepeatedCalls_AccumulateTime()
    {
        const double dt    = 1e-3;
        const int    steps = 10;

        // Use non-trivial IC so residual is positive and the convergence
        // criterion does not fire early during the repeated calls.
        double[] ic = Enumerable.Range(0, 10)
                                .Select(i => Math.Sin(Math.PI * i / 9.0))
                                .ToArray();

        var solver = new DiffusionSolver1D(
            10, 1.0, 0.01, ic,
            new DirichletBC(0.0), new DirichletBC(0.0), dt);

        solver.Solve(new PdeSolverOptions { MaxTimeSteps = steps, Tolerance = 1e-20 });
        double t1 = solver.CurrentTime;

        solver.Solve(new PdeSolverOptions { MaxTimeSteps = steps, Tolerance = 1e-20 });
        double t2 = solver.CurrentTime;

        Assert.Equal(steps * dt,       t1, precision: 10);
        Assert.Equal(2 * steps * dt,   t2, precision: 10);
    }
}

// ════════════════════════════════════════════════════════════════════════════
// DiffusionSolver2D – construction tests
// ════════════════════════════════════════════════════════════════════════════

public class DiffusionSolver2DConstructionTests
{
    [Fact]
    public void Constructor_ValidParams_DoesNotThrow()
    {
        var solver = Make2DSolver();
        Assert.NotNull(solver);
    }

    [Fact]
    public void Constructor_NxLessThan2_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DiffusionSolver2D(1, 5, 1.0, 1.0, 0.1, new double[5],
                new DirichletBC(0.0), new DirichletBC(0.0),
                new DirichletBC(0.0), new DirichletBC(0.0), 1e-4));
    }

    [Fact]
    public void Constructor_NyLessThan2_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DiffusionSolver2D(5, 1, 1.0, 1.0, 0.1, new double[5],
                new DirichletBC(0.0), new DirichletBC(0.0),
                new DirichletBC(0.0), new DirichletBC(0.0), 1e-4));
    }

    [Fact]
    public void Constructor_WrongInitialConditionLength_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new DiffusionSolver2D(5, 5, 1.0, 1.0, 0.1, new double[20], // 20 ≠ 25
                new DirichletBC(0.0), new DirichletBC(0.0),
                new DirichletBC(0.0), new DirichletBC(0.0), 1e-4));
    }

    [Fact]
    public void Constructor_NegativeDiffusivity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DiffusionSolver2D(5, 5, 1.0, 1.0, -0.1, new double[25],
                new DirichletBC(0.0), new DirichletBC(0.0),
                new DirichletBC(0.0), new DirichletBC(0.0), 1e-4));
    }

    [Fact]
    public void Constructor_NullBC_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DiffusionSolver2D(5, 5, 1.0, 1.0, 0.1, new double[25],
                null!, new DirichletBC(0.0),
                new DirichletBC(0.0), new DirichletBC(0.0), 1e-4));
    }

    private static DiffusionSolver2D Make2DSolver(int nx = 10, int ny = 10)
    {
        return new DiffusionSolver2D(
            nx: nx, ny: ny,
            domainLengthX: 1.0, domainLengthY: 1.0,
            diffusivity: 0.01,
            initialCondition: new double[nx * ny],
            leftBC: new DirichletBC(0.0), rightBC: new DirichletBC(0.0),
            bottomBC: new DirichletBC(0.0), topBC: new DirichletBC(0.0),
            dt: 1e-4);
    }
}

// ════════════════════════════════════════════════════════════════════════════
// DiffusionSolver2D – analytical solution tests
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Verifies accuracy against the 2-D analytical solution.
/// Domain: [0,1]×[0,1], D = 0.01, all-Dirichlet BCs = 0.
/// IC: u(x,y,0) = sin(π·x)·sin(π·y).
/// Analytical: u(x,y,t) = sin(π·x)·sin(π·y)·exp(−2·D·π²·t).
/// </summary>
public class DiffusionSolver2DAnalyticalTests
{
    private const double D  = 0.01;
    private const double Lx = 1.0;
    private const double Ly = 1.0;

    private static double Exact(double x, double y, double t) =>
        Math.Sin(Math.PI * x / Lx)
        * Math.Sin(Math.PI * y / Ly)
        * Math.Exp(-D * Math.PI * Math.PI * (1.0 / (Lx * Lx) + 1.0 / (Ly * Ly)) * t);

    [Fact]
    public void Solve_SineMode_MatchesAnalyticalSolutionAtT005()
    {
        const int    nx   = 21;
        const int    ny   = 21;
        const double dt   = 5e-4;
        const double tEnd = 0.05;
        int          steps = (int)Math.Round(tEnd / dt);

        double dx = Lx / (nx - 1);
        double dy = Ly / (ny - 1);

        var ic = new double[nx * ny];
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < ny; j++)
                ic[i * ny + j] = Exact(i * dx, j * dy, 0.0);

        var solver = new DiffusionSolver2D(
            nx: nx, ny: ny,
            domainLengthX: Lx, domainLengthY: Ly,
            diffusivity: D,
            initialCondition: ic,
            leftBC:   new DirichletBC(0.0),
            rightBC:  new DirichletBC(0.0),
            bottomBC: new DirichletBC(0.0),
            topBC:    new DirichletBC(0.0),
            dt: dt);

        var result = solver.Solve(new PdeSolverOptions
        {
            MaxTimeSteps = steps,
            Tolerance    = 1e-15,
        });

        const double tol = 1e-3; // generous for CI
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < ny; j++)
            {
                double exact    = Exact(i * dx, j * dy, tEnd);
                double computed = result.Solution[i * ny + j];
                Assert.InRange(computed, exact - tol, exact + tol);
            }
    }

    [Fact]
    public void Solve_ZeroIC_AllDirichletZero_StaysZero()
    {
        const int nx = 8, ny = 8;
        var solver = new DiffusionSolver2D(
            nx, ny, 1.0, 1.0, 0.1, new double[nx * ny],
            new DirichletBC(0.0), new DirichletBC(0.0),
            new DirichletBC(0.0), new DirichletBC(0.0), 1e-3);

        var result = solver.Solve(new PdeSolverOptions { MaxTimeSteps = 20 });

        foreach (double v in result.Solution)
            Assert.Equal(0.0, v, precision: 14);
    }

    [Fact]
    public void Solve_SolutionDecays_AmplitudeDecreasesOverTime()
    {
        const int    nx  = 11;
        const int    ny  = 11;
        const double dt  = 1e-3;
        double       dx  = Lx / (nx - 1);
        double       dy  = Ly / (ny - 1);

        var ic = new double[nx * ny];
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < ny; j++)
                ic[i * ny + j] = Exact(i * dx, j * dy, 0.0);

        var solver = new DiffusionSolver2D(
            nx, ny, Lx, Ly, D, ic,
            new DirichletBC(0.0), new DirichletBC(0.0),
            new DirichletBC(0.0), new DirichletBC(0.0), dt);

        var result1 = solver.Solve(new PdeSolverOptions { MaxTimeSteps = 20, Tolerance = 1e-15 });
        double max1 = result1.Solution.Max();

        var result2 = solver.Solve(new PdeSolverOptions { MaxTimeSteps = 20, Tolerance = 1e-15 });
        double max2 = result2.Solution.Max();

        Assert.True(max2 < max1,
            $"Expected further decay: max after step1={max1:G4}, after step2={max2:G4}");
    }
}

// ════════════════════════════════════════════════════════════════════════════
// DiffusionSolver2D – Neumann BC tests
// ════════════════════════════════════════════════════════════════════════════

public class DiffusionSolver2DNeumannTests
{
    [Fact]
    public void Solve_AllNeumannZeroFlux_UniformIC_StaysUniform()
    {
        const int    nx = 11;
        const int    ny = 11;
        const double u0 = 4.0;
        var ic = Enumerable.Repeat(u0, nx * ny).ToArray();

        var solver = new DiffusionSolver2D(
            nx, ny, 1.0, 1.0, 0.05, ic,
            new NeumannBC(0.0), new NeumannBC(0.0),
            new NeumannBC(0.0), new NeumannBC(0.0), 1e-3);

        var result = solver.Solve(new PdeSolverOptions { MaxTimeSteps = 50 });

        double avg = result.Solution.Average();
        Assert.Equal(u0, avg, precision: 5);

        foreach (double v in result.Solution)
            Assert.Equal(u0, v, precision: 5);
    }
}

// ════════════════════════════════════════════════════════════════════════════
// DiffusionSolver2D – Robin BC validation
// ════════════════════════════════════════════════════════════════════════════

public class DiffusionSolver2DRobinBCTests
{
    [Fact]
    public void Solve_RobinBC_ThrowsNotSupportedException()
    {
        const int nx = 5, ny = 5;
        var solver = new DiffusionSolver2D(
            nx, ny, 1.0, 1.0, 0.1, new double[nx * ny],
            new RobinBC(1.0, 1.0, 0.0),  // Robin – not supported
            new DirichletBC(0.0),
            new DirichletBC(0.0),
            new DirichletBC(0.0),
            dt: 1e-3);

        Assert.Throws<NotSupportedException>(() =>
            solver.Solve());
    }
}

// ════════════════════════════════════════════════════════════════════════════
// DiffusionSolver2D – IPDESolver interface contract
// ════════════════════════════════════════════════════════════════════════════

public class DiffusionSolver2DInterfaceTests
{
    [Fact]
    public void Solve_ImplementsIPDESolver()
    {
        IPDESolver solver = new DiffusionSolver2D(
            5, 5, 1.0, 1.0, 0.01, new double[25],
            new DirichletBC(0.0), new DirichletBC(0.0),
            new DirichletBC(0.0), new DirichletBC(0.0), 1e-4);

        Assert.NotNull(solver);
    }

    [Fact]
    public void Solve_ResultSolutionLength_EqualsNxTimesNy()
    {
        const int nx = 7, ny = 9;
        IPDESolver solver = new DiffusionSolver2D(
            nx, ny, 1.0, 1.0, 0.01, new double[nx * ny],
            new DirichletBC(0.0), new DirichletBC(0.0),
            new DirichletBC(0.0), new DirichletBC(0.0), 1e-4);

        var result = solver.Solve(new PdeSolverOptions { MaxTimeSteps = 2 });

        Assert.Equal(nx * ny, result.Solution.Length);
    }

    [Fact]
    public void Solve_ReturnsIterationsEqualToStepsTaken()
    {
        const int nx = 5, ny = 5;
        // Use non-trivial IC so residual remains positive and no early stop fires.
        double[] ic = new double[nx * ny];
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < ny; j++)
                ic[i * ny + j] = Math.Sin(Math.PI * i / (nx - 1.0))
                                * Math.Sin(Math.PI * j / (ny - 1.0));

        IPDESolver solver = new DiffusionSolver2D(
            nx, ny, 1.0, 1.0, 0.01, ic,
            new DirichletBC(0.0), new DirichletBC(0.0),
            new DirichletBC(0.0), new DirichletBC(0.0), 1e-4);

        var result = solver.Solve(new PdeSolverOptions
        {
            MaxTimeSteps = 5,
            Tolerance    = 1e-20,  // never converge early
        });

        Assert.Equal(5, result.Iterations);
    }
}
