---
name: audit-freshness
description: Report every resource in this repository whose last-reviewed date has drifted past tolerance, whose targets lag the latest released .NET/C#/F# versions, whose template version pins (global.json SDK band, Directory.Packages.props package versions) lag the latest stable releases, or whose sources are no longer on the AWESOME-HUMANS.md roster. Use on a periodic cadence or when asked whether the repository is stale, current, or due for a refresh.
license: See repository LICENSE
compatibility: Requires git; internet access only needed to check latest release versions
metadata:
  repo: dotnet-awesome-humans
  change-flow: report-only
---

# Audit freshness

Make staleness visible. This skill only **reports** — it never edits content. Follow-up work goes to `refresh-dotnet-versions` (version drift) or `harvest-sources` (content drift).

## Tolerances

| Check                                                                                      | Tolerance                                                                                                                                                                                            |
| ------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `last-reviewed` on any `opinions/` file                                                    | 180 days                                                                                                                                                                                             |
| `last-reviewed` after a new major .NET release ships                                       | 60 days from release                                                                                                                                                                                 |
| `targets:` vs latest released .NET/C#/F#                                                   | zero — any lag is a finding                                                                                                                                                                          |
| `last-reviewed` on any commented `templates/` header                                       | 180 days, same as `opinions/`                                                                                                                                                                        |
| `templates/global.json` `sdk.version` vs latest GA SDK feature band                        | one feature band (e.g. pinned `10.0.400` against a released `10.0.500` is a finding)                                                                                                                 |
| `<PackageVersion>` pins in `templates/Directory.Packages.props` vs nuget.org latest stable | one minor version, or any major — patch-only drift is informational                                                                                                                                  |
| `Versions verified against nuget.org … on <date>` comment in `Directory.Packages.props`    | 90 days — the pins move faster than the prose, so they get a tighter leash                                                                                                                           |
| `research/` briefs with `status: open`                                                     | 90 days — then reported as promote-or-discard triage (see `research-topic`); `promoted` lingering on disk is itself a finding (promotion ends in deletion)                                           |
| `sources:` ids                                                                             | must all exist in `AWESOME-HUMANS.md` roster tables, or be the reserved `house` id (never a finding). `research/` briefs only: unvetted source names are permitted when flagged as such in the brief |
| `last-used`                                                                                | informational only — never a finding, but report never-used resources as candidates for pruning                                                                                                      |
| Citation and roster URLs resolving                                                         | zero — a dead link is a content finding, not a formatting nit. Checked by the `Markdown` workflow (`lychee.toml`); read its most recent run rather than re-checking every URL by hand                |

## Steps

1. **Collect frontmatter** from every file under `opinions/` and any commented headers in `templates/` files: `targets`, `last-reviewed`, `last-used`, `sources`.
2. **Determine the latest released versions** of .NET, C#, and F# (same sources as `refresh-dotnet-versions`, step 1), plus the latest GA SDK feature band and the latest stable version of every package pinned in `templates/Directory.Packages.props` (same lookups as `refresh-dotnet-versions`, steps 4a and 4b). Skip whichever of these checks is unavailable if offline and say so in the report — an unchecked pin is reported as "not verified", never as fresh.
3. **Evaluate each resource** against the tolerances above. Malformed or missing frontmatter is itself a finding (severity: high — the living-reference mechanism depends on it).
4. **Check the roster:** flag `AWESOME-HUMANS.md` sources with no activity recorded in over a year as candidates for `vet-source` re-evaluation.
5. **Produce the report**, ordered most-stale first:
   - Per resource: path, findings, days over tolerance, and which skill fixes it.
   - Summary counts: fresh / stale / malformed, plus the oldest `last-reviewed` in the repository.
6. **Recommend next actions** — typically "run `refresh-dotnet-versions`" or "run `harvest-sources`" — but do not run them unless asked.

## Edge cases

- **Brand-new repository** (no opinions yet): report that the audit has nothing to check rather than passing vacuously.
- **House content** (`house` in `sources:`, `**House:**` markings — see HOUSE-OPINIONS.md): the reserved id is never an unknown-source finding, and this skill must never suggest stripping it. A `house` id with no `**House:**`-marked content in the file (or vice versa) IS a finding — the marking and the id travel together.
- **Files that cannot carry a comment header** (`templates/global.json`, `.slnx`/`.slnf`, JSON generally): they have no `last-reviewed`, so absence of frontmatter is not a malformed-frontmatter finding for them. Audit the pinned value itself against the latest release instead.
- **A package pin deliberately held back** (a known-bad release, or a major whose migration is tracked elsewhere): still report the drift, but as informational once the reason is recorded as a comment beside the pin. Silent staleness and deliberate staleness must look different in the report.
- **A resource that is intentionally version-agnostic** (e.g. naming conventions): may declare `targets: [any]`; version-drift checks skip it, review-age checks still apply.
- **Clock skew / future dates** in frontmatter: report as malformed.
