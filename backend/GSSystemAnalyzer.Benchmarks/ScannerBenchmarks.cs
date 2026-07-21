using BenchmarkDotNet.Attributes;
using GSSystemAnalyzer.Services;

namespace GSSystemAnalyzer.Benchmarks;

// [MemoryDiagnoser] reports Gen0/1/2 + Allocated bytes, so github-action-benchmark
// gates BOTH CPU time and allocation churn. True retention leaks are caught by the
// soak test (LeakTests.cs); this catches per-operation regressions.
[MemoryDiagnoser]
[JsonExporterAttribute.FullCompressed]
public class ScannerBenchmarks
{
    [Benchmark]
    public int ResolveCleanTargets()
        => TempFolderCleanerService.ResolveCleanTargets().Count;

    [Benchmark]
    public int ResolveTempPaths()
        => TempFolderCleanerService.ResolveTempPaths().Count;

    // ---- Optional: end-to-end PreviewAsync benchmark ----
    // Requires a service instance (mocked nuke service + seeded dir). Uncomment and
    // add Moq + a [GlobalSetup]/[GlobalCleanup] pair if you want to track the full path:
    //
    // private TempFolderCleanerService _svc = null!;
    // private string _dir = null!;
    //
    // [GlobalSetup]
    // public void Setup()
    // {
    //     _dir = Path.Combine(Path.GetTempPath(), "gs_bench_" + Guid.NewGuid().ToString("N"));
    //     Directory.CreateDirectory(_dir);
    //     for (int i = 0; i < 50; i++) File.WriteAllText(Path.Combine(_dir, $"f{i}.tmp"), "x");
    //     var nuke = new Mock<INukeProtocolService>().Object;
    //     _svc = new TempFolderCleanerService(nuke, NullLogger<TempFolderCleanerService>.Instance, new[] { _dir });
    // }
    //
    // [Benchmark]
    // public async Task PreviewAsync_SmallDir() => await _svc.PreviewAsync();
    //
    // [GlobalCleanup]
    // public void Cleanup() { try { Directory.Delete(_dir, true); } catch { } }
}
