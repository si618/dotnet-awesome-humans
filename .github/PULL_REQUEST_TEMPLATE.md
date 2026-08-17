<!-- Thanks for contributing! For opinion/template changes, fill in the section below.
     For mechanical fixes (typos, broken links, CI), delete it and describe the fix. -->

## Proposed opinion

**Opinion (one sentence, one recommendation — not a menu):**

**Why (rationale, trade-offs, what it replaces):**

**Where it belongs (`opinions/` / `templates/` file):**

## Provenance — pick one

- [ ] **Sourced** — traces to a vetted source in [AWESOME-HUMANS.md](../AWESOME-HUMANS.md). Source id(s) and links:
- [ ] **Candidate source** — traces to a source not yet on the roster (it will need to pass [`vet-source`](../skills/vet-source/SKILL.md) before the opinion can merge). Source and evidence of track record:
- [ ] **Experience-based** — from my own practice, no published source. If the owner accepts, they **adopt** it as a house opinion ([HOUSE-OPINIONS.md](../HOUSE-OPINIONS.md)) — the owner takes responsibility for it, and this PR and its author are credited in the Woven table's Provenance column.

## Checklist

- [ ] Opinion-first style: recommendation in the first sentence, rationale after, sources last (see [AGENTS.md](../AGENTS.md))
- [ ] Code examples compile against the declared `targets:` (state the SDK you built with)
- [ ] Frontmatter complete on every file touched — all four CI-validated fields: `targets`, `last-reviewed`, `last-used`, `sources`
- [ ] New, renamed, or removed `opinions/` file indexed in [README.md](../README.md) — a linked **Scope** bullet and a **Repository layout** entry, with no stale ones left behind (CI-validated)
- [ ] Markdown formatted with `npx prettier --prose-wrap preserve --write <file>`
- [ ] No preview-version features stated as opinions (previews go in "Coming next" asides)
