---
targets: [net10.0, csharp-14]
last-reviewed: 2026-08-12
last-used: 2026-08-12
sources: [ms-learn, meziantou, andrew-lock, house]
---

# Testing

Tests are first-class code: same review bar, same conventions.

## Framework and platform

- **Use xUnit v3 on Microsoft.Testing.Platform (MTP) for new test projects.** xunit.v3 ships a native MTP runner, so each test project builds to a self-contained executable — `dotnet test` in the .NET 10 SDK runs it natively, and `dotnet run` on the project executes the suite directly, replacing VSTest's slower process model. MSTest and NUnit also run on MTP but xUnit's constructor-per-test isolation model and ecosystem weight make it the default; TUnit is promising but too young for a track record. ([Microsoft.Testing.Platform overview](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro), [What's new in .NET 10 — SDK](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10/overview))

  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>
      <OutputType>Exe</OutputType>
      <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
    </PropertyGroup>
    <ItemGroup>
      <PackageReference Include="xunit.v3" Version="3.0.0" />
    </ItemGroup>
  </Project>
  ```

  No `Microsoft.NET.Test.Sdk` reference — that is the VSTest world; the `xunit.v3` package is self-sufficient under MTP.

## Naming and structure

- **House:** name tests `UnitOfWork_Scenario_ExpectedBehaviour` and structure bodies with explicit Arrange / Act / Assert comments. ([HOUSE-OPINIONS.md](../HOUSE-OPINIONS.md))

  ```csharp
  public class BasketTests
  {
      [Fact]
      public void Total_MultipleItemsAdded_SumsAllItemPrices()
      {
          // Arrange
          var basket = new Basket();
          basket.Add(10.00m);
          basket.Add(2.50m);

          // Act
          var total = basket.Total;

          // Assert
          Assert.Equal(12.50m, total);
      }
  }
  ```

## Integration testing

- **Integration-test ASP.NET Core apps with `WebApplicationFactory<TEntryPoint>` instead of unit-testing controllers or endpoint handlers.** It boots the real app in-memory (routing, model binding, filters, DI, middleware all live) via `Microsoft.AspNetCore.Mvc.Testing`, so a handful of factory-based tests catch the wiring bugs that controller unit tests structurally cannot. Unit-test the domain logic the endpoints call, not the endpoints themselves. ([Integration tests in ASP.NET Core](https://learn.microsoft.com/aspnet/core/test/integration-tests), [Andrew Lock — Should you unit-test API/MVC controllers?](https://andrewlock.net/should-you-unit-test-controllers-in-aspnetcore/))
- **Override services for tests through `WithWebHostBuilder`/`ConfigureTestServices`, never through production code branches** — the app under test must be the app you ship, with only its edges (database, outbound HTTP) swapped. ([Integration tests in ASP.NET Core](https://learn.microsoft.com/aspnet/core/test/integration-tests))

## Output and scale

- **Use snapshot testing for complex serialized output** (generated code, API payloads, rendered documents) instead of asserting field-by-field. ([Meziantou — Snapshot testing](https://www.meziantou.net/snapshot-testing-in-dotnet-with-meziantou-framework-snapshottesting.htm))
- **Shard slow CI test suites deterministically across parallel jobs — but measure first**: sharding pays off for CPU-bound suites far more than IO-bound ones. ([Meziantou — Test sharding](https://www.meziantou.net/split-dotnet-test-projects-into-shards-with-meziantou-shardedtest.htm))

## Coverage

- **Collect line coverage on every CI run and watch the trend; do not gate merges on an absolute percentage.** Coverage is a diagnostic that finds untested areas, not a quality score — a hard bar breeds assertion-free tests written to satisfy the number. Investigate drops and conspicuously dark areas instead. ([Use code coverage for unit testing](https://learn.microsoft.com/dotnet/core/testing/unit-testing-code-coverage))

## F#

- **F# test projects use the same xUnit v3 stack** — `[<Fact>]` on `let`-bound functions, no test class needed. See [fsharp.md](fsharp.md#testing-f) for the stance and an example.
