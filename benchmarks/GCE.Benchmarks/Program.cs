using BenchmarkDotNet.Running;

// Run all benchmarks in this assembly.
// Usage (from repository root):
//   dotnet run --project benchmarks/GCE.Benchmarks -c Release
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
