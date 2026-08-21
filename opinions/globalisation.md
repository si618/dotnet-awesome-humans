---
targets: [net10.0, csharp-14]
last-reviewed: 2026-08-21
last-used: 2026-08-21
sources:
  [
    ms-learn,
    meziantou,
    andrew-lock,
    khalid,
    steve-gordon,
    jon-skeet,
    avalonia-blog,
  ]
---

# Globalisation & localisation

The mechanism is settled: `.resx` resources behind `IStringLocalizer<T>`, culture selection via request localisation middleware, ICU as the culture-data engine on every platform. The decisions that bite are all defaults — culture leaking into machine-facing strings, invariant mode arriving by template rather than by choice, and container images that quietly ship without the data the runtime needs.

## Opinions

### Split every format, parse, and compare call by audience

**Human-facing text uses the current culture; machine-facing text uses `CultureInfo.InvariantCulture` explicitly, and machine-facing comparison uses `StringComparison.Ordinal`.** This one rule prevents most globalisation bugs, and the second half is the half that gets skipped — the failure is silent until someone runs the service under `de-DE` and `1.5` round-trips as `15`.

| Audience                                                         | Formatting & parsing                       | Comparison                                     |
| ---------------------------------------------------------------- | ------------------------------------------ | ---------------------------------------------- |
| A human reading a screen                                         | `CurrentCulture` (the default)             | `CurrentCulture`, for sorting a displayed list |
| A machine — JSON, logs, URLs, file names, config, DB round-trips | `CultureInfo.InvariantCulture`, explicitly | `StringComparison.Ordinal`                     |

