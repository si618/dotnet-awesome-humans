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

Where the host supports worker agents, delegate the evidence gathering (step 2's archive digging and post sampling) to **lower-cost worker agents** — they return dates, links, and extracted observations only. The orchestrating model acts as the **editor**: it alone weighs the evidence, makes the admission decision, and edits the roster.

## Steps

1. **Identify the candidate** (URL, author, focus area) and check the roster: already listed? Then this is a re-evaluation (promotion/demotion), not an admission.
2. **Establish the track record.** Gather evidence for each criterion:
   - **Longevity:** earliest verifiable publication (archives, post history, Wayback Machine). Tier 1 needs 5+ years of sustained output; Tier 2 covers 2–5 years. Under two years is the watch list regardless of quality.
   - **Depth:** sample 3–5 representative posts. Original insight (internals, measurements, worked reasoning) or paraphrased release notes?
   - **Accuracy:** any history of corrections issued, or of claims later shown wrong and left standing?
   - **Independence of signal:** does the content stand on merit, or on marketing reach / algorithm-chasing? Vendor blogs and personality-driven channels need extra scrutiny here.
3. **Classify**, applying the two qualifying rules from [AWESOME-HUMANS.md: Admission criteria](../../AWESOME-HUMANS.md#admission-criteria) exactly as written there:
   - All four criteria met at Tier 1/2 longevity → **admit** at the appropriate tier.
   - **Independence caps tier**: Tier 1 longevity with a live independence concern (vendor DevRel or product-team employment producing adoption-focused content with little critical distance) → admit at **Tier 2**, concern recorded in Notes. Vendor employment alone is not a cap — depth with critical distance keeps full tier.
   - **Dormancy blocks admission**: publishing stopped for over a year → **watch list**, unless their **writing** demonstrably continues elsewhere (official documentation, another publication, a book), in which case the track record follows the human and the dormant channel is noted.
   - Strong on depth/accuracy but short on longevity → **watch list**, with the blocker recorded.
   - Aggregators (link roundups, newsletters) → admissible, but marked as **discovery-only**; they never appear in an opinion's `sources:`.
   - Otherwise → **decline**, with a one-line reason (kept only in the PR, not the roster).
4. **For re-evaluations:** promote a watch-list source whose blocker has cleared; demote or annotate an admitted source that has gone dormant (no posts in over a year) or declined in quality. A demoted source keeps its row with a note, but **demotion to the watch list revokes citation, including the back catalogue** — `scripts/validate-opinion-sources.cs` rejects a watch-list id wherever it appears in `sources:`. So a demotion is not finished until no opinion cites it: in the same PR, re-source each affected claim from a Tier 1/2 source, or drop it. A back catalogue worth keeping citable is an argument for annotating the row rather than demoting it.
5. **Apply on a working branch** (never the default branch; follow the host environment's branch-naming convention): update the appropriate roster table, assign a stable kebab-case `id` and insert the row alphabetically by that id, and append the decision with evidence summary to the decision log.
6. **Open a PR** with the evidence per criterion so a human ratifies the admission. A human reviews before the source can feed opinions.

## Edge cases

- **Institutional sources** (Microsoft, JetBrains): longevity attaches to the publication, not individual authors; depth still needs per-author scrutiny when citing.
- **An author who moved platforms** (e.g. personal blog → an employer's engineering blog → newsletter): the track record follows the human, not the URL, so aggregate their writing across platforms. A move to video, talks or a podcast is not a platform change but an exit from scope ([AWESOME-HUMANS.md: Admission criteria](../../AWESOME-HUMANS.md#admission-criteria)).
- **Candidate found via a single viral post:** never admit on one post; watch-list at most.
- **Conflicts of interest** (the candidate sells a product the opinions might recommend): admissible, but note it in the roster's Notes column so opinions citing them flag it.
