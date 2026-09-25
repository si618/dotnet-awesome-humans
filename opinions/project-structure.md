---
targets: [net10.0, csharp-14, fsharp-10]
last-reviewed: 2026-08-21
last-used: 2026-09-25
sources: [ms-learn, dotnet-blog, gerald-versluis, steve-gordon, house]
---

# Project structure & SDK

One solution format, central package management, versions pinned at the root. The `templates/` directory encodes these opinions as copy-paste-ready files: copy them verbatim and trim, rather than authoring from scratch.

## Solution & SDK

- **Use `.slnx` for new solutions:** `dotnet new sln` defaults to it in the .NET 10 SDK; migrate `.sln` files opportunistically. Use `.slnf` filters for large solutions. Start from [templates/example.slnx](../templates/example.slnx) and [templates/example.slnf](../templates/example.slnf). ([Microsoft Learn: Breaking changes in .NET 10](https://learn.microsoft.com/dotnet/core/compatibility/10))
- **Pin the SDK with `global.json`** (roll-forward `latestFeature`, per [templates/global.json](../templates/global.json)). ([Microsoft Learn: What's new in .NET 10](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10/overview))
- **The pinned version is a floor, not a selection, so raise it only when you need a newer feature band.** Under `rollForward: latestFeature`, `10.0.400` means that version or any later band and patch installed on the machine, so a new band reaches the build without the file changing. Pinning a patch (`10.0.412`) or chasing each band as it ships narrows who can build and fails contributors one servicing release behind, buying nothing. Move the floor when a band ships something the repository actually uses, and record which feature in the commit. ([Microsoft Learn: global.json overview](https://learn.microsoft.com/dotnet/core/tools/global-json))
- **Trial a preview SDK with `sdk.paths`, and ignore the folder it installs into in the same commit.** `./dotnet-install.sh --install-dir .dotnet` (`-InstallDir` on the PowerShell script) puts the preview SDK inside the repository rather than on the machine, and `paths` makes the host prefer it while still falling back to the machine install via `$host$`. Paths resolve relative to `global.json`, not the working directory, so this holds from any subdirectory; `errorMessage` is what a contributor who has neither SDK sees instead of a bare version error. Two limits: `paths` applies only to commands that engage the SDK (`dotnet build`, `dotnet run`) and is ignored by the apphost and `dotnet app.dll`; and the folder is a full SDK, hundreds of megabytes that must never be committed. ([Versluis: Test .NET MAUI Preview SDKs Locally with global.json sdk.paths](https://blog.verslu.is/maui/test-dotnet-maui-preview-sdk-locally/), [Microsoft Learn: test prerelease SDKs locally](https://learn.microsoft.com/dotnet/core/tools/test-prerelease-sdk-locally))

  ```json
  {
    "sdk": {
      "version": "11.0.100-preview.1.25000.1",
      "paths": [".dotnet", "$host$"],
      "errorMessage": "Run ./dotnet-install.sh --install-dir .dotnet --version 11.0.100-preview.1.25000.1"
    }
  }
  ```

  `dotnet new gitignore` does not cover `.dotnet/`, because the template ignores build output and a locally installed SDK is a build _prerequisite_. Every .NET repository that installs one this way adds the rule by hand (`dotnet/runtime`, `dotnet/aspnetcore`, `dotnet/sdk`, `dotnet/maui` all carry it). Adopting `sdk.paths` therefore means one line in the repo-specific block of `.gitignore` below, and it is not optional.

- **Use Central Package Management (`Directory.Packages.props`) in multi-project repositories:** per-project versions get harder to keep consistent as the project count grows. NU1510 flags pruned direct references. [templates/Directory.Packages.props](../templates/Directory.Packages.props) is the canonical starting point, including `CentralPackageTransitivePinningEnabled`. ([Microsoft Learn: Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management), [Microsoft Learn: Breaking changes in .NET 10](https://learn.microsoft.com/dotnet/core/compatibility/10))
- **Shared build settings live in `Directory.Build.props`:** TFM, `LangVersion` (latest), nullable enabled, analyzers on, warnings as errors. Copy [templates/Directory.Build.props](../templates/Directory.Build.props); the ideal project file is then nearly empty (see [templates/projects/](../templates/projects/)).
- **House:** the warnings-as-errors setting above is always on, never CI-only, and every suppression carries a comment giving the reason. The canonical statement is in [ci.md](ci.md).
- **Run one-shot tools with `dotnet tool exec` / `dnx`** instead of installing them globally; note `dnx` bypasses `global.json` SDK selection. ([What's new in .NET 10: SDK](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10/sdk), [Microsoft Learn: Breaking changes in .NET 10](https://learn.microsoft.com/dotnet/core/compatibility/10))
- **Use file-based apps (`dotnet run app.cs`) for scripts and samples:** they now support publish and NativeAOT; prefer them over scratch console projects. ([What's new in .NET 10: SDK](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10/sdk))
- **Target the latest release regardless of LTS/STS** (this repository's freshness policy); .NET 10 is LTS through November 2028. ([Microsoft Learn: .NET releases, patches, and support](https://learn.microsoft.com/dotnet/core/releases-and-support))

## Repository layout

- **`src/` for shipping code, `tests/` for test projects, build configuration at the root.** One project per directory, directory named after the assembly, and the solution mirrors the disk layout with `/src/` and `/tests/` solution folders. [templates/example.slnx](../templates/example.slnx) encodes exactly this shape. ([Microsoft Learn: Organize projects for .NET Framework and .NET](https://learn.microsoft.com/dotnet/core/porting/project-structure), [dotnet/samples: migrate-library-csproj](https://github.com/dotnet/samples/tree/main/framework/libraries/migrate-library-csproj))
- **House:** the solution also carries a `/build/` solution folder listing the root build files (`Directory.Build.props`, `Directory.Packages.props`, `global.json`, `.editorconfig`). They hold every project's settings, and listing them in the solution puts them in Solution Explorer beside the projects they configure. [templates/example.slnx](../templates/example.slnx) includes it; [dotnet-skills: dotnet-project-structure](https://agentskills.so/skills/aaronontheweb-dotnet-skills-dotnet-project-structure) uses the same shape.
- **Enable the artifacts output layout: `<UseArtifactsOutput>true</UseArtifactsOutput>` in the root `Directory.Build.props`.** All build output from all projects lands under one `artifacts/<type>/<project>/<pivot>` root instead of a `bin/`+`obj/` pair per project directory. The types are `bin`, `obj`, `publish` and `package`, and the package type omits the project segment, so nupkgs land at `artifacts/package/<configuration>`. Tools and CI steps can rely on that layout, where the per-project one "can change drastically via relatively simple MSBuild changes". GA since .NET 8 but still opt-in on the .NET 10 SDK, so adopting it is a deliberate choice; [templates/Directory.Build.props](../templates/Directory.Build.props) makes it. Two things to know when adopting: MSBuild reads the property before project evaluation, so it works only from `Directory.Build.props` or the command line, and setting it in a project file fails the build with `NETSDK1199`; and the pivot is the lowercase configuration alone for a single-TFM project (`artifacts/bin/App/release`, no TFM segment), so every hardcoded `bin/<Config>/<tfm>` path in Dockerfiles, CI copy steps and scripts must change in the same commit. `dotnet new gitignore` already ignores `artifacts/`. ([Microsoft Learn: Artifacts output layout](https://learn.microsoft.com/dotnet/core/sdk/artifacts-output), [.NET Blog: Announcing .NET 8 Preview 3](https://devblogs.microsoft.com/dotnet/announcing-dotnet-8-preview-3/))
- **Root files are the contract.** `global.json`, `Directory.Build.props`, `Directory.Packages.props`, and `.editorconfig` live at the repository root so every project inherits them with no per-project setup: MSBuild walks up from each project to the nearest `Directory.Build.props`, and Central Package Management reads `Directory.Packages.props` the same way. ([Microsoft Learn: Customize the build by folder](https://learn.microsoft.com/visualstudio/msbuild/customize-by-directory), [Microsoft Learn: Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management))
- **Test projects sit beside, never inside, the code under test:** `tests/Example.Library.Tests` mirrors `src/Example.Library` and is named `<Project>.Tests`, as in the same sample; [opinions/testing.md](testing.md) covers what goes in them.
- **Inside a project, group by feature, not by pattern:** see [architecture.md](architecture.md) for how modules and vertical slices sit under this layout.
- **Don't add layout you don't need yet.** A single-project tool is fine as `src/Tool` plus `tests/Tool.Tests`; add `docs/` and solution filters when the repository needs them, not on day one. ([Microsoft Learn: Organizing and testing projects with the .NET CLI](https://learn.microsoft.com/dotnet/core/tutorials/testing-with-cli))
- **Generate `.gitignore` with `dotnet new gitignore`, and don't hand-maintain one.** The SDK template is the canonical .NET ignore set and evolves with the toolchain; a hand-rolled copy (or a copy-paste from another repo) drifts, which is why this repository deliberately ships no `.gitignore` template. Regenerate after major SDK upgrades; keep any repo-specific additions in a clearly marked block at the bottom so regeneration is a safe overwrite-above-the-line. `.dotnet/` from an `sdk.paths` install is exactly such an addition. ([Microsoft Learn: dotnet new gitignore](https://learn.microsoft.com/dotnet/core/tools/dotnet-new-sdk-templates))

## .editorconfig

- **Every repository carries a root `.editorconfig`, and [templates/.editorconfig](../templates/.editorconfig) is the canonical one.** Copy it verbatim and trim rules you disagree with, but disagree deliberately. It encodes the house style: file-scoped namespaces, expression-bodied members where they fit on a line, collection expressions, auto-implemented properties (IDE0032), and standard .NET naming, with analyzer diagnostics defaulted to `warning`.
- **Style is enforced by the build, not by reviewers.** `.editorconfig` severities only fail the build because [templates/Directory.Build.props](../templates/Directory.Build.props) sets `EnforceCodeStyleInBuild` and `TreatWarningsAsErrors`. Adopt the pair together, or the style file is documentation, not enforcement.
- **One `.editorconfig` at the root, not one per project.** Nested files are for real exceptions (e.g. relaxing doc-comment rules under `tests/`), and each nested file should contain only the delta: for a key set in both, the file deeper in the tree wins. ([Microsoft Learn: Configuration files for code analysis rules](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/configuration-files))

## Containers

- **Build images with the SDK (`dotnet publish /t:PublishContainer`), not a hand-written Dockerfile.** The SDK produces the image directly, with no Dockerfile to drift out of sync with the project, and pushes to the local Docker/Podman daemon by default, a registry via `ContainerRegistry`, or a tarball via `ContainerArchiveOutputPath`. In .NET 10 this covers console apps natively too: `<EnableSdkContainerSupport>` is no longer required, aligning them with ASP.NET Core and Worker apps. ([Microsoft Learn: Containerize a .NET app with dotnet publish](https://learn.microsoft.com/dotnet/core/containers/sdk-publish), [Microsoft Learn: What's new in the .NET 10 SDK](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10/sdk))

  ```xml
  <!-- All container config is MSBuild properties; illustrative values -->
  <PropertyGroup Label="Container image">
    <ContainerRepository>contoso/example-worker</ContainerRepository>
    <ContainerImageTags>1.4.0;latest</ContainerImageTags>
    <ContainerFamily>noble-chiseled</ContainerFamily>
  </PropertyGroup>
  ```

- **Know that .NET 10 base images are Ubuntu, not Debian.** The version-only tags (`mcr.microsoft.com/dotnet/aspnet:10.0`) now resolve to Ubuntu 24.04 "Noble", and Microsoft ships no Debian images for .NET 10, so there is no tag to opt back into. If image size matters, prefer the chiseled variants via `ContainerFamily` (e.g. `noble-chiseled`) or Alpine (`alpine`) over anything hand-rolled. ([Microsoft Learn: Default .NET container tags now use Ubuntu](https://learn.microsoft.com/dotnet/core/compatibility/containers/10.0/default-images-use-ubuntu))
- **Take the `-extra` tag if the app is not invariant.** The chiseled and Alpine variants recommended above ship neither ICU nor `tzdata`, and they set `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true` in the image. An app that names a culture therefore throws `CultureNotFoundException` at startup and one that only reads `CurrentCulture` formats and compares as invariant without complaint. A project-file `<InvariantGlobalization>false</InvariantGlobalization>` will not override an environment variable. `noble-chiseled-extra` and `alpine-extra` restore both. ([globalization.md](globalization.md))
- **Keep the rootless default.** Linux images run as the non-root `app` user (since .NET 8) and `ContainerPort` is inferred from `ASPNETCORE_URLS`, `ASPNETCORE_HTTP_PORTS` or `ASPNETCORE_HTTPS_PORTS`; don't set `ContainerUser` to `root` or re-expose privileged ports to make a broken volume mount work: fix the mount. ([Microsoft Learn: Containerize a .NET app reference](https://learn.microsoft.com/dotnet/core/containers/publish-configuration))
