# Agent Instructions

This repository defines opinionated best practices for modern .NET development. If you are an AI agent, this file tells you how to _use_ the repository as a reference and how to _maintain_ it.

## What this repository is

- The **source of truth for "what good looks like"** in .NET: runtime, BCL, SDK/tooling, C#, F#, ASP.NET Core, testing, and library selection.
- A set of **copy-paste-ready template files** under `templates/` that encode those opinions.
- A **living reference**: every resource records when it was last reviewed and last used, and skills under `skills/` keep it current.

This repository is **LLM- and agent-agnostic**. Skills follow the [Agent Skills specification](https://agentskills.io/specification); nothing here should assume a specific agent product (CLAUDE.md exists only as a pointer to this file).

## Using this repo as a reference (consumer mode)

When generating or reviewing .NET code for another project:

1. Read the relevant file(s) under `opinions/` — each leads with the opinion, then rationale, then sources.
2. Use `templates/` files as the canonical starting point for `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `.slnx`, `.slnf`, and project files. Prefer copying them verbatim and trimming, over authoring from scratch.
3. Always target the versions declared in the frontmatter of the opinion files (`targets:`) — never silently downgrade to an older TFM or language version.
4. When you use a resource as a reference, update its `last-used` frontmatter date (see [Metadata](#metadata)).

## Maintaining this repo (maintainer mode)

- All substantive content changes must trace back to sources listed in [AWESOME-HUMANS.md](AWESOME-HUMANS.md), with one exception: **house opinions** — the repository owner's own preferences, recorded via [HOUSE-OPINIONS.md](HOUSE-OPINIONS.md) and the `weave-house-opinion` skill, carried under the reserved `house` source id and always visibly marked **House:**. Do not fold in guidance from other unvetted sources; instead, propose the source for admission via the `vet-source` skill.
- House precedence, the `**House:**` marking literal, and contributor adoption are defined canonically in [HOUSE-OPINIONS.md — How this works](HOUSE-OPINIONS.md#how-this-works); follow that, not paraphrases.
- Opinions target the **latest released** .NET / C# / F# versions. Previews may be mentioned in a clearly marked "coming next" note but never as the opinion itself.
- Use the skills rather than ad-hoc edits for version bumps (`refresh-dotnet-versions`), content sweeps (`harvest-sources`), and staleness checks (`audit-freshness`).
- Keep opinions **opinionated**: one recommendation, not a menu. Alternatives get at most one line explaining why they lost.
- **A new file under `opinions/` is not finished until `README.md` lists it** — add a bullet under [Scope](README.md#scope) linking the file (`- **[Title](opinions/<file>.md)** — one line on what it covers`) and an entry in the [Repository layout](README.md#repository-layout) tree, kept alphabetical. Renaming or removing an opinion file means updating both places in the same commit. The `Validate skills and opinions` workflow enforces this in both directions — a missing entry **and** one left pointing at a file that no longer exists both fail the pull request, rather than drifting silently. The same applies to `skills/`: adding or removing a skill directory means adding or removing its row in the [Maintenance via skills](README.md#maintenance-via-skills) table (the row's name links to its `skills/<name>/SKILL.md`), equally enforced.

### Orchestration and model economy

Maintenance skills involve wide, shallow work (sweeping sources, fetching pages, checking versions) and narrow, deep work (deciding what an opinion should say). Split them accordingly:

- **Fan out the sweeps to lower-cost worker agents.** Web searches, feed checks, page fetches, and per-source summarization are cheap-model work — run them in parallel where the host supports it. Worker agents return raw findings (links, dates, extracted claims), never edits.
- **Reserve the strongest available model as the "editor".** Only the editor — the orchestrating model — synthesizes worker-agent findings, resolves conflicts between sources, and actually updates `opinions/` and `templates/`. Opinion-shaping judgment is exactly where model quality matters; never delegate the final edit to a cheap model.
- This split is a recommendation, not a requirement — a single-model host can run everything itself, but should still keep the gather/decide separation.

## Metadata

Every file under `opinions/` (and documented template files where a comment header is possible) carries YAML frontmatter:

```yaml
---
targets: [net10.0, csharp-14, fsharp-10]
last-reviewed: 2026-08-12 # last time content was verified against sources
last-used: 2026-08-12 # last time an agent used this as a reference
sources: [dotnet-blog, andrew-lock] # ids from AWESOME-HUMANS.md
---
```

Rules:

- Update `last-used` whenever you consume a resource; update `last-reviewed` only after verifying content against its sources.
- `sources` ids must exist in `AWESOME-HUMANS.md` — either in the roster tables or as the reserved `house` id.
- Dates are ISO 8601 (`YYYY-MM-DD`), always absolute, never relative.

## Writing style for opinions

- **Opinion first.** State the recommendation in the first sentence. Rationale follows. Sources last.
- Concise enough for a human to skim; precise enough for an agent to apply mechanically.
- Include actual code examples wherever an opinion is easier shown than told — minimal, idiomatic for the declared `targets`, and preferring a before/after pair when superseding an old idiom. The same applies to `templates/`: exemplar code files are welcome alongside configuration.
- Code samples must compile against the declared `targets`.
- British or American spelling — either, but consistent within a file.

## Conventions in this repo

- Markdown follows GFM and markdownlint, configured in `.markdownlint-cli2.jsonc` (defaults, minus three rules that fight the prose style — reasons recorded there).
- Run `npm run format` to format and `npm run check` to verify formatting and lint together. Both use the versions pinned in `package.json`; do not invoke `npx prettier`/`npx markdownlint-cli2` directly, which resolves whatever is latest.
- One topic per file under `opinions/`; keep files under ~300 lines — split rather than sprawl.
- Links are checked by the `Markdown` workflow (`lychee.toml`) and **block pull requests** — a dead citation is a content defect. If a failure is a third-party host being down rather than link rot, re-run the job once it recovers; do not merge past a red link check.
