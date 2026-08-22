---
name: verify-project
description: Check an external .NET project or solution — given a local path or a repository URL — against this repository's opinions and templates, and report deviations with severity and suggested fixes. Use when asked to verify, audit, review, or grade a .NET codebase against dotnet-awesome-humans conventions, or to check whether a project "does what good looks like".
license: See repository LICENSE
compatibility: Requires read access to the target project; git and internet access for URL targets; .NET SDK helpful but optional
metadata:
  repo: dotnet-awesome-humans
  change-flow: report-only
---

# Verify project

Compare a target .NET project against the opinions and `templates/` files in this repository and report deviations. This skill **reports by default** — it only applies fixes when the user explicitly asks.

## Steps

1. **Resolve the target**, given either a local path or a repository URL:
   - **Local path:** read it in place; never write to it during the review.
   - **URL:** read it remotely through whatever repository browsing the host offers. Cloning is for _writing_, not reading: clone only when a fix is requested (step 9), or when the host cannot browse the tree well enough to sample files, in which case a shallow `git clone --depth 1` into a scratch location is a local read cache — never a write to the target.
   - **Record what was read.** For a URL: the branch and commit sha — a review without a sha cannot be reproduced, and step 9 branches from it. For a local path: the sha when it is a git worktree, plus a note when that tree is dirty, since the findings then describe the working tree and not the commit; when it is not a git checkout at all, say so and let the file paths stand as the record.
2. **Inventory the target**: solution files (`.slnx`/`.sln`/`.slnf`), project files, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, test projects, language mix (C#/F#).
3. **Update `last-used`** on every opinion and template file consulted during this run — YAML frontmatter on an opinion, the first-line comment header on a template, always an ISO 8601 date (see [AGENTS.md: Metadata](../../AGENTS.md#metadata)). This is how the repository knows it is being used.
4. **Compare structure and tooling** against `templates/`:
   - Missing files the opinions mandate (e.g. no `.editorconfig`, no central package management).
   - Present-but-divergent files: diff against the template and classify each divergence as _violation_ (contradicts an opinion) or _local choice_ (the opinions are silent).
5. **Compare versions:** TFMs, `LangVersion`, and SDK version against the `targets:` declared in the opinions. Older LTS targets are findings, per the freshness policy — note them even if the project has reasons.
6. **Compare code-level opinions** for areas the target actually uses (ASP.NET Core, testing, F#, etc.) — sample representative files rather than exhaustively reading everything, and say what was sampled.
7. **Produce the report:**
   - Findings grouped by severity: **violation** (contradicts an opinion — cite the opinion file), **drift** (older versions), **gap** (missing scaffolding), **observation** (local choices worth a look).
   - Each finding: file/path in the target, what the opinion says, one-line suggested fix (ideally "copy `templates/<file>` and trim").
   - When the violated opinion is `**House:**`-marked (see HOUSE-OPINIONS.md), say so — the target may reasonably follow the community default instead of this repository's local convention; grade those findings one level lower.
   - A short verdict paragraph a human can read standalone.
8. **Ask whether to save the report, and where.** Once the report has been presented, ask in one question — never save silently, and never assume a location. Offer a default of `verify-<target-name>-<YYYY-MM-DD>.md` in the working directory the user invoked the skill from, and accept any path they name instead; `<target-name>` is the target repository or solution name, so two reviews of different projects on the same day don't collide. Write it as a single Markdown file: the same content as the conversational report, led by a heading naming the target and the sha (or dirty-worktree note) recorded in step 1, so the file stands alone once it's out of the conversation. If the user declines, the report lives in the conversation only and nothing is written.

   **Never save the report inside this repository.** It describes someone else's code, and `opinions/`, `templates/`, and `research/` are all the wrong home for it — a verification report is an output about a target, not a resource this repository maintains. Saving into the _target_ is fine if the user asks for that, but it is a write to a tree step 1 promised not to touch, so it needs the same explicit go-ahead as a fix.

9. **Only if asked to fix:** apply changes in the _target_ project on a working branch (never its default branch; follow the host environment's branch-naming convention), starting with gaps and drift — mechanical fixes first, opinionated rewrites only with explicit approval. A URL target reviewed remotely is cloned at this point, and the branch starts from **the sha recorded in step 1** — clone without `--depth 1`, or `git fetch origin <full sha>` to deepen the shallow read cache (the abbreviated sha is rejected there), since a depth-1 clone holds no earlier commit to branch from. If the branch must start from a default branch that has since moved, re-check the files behind each finding and say which ones changed. Fixes written against code that has moved are the one way this skill produces a confidently wrong diff.

## Edge cases

- **The URL is unreachable, private with no credentials to read it, or not a git repository:** say so and stop — never verify from README prose, a package listing, or memory of the project.
- **No push access to the target** (someone else's repository): make the branch in the clone and hand back a diff or patch. Never push to a repository the user does not control.
- **The target pins an older .NET for a stated reason** (e.g. deployment constraint documented in its README): still report the drift, but mark it acknowledged rather than actionable.
- **F#-only or mixed solutions:** verify against the F# opinions too — do not report C#-specific conventions as violations in F# projects.
- **No opinions exist yet for something the target does:** report it as out of scope, and note it as a candidate opinion gap for `harvest-sources`.
- **The target spells things differently:** never a finding, in either direction. This repository's spelling rule (AGENTS.md) governs prose written here and says nothing about anyone else's code. British or American, in identifiers, comments or documentation, is the target's own business — report it and you bury the real findings under noise the author never asked for.
- **The template files and opinions disagree** (repository bug): report the inconsistency against _this_ repository, and verify the target against the opinion text, which wins.
