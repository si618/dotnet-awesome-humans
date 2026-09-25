---
targets: [net10.0, csharp-14]
last-reviewed: 2026-09-15
last-used: 2026-09-25
sources: [ms-learn, meziantou, andrew-lock, house, dotnet-blog]
---

# Testing

Tests are first-class code: same review bar, same conventions.

## Framework and platform

- **Use xUnit v3 on Microsoft.Testing.Platform (MTP) for new test projects, and opt into the MTP runner in `global.json`.** xunit.v3 ships a native MTP runner, so each test project builds to a self-contained executable that `dotnet run` executes directly, replacing VSTest's slower process model. `dotnet test` reaches it only through the runner opt-in below. Without it the SDK still routes through VSTest, which xunit.v3 4.0.0 (MTP v2) refuses on .NET 10 with `Testing with VSTest target is no longer supported`. On xunit.v3 3.x the same misconfiguration is worse than an error: `dotnet test` runs **zero** tests and exits 0.

  ```json
  {
    "sdk": { "version": "10.0.400", "rollForward": "latestFeature" },
    "test": { "runner": "Microsoft.Testing.Platform" }
  }
  ```

  Do not use `<TestingPlatformDotnetTestSupport>` instead. That property is the VSTest bridge, and it is what triggers the error above under MTP v2. MSTest and NUnit also run on MTP but xUnit's constructor-per-test isolation model and ecosystem weight make it the default; TUnit is promising but too young for a track record. ([Microsoft Learn: Microsoft.Testing.Platform overview](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro), [Microsoft Learn: What's new in .NET 10: SDK](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10/overview))

  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
      <Nullable>enable</Nullable>
      <OutputType>Exe</OutputType>
      <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
    </PropertyGroup>
    <ItemGroup>
      <PackageReference Include="xunit.v3" />
    </ItemGroup>
  </Project>
  ```

  The reference carries no version: central package management is mandatory (see [project-structure.md](project-structure.md)), so the pin lives in [templates/Directory.Packages.props](../templates/Directory.Packages.props). No `Microsoft.NET.Test.Sdk` reference: that is the VSTest world, and the `xunit.v3` package is self-sufficient under MTP. Do not pin `Microsoft.Testing.Platform` yourself either: xunit.v3 4.0.0 dropped MTP v1 and brings its own v2, and a separate pin overrides it (via transitive pinning under CPM) into a runtime `TypeLoadException`.

- **Getting off VSTest gains more speed than switching framework.** Meziantou benchmarked xUnit v3 4.0.0, NUnit 4.6.1, MSTest 4.4.0 and TUnit 1.65.68 on the .NET 10 SDK at up to 10,000 tests. The legacy VSTest path ran 4.9× slower than the MTP executable for xUnit v3, 5.1× slower for MSTest and 1.5× slower for NUnit, a gap he puts at three to four times what the choice of framework is worth. Per-test marginal cost separates the frameworks far less: 15µs for MSTest, 42µs for TUnit, 56µs for xUnit v3, 85µs for NUnit. Read those figures as a reason to keep the `global.json` opt-in above, not as a reason to leave xUnit. ([Meziantou: Benchmarking .NET test frameworks: xUnit v3, NUnit, MSTest, and TUnit](https://www.meziantou.net/benchmarking-dotnet-test-frameworks-xunit-v3-nunit-mstest-and-tunit.htm))

- **If the app ships Native AOT, add a representative native test lane beside the managed suite.** The managed run stays the fast-feedback lane on every change. The native lane publishes one C# test project with `<PublishAot>true</PublishAot>` and runs the resulting executable, because trimming and the reflection-free serialization that comes with it change behaviour a managed run cannot observe, so a green suite can still fail once published. `System.Text.Json` is the usual first casualty, throwing `InvalidOperationException` because reflection-based serialization is disabled; the fix is a `[JsonSerializable]` context on the app side rather than anything wrong with the test. That project references `xunit.v3.aot.mtp-v2` in place of `xunit.v3`: the AOT variant replaces the reflection-based package, so it cannot share a project with the managed suite, and F# test projects stay on the managed lane. NUnit has no Native AOT support at all. ([xUnit.net: Testing with Native AOT](https://xunit.net/docs/getting-started/v3/native-aot), [.NET Blog: Test what you ship: MSTest and Native AOT](https://devblogs.microsoft.com/dotnet/mstest-source-generation/), [Meziantou: Benchmarking .NET test frameworks: xUnit v3, NUnit, MSTest, and TUnit](https://www.meziantou.net/benchmarking-dotnet-test-frameworks-xunit-v3-nunit-mstest-and-tunit.htm))

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

- **Integration-test ASP.NET Core apps with `WebApplicationFactory<TEntryPoint>` instead of unit-testing controllers or endpoint handlers.** It boots the real app in-memory (routing, model binding, filters, DI, middleware all live) via `Microsoft.AspNetCore.Mvc.Testing`, so a handful of factory-based tests catch the wiring bugs that controller unit tests structurally cannot. Unit-test the domain logic the endpoints call, not the endpoints themselves. ([Microsoft Learn: Integration tests in ASP.NET Core](https://learn.microsoft.com/aspnet/core/test/integration-tests), [Andrew Lock: Should you unit-test API/MVC controllers?](https://andrewlock.net/should-you-unit-test-controllers-in-aspnetcore/))
- **Override services for tests through `ConfigureTestServices`, scoped to the test with `WithWebHostBuilder`.** ([Microsoft Learn: Integration tests in ASP.NET Core](https://learn.microsoft.com/aspnet/core/test/integration-tests))

## Deterministic time

- **Test time-dependent code through an injected `TimeProvider` with `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`), never with `Thread.Sleep` or the real clock.** How to drive it, its large-`Advance` caveat, and the awkward instants worth exercising (DST gaps and overlaps, leap days, ISO-week year boundaries) are in [datetime.md](datetime.md#testing-time), along with the analyzer rules that already ban hand-rolled clock abstractions in this repository's templates.
- **Give a test one clock: construct the `FakeTimeProvider` with an explicit start instant, and derive every other date in the test from it.** Fixed dates are not the problem; a test probing an awkward instant has to name it. The rule is where the literal lives: once, as the provider's start, where its choice reads as deliberate. From there, arrange with `time.GetUtcNow()`, move with `Advance`, and assert the code's time-derived outputs against `time.Start` plus the span, so the relationship between cause and expected effect is visible in the test body. Two literal dates that happen to be six minutes apart are correct today and opaque at the next review, and a hand-typed expected value silently re-encodes the arithmetic the code under test is supposed to be doing. The two constructor overloads that break this rule are in [datetime.md](datetime.md#testing-time). ([Andrew Lock: Avoiding flaky tests with TimeProvider and ITimer](https://andrewlock.net/exploring-the-dotnet-8-preview-avoiding-flaky-tests-with-timeprovider-and-itimer/) for `Advance` and `GetUtcNow`)

  Before: two unrelated literals, and the reader diffs them to learn the test's intent.

  ```csharp
  public class TokenValidatorTests
  {
      [Fact]
      public void IsExpired_LifetimeElapsed_ReturnsTrue()
      {
          // Arrange
          var token = new Token(issuedAt: new DateTimeOffset(2026, 10, 25, 0, 57, 0, TimeSpan.Zero), lifetime: TimeSpan.FromMinutes(5));
          var validator = new TokenValidator(new FakeTimeProvider(new DateTimeOffset(2026, 10, 25, 1, 3, 0, TimeSpan.Zero)));

          // Act
          var expired = validator.IsExpired(token);

          // Assert
          Assert.True(expired);
      }
  }
  ```

  After: one clock, one deliberately chosen start, and the elapsed time is the test. The start sits six minutes before the EU fall-back in a zone that observes it, so the local wall clock runs from 02:57 CEST back to 02:03 CET while only six minutes elapse: a validator that compared wall-clock times would call the token unexpired, and this test would catch it.

  ```csharp
  public class TokenValidatorTests
  {
      [Fact]
      public void IsExpired_LifetimeElapsedAcrossFallBack_ReturnsTrue()
      {
          // Arrange
          var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 25, 0, 57, 0, TimeSpan.Zero));
          time.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris"));
          var lifetime = TimeSpan.FromMinutes(5);
          var token = new Token(issuedAt: time.GetUtcNow(), lifetime);
          var validator = new TokenValidator(time);

          // Act
          time.Advance(TimeSpan.FromMinutes(6));

          // Assert
          Assert.Equal(time.Start.Add(lifetime), token.ExpiresAt);
          Assert.True(validator.IsExpired(token));
      }
  }
  ```

## Output and scale

- **Use snapshot testing for complex serialized output** (generated code, API payloads, rendered documents) instead of asserting field-by-field. ([Meziantou: Snapshot testing](https://www.meziantou.net/snapshot-testing-in-dotnet-with-meziantou-framework-snapshottesting.htm))

  Snapshot libraries locate the `.verified` files from the test's own source path, which deterministic builds rewrite to `/_/…`, so a project with `ContinuousIntegrationBuild`/`DeterministicSourcePaths` on cannot read or write its snapshots. Restore the mapping at startup rather than turning determinism off: an MSBuild target emitting a `[ModuleInitializer]` that registers `SourceRoot`'s `MappedPath` against the real directory keeps both properties. ([Meziantou: Reproducible builds and snapshot testing](https://www.meziantou.net/reproducible-builds-and-snapshot-testing-handling-file-paths-in-dotnet.htm))

- **Shard slow CI test suites deterministically across parallel jobs, but measure first**: sharding pays off for CPU-bound suites far more than IO-bound ones. ([Meziantou: Test sharding](https://www.meziantou.net/split-dotnet-test-projects-into-shards-with-meziantou-shardedtest.htm))

## Coverage

- **Collect line coverage on every CI run, and read it as a pointer to untested code, not a quality score.** Coverage shows which lines ran, not whether a test asserted anything about them, so a high number is no proof of good tests. ([Microsoft Learn: Use code coverage for unit testing](https://learn.microsoft.com/dotnet/core/testing/unit-testing-code-coverage), [Meziantou: Is the code coverage a sufficient metric?](https://www.meziantou.net/is-the-code-coverage-a-sufficient-metric.htm))

## F#

- **F# test projects use the same xUnit v3 stack:** `[<Fact>]` on `let`-bound functions, no test class needed. See [fsharp.md](fsharp.md#testing-f) for the stance and an example.
