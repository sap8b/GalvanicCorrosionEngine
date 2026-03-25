using BenchmarkDotNet.Attributes;
using GCE.Numerics.Solvers;

namespace GCE.Benchmarks;

/// <summary>
/// Benchmarks for the numerical PDE solvers
/// (<see cref="PoissonSolver2D"/> and <see cref="DiffusionSolver2D"/>)
/// at representative grid sizes.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class NumericalSolverBenchmarks
{
    // Benchmark grid sizes
    private const int SmallGrid  = 32;
    private const int MediumGrid = 64;
    private const int LargeGrid  = 128;

    private static readonly PdeSolverOptions PoissonOpts   = new() { MaxIterations = 200, Tolerance = 1e-4 };
    private static readonly PdeSolverOptions DiffusionOpts = new() { MaxTimeSteps  = 50,  Tolerance = 1e-6 };

    // ── PoissonSolver2D ───────────────────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public PdeSolverResult Poisson_32x32()   => MakePoisson(SmallGrid).Solve(PoissonOpts);

    [Benchmark]
    public PdeSolverResult Poisson_64x64()   => MakePoisson(MediumGrid).Solve(PoissonOpts);

    [Benchmark]
    public PdeSolverResult Poisson_128x128() => MakePoisson(LargeGrid).Solve(PoissonOpts);

    // ── DiffusionSolver2D ─────────────────────────────────────────────────────

    [Benchmark]
    public PdeSolverResult Diffusion_32x32()   => MakeDiffusion(SmallGrid).Solve(DiffusionOpts);

    [Benchmark]
    public PdeSolverResult Diffusion_64x64()   => MakeDiffusion(MediumGrid).Solve(DiffusionOpts);

    [Benchmark]
    public PdeSolverResult Diffusion_128x128() => MakeDiffusion(LargeGrid).Solve(DiffusionOpts);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PoissonSolver2D MakePoisson(int n) =>
        new(n, n, 1.0, 1.0,
            leftBC:   new DirichletBC(_ => 0.0),
            rightBC:  new DirichletBC(_ => 1.0),
            bottomBC: new NeumannBC(_ => 0.0),
            topBC:    new NeumannBC(_ => 0.0),
            source:   (_, _) => 0.0,
            omega:    1.5);

    private static DiffusionSolver2D MakeDiffusion(int n) =>
        new(n, n, 1.0, 1.0,
            diffusivity:      1e-5,
            initialCondition: new double[n * n],
            leftBC:           new NeumannBC(_ => 0.0),
            rightBC:          new DirichletBC(_ => 1.0),
            bottomBC:         new NeumannBC(_ => 0.0),
            topBC:            new NeumannBC(_ => 0.0),
            dt:               1e-3);
}
