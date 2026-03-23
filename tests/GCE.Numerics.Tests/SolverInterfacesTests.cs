using GCE.Numerics.Solvers;

namespace GCE.Numerics.Tests;

// ── LinearSolverOptions ───────────────────────────────────────────────────────

public class LinearSolverOptionsTests
{
    [Fact]
    public void LinearSolverOptions_Defaults_AreReasonable()
    {
        var options = new LinearSolverOptions();
        Assert.Equal(1_000, options.MaxIterations);
        Assert.Equal(1e-10, options.Tolerance);
    }

    [Fact]
    public void LinearSolverOptions_CanBeCustomised_ViaInitProperties()
    {
        var options = new LinearSolverOptions { MaxIterations = 500, Tolerance = 1e-6 };
        Assert.Equal(500,  options.MaxIterations);
        Assert.Equal(1e-6, options.Tolerance);
    }
}

// ── LinearSolverResult ────────────────────────────────────────────────────────

public class LinearSolverResultTests
{
    [Fact]
    public void LinearSolverResult_StoresAllProperties()
    {
        var result = new LinearSolverResult
        {
            Solution   = [1.0, 2.0, 3.0],
            Converged  = true,
            Iterations = 7,
            Residual   = 1e-12,
        };

        Assert.Equal([1.0, 2.0, 3.0], result.Solution);
        Assert.True(result.Converged);
        Assert.Equal(7,     result.Iterations);
        Assert.Equal(1e-12, result.Residual);
    }

    [Fact]
    public void LinearSolverResult_ConvergedFalse_RepresentsFailure()
    {
        var result = new LinearSolverResult
        {
            Solution   = [0.0],
            Converged  = false,
            Iterations = 1_000,
            Residual   = 0.5,
        };

        Assert.False(result.Converged);
        Assert.Equal(1_000, result.Iterations);
    }
}

// ── ILinearSolver (via stub implementation) ───────────────────────────────────

/// <summary>
/// Minimal stub that solves 1×1 systems directly, used only to validate the interface contract.
/// </summary>
file sealed class StubLinearSolver : ILinearSolver
{
    public LinearSolverResult Solve(double[,] a, double[] b, LinearSolverOptions? options = null)
    {
        if (a.GetLength(0) != a.GetLength(1))
            throw new ArgumentException("Matrix must be square.", nameof(a));
        if (a.GetLength(0) != b.Length)
            throw new ArgumentException("Matrix dimension must match rhs length.", nameof(b));

        int n = b.Length;
        // Naive diagonal solve: x_i = b_i / a_ii  (valid when A is diagonal)
        var x = new double[n];
        for (int i = 0; i < n; i++)
            x[i] = a[i, i] == 0.0 ? 0.0 : b[i] / a[i, i];

        return new LinearSolverResult
        {
            Solution   = x,
            Converged  = true,
            Iterations = 1,
            Residual   = 0.0,
        };
    }
}

public class ILinearSolverContractTests
{
    [Fact]
    public void ILinearSolver_CanBeAssignedFromConcreteType()
    {
        ILinearSolver solver = new StubLinearSolver();
        Assert.NotNull(solver);
    }

    [Fact]
    public void ILinearSolver_Solve_ReturnsSolution()
    {
        ILinearSolver solver = new StubLinearSolver();
        double[,] a = { { 2.0, 0.0 }, { 0.0, 4.0 } };
        double[]  b = { 6.0, 8.0 };

        LinearSolverResult result = solver.Solve(a, b);

        Assert.True(result.Converged);
        Assert.Equal(2, result.Solution.Length);
        Assert.Equal(3.0, result.Solution[0], precision: 10);
        Assert.Equal(2.0, result.Solution[1], precision: 10);
    }

    [Fact]
    public void ILinearSolver_Solve_AcceptsCustomOptions()
    {
        ILinearSolver solver  = new StubLinearSolver();
        var           options = new LinearSolverOptions { MaxIterations = 50, Tolerance = 1e-8 };
        double[,]     a       = { { 3.0 } };
        double[]      b       = { 9.0 };

        LinearSolverResult result = solver.Solve(a, b, options);

        Assert.True(result.Converged);
        Assert.Equal(3.0, result.Solution[0], precision: 10);
    }

