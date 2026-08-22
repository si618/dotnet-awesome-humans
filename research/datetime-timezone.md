---
targets: [net10.0, csharp-14]
last-reviewed: 2026-08-21
sources: [jon-skeet]
---

# Date, time, and time zones — blocked remainder

> **Partial promotion, 2026-08-21.** Everything roster-sourced in this brief promoted into [`opinions/datetime.md`](../opinions/datetime.md), with cross-links woven into `testing.md`, `data-access.md`, `ui-frameworks.md`, and `globalisation.md`, and `MA0167` enabled in `templates/.editorconfig`. What remains below is the one paragraph blocked on source vetting; it owes nothing else.

## Blocked: PostgreSQL timestamp mapping — waiting on `vet-source`

**Shay Rojansky — [roji.org](https://www.roji.org/)** (unvetted, not on the roster). Lead maintainer of Npgsql and an EF Core team member; the deepest independent writing on .NET ↔ PostgreSQL type mapping, which no current roster source covers. Likely Tier 2 under the independence cap given the EF Core employment, and cadence needs checking — the key post is from 2021.

The blocked claim, from [his timestamp-mapping post](https://www.roji.org/postgresql-dotnet-timestamp-mapping): PostgreSQL's `timestamptz` despite its name stores a UTC instant and no zone, so Npgsql maps UTC `DateTime` → `timestamptz`, Local/Unspecified `DateTime` → `timestamp`, and rejects `DateTimeOffset` with a non-zero offset because the offset could not round-trip. His "UTC everywhere" default is provider-shaped rather than domain-shaped, and reconciles with the promoted position (`jon-skeet`, Tier 1): keep the instant column UTC, and put the local time and IANA zone id in their own columns whenever the row describes a future or recurring human-scheduled event.

**Next step:** run `vet-source` on Rojansky; if admitted, promote the paragraph above into [`opinions/data-access.md`](../opinions/data-access.md) (its TODO points here) and delete this file. If declined, discard it — the promoted opinion stands without it.
