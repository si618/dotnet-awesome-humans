---
targets: [net10.0, csharp-14]
last-reviewed: 2026-08-19
last-used: 2026-08-19
sources: [ms-learn, meziantou, andrew-lock, khalid, steve-gordon, jon-skeet]
status: open
---

# Internationalisation and localisation

**The mechanism is settled and boring; the decisions are all about defaults.** For .NET 10 the answer is `.resx` resources behind `IStringLocalizer<T>`, culture selection via `RequestLocalizationMiddleware`, and ICU as the culture-data engine on every platform. None of that has moved meaningfully in years, and .NET 10 ships exactly one globalization breaking change — an environment-variable rename (`ms-learn` T1 — [Breaking changes in .NET 10](https://learn.microsoft.com/dotnet/core/compatibility/10)). What actually bites teams are three defaults: globalization-invariant mode arriving uninvited, culture leaking into machine-facing strings, and container/WASM trimming silently removing the data the runtime needs.

This repository has **no coverage of the topic at all** — the roster sweep below is the whole answer, not a supplement to an existing opinion. See [Where this lands](#where-this-lands).

## 1. The one rule that prevents most bugs

Split every format/parse/compare call by audience:

| Audience                                                         | Formatting & parsing                       | Comparison                                                        |
| ---------------------------------------------------------------- | ------------------------------------------ | ----------------------------------------------------------------- |
| A human reading a screen                                         | `CurrentCulture` (the default)             | `CurrentCulture` linguistic compare, for sorting a displayed list |
| A machine — JSON, logs, URLs, file names, config, DB round-trips | `CultureInfo.InvariantCulture`, explicitly | `StringComparison.Ordinal`                                        |

The second row is the one that gets skipped, and the failure is silent until someone runs the service under `de-DE` and `1.5` round-trips as `15`.

`InvariantCulture` is the right answer for _formatting and parsing_, and the wrong answer for _comparison_: Gérald Barré's [StringComparison.InvariantCulture is not always invariant](https://www.meziantou.net/stringcomparison-invariantculture-is-not-always-invariant.htm) (`meziantou` T1, 2025-05-05) shows that only the culture _data_ is invariant — the collation still tracks whichever NLS/ICU version the host happens to carry, so the same comparison can differ across machines. His practice is to ban `StringComparison.InvariantCulture` and `InvariantCultureIgnoreCase` outright via `Microsoft.CodeAnalysis.BannedApiAnalyzers` and use `Ordinal`. That matches the runtime docs, which flag ICU-vs-NLS differences in `IndexOf`, `StartsWith`, `EndsWith`, default sort order and ligature handling, and point at CA1307/CA1309 to find the call sites (`ms-learn` T1 — [Globalization and ICU](https://learn.microsoft.com/dotnet/core/extensions/globalization-icu)).

## 2. ICU is the engine, and it is swappable

Since .NET 5, globalization runs on ICU on every platform, including Windows (which ships `icu.dll`); .NET falls back to NLS only where ICU can't load. Three escape hatches exist, all documented at [Globalization config settings](https://learn.microsoft.com/dotnet/core/runtime-config/globalization) and [Globalization and ICU](https://learn.microsoft.com/dotnet/core/extensions/globalization-icu) (`ms-learn` T1):

- **`System.Globalization.UseNls`** — back to Windows NLS. Only for bug-compatibility with a legacy app; it forfeits the IANA/Windows time-zone-ID conversion APIs.
- **`System.Globalization.AppLocalIcu`** (+ a `Microsoft.ICU.ICU4C.Runtime` package reference) — carry a pinned ICU with the app so collation and CLDR data are identical across every deployment. This is the answer when _reproducible_ comparison matters more than _current_ comparison.
- **`DOTNET_ICU_VERSION_OVERRIDE`** on Linux — pin a system ICU version. **This is the one .NET 10 change in this area**: it was `CLR_ICU_VERSION_OVERRIDE` before .NET 10, and it only works on Microsoft-built .NET, not distro builds.

Precedence note worth knowing: since .NET 9 the _environment variable_ wins over the project/`runtimeconfig.json` value for `UseNls`; before .NET 9 it was the other way round.

## 3. Globalization-invariant mode: know where it came from

`InvariantGlobalization=true` makes the runtime skip ICU entirely and use built-in invariant data. It's a legitimate size/startup optimisation for a service that renders no localised output — and a trap when it arrives by template rather than by decision.

**Verified locally against SDK 10.0.400 (2026-08-19):** `dotnet new webapi` does _not_ set it; `dotnet new webapiaot` _does_ — `<InvariantGlobalization>true</InvariantGlobalization>` sits in the generated `.csproj` next to `<PublishAot>true</PublishAot>`. So the property tends to enter a codebase attached to an unrelated decision (going AOT), and then quietly changes how every `ToString` and `Compare` in the app behaves.

What breaks when it's on:

- Only the invariant culture exists. Creating any other `CultureInfo` throws, unless `PredefinedCulturesOnly=false` (`ms-learn` T1 — [Globalization config settings](https://learn.microsoft.com/dotnet/core/runtime-config/globalization)).
- `TimeZoneInfo.TryConvertIanaIdToWindowsId` / `TryConvertWindowsIdToIanaId` fail — they are ICU-dependent.
- Casing and collation stop being linguistic. Note it is **not** a substitute for `Ordinal`: Barré makes this point explicitly.

**Position:** treat `InvariantGlobalization` as an explicit, commented decision, never an inherited template line. If you take AOT, decide invariant mode separately.

## 4. ASP.NET Core: the mechanism

Registration and middleware, per [Globalization and localization in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/localization?view=aspnetcore-10.0) (`ms-learn` T1, doc reviewed 2025-06-20):

```csharp
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var supportedCultures = new[] { "en-GB", "fr", "de" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));
```

Points that decide the design:

- **`SupportedCultures` and `SupportedUICultures` are different axes.** `CurrentCulture` drives number/date/currency formatting and sorting; `CurrentUICulture` drives which `.resx` the `ResourceManager` resolves. A user who wants English text with German number formatting is a supported configuration, not a bug.
- **Provider order is query string → cookie → `Accept-Language`, first match wins.** Andrew Lock's recommendation is to drive production off the cookie and keep the query string for debugging — `Accept-Language` reflects the OS, not a choice the user made (`andrew-lock` T1 — [Adding Localisation to an ASP.NET Core application](https://andrewlock.net/adding-localisation-to-an-asp-net-core-application/), 2016-09-20). The docs agree: "A production app should include a way for a user to customize their choice of culture."
- **Middleware order matters.** `UseRequestLocalization` must run before anything that reads the culture, and _after_ routing if you use `RouteDataRequestCultureProvider`.
- **Culture fallback is per-resource, and the default file is the fallback.** `Welcome.fr-CA.resx` → `Welcome.fr.resx` → `Welcome.resx`. If you want a missing translation to surface as the key rather than silently render English, don't ship a default `.resx` at all.
- **`RootNamespace ≠ AssemblyName` breaks lookup entirely** (e.g. a project named `my-project-name`). Fix with `[assembly: RootNamespace]` and `[assembly: ResourceLocation]`, not by renaming resources.

### Resource keys: the unresolved bit

`IStringLocalizer` deliberately lets you use the default-language string as the key (`_localizer["About Title"]`), which removes the up-front `.resx` work — and scatters magic strings through the codebase. Lock flagged this the same week he wrote the introduction and answered it with a `static class ResourceKeys` of constants ([Localising the DisplayAttribute and avoiding magic strings](https://andrewlock.net/localising-the-displayattribute-and-avoiding-magic-strings-in-asp-net-core/), `andrew-lock` T1, 2016-09-27). Khalid takes the other route — designer-generated strongly-typed classes with a `Culture` property ([Getting Started With .NET Localization](https://khalidabuhakmeh.com/getting-started-with-net-localization), `khalid` T1, 2020-03-24).

**Where the sources disagree, and how to weigh it:** the docs and Lock optimise for _not blocking development_ on translation; Khalid optimises for _compile-time safety_. The reason the tension has never resolved is that .NET still has no cross-platform strongly-typed resource generator in the box — **verified locally**: adding `<EmbeddedResource Update="Strings.resx"><GenerateSource>true</GenerateSource></EmbeddedResource>` to a net10.0 library builds clean and emits nothing. The designer-file route is Visual Studio tooling (the generated `.Designer.cs` is then checked in and works anywhere); the cross-platform route is a source generator — Microsoft ships [`Microsoft.CodeAnalysis.ResxSourceGenerator`](https://www.nuget.org/packages/Microsoft.CodeAnalysis.ResxSourceGenerator), used by dotnet/runtime itself, but only as a prerelease (5.0.0-1.25277.114, 2025-06-06). Prerelease-only is a real adoption cost; a constants class costs nothing.

## 5. Client-side and mobile

- **Blazor WebAssembly.** Only the app's own culture data ships by default; `<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>` is required if the user can switch culture at runtime, and `loadAllSatelliteResources: true` in `Blazor.start({ configureRuntime: ... })` loads every satellite assembly at startup so switching doesn't need a reload. Time-zone data is separately trimmable via `<InvariantTimezone>true</InvariantTimezone>` (.NET 8+); `<BlazorEnableTimeZoneSupport>` is superseded and should be removed. **.NET 10 change:** standalone WASM apps now load globalization data for `CultureInfo.DefaultThreadCurrentUICulture` as well as `DefaultThreadCurrentCulture` — on .NET 9 and earlier only the latter counted. (`ms-learn` T1 — [Blazor globalization and localization](https://learn.microsoft.com/aspnet/core/blazor/globalization-localization?view=aspnetcore-10.0), doc updated 2026-08-17.)
- **.NET MAUI.** Plain `.resx` per culture plus `CultureInfo.DefaultThreadCurrentUICulture`; there is no MAUI-specific abstraction and no `RequestLocalization` equivalent, so runtime culture switching means re-resolving bindings yourself (`ms-learn` T1 — [Localization — .NET MAUI](https://learn.microsoft.com/dotnet/maui/fundamentals/localization?view=net-maui-10.0)).
- **Avalonia.** Documented approach is `.resx` reached from XAML via `{x:Static}` against a generated resources class ([Localizing using ResX](https://docs.avaloniaui.net/docs/app-development/localizing) — official Avalonia docs, _not_ a roster source). Dynamic language switching without a restart needs a custom markup extension or a third-party package; there is no first-party answer, and the community options are all **unvetted**. Consistent with the source-redundancy caveat already recorded in [`opinions/ui-frameworks.md`](../opinions/ui-frameworks.md).

## 6. Time, which is where correctness actually goes wrong

Localisation makes dates _look_ right; it does nothing to make them _be_ right. The roster's authority here is Jon Skeet, whose objection is structural: `DateTime` carries a `Kind` that gives one type "different modes which make it mean fairly significantly different things", and a local date/time can legitimately occur twice or not at all ([More fun with DateTime](https://codeblog.jonskeet.uk/2012/05/02/more-fun-with-datetime/), `jon-skeet` T1, 2012-05-02; [The mysteries of BCL time zone data](https://codeblog.jonskeet.uk/2014/09/30/the-mysteries-of-bcl-time-zone-data/), 2014-09-30). His answer is [Noda Time](https://nodatime.org/)'s distinct types — `Instant`, `LocalDateTime`, `ZonedDateTime` — so the ambiguity is in the type system rather than in a comment. These posts predate `DateOnly`/`TimeOnly` (.NET 6), which absorb part of the argument; the zoned-vs-local distinction is untouched by them.

Practical floor regardless of whether you take Noda Time: store instants as UTC, store a user's _time zone ID_ rather than a fixed offset, and convert at the edge. Use IANA IDs — `TimeZoneInfo.TryConvertIanaIdToWindowsId` bridges the two, but only outside invariant and NLS modes.

## 7. Containers: where globalization silently disappears

[`opinions/project-structure.md`](../opinions/project-structure.md) already steers container builds to `noble-chiseled` or `alpine` for size. Both are exactly the images that drop this data:

- **`tzdata`** is absent from Alpine images, so `TimeZoneInfo.FindSystemTimeZoneById` throws `TimeZoneNotFoundException`. Steve Gordon documented this and the fix — `RUN apk add --no-cache tzdata`, or move to a Debian/Ubuntu-based image ([TimeZoneNotFoundException in Alpine Based Docker Images](https://www.stevejgordon.co.uk/timezonenotfoundexception-in-alpine-based-docker-images), `steve-gordon` T1, 2019-11-12). Old, and still exactly right.
- **ICU** is absent from the `-extra`-less chiseled and `-composite` variants; those images only work for apps in invariant mode (`ms-learn` T1 — [.NET container images](https://learn.microsoft.com/dotnet/core/docker/container-images)).

The failure mode is the same in both cases: it passes on the developer's machine and throws in production, because the missing piece is in the base image, not the code.

## 8. Analyzer coverage — better than expected

Hypothesis going in was that the globalization CA rules are off by default and that [`templates/Directory.Build.props`](../templates/Directory.Build.props) would need `<AnalysisModeGlobalization>All</AnalysisModeGlobalization>`. **Tested locally (net10.0, `EnableNETAnalyzers` + `AnalysisLevel=latest-recommended`, SDK 10.0.400) — that is wrong.** The template as it stands already reports:

| Rule       | Trigger                                         | Status at `latest-recommended`                            |
| ---------- | ----------------------------------------------- | --------------------------------------------------------- |
| CA1304     | `string.ToUpper()`                              | ✅ warns                                                  |
| CA1305     | `double.ToString()`                             | ✅ warns                                                  |
| CA1309     | `StringComparison.InvariantCulture`             | ✅ warns                                                  |
| CA1310     | `string.IndexOf(string)`                        | ✅ warns                                                  |
| CA1311     | culture-less casing                             | ✅ warns                                                  |
| **CA1307** | `string.Equals(string)` — no `StringComparison` | ❌ **silent**; needs an explicit `.editorconfig` severity |

Adding `<AnalysisModeGlobalization>All</AnalysisModeGlobalization>` changed nothing — it did not enable CA1307 either. The single gap is one line in [`templates/.editorconfig`](../templates/.editorconfig):

```ini
dotnet_diagnostic.CA1307.severity = warning
```

With `TreatWarningsAsErrors` already on in the template, that turns "someone forgot a `StringComparison`" into a build failure. Barré's banned-API list for `InvariantCulture` is the optional second step; CA1309 already covers most of it.

## 9. Preview — not yet an opinion

- **`WebAssemblyComponentsOptions.UseCultureFromServer`** — lets a Blazor Web App's WASM client pick a culture independently of the server during prerendering. The docs place it under the **`aspnetcore-11.0` moniker only**, i.e. .NET 11, in preview as of 2026-08-19. A general web search summarised it as a .NET 10 feature; the moniker on the page itself says otherwise, and the moniker wins. Not applicable to .NET 10 apps.

## Where this lands

The repository answers **none** of this today. `internationalisation`, `localisation`, `IStringLocalizer`, `CultureInfo`, `resx` and `TimeZoneInfo` appear nowhere in `opinions/`; the sole adjacent line is the parenthetical "culture-sensitive strings" justification for Meziantou.Analyzer in [`opinions/csharp.md`](../opinions/csharp.md) and [`templates/Directory.Build.props`](../templates/Directory.Build.props). This is a genuine gap rather than a deliberate omission — the topic cuts across four existing files:

| Finding                                                                             | Lands in                                        |
| ----------------------------------------------------------------------------------- | ----------------------------------------------- |
| Invariant/ordinal split; CA1307 severity line                                       | `opinions/csharp.md`, `templates/.editorconfig` |
| `AddLocalization` / `UseRequestLocalization` / provider order / resource-key policy | `opinions/aspnet-core.md`                       |
| `InvariantGlobalization` as an explicit decision, decoupled from `PublishAot`       | `opinions/runtime-performance.md` (Native AOT)  |
| ICU and `tzdata` absent from chiseled/Alpine images                                 | `opinions/project-structure.md` (Containers)    |
| Blazor WASM globalization data + satellite loading; MAUI/Avalonia `.resx`           | `opinions/ui-frameworks.md`                     |

Whether that is five cross-cutting additions or a new `opinions/globalisation.md` is the promotion decision this brief is waiting on. The one-file argument is that the invariant/ordinal rule is a single idea whose five consequences read as unrelated trivia when scattered; the five-additions argument is that each lands next to the setting it modifies, and the repository's stated convention is one topic per file.

Every claim above except the Avalonia docs link is roster-sourced, so this brief is promotable as-is — no `vet-source` run is a precondition. Non-roster material was consulted for discovery and none of it survived into a load-bearing claim.

**Recommended next step:** `harvest-sources`-style fold-in of the table above, with the one-file-versus-five decision made first. `refresh-dotnet-versions` is not implicated — nothing here is version-drift.
