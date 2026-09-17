---
targets: [net10.0]
last-reviewed: 2026-09-17
sources: [house, meziantou]
---

# `just` as this repository's command runner

**No.** The problem `just` would be adopted to solve is real, but it is a coverage problem rather than a runner problem, and `just` is the most expensive way to fix it.

Six of this repository's eight CI gates have no documented local entry point.
A contributor or agent who runs the documented check, `npm run check`, sees green while the metadata, sources, README-index, skills-spec and script-permission gates are still unrun.
Closing that gap needs one command that runs everything; it does not need a fourth toolchain.
The cheapest fix that fits the conventions already written down in [AGENTS.md](../AGENTS.md) is a `scripts/check.cs` orchestrator, because [AGENTS.md](../AGENTS.md) already says checks live in `scripts/` as .NET file-based apps.

Revisit this if the repository starts building the example projects under `templates/`, which would multiply the task count and change the arithmetic below.

## What runs today

Two workflows carry eight steps between them.

| Gate                                   | Command                                       | Local entry point |
| -------------------------------------- | --------------------------------------------- | ----------------- |
| Markdown formatting                    | `npm run format:check`                        | `npm run check`   |
| Markdown lint                          | `npm run lint`                                | `npm run check`   |
| Shebang ↔ executable bit               | inline bash in `validate-skills.yml`          | none              |
| Agent Skills spec                      | `agentskills validate skills/<name>/`         | none              |
| Resource metadata                      | `dotnet run scripts/validate-metadata.cs`     | none              |
| Roster and cited sources               | `dotnet run scripts/validate-sources.cs`      | none              |
| README indexes every opinion and skill | `dotnet run scripts/validate-readme-index.cs` | none              |
| Link health                            | `lychee --config lychee.toml`                 | none              |

