---
targets: [net10.0]
last-reviewed: 2026-08-20
last-used: 2026-08-20
sources: [ms-learn, dotnet-blog]
status: open
---

# UseArtifactsOutput and the artifacts output layout

**Adopt it.** Set `<UseArtifactsOutput>true</UseArtifactsOutput>` in the root `Directory.Build.props` of every new multi-project repository. It is a GA .NET 8+ SDK feature, still opt-in as of the .NET 10 SDK, and the last significant tooling hole — `dotnet ef` — was closed in EF Core 10.0.0. The repository has no opinion on it today, which is the notable finding: `templates/Directory.Build.props` is exactly the file the property belongs in, and it isn't there.

## What it does

All build output from all projects is gathered under one root instead of a `bin/`+`obj/` pair per project directory ([ms-learn, Tier 1](https://learn.microsoft.com/dotnet/core/sdk/artifacts-output)):

```text
artifacts/<type>/<project>/<pivot>
```

- **type** — `bin`, `obj`, `publish`, `package`
- **project** — MSBuild project name; overridable per project with `ArtifactsProjectName`. Omitted for `package`.
- **pivot** — lowercase configuration, joined by `_` to the TFM and RID when those disambiguate; overridable with `ArtifactsPivots`. For `package`, configuration only.

Verified locally against SDK 10.0.400 on 2026-08-20 — a single-TFM class library produced `artifacts/bin/App/release`, `artifacts/obj/App/release`, `artifacts/publish/App/release`, and `artifacts/package/release/App.1.0.0.nupkg`; retargeting it to `net9.0;net10.0` produced `artifacts/bin/App/debug_net9.0` and `.../debug_net10.0`. Note the pivot carries no TFM at all in the single-TFM case, which is a real difference from `bin/Release/net10.0/` and the thing most path-hardcoding breaks on.

The motivation is tooling, not tidiness: the .NET team's stated problem was that the per-project layout "can change drastically via relatively simple MSBuild changes" and is therefore "difficult for tools to anticipate" ([dotnet-blog, Tier 1 — .NET 8 Preview 3](https://devblogs.microsoft.com/dotnet/announcing-dotnet-8-preview-3/)). Preview 3 defaulted the root to `.artifacts`; the shipped default is `artifacts` (**non-roster, first-party** — [dotnet/sdk#31955](https://github.com/dotnet/sdk/pull/31955)). Treat the Preview 3 post as rationale only, not as current path guidance.

## Configuration

```xml
<!-- Directory.Build.props at the repository root — nowhere else -->
<PropertyGroup>
  <UseArtifactsOutput>true</UseArtifactsOutput>
</PropertyGroup>
```

`ArtifactsPath` moves the root (`$(MSBuildThisFileDirectory)artifacts` by convention) and setting it implies opt-in on its own. `dotnet new buildprops --use-artifacts` scaffolds the file for a repository that has none ([ms-learn, Tier 1](https://learn.microsoft.com/dotnet/core/sdk/artifacts-output)).

## Constraints worth knowing before adopting

- **It cannot live in a `.csproj`.** Both properties are read before project-file evaluation; setting either in a project fails the build with `NETSDK1199` — "cannot be set in a project file, due to MSBuild ordering constraints" (reproduced locally on SDK 10.0.400, 2026-08-20). `Directory.Build.props` or the command line, nothing else. It is therefore genuinely all-or-nothing per `Directory.Build.props` cone; a project outside that cone keeps the old layout silently.
- **Opting a single project back out is not supported cleanly.** Enabling in `Directory.Build.props` and disabling further down reportedly breaks `dotnet` commands with an `MSB5029` wildcard-enumeration warning (**non-roster** — [dotnet/sdk#45953](https://github.com/dotnet/sdk/issues/45953)). Don't design around per-project opt-out.
- **EF Core tooling is fixed, recently.** `dotnet ef migrations add` failed with "The target 'GetEFProjectMetadata' does not exist in the project" under the artifacts layout from 2023 until the fix landed for the **10.0.0** milestone (**non-roster, first-party** — [dotnet/efcore#30725](https://github.com/dotnet/efcore/issues/30725)). This is a direct argument for adopting on .NET 10 rather than having adopted on 8 or 9; on older SDKs the workaround was `--msbuildprojectextensionspath`.
- **Everything that hardcodes `bin/<Config>/<tfm>` must be updated in the same change** — Dockerfiles, CI copy steps, scripts, `COPY --from=build` lines. Not a blocker, but it makes adoption a repository-wide commit rather than a one-line one.
- **`.gitignore` already covers it.** The SDK template emits `artifacts/` (verified in `dotnet new gitignore` output from SDK 10.0.400, 2026-08-20), so the existing `project-structure.md` rule — generate the ignore file, don't hand-maintain it — needs no amendment. This repository's own root `.gitignore` already ignores `artifacts/`.

## Still opt-in

It is not the default for new projects on the .NET 10 SDK and there is no announced plan making it so. Adopting it is a deliberate house choice, not the path of least resistance — which is exactly the kind of call this repository exists to make.

## What this reveals about the repository

- **No opinion covers it.** `opinions/project-structure.md` §"Repository layout" and `templates/Directory.Build.props` are the two places it belongs, and neither mentions `artifacts`, `UseArtifactsOutput`, or `ArtifactsPath`. Recommend promoting this research topic: a bullet under "Repository layout" (the layout claim) plus the property in the template's existing `Directory.Build.props`. Both claims trace to `ms-learn` and `dotnet-blog`, so both are promotable without further vetting; the GitHub-issue caveats are supporting colour, not opinion text.
- **`opinions/ci.md` uses the word "artifact" for a different thing** — CI upload artifacts, and a `dotnet publish --output ./artifacts` example whose path would collide with the SDK's root if this is adopted. Promotion should fix that example (`--no-build` publish already writes to `artifacts/publish/<Project>/release` under the layout, so the `--output` flag becomes unnecessary rather than merely renamed).
- **No `vet-source` candidate came out of this.** The two Tier 1 roster sources carried the research; the supporting material was first-party GitHub issues, which are not blog-shaped sources and shouldn't be proposed for the roster.
