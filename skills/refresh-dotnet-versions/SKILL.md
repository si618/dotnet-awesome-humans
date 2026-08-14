---
name: refresh-dotnet-versions
description: Detect new .NET, C#, and F# releases and update all opinions and templates in this repository to target them, including the central NuGet package pins in templates/Directory.Packages.props. Use when a new .NET SDK/runtime, C# language version, or F# version has shipped, when a pinned package has a newer stable release, or when asked to check whether the repository targets the latest released versions.
license: See repository LICENSE
compatibility: Requires git and internet access
metadata:
  repo: dotnet-awesome-humans
  change-flow: branch-pr
---

# Refresh .NET versions

Bring every resource in this repository up to the **latest released** (GA, not preview) versions of .NET, C#, and F#, and the **latest stable** versions of the NuGet packages pinned in `templates/Directory.Packages.props`.

## Trigger

The `dotnet-release-watch` GitHub Action (`.github/workflows/dotnet-release-watch.yml`) polls the official releases index daily and opens a **trigger PR** (label `dotnet-release`, branch `automation/dotnet-release-watch`) whenever the GA state changes. That PR only updates the snapshot at `.github/state/dotnet-releases.json` — running this skill against the PR's branch produces the actual update, and both merge together. The skill can also be run standalone; the snapshot doubles as a fast offline answer to "what GA versions does this repository currently know about?".

## Orchestration

Where the host supports worker agents, delegate the web lookups (step 1's version checks, step 4a/4b's SDK-band and nuget.org resolutions, step 5's "What's new" reading) to **lower-cost worker agents** in parallel — they return raw findings only. The orchestrating model acts as the **editor**: it alone decides what changes and edits `opinions/` and `templates/`.

## Steps

1. **Determine the latest released versions.**
   - .NET: check <https://dotnet.microsoft.com/download/dotnet> and the release announcement on the .NET Blog (<https://devblogs.microsoft.com/dotnet/>).
   - C#: check <https://learn.microsoft.com/dotnet/csharp/whats-new/> for the latest released language version.
   - F#: check <https://learn.microsoft.com/dotnet/fsharp/whats-new/>.
   - GA releases only. Previews and RCs never become the target; they may be noted in a "coming next" aside.
2. **Compare against the repository's current targets.** Grep the `targets:` frontmatter across `opinions/` and the version pins in `templates/` (`global.json` SDK version, `<TargetFramework>` in project files, `<LangVersion>` if pinned). If everything already matches, the framework half of this run is a no-op — say so, but **still run steps 4a and 4b**, which drift on their own schedule. Only report "up to date" and stop once those also come back clean.
3. **Create a working branch** (never commit to the default branch): `git switch -c <user-prefix>/refresh-dotnet-<version>`. Respect any branch-naming convention in the host environment's instructions.
4. **Update template files** under `templates/`: TFMs in exemplar projects and anything version-pinned. Then the two pins that drift independently of a .NET release:
   - **4a. `templates/global.json` SDK band.** Pin `sdk.version` to the **latest released SDK feature band** (e.g. `10.0.400`), not a patch-level build — with `"rollForward": "latestFeature"` the band is the floor, and pinning a patch needlessly fails machines one servicing release behind. Prefer `latestFeature` roll-forward wherever the opinion says so. `global.json` cannot carry a comment header, so it has no `last-reviewed` of its own: the pinned value **is** the record, and step 8's PR description must state what it was checked against.
   - **4b. `templates/Directory.Packages.props` package pins.** Resolve every `<PackageVersion>` against nuget.org — the registration index (`https://api.nuget.org/v3/registration5-gz-semver2/<lowercased-id>/index.json`) or `dotnet package search <id>`. Take the **latest stable** version only; never a prerelease, and never a version whose own TFM support drops below the repository's declared `targets:`. Where a package major bumps, read its release notes before taking it — a major that changes the idiom an opinion teaches is a content change, not a version bump, and belongs in step 5. Refresh the `Versions verified against nuget.org … on <date>` header comment to the run date **whether or not any version changed** — that comment is the only freshness record the file has.

5. **Update each opinion file** under `opinions/`:
   - Read the release's "What's new" from Tier 1 sources in [AWESOME-HUMANS.md](../../AWESOME-HUMANS.md) (`dotnet-blog`, `ms-learn`) and per-feature deep-dives from Tier 2 (`andrew-lock`'s "Exploring .NET" series when available).
   - Fold in new language/runtime features where they change an existing opinion (e.g. a new syntax supersedes an old idiom). New features that warrant a brand-new opinion get a stub with a `TODO` and a source link.
   - Update `targets:` and `last-reviewed:` frontmatter on every file touched **or verified unchanged**.
   - **Never remove or dilute `**House:**`-marked content** (`house` source id — see HOUSE-OPINIONS.md). Version bumps may update the sourced context around a house opinion, and may mark it superseded-by-release where a new version genuinely obsoletes it — but the marking and the owner's intent stay.
6. **Verify** code samples still compile against the new targets where a .NET SDK is available (`dotnet build` a scratch project); otherwise flag samples as unverified in the PR description.
7. **Record the change** in `AWESOME-HUMANS.md`'s decision log only if source rosters changed; otherwise just summarize in the PR.
8. **Open a PR** to the default branch describing: versions before/after (framework, SDK band, and each package pin that moved), opinions changed, opinions verified-unchanged, and anything left as TODO. A human reviews before it becomes "the opinion".

## Edge cases

- A new major .NET version ships every November; C# ships with it. Mid-year, expect only servicing releases — a no-op for the framework targets, but **not** for steps 4a and 4b: SDK feature bands and package pins move on their own cadence, so a mid-year run that changes only `global.json` and `Directory.Packages.props` is a normal, expected outcome.
- A package pin whose latest stable release predates the repository's current `targets:` by a wide margin is a signal the library is unmaintained — leave the pin alone and raise it for `vet-source`/opinion review rather than silently churning the version.
- If a feature is GA in the runtime but the guidance from vetted sources hasn't caught up, update the target version but keep the old idiom with a note, rather than inventing unsourced guidance.
- STS vs LTS: target the **latest release** regardless of support track; note the support horizon in `opinions/project-structure.md`.
