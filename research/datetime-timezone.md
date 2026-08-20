---
targets: [net10.0, csharp-14]
last-reviewed: 2026-08-20
last-used: 2026-08-20
sources: [jon-skeet, ms-learn, meziantou, andrew-lock, steve-gordon]
---

# Date, time, and time zones across UI, persistence, services, and hosts

Four rules carry most of the weight:

1. **`DateTimeOffset` is the default type**, not `DateTime` — Microsoft says so outright: "consider `DateTimeOffset` as the default date and time type for application development" (`ms-learn`, Tier 1). Reach for `DateOnly`/`TimeOnly` when there genuinely is no time or no date, and `TimeSpan` only for durations.
2. **UTC is right for timestamps and wrong for future local events.** A machine-generated "this happened at" is naturally an instant; store UTC. A human-supplied "the conference starts 9am in Amsterdam on 2022-07-10" is not an instant until you apply time zone rules that may still change — store the local time and the IANA zone id, and treat UTC as derived (`jon-skeet`, Tier 1).
3. **Inject `TimeProvider`, never call `DateTime.UtcNow` in a service.** It is in-box since .NET 8, covers `GetUtcNow`, `GetLocalNow`, timers, and elapsed-time, and has a first-class test double (`ms-learn`, Tier 1; `andrew-lock`, Tier 1).
4. **A time zone is not an offset.** Persist IANA ids (`Europe/Amsterdam`), never abbreviations or offsets, and make sure the host actually has tzdata (`meziantou`, Tier 1; `steve-gordon`, Tier 1).

Everything below is GA on .NET 10 / C# 14. No preview features are load-bearing in this brief.

## Choosing the type

