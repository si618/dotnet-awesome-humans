---
targets: [net10.0, csharp-14]
last-reviewed: 2026-08-12
last-used: 2026-08-12
sources: [ms-learn, meziantou]
---

# Testing

Tests are first-class code: same review bar, same conventions.

## Opinions

- **Adopt Microsoft.Testing.Platform (MTP) for new test projects** — `dotnet test` in the .NET 10 SDK runs it natively; it replaces VSTest's process model with faster, self-contained test executables. ([What's new in .NET 10 — SDK](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10/overview))
- **Name tests `UnitOfWork_Scenario_ExpectedBehaviour`** and structure bodies with explicit Arrange / Act / Assert comments. <!-- repository convention; TODO: code example -->
- **Use snapshot testing for complex serialized output** (generated code, API payloads, rendered documents) instead of asserting field-by-field. ([Meziantou — Snapshot testing](https://www.meziantou.net/snapshot-testing-in-dotnet-with-meziantou-framework-snapshottesting.htm))
- **Shard slow CI test suites deterministically across parallel jobs — but measure first**: sharding pays off for CPU-bound suites far more than IO-bound ones. ([Meziantou — Test sharding](https://www.meziantou.net/split-dotnet-test-projects-into-shards-with-meziantou-shardedtest.htm))

<!-- TODO: full treatment — xUnit v3 vs alternatives stance, integration testing with WebApplicationFactory, F# testing (Expecto), coverage policy -->
