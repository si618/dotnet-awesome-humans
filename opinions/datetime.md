---
targets: [net10.0, csharp-14]
last-reviewed: 2026-08-21
last-used: 2026-08-21
sources: [ms-learn, dotnet-blog, jon-skeet, meziantou, andrew-lock]
---

# Dates, times, and time zones

Four rules carry most of the weight: `DateTimeOffset` is the default type, not `DateTime`; UTC is right for timestamps and wrong for future local events; services take an injected `TimeProvider`, never call `DateTime.UtcNow`; and a time zone is an IANA id (`Europe/Amsterdam`), never an abbreviation or an offset.

## Choosing the type

**Default to `DateTimeOffset`; pick a narrower type only when the data genuinely has no time, no date, or no clock.** Microsoft says so outright: "consider `DateTimeOffset` as the default date and time type for application development". ([Compare types related to date and time](https://learn.microsoft.com/dotnet/standard/datetime/choosing-between-datetime))

| Need                                      | Type             | Why                                                                         |
| ----------------------------------------- | ---------------- | --------------------------------------------------------------------------- |
| An unambiguous point in time              | `DateTimeOffset` | Carries the UTC offset, so it identifies one instant on any machine         |
| A whole date, no time (birthday, invoice) | `DateOnly`       | Cannot be shifted a day by a time zone; serializes less; matches SQL `date` |
| A time of day (opening hours, alarm)      | `TimeOnly`       | Wraps within 24h instead of over/underflowing like `TimeSpan` or `DateTime` |
| A duration or elapsed time                | `TimeSpan`       | It is an interval, not a clock reading                                      |
| Zone rules and conversions                | `TimeZoneInfo`   | The only type that knows about DST transitions                              |
| `DateTime`                                | rarely           | Only for abstract dates, or UTC-only code where `Kind` is set to `Utc`      |

Two caveats the documentation states explicitly:

- **`DateTimeOffset` does not know its time zone.** It records the offset that applied at one moment, and the same offset belongs to many zones — it "can't reflect a time zone's transition to and from daylight saving time". Adding 24 hours is not the same as adding one day; for DST-sensitive arithmetic, convert to the zone with `TimeZoneInfo`, do the arithmetic in local terms, convert back.
- **`DateTime` with `Kind = Unspecified` is ambiguous even on the machine that produced it.** If a `DateTime` must cross a boundary, it is UTC with `Kind = Utc` or it is a bug.

And the assumptions to stop making: offsets are not whole hours (Newfoundland is UTC−3:30), DST shifts are not always an hour (Lord Howe is 30 minutes), zones within one country differ on whether they observe DST at all, and abbreviations like "BST" are ambiguous across three real zones. Anything narrower than an IANA id loses information. ([Meziantou — 39 misconceptions about date and time](https://www.meziantou.net/misconceptions-about-date-and-time.htm))

## Persistence: split by kind of data, not by layer

**Machine-generated timestamps are instants — store them UTC. Human-supplied future or recurring events are not instants until zone rules that may still change are applied — store the local time and the IANA zone id, and treat UTC as derived.** ([Skeet — Storing UTC is not a silver bullet](https://codeblog.jonskeet.uk/2019/03/27/storing-utc-is-not-a-silver-bullet/))

The failure the blanket "convert to UTC on the way in" advice invites: a conference registered for 9am Amsterdam on 2022-07-10 converts to `07:00Z` under the tzdb rules of the day. If the Netherlands later drops summer time, the correct instant becomes `08:00Z` — and a row storing only UTC is silently an hour wrong, with no way to recover the organiser's intent. The schema that survives rule changes stores what was supplied and caches the conversion:

```text
LocalStart:    2022-07-10T09:00:00     ← what the user said; never mutated
TimeZoneId:    Europe/Amsterdam        ← IANA id, not an offset
UtcStart:      2022-07-10T07:00:00Z    ← derived; recomputed when tzdb updates
TimeZoneRules: 2019a                   ← optional, for resumable re-derivation
```

The same holds for recurring events — "10 AM in New York every week" is a rule, not a series of instants. ([Meziantou — 39 misconceptions](https://www.meziantou.net/misconceptions-about-date-and-time.htm)) Two operational corollaries: IANA publishes multiple tzdb releases a year, sometimes days before they take effect, so updating the base image is also the trigger to re-derive the UTC column; and persist IANA ids, not Windows ids — .NET converts between the families, but Windows ids tie data to a platform.

Provider mechanics worth knowing ([EF Core](https://learn.microsoft.com/ef/core/), Microsoft Learn):

- EF Core 8+ maps `DateOnly` ↔ SQL Server `date` and `TimeOnly` ↔ `time`, and scaffolding generates those types instead of `DateTime`/`TimeSpan` ([EF8 announcement](https://devblogs.microsoft.com/dotnet/announcing-ef8-preview-1/)). The old `DateTime`-for-a-date mapping is a legacy shape.
- SQL Server has a real `datetimeoffset` column type; PostgreSQL does not. Do not assume a `DateTimeOffset` property is portable across providers.
- `Microsoft.Data.Sqlite` 10.0 converts `DateTimeOffset` to UTC before writing to REAL columns — a behaviour change to check if you target SQLite ([EF Core 10 breaking changes](https://learn.microsoft.com/ef/core/what-is-new/ef-core-10.0/breaking-changes)).
- On the wire, `System.Text.Json` round-trips `DateOnly`/`TimeOnly` natively since .NET 7, and `DateTimeOffset` serializes as ISO 8601 with the offset ([STJ date/time support](https://learn.microsoft.com/dotnet/standard/datetime/system-text-json-support)) — so minimal-API bodies need no custom converters, and a wire format that omits the offset is pushing the ambiguity onto the client.

### When the BCL types aren't enough

**Reach for [NodaTime](https://nodatime.org/) when the domain models future or recurring local-time events across zones — the schema above — and stay on the BCL types otherwise.** NodaTime turns this file's conventions into compile-time properties: `Instant` for machine timestamps, `LocalDateTime` + `DateTimeZone` for the human-supplied columns, `ZonedDateTime` for the derived conversion — the argument that a local date and time is not yet an instant is the library's founding design case. The cost is an adapter at every boundary the BCL types cross natively (`NodaTime.Serialization.SystemTextJson`, per-provider EF Core plugins such as `Npgsql.NodaTime`, model binding), which is why it is the escape hatch and not the default — `DateTimeOffset`, `DateOnly`/`TimeOnly`, and `TimeProvider` cover the ordinary service in-box. Conflict of interest: the source cited here created NodaTime. ([Skeet — More fun with DateTime](https://codeblog.jonskeet.uk/2012/05/02/more-fun-with-datetime/))

## Services: inject `TimeProvider`, stop writing your own clock

**Take `TimeProvider` as a dependency and register `TimeProvider.System` once; never call `DateTime.UtcNow` or `DateTimeOffset.Now` in a service, and never hand-roll an `IClock` abstraction.** It is in-box since .NET 8 (`Microsoft.Bcl.TimeProvider` back to netstandard2.0), with `GetUtcNow()`/`GetLocalNow()` returning `DateTimeOffset`, `GetTimestamp()`/`GetElapsedTime()` for measurement, `CreateTimer(...)`, and `LocalTimeZone` as the seam that lets one process serve users in different zones. ([What is TimeProvider](https://learn.microsoft.com/dotnet/standard/datetime/timeprovider-overview))

```csharp
// Register once; TimeProvider.System is the production implementation.
builder.Services.AddSingleton(TimeProvider.System);

public sealed class OrderService(TimeProvider time)
{
    public Order Place(Basket basket) => new(basket, PlacedAt: time.GetUtcNow());
}
```

Forward it rather than dropping it — `Task.Delay`, `Task.WaitAsync`, and `CancellationTokenSource` all have `TimeProvider` overloads, and a `PeriodicTimer` or `BackgroundService` built on the raw ones is untestable for no gain.

This is enforced, not aspirational — but the enforcement is a pair, not one file. [templates/Directory.Build.props](../templates/Directory.Build.props) ships Meziantou.Analyzer with `TreatWarningsAsErrors`, and the date/time rules below all ship at **info** severity, so on their own they never trip it; it is [templates/.editorconfig](../templates/.editorconfig)'s `dotnet_analyzer_diagnostic.severity = warning` that raises them to warnings, which `TreatWarningsAsErrors` then turns into build failures. Adopt both files, or a hand-rolled `IClock` builds clean ([analyzer docs](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/README.md)):

| Rule     | Title                                                          | Default                                                                         |
| -------- | -------------------------------------------------------------- | ------------------------------------------------------------------------------- |
| `MA0188` | Use `System.TimeProvider` instead of a custom time abstraction | enabled, info severity                                                          |
| `MA0166` | Forward the `TimeProvider` to methods that take one            | enabled, info severity                                                          |
| `MA0167` | Use an overload with a `TimeProvider` argument                 | disabled — [templates/.editorconfig](../templates/.editorconfig) enables it too |

## Testing time

**Use `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing` — set a start instant, advance manually with `Advance(TimeSpan)`, and timers created from it fire as time moves** — which is what makes retry, backoff, and scheduling logic testable without `Thread.Sleep`. One caveat: a single large `Advance` fires every elapsed callback at the boundary rather than spread out, so tests asserting interleaving should step time in small increments. ([Lock — Avoiding flaky tests with TimeProvider and ITimer](https://andrewlock.net/exploring-the-dotnet-8-preview-avoiding-flaky-tests-with-timeprovider-and-itimer/))

For date-sensitive logic, exercise the awkward instants deliberately: a DST spring-forward gap (a local time that does not exist), a fall-back overlap (a local time that happens twice), a leap day, and a year boundary that splits calendar year from ISO week year — 2022-01-02 is in week 52 of 2021. ([Meziantou — 39 misconceptions](https://www.meziantou.net/misconceptions-about-date-and-time.htm)) See [testing.md](testing.md) for the framework stack this slots into.

## UI and hosts

- **The user's zone lives in the browser, not the server.** In interactive-server Blazor, register a scoped per-circuit `TimeProvider` filled from JS interop — the pattern is in [ui-frameworks.md](ui-frameworks.md#blazor).
- **Run hosts in UTC and convert at the edges.** The server's own local zone should never be load-bearing: express a scheduled job's schedule in an explicit zone, because a cron-style job pinned to server-local time either runs twice or not at all on DST transition days.
- **Container images must actually carry tzdata and ICU** for any of the zone conversion above to work — image tags, invariant mode, and the failure modes are in [globalisation.md](globalisation.md#containers-chiseled-and-alpine-images-drop-this-data), along with the display-side rule: format for humans with their culture, for machines with the invariant one.

## Smaller traps

- **`TimeSpan.From*` gained integer overloads in .NET 9** because the `double` ones are lossy — `TimeSpan.FromSeconds(101.832)` is not 101.832 seconds. In F# this broke overload resolution: `TimeSpan.FromMinutes(20)` now needs an explicit type annotation. ([Breaking change note](https://learn.microsoft.com/dotnet/core/compatibility/core-libraries/9.0/timespan-from-overloads))
- **`TimeZoneInfo.FindSystemTimeZoneById` accepts the platform's native ids**, and the lookup itself needs only tzdata — it works even in invariant-globalization mode. Converting between the IANA and Windows id families (`TryConvertIanaIdToWindowsId` / `TryConvertWindowsIdToIanaId`) is the ICU-dependent part, unavailable in invariant and NLS modes ([globalisation.md](globalisation.md)).
