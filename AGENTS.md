# Agent Instructions

This repository defines opinionated best practices for modern .NET development. If you are an AI agent, this file tells you how to _use_ the repository as a reference and how to _maintain_ it.

## What this repository is

- The **source of truth for "what good looks like"** in .NET: runtime, BCL, SDK/tooling, C#, F#, ASP.NET Core, testing, and library selection.
- A set of **copy-paste-ready template files** under `templates/` that encode those opinions.
- A **living reference**: every resource records when it was last reviewed — and, outside `research/`, when it was last used — and skills under `skills/` keep it current.

This repository is **LLM- and agent-agnostic**. Skills follow the [Agent Skills specification](https://agentskills.io/specification); nothing here should assume a specific agent product (CLAUDE.md exists only as a pointer to this file).

## Using this repo as a reference (consumer mode)

When generating or reviewing .NET code for another project:

1. Read the relevant file(s) under `opinions/` — each leads with the opinion, then rationale, then sources.
2. Use `templates/` files as the canonical starting point for `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `.slnx`, `.slnf`, and project files. Prefer copying them verbatim and trimming, over authoring from scratch.
3. Always target the versions declared in the frontmatter of the opinion files (`targets:`) — never silently downgrade to an older TFM or language version.
4. When you use an `opinions/` or `templates/` file as a reference, update its `last-used` date (see [Metadata](#metadata)). `research/` topics do not carry one.

## Maintaining this repo (maintainer mode)

- All substantive content changes must trace back to sources listed in [AWESOME-HUMANS.md](AWESOME-HUMANS.md), with one exception: **house opinions** — the repository owner's own preferences, recorded via [HOUSE-OPINIONS.md](HOUSE-OPINIONS.md) and the `weave-house-opinion` skill, carried under the reserved `house` source id and always visibly marked **House:**. Do not fold in guidance from other unvetted sources; instead, propose the source for admission via the `vet-source` skill.
- House precedence, the `**House:**` marking literal, and contributor adoption are defined canonically in [HOUSE-OPINIONS.md — How this works](HOUSE-OPINIONS.md#how-this-works); follow that, not paraphrases.
- Opinions target the **latest released** .NET / C# / F# versions. Previews may be mentioned in a clearly marked "coming next" note but never as the opinion itself.
- Use the skills rather than ad-hoc edits for version bumps (`refresh-dotnet-versions`), content sweeps (`harvest-sources`), and staleness checks (`audit-freshness`).
- Keep opinions **opinionated**: one recommendation, not a menu. Alternatives get at most one line explaining why they lost.
- **A new file under `opinions/` is not finished until `README.md` lists it** — add a bullet under [Scope](README.md#scope) linking the file (`- **[Title](opinions/<file>.md)** — one line on what it covers`) and an entry in the [Repository layout](README.md#repository-layout) tree, kept alphabetical. Renaming or removing an opinion file means updating both places in the same commit. The `Validate skills and opinions` workflow enforces this in both directions — a missing entry **and** one left pointing at a file that no longer exists both fail the pull request, rather than drifting silently. The same applies to `skills/`: adding or removing a skill directory means adding or removing its row in the [Maintenance via skills](README.md#maintenance-via-skills) table (the row's name links to its `skills/<name>/SKILL.md`), equally enforced.
- `README.md` carries diagrams of the research and source-admission lifecycles. They are orientation only — the prose here and in the `SKILL.md` files stays canonical — but a change to either lifecycle updates its diagram in the same commit, so the picture never contradicts the rules.

### Orchestration and model economy

Maintenance skills involve wide, shallow work (sweeping sources, fetching pages, checking versions) and narrow, deep work (deciding what an opinion should say). Split them accordingly:

- **Fan out the sweeps to lower-cost worker agents.** Web searches, feed checks, page fetches, and per-source summarization are cheap-model work — run them in parallel where the host supports it. Worker agents return raw findings (links, dates, extracted claims), never edits.
- **Reserve the strongest available model as the "editor".** Only the editor — the orchestrating model — synthesizes worker-agent findings, resolves conflicts between sources, and actually updates `opinions/` and `templates/`. Opinion-shaping judgment is exactly where model quality matters; never delegate the final edit to a cheap model.
- This split is a recommendation, not a requirement — a single-model host can run everything itself, but should still keep the gather/decide separation.

## Metadata

Three kinds of resource carry this metadata, in the two syntaxes their file formats allow. `opinions/` and `templates/` carry four fields — `targets`, `last-reviewed`, `last-used`, `sources`. `research/` carries three: no `last-used`.

**`opinions/` and `research/` carry YAML frontmatter:**

```yaml
---
targets: [net10.0, csharp-14, fsharp-10]
last-reviewed: 2026-08-12 # last time content was verified against sources
last-used: 2026-08-12 # last time an agent used this as a reference
sources: [dotnet-blog, andrew-lock] # ids from AWESOME-HUMANS.md
---
```

(A `research/` topic omits `last-used`, and carries no status field either:

- **No `last-used`** — the only way to consult a topic is to build on it, which re-verifies it, so the two dates would always move together. `last-reviewed` already says everything the pair would.
- **No status** — promoting or discarding one ends in deletion, so a topic on disk is unresolved by definition and a status could only ever read `open`.

See [research-topic](skills/research-topic/SKILL.md) and [resolve-research](skills/resolve-research/SKILL.md).)

**`templates/` carry a first-line comment header** — an XML, INI or source file cannot open with a `---` block and stay valid for the tools that read it, so the same fields ride in a comment instead, pipe-separated after a fixed marker:

```xml
<!-- dotnet-awesome-humans template | targets: net10.0 | last-reviewed: 2026-08-12 | last-used: 2026-08-12 | sources: ms-learn -->
```

```ini
# dotnet-awesome-humans template | targets: net10.0 | last-reviewed: 2026-08-12 | last-used: 2026-08-12 | sources: ms-learn
```

```fsharp
// dotnet-awesome-humans template | targets: net10.0, fsharp-10 | last-reviewed: 2026-08-12 | last-used: 2026-08-12 | sources: scott-wlaschin
```

Rules:

- Update `last-used` whenever you consume an opinion or a template — an opinion you read to decide something, a template you diffed a project against. It does two jobs: it orders staleness triage (of ten drifted `last-reviewed` dates, review the ones being read first), and it marks candidates for pruning. Know its one limitation: only reads that happen _in this repository_ stamp anything, so a human copying a template or reading an opinion on the web leaves no trace, and the field always undercounts. That is why `audit-freshness` treats it as informational and never a finding: a recent date is weak evidence that a file is in use, and an old date is no evidence that it is not.
- Update `last-reviewed` only after verifying content against its sources.
- `sources` ids must exist in `AWESOME-HUMANS.md` — either in the roster tables or as the reserved `house` id.
- Dates are ISO 8601 (`YYYY-MM-DD`), always absolute, never relative.
- Two template files carry no header and are exempt: `templates/global.json` and `templates/example.slnf`, because JSON has no comment syntax. What they pin is audited against the latest releases instead. Build output left under `templates/` by smoke-testing the exemplars (`artifacts/`, `bin/`, `obj/` — all gitignored) is generated, not a resource, and the check skips it.
- `skills/` are the exception on purpose: their frontmatter is defined by the [Agent Skills specification](https://agentskills.io/specification), they are procedures rather than reference material, and nothing consumes a date on them — `git log` answers when a skill last changed.

All of this is enforced by `dotnet run scripts/validate-metadata.cs` in CI, in both syntaxes, so a missing field or a relative date fails the pull request rather than drifting silently.

## Writing style for opinions

- **Opinion first.** State the recommendation in the first sentence. Rationale follows. Sources last.
- Concise enough for a human to skim; precise enough for an agent to apply mechanically.
- Include actual code examples wherever an opinion is easier shown than told — minimal, idiomatic for the declared `targets`, and preferring a before/after pair when superseding an old idiom. The same applies to `templates/`: exemplar code files are welcome alongside configuration.
- Code samples must compile against the declared `targets`.
- British or American spelling — either, but consistent within a file.

## Conventions in this repo

- **Work in a git worktree, one branch per directory.** The local clone is bare, with each branch checked out beside it, so moving between pieces of work is a `cd` rather than a `git checkout`. An in-place branch switch swaps the tree out from under anything holding a path into it — a running `dotnet` build, a formatter, another agent mid-edit — and discards the gitignored state (`node_modules/`, `artifacts/`, `bin/`, `obj/`) that the checks on the branch you left had already paid for.
  - **Starting** — every new piece of work, whether a feature, a fix, a research topic or a version bump, starts as a **new worktree, not merely a new branch**: `git worktree add -b <branch> <dir> main`, in that one step. Never `git checkout -b` inside a worktree that already holds other work.
  - **Naming** — the directory is the branch name with slashes flattened to dashes, so `resolve/globalisation` becomes `resolve-globalisation/`. Every worktree then sits one flat level beside the bare `.bare/` directory, tab-completable in a single step, and no nested `research/` appears that is not the repository's own `research/`.
  - **Setup** — a fresh worktree carries no gitignored local state, so run `npm install` in it before `npm run check`, and re-create the `.claude/skills` symlink there if your harness needs one (see [CLAUDE.md](CLAUDE.md)).
  - **Finishing** — when a worktree's pull request merges, **offer to delete that worktree** (`git worktree remove <dir>`, then `git branch -d <branch>`) so stale checkouts do not accumulate. Ask rather than assume: the branch may still hold work that never reached the pull request.
- Markdown follows GFM and markdownlint, configured in `.markdownlint-cli2.jsonc` (defaults, minus three rules that fight the prose style — reasons recorded there).
- Run `npm run format` to format and `npm run check` to verify formatting and lint together. Both use the versions pinned in `package.json`; do not invoke `npx prettier`/`npx markdownlint-cli2` directly, which resolves whatever is latest.
- Prettier owns Markdown **and** the JSON/JSONC/YAML configuration, so `.editorconfig` and the formatter agree rather than fighting across saves. Two exceptions, both recorded in `.prettierignore`: `package-lock.json` and `.github/state/dotnet-releases.json` are machine-written, so gating them on formatting would only redden a build nobody can fix by editing. `lychee.toml` is unformatted — Prettier has no built-in TOML parser, and a third-party plugin is not worth one hand-written file.
- One topic per file under `opinions/`; keep files under ~300 lines — split rather than sprawl.
- CI checks live in `scripts/` as .NET file-based apps, run with `dotnet run scripts/<name>.cs` from the repository root; anything two of them need is a helper file they `#:include` rather than a copy. Entry points are kebab-case because they are typed on a command line; helper files take the name of the type they contain, following C# file-naming convention. Add a check there, not as an inline script in a workflow, and hold it to the repository's own opinions (`global.json` pins the SDK, `TreatWarningsAsErrors`, latest language version, and the `[scripts/*.cs]` block in `.editorconfig` turns the analyzer set into build failures — the repository is held to the bar it publishes). Adding or renaming one means updating the [Repository scripts](README.md#repository-scripts) table and the layout tree. `#:package` pins are bumped by Renovate, which reads the directive but only scans `.cs` because `renovate.json` opts `scripts/` into the nuget manager — a check placed elsewhere would go unbumped.
- Links are checked by the `Markdown` workflow (`lychee.toml`) and **block pull requests** — a dead citation is a content defect. If a failure is a third-party host being down rather than link rot, re-run the job once it recovers; do not merge past a red link check.
