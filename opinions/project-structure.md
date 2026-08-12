---
targets: [net10.0, csharp-14, fsharp-10]
last-reviewed: 2026-08-12
last-used: 2026-08-12
sources: [ms-learn, dotnet-blog, gerald-versluis]
---

# Project structure & SDK

One solution format, central package management, versions pinned at the root.

## Opinions

- **Use `.slnx` for new solutions** — `dotnet new sln` defaults to it in the .NET 10 SDK; migrate `.sln` files opportunistically. Use `.slnf` filters for large solutions. ([Breaking changes in .NET 10](https://learn.microsoft.com/dotnet/core/compatibility/10)) <!-- TODO: templates/example.slnx -->
- **Pin the SDK with `global.json`** (roll-forward `latestFeature`); use `sdk.paths` to trial preview SDKs per-repo without touching the machine. ([Versluis — sdk.paths](https://blog.verslu.is/), [What's new in .NET 10](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10/overview))
- **Central Package Management (`Directory.Packages.props`) is mandatory** for multi-project repositories; note NU1510 now flags pruned direct references. ([Breaking changes in .NET 10](https://learn.microsoft.com/dotnet/core/compatibility/10)) <!-- TODO: templates/Directory.Packages.props -->
- **Shared build settings live in `Directory.Build.props`** — TFM, `LangVersion` (latest), nullable enabled, analyzers on, warnings as errors. <!-- TODO: templates/Directory.Build.props -->
- **Run one-shot tools with `dotnet tool exec` / `dnx`** instead of installing them globally; note `dnx` bypasses `global.json` SDK selection. ([What's new in .NET 10 — SDK](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10/overview), [Breaking changes](https://learn.microsoft.com/dotnet/core/compatibility/10))
- **Use file-based apps (`dotnet run app.cs`) for scripts and samples** — they now support publish and NativeAOT; prefer them over scratch console projects. ([What's new in .NET 10 — SDK](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10/overview))
- **Target the latest release regardless of LTS/STS** (this repository's freshness policy); .NET 10 is LTS through November 2028.

<!-- TODO: full treatment — repo layout conventions (src/tests/docs), .editorconfig stance, containers (Ubuntu default base images in .NET 10) -->