**Never use `StringComparison.InvariantCulture` or `InvariantCultureIgnoreCase`.** They look like the machine-facing answer and are not: only the culture _data_ is invariant, while the collation still tracks whichever ICU (or NLS) version the host carries, so the same comparison can return different answers on two machines. `InvariantCulture` is right for formatting and parsing and wrong for comparison — the two halves of the type are not the same promise. ([Meziantou — StringComparison.InvariantCulture is not always invariant](https://www.meziantou.net/stringcomparison-invariantculture-is-not-always-invariant.htm), [Globalization and ICU](https://learn.microsoft.com/dotnet/core/extensions/globalization-icu))

```csharp
// Wrong — the host's collation decides, and hosts disagree
if (string.Equals(header, "application/json", StringComparison.InvariantCultureIgnoreCase))

// Right — a protocol token is machine-facing
if (string.Equals(header, "application/json", StringComparison.OrdinalIgnoreCase))
```

The analyzers find the call sites: CA1304/CA1305 for culture-less formatting, CA1309 for `InvariantCulture` comparison, CA1310/CA1311 for culture-less `IndexOf` and casing. All five warn at the `latest-recommended` level [templates/Directory.Build.props](../templates/Directory.Build.props) sets — **CA1307 is the exception and needs an explicit severity**, which [templates/.editorconfig](../templates/.editorconfig) now carries:

```ini
dotnet_diagnostic.CA1307.severity = warning
```

With `TreatWarningsAsErrors` already on, that turns "someone forgot a `StringComparison`" into a build failure. Meziantou.Analyzer (see [csharp.md](csharp.md)) covers the same ground from the other direction; a `BannedApiAnalyzers` list for the two invariant comparisons is the optional second step, and lost here because CA1309 already catches them without a second third-party analyzer.

### Treat `InvariantGlobalization` as an explicit decision, never an inherited template line

**Decide invariant mode on its own merits, and comment the property where you set it.** `<InvariantGlobalization>true</InvariantGlobalization>` makes the runtime skip ICU entirely and use built-in invariant data — a legitimate size and startup win for a service that renders no localised output, and a trap when it arrives attached to an unrelated decision. Verified on SDK 10.0.400: `dotnet new webapi` does not set it, `dotnet new webapiaot` does, sitting in the generated project file next to `<PublishAot>true</PublishAot>`. So it typically enters a codebase because someone went Native AOT (see [runtime-performance.md](runtime-performance.md)), and then quietly changes how every `ToString` and `Compare` in the app behaves.

What turning it on costs:

- Only the invariant culture exists. Constructing any other `CultureInfo` throws, unless you also set `PredefinedCulturesOnly=false`.
- `TimeZoneInfo.TryConvertIanaIdToWindowsId` and `TryConvertWindowsIdToIanaId` fail — both are ICU-dependent.
- Casing and collation stop being linguistic. This is **not** a substitute for `Ordinal`: the audience split above still applies.

([Globalization config settings](https://learn.microsoft.com/dotnet/core/runtime-config/globalization), [Globalization and ICU](https://learn.microsoft.com/dotnet/core/extensions/globalization-icu))

### Pin ICU only when reproducible comparison beats current comparison

**Take the default — system ICU — unless you have a stated reason not to.** Globalisation has run on ICU on every platform since .NET 5, including Windows, which ships `icu.dll`. Three escape hatches exist, in descending order of how often they are right:

- **`System.Globalization.AppLocalIcu`** plus a `Microsoft.ICU.ICU4C.Runtime` package reference carries a pinned ICU with the app, so collation and CLDR data are byte-identical across every deployment. The right answer when a sort order is part of your contract.
- **`DOTNET_ICU_VERSION_OVERRIDE`** pins a system ICU version on Linux. Note the .NET 10 rename — it was `CLR_ICU_VERSION_OVERRIDE` before — and that it only applies to Microsoft-built .NET, not distro builds. ([Breaking changes in .NET 10](https://learn.microsoft.com/dotnet/core/compatibility/10))
- **`System.Globalization.UseNls`** goes back to Windows NLS. Bug-compatibility with a legacy app only; it forfeits the IANA/Windows time-zone-id conversion APIs. Since .NET 9 the environment variable wins over the `runtimeconfig.json` value for this setting — it was the other way round before.

([Globalization config settings](https://learn.microsoft.com/dotnet/core/runtime-config/globalization))

### ASP.NET Core: register localisation, then drive culture from a cookie

**Call `AddLocalization` with a `ResourcesPath`, and select culture with `UseRequestLocalization` early in the pipeline.**

```csharp
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var supportedCultures = new[] { "en-GB", "fr", "de" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));
```

Four things decide whether this works in production:

- **`SupportedCultures` and `SupportedUICultures` are separate axes.** `CurrentCulture` drives number, date, and currency formatting and sorting; `CurrentUICulture` drives which `.resx` the `ResourceManager` resolves. English text with German number formatting is a supported configuration, not a bug.
- **Provider order is query string → cookie → `Accept-Language`, first match wins.** Drive production off the cookie and keep the query string for debugging: `Accept-Language` reflects the user's OS, not a choice they made, so a production app needs a way for a user to set their own culture. ([Lock — Adding Localisation to an ASP.NET Core application](https://andrewlock.net/adding-localisation-to-an-asp-net-core-application/))
- **Middleware order matters.** `UseRequestLocalization` runs before anything that reads the culture, and _after_ routing if you use `RouteDataRequestCultureProvider`.
- **Culture fallback is per-resource and ends at the default file** — `Welcome.fr-CA.resx` → `Welcome.fr.resx` → `Welcome.resx`. If you want a missing translation to surface as its key rather than silently render English, ship no default `.resx` at all.

One failure worth recognising on sight: **if `RootNamespace` and `AssemblyName` differ** (a project directory named `my-project-name`, say), resource lookup fails entirely. Fix it with `[assembly: RootNamespace]` and `[assembly: ResourceLocation]`, not by renaming resources. ([Globalization and localization in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/localization?view=aspnetcore-10.0))

### Name resource keys with a constants class, not magic strings

**Reach `IStringLocalizer` through a `static class ResourceKeys` of `const string` fields.** `IStringLocalizer` deliberately accepts the default-language string as the key (`_localizer["About Title"]`), which removes the up-front `.resx` work and scatters magic strings through the codebase in exchange. A constants class costs one file, keeps the key in one place, and works in attributes such as `[Display(Name = ResourceKeys.AboutTitle)]`. ([Lock — Localising the DisplayAttribute and avoiding magic strings](https://andrewlock.net/localising-the-displayattribute-and-avoiding-magic-strings-in-asp-net-core/))

Designer-generated strongly-typed resource classes are the compile-time-safe alternative and lost on portability: .NET still ships no cross-platform strongly-typed resource generator in the box — `<GenerateSource>true</GenerateSource>` on an `EmbeddedResource` builds clean and emits nothing on net10.0 — so the designer route is Visual Studio tooling with a checked-in `.Designer.cs`, and the cross-platform route is `Microsoft.CodeAnalysis.ResxSourceGenerator`, which dotnet/runtime itself uses but Microsoft still publishes only as a prerelease. Revisit when it ships stable. ([Abuhakmeh — Getting Started With .NET Localization](https://khalidabuhakmeh.com/getting-started-with-net-localization))

### Client-side: ship the globalisation data the app can actually reach

- **Blazor WebAssembly loads only the app's own culture data by default.** Blazor loads a subset covering the app's own culture, so set `<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>` if the user can switch culture at runtime. Time-zone data trims separately via `<InvariantTimezone>true</InvariantTimezone>`; `<BlazorEnableTimeZoneSupport>` is superseded — delete it. In .NET 10, standalone WASM apps also load globalisation data for `CultureInfo.DefaultThreadCurrentUICulture`, where .NET 9 and earlier honoured only `DefaultThreadCurrentCulture`. ([Blazor globalization and localization](https://learn.microsoft.com/aspnet/core/blazor/globalization-localization?view=aspnetcore-10.0))
- **.NET MAUI has no localisation abstraction** — plain per-culture `.resx` plus `CultureInfo.DefaultThreadCurrentUICulture`, and no `RequestLocalization` equivalent, so switching culture at runtime means re-resolving bindings yourself. ([Localization — .NET MAUI](https://learn.microsoft.com/dotnet/maui/fundamentals/localization?view=net-maui-10.0))
- **Avalonia reaches `.resx` from XAML via `{x:Static}`** against a generated resources class. Switching language without a restart has no first-party answer — it needs a custom markup extension, and the community packages that offer one are unvetted. This is the project's own documentation, so take it for mechanism rather than for whether you should want it; the source-redundancy caveat in [ui-frameworks.md](ui-frameworks.md) applies. ([Localizing using ResX](https://docs.avaloniaui.net/docs/app-development/localizing))

### Containers: chiseled and Alpine images drop this data

**If the app formats dates, sorts text, or resolves a time zone, use the `-extra` image tag.** [project-structure.md](project-structure.md) steers container builds to `noble-chiseled` or `alpine` for size, and the size-optimised images — Alpine, Ubuntu chiseled, Azure Linux distroless — are precisely the ones that "don't include globalization dependencies such as ICU or tzdata … only work with apps that are configured for globalization invariant mode". Every one of them has an `-extra` counterpart (`10.0-alpine-extra`, `10.0-noble-chiseled-extra`) that adds ICU, tzdata, and `stdc++` back. ([.NET container images](https://learn.microsoft.com/dotnet/core/docker/container-images))

The two missing pieces fail differently, and so does the ICU one depending on what the app asks for:

- **No ICU — and the image has already decided for you.** The base images set `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true` as an image environment variable, so the app runs in invariant mode however it was built. An app that names a culture throws `CultureNotFoundException` at startup — `PredefinedCulturesOnly` defaults to true here, so the `RequestLocalizationOptions` above dies before serving a request. An app that only leans on `CurrentCulture` fails quietly instead, formatting and comparing as invariant. Because the setting lives in the environment rather than the project file, `<InvariantGlobalization>false</InvariantGlobalization>` does not undo it — installing `icu-libs` and `icu-data-full` by hand means clearing the variable too. Taking `-extra` is the version that works, because those images simply omit the variable.
- **No tzdata — a loud failure.** `TimeZoneInfo.FindSystemTimeZoneById` throws `TimeZoneNotFoundException`, which is unaffected by invariant mode and so survives as a real exception. `RUN apk add --no-cache tzdata` fixes that one alone. ([Gordon — TimeZoneNotFoundException in Alpine Based Docker Images](https://www.stevejgordon.co.uk/timezonenotfoundexception-in-alpine-based-docker-images))

### Localisation makes dates look right, not be right

**Store instants as UTC, store a user's IANA time-zone id rather than a fixed offset, and convert at the edge** — the full storage and type-choice stance, including when a future local event should _not_ collapse to UTC, is in [datetime.md](datetime.md). A local date and time can legitimately occur twice or not at all, and no amount of culture-correct formatting fixes a value that was ambiguous before it was formatted — which is the argument behind Noda Time's separate `Instant`, `LocalDateTime`, and `ZonedDateTime` types. ([Skeet — More fun with DateTime](https://codeblog.jonskeet.uk/2012/05/02/more-fun-with-datetime/))

`TimeZoneInfo.TryConvertIanaIdToWindowsId` bridges the two id families, but only outside invariant and NLS modes — so the storage decision above and the two configuration decisions above are the same decision.

## Coming next (preview — not yet the opinion)

`WebAssemblyComponentsOptions.UseCultureFromServer` lets a Blazor Web App's WASM client pick a culture independently of the server during prerendering. The documentation carries it under the `aspnetcore-11.0` moniker only — it is .NET 11 preview, not a .NET 10 feature, despite being summarised elsewhere as one. ([Blazor globalization and localization](https://learn.microsoft.com/aspnet/core/blazor/globalization-localization?view=aspnetcore-10.0))
