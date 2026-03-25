using BenchmarkDotNet.Attributes;
using GCE.Numerics.LinearAlgebra;

namespace GCE.Benchmarks;

/// <summary>
/// Benchmarks for core linear-algebra operations in <see cref="Vector"/> and
/// <see cref="Matrix"/>, covering SIMD-accelerated paths and matrix multiply.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class LinearAlgebraBenchmarks
{
    private Vector _v256  = null!;
    private Vector _w256  = null!;
    private Vector _v1024 = null!;
    private Vector _w1024 = null!;

    private Matrix _m64  = null!;
    private Matrix _m128 = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _v256  = RandomVector(rng, 256);
        _w256  = RandomVector(rng, 256);
        _v1024 = RandomVector(rng, 1024);
        _w1024 = RandomVector(rng, 1024);
        _m64   = RandomMatrix(rng, 64);
        _m128  = RandomMatrix(rng, 128);
    }

    // ── Vector.Dot (SIMD-accelerated) ─────────────────────────────────────────

    [Benchmark(Baseline = true)]
    public double Dot_256() => Vector.Dot(_v256, _w256);

    [Benchmark]
    public double Dot_1024() => Vector.Dot(_v1024, _w1024);

    // ── Vector arithmetic ─────────────────────────────────────────────────────

    [Benchmark]
    public Vector VectorAdd_1024() => _v1024 + _w1024;

    [Benchmark]
    public Vector VectorMul_1024() => _v1024 * 2.5;

    // ── Matrix multiply ───────────────────────────────────────────────────────

    [Benchmark]
    public Matrix MatMul_64x64() => _m64 * _m64;

    [Benchmark]
    public Matrix MatMul_128x128() => _m128 * _m128;

    // ── Matrix-vector multiply ────────────────────────────────────────────────

    [Benchmark]
    public Vector MatVec_128() => _m128 * _v1024.Slice(0, 128);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Vector RandomVector(Random rng, int n)
    {
        var data = new double[n];
        for (int i = 0; i < n; i++) data[i] = rng.NextDouble();
        return new Vector(data);
    }

    private static Matrix RandomMatrix(Random rng, int n)
    {
        var data = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                data[i, j] = rng.NextDouble();
        return new Matrix(data);
    }
}
