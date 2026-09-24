---
name: vet-source
description: Evaluate a candidate source (blog, author, documentation site, newsletter) against the AWESOME-HUMANS.md admission criteria and admit, watch-list, decline, promote, or demote it. Use when proposing a new source, when a watch-list source may be ready for promotion, or when an admitted source has gone quiet or declined in quality.
license: See repository LICENSE
compatibility: Requires git and internet access
metadata:
  repo: dotnet-awesome-humans
  change-flow: branch-pr
---

# Vet a source

Apply the admission criteria in [AWESOME-HUMANS.md](../../AWESOME-HUMANS.md) to a candidate source, or re-evaluate an existing one. The roster is the trust boundary for every opinion in this repository — err on the side of declining.

## Orchestration

Per [AGENTS.md: Orchestration and model economy](../../AGENTS.md#orchestration-and-model-economy), delegate the evidence gathering (step 2's archive digging and post sampling) to worker agents; the editor alone weighs the evidence, decides, and edits the roster.

## Steps

1. **Identify the candidate** (URL, author, focus area) and check the roster: already listed? Then this is a re-evaluation (promotion/demotion), not an admission.
2. **Establish the track record.** Gather evidence for each criterion:
   - **Longevity:** earliest verifiable publication (archives, post history, Wayback Machine). Admission needs two years of sustained output; under two years is the watch list regardless of quality. Record the start year precisely — it goes in the roster's `Since` column, which is what carries the length of a record now that there is one citable tier.
   - **Depth:** sample 3–5 representative posts. Original insight (internals, measurements, worked reasoning) or paraphrased release notes?
   - **Accuracy:** any history of corrections issued, or of claims later shown wrong and left standing?
   - **Independence of signal:** does the content stand on merit, or on marketing reach / algorithm-chasing? Vendor blogs and personality-driven channels need extra scrutiny here.
3. **Classify**, applying the two qualifying rules from [AWESOME-HUMANS.md: Admission criteria](../../AWESOME-HUMANS.md#admission-criteria) exactly as written there:
   - All four criteria met at two years or more → **admit** to the **Citable** table.
   - **An independence concern is a recorded usage limit, never a rank**: a live concern (vendor DevRel or product-team employment producing adoption-focused content with little critical distance) → admit, and write into Notes what a citation may rest on — typically mechanism rather than adoption. Vendor employment alone needs no limit; depth with critical distance carries itself. A concern no limit would contain is a decline or a watch-listing, not an admission with a weaker limit.
   - **Dormancy blocks admission**: publishing stopped for over a year → **watch list**, unless their **writing** demonstrably continues elsewhere (official documentation, another publication, a book), in which case the track record follows the human and the dormant channel is noted. Repository activity is not evidence: commits, releases and issue threads are not what an opinion cites, so a busy GitHub profile beside a silent blog is a dormant source rather than a live one. Documentation authored in a repository does count — what was published is the test, not where the commits landed.
   - Strong on depth/accuracy but short on longevity → **watch list**, with the blocker recorded.
   - Aggregators (link roundups, newsletters) → admissible, but marked as **discovery-only**; they never appear in an opinion's `sources:`.
   - Thin on depth but sound on the other three → admit and mark **`**Corroborate.**`**. The table states that a source is citable; the marking states what a citation may rest on.
   - Otherwise → **decline**, with a one-line reason (kept only in the PR, not the roster).
4. **For re-evaluations:** promote a watch-list source whose blocker has cleared; demote or annotate an admitted source that has gone dormant (no posts in over a year) or declined in quality. With one citable tier, an admitted source that slips has two outcomes rather than three: a tightened usage limit in its notes, or the watch list. A demoted source keeps its row with a note, but **demotion to the watch list revokes citation, including the back catalogue** — `scripts/validate-sources.cs` rejects a watch-list id wherever it appears in `sources:`. So a demotion is not finished until no opinion cites it: in the same PR, re-source each affected claim from a citable source, or drop it. A back catalogue worth keeping citable is an argument for annotating the row rather than demoting it.
5. **Apply in a worktree** on branch `vet/<source-id>`, or a slug naming a multi-source pass, per [AGENTS.md](../../AGENTS.md): update the roster table, assign a stable kebab-case `id` and insert the row alphabetically by it, add the source's section under `## Source notes` (also alphabetical) with the evidence and any marking, and append the decision to the decision log. **Keep the log entry to the decision and the fact behind it**, in two sentences. The per-criterion evidence belongs in the PR (step 6), not the table.
6. **Open a PR** with the evidence per criterion so a human ratifies the admission. A human reviews before the source can feed opinions.

## Edge cases

- **Institutional sources** (Microsoft, JetBrains): longevity attaches to the publication, not individual authors; depth still needs per-author scrutiny when citing.
- **An author who moved platforms** (e.g. personal blog → an employer's engineering blog → newsletter): the track record follows the human, not the URL, so aggregate their writing across platforms. A move to video, talks or a podcast is not a platform change but an exit from scope ([AWESOME-HUMANS.md: Admission criteria](../../AWESOME-HUMANS.md#admission-criteria)).
- **Candidate found via a single viral post:** never admit on one post; watch-list at most.
- **Conflicts of interest** (the candidate sells a product the opinions might recommend): admissible, but record it in the source's section under `## Source notes` so opinions citing them flag it.
