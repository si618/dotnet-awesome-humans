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

Per [AGENTS.md: Orchestration and model economy](../../AGENTS.md#orchestration-and-model-economy), fan the per-source reading out to worker agents; the editor alone weighs conflicting sources, assembles the research topic, and answers follow-ups.

## Steps

1. **Clarify intent in at most one question**, and only when the request forks: _learning_ it (explain + idioms), _deciding_ on it (trade-offs + maturity), or _migrating_ to it (diffs from the old way + breaking changes). A clear request gets researched immediately.
2. **Start a worktree** on branch `research/<topic-slug>` (e.g. `research/union-types`), per the worktree and branch-name rules in [AGENTS.md](../../AGENTS.md). One per topic, created before the first write, so the research topic and the `last-used` bumps land together. The slug is the topic kebab-cased as the repository already names it (`union-types`, not `union-type`), so branch, research topic and opinions share one search term.
3. **Start at home.** Read the matching `opinions/` file(s) — the repository may already hold the distilled answer or a "Coming next" aside. Surface **House:**-marked content as "local convention, not community consensus". Update `last-used` frontmatter on every opinion consulted.
4. **Sweep the roster in precedence order:**
   - **Citable sources** weighted by focus match (check the roster's Focus column) — these are quotable.
   - **Watch-list sources** for discovery and cross-checking — label them.
   - **Non-roster material is welcome in research:** recent topics are often covered first by newer, not-yet-vetted voices, and research is where they prove useful. Flag every such citation as **unvetted**, keep unvetted claims visually distinct from roster-sourced ones, and record promising sources as `vet-source` candidates in the closing section. A named package's own documentation, release notes and issues are references rather than unvetted sources, so they need no flag ([AWESOME-HUMANS.md: Admission criteria](../../AWESOME-HUMANS.md#admission-criteria)). Research is permissive; **promotion is the strict gate** (see Lifecycle).
5. **Assemble the research topic:**
   - Answer-first: the recommendation or state-of-play in the opening sentences, depth after.
   - Every claim cites its source id, and says where it stands when that narrows it (`**Corroborate.**`, or a limit its notes record); dates on anything time-sensitive.
   - **Preview features are labelled per the freshness policy:** "in preview as of `<date>`, not yet an opinion" — and clearly separated from GA guidance.
   - Where sources disagree, say so and weigh them; don't average them into mush.
6. **Converse.** Follow-up questions reuse the gathered material — re-sweep only when the follow-up leaves the researched ground. Stay in the same precedence order.
7. **Close the loop.** End it by noticing what the research revealed about the repository, and recommend (never run unprompted) the matching skill:
   - Topic has no opinion file / no coverage → recommend `resolve-research`, which decides between folding into an existing file and opening a new one.
   - Existing aside or opinion is stale (feature GA'd, guidance superseded) → recommend `refresh-dotnet-versions` or `harvest-sources`.
   - A strong non-roster source carried the research → recommend `vet-source`.
   - Repository already answers it fully → say so; that is the system working, and it can be discarded via `resolve-research`.
   - A previously saved research topic on this subject exists in `research/` → build on it, refresh its dates, and surface its promote-or-discard status.

## Persistence and lifecycle

**Save the research topic by default** to `research/<topic-slug>.md` with the frontmatter a topic carries ([AGENTS.md: Metadata](../../AGENTS.md#metadata)). Skip saving only if the user says the question was throwaway.

Commit the topic and its `last-used` bumps on the `research/<topic-slug>` branch and open a PR to the default branch. Merging puts the research on record; only then does `resolve-research` pick it up, on its own branch and PR. Never fold the promote-or-discard decision into the still-open research PR. If the user declined saving, delete the branch and answer in conversation only.

**Commit and title the PR `research: <topic>`** (e.g. `research: union types`), not `docs: research <topic>`. A distinct type keeps staged research apart from settled opinion edits in `git log`, and lists every topic the repository has taken on. Resolution commits are typed by `resolve-research`.

Every saved topic must eventually resolve, by promotion or discard. `resolve-research` owns both, and both end with the file deleted, so a topic on disk is unresolved by definition. `audit-freshness` reports one past its tolerance as promote-or-discard triage.

## Edge cases

- **Preview-only topics** (the "union types" case): research them fully — that is a legitimate ask — but it must lead with maturity status, and any "when it GAs" guidance is clearly hypothetical.
- **Topic outside .NET entirely**: say the roster has no authority there and stop, rather than improvising from general knowledge dressed up as vetted research.
- **Offline**: answer from `opinions/` alone and say the roster sweep was skipped — an opinions-only answer is still a sourced answer, but its ceiling is the repository's `last-reviewed` dates.
- **Conflicting House and community positions**: present both, in that order, and note the house rationale — the reader may be working outside this repository's conventions.