`ms-learn` (Tier 1, [Compare types related to date and time](https://learn.microsoft.com/dotnet/standard/datetime/choosing-between-datetime), page updated 2026-03-30):

| Need                                      | Type             | Why                                                                         |
| ----------------------------------------- | ---------------- | --------------------------------------------------------------------------- |
| An unambiguous point in time              | `DateTimeOffset` | Carries the UTC offset, so it identifies one instant on any machine         |
| A whole date, no time (birthday, invoice) | `DateOnly`       | Cannot be shifted a day by a time zone; serializes less; matches SQL `date` |
| A time of day (opening hours, alarm)      | `TimeOnly`       | Wraps within 24h instead of over/underflowing like `TimeSpan` or `DateTime` |
| A duration or elapsed time                | `TimeSpan`       | It is an interval, not a clock reading                                      |
| Zone rules and conversions                | `TimeZoneInfo`   | The only type that knows about DST transitions                              |
| `DateTime`                                | rarely           | Only for abstract dates, or UTC-only code where `Kind` is set to `Utc`      |

Two caveats `ms-learn` states explicitly:

- **`DateTimeOffset` does not know its time zone.** It records the offset that applied at one moment, and the same offset belongs to many zones. It "can't reflect a time zone's transition to and from daylight saving time", which makes arithmetic on it wrong whenever the answer must respect DST. Adding 24 hours is not the same as adding one day.
- **`DateTime` with `Kind = Unspecified` is ambiguous even on the machine that produced it.** If you must use `DateTime` across a boundary, it is UTC with `Kind = Utc` or it is a bug.

`meziantou` (Tier 1, [39 misconceptions about date and time](https://www.meziantou.net/misconceptions-about-date-and-time.htm)) supplies the assumptions to stop making: offsets are not whole hours (Newfoundland is UTC−2:30, Australian Central Western is UTC+8:45); DST shifts are not always an hour (Lord Howe is 30 minutes, Troll is two); zones within a country differ on whether they observe DST at all; and abbreviations like "BST" are ambiguous across three real zones. Anything narrower than an IANA id loses information.

## Persistence — where the sources genuinely disagree

This is the one place the community does not speak with one voice, and averaging the positions produces the wrong schema.

**`jon-skeet` (Tier 1, [Storing UTC is not a silver bullet](https://codeblog.jonskeet.uk/2019/03/27/storing-utc-is-not-a-silver-bullet/), 2019-03-27)** argues the "convert to UTC on the way in" advice is overly broad. His worked example: a conference registered for 9am Amsterdam on 2022-07-10 under tzdb 2019a converts to `07:00Z`. If the Netherlands later drops summer time, the correct instant becomes `08:00Z` — and a row storing only UTC is now silently an hour wrong, with no way to recover the organiser's intent. His preferred schema stores what was supplied and treats UTC as a cache:

```text
LocalStart:    2022-07-10T09:00:00     ← what the user said; never mutated
TimeZoneId:    Europe/Amsterdam        ← IANA id, not an offset
UtcStart:      2022-07-10T07:00:00Z    ← derived; recomputed when tzdb updates
TimeZoneRules: 2019a                   ← optional, for resumable re-derivation
```

> "Defaulting to 'convert to UTC' is a default to discarding information which in _some_ cases is valid, but not all." — `jon-skeet`

He is equally clear about the other half: "Machine-generated timestamps are _naturally_ instants in time… Storing those in UTC is entirely reasonable." So the split is **by kind of data, not by layer**: derived-from-a-clock → UTC; supplied-by-a-human-about-the-future → local + zone id. `meziantou` (Tier 1) reaches the same conclusion for recurring events — "10 AM in New York" every week is a rule, not a series of instants.

**Shay Rojansky ([roji.org](https://www.roji.org/postgresql-dotnet-timestamp-mapping), Npgsql/EF Core maintainer — unvetted, not on the roster)** argues the opposite default: "UTC everywhere", with explicit conversion in application code. His reasoning is provider-shaped rather than domain-shaped — PostgreSQL's `timestamptz` despite its name stores a UTC instant and no zone, so Npgsql maps UTC `DateTime` → `timestamptz`, Local/Unspecified `DateTime` → `timestamp`, and **rejects `DateTimeOffset` with a non-zero offset** because the offset could not round-trip.

**Weighing them:** these are answers to different questions and Skeet's is the one that scopes the other. Rojansky is describing what a column can faithfully round-trip; Skeet is describing what the domain must not lose. Reconciled: keep the instant column UTC (which is what every mainstream database actually stores anyway), and put the local time and IANA zone id in their _own_ columns beside it whenever the row describes a future or recurring human-scheduled event. That satisfies both, and it is Skeet's option 3 verbatim.

Provider mechanics worth knowing (`ms-learn`, Tier 1):

- EF Core 8+ maps `DateOnly` ↔ SQL Server `date` and `TimeOnly` ↔ `time`, and scaffolding now generates those types instead of `DateTime`/`TimeSpan` ([EF8 announcement](https://devblogs.microsoft.com/dotnet/announcing-ef8-preview-1/)). Use them; the old `DateTime`-for-a-date mapping is a legacy shape.
- SQL Server has a real `datetimeoffset`, PostgreSQL does not. Do not assume a `DateTimeOffset` property is portable across providers.
- `Microsoft.Data.Sqlite` 10.0 now converts `DateTimeOffset` to UTC before writing to REAL columns — a behaviour change to check if you target SQLite ([EF Core 10 breaking changes](https://learn.microsoft.com/ef/core/what-is-new/ef-core-10.0/breaking-changes)).
- On the wire, `System.Text.Json` round-trips `DateOnly`/`TimeOnly` natively since .NET 7 ([STJ date/time support](https://learn.microsoft.com/dotnet/standard/datetime/system-text-json-support)), so minimal-API bodies need no custom converters.

## Services — `TimeProvider` is the abstraction; stop writing your own

`ms-learn` (Tier 1, [What is TimeProvider](https://learn.microsoft.com/dotnet/standard/datetime/timeprovider-overview), page updated 2026-01-20). In-box from .NET 8; `Microsoft.Bcl.TimeProvider` back to netstandard2.0 and .NET Framework 4.6.2. It gives you:

- `GetUtcNow()` / `GetLocalNow()` — both return `DateTimeOffset`
- `GetTimestamp()` / `GetElapsedTime()` — high-frequency measurement, `Stopwatch`-backed
- `CreateTimer(...)` returning `ITimer`
- `LocalTimeZone` — the seam that lets one process serve users in different zones

```csharp
// Register once; TimeProvider.System is the production implementation.
builder.Services.AddSingleton(TimeProvider.System);

public sealed class OrderService(TimeProvider time)
{
    public Order Place(Basket basket) => new(basket, PlacedAt: time.GetUtcNow());
}
```

Forward it rather than dropping it — `Task.Delay`, `Task.WaitAsync`, and `CancellationTokenSource` all have `TimeProvider` overloads, and a `PeriodicTimer` or `BackgroundService` built on the raw ones is untestable for no gain.

**This repository already enforces some of this and does not know it.** `templates/Directory.Build.props` ships `Meziantou.Analyzer` with `TreatWarningsAsErrors`. Three of its rules are on by default and are date/time rules (`meziantou`, Tier 1, [analyzer docs](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/README.md)):

| Rule       | Title                                                          | Default  |
| ---------- | -------------------------------------------------------------- | -------- |
| `MA0188`   | Use `System.TimeProvider` instead of a custom time abstraction | enabled  |
| `MA0166`   | Forward the `TimeProvider` to methods that take one            | enabled  |
| `MA0167`   | Use an overload with a `TimeProvider` argument                 | disabled |
| `MA0113/4` | Use `DateTime.UnixEpoch` / `DateTimeOffset.UnixEpoch`          | enabled  |
| `MA0178`   | Use `TimeSpan.Zero` instead of `TimeSpan.FromXXX(0)`           | enabled  |

So a project copying our template today already fails the build for hand-rolled `IClock` interfaces — a rule we ship without documenting. `MA0167` is worth turning on deliberately.

### Testing time

`andrew-lock` (Tier 1, [Avoiding flaky tests with TimeProvider and ITimer](https://andrewlock.net/exploring-the-dotnet-8-preview-avoiding-flaky-tests-with-timeprovider-and-itimer/)): `FakeTimeProvider` from **`Microsoft.Extensions.TimeProvider.Testing`** sets a start instant, advances manually via `Advance(TimeSpan)`, and fires timers created from it as time moves — which is what makes retry/backoff/scheduling logic testable without `Thread.Sleep`. His caveat: a single large `Advance` fires every elapsed callback at the boundary rather than spread out, so tests asserting interleaving should step time in small increments.

The obvious extension of this repository's `testing.md` position: for date-sensitive tests, exercise the awkward instants deliberately — a DST spring-forward gap (a local time that does not exist), a fall-back overlap (a local time that happens twice), a leap day, and a year boundary that splits calendar year from ISO week year (`meziantou`, Tier 1: 2022-01-02 is in week 52 of 2021).

## Hosts — the layer that silently breaks

Everything above assumes the process can resolve a zone id. In a container that is often false.

- **Alpine ships no tzdata.** `steve-gordon` (Tier 1, [TimeZoneNotFoundException in Alpine-based Docker images](https://www.stevejgordon.co.uk/timezonenotfoundexception-in-alpine-based-docker-images)) — `TimeZoneInfo.FindSystemTimeZoneById` throws `TimeZoneNotFoundException` in Alpine containers that work fine on the dev machine. Fix is one line: `RUN apk add --no-cache tzdata`. Worse than the exception is the silent case: setting `ENV TZ=America/New_York` without tzdata makes musl fall back to UTC with no error at all.
- **Globalization-invariant mode disables zone lookup too.** `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true` (and the `InvariantGlobalization` MSBuild property, which the ASP.NET Core template sets in some configurations) leaves you with UTC and the invariant culture. If the app converts zones or formats dates for humans, it must be off, with ICU present — on Alpine that means `apk add --no-cache icu-data-full icu-libs` ([dotnet-docker globalization sample](https://github.com/dotnet/dotnet-docker/blob/main/samples/enable-globalization.md) — official repo, not a roster source).
- **Run hosts in UTC and convert at the edges.** The corollary of `TimeProvider.LocalTimeZone` being a seam is that the server's own local zone should never be load-bearing. If a scheduled job matters, express its schedule in an explicit zone; a cron-style job pinned to server-local time either runs twice or not at all on DST transition days.
- **tzdb changes are operational events, not one-offs.** IANA publishes multiple releases a year, sometimes days before they take effect (`jon-skeet`, Tier 1). If you adopted the derived-UTC schema above, updating the base image is also a data-migration trigger: re-derive the UTC column.

## UI — the zone lives in the browser, not the server

- **Blazor Server / interactive server:** the process's local zone is the server's, which is meaningless to the user. `meziantou` (Tier 1, [Convert DateTime to the user's time zone with Blazor in .NET 8](https://www.meziantou.net/convert-datetime-to-user-s-time-zone-with-server-side-blazor-time-provider.htm)) registers a scoped `TimeProvider` per circuit that overrides `LocalTimeZone`, filled from JS interop:

  ```javascript
  export function getBrowserTimeZone() {
    return Intl.DateTimeFormat().resolvedOptions().timeZone; // e.g. "Europe/Amsterdam"
  }
  ```

  The zone id is validated with `TimeZoneInfo.TryFindSystemTimeZoneById` and a `LocalTimeZoneChanged` event re-renders subscribers. Two caveats he names: the interop only runs in `OnAfterRenderAsync` under `InteractiveServer`, so the **first render shows the wrong zone**, and `JSDisconnectedException` must be handled. Design the component to tolerate one corrected re-render rather than pretending the zone is known at first paint.

  This is also the cleanest reason to inject `TimeProvider` everywhere rather than call `DateTimeOffset.Now`: swapping the scoped registration makes the whole render tree user-local for free.

- **Blazor WebAssembly:** the runtime loads only a subset of globalization data covering the app's own culture (**corrected 2026-08-20** — an earlier draft of this brief said "invariant globalization by default", which the docs contradict; see [`opinions/globalisation.md`](../opinions/globalisation.md)). Set `<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>` when you need it, and weigh the payload cost ([Blazor globalization and localization](https://learn.microsoft.com/aspnet/core/blazor/globalization-localization?view=aspnetcore-10.0), `ms-learn`, Tier 1). `ms-learn` also advises against `@bind:culture` on date and number field types — the built-in current-culture rendering is the supported path.
- **Send offsets, not naked local strings.** `DateTimeOffset` over `System.Text.Json` serializes ISO 8601 with the offset; `DateOnly`/`TimeOnly` round-trip as `"2026-08-19"` / `"17:00:00.0000000"`. A wire format that omits the offset pushes the ambiguity onto the client.
- **Never format for a user with the invariant culture, and never parse user input with the current one.** The pairing is the reverse of the intuition: display uses the user's culture, storage and interchange use `"O"` / invariant.

## Smaller traps worth carrying

- **`TimeSpan.From*` gained integer overloads in .NET 9** because the `double` ones are lossy — `TimeSpan.FromSeconds(101.832)` is not 101.832 seconds ([breaking change note](https://learn.microsoft.com/dotnet/core/compatibility/core-libraries/9.0/timespan-from-overloads), `ms-learn`, Tier 1). Relevant to `fsharp.md`: this broke F# overload resolution — `TimeSpan.FromMinutes(20)` now needs an explicit type annotation.
- **Windows vs IANA ids.** `TimeZoneInfo.FindSystemTimeZoneById` accepts the platform's native ids; .NET converts between the two families on modern versions, but persisting Windows ids ties data to a platform. Persist IANA.
- **Don't do DST-sensitive arithmetic on `DateTimeOffset`.** Convert to the zone, do the arithmetic in local terms, convert back.

## What this reveals about the repository

**There is no `opinions/datetime.md`, and the topic is not covered anywhere else.** A grep across `opinions/` and `templates/` finds exactly three date/time mentions, all incidental uses of `DateTimeOffset.UtcNow` in examples about other things (`csharp.md:89`, `csharp.md:96`, `ui-frameworks.md:96`). Nothing states a type default, nothing mentions `TimeProvider`, nothing mentions tzdata or invariant globalization.

That is a real gap rather than a niche one, on three counts:

1. **We ship enforcement we don't document.** `Meziantou.Analyzer` in `templates/Directory.Build.props` plus `TreatWarningsAsErrors` means `MA0188` already fails builds for custom clock abstractions in every project that copies our template. Shipping a rule without the opinion behind it is the wrong way round.
2. **`testing.md` is silent on `FakeTimeProvider`,** despite being detailed about xUnit v3, MTP, and snapshot testing. Deterministic time is the single most common source of flaky tests, and the in-box answer is one package.
3. **`data-access.md`'s own TODO** lists the query-side opinions still missing; date/time column mapping (`DateOnly` ↔ `date`, provider differences on `datetimeoffset`) belongs on that list.

> **Overlap resolved 2026-08-20.** The globalisation brief promoted into [`opinions/globalisation.md`](../opinions/globalisation.md) and took the shared host/client material with it: container `tzdata` and ICU, Blazor WebAssembly globalisation data, and `InvariantGlobalization` as a decision separate from AOT. It also carries a one-line floor on storage (UTC instants, IANA zone ids) pointing back here. When this brief promotes, cross-link those rather than restating them, and keep `opinions/datetime.md` to type choice, `TimeProvider`, persistence, and testing.

### Recommended next step

Open an opinion stub at `opinions/datetime.md` covering the four rules at the top of this brief, and cross-link:

- `testing.md` → `FakeTimeProvider` and the DST/leap-day test cases
- `data-access.md` → the storage schema decision and provider mapping
- `ui-frameworks.md` → the per-circuit `BrowserTimeProvider` pattern
- `ci.md` or a new host section → tzdata and `InvariantGlobalization` in container images

Everything above except the Rojansky paragraph is roster-sourced and can promote as-is. Also add `research/` to the README Scope list if a saved brief should be discoverable — it is already in the layout tree.

### `vet-source` candidate

**Shay Rojansky — [roji.org](https://www.roji.org/)** (unvetted). Lead maintainer of Npgsql and an EF Core team member; the deepest independent writing on .NET↔PostgreSQL type mapping, which no current roster source covers. Likely Tier 2 under the independence cap given the EF Core employment, and cadence needs checking — the timestamp-mapping post is from 2021. Worth a formal `vet-source` pass before any Postgres-specific claim promotes into `data-access.md`.
