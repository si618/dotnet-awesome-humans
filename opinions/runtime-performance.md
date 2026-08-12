---
targets: [net10.0, csharp-14]
last-reviewed: 2026-08-12
last-used: 2026-08-12
sources: [stephen-toub, dotnet-blog, steve-gordon, jetbrains-dotnet]
---

# Runtime & performance

The .NET 10 JIT rewards idiomatic code. Optimize by measuring, not by folklore.

## Opinions

- **Write idiomatic C# and let the JIT work.** .NET 10's escape analysis, stack allocation of small objects, doubled inlining budgets, and bounds-check elimination apply automatically to clean code — do not reach for `unsafe` or exotic patterns to "help" the compiler. ([Toub — Performance Improvements in .NET 10](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-10/))
- **Profile before optimizing; benchmark the inner loop after.** Use a profiler (dotTrace, PerfView) to find the real bottleneck, then BenchmarkDotNet to iterate on it. Benchmark allocation numbers and profiler numbers legitimately differ (GC padding/alignment) — trust benchmarks for inner-loop deltas and profilers for locating allocation sources. ([Gordon — The Grand Mystery of the Missing 18 Bytes](https://www.stevejgordon.co.uk/the-grand-mystery-of-the-missing-18-bytes), [JetBrains — profiling methodology](https://blog.jetbrains.com/dotnet/2026/06/25/performance-profiling-agent-skill-in-rider/))
- **Use `Span<T>` for parsing/formatting hot paths and `ArrayPool<T>` for transient large buffers.** ([Gordon — low-allocation serialization, part 2](https://www.stevejgordon.co.uk/encrypting-properties-with-system-text-json-and-a-typeinforesolver-modifier-part-2)) <!-- TODO: code example -->
- **Read Toub's annual "Performance Improvements in .NET X" post at each GA** — it is the canonical record of what the runtime now does for free, and therefore of which manual optimizations to delete.

<!-- TODO: full treatment — async guidance (ConfigureAwait, ValueTask), GC modes, NativeAOT stance -->
