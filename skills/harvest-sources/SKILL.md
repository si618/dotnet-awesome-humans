---
name: harvest-sources
description: Sweep the vetted sources in AWESOME-HUMANS.md for new posts and updates, then fold notable guidance into the opinion files. Use on a periodic cadence, or when asked to check what the awesome humans have published lately, or after a major community post (e.g. a new "Performance Improvements in .NET" article) ships.
license: See repository LICENSE
compatibility: Requires git and internet access
metadata:
  repo: dotnet-awesome-humans
  change-flow: branch-pr
---

# Harvest sources

Collate what the vetted sources have published since the last sweep and refine the repository's opinions with it. The humans provide the wisdom; this skill keeps the repository current with it.

## Orchestration

Where the host supports worker agents, fan the per-source sweeps (step 3) out to **lower-cost worker agents** in parallel — they fetch, search, and return raw findings (links, dates, extracted claims) only. The orchestrating model acts as the **editor**: it alone triages findings, resolves conflicts between sources, and edits `opinions/` and `templates/`. Never delegate the final edit to a cheap model.

## Steps

1. **Load the roster** from [AWESOME-HUMANS.md](../../AWESOME-HUMANS.md). Only Tier 1 and Tier 2 sources feed opinions. Watch-list sources are for discovery and cross-checking only — anything found there needs a Tier 1/2 corroboration or a `vet-source` admission before it can shape an opinion.
2. **Determine the sweep window.** Use the most recent harvest entry in the decision log of `AWESOME-HUMANS.md` (or the newest `last-reviewed` date across `opinions/` if none). Sweep from then to today.
3. **Sweep each source** for posts in the window. Sources whose roster Notes carry the `**Discovery-only.**` marking (currently `awesome-dotnet`, `fsharp-weekly`, `csharp-digest`, `awesome-blazor`, `awesome-avalonia`) are leads to primary posts — follow the links; never cite the discovery source itself in `sources:`, which CI rejects.
4. **Triage each notable post** into one of:
   - **Changes an existing opinion:** the guidance supersedes or refines something in `opinions/`. Queue an edit.
   - **Suggests a new opinion:** a recurring theme with no home yet. Create a stub opinion with frontmatter, the source link, and a `TODO`, then index it in `README.md` (a linked Scope bullet and a Repository layout entry) in the same commit — CI fails the PR otherwise.
   - **Noise:** release chatter, product marketing, one-off tips that don't generalize. Skip.
5. **Apply the edits on a working branch** (never the default branch; follow the host environment's branch-naming convention):
   - Keep opinions opinionated — one recommendation. If a new post contradicts the current opinion, prefer the stronger-sourced or better-evidenced position and note the supersession in one line.
   - **Never remove, dilute, or un-mark house content** (`house` source id, `**House:**` marking — canonical rules in [HOUSE-OPINIONS.md: How this works](../../HOUSE-OPINIONS.md#how-this-works)). If a source contradicts a house opinion, the house opinion stands per those rules; if a source newly agrees, add the citation alongside the marking — the marking stays.
   - Add the post's source id to the opinion's `sources:` frontmatter and update `last-reviewed:`.
6. **Record the sweep** in the `AWESOME-HUMANS.md` decision log: date, window covered, sources swept, posts folded in.
7. **Open a PR** to the default branch summarizing per-source findings and per-opinion changes. A human reviews before it becomes "the opinion".

## Edge cases

- **Conflicting guidance between vetted sources:** present both in the PR description and pick one for the opinion, stating why. Never leave a menu in the opinion file.
- **A source has gone quiet or its content quality has dropped:** note it in the PR; propose demotion via `vet-source` rather than editing the roster inline.
- **Preview-version content:** may be captured in a "coming next" aside but never becomes the opinion (see repository freshness policy).
- **Nothing notable found:** still record the sweep in the decision log (on the default branch or via a trivial PR) so the window tracking stays honest.
