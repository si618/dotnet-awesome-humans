---
name: resolve-research
description: Resolve a saved research topic in research/ — weave its recommendation into the opinion and template files, or discard it — deleting the file once fully resolved. Use when a research topic is ready to become the opinion, when audit-freshness reports one open past tolerance, or when deciding that it has served its purpose and owes nothing further.
license: See repository LICENSE
compatibility: Requires git; internet access only needed to re-verify a stale research topic's claims
metadata:
  repo: dotnet-awesome-humans
  change-flow: branch-pr
---

# Resolve a research topic

`research/` is a staging area, and this skill is what drains it. A research topic written by `research-topic` is evidence gathered at a moment; promotion is the decision to make it the repository's position, and discard is the equally valid decision that it was only ever an answer to a question. Both end with the file gone — the one exception is a partial promotion, which keeps the blocked remainder on disk (see Edge cases).

## Orchestration

This is **editor-only work — do not fan it out**. The wide, shallow sweeping already happened during the research; what remains is exactly the opinion-shaping judgment that [AGENTS.md](../../AGENTS.md#orchestration-and-model-economy) reserves for the strongest available model. If a claim needs re-checking, that is a `research-topic` re-run, not a worker sweep.

## Steps

1. **Read it whole**, at `research/<topic-slug>.md` on the default branch — the research PR has already merged, so the topic is on record and was reviewable; if it is still open, merge it first rather than resolving on top of it. Resolution is its own branch and PR, never a continuation of the research branch. Its closing "what this reveals about the repository" section is the promotion plan — it already names the target files and usually the specific sections. Treat that as the proposal to verify, not as instructions to execute unread.
2. **Decide promote or discard.** Ask only if the research topic does not already settle it. **Discard is a first-class outcome**, not a failure: one whose finding was "the repository already answers this" has done its job by confirming the system works.
3. **Gate every load-bearing claim on the roster.** Walk the citations:
   - **Tier 1/2 roster ids:** promotable, and their ids go into the target's `sources:` frontmatter.
   - **Watch-list, unvetted, or non-roster:** not promotable as written. Either drop the claim from the promoted text, corroborate it from a roster source, or hold it back pending `vet-source` admission. **Never launder an unvetted claim** by rewriting it until its origin stops showing — that defeats the whole trust boundary.
   - **First-party issues, PRs, and release notes:** supporting colour. They can justify the decision in the PR description without becoming a citation in `sources:`; they are not blog-shaped sources and do not belong on the roster.
4. **Choose the shape before editing**: fold into existing opinion file(s), or create a new one. The repository's convention is one topic per file, under ~300 lines — split rather than sprawl. A new file under `opinions/` is not finished until `README.md` lists it in both places (a linked Scope bullet and a Repository layout entry), in the same commit; CI fails the PR otherwise.
5. **Weave it in.** Opinion first, rationale after, sources last — the surveying and hedging prose does not survive the move. One recommendation, not a menu; alternatives get at most one line explaining why they lost. Never remove, dilute, or un-mark house content: where the research contradicts a house opinion, [HOUSE-OPINIONS.md: How this works](../../HOUSE-OPINIONS.md#how-this-works) governs, and the house position stands with the researched view kept as the cited one-line note.
6. **Follow through into `templates/`** wherever the opinion changes scaffolding — a build property, an `.editorconfig` rule, a package pin. An opinion naming a setting its nominated template omits is a finding `audit-freshness` will raise, so land both halves together. Add the source ids to the template's pipe-delimited comment header too.
7. **Fix what the research noticed in passing.** A sweep routinely spots collateral damage — a stale example, a path that now collides, a rule enforced with no opinion behind it. Fix it in the same PR; noticing it was the point.
8. **Update metadata** on every file touched — YAML frontmatter on an opinion, the first-line comment header on a template: add the promoted source ids to `sources:`, and bump `last-reviewed` — but only honestly (see Rules).
9. **Delete the research topic:** that file only; `research/.gitkeep` keeps the directory tracked once the last one goes. Then commit on a branch (`resolve/<topic-slug>`) and open a PR to the default branch, titled `docs: promote <topic>` for a promotion or `research: discard <topic>` for a discard.

## Rules

- **Only roster-sourced claims promote.** This is the one gate that makes `research/` permissive and `opinions/` trustworthy — research may cite anyone, the opinions may not.
- **Promotion ends in deletion.** Do not leave the file behind; a promoted research topic left on disk is a second, unratified copy of the opinion, which is precisely what the staging area exists to prevent. Nothing on the file marks it as promoted — deletion is the only signal, which is why the deletion is part of the same pull request as the weave, never a follow-up. `audit-freshness` reports a lingering one as a finding. Git history is the record, and the PR that weaves it is the citation.
- **Promotion is `docs:`, discard is `research:`.** Promotion keeps the type of what it changes — it edits opinions, so it files with opinion edits. A discard touches nothing but the staging area, so it stays in the research lifecycle's own type. Neither takes the `research: <topic>` form that `research-topic` uses when saving.
- **`last-reviewed` means verified against sources, not copied across.** If the research is inside the freshness tolerance, its verification carries and today's date is honest. If it is not, re-verify first (see Edge cases) — bumping the date on the strength of stale research is exactly the lie the metadata exists to prevent.
- **A research topic is evidence, not prose to transplant.** It is written to survey and weigh; an opinion is written to be applied mechanically. Rewrite, don't paste.

## Edge cases

- **It is stale** (past the tolerance `audit-freshness` applies, or a major .NET release has shipped since): re-verify the load-bearing claims against their sources before promoting, and check whether `refresh-dotnet-versions` should run first. Research that has aged past a release boundary is an input again, not a finished decision.
- **Only part of it can promote.** Promote the roster-sourced part and rewrite the rest down to the blocked remainder, leaving the file in place with a note saying what promoted and what it is waiting on. Do not hold a sound recommendation hostage to one unvetted paragraph, and do not delete the blocked part silently.
- **Everything in it is unvetted**: it cannot promote. Run `vet-source` on the strongest candidate and revisit, or discard.
- **It covers a preview feature**: it cannot become an opinion — the freshness policy allows only a clearly marked "coming next" aside. Promote that aside if it is worth having, otherwise leave it open until GA.
- **Two overlap** (the same opinion file, or the same underlying idea): promote them in one PR, or sequence them deliberately — whichever avoids one rewriting the other's frontmatter twice.
- **The recommendation has been overtaken** by a `harvest-sources` fold-in that landed first: discard it, and say so in the PR. Duplicated guidance is worse than none.
