---
targets: [net10.0, csharp-14]
last-reviewed: 2026-09-25
last-used: 2026-09-25
sources: [code-with-mukesh, milan-jovanovic, ms-learn, shay-rojansky]
---

# Data access

EF Core is the default ORM. Write set-based work as set-based SQL; keep the change tracker for the writes that need it.

<!-- Seeded 2026-08-14 by harvest-sources from newly admitted sources; see AWESOME-HUMANS.md decision log -->

## Opinions

- **Use `ExecuteUpdateAsync` / `ExecuteDeleteAsync` for updates and deletes that don't need the entities loaded.** Load-modify-`SaveChanges` pulls every row into the change tracker to emit statements the database could have generated itself; the set-based APIs issue one `UPDATE`/`DELETE`. Independent benchmarks put the gap in the hundreds of times on 10,000-row operations. ([Mukesh: Bulk operations in EF Core 10](https://codewithmukesh.com/blog/bulk-operations-efcore/), [Jovanović: What you need to know about EF Core bulk updates](https://milanjovanovic.tech/blog/what-you-need-to-know-about-ef-core-bulk-updates))

  ```csharp
  // Set-based: one statement, no entities materialized
  await db.Orders
      .Where(o => o.Status == OrderStatus.Pending && o.Placed < cutoff)
      .ExecuteUpdateAsync(
          s => s.SetProperty(o => o.Status, OrderStatus.Expired),
          cancellationToken);
  ```

