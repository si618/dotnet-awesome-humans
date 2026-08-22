---
targets: [net10.0, csharp-14, fsharp-10]
last-reviewed: 2026-08-21
last-used: 2026-08-21
sources: [ms-learn, dotnet-blog, gerald-versluis, steve-gordon, house]
---

# Project structure & SDK

One solution format, central package management, versions pinned at the root. The `templates/` directory encodes these opinions as copy-paste-ready files — copy them verbatim and trim, rather than authoring from scratch.

## Solution & SDK

- **Use `.slnx` for new solutions:** `dotnet new sln` defaults to it in the .NET 10 SDK; migrate `.sln` files opportunistically. Use `.slnf` filters for large solutions. Start from [templates/example.slnx](../templates/example.slnx) and [templates/example.slnf](../templates/example.slnf). ([Breaking changes in .NET 10](https://learn.microsoft.com/dotnet/core/compatibility/10))
- **Pin the SDK with `global.json`** (roll-forward `latestFeature`, per [templates/global.json](../templates/global.json)); use `sdk.paths` to trial preview SDKs per-repo without touching the machine. ([Versluis: sdk.paths](https://blog.verslu.is/), [What's new in .NET 10](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10/overview))
- **Central Package Management (`Directory.Packages.props`) is mandatory** for multi-project repositories; note NU1510 now flags pruned direct references. [templates/Directory.Packages.props](../templates/Directory.Packages.props) is the canonical starting point, including `CentralPackageTransitivePinningEnabled`. ([Breaking changes in .NET 10](https://learn.microsoft.com/dotnet/core/compatibility/10))
- **Shared build settings live in `Directory.Build.props`:** TFM, `LangVersion` (latest), nullable enabled, analyzers on, warnings as errors. Copy [templates/Directory.Build.props](../templates/Directory.Build.props); the ideal project file is then nearly empty (see [templates/projects/](../templates/projects/)).
- **House:** the warnings-as-errors setting above is always on, never CI-only, and every suppression carries a comment giving the reason — canonical statement in [ci.md](ci.md).
- **Run one-shot tools with `dotnet tool exec` / `dnx`** instead of installing them globally; note `dnx` bypasses `global.json` SDK selection. ([What's new in .NET 10: SDK](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10/sdk), [Breaking changes](https://learn.microsoft.com/dotnet/core/compatibility/10))
- **Use file-based apps (`dotnet run app.cs`) for scripts and samples:** they now support publish and NativeAOT; prefer them over scratch console projects. ([What's new in .NET 10: SDK](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10/sdk))
- **Target the latest release regardless of LTS/STS** (this repository's freshness policy); .NET 10 is LTS through November 2028.

## Repository layout

- **`src/` for shipping code, `tests/` for test projects, `docs/` for long-form documentation, build configuration at the root.** One project per directory, directory named after the assembly, and the solution mirrors the disk layout with `/src/`, `/tests/`, and `/build/` solution folders — [templates/example.slnx](../templates/example.slnx) encodes exactly this shape.
- **Enable the artifacts output layout: `<UseArtifactsOutput>true</UseArtifactsOutput>` in the root `Directory.Build.props`.** All build output from all projects lands under one `artifacts/<type>/<project>/<pivot>` root (`bin`, `obj`, `publish`, `package` — though the package type omits the project segment: nupkgs land at `artifacts/package/<configuration>`) instead of a `bin/`+`obj/` pair per project directory — a layout tools and CI steps can rely on, where the per-project layout "can change drastically via relatively simple MSBuild changes". GA since .NET 8 but still opt-in on the .NET 10 SDK, so adopting it is a deliberate choice; [templates/Directory.Build.props](../templates/Directory.Build.props) makes it. Two things to know when adopting: MSBuild reads the property before project evaluation, so it works only from `Directory.Build.props` or the command line — setting it in a project file fails the build with `NETSDK1199`; and the pivot is the lowercase configuration alone for a single-TFM project (`artifacts/bin/App/release`, no TFM segment), so every hardcoded `bin/<Config>/<tfm>` path — Dockerfiles, CI copy steps, scripts — must change in the same commit. `dotnet new gitignore` already ignores `artifacts/`. ([Microsoft Learn: Artifacts output layout](https://learn.microsoft.com/dotnet/core/sdk/artifacts-output), [Announcing .NET 8 Preview 3](https://devblogs.microsoft.com/dotnet/announcing-dotnet-8-preview-3/))
- **Root files are the contract.** `global.json`, `Directory.Build.props`, `Directory.Packages.props`, and `.editorconfig` live at the repository root so every project inherits them with zero per-project ceremony; surface them in the solution's `/build/` folder so they get reviewed, not forgotten.
- **Test projects sit beside, never inside, the code under test:** `tests/Example.Library.Tests` mirrors `src/Example.Library` and is named `<Project>.Tests`. This keeps packing, coverage filters, and `.slnf` filters trivial (see [opinions/testing.md](testing.md) for what goes in them).
- **Inside a project, group by feature, not by pattern:** see [architecture.md](architecture.md) for how modules and vertical slices sit under this layout.
- **Don't add layout you don't need yet.** A single-project tool is fine as `src/Tool` plus `tests/Tool.Tests`; add `docs/` and solution filters when the repository earns them, not on day one.
- **Generate `.gitignore` with `dotnet new gitignore` — don't hand-maintain one.** The SDK template is the canonical .NET ignore set and evolves with the toolchain; a hand-rolled copy (or a copy-paste from another repo) drifts, which is why this repository deliberately ships no `.gitignore` template. Regenerate after major SDK upgrades; keep any repo-specific additions in a clearly marked block at the bottom so regeneration is a safe overwrite-above-the-line. ([Microsoft Learn: dotnet new gitignore](https://learn.microsoft.com/dotnet/core/tools/dotnet-new-sdk-templates))

## .editorconfig

- **Every repository carries a root `.editorconfig`, and [templates/.editorconfig](../templates/.editorconfig) is the canonical one** — copy it verbatim and trim rules you disagree with, but disagree deliberately. It encodes the house style: file-scoped namespaces, expression-bodied members where they fit on a line, collection expressions, C# 14 field-backed properties (IDE0032), and standard .NET naming, with analyzer diagnostics defaulted to `warning`.
- **Style is enforced by the build, not by reviewers.** `.editorconfig` severities only bite because [templates/Directory.Build.props](../templates/Directory.Build.props) sets `EnforceCodeStyleInBuild` and `TreatWarningsAsErrors` — adopt the pair together, or the style file is documentation, not enforcement.
- **One `.editorconfig` at the root, not one per project.** Nested files are for genuine exceptions (e.g. relaxing doc-comment rules under `tests/`), and each nested file should contain only the delta.

## Containers

- **Build images with the SDK (`dotnet publish /t:PublishContainer`), not a hand-written Dockerfile.** The SDK produces the image directly — no Dockerfile to drift out of sync with the project — and pushes to the local Docker/Podman daemon by default, a registry via `ContainerRegistry`, or a tarball via `ContainerArchiveOutputPath`. In .NET 10 this covers console apps natively too: `<EnableSdkContainerSupport>` is no longer required, aligning them with ASP.NET Core and Worker apps. ([Containerize a .NET app with dotnet publish](https://learn.microsoft.com/dotnet/core/containers/sdk-publish), [What's new in the .NET 10 SDK](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10/sdk))

  ```xml
  <!-- All container config is MSBuild properties; illustrative values -->
  <PropertyGroup Label="Container image">
    <ContainerRepository>contoso/example-worker</ContainerRepository>
    <ContainerImageTags>1.4.0;latest</ContainerImageTags>
    <ContainerFamily>noble-chiseled</ContainerFamily>
  </PropertyGroup>
  ```

- **Know that .NET 10 base images are Ubuntu, not Debian.** The version-only tags (`mcr.microsoft.com/dotnet/aspnet:10.0`) now resolve to Ubuntu 24.04 "Noble", and Microsoft ships no Debian images for .NET 10 — there is no tag to opt back into. If image size matters, prefer the chiseled variants via `ContainerFamily` (e.g. `noble-chiseled`) or Alpine (`alpine`) over anything hand-rolled. ([Default .NET container tags now use Ubuntu](https://learn.microsoft.com/dotnet/core/compatibility/containers/10.0/default-images-use-ubuntu))
- **Take the `-extra` tag if the app is not invariant.** The chiseled and Alpine variants recommended above ship neither ICU nor `tzdata`, and they set `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true` in the image — so an app that names a culture throws `CultureNotFoundException` at startup and one that only reads `CurrentCulture` formats and compares as invariant without complaint. A project-file `<InvariantGlobalization>false</InvariantGlobalization>` will not override an environment variable. `noble-chiseled-extra` and `alpine-extra` restore both. ([globalisation.md](globalisation.md))
- **Keep the rootless default.** Linux images run as the non-root `app` user (since .NET 8) and `ContainerPort` is inferred from `ASPNETCORE_HTTP_PORTS`; don't set `ContainerUser` to `root` or re-expose privileged ports to make a broken volume mount work — fix the mount. ([Containerize a .NET app reference](https://learn.microsoft.com/dotnet/core/containers/publish-configuration))
