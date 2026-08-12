# Awesome Humans

The vetted sources behind every opinion in this repository. Content only enters the opinions if it traces back to a source listed here.

## Admission criteria

A **source** is the published work of an awesome human — an individual or a publication. Admission requires an **established, proven track record**:

1. **Longevity** — sustained, consistent publishing. Tier 1 requires roughly a decade or more; Tier 2 requires 8+ years.
2. **Depth** — original insight (internals, measurements, worked reasoning), not paraphrased release notes.
3. **Accuracy** — a history of being right; corrections issued when wrong.
4. **Independence of signal** — the content stands on its own merit, not on marketing reach or algorithm-chasing.

Candidates that don't yet qualify are tracked under [Watch list](#watch-list) and re-evaluated by the `vet-source` skill. Admission and demotion decisions are recorded in the log at the bottom of this file.

Each source has a stable `id` used by the `sources:` frontmatter in `opinions/`.

## Tier 1 — decade-class track record

| id                | Source                                                                                         | Focus                                                          | Since                                       | Notes                                                                                               |
| ----------------- | ---------------------------------------------------------------------------------------------- | -------------------------------------------------------------- | ------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| `dotnet-blog`     | [.NET Blog](https://devblogs.microsoft.com/dotnet/)                                            | Everything .NET; canonical announcements                       | ~2004                                       | Includes Stephen Toub's annual "Performance Improvements in .NET" posts and the "What's new" series |
| `aspnet-blog`     | [ASP.NET Core category, .NET Blog](https://devblogs.microsoft.com/dotnet/category/aspnetcore/) | ASP.NET Core announcements & deep-dives                        | ~2004                                       |                                                                                                     |
| `ms-learn`        | [Microsoft Learn — .NET docs](https://learn.microsoft.com/dotnet/)                             | Official documentation, breaking changes, architecture e-books | ~2016 (docs.microsoft.com era; MSDN before) | "What's new" and "Breaking changes" pages per release                                               |
| `stephen-toub`    | [Stephen Toub (author page)](https://devblogs.microsoft.com/dotnet/author/toub/)               | Runtime/BCL performance, async internals                       | ~2005                                       | Publishes via `dotnet-blog`; also the "Deep .NET" video series with Scott Hanselman                 |
| `mikesdotnetting` | [Mike Brind — Mikesdotnetting](https://www.mikesdotnetting.com/)                               | ASP.NET web dev, Razor Pages                                   | 2007                                        | One of the longest-running independent .NET blogs                                                   |
| `morning-brew`    | [The Morning Brew](https://blog.cwa.me.uk/)                                                    | Daily .NET link aggregation                                    | 2008                                        | Discovery channel, not an opinion source itself                                                     |
| `awesome-dotnet`  | [awesome-dotnet](https://github.com/quozd/awesome-dotnet)                                      | Curated library/tool list                                      | 2014                                        | Library discovery; verify recommendations independently                                             |
| `meziantou`       | [Gérald Barré — Meziantou's blog](https://www.meziantou.net/)                                  | C# language, analyzers, practical best practices               | ~2015                                       | Very high volume and consistency                                                                    |
| `khalid`          | [Khalid Abuhakmeh](https://khalidabuhakmeh.com/)                                               | Practical .NET, tooling                                        | ~2012                                       | Also writes much of JetBrains' .NET content                                                         |
| `scott-wlaschin`  | [Scott Wlaschin — F# for Fun and Profit](https://fsharpforfunandprofit.com/)                   | F#, functional design, domain modelling                        | 2012                                        | The definitive independent F# resource; author of _Domain Modeling Made Functional_                 |
| `fsharp-weekly`   | [Sergey Tihon — F# Weekly](https://sergeytihon.com/category/f-weekly/)                         | Weekly F# link aggregation                                     | 2011                                        | Discovery channel for the F# ecosystem, not an opinion source itself                                |

## Tier 2 — established (8+ years)

| id                 | Source                                                    | Focus                                                       | Since  | Notes                                                                                                   |
| ------------------ | --------------------------------------------------------- | ----------------------------------------------------------- | ------ | ------------------------------------------------------------------------------------------------------- |
| `andrew-lock`      | [Andrew Lock — .NET Escapades](https://andrewlock.net/)   | ASP.NET Core internals, "Exploring .NET" per-release series | 2016   | The de facto reference for ASP.NET Core internals outside Microsoft; author of _ASP.NET Core in Action_ |
| `steve-gordon`     | [Steve Gordon](https://www.stevejgordon.co.uk/)           | High-performance .NET, HttpClient, Span/ArrayPool           | ~2016  | Cadence has slowed; back catalogue remains authoritative                                                |
| `jetbrains-dotnet` | [JetBrains .NET Blog](https://blog.jetbrains.com/dotnet/) | C# language features, tooling                               | ~2010s | Institutional; tool-flavoured but strong language explainers                                            |

## Watch list — strong today, track record still forming

Not yet quotable as primary sources for opinions; usable for discovery and cross-checking. Re-evaluate periodically.

| id                 | Source                                                         | Focus                                          | Since | Blocker                                      |
| ------------------ | -------------------------------------------------------------- | ---------------------------------------------- | ----- | -------------------------------------------- |
| `milan-jovanovic`  | [Milan Jovanović](https://www.milanjovanovic.tech/blog)        | Architecture, EF Core, CQRS, modular monoliths | ~2020 | Longevity — strong candidate for promotion   |
| `nick-chapsas`     | [Nick Chapsas (YouTube)](https://www.youtube.com/@nickchapsas) | New features, anti-patterns                    | ~2020 | Longevity; trend-driven format               |
| `code-with-mukesh` | [CodeWithMukesh](https://codewithmukesh.com/)                  | Best-practice roundups                         | ~2020 | Longevity; listicle-styled                   |
| `csharp-digest`    | [C# Digest](https://csharpdigest.net/)                         | Weekly newsletter aggregation                  | —     | Aggregator; use for discovery                |
| `telerik-blog`     | [Telerik Blogs — .NET](https://www.telerik.com/blogs)          | Release summaries                              | —     | Marketing-adjacent; quality varies by author |
| `syncfusion-blog`  | [Syncfusion Blogs](https://www.syncfusion.com/blogs/)          | Release summaries, performance roundups        | —     | Marketing-adjacent; quality varies by author |

## Decision log

| Date       | Decision                                            | Detail                                                                                            |
| ---------- | --------------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| 2026-08-12 | Initial roster                                      | Seeded Tier 1/Tier 2/Watch list from curated research (verified against .NET 10 era, August 2026) |
| 2026-08-12 | Admitted `scott-wlaschin`, `fsharp-weekly` (Tier 1) | Filled the F# gap; both decade-class (2012 / 2011)                                                |