- **Know what the set-based APIs skip, and keep audited writes on `SaveChanges`.** They bypass the change tracker entirely: interceptors don't fire, `SaveChanges`-based audit and outbox logic doesn't run, and global query filters are not applied to the predicate. A soft-delete filter you rely on everywhere else is silently missing. Spell the filter out in the `Where` clause, or keep that write on `SaveChanges`. ([Mukesh: Bulk operations in EF Core 10](https://codewithmukesh.com/blog/bulk-operations-efcore/), [Jovanović: EF Core bulk updates](https://milanjovanovic.tech/blog/what-you-need-to-know-about-ef-core-bulk-updates))
- **Model dates and times with the types in [datetime.md](datetime.md), and let EF Core map them natively:** `DateOnly`/`TimeOnly` to the SQL date and time column types on EF Core 8+, not the legacy `DateTime`-for-a-date shape. The provider differences and the UTC-vs-local storage decision live in [datetime.md](datetime.md#persistence-split-by-kind-of-data-not-by-layer).
- **On PostgreSQL, store instants as `DateTime` with `Kind.Utc` and let Npgsql map them to `timestamptz`.** Despite its name `timestamptz` stores a UTC instant and no zone at all, so the offset on a `DateTimeOffset` has nowhere to round-trip to: Npgsql rejects any `DateTimeOffset` whose offset is non-zero, maps `Kind.Utc` to `timestamptz`, and maps `Local`/`Unspecified` to plain `timestamp`. Read the resulting "UTC everywhere" rule as provider-shaped rather than domain-shaped: it describes what the column can faithfully round-trip, not what the domain is allowed to forget. It therefore sits underneath the split in [datetime.md](datetime.md#persistence-split-by-kind-of-data-not-by-layer) rather than against it: the instant column is UTC either way, and a future or recurring human-scheduled event still needs its local time and IANA zone id in their own columns beside it. ([Rojansky: PostgreSQL/.NET timestamp mapping](https://www.roji.org/postgresql-dotnet-timestamp-mapping))
- **Choose indexes from the queries the application runs, then prove each one with `EXPLAIN ANALYZE`.** The schema does not tell you what to index; the access paths do. For a B-tree, lead with the columns compared by equality and put the column you range over or sort by last. Measure before and after, because the plan is the only evidence that the index is used at all. Jovanović's issue-tracker demo over a million comments takes a count from a 17ms sequential scan to a 0.6ms index-only scan, and the issue-and-user query from 436ms to roughly half a millisecond. Those are his numbers on his data, so reproduce them on yours. ([Jovanović: SQL indexing explained: composite indexes and column order](https://www.milanjovanovic.tech/blog/how-to-design-the-right-sql-index))
- **A global query filter is a convenience, not tenant isolation: put row-level security under it.** EF Core adds the predicate to the SQL it generates and to nothing else, so `ExecuteSql`, an attached entity you save, and anything behind `IgnoreQueryFilters()` walk straight past it. A PostgreSQL policy attaches to every statement for every non-exempt role, so it holds when someone forgets the filter. Three details decide whether it actually holds. Connect as a role that owns nothing, because owners, superusers and `BYPASSRLS` roles skip policies, and run migrations as a separate owner. Add `FORCE ROW LEVEL SECURITY` so the owner is covered too. Write the predicate so an unset tenant matches no rows instead of every row. ([Jovanović: PostgreSQL row-level security with EF Core and Npgsql](https://www.milanjovanovic.tech/blog/postgres-row-level-security-with-ef-core))
- **Set the tenant when the connection opens, not when the request begins.** EF Core opens a connection per command and closes it afterwards, and Npgsql resets pooled session state, so a `SET` issued at the start of a request lands on a connection the next query never sees. Disabling that reset is worse, because the pooled connection then arrives carrying the previous request's tenant. A `DbConnectionInterceptor` overriding `ConnectionOpened` runs on whichever physical connection the pool hands out, which is the only place the setting is reliably in scope. ([Jovanović: PostgreSQL row-level security with EF Core and Npgsql](https://www.milanjovanovic.tech/blog/postgres-row-level-security-with-ef-core))
- **Write raw SQL through the interpolated methods, and never pass input to a `*Raw` one.** "The FromSql and FromSqlInterpolated methods are safe against SQL injection, and always integrate parameter data as a separate SQL parameter. However, the FromSqlRaw method can be vulnerable to SQL injection attacks, if improperly used", and the same split holds for `ExecuteSql` against `ExecuteSqlRaw` and `SqlQuery` against `SqlQueryRaw` ([Microsoft Learn: SQL Queries](https://learn.microsoft.com/ef/core/querying/sql-queries)). Reserve the `*Raw` overloads for SQL your own code composes, such as a table name from a fixed list, and never for a value that reached you from a request.

  ```csharp
  // Interpolated: the value becomes a parameter, not part of the statement.
  var orders = await db.Orders
      .FromSql($"SELECT * FROM Orders WHERE Status = {status}")
      .ToListAsync(cancellationToken);
  ```

- **For inserts, batched `SaveChanges` is the default; switch to a bulk-copy path only above roughly ten thousand rows.** `AddRange` + one `SaveChanges` keeps interceptors and audit trails working and is fast enough for ordinary write paths. Adding entities one at a time in a loop is the anti-pattern, an order of magnitude slower than the batched call for no benefit. ([Mukesh: Fastest way to bulk insert thousands of rows in EF Core](https://codewithmukesh.com/blog/ef-core-bulk-insert/))

## Source redundancy

`code-with-mukesh` (marked **Corroborate.**) and `milan-jovanovic` were both admitted on 2026-08-14 under the lowered longevity bars, and the benchmark numbers behind the first opinion are theirs, not reproduced here. The shape of the guidance (set-based writes for set-based work; change-tracker semantics are the trade) is corroborated across both; the specific ratios are not this repository's claim. Re-verify against Microsoft Learn's EF Core documentation before quoting figures.

`shay-rojansky` carries a recorded independence limit: he maintains Npgsql and is on Microsoft's EF Core team, so the mapping rules above are cited as mechanism, which is what his notes permit. The judgment of when to use those types is `jon-skeet`'s in [datetime.md](datetime.md), not his.

<!-- TODO: extend with query-side opinions (AsNoTracking defaults, split queries, projection over Include), migrations workflow, and connection resiliency. None of it is sourced yet. -->