[AGENTS.md](../AGENTS.md) documents `npm run format` and `npm run check`, and nothing else.
The three `dotnet run scripts/*.cs` invocations appear in [README.md: Repository scripts](../README.md#repository-scripts), so they are discoverable, but they sit outside the command a contributor is told to run before pushing.
That is the gap worth closing, and it is worth closing whichever tool wins.

Three toolchains already have to be present for the full set: Node for Prettier and markdownlint, .NET for the validators, Python for `agentskills`.
Only Node and .NET are pinned by something the repository commits — `package-lock.json` and `global.json`.

## What `just` is, as of 2026-09-17

A command runner written in Rust, reading recipes from a `justfile`.
It is explicitly not a build system: it tracks no timestamps and produces no artefacts, so every recipe behaves as `make`'s `.PHONY` targets do without the ceremony ([just: README](https://github.com/casey/just/blob/master/README.md)).

It is mature and actively maintained.
The first release was 0.3.1 in October 2017, 1.0.0 landed on 2022-02-22, and the current release is 1.58.0 of 2026-08-03 — 21 releases in the twelve months to September 2026 ([just: CHANGELOG.md](https://github.com/casey/just/blob/master/CHANGELOG.md)).
Maturity is not the objection.

Distribution is broad: winget (`Casey.Just`), Scoop and Chocolatey on Windows, Homebrew on macOS, `apt`, `dnf`, Nix and Snap on Linux, plus `cargo install just`, Conda, `uv`, `pipx` and asdf.
Pre-built binaries and a `SHA256SUMS` file accompany every release.
On GitHub Actions the documented routes are `extractions/setup-just` — which takes an optional `just-version` — and `taiki-e/install-action@just` ([just: README](https://github.com/casey/just/blob/master/README.md)).

Cross-platform recipes work, with deliberate effort.
`set shell` is chosen per platform through the `[windows]` and `[unix]` attributes; the older `set windows-shell` and `set windows-powershell` settings are deprecated in favour of that.
Features not yet stabilised are gated behind `--unstable`, `set unstable` or `JUST_UNSTABLE`, so a justfile can pin itself to the stable surface.

**Reference, not source.** No roster entry in [AWESOME-HUMANS.md](../AWESOME-HUMANS.md) covers general-purpose command runners, so every factual claim above comes from `just`'s own documentation and release notes, which are references under [AWESOME-HUMANS.md: Admission criteria](../AWESOME-HUMANS.md#admission-criteria). The judgement below rests on this repository's own opinions instead.

## What a justfile would buy here

`just --list` with doc comments and `[group]` attributes gives a self-describing verb list, which is a genuine improvement over `npm run`'s bare script names.
Recipes take parameters, so `just validate metadata` and `just validate` could share one definition.
Recipe dependencies express ordering without a shell chain.

None of that is worth much at this repository's size.
Every task listed above is a zero-argument one-liner, and there would be roughly eight recipes.
That is squarely in the band where a justfile is a second place to look rather than a simplification.

The discovery argument is also weaker here than it looks.
This repository's primary readers are agents, and agents discover commands by reading [AGENTS.md](../AGENTS.md), not by running `--list`.
A runner that makes commands discoverable to a human at a prompt solves a problem the repository has already solved in prose.

## What it would cost here

**A fourth toolchain, and the only one with no pinning story.**
`global.json` pins the SDK, `package-lock.json` pins the Node tools, `.github/requirements.txt` pins `agentskills`.
A `just` binary installed by winget, Homebrew or `apt` is whatever the contributor's package manager last shipped.
CI can pin it through `extractions/setup-just` with an explicit `just-version` and a SHA-pinned action, so CI is fine; the local half is not, and drift between a contributor's `just` and CI's is exactly the failure this repository's pinning opinion exists to prevent.

**The npm route trades one problem for a worse one.**
`rust-just` looks like the clean answer — a pinned devDependency, restored by the `npm ci` that already runs.
It is a third-party redistribution: published by `gnpaone` from `gnpaone/rust-just`, not by `just`'s author, and it pulls ten per-platform binary sub-packages.
It also lags, sitting at 1.57.0 of 2026-07-19 against upstream's 1.58.0.
[opinions/ci.md](../opinions/ci.md) leads with supply-chain hygiene and says to restrict what a package may run, so adding a republished binary distribution to get a task runner is the repository contradicting its own published guidance.

**Renovate would not see it.**
`renovate.json` opts `scripts/` into the nuget manager so `#:package` pins get bumped, and the Node and Python manifests are handled by their own managers.
A version pinned in a workflow's `just-version` input and a version documented for contributors would both be hand-maintained.

**It buys nothing the repository is short of.**
`just` earns its place where tasks are numerous, parameterised, or awkward to express in a shell chain.
Here they are eight sequential invocations of tools that are already installed.

## What closes the same gap more cheaply

**A `scripts/check.cs` file-based app.**
This is what [AGENTS.md](../AGENTS.md) already prescribes: "CI checks live in `scripts/` as .NET file-based apps ... Add a check there, not as an inline script in a workflow."
It is pinned by `global.json`, scanned by Renovate, and held to the repository's own analyzer bar through the `[scripts/*.cs]` block in `.editorconfig`.
It absorbs the inline shebang check currently embedded in `validate-skills.yml`, which [AGENTS.md](../AGENTS.md) says should not be there.
The cost is process-spawning boilerplate to shell out to `npm`, `pip` and `lychee`, and one .NET start-up per run.

**Extending `package.json` scripts.**
Adding `check:metadata`, `check:sources`, `check:readme` and `check:skills`, then chaining them into `check`, is the smallest possible diff.
Node is already a hard dependency and `npm ci` already runs in CI.
It inverts the tooling, though: a .NET repository would route its .NET checks through an npm verb, and `package.json` declares itself scoped to "Markdown and configuration formatting for this repository".

Between the two, `scripts/check.cs` is the better fit, because it makes the repository's own stated convention carry its own checks.

## What this reveals about the repository

Three things, none of which needs `just`:

- **The documented local check is incomplete.** [AGENTS.md](../AGENTS.md) presents `npm run check` as the verification step, and it verifies a quarter of what CI does. That wording should change whether or not a runner is adopted.
- **An inline workflow script contradicts a stated rule.** The shebang ↔ executable-bit check lives in `validate-skills.yml`, and [AGENTS.md](../AGENTS.md) says checks belong in `scripts/`. The comment above it explains why the check exists, not why it is inline.
- **Link checking has no local form.** `lychee.toml` is committed but `lychee` is invoked only by the `Markdown` workflow, so a contributor cannot reproduce a link failure without reconstructing the action's arguments.

No roster source carried this research and none is a `vet-source` candidate: `just` is outside .NET entirely, and the roster is not the place for it.

**Resolution:** discard rather than promote.
This is a decision about this repository's own tooling, not .NET guidance, so there is no `opinions/` file it belongs in.
`resolve-research` should delete it once the decision is recorded, and the follow-up work — a `scripts/check.cs` and the [AGENTS.md](../AGENTS.md) wording — belongs in its own pull request.
