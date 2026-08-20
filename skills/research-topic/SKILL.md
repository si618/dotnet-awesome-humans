---
name: research-topic
description: Research a .NET topic conversationally using this repository's opinions and vetted sources — e.g. "research union types", "what's the story on hybrid caching", "should we adopt Native AOT". Produces a cited research topic where every claim carries its source and tier, respects the freshness policy's preview labeling, and closes by noticing what the research reveals about the repository (missing opinion, stale aside, GA'd feature). Use whenever the user wants to understand, evaluate, or decide on a .NET subject rather than change the repository.
license: See repository LICENSE
compatibility: Requires internet access for source reading; degrades to opinions-only offline
metadata:
  repo: dotnet-awesome-humans
  change-flow: branch-pr
---

# Research a topic

Answer "what should I know / do about X?" using the trust boundary this repository maintains: its own opinions first, then the vetted roster. The only files this skill writes are its own research topic and the `last-used` dates of the opinions it consulted, both on a research branch — changes to `opinions/` and `templates/` happen only via the existing skills it may recommend at the end.

## Orchestration

Where the host supports worker agents, fan the per-source reading out to **lower-cost worker agents** in parallel — they fetch and return raw findings (claims, dates, URLs) only. The orchestrating model acts as the **editor**: it alone weighs conflicting sources, assembles the research topic, and answers follow-ups.

## Steps

1. **Clarify intent in at most one question**, and only when the request genuinely forks: _learning_ it (explain + idioms), _deciding_ on it (trade-offs + maturity), or _migrating_ to it (diffs from the old way + breaking changes). A clear request gets researched immediately, no ceremony.
2. **Start a research branch** (never the default branch): `git switch -c research/<topic-slug>` — e.g. `research/union-types`. One branch per topic, created before the first write, so the research topic and the `last-used` bumps land together and stay reviewable. The slug is the topic kebab-cased as the repository already names it — `union-types`, not `union-type` — so branch, research topic, and opinions stay searchable by one term. Respect any branch-naming convention in the host environment's instructions; where the host mandates a prefix, keep `research/<topic-slug>` as the trailing part so the topic stays legible.
3. **Start at home.** Read the matching `opinions/` file(s) — the repository may already hold the distilled answer or a "Coming next" aside. Surface **House:**-marked content as "local convention, not community consensus". Update `last-used` frontmatter on every opinion consulted.
4. **Sweep the roster in precedence order:**
   - **Tier 1/2 sources** weighted by focus match (check the roster's Focus column) — these are quotable.
   - **Watch-list sources** for discovery and cross-checking — label them.
   - **Non-roster material is welcome in research** — recent topics are often covered first by newer, not-yet-vetted voices, and research is where they prove useful. Flag every such citation as **unvetted**, keep unvetted claims visually distinct from roster-sourced ones, and record promising sources as `vet-source` candidates in the closing section. Research is permissive; **promotion is the strict gate** (see Lifecycle).
5. **Assemble the research topic:**
   - Answer-first: the recommendation or state-of-play in the opening sentences, depth after.
   - Every claim cites its source id (and tier); dates on anything time-sensitive.
   - **Preview features are labeled per the freshness policy** — "in preview as of `<date>`, not yet an opinion" — and clearly separated from GA guidance.
   - Where sources disagree, say so and weigh them; don't average them into mush.
6. **Converse.** Follow-up questions reuse the gathered material — re-sweep only when the follow-up leaves the researched ground. Stay in the same precedence order.
7. **Close the loop.** End it by noticing what the research revealed about the repository, and recommend (never run unprompted) the matching skill:
   - Topic has no opinion file / no coverage → recommend `resolve-research`, which decides between folding into an existing file and opening a new one.
   - Existing aside or opinion is stale (feature GA'd, guidance superseded) → recommend `refresh-dotnet-versions` or `harvest-sources`.
   - A strong non-roster source carried the research → recommend `vet-source`.
   - Repository already answers it fully → say so; that is the system working, and it can be discarded via `resolve-research`.
   - A previously saved research topic on this subject exists in `research/` → build on it, refresh its dates, and surface its promote-or-discard status.

## Persistence and lifecycle

**Save the research topic by default** to `research/<topic-slug>.md` with the standard frontmatter (`targets`, `last-reviewed`, `last-used`, `sources`) plus a `status:` field — skip saving only if the user says the question was throwaway. A saved research topic records the state of a moment; the frontmatter dates are its honesty mechanism.

The research topic and its `last-used` bumps are committed on the `research/<topic-slug>` branch from step 2 and opened as a PR to the default branch — the same branch-and-review flow every other writing skill uses. The topic slug is shared by the branch and the file, so `research/union-types` the branch carries `research/union-types.md` the file. If the user declined saving, the branch is never needed; delete it and answer in conversation only.

**Commit and title the PR `research: <topic>`, not `docs: research <topic>`.** A research topic is its own kind of change — staged, dated, and destined to be promoted or discarded — and giving it a distinct type makes that visible in `git log` and lets one command list every research topic the repository has ever taken on. Filing them under `docs` buries them among opinion edits, which are the opposite thing: settled, not staged. Use the topic as the subject, matching the slug in the branch and filename, so `research/union-types` carries `research: union types`. Promotion is not research and keeps the type of whatever it changes — weaving a research topic into an opinion is a `docs:` commit.

Every saved research topic must eventually resolve — `research/` is a staging area, not a second opinions directory:

- `status: open` — freshly saved; awaiting a promote-or-discard decision. It is the only resting value: both resolutions end with the file deleted, so anything on disk is by definition unresolved.
- **Promote**: the recommendation is woven into the matching `opinions/` file(s) under the normal rules — **only roster-sourced claims may promote**; an unvetted claim first needs its source admitted via `vet-source` or corroboration from a roster source. After weaving, the file is deleted and the promotion is recorded in the PR that weaves it. `resolve-research` owns this.
- **Discard**: delete the file — research that answered a moment's question owes nothing further. No tombstone needed; git history is the record. `resolve-research` owns this too; the two outcomes are one decision.
- `audit-freshness` covers `research/` like everything else: an `open` research topic older than 90 days is reported as promote-or-discard triage, so they cannot silently accumulate as unratified pseudo-opinions.

## Edge cases

- **Preview-only topics** (the "union types" case): research them fully — that is a legitimate ask — but it must lead with maturity status, and any "when it GAs" guidance is clearly hypothetical.
- **Topic outside .NET entirely**: say the roster has no authority there and stop, rather than improvising from general knowledge dressed up as vetted research.
- **Offline**: answer from `opinions/` alone and say the roster sweep was skipped — an opinions-only answer is still a sourced answer, but its ceiling is the repository's `last-reviewed` dates.
- **Conflicting House and community positions**: present both, in that order, and note the house rationale — the reader may be working outside this repository's conventions.