    [Fact]
    public void ILinearSolver_Solve_ThrowsWhenMatrixIsNotSquare()
    {
        ILinearSolver solver = new StubLinearSolver();
        double[,] a = { { 1.0, 0.0, 0.0 } };  // 1×3 – not square
        double[]  b = { 1.0 };

        Assert.Throws<ArgumentException>(() => solver.Solve(a, b));
    }

    [Fact]
    public void ILinearSolver_Solve_ThrowsWhenDimensionMismatch()
    {
        ILinearSolver solver = new StubLinearSolver();
        double[,] a = { { 1.0, 0.0 }, { 0.0, 1.0 } };  // 2×2
        double[]  b = { 1.0 };                            // length 1 – mismatch

        Assert.Throws<ArgumentException>(() => solver.Solve(a, b));
    }
}

// ── PdeSolverOptions ──────────────────────────────────────────────────────────

public class PdeSolverOptionsTests
{
    [Fact]
    public void PdeSolverOptions_Defaults_AreReasonable()
    {
        var options = new PdeSolverOptions();
        Assert.Equal(1_000, options.MaxIterations);
        Assert.Equal(1e-10, options.Tolerance);
        Assert.Equal(1_000, options.MaxTimeSteps);
    }

    [Fact]
    public void PdeSolverOptions_CanBeCustomised_ViaInitProperties()
    {
        var options = new PdeSolverOptions
        {
            MaxIterations = 200,
            Tolerance     = 1e-8,
            MaxTimeSteps  = 500,
        };

        Assert.Equal(200,  options.MaxIterations);
        Assert.Equal(1e-8, options.Tolerance);
        Assert.Equal(500,  options.MaxTimeSteps);
    }
}

// ── PdeSolverResult ───────────────────────────────────────────────────────────

public class PdeSolverResultTests
{
    [Fact]
    public void PdeSolverResult_StoresAllProperties()
    {
        double[] solution = [0.0, 0.5, 1.0];
        var result = new PdeSolverResult
        {
            Solution   = solution,
            Converged  = true,
            Iterations = 42,
            Residual   = 1e-11,
        };

        Assert.Equal(solution, result.Solution);
        Assert.True(result.Converged);
        Assert.Equal(42,   result.Iterations);
        Assert.Equal(1e-11, result.Residual);
    }

    [Fact]
    public void PdeSolverResult_ConvergedFalse_RepresentsFailure()
    {
        var result = new PdeSolverResult
        {
            Solution   = [0.0, 0.0],
            Converged  = false,
            Iterations = 1_000,
            Residual   = 1.0,
        };

        Assert.False(result.Converged);
        Assert.Equal(1_000, result.Iterations);
    }
}

// ── IPDESolver (via stub implementation) ─────────────────────────────────────

/// <summary>
/// Minimal stub that returns a trivial zero solution, used only to validate the interface contract.
/// </summary>
file sealed class StubPdeSolver : IPDESolver
{
    private readonly int _nodes;

    public StubPdeSolver(int nodes) => _nodes = nodes;

    public PdeSolverResult Solve(PdeSolverOptions? options = null)
    {
        var opts = options ?? new PdeSolverOptions();
        return new PdeSolverResult
        {
            Solution   = new double[_nodes],
            Converged  = true,
            Iterations = 1,
            Residual   = 0.0,
        };
    }
}

public class IPDESolverContractTests
{
    [Fact]
    public void IPDESolver_CanBeAssignedFromConcreteType()
    {
        IPDESolver solver = new StubPdeSolver(10);
        Assert.NotNull(solver);
    }

    [Fact]
    public void IPDESolver_Solve_ReturnsSolutionOfExpectedLength()
    {
        IPDESolver solver = new StubPdeSolver(25);
        PdeSolverResult result = solver.Solve();

        Assert.True(result.Converged);
        Assert.Equal(25, result.Solution.Length);
    }

    [Fact]
    public void IPDESolver_Solve_AcceptsCustomOptions()
    {
        IPDESolver      solver  = new StubPdeSolver(5);
        var             options = new PdeSolverOptions { MaxIterations = 100, Tolerance = 1e-6 };
        PdeSolverResult result  = solver.Solve(options);

        Assert.True(result.Converged);
        Assert.Equal(5, result.Solution.Length);
    }

    [Fact]
    public void IPDESolver_Solve_NullOptions_UsesDefaults()
    {
        IPDESolver      solver = new StubPdeSolver(4);
        PdeSolverResult result = solver.Solve(null);

        Assert.True(result.Converged);
        Assert.Equal(4, result.Solution.Length);
    }
}
