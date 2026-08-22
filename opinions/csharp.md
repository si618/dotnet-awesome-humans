---
targets: [net10.0, csharp-14]
last-reviewed: 2026-08-18
last-used: 2026-08-20
sources:
  [ms-learn, dotnet-blog, jetbrains-dotnet, andrew-lock, meziantou, jon-skeet]
---

# C#

Target C# 14 (ships with .NET 10). Adopt its features where they replace an older idiom — not everywhere they compile.

## Opinions

### Extension members

**Use extension members (extension blocks) instead of scattered static extension-method classes.** C# 14's extension blocks group extension methods, properties, and operators for a receiver type in one place, declare the receiver once, and allow extension properties — impossible with the old `this`-parameter syntax.

Before (C# 13 and earlier):

```csharp
public static class OrderExtensions
{
    public static bool IsOpen(this Order order) => order.ClosedAt is null;

    public static decimal Total(this Order order) => order.Lines.Sum(l => l.Price);
}
```

After (C# 14):

```csharp
public static class OrderExtensions
{
    extension(Order order)
    {
        public bool IsOpen => order.ClosedAt is null;

        public decimal Total => order.Lines.Sum(l => l.Price);
    }
}
```

Callers are unchanged (`order.Total`), so migrate opportunistically — whenever you touch an extension class, convert it. ([Introducing C# 14](https://devblogs.microsoft.com/dotnet/introducing-csharp-14/), [What's new in C# 14](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-14))

### The `field` keyword

**Use the `field` keyword instead of hand-written backing fields** when a property needs simple validation, normalisation, or lazy logic in an accessor. Declare an explicit backing field only when it is used outside the accessors. This removes the field/property naming ceremony and the risk of code bypassing the accessor by writing to the field directly.

Before:

```csharp
public class Sensor
{
    private string _name = "";

    public string Name
    {
        get => _name;
        set => _name = value.Trim();
    }
}
```

After:

```csharp
public class Sensor
{
    public string Name
    {
        get;
        set => field = value.Trim();
    } = "";
}
```

([Introducing C# 14](https://devblogs.microsoft.com/dotnet/introducing-csharp-14/))

### Null-conditional assignment

**Use null-conditional assignment (`obj?.Property = value`) to replace guarded `if (obj is not null)` nesting.** The right-hand side is only evaluated when the receiver is non-null, so it is a faithful, flatter rewrite of the guard.

Before:

```csharp
if (customer is not null)
{
    customer.LastSeen = DateTimeOffset.UtcNow;
}
```

After:

```csharp
customer?.LastSeen = DateTimeOffset.UtcNow;
```

([Introducing C# 14](https://devblogs.microsoft.com/dotnet/introducing-csharp-14/))

### Pattern matching

**Use `is null` / `is not null` for all null checks, and a switch expression whenever three or more branches produce a value.** Pattern `is` checks cannot be hijacked by overloaded `==`/`!=` operators, and switch expressions with property, relational, and type patterns collapse chained `if`/`else` into one declarative table the compiler checks for exhaustiveness (CS8509 when an input is unhandled).

```csharp
public static decimal DiscountFor(Customer customer) => customer switch
{
    { Tier: Tier.Gold, YearsActive: >= 5 } => 0.20m,
    { Tier: Tier.Gold } => 0.10m,
    { Tier: Tier.Silver } => 0.05m,
    _ => 0m,
};
```

Order arms most-specific first — arms are matched top to bottom. Don't force patterns where a plain boolean expression reads better; a two-way `if` is not improved by becoming a switch. ([Microsoft Learn: Pattern matching](https://learn.microsoft.com/dotnet/csharp/fundamentals/functional/pattern-matching))

### Collection expressions

**Create and combine collections with collection expressions (`[...]`), not constructor-plus-initializer or LINQ `Concat`/`ToArray` chains.** One syntax targets arrays, `List<T>`, spans, and immutable collections alike, the compiler picks the most efficient construction for the target type, and the spread element `..` replaces allocating concatenation pipelines.

Before:

```csharp
var ids = new List<int> { 1, 2, 3 };
int[] merged = first.Concat(second).ToArray();
```

After:

```csharp
List<int> ids = [1, 2, 3];
int[] merged = [.. first, .. second];
```

The one cost: the target type must be explicit (`List<int> ids = [...]`, not `var ids = [...]`). Accept that — the type is documentation. ([Microsoft Learn: Collection expressions](https://learn.microsoft.com/dotnet/csharp/language-reference/operators/collection-expressions))

### Nullable reference types

**Enable nullable reference types in every project and promote nullable warnings to errors** — `<Nullable>enable</Nullable>` plus `<WarningsAsErrors>nullable</WarningsAsErrors>` in `Directory.Build.props`, so annotations are load-bearing rather than advisory. Never start a new project without it; never use `#nullable disable` in new code. Reserve the null-forgiving operator `!` for cases the flow analysis genuinely cannot see (e.g. values populated by a serializer or test setup), and treat every `!` as a code smell to justify in review. In annotated code, `ArgumentNullException.ThrowIfNull` at public API boundaries is still correct — annotations are compile-time only and do not protect against un-annotated or reflection-based callers. ([Microsoft Learn: Nullable reference types](https://learn.microsoft.com/dotnet/csharp/nullable-references))

### Analyzers

**Turn the built-in .NET analyzers up to `latest-recommended`, enforce code style in build, and add exactly one third-party analyzer: Meziantou.Analyzer.** In `Directory.Build.props`:

```xml
<PropertyGroup>
  <AnalysisLevel>latest-recommended</AnalysisLevel>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
</PropertyGroup>
```

The built-in analyzers ship with the SDK and track it; `latest-recommended` keeps rules current across SDK updates without opting into the noisy `all` bucket. `EnforceCodeStyleInBuild` makes `.editorconfig` style rules (IDExxxx) build-time diagnostics instead of IDE-only suggestions, so CI and editors agree. Meziantou.Analyzer adds the correctness rules the SDK set misses (culture-sensitive string operations, `CancellationToken` forwarding, async pitfalls) with a low false-positive rate; StyleCop lost on signal-to-noise — it polices formatting the SDK analyzers and `dotnet format` already cover. Fix or explicitly suppress with justification; never blanket-lower severity. ([Microsoft Learn: Code analysis overview](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/overview), [Meziantou.Analyzer](https://github.com/meziantou/Meziantou.Analyzer))

### Records

**Keep records to the data they are constructed from — no derived state, no collection members.** A record's generated members only behave the way the syntax suggests when every property is a constructor parameter held as-is. Two traps follow from that, both worth an analyzer or a code-review reflex:

- **A property initialized from a constructor parameter goes stale under `with`.** `with` does not re-run the constructor: it lowers to roughly `var copy = original.<Clone>$(); copy.Value = 3;`, and the copy constructor duplicates every field _before_ the property assignments run — so anything computed at construction keeps its old value while the parameter it was computed from changes. ([Skeet: Unexpected inconsistency in records](https://codeblog.jonskeet.uk/2025/07/19/unexpected-inconsistency-in-records/), [Skeet: Records and the `with` operator, redux](https://codeblog.jonskeet.uk/2025/07/29/records-and-the-with-operator-redux/))

  ```csharp
  // Wrong — Even is computed once, at construction
  public sealed record Number(int Value)
  {
      public bool Even { get; } = (Value & 1) == 0;
  }

  var n3 = new Number(2) with { Value = 3 };  // Number { Value = 3, Even = True }

  // Right — derive on read, so `with` cannot desynchronize it
  public sealed record Number(int Value)
  {
      public bool Even => (Value & 1) == 0;
  }
  ```

- **A collection member breaks value equality.** Generated `Equals` compares each member with `EqualityComparer<T>.Default`, and the immutable collections do not override `Equals`/`GetHashCode` — so `ImmutableList<T>` and friends compare by reference, and two records with identical contents are unequal. Hold a collection in a record only where reference equality is genuinely what you want (shared instances within one object graph); otherwise expose the collection outside the record, or accept that equality is identity and document it. ([Skeet: Records and Collections](https://codeblog.jonskeet.uk/2025/03/27/records-and-collections/))

### Smaller opinions

- **Prefer `Span<T>`/`ReadOnlySpan<T>` parameters in new APIs:** C# 14's implicit span conversions make them as ergonomic as arrays, without the allocation. Note the .NET 10 breaking change: span overloads now win overload resolution in more cases. ([What's new in C# 14](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-14), [Breaking changes in .NET 10](https://learn.microsoft.com/dotnet/core/compatibility/10))
- **Use `nameof(List<>)` on unbound generics** rather than hard-coded strings in diagnostics and exceptions. ([What's new in C# 14](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-14))
- **Use `StringComparison.Ordinal` for every machine-facing comparison and `CultureInfo.InvariantCulture` for every machine-facing format or parse** — and never `StringComparison.InvariantCulture`, whose collation is not actually invariant. ([globalization.md](globalization.md))
- **Clone records with `with` expressions — never declare an instance `Clone()` method**, which conflicts with the compiler-generated cloning; wrap `with` in an extension method if a named method is wanted. ([Meziantou: Adding a Clone method to a C# record](https://www.meziantou.net/adding-a-clone-method-to-a-csharp-record.htm))

## Coming next (preview — not yet the opinion)

.NET 11 previews add **union types** and **closed class hierarchies** with compile-time exhaustiveness checking. Track via Andrew Lock's series ([union types](https://andrewlock.net/exploring-the-dotnet-11-preview-2-dotnet-gets-union-types/), [closed hierarchies](https://andrewlock.net/exploring-the-dotnet-11-preview-4-closed-class-hierarchies/)); fold into opinions at .NET 11 GA.
