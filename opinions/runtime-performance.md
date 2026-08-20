---
targets: [net10.0, csharp-14]
last-reviewed: 2026-08-20
last-used: 2026-08-20
sources:
  [
    stephen-toub,
    dotnet-blog,
    ms-learn,
    steve-gordon,
    jetbrains-dotnet,
    jon-skeet,
  ]
---

# Runtime & performance

The .NET 10 JIT rewards idiomatic code. Optimize by measuring, not by folklore.

## Opinions

- **Write idiomatic C# and let the JIT work.** .NET 10's escape analysis, stack allocation of small objects, doubled inlining budgets, and bounds-check elimination apply automatically to clean code — do not reach for `unsafe` or exotic patterns to "help" the compiler. ([Toub — Performance Improvements in .NET 10](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-10/))
- **Profile before optimizing; benchmark the inner loop after.** Use a profiler (dotTrace, PerfView) to find the real bottleneck, then BenchmarkDotNet to iterate on it. Benchmark allocation numbers and profiler numbers legitimately differ (GC padding/alignment) — trust benchmarks for inner-loop deltas and profilers for locating allocation sources. ([Gordon — The Grand Mystery of the Missing 18 Bytes](https://www.stevejgordon.co.uk/the-grand-mystery-of-the-missing-18-bytes), [JetBrains — profiling methodology](https://blog.jetbrains.com/dotnet/2026/06/25/performance-profiling-agent-skill-in-rider/))
- **Use `Span<T>` for parsing/formatting hot paths and `ArrayPool<T>` for transient large buffers** — see [the worked example](#spant-and-arraypoolt) below. ([Gordon — low-allocation serialization, part 2](https://www.stevejgordon.co.uk/encrypting-properties-with-system-text-json-and-a-typeinforesolver-modifier-part-2))
- **Read Toub's annual "Performance Improvements in .NET X" post at each GA** — it is the canonical record of what the runtime now does for free, and therefore of which manual optimizations to delete.

## Immutable collections

**Pick the immutable collection by how the collection is built, not by the word "immutable".** `ImmutableList<T>` and `ImmutableDictionary<K,V>` pay for incremental change — their tree structure exists so `Add` can return a new collection sharing most of the old one, and every read walks that tree. Most application state isn't built that way: it's assembled once at startup or per refresh, then read constantly and replaced wholesale. For that shape use `ImmutableArray<T>` and `FrozenDictionary<K,V>`, which trade construction cost for flat, fast reads. Skeet measured a validation pass over election data drop from 5.5ms to 0.826ms on the switch, "due to it performing lots of read accesses". ([Skeet — Changing Immutable Collections](https://codeblog.jonskeet.uk/2025/12/31/changing-immutable-collections/))

Keep the `Immutable*` builders only where the incremental-change semantics are the point (an accumulating snapshot handed to concurrent readers between edits). One migration cost to expect: `ImmutableArray<T>` is a struct, so `default` is a valid-but-unusable value where `ImmutableList<T>` would simply have been `null` — a nullable `ImmutableArray<T>?` field needs unwrapping via `.Value` or an `is` pattern before use.

## Async

- **Return `Task`, not `void`; await, don't block.** `async void` is for event handlers only, and `.Result` / `.Wait()` on async work is a deadlock and thread-starvation hazard — the compiler-generated state machine expects to resume via continuations, not blocked threads. ([Toub — How Async/Await Really Works in C#](https://devblogs.microsoft.com/dotnet/how-async-await-really-works/))
- **`ConfigureAwait(false)` in library code; omit it in application code.** Libraries can't know their caller's context, so they should not capture it — it avoids deadlocks with context-blocking callers and skips a needless context hop. Application code on ASP.NET Core has no `SynchronizationContext` to capture, so `ConfigureAwait(false)` there is noise; UI application code usually _wants_ the context. One rule per layer — don't case-by-case it. ([Toub — ConfigureAwait FAQ](https://devblogs.microsoft.com/dotnet/configureawait-faq/))
- **Default to `Task<T>`; reserve `ValueTask<T>` for hot APIs that usually complete synchronously.** `ValueTask` earns its keep only when profiling shows `Task` allocations matter and the synchronous path dominates (e.g. a buffered read). Its contract is stricter: await it exactly once, never concurrently, never twice — when in doubt, `Task` is the safe, composable choice. ([Toub — Understanding the Whys, Whats, and Whens of ValueTask](https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/))

## `Span<T>` and `ArrayPool<T>`

Before — one string allocation per field:

```csharp
static int SumCsvAllocating(string line)
{
    var sum = 0;
    foreach (var field in line.Split(','))
    {
        sum += int.Parse(field);
    }
    return sum;
}
```

After — zero allocations; `Split` over a span yields `Range`s, not strings:

```csharp
static int SumCsv(ReadOnlySpan<char> line)
{
    var sum = 0;
    foreach (var range in line.Split(','))
    {
        sum += int.Parse(line[range]);
    }
    return sum;
}
```

For transient large buffers, rent from the shared pool instead of allocating per call, and always return in a `finally`:

```csharp
using System.Buffers;

static async Task CopyAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
{
    var buffer = ArrayPool<byte>.Shared.Rent(81920);
    try
    {
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }
}
```

Never use a rented buffer after returning it, and never assume `Rent` gives exactly the requested size — slice to what you used.

## GC modes

**Keep the defaults.** ASP.NET Core defaults to server GC with DATAS (dynamic heap-count adaptation, on by default since .NET 9), which gives throughput under load and shrinks the heap when idle; everything else defaults to workstation GC. Override in exactly two situations, and verify with memory/latency measurements:

- **Many .NET services on one small node** (dense containers, sidecars): force workstation GC (`<ServerGarbageCollection>false</ServerGarbageCollection>`) — per-core server heaps multiply across processes and DATAS only softens, not removes, that footprint.
- **A CPU-bound batch/worker app that isn't ASP.NET Core**: opt _in_ to server GC for throughput.

([Microsoft Learn — Workstation and server GC](https://learn.microsoft.com/dotnet/standard/garbage-collection/workstation-server-gc), [Toub — Performance Improvements in .NET 10](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-10/))

## Native AOT

**Use Native AOT for short-lived and size-sensitive workloads — CLI tools, serverless functions, sidecars; keep the JIT for long-running services.** AOT wins startup (milliseconds, no JIT warmup) and disk/memory footprint; the JIT wins steady-state throughput via tiered compilation and dynamic PGO, and tolerates reflection-heavy libraries that AOT's trimming breaks. Going AOT means the whole dependency graph must be trim/AOT-safe (source-generated JSON, no runtime codegen) — audit `IsAotCompatible` warnings before committing. .NET 10 file-based apps make the CLI-tool case trivial: `dotnet publish app.cs` produces a Native AOT binary by default. ([Microsoft Learn — Native AOT deployment](https://learn.microsoft.com/dotnet/core/deploying/native-aot/), [Microsoft Learn — File-based apps](https://learn.microsoft.com/dotnet/core/sdk/file-based-apps))

**Decide invariant globalization separately from AOT.** `dotnet new webapiaot` sets `<InvariantGlobalization>true</InvariantGlobalization>` next to `<PublishAot>true</PublishAot>`, so the property tends to enter a codebase attached to a decision that has nothing to do with it — and it changes what every `ToString` and `Compare` in the app does. Keep it only if you meant it (see [globalisation.md](globalisation.md)).
