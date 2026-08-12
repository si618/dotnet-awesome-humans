---
targets: [net10.0, csharp-14]
last-reviewed: 2026-08-12
last-used: 2026-08-12
sources: [ms-learn, dotnet-blog, jetbrains-dotnet, andrew-lock]
---

# C#

Target C# 14 (ships with .NET 10). Adopt its features where they replace an older idiom — not everywhere they compile.

## Opinions

- **Use extension members (extension blocks) instead of scattered static extension-method classes.** C# 14's extension blocks group extension methods, properties, and operators for a receiver type in one place. ([Introducing C# 14](https://devblogs.microsoft.com/dotnet/introducing-csharp-14/), [What's new in C# 14](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-14)) <!-- TODO: expand with before/after code example -->
- **Use the `field` keyword instead of hand-written backing fields** when a property needs simple validation or lazy logic in an accessor. Declare an explicit backing field only when it is used outside the accessors. ([Introducing C# 14](https://devblogs.microsoft.com/dotnet/introducing-csharp-14/)) <!-- TODO: code example -->
- **Use null-conditional assignment (`obj?.Property = value`) to replace guarded `if (obj is not null)` nesting.** ([Introducing C# 14](https://devblogs.microsoft.com/dotnet/introducing-csharp-14/))
- **Prefer `Span<T>`/`ReadOnlySpan<T>` parameters in new APIs** — C# 14's implicit span conversions make them as ergonomic as arrays, without the allocation. Note the .NET 10 breaking change: span overloads now win overload resolution in more cases. ([What's new in C# 14](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-14), [Breaking changes in .NET 10](https://learn.microsoft.com/dotnet/core/compatibility/10))
- **Use `nameof(List<>)` on unbound generics** rather than hard-coded strings in diagnostics and exceptions. ([What's new in C# 14](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-14))
- **Clone records with `with` expressions — never declare an instance `Clone()` method**, which conflicts with the compiler-generated cloning; wrap `with` in an extension method if a named method is wanted. ([Meziantou — Adding a Clone method to a C# record](https://www.meziantou.net/adding-a-clone-method-to-a-csharp-record.htm))

## Coming next (preview — not yet the opinion)

.NET 11 previews add **union types** and **closed class hierarchies** with compile-time exhaustiveness checking. Track via Andrew Lock's series ([union types](https://andrewlock.net/exploring-the-dotnet-11-preview-2-dotnet-gets-union-types/), [closed hierarchies](https://andrewlock.net/exploring-the-dotnet-11-preview-4-closed-class-hierarchies/)); fold into opinions at .NET 11 GA.

<!-- TODO: full treatment — pattern matching guidance, collection expressions, nullable reference types stance, analyzer set -->
