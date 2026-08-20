---
targets: [net10.0, csharp-14]
last-reviewed: 2026-08-14
last-used: 2026-08-19
sources: [code-with-mukesh, milan-jovanovic]
---

# Data access

EF Core is the default ORM. Write set-based work as set-based SQL; keep the change tracker for the writes that need it.

<!-- Seeded 2026-08-14 by harvest-sources from newly admitted sources; see AWESOME-HUMANS.md decision log -->

## Opinions

- **Use `ExecuteUpdateAsync` / `ExecuteDeleteAsync` for updates and deletes that don't need the entities loaded.** Load-modify-`SaveChanges` pulls every row into the change tracker to emit statements the database could have generated itself; the set-based APIs issue one `UPDATE`/`DELETE`. Independent benchmarks put the gap in the hundreds of times on 10,000-row operations. ([Mukesh — Bulk operations in EF Core 10](https://codewithmukesh.com/blog/bulk-operations-efcore/), [Jovanović — What you need to know about EF Core bulk updates](https://milanjovanovic.tech/blog/what-you-need-to-know-about-ef-core-bulk-updates))

  ```csharp
  // Set-based: one statement, no entities materialised
  await db.Orders
      .Where(o => o.Status == OrderStatus.Pending && o.Placed < cutoff)
      .ExecuteUpdateAsync(
          s => s.SetProperty(o => o.Status, OrderStatus.Expired),
          cancellationToken);
  ```

- **Know what the set-based APIs skip, and keep audited writes on `SaveChanges`.** They bypass the change tracker entirely: interceptors don't fire, `SaveChanges`-based audit and outbox logic doesn't run, and global query filters are not applied to the predicate — so a soft-delete filter you rely on everywhere else silently isn't there. Spell the filter out in the `Where` clause, or keep that write on `SaveChanges`. ([Mukesh — Bulk operations in EF Core 10](https://codewithmukesh.com/blog/bulk-operations-efcore/), [Jovanović — EF Core bulk updates](https://milanjovanovic.tech/blog/what-you-need-to-know-about-ef-core-bulk-updates))
- **For inserts, batched `SaveChanges` is the default; reach for a bulk-copy path only above roughly ten thousand rows.** `AddRange` + one `SaveChanges` keeps interceptors and audit trails working and is fast enough for ordinary write paths. Adding entities one at a time in a loop is the anti-pattern — an order of magnitude slower than the batched call for no benefit. ([Mukesh — Fastest way to bulk insert thousands of rows in EF Core](https://codewithmukesh.com/blog/ef-core-bulk-insert/))

## Source redundancy

Both sources were admitted on 2026-08-14 under the lowered longevity bars, and the benchmark numbers behind the first opinion are theirs, not reproduced here. The shape of the guidance (set-based writes for set-based work; change-tracker semantics are the trade) is corroborated across both; the specific ratios are not this repository's claim. Re-verify against Microsoft Learn's EF Core documentation before quoting figures.

<!-- TODO: extend with query-side opinions (AsNoTracking defaults, split queries, projection over Include), migrations workflow, and connection resiliency — none of it sourced yet -->
