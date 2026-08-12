---
name: verify-project
description: Check an external .NET project or solution against this repository's opinions and reference files, and report deviations with severity and suggested fixes. Use when asked to verify, audit, review, or grade a .NET codebase against dotnet-awesome-humans conventions, or to check whether a project "does what good looks like".
license: See repository LICENSE
compatibility: Requires read access to the target project; .NET SDK helpful but optional
metadata:
  repo: dotnet-awesome-humans
  change-flow: report-only
---

# Verify project

Compare a target .NET project against the opinions and `reference/` files in this repository and report deviations. This skill **reports by default** — it only applies fixes when the user explicitly asks.

## Steps

1. **Locate the target** (path given by the user) and inventory it: solution files (`.slnx`/`.sln`/`.slnf`), project files, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, test projects, language mix (C#/F#).
2. **Update `last-used`** on every opinion and reference file consulted during this run (frontmatter, ISO 8601 date) — this is how the repository knows it is being used.
3. **Compare structure and tooling** against `reference/`:
   - Missing files the opinions mandate (e.g. no `.editorconfig`, no central package management).
   - Present-but-divergent files: diff against the reference and classify each divergence as _violation_ (contradicts an opinion) or _local choice_ (the opinions are silent).
4. **Compare versions:** TFMs, `LangVersion`, and SDK version against the `targets:` declared in the opinions. Older LTS targets are findings, per the currency policy — note them even if the project has reasons.
5. **Compare code-level opinions** for areas the target actually uses (ASP.NET Core, testing, F#, etc.) — sample representative files rather than exhaustively reading everything, and say what was sampled.
6. **Produce the report:**
   - Findings grouped by severity: **violation** (contradicts an opinion — cite the opinion file), **drift** (older versions), **gap** (missing scaffolding), **observation** (local choices worth a look).
   - Each finding: file/path in the target, what the opinion says, one-line suggested fix (ideally "copy `reference/<file>` and trim").
   - A short verdict paragraph a human can read standalone.
7. **Only if asked to fix:** apply changes in the _target_ project on a working branch (never its default branch; follow the host environment's branch-naming convention), starting with gaps and drift — mechanical fixes first, opinionated rewrites only with explicit approval.

## Edge cases

- **The target pins an older .NET for a stated reason** (e.g. deployment constraint documented in its README): still report the drift, but mark it acknowledged rather than actionable.
- **F#-only or mixed solutions:** verify against the F# opinions too — do not report C#-specific conventions as violations in F# projects.
- **No opinions exist yet for something the target does:** report it as out of scope, and note it as a candidate opinion gap for `harvest-awesome-humans`.
- **The reference files and opinions disagree** (repository bug): report the inconsistency against _this_ repository, and verify the target against the opinion text, which wins.
