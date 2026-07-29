## ⚡ Performance / Memory

Closes #

### What this optimizes / what regressed
<!-- e.g. cache footprint, scan time, allocations, return-to-idle after ClearCache. -->

### Before → after (numbers)
<!-- Paste the relevant BenchmarkDotNet rows. Do not eyeball — use MemoryDiagnoser output. -->

| Metric | Before | After | Threshold |
|---|---|---|---|
| Mean (ms) |  |  |  |
| Allocated |  |  |  |
| Residual after ClearCache |  |  | 8 MB |

### How measured
<!-- Which suite/benchmark, machine, invocation settings (InvocationCount/IterationCount). -->

### Threshold changes (`gs-benchmark/GSAnalyzer.Benchmarks/thresholds.json`)
- [ ] Updated keys/limits — state old → new below
- [ ] N/A — no threshold change

<!-- old → new here -->

### Checklist
- [ ] Benchmark job is green on this PR (or intentionally red, with explanation)
- [ ] No functional regression — `dotnet test` passes
- [ ] Memory/speed claims backed by benchmark output, not estimates
- [ ] Threshold keys match the real class + method names (`Class.Method`)
- [ ] Verified on Windows
