---
targets: [net10.0, fsharp-10]
last-reviewed: 2026-09-14
last-used: 2026-09-14
sources: [dotnet-blog, ms-learn, scott-wlaschin, mark-seemann]
---

# F#

Target F# 10 (ships with .NET 10). F# is first-class in this repository, not an afterthought.

## Domain modelling

- **Make illegal states unrepresentable: model states as discriminated unions, not flag-and-nullable combinations.** If a value can be one of several shapes (cash vs card vs direct debit; verified vs unverified email), a union with one case per shape means the compiler rejects every invalid combination, with no defensive checks and no unit tests for states that cannot exist. ([Wlaschin: Designing with types: Making illegal states unrepresentable](https://fsharpforfunandprofit.com/posts/designing-with-types-making-illegal-states-unrepresentable/), and the whole [Wlaschin: Designing with types series](https://fsharpforfunandprofit.com/series/designing-with-types/))
- **Wrap domain primitives in single-case unions with a private constructor and a `create` function that validates.** `CustomerId of int` cannot be confused with `OrderId of int`, and a private `EmailAddress` case guarantees every instance in the system already passed validation. ([Wlaschin: Designing with types: Single case union types](https://fsharpforfunandprofit.com/posts/designing-with-types-single-case-dus/))
- **Return `Result`/`Option` from domain operations instead of throwing:** model the failure path as data and compose with `Result.bind` (railway-oriented programming). Exceptions are for the truly exceptional (infrastructure faults), not for "customer not found". ([Wlaschin: Railway oriented programming](https://fsharpforfunandprofit.com/rop/), [Wlaschin: Domain Modeling Made Functional](https://fsharpforfunandprofit.com/ddd/))

```fsharp
type CustomerId = CustomerId of int

type EmailAddress = private EmailAddress of string

module EmailAddress =
    let create (s: string) =
        if s.Contains "@" then Some(EmailAddress s) else None

    let value (EmailAddress s) = s

type PaymentMethod =
    | Cash
    | Card of cardNumber: string * expiry: string
    | DirectDebit of iban: string

// Exhaustive match: adding a case is a compile error at every use site until handled
let describe payment =
    match payment with
    | Cash -> "cash"
    | Card(number, _) -> $"card ending {number.Substring(number.Length - 4)}"
    | DirectDebit iban -> $"direct debit from {iban}"
```

## Language opinions (F# 10)

- **Use `and!` in `task` expressions for concurrent awaits** instead of sequential `let!` chains when the operations are independent. ([Microsoft Learn: What's new in F# 10](https://learn.microsoft.com/dotnet/fsharp/whats-new/fsharp-10))

  ```fsharp
  let loadDashboard id =
      task {
          let! user = fetchUser id
          and! orders = fetchOrders id // starts before fetchUser completes
          return user, List.length orders
      }
  ```

- **Mark optional parameters `[<Struct>]` on hot paths** so they compile to `ValueOption<'T>` and skip the heap allocation of `Option<'T>`. ([.NET Blog: Introducing F# 10](https://devblogs.microsoft.com/dotnet/introducing-fsharp-10/), [Microsoft Learn: What's new in F# 10](https://learn.microsoft.com/dotnet/fsharp/whats-new/fsharp-10))

  ```fsharp
  type Formatter =
      static member Pad(text: string, [<Struct>] ?width: int) =
          let w =
              match width with
              | ValueSome w -> w
              | ValueNone -> text.Length

          text.PadLeft w
  ```

- **Write `seq { ... }` explicitly:** a brace block on its own is a deprecated sequence expression, and F# 10 warns `FS3873: This construct is deprecated. Sequence expressions should be of the form 'seq { ... }'`. The warning is about the braces, not what is inside them, so a comprehension already written inside `seq { ... }` is the correct form with or without an explicit `yield`. ([Microsoft Learn: What's new in F# 10](https://learn.microsoft.com/dotnet/fsharp/whats-new/fsharp-10))

  ```fsharp
  // Before: FS3873
  let numbers: seq<int> = { 1..10 }

  // After
  let numbers = seq { 1..10 }

  let evenSquares =
      seq {
          for i in 1..10 do
              if i % 2 = 0 then
                  i * i
      }
  ```

- **Scope warning suppression with `#nowarn`/`#warnon` pairs** around the exact lines concerned; never leave a bare `#nowarn` suppressing to end-of-file. ([.NET Blog: Introducing F# 10](https://devblogs.microsoft.com/dotnet/introducing-fsharp-10/))
- **Enable `ParallelCompilation` on multi-project F# solutions** for graph-based type checking, parallel IL generation and parallel optimization (preview in F# 10; adopt when it exits preview or if build time hurts today). ([Microsoft Learn: What's new in F# 10](https://learn.microsoft.com/dotnet/fsharp/whats-new/fsharp-10))

## Mixed C#/F# solutions

- **Put the domain model and core business logic in F# projects; hosts and framework-heavy edges (ASP.NET Core startup, UI shells) can stay C#.** The type-driven modelling above is where F# repays its cost; project references work in both directions, so the C# host consumes the F# domain like any other assembly. ([Microsoft Learn: F# component design guidelines](https://learn.microsoft.com/dotnet/fsharp/style-guide/component-design-guidelines))
- **The F# projects also give you a ban on cycles.** F# compiles files in declaration order and disallows circular dependencies between them, so a layering violation fails the build instead of waiting for a reviewer to notice it. Seemann rates this the strongest architectural argument for the language: what C# asks you to hold to by convention, F# enforces by construction. He is even-handed about the cost, giving C# the Razor integration, the editor tooling, and the mature complexity, coverage and mutation-testing support. ([Seemann: Worse is better: C# versus F#](https://blog.ploeh.dk/2026/09/08/worse-is-better-c-versus-f/))
- **Keep F#-specific types off public API boundaries consumed by C#.** Inside F# projects, use `Option`, F# lists, and curried functions freely; on the boundary, expose namespaces with classes and tupled methods, and prefer `ValueOption`/nullable and `IReadOnlyList<'T>`/`seq` over `FSharpOption`/`FSharpList` leaking into C# signatures. ([F# component design guidelines: Guidelines for libraries for use from other .NET languages](https://learn.microsoft.com/dotnet/fsharp/style-guide/component-design-guidelines))
- **Share the repository-standard `Directory.Build.props` / `Directory.Packages.props` across both languages** (see `templates/`); F# projects get the same TFM, CPM, and CI treatment, with no parallel build conventions. Central Package Management applies to every `PackageReference`-based project, `.fsproj` included. ([Microsoft Learn: Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management))

## Testing F#

**Use xUnit v3 for F# test projects, the same stack as the rest of the solution.** F# works first-class with xUnit (`[<Fact>]` on `let`-bound functions, no class required, as Microsoft Learn's F# walkthrough shows), and one test stack means shared fixtures, tooling, and CI config across a mixed C#/F# solution (see [testing.md](testing.md)). Expecto's tests-as-values model is elegant, but a second runner and assertion dialect in the same solution costs more than it returns. ([Microsoft Learn: Unit testing F# with dotnet test and xUnit](https://learn.microsoft.com/dotnet/core/testing/unit-testing-fsharp-with-dotnet-test))

```fsharp
module EmailAddressTests

open Xunit

[<Fact>]
let ``Create_StringWithoutAtSign_ReturnsNone`` () =
    // Arrange
    let input = "not-an-email"

    // Act
    let result = EmailAddress.create input

    // Assert
    Assert.Equal(None, result)
```

## Source-redundancy note

The independent F# bench is thin. F# for Fun and Profit has published nothing since its two-part design series of December 2025; its back catalogue on domain modelling and functional design remains the canonical reference, and the silence is recorded against `scott-wlaschin` for the next `vet-source` pass. F# Weekly keeps surfacing ecosystem news rather than argument. `mark-seemann` is the one addition here, independent and unmarked, though he argues about the language more than he writes in it. F# opinions therefore still lean more heavily on official sources than the C# opinions do.
