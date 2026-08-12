# dotnet-awesome-humans

Opinionated best practices for modern .NET software development — written for humans, applied by AI agents.

The name is the thesis: everything here distils the published guidance of **awesome humans** — the people and publications with a proven, decade-class track record, catalogued in [AWESOME-HUMANS.md](AWESOME-HUMANS.md).

This repository is a **living reference**. It exists to answer one question with confidence: _"what does good look like in .NET right now?"_ — for both AI agents and humans.

## Purpose

1. **A reference for AI agents.** Point a coding agent at this repository to define "good" — conventions, project layout, language usage, and library choices it should follow when generating or reviewing .NET code.
2. **Scaffolding and verification.** Copy-paste-ready template files that encode the opinions, so new projects start right and existing projects can be verified against them.
3. **A concise guide for humans.** Each opinion is written to be skimmable — the opinion first, the rationale second, the sources last, with actual code examples wherever an opinion is easier shown than told.

## Scope

Modern .NET, end to end:

- **Runtime & BCL** — performance idioms, `Span<T>`/memory, async, GC awareness
- **SDK & tooling** — project files, solution formats, central package management, analyzers, source generators
- **C#** — always targeting the **latest released language version**, with idiomatic use of new features
- **F#** — first-class, not an afterthought
- **ASP.NET Core** — minimal APIs, hosting, auth, OpenAPI, performance
- **UI frameworks** — Blazor/WebAssembly, .NET MAUI, and cross-platform desktop (Avalonia)
- **Testing** — structure, naming, patterns
- **Libraries** — an opinionated shortlist of what to reach for (and what to avoid)

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

Opinions must be earned. Every opinion traces back to a vetted **source** — the work of an awesome human, whether an individual (Stephen Toub, Andrew Lock) or a publication (the .NET Blog, Microsoft Learn) — with a proven, decade-class track record. The roster and the admission criteria for joining the club live in [AWESOME-HUMANS.md](AWESOME-HUMANS.md).

AI agents collate and refine content from these sources using the repository's skills — the humans provide the wisdom; the agents keep it current and consistent.

## Repository layout

```text
├── README.md                 ← you are here
├── AGENTS.md                 ← instructions for AI agents working in this repo
├── CLAUDE.md                 ← pointer to AGENTS.md
├── AWESOME-HUMANS.md         ← vetted sources and admission criteria
├── opinions/                 ← the opinions, one topic per file, code examples as needed
│   ├── project-structure.md
│   ├── csharp.md
│   ├── fsharp.md
│   ├── aspnet-core.md
│   ├── ui-frameworks.md
│   ├── testing.md
│   └── ...
├── templates/                ← copy-paste-ready example files
│   ├── .editorconfig
│   ├── Directory.Build.props
│   ├── Directory.Packages.props
│   ├── global.json
│   ├── example.slnx
│   ├── example.slnf
│   └── projects/             ← exemplar .csproj / .fsproj files
└── skills/                   ← maintenance skills (see below)
```

## Maintenance via skills

The repository maintains itself through agent skills following the [Agent Skills specification](https://agentskills.io/specification) — LLM- and tool-agnostic, so any compliant agent can run them. Each skill is a directory under `skills/` with a `SKILL.md`.

Skills are designed for a cost-aware split: **lower-cost worker agents** fan out to do the web searches and source sweeps, while the strongest available model acts as the **editor** — the orchestrator that synthesizes findings and is the only one that updates the actual opinions and templates.

| Skill                     | Purpose                                                                                    |
| ------------------------- | ------------------------------------------------------------------------------------------ |
| `refresh-dotnet-versions` | Detect new .NET / C# / F# releases and update all opinions and templates to target them    |
| `harvest-sources`         | Sweep the awesome-humans sources for new posts and fold notable guidance into the opinions |
| `vet-source`              | Evaluate a candidate source against the track-record criteria and admit or decline         |
| `audit-freshness`         | Report resources whose `last-reviewed` date has drifted past tolerance                     |
| `verify-project`          | Check an external project against the template files and report deviations                 |

### Release watch automation

A scheduled GitHub Action (`.github/workflows/dotnet-release-watch.yml`) polls the official [.NET releases index](https://github.com/dotnet/core/blob/main/release-notes/releases-index.json) daily and compares it against the checked-in snapshot at `.github/state/dotnet-releases.json`. When a new GA release lands (runtime, ASP.NET Core, SDK), it opens a pull request updating the snapshot. That PR is the **trigger** for running `refresh-dotnet-versions` — it does not itself update any opinions or template files.

## Using this repository

- **As an agent instruction:** "Follow the conventions in <https://github.com/…/dotnet-awesome-humans> when writing .NET code."
- **As scaffolding:** copy files from `templates/` into a new repository.
- **As a human:** read `opinions/` — each file leads with the opinion so you can skim.

## License

See [LICENSE](LICENSE).
