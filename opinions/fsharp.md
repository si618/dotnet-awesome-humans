---
targets: [net10.0, fsharp-10]
last-reviewed: 2026-08-12
last-used: 2026-08-12
sources: [dotnet-blog, ms-learn, scott-wlaschin]
---

# F#

Target F# 10 (ships with .NET 10). F# is first-class in this repository, not an afterthought.

## Domain modelling

- **Make illegal states unrepresentable: model states as discriminated unions, not flag-and-nullable combinations.** If a value can be one of several shapes (cash vs card vs direct debit; verified vs unverified email), a union with one case per shape means the compiler rejects every invalid combination — no defensive checks, no unit tests for states that cannot exist. ([Designing with types: Making illegal states unrepresentable](https://fsharpforfunandprofit.com/posts/designing-with-types-making-illegal-states-unrepresentable/), and the whole [Designing with types series](https://fsharpforfunandprofit.com/series/designing-with-types/))
- **Wrap domain primitives in single-case unions with a private constructor and a `create` function that validates.** `CustomerId of int` cannot be confused with `OrderId of int`, and a private `EmailAddress` case guarantees every instance in the system already passed validation. ([Designing with types: Single case union types](https://fsharpforfunandprofit.com/posts/designing-with-types-single-case-dus/))
- **Return `Result`/`Option` from domain operations instead of throwing:** model the failure path as data and compose with `Result.bind` (railway-oriented programming). Exceptions are for the truly exceptional (infrastructure faults), not for "customer not found". ([Railway oriented programming](https://fsharpforfunandprofit.com/rop/), [Domain Modeling Made Functional](https://fsharpforfunandprofit.com/ddd/))

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

- **Use `and!` in `task` expressions for concurrent awaits** instead of sequential `let!` chains when the operations are independent. ([What's new in F# 10](https://learn.microsoft.com/dotnet/fsharp/whats-new/fsharp-10))

  ```fsharp
  let loadDashboard id =
      task {
          let! user = fetchUser id
          and! orders = fetchOrders id // starts before fetchUser completes
          return user, List.length orders
      }
  ```

- **Mark optional parameters `[<Struct>]` on hot paths** so they compile to `ValueOption<'T>` and skip the heap allocation of `Option<'T>`. ([Introducing F# 10](https://devblogs.microsoft.com/dotnet/introducing-fsharp-10/), [What's new in F# 10](https://learn.microsoft.com/dotnet/fsharp/whats-new/fsharp-10))

  ```fsharp
  type Formatter =
      static member Pad(text: string, [<Struct>] ?width: int) =
          let w =
              match width with
              | ValueSome w -> w
              | ValueNone -> text.Length

          text.PadLeft w
  ```

- **Write `seq { ... }` explicitly:** bare sequence expressions now raise FS3873 and the explicit form was always clearer. ([What's new in F# 10](https://learn.microsoft.com/dotnet/fsharp/whats-new/fsharp-10))

  ```fsharp
  let evenSquares =
      seq {
          for i in 1..10 do
              if i % 2 = 0 then
                  i * i
      }
  ```

- **Scope warning suppression with `#nowarn`/`#warnon` pairs** around the exact lines concerned — never leave a bare `#nowarn` suppressing to end-of-file. ([Introducing F# 10](https://devblogs.microsoft.com/dotnet/introducing-fsharp-10/))
- **Enable `ParallelCompilation` on multi-project F# solutions** for graph-based type checking and parallel IL generation (preview in F# 10; adopt when it exits preview or if build time hurts today). ([What's new in F# 10](https://learn.microsoft.com/dotnet/fsharp/whats-new/fsharp-10))

## Mixed C#/F# solutions

- **Put the domain model and core business logic in F# projects; hosts and framework-heavy edges (ASP.NET Core startup, UI shells) can stay C#.** The type-driven modelling above is where F# pays for itself; project references work in both directions, so the C# host consumes the F# domain like any other assembly. ([F# component design guidelines](https://learn.microsoft.com/dotnet/fsharp/style-guide/component-design-guidelines))
- **Keep F#-specific types off public API boundaries consumed by C#.** Inside F# projects, use `Option`, F# lists, and curried functions freely; on the boundary, expose namespaces with classes and tupled methods, and prefer `ValueOption`/nullable and `IReadOnlyList<'T>`/`seq` over `FSharpOption`/`FSharpList` leaking into C# signatures. ([F# component design guidelines: Guidelines for libraries for use from other .NET languages](https://learn.microsoft.com/dotnet/fsharp/style-guide/component-design-guidelines))
- **Share the repository-standard `Directory.Build.props` / `Directory.Packages.props` across both languages** (see `templates/`); F# projects get the same TFM, CPM, and CI treatment — no parallel build conventions.

## Testing F#

**Use xUnit v3 for F# test projects — the same stack as the rest of the solution.** F# works first-class with xUnit (`[<Fact>]` on `let`-bound functions, no class required), it is the officially documented path for F# unit testing, and one test stack means shared fixtures, tooling, and CI config across a mixed C#/F# solution (see [testing.md](testing.md)). Expecto's tests-as-values model is elegant, but a second runner and assertion dialect in the same solution costs more than it returns. ([Unit testing F# with dotnet test and xUnit](https://learn.microsoft.com/dotnet/core/testing/unit-testing-fsharp-with-dotnet-test))

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

The independent F# bench is thin right now: F# for Fun and Profit published only a two-part design series in the 2025-11 → 2026-08 window (December 2025; its back catalogue on domain modelling and functional design remains the canonical reference), and F# Weekly's window surfaced mostly ecosystem news. F# opinions therefore lean more heavily on official sources than the C# opinions do.
