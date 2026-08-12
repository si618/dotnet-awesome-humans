---
name: weave-house-opinion
description: Weave a repository-owner opinion from HOUSE-OPINIONS.md (or given directly) into the opinion and template files, marked as House and kept distinguishable from source-derived opinions. Use when the owner adds an entry to the HOUSE-OPINIONS.md inbox, states a personal preference to record, or when a contributor PR proposes an experience-based opinion that the owner accepts.
license: See repository LICENSE
compatibility: Requires git
metadata:
  repo: dotnet-awesome-humans
  change-flow: branch-pr
---

# Weave a house opinion

House opinions are the owner's preferences — first-class, but never disguised as community best practice. This skill folds them into the living reference while keeping the two visibly separate.

## Steps

1. **Take the entry** from the HOUSE-OPINIONS.md inbox (or from the owner's request directly — in that case, add it to the inbox first so the audit trail starts there).
2. **Find its home**: the matching `opinions/` file and, where the opinion changes scaffolding (an `.editorconfig` rule, a build property), the matching `templates/` file. A house opinion with no natural home yet gets a new opinion file, same rules as any other.
3. **Weave it in, marked**:
   - In `opinions/`: write it in the house style (opinion first, rationale after) prefixed with **House:** in bold, and add `house` to the file's `sources:` frontmatter.
   - In `templates/`: apply the change and add `house` to the header's `sources:` list (the pipe-delimited comment header audit-freshness reads), with an inline comment on the changed setting where the format allows.
4. **Handle conflicts explicitly** per the canonical precedence rule in [HOUSE-OPINIONS.md — How this works](../../HOUSE-OPINIONS.md#how-this-works): house wins, the sourced position stays as the one-line cited note. Never silently delete the sourced view.
5. **Move the inbox entry to the Woven table** in HOUSE-OPINIONS.md with the date and destination link.
6. **Update frontmatter** (`last-reviewed`) on every file touched, then open a PR to the default branch (work on a branch; follow the host environment's branch-naming convention). Title it so the house origin is obvious, e.g. `docs: weave house opinion — <title>`.

## Rules

- **Never launder a house opinion into a sourced one.** If a vetted source later publishes the same guidance, the harvest may add the source citation alongside the house marking — the marking stays.
- **House opinions are exempt from source tracing but not from quality**: code examples must still compile against the declared `targets`, and the opinion must still be one recommendation, not a menu.
- **Only the repository owner's opinions weave directly.** Contributor-proposed opinions arrive by PR (see `.github/PULL_REQUEST_TEMPLATE.md`) and only weave after the owner accepts them — at which point they are house opinions credited in the PR history.
- Harvest and refresh skills must not remove or dilute house-marked content; they update the sourced context around it.

## Edge cases

- **The owner changes their mind**: weave the reversal the same way and move the old Woven row's entry note to reflect supersession — the audit trail is append-only in spirit; do not rewrite history.
- **A house opinion becomes obsolete** (e.g. the framework now does it automatically): mark it superseded in place with the release that obsoleted it, citing the source.
