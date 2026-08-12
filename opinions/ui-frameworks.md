---
targets: [net10.0, csharp-14]
last-reviewed: 2026-08-12
last-used: 2026-08-12
sources: [ms-learn, aspnet-blog, gerald-versluis, james-montemagno, nick-polyak]
---

# UI frameworks

Blazor for web UI, .NET MAUI for mobile + desktop, Avalonia for cross-platform desktop. Pick per app; mix render modes within a Blazor app deliberately.

## Blazor

- **Choose render mode per page, not per app:** static SSR for content/SEO, Interactive Server for low-latency internal apps, Interactive WebAssembly for offline/client-heavy work, Interactive Auto when startup time matters but you want client-side steady state. ([What's new in ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0))
- **Use `[PersistentState]` for state that must survive prerendering and circuit loss** instead of ad-hoc session storage. ([What's new in ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0))
- **Use `NavigationManager.NotFound()` for missing resources** so Blazor returns real 404 semantics. ([What's new in ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0))

## .NET MAUI

- **Abstract platform billing/purchases behind a single interface (`IBillingService`) with conditional compilation per store, and always validate purchases server-side.** ([Versluis — Cross-platform in-app billing](https://devblogs.microsoft.com/dotnet/cross-platform-billing-dotnet-maui/)) <!-- TODO: code example -->
- **Enable Material 3 on Android (`<UseMaterial3>true</UseMaterial3>`, MAUI 10.0.60+)** for current-generation theming. ([Versluis — Material 3 makeover](https://devblogs.microsoft.com/dotnet/dotnet-maui-material-3/))
- **Treat trimming compliance as a release requirement** (store mandates like Google Play's 16 KB page size; exclude problem assemblies explicitly rather than disabling trimming). (Versluis — trimming and store-compliance posts)
- **Test preview SDKs with `sdk.paths` in `global.json`** rather than polluting the machine default. ([Versluis — sdk.paths](https://blog.verslu.is/))

## Avalonia & reactive UI

- **For reactive collection state in XAML UIs, use Dynamic Data over hand-rolled `ObservableCollection` plumbing.** ([Polyak — Dynamic Data series](https://dev.to/npolyak/introduction-to-dynamic-data-10hf))
- **Source-redundancy note:** Avalonia guidance currently has lower source redundancy than the rest of this repository — the vetted bench is `nick-polyak` (Tier 1) plus watch-listed institutional sources (`avalonia-blog`, `awesome-avalonia`). Verify Avalonia-specific claims against the project docs.

<!-- TODO: full treatment — Blazor component testing, MAUI app lifecycle and offline/sync patterns, Avalonia MVVM conventions; mobile practice opinions split out if this section outgrows the file -->
