---
targets: [net10.0]
last-reviewed: 2026-08-12
last-used: 2026-08-14
sources: [meziantou, ms-learn, house]
---

# CI & automation

Supply-chain hygiene is not optional in the agentic era.

## Opinions

- **Pin GitHub Actions to commit SHAs, not mutable tags** — tags move silently; SHAs don't. Automate the sweep across repositories. ([Meziantou — SHA pinning](https://www.meziantou.net/enable-sha-pinning-for-github-actions-across-personal-repositories.htm))
- **Never interpolate user-provided input into workflow scripts** — pass it via environment variables and parse deliberately (script-injection is the top Actions vulnerability). ([Meziantou — Safely passing extra arguments](https://www.meziantou.net/safely-passing-extra-arguments-in-github-actions-workflows-using-powershell.htm))
- **CI builds are pinned and reproducible:** `global.json` decides the SDK, lock files or CPM decide packages — a CI run must not float versions the repo didn't choose.
- **House:** Warnings are treated as errors everywhere, not only in CI — a warning discovered on the build machine and not the workstation is one that shipped a day late. Suppressing a warning — `#pragma warning disable`, `[SuppressMessage]`, or a `<NoWarn>` entry — always carries an inline comment stating why; an unexplained suppression is reverted on sight, since the reviewer should never have to reconstruct the reason.

## Pipeline shape

**One pipeline, four explicit stages — restore, build, test, publish — each forbidding the previous stage's work.** `--no-restore` on build and `--no-build` on test/publish guarantee every stage runs against exactly the outputs of the one before it, instead of silently rebuilding with different flags. The shape is platform-agnostic; GitHub Actions is the worked example here because that is where the cited guidance lives.

- **Restore** with `--locked-mode` so a lock-file drift fails the build rather than floating a version.
- **Build** once, in `Release`, with `-warnaserror` — keep the switch even though the template sets `TreatWarningsAsErrors`: the property covers compiler and analyzer diagnostics, while the switch also promotes MSBuild engine warnings (e.g. MSB3277 assembly-version conflicts) that the property leaves as warnings. **House:** `TreatWarningsAsErrors` is set unconditionally in [templates/Directory.Build.props](../templates/Directory.Build.props), so those warnings fail the build on every workstation, not only in CI — this file previously recommended CI-only enforcement to keep local iteration fluid, and the house rule supersedes it, because a build that is only red in CI is a warning that already reached a PR. Enforce style the same way: analyzers in the build, `dotnet format --verify-no-changes` as a step. ([Meziantou — Enforce .NET code style in CI](https://www.meziantou.net/enforce-dotnet-code-style-in-ci-with-dotnet-format.htm), [Meziantou — The Roslyn analyzers I use](https://www.meziantou.net/the-roslyn-analyzers-i-use.htm))
- **Test** via `dotnet test` on Microsoft.Testing.Platform (native in the .NET 10 SDK — see [testing](testing.md)); publish TRX and coverage as build artifacts so failures are diagnosable without a rerun. ([What's new in .NET 10 — SDK](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10/overview))
- **Publish/pack** with `--no-build` and upload the output as the single artifact that later stages (deploy, release) consume — never rebuild for deployment.

```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@08c6903cd8c0fde910a37f88322edcfb5dd907a8 # v5.0.0
      - uses: actions/setup-dotnet@67a3573c9a986a3f9c594539f4ab511d57bb3ce9 # v4.3.1
        with:
          global-json-file: global.json
      - run: dotnet restore --locked-mode
      - run: dotnet build --no-restore --configuration Release -warnaserror
      - run: dotnet test --no-build --configuration Release
      - run: dotnet publish --no-build --configuration Release --output ./artifacts
      - uses: actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02 # v4.6.2
        with:
          name: app
          path: ./artifacts
```

(SHAs shown are illustrative of the _pinning shape_ — resolve the current release SHA when copying.)

## Artifact signing

**Spend your signing budget on provenance and credential hygiene, not author-signing ceremony.** For NuGet packages: ship deterministic builds with Source Link, symbols, and package validation enabled; publish with a least-privilege, short-lived credential (trusted publishing / scoped API key stored as a repository secret) rather than a long-lived org-wide key. ([Meziantou — Publishing a NuGet package using GitHub Actions](https://www.meziantou.net/publishing-a-nuget-package-following-best-practices-using-github.htm)) Author-sign packages only if your organization already operates certificate infrastructure — nuget.org repository-signs everything it serves, so for most publishers author signing adds cost without a consumer who verifies it. ([Microsoft Learn — Sign a NuGet package](https://learn.microsoft.com/nuget/create-packages/sign-a-package)) On the consuming side, pinning is the protection that pays: locked restore, pinned SDK, SHA-pinned actions.

## Dependency updates

**Use Renovate.** One bot, one shared config preset reused across every repository, and it updates the things .NET repos actually pin: NuGet packages (grouped, with lock-file maintenance), the SDK version in `global.json`, `.nuspec` dependencies, and the action SHAs the pinning opinion above creates. ([Meziantou — Sharing the Renovate configuration across multiple projects](https://www.meziantou.net/sharing-the-renovate-configuration-across-multiple-projects.htm), [Meziantou — Update dependencies in nuspec with Renovate](https://www.meziantou.net/update-dependencies-in-nuspec-file-using-renovate.htm)) Dependabot lost on one axis: no cross-repository shared configuration, so every repo drifts its own policy. Whatever the bot, updates merge only through the same pipeline above — an update PR that skips `--locked-mode` restore and tests is supply-chain exposure, not hygiene.
