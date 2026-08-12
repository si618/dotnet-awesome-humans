---
targets: [net10.0, fsharp-10]
last-reviewed: 2026-08-12
last-used: 2026-08-12
sources: [dotnet-blog, ms-learn]
---

# F#

Target F# 10 (ships with .NET 10). F# is first-class in this repository, not an afterthought.

## Opinions

- **Scope warning suppression with `#nowarn`/`#warnon` pairs** around the exact lines concerned — never leave a bare `#nowarn` suppressing to end-of-file. ([Introducing F# 10](https://devblogs.microsoft.com/dotnet/introducing-fsharp-10/))
- **Use `[<Struct>]` `ValueOption<'T>` for optional parameters on hot paths** to avoid the heap allocation of `Option<'T>`. ([Introducing F# 10](https://devblogs.microsoft.com/dotnet/introducing-fsharp-10/), [What's new in F# 10](https://learn.microsoft.com/dotnet/fsharp/whats-new/fsharp-10))
- **Use `and!` in `task` expressions for concurrent awaits** instead of sequential `let!` chains when the operations are independent. ([What's new in F# 10](https://learn.microsoft.com/dotnet/fsharp/whats-new/fsharp-10)) <!-- TODO: code example -->
- **Write `seq { ... }` explicitly** — bare sequence expressions now raise FS3873 and the explicit form was always clearer. ([What's new in F# 10](https://learn.microsoft.com/dotnet/fsharp/whats-new/fsharp-10))
- **Enable `ParallelCompilation` on multi-project F# solutions** for graph-based type checking and parallel IL generation (preview in F# 10; adopt when it exits preview or if build time hurts today). ([What's new in F# 10](https://learn.microsoft.com/dotnet/fsharp/whats-new/fsharp-10))

## Source-redundancy note

The independent F# bench is thin right now: F# for Fun and Profit published only a two-part design series in the 2025-11 → 2026-08 window (December 2025; its back catalogue on domain modelling and functional design remains the canonical reference), and F# Weekly's window surfaced mostly ecosystem news. F# opinions therefore lean more heavily on official sources than the C# opinions do.

<!-- TODO: full treatment — domain modelling with types (wlaschin back catalogue), project conventions for mixed C#/F# solutions, testing with Expecto vs xUnit -->
