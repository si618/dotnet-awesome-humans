# dotnet-awesome-humans

Opinionated best practices for modern .NET software development — written for humans, applied by AI agents.

The name is the thesis: everything here distils the published guidance of **awesome humans** — the people and publications with a proven, multi-year track record, catalogued in [AWESOME-HUMANS.md](AWESOME-HUMANS.md).

This repository is a **living reference**. It exists to answer one question with confidence: _"what does good look like in .NET right now?"_ — for both AI agents and humans.

## Purpose

1. **A reference for AI agents.** Point a coding agent at this repository to define "good" — conventions, project layout, language usage, and library choices it should follow when generating or reviewing .NET code.
2. **Scaffolding and verification.** Copy-paste-ready template files that encode the opinions, so new projects start right and existing projects can be verified against them.
3. **A concise guide for humans.** Each opinion is written to be skimmable — the opinion first, the rationale second, the sources last, with actual code examples wherever an opinion is easier shown than told.

## Scope

Modern .NET, end to end — one bullet per file in [`opinions/`](opinions):

- **[Runtime & BCL](opinions/runtime-performance.md)** — performance idioms, `Span<T>`/memory, async, GC awareness
- **[Project structure & SDK](opinions/project-structure.md)** — project files, solution formats, central package management, analyzers, source generators
- **[C#](opinions/csharp.md)** — always targeting the **latest released language version**, with idiomatic use of new features
- **[F#](opinions/fsharp.md)** — first-class, not an afterthought
- **[Application architecture](opinions/architecture.md)** — modular monolith, vertical slices, and when layering earns its keep
- **[ASP.NET Core](opinions/aspnet-core.md)** — minimal APIs, hosting, auth, OpenAPI, performance
- **[Data access](opinions/data-access.md)** — EF Core defaults, set-based work, when to drop to SQL
- **[Logging & tracing](opinions/logging.md)** — structured logging, source-generated log messages, OpenTelemetry over OTLP
- **[UI frameworks](opinions/ui-frameworks.md)** — Blazor/WebAssembly, .NET MAUI (mobile + desktop), and cross-platform desktop (Avalonia)
- **[Testing](opinions/testing.md)** — structure, naming, patterns
- **[CI & automation](opinions/ci.md)** — pinned, reproducible builds and supply-chain hygiene
- **Libraries** — an opinionated shortlist of what to reach for (and what to avoid), spread across the files above

Adding an opinion file means adding its bullet here and its entry in [Repository layout](#repository-layout) — CI fails the pull request otherwise.

## Freshness policy

Opinions here always target the **latest release** versions of .NET, C#, and F# — not previews, not old LTS-for-comfort. When a new version ships, the repository is updated via the [skills](#maintenance-via-skills) below.

Every resource in this repository carries metadata recording when it was last reviewed and last used as a reference, so staleness is visible, not silent:

```yaml
---
targets: [net10.0, csharp-14, fsharp-10]
last-reviewed: 2026-08-12
last-used: 2026-08-12
sources: [dotnet-blog, andrew-lock]
---
```

## Awesome humans

Opinions must be earned. Every opinion traces back to a vetted **source** — the work of an awesome human, whether an individual (Stephen Toub, Andrew Lock) or a publication (the .NET Blog, Microsoft Learn) — with a proven track record: five years of sustained publishing for Tier 1, two to five for Tier 2, plus depth, accuracy, and independence of signal. The roster and the full admission criteria live in [AWESOME-HUMANS.md](AWESOME-HUMANS.md).

AI agents collate and refine content from these sources using the repository's skills — the humans provide the wisdom; the agents keep it current and consistent.

### House opinions

One human outranks the roster: the **repository owner**. Their own preferences are woven into the opinions and templates via [HOUSE-OPINIONS.md](HOUSE-OPINIONS.md) and the [`weave-house-opinion`](skills/weave-house-opinion/SKILL.md) skill — first-class, but always visibly marked so readers can tell community best practice from local convention (the marking literal and conflict-precedence rules are defined in HOUSE-OPINIONS.md). Other contributors can propose opinions (sourced or experience-based) through the [pull request template](.github/PULL_REQUEST_TEMPLATE.md).

## Repository layout

```text
├── README.md                 ← you are here
├── AGENTS.md                 ← instructions for AI agents working in this repo
├── CLAUDE.md                 ← pointer to AGENTS.md
├── AWESOME-HUMANS.md         ← vetted sources and admission criteria
├── HOUSE-OPINIONS.md         ← the owner's own opinions: intake and audit trail
├── opinions/                 ← the opinions, one topic per file, code examples as needed
│   ├── architecture.md
│   ├── aspnet-core.md
│   ├── ci.md
│   ├── csharp.md
│   ├── data-access.md
│   ├── fsharp.md
│   ├── logging.md
│   ├── project-structure.md
│   ├── runtime-performance.md
│   ├── testing.md
│   └── ui-frameworks.md
├── research/                 ← saved research-topic briefs (staging: promote or discard)
├── templates/                ← copy-paste-ready example files
│   ├── .editorconfig
│   ├── Directory.Build.props
│   ├── Directory.Packages.props
│   ├── example.slnf
│   ├── example.slnx
│   ├── global.json
│   └── projects/             ← exemplar .csproj / .fsproj files
└── skills/                   ← maintenance skills (see below)
```

## Maintenance via skills

The repository maintains itself through agent skills following the [Agent Skills specification](https://agentskills.io/specification) — LLM- and tool-agnostic, so any compliant agent can run them. Each skill is a directory under [`skills/`](skills) with a `SKILL.md`.

Skills are designed for a cost-aware split: **lower-cost worker agents** fan out to do the web searches and source sweeps, while the strongest available model acts as the **editor** — the orchestrator that synthesizes findings and is the only one that updates the actual opinions and templates.

| Skill                                                                | Purpose                                                                                    |
| -------------------------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| [`refresh-dotnet-versions`](skills/refresh-dotnet-versions/SKILL.md) | Detect new .NET / C# / F# releases and update all opinions and templates to target them    |
| [`harvest-sources`](skills/harvest-sources/SKILL.md)                 | Sweep the awesome-humans sources for new posts and fold notable guidance into the opinions |
| [`vet-source`](skills/vet-source/SKILL.md)                           | Evaluate a candidate source against the track-record criteria and admit or decline         |
| [`audit-freshness`](skills/audit-freshness/SKILL.md)                 | Report resources whose `last-reviewed` date has drifted past tolerance                     |
| [`verify-project`](skills/verify-project/SKILL.md)                   | Check an external project against the template files and report deviations                 |
| [`weave-house-opinion`](skills/weave-house-opinion/SKILL.md)         | Weave a repository-owner opinion into the opinions and templates, visibly marked as House  |
| [`research-topic`](skills/research-topic/SKILL.md)                   | Research a .NET topic conversationally using the opinions and vetted sources — cited brief |

### Release watch automation

A scheduled GitHub Action (`.github/workflows/dotnet-release-watch.yml`) polls the official [.NET releases index](https://github.com/dotnet/core/blob/main/release-notes/releases-index.json) daily and compares it against the checked-in snapshot at `.github/state/dotnet-releases.json`. When a new GA release lands (runtime, ASP.NET Core, SDK), it opens a pull request updating the snapshot. That PR is the **trigger** for running `refresh-dotnet-versions` — it does not itself update any opinions or template files.

## Using this repository

- **As an agent instruction:** "Follow the conventions in <https://github.com/si618/dotnet-awesome-humans> when writing .NET code."
- **As scaffolding:** copy files from [`templates/`](templates) into a new repository.
- **As a human:** read [`opinions/`](opinions) — each file leads with the opinion so you can skim.

## License

See [LICENSE](LICENSE).
