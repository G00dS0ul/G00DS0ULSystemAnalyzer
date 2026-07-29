using System.Reflection;
using BenchmarkDotNet.Running;

// Runs every [Benchmark] discovered in this assembly.
// CI invokes it as:  dotnet run -c Release -- --exporters json --filter '*'
BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args);
