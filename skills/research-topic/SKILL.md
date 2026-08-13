---
name: research-topic
description: Research a .NET topic conversationally using this repository's opinions and vetted sources — e.g. "research union types", "what's the story on hybrid caching", "should we adopt Native AOT". Produces a cited brief where every claim carries its source and tier, respects the freshness policy's preview labeling, and closes by noticing what the research reveals about the repository (missing opinion, stale aside, GA'd feature). Use whenever the user wants to understand, evaluate, or decide on a .NET subject rather than change the repository.
license: See repository LICENSE
compatibility: Requires internet access for source reading; degrades to opinions-only offline
metadata:
  repo: dotnet-awesome-humans
  change-flow: report-only
---

# Research a topic

Answer "what should I know / do about X?" using the trust boundary this repository maintains: its own opinions first, then the vetted roster. This skill reads and reports — repository changes happen only via the existing skills it may recommend at the end.

## Orchestration

Where the host supports worker agents, fan the per-source reading out to **lower-cost worker agents** in parallel — they fetch and return raw findings (claims, dates, URLs) only. The orchestrating model acts as the **editor**: it alone weighs conflicting sources, assembles the brief, and answers follow-ups.

## Steps

1. **Clarify intent in at most one question**, and only when the request genuinely forks: _learning_ it (explain + idioms), _deciding_ on it (trade-offs + maturity), or _migrating_ to it (diffs from the old way + breaking changes). A clear request gets researched immediately, no ceremony.
2. **Start at home.** Read the matching `opinions/` file(s) — the repository may already hold the distilled answer or a "Coming next" aside. Surface **House:**-marked content as "local convention, not community consensus". Update `last-used` frontmatter on every opinion consulted (the one repository write this skill makes).
3. **Sweep the roster in precedence order:**
   - **Tier 1/2 sources** weighted by focus match (check the roster's Focus column) — these are quotable.
   - **Watch-list sources** for discovery and cross-checking only — never the sole support for a claim; label them.
   - **Non-roster material only if the user explicitly asks** to go beyond the roster. Flag every such citation as **unvetted**, and record promising ones as `vet-source` candidates in the brief's closing section.
4. **Assemble the brief:**
   - Answer-first: the recommendation or state-of-play in the opening sentences, depth after.
   - Every claim cites its source id (and tier); dates on anything time-sensitive.
   - **Preview features are labeled per the freshness policy** — "in preview as of <date>, not yet an opinion" — and clearly separated from GA guidance.
   - Where sources disagree, say so and weigh them; don't average them into mush.
5. **Converse.** Follow-up questions reuse the gathered material — re-sweep only when the follow-up leaves the researched ground. Stay in the same precedence order.
6. **Close the loop.** End the brief by noticing what the research revealed about the repository, and recommend (never run unprompted) the matching skill:
   - Topic has no opinion file / no coverage → offer an opinion stub via `harvest-sources`-style fold-in.
   - Existing aside or opinion is stale (feature GA'd, guidance superseded) → recommend `refresh-dotnet-versions` or `harvest-sources`.
   - A strong non-roster source carried the research → recommend `vet-source`.
   - Repository already answers it fully → say so; that is the system working.

## Output and persistence

The brief is conversational by default — it lives in the session. If the user asks to keep it, save to `research/<topic-slug>.md` with the standard frontmatter (`targets`, `last-reviewed`, `last-used`, `sources`) so `audit-freshness` covers saved briefs like any other resource; note in the brief that a saved research document records the state of a moment, and its `last-reviewed` date is the honesty mechanism.

## Edge cases

- **Preview-only topics** (the "union types" case): research them fully — that is a legitimate ask — but the brief must lead with maturity status, and any "when it GAs" guidance is clearly hypothetical.
- **Topic outside .NET entirely**: say the roster has no authority there and stop, rather than improvising from general knowledge dressed up as vetted research.
- **Offline**: answer from `opinions/` alone and say the roster sweep was skipped — an opinions-only answer is still a sourced answer, but its ceiling is the repository's `last-reviewed` dates.
- **Conflicting House and community positions**: present both, in that order, and note the house rationale — the reader may be working outside this repository's conventions.
