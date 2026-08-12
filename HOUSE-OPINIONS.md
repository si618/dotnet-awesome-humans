# House Opinions

The repository owner's own opinions — preferences earned from experience rather than traced to a vetted source. They are first-class but **always visibly marked**, so a reader (human or agent) can tell "the community's best practice" from "how we do it here".

## How this works

1. **The owner adds an entry** to the [Inbox](#inbox) below — a sentence or two is enough; the rationale matters more than polish.
2. **The `weave-house-opinion` skill weaves it in**: the opinion lands in the right `opinions/` file (and `templates/` where applicable) marked as **House** with `house` in the file's `sources:` frontmatter, and the inbox entry moves to [Woven](#woven) with a link.
3. **House beats roster on conflict.** If a house opinion contradicts a sourced one, the house opinion wins and the sourced position is kept as a one-line "the community default is X; we do Y because Z" note.
4. External contributors propose opinions via pull request instead — see the [PR template](.github/PULL_REQUEST_TEMPLATE.md).

The `house` source id is reserved and documented in [AWESOME-HUMANS.md](AWESOME-HUMANS.md); its authority is repository ownership, so it is exempt from the admission criteria — which is exactly why it must stay visibly marked wherever it is used.

## Entry format

```markdown
### <short title>

- **Opinion:** <what to do, one sentence>
- **Why:** <the experience or reasoning behind it>
- **Scope:** <which topic/file it belongs to, if known>
```

## Inbox

_Nothing waiting._

## Woven

| Date       | Opinion                                                                                    | Woven into                                                                    |
| ---------- | ------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------- |
| 2026-08-12 | Test naming `UnitOfWork_Scenario_ExpectedBehaviour` + explicit Arrange/Act/Assert comments | [opinions/testing.md](opinions/testing.md) (retrofitted — predates this file) |
