---
name: audit-freshness
description: Report every resource in this repository whose last-reviewed date has drifted past tolerance, whose targets lag the latest released .NET/C#/F# versions, or whose sources are no longer on the AWESOME-HUMANS.md roster. Use on a periodic cadence or when asked whether the repository is stale, current, or due for a refresh.
license: See repository LICENSE
compatibility: Requires git; internet access only needed to check latest release versions
metadata:
  repo: dotnet-awesome-humans
  change-flow: report-only
---

# Audit freshness

Make staleness visible. This skill only **reports** — it never edits content. Follow-up work goes to `refresh-dotnet-versions` (version drift) or `harvest-awesome-humans` (content drift).

## Tolerances

| Check                                                | Tolerance                                                                                       |
| ---------------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| `last-reviewed` on any `opinions/` file              | 180 days                                                                                        |
| `last-reviewed` after a new major .NET release ships | 60 days from release                                                                            |
| `targets:` vs latest released .NET/C#/F#             | zero — any lag is a finding                                                                     |
| `sources:` ids                                       | must all exist in `AWESOME-HUMANS.md`                                                           |
| `last-used`                                          | informational only — never a finding, but report never-used resources as candidates for pruning |

## Steps

1. **Collect frontmatter** from every file under `opinions/` and any commented headers in `reference/` files: `targets`, `last-reviewed`, `last-used`, `sources`.
2. **Determine the latest released versions** of .NET, C#, and F# (same sources as `refresh-dotnet-versions`, step 1). Skip this check if offline and say so in the report.
3. **Evaluate each resource** against the tolerances above. Malformed or missing frontmatter is itself a finding (severity: high — the living-repository mechanism depends on it).
4. **Check the roster:** flag `AWESOME-HUMANS.md` sources with no activity recorded in over a year as candidates for `vet-awesome-human` re-evaluation.
5. **Produce the report**, ordered most-stale first:
   - Per resource: path, findings, days over tolerance, and which skill fixes it.
   - Summary counts: fresh / stale / malformed, plus the oldest `last-reviewed` in the repository.
6. **Recommend next actions** — typically "run `refresh-dotnet-versions`" or "run `harvest-awesome-humans`" — but do not run them unless asked.

## Edge cases

- **Brand-new repository** (no opinions yet): report that the audit has nothing to check rather than passing vacuously.
- **A resource that is intentionally version-agnostic** (e.g. naming conventions): may declare `targets: [any]`; version-drift checks skip it, review-age checks still apply.
- **Clock skew / future dates** in frontmatter: report as malformed.
