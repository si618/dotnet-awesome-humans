# Agent Instructions

This repository defines opinionated best practices for modern .NET development. If you are an AI agent, this file tells you how to _use_ the repository as a reference and how to _maintain_ it.

## What this repository is

- The **source of truth for "what good looks like"** in .NET: runtime, BCL, SDK/tooling, C#, F#, ASP.NET Core, testing, and library selection.
- A set of **copy-paste-ready template files** under `templates/` that encode those opinions.
- A **living reference**: every resource records when it was last reviewed (and, outside `research/`, when it was last used), and skills under `skills/` keep it current.

This repository is **LLM- and agent-agnostic**. Skills follow the [Agent Skills specification](https://agentskills.io/specification); nothing here should assume a specific agent product.

## Skills

Skills live at the specification's canonical path, `skills/<name>/SKILL.md`. Read and follow them from there. If your harness only discovers skills under a directory of its own, symlink that directory to `skills/` locally and keep the link out of git. Do not restructure the repository to suit a harness.

## Using this repo as a reference (consumer mode)

When generating or reviewing .NET code for another project:

1. Read the relevant file(s) under `opinions/`. Each leads with the opinion, then rationale, then sources.
2. Use `templates/` files as the canonical starting point for `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `.slnx`, `.slnf`, and project files. Prefer copying them verbatim and trimming, over authoring from scratch.
3. Always target the versions declared in the frontmatter of the opinion files (`targets:`), and never silently downgrade to an older TFM or language version.
4. When you use an `opinions/` or `templates/` file as a reference, update its `last-used` date (see [Metadata](#metadata)). `research/` topics do not carry one.

## Maintaining this repo (maintainer mode)

- All substantive content changes must trace back to sources listed in [AWESOME-HUMANS.md](AWESOME-HUMANS.md), with one exception: **house opinions** — the repository owner's own preferences, recorded via [HOUSE-OPINIONS.md](HOUSE-OPINIONS.md) and the `weave-house-opinion` skill, carried under the reserved `house` source id and always visibly marked **House:**. Do not fold in guidance from other unvetted sources; instead, propose the source for admission via the `vet-source` skill. Linking a named package's own documentation as a **reference** is not folding in a source: a reference corroborates a roster-sourced claim and never carries one, as defined in [AWESOME-HUMANS.md: Admission criteria](AWESOME-HUMANS.md#admission-criteria).
- House precedence, the `**House:**` marking literal, and contributor adoption are defined canonically in [HOUSE-OPINIONS.md: How this works](HOUSE-OPINIONS.md#how-this-works); follow that, not paraphrases.
- Opinions target the **latest released** .NET / C# / F# versions. Previews may be mentioned in a clearly marked "coming next" note but never as the opinion itself.
- Use the skills rather than ad-hoc edits for version bumps (`refresh-dotnet-versions`), content sweeps (`harvest-sources`), and staleness checks (`audit-freshness`).
- Keep opinions **opinionated**: one recommendation, not a menu. Alternatives get at most one line explaining why they lost.
- **A new file under `opinions/` is not finished until `README.md` lists it** in two places: a bullet under [Scope](README.md#scope) (`- **[Title](opinions/<file>.md):** one line on what it covers`), sorted by title ignoring case with the unlinked **Libraries** bullet last, and an entry in the [Repository layout](README.md#repository-layout) tree, sorted by file name. The two orders differ (`architecture.md` is titled "Application architecture"). Renaming or removing a file updates both in the same commit. A skill directory and its row in the [Maintenance via skills](README.md#maintenance-via-skills) table follow the same rule. `scripts/validate-readme-index.cs` fails the pull request on an entry that is missing, out of order, or pointing at a file that no longer exists.
- **Commit messages and pull request titles follow [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/).** A pull request squash-merges under its title, so the title is the commit subject `git log` keeps: `<type>[(scope)]: <description>`, lower-case and in the imperative. The types are `docs` for opinion, template and guidance changes, `fix` and `feat` for the scripts and workflows, `ci` for workflow plumbing, `chore` for dependencies and housekeeping, and `research`, this repository's own addition, for staging or discarding a research topic. A skill that names its title says so in its `SKILL.md`, as `research-topic` and `resolve-research` do. A scope is optional (`chore(deps)`, `docs(verify-project)`).
- `README.md` carries diagrams of the research and source-admission lifecycles. They are orientation only: the prose here and in the `SKILL.md` files stays canonical. But a change to either lifecycle updates its diagram in the same commit, so the picture never contradicts the rules.

### Orchestration and model economy

Maintenance skills involve wide, shallow work (sweeping sources, fetching pages, checking versions) and narrow, deep work (deciding what an opinion should say). Split them accordingly:

- **Fan out the sweeps to lower-cost worker agents.** Web searches, feed checks, page fetches, and per-source summarisation are cheap-model work, so run them in parallel where the host supports it. Worker agents return raw findings (links, dates, extracted claims), never edits.
- **Reserve the strongest available model as the "editor".** Only the editor (the orchestrating model) synthesizes worker-agent findings, resolves conflicts between sources, and actually updates `opinions/` and `templates/`. Opinion-shaping judgment is exactly where model quality matters; never delegate the final edit to a cheap model.
- This split is a recommendation, not a requirement: a single-model host can run everything itself, but should still keep the gather/decide separation.

### Agent attribution

An agent working with the author's credentials speaks in the author's name: a comment it posts appears under the author's account, with nothing to show a reader that a person did not write it. Attribution belongs where that matters, in the conversation, not in the history:

- **No agent attribution in history.** Pull request titles and descriptions become squash commits, so they carry no `Co-Authored-By` trailers or "Generated with" lines, and neither do commit messages. The project is AI driven by design, and this overrides any default an agent harness applies. It covers every commit on a branch, not only the pull request: squashing appends a `Co-authored-by` line to the merged commit for each co-author trailer and each author other than the merger on the branch's commits, whatever the pull request description says. So commit as the author's git identity, never as an identity the agent harness supplies, and leave the trailer out of branch commits too.
- **Agent attribution in conversation.** Comments an agent writes on issues and pull requests, including replies in review threads, end with a line saying so, such as `Written by an agent: <harness>, <model>, running <skill>.`, leaving out the skill when there is none. A reader, or another agent, then knows a person did not write them.

## Metadata

Three kinds of resource carry this metadata, in the two syntaxes their file formats allow. `opinions/` and `templates/` carry four fields: `targets`, `last-reviewed`, `last-used`, `sources`. `research/` carries three: no `last-used`.

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

- **No `last-used`:** the only way to consult a topic is to build on it, which re-verifies it, so the two dates would always move together. `last-reviewed` already says everything the pair would.
- **No status:** promoting or discarding one ends in deletion, so a topic on disk is unresolved by definition and a status could only ever read `open`.

See [research-topic](skills/research-topic/SKILL.md) and [resolve-research](skills/resolve-research/SKILL.md).)

**`templates/` carry a first-line comment header:** an XML, INI or source file cannot open with a `---` block and stay valid for the tools that read it, so the same fields ride in a comment instead, pipe-separated after a fixed marker:

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

- Update `last-used` whenever you consume an opinion or a template: an opinion you read to decide something, a template you diffed a project against. It orders staleness triage (of ten drifted `last-reviewed` dates, review the ones being read first) and marks candidates for pruning. Only reads _in this repository_ stamp it, so a human copying a template or reading an opinion on the web leaves no trace and the field always undercounts. `audit-freshness` therefore treats it as informational, never a finding.
- Update `last-reviewed` only after verifying content against its sources.
- `sources` ids must exist in `AWESOME-HUMANS.md`, either in the roster tables or as the reserved `house` id. The two markings are read from a source's section under `## Source notes`, not from its table row. On `opinions/` and `templates/` the id must also be allowed to cite, so a watch-list or `**Discovery-only.**` row is rejected, and a file whose every citable source is `**Corroborate.**`-marked is rejected too, because something unmarked has to stand beside it. `research/` is exempt, because a topic may cite unvetted material as long as the text flags it (see [research-topic](skills/research-topic/SKILL.md)).
- Dates are ISO 8601 (`YYYY-MM-DD`), always absolute, never relative.
- Two template files carry no header and are exempt: `templates/global.json` and `templates/example.slnf`, because JSON has no comment syntax. What they pin is audited against the latest releases instead. Build output left under `templates/` by smoke-testing the example projects (`artifacts/`, `bin/`, `obj/`, all gitignored) is generated, not a resource, and the check skips it.
- `skills/` are the exception on purpose: their frontmatter is defined by the [Agent Skills specification](https://agentskills.io/specification), they are procedures rather than reference material, and nothing consumes a date on them. `git log` answers when a skill last changed.

All of this is enforced by `dotnet run scripts/validate-metadata.cs` in CI, in both syntaxes, so a missing field or a relative date fails the pull request rather than drifting silently.

## Writing style

These rules cover every piece of prose this repository carries. That includes opinions, research topics, skills, template comments and root documents such as this one. It also includes commit messages, pull request titles and descriptions, and the comments an agent posts on issues and pull requests. [Writing style for opinions](#writing-style-for-opinions) adds the rules for opinions alone.

- **Write for a working .NET developer, and give every sentence a fact they can act on or check.** Assume they know C#, the BCL and the SDK, so do not explain what dependency injection or a `Task` is. Name the exact type, member, property, diagnostic id and version (`CA1307`, `<InvariantGlobalization>`, .NET 10) rather than describing it. A measured number beats an adjective: "5.5ms to 0.826ms", not "much faster". Cut a sentence that restates the one before it, and one that comments on the text rather than the subject (`which is worth noticing`, `the part that matters`). Decide what a sentence carries before deciding how it sounds; the rhythm rules below cover the second.
- **Watch the rhythm, not the vocabulary.** Prose here is mostly agent-written, and the way it drifts is not the way generated text is usually caricatured: the classic tells (`delve`, `leverage`, `seamless`, `robust`, `comprehensive`, `in today's`, the `here is` lead-in, the `in conclusion` wrap-up) are absent from this repository and are not worth policing. What drifts is rhythm. Four moves are each fine occasionally and read as machine-written in bulk:
  - **Em dashes.** Keep the one carrying the sentence's turn. A second in the same sentence is nearly always one too many, and a colon, a comma or a full stop usually does the work.
  - **`X, not Y` antithesis.** This repository argues by contrast on purpose, so keep it where the rejected alternative is the one a reader would otherwise choose (`Pin to commit SHAs, not mutable tags`). Cut it where the contrast is invented: `a build artefact, not an afterthought` rejects nothing anybody was going to choose.
  - **Parenthetical asides.** Promote one that carries real content to its own sentence, and delete one that restates the sentence it hangs off.
  - **Rule-of-three lists.** A real enumeration is fine at any length; padding to three because three sounds complete is the tell. Cut to the two that matter.
- **Drop the stock idioms.** The exception to the rule above is a short list of agent idioms, and they did get in: `load-bearing`, `belt and braces`, `earns its keep` or `earns its place`, `escape hatch`, `bites`, `for free`, `reach for`, `worth noting`, and `honest`, `genuinely` or `quietly` used as intensifiers. Each stands in for a plainer statement, so make that statement instead: the failure a guard catches, the claim an opinion rests on, the cost a feature repays. A literal use is not an idiom: `silently` for a failure that raises no error, or `drift` for two copies that diverge, stays.
- **Split long sentences.** The median sentence here is 19 words. Past about 45, split at the clause doing separate work: a runaway sentence is usually where the four moves above have accumulated.
- **House: cite as `Source: Title`, and never the reverse.** The source leads whether it is a person or a publication, so `[Toub: Performance Improvements in .NET 10]` and `[Microsoft Learn: Native AOT deployment]`, not `[Native AOT deployment: Microsoft Learn]`. A description-list line takes the same colon: `**Label:** definition`. Both shapes once used an em dash as the separator, and between them they carried most of the em dashes in the repository and none of the meaning. Use a colon, or nothing. Two things this does not touch: a colon inside a post's own title stays as its author wrote it (`[Avalonia 12: Ready for What's Next]`), and an internal reference keeps its section (`[AGENTS.md: Metadata]`).
- **These are judgment calls, so they belong in review.** Whether a contrast is justified is not decidable by counting, and a density threshold mostly flags the best-cited files. Read for them; do not build a check that fails a build over them.
- **Spelling: British, except where .NET names the word.** The house writes British English, so defence, modelling, catalogue, behaviour and colour stay as they are. The exception is narrow and mechanical: where a word _is_ a .NET concept, it takes the platform's spelling so that prose and identifier match and a search for one finds the other. `System.Globalization` and `IStringLocalizer` give us globalization and localization down to the filename `opinions/globalization.md`; EF Core's materialization gives us materialized. A word that merely shares a stem with an API is not covered, so "a behaviour change in SQLite" keeps its British spelling even though MAUI has a `Behavior` type, and "back catalogue" is untouched by `Initial Catalog`. Anglicising an API name is always wrong; Americanising ordinary prose gains nothing.
- **This rule governs prose written here, and nothing else.** It is not a conformance check. A third-party title and its URL keep whatever the author wrote, so Andrew Lock's "Adding Localisation to an ASP.NET Core application" stays as he spelled it: correcting a source's own words misquotes it, and correcting a URL breaks it. Nor does it travel to other people's code. `verify-project` reviews a codebase against the opinions in `opinions/`, and spelling in someone else's identifiers, comments or documentation is never a finding.

## Writing style for opinions

- **Opinion first.** State the recommendation in the first sentence. Rationale follows. Sources last.
- **Precise enough to apply mechanically.** An agent should be able to turn an opinion into an edit or a `verify-project` finding without interpreting it. A rule that names no setting, type or threshold is not finished.
- Include actual code examples wherever an opinion is easier shown than told: minimal, idiomatic for the declared `targets`, and preferring a before/after pair when superseding an old idiom. The same applies to `templates/`: example code files are welcome alongside configuration.
- Code samples must compile against the declared `targets`.
- **Use a table when the same attributes repeat across items.** Options, versions or frameworks compared on the same few points read faster as rows than as parallel paragraphs, as in [datetime.md](opinions/datetime.md). Reasoning stays in sentences: a cell holds a fact, not the argument for it.
- One topic per file under `opinions/`; keep files under ~300 lines, and split rather than sprawl.

## Workflow

- **Work in a git worktree, one branch per directory.** The local clone is bare, with each branch checked out beside it, so moving between pieces of work is a `cd` rather than a `git checkout`. An in-place branch switch swaps the tree out from under anything holding a path into it (a running `dotnet` build, a formatter, another agent mid-edit) and discards the gitignored state (`node_modules/`, `artifacts/`, `bin/`, `obj/`) that the checks on the branch you left had already paid for.
  - **Starting:** every new piece of work, whether a feature, a fix, a research topic or a version bump, starts as a **new worktree, not merely a new branch**: `git worktree add -b <branch> <dir> main`, in that one step. Never `git checkout -b` inside a worktree that already holds other work.
  - **Branch names:** a skill's branch is prefixed with what the skill produces: `research/<topic-slug>`, `resolve/<topic-slug>`, `harvest/<YYYY-MM-DD>` dated the day the sweep window ends, `vet/<source-id>` or a slug naming a multi-source pass, `house/<slug>` and `refresh/dotnet-<version>`. Any other change takes its commit's [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) type (`docs/`, `fix/`, `feat/`, `ci/` or `chore/`) and a kebab-case slug. `automation/` is reserved for workflows. This convention outranks the host environment's own, so a generated name such as `agent/youthful-wozniak-gm4k7m` is replaced before the first push. A host that will only push under its own prefix keeps the convention as the trailing part: `agent/harvest/2026-09-14`. Settle the name before opening the pull request, because renaming the head branch of an open pull request on GitHub closes it, and it cannot be reopened under the new name.
  - **Directory names:** the directory is the branch name with slashes flattened to dashes, so `resolve/globalization` becomes `resolve-globalization/`. Every worktree then sits one flat level beside the bare `.bare/` directory, tab-completable in a single step, and no nested `research/` appears that is not the repository's own `research/`.
  - **Setup:** a fresh worktree carries no gitignored local state, so run `npm install` in it before `npm run check`, and re-create the skills symlink there if your harness needs one (see [Skills](#skills)).
  - **Finishing:** when a worktree's pull request merges, **offer to delete that worktree** (`git worktree remove <dir>`, then `git branch -d <branch>`) so stale checkouts do not accumulate. Ask rather than assume: the branch may still hold work that never reached the pull request.
- Markdown follows GFM and markdownlint, configured in `.markdownlint-cli2.jsonc` (defaults, minus three rules that fight the prose style, with the reasons recorded there).
- Run `npm run format` to format and `npm run check` to verify formatting and lint together. Both use the versions pinned in `package.json`; do not invoke `npx prettier`/`npx markdownlint-cli2` directly, which resolves whatever is latest.
- Prettier owns Markdown **and** the JSON/JSONC/YAML configuration, so `.editorconfig` and the formatter agree rather than fighting across saves. Two exceptions, both recorded in `.prettierignore`: `package-lock.json` and `.github/state/dotnet-releases.json` are machine-written, so gating them on formatting would only redden a build nobody can fix by editing. `lychee.toml` is unformatted: Prettier has no built-in TOML parser, and a third-party plugin is not worth one hand-written file.
- CI checks live in `scripts/` as .NET file-based apps, run with `dotnet run scripts/<name>.cs` from the repository root; anything two of them need is a helper file they `#:include` rather than a copy. Entry points are kebab-case because they are typed on a command line; helper files take the name of the type they contain, following C# file-naming convention. Add a check there, not as an inline script in a workflow, and hold it to the repository's own opinions (`global.json` pins the SDK, `TreatWarningsAsErrors`, latest language version, and the `[scripts/*.cs]` block in `.editorconfig` turns the analyzer set into build failures), so the repository is held to the bar it publishes. Adding or renaming one means updating the [Repository scripts](README.md#repository-scripts) table and the layout tree. `#:package` pins are bumped by Renovate, which reads the directive but only scans `.cs` because `renovate.json` opts `scripts/` into the nuget manager, so a check placed elsewhere would go unbumped.
- Links are checked by the `Markdown` workflow (`lychee.toml`) and **block pull requests**, because a dead citation is a content defect. If a failure is a third-party host being down rather than link rot, re-run the job once it recovers; do not merge past a red link check.
