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

### Component testing

- **Unit-test components with bUnit; reserve Playwright for end-to-end.** There is no official Microsoft component-testing framework — Microsoft Learn points at bUnit, which renders components in-process with no browser. Assert with `MarkupMatches` (semantic HTML comparison — insignificant whitespace and attribute order don't fail the test), never raw string equality on markup. Behaviour that depends on real JS interop or browser DOM manipulation is E2E territory: test it with Playwright, not by faking `IJSRuntime` into meaninglessness. ([Test components in ASP.NET Core Blazor](https://learn.microsoft.com/aspnet/core/blazor/test))
- **Use bUnit 2.x with xUnit v3** to match this repository's testing stack ([opinions/testing.md](testing.md)); in 2.x the test class inherits `BunitContext` and renders with `Render<T>()`.

  <!-- Verified: compiles and passes on net10.0 / bunit 2.9.0 / xunit.v3 3.2.2 (2026-08-12) -->

  ```csharp
  public class CounterTests : BunitContext
  {
      [Fact]
      public void Counter_ClickingButton_IncrementsCount()
      {
          // Arrange
          var cut = Render<Counter>();

          // Act
          cut.Find("button").Click();

          // Assert
          cut.Find("[role=status]").MarkupMatches(
              """<p role="status">Current count: 1</p>""");
      }
  }
  ```

## .NET MAUI

- **Abstract platform billing/purchases behind a single interface (`IBillingService`) with conditional compilation per store, and always validate purchases server-side.** ([Versluis — Cross-platform in-app billing](https://devblogs.microsoft.com/dotnet/cross-platform-billing-dotnet-maui/))
- **Enable Material 3 on Android (`<UseMaterial3>true</UseMaterial3>`, MAUI 10.0.60+)** for current-generation theming. ([Versluis — Material 3 makeover](https://devblogs.microsoft.com/dotnet/dotnet-maui-material-3/))
- **Treat trimming compliance as a release requirement** (store mandates like Google Play's 16 KB page size; exclude problem assemblies explicitly rather than disabling trimming). (Versluis — trimming and store-compliance posts)
- **Test preview SDKs with `sdk.paths` in `global.json`** rather than polluting the machine default. ([Versluis — sdk.paths](https://blog.verslu.is/))

### App lifecycle

- **Handle lifecycle with the cross-platform `Window` events, not per-platform code.** Subclass `Window`, override the events you need (`OnCreated`, `OnActivated`, `OnDeactivated`, `OnStopped`, `OnResumed`, `OnDestroying`; `OnBackgrounding` on iOS/Mac Catalyst), and return it from `App.CreateWindow`. In `Stopped`, disconnect long-running work and cancel pending requests; in `Resumed`, resubscribe and refresh visible content. Drop to `ConfigureLifecycleEvents` in `MauiProgram` only when a genuinely platform-specific hook has no cross-platform event. ([.NET MAUI app lifecycle](https://learn.microsoft.com/dotnet/maui/fundamentals/app-lifecycle))

  <!-- Illustrative only: MAUI workloads are not installed on the authoring machine, so this snippet is unverified -->

  ```csharp
  public class MainWindow : Window
  {
      protected override void OnStopped() => _sync.PauseBackgroundSync();

      protected override void OnResumed() => _sync.ResumeAndRefresh();
  }
  ```

### Offline & sync

- **Build mobile apps offline-first: the local store is the source of truth, the network is a sync detail.** Cache remote data locally with explicit expiry — SQLite-net for queryable data, or MonkeyCache's `Barrel.Current.Add(key, data, expireIn, eTag)` for simple payload caching; the ETag overloads let you skip re-downloading unchanged responses. ([Montemagno — Data caching made simple with Monkey Cache](https://montemagno.com/data-caching-made-simple-with-monkey-cache/))
- **Gate remote calls on `Connectivity.NetworkAccess == NetworkAccess.Internet` and react to `ConnectivityChanged`; never ping-test reachability.** The old `IsReachable`-style APIs were dropped deliberately — make the real request and handle failure. ([Montemagno — Upgrading to Xamarin.Essentials from Plugins](https://montemagno.com/upgrading-from-plugins-to-xamarin-essentials/))

## Avalonia & reactive UI

- **Keep view models free of UI-framework references.** Property-change notification (`INotifyPropertyChanged`) and Rx/Dynamic Data types are platform-neutral — a view model with no Avalonia (or WPF) reference binds unchanged on either framework and unit-tests without a UI. ([Polyak — Rx and Dynamic Data overview](https://dev.to/npolyak/reactive-extensions-rxnet-and-dynamic-data-overview-in-c-59lb))
- **For reactive collection state in XAML UIs, use Dynamic Data over hand-rolled `ObservableCollection` plumbing.** Key entities in a `SourceCache<T, TKey>` and project filtered/sorted views into a bindable collection with `Connect()` — updates, filters, and sorts then compose instead of being re-implemented per screen. ([Polyak — Introduction to Dynamic Data](https://dev.to/npolyak/introduction-to-dynamic-data-10hf), [Observable Cache](https://dev.to/npolyak/introduction-to-dynamic-datas-observable-cache-eeh))

  <!-- Verified: compiles and runs on net10.0 / DynamicData 9.4.33 (2026-08-12) -->

  ```csharp
  var orders = new SourceCache<Order, int>(o => o.Id);

  ReadOnlyObservableCollection<Order> openOrders;
  using var subscription = orders.Connect()
      .Filter(o => o.Status == OrderStatus.Open)
      .SortBy(o => o.Placed)
      .Bind(out openOrders)   // the view binds to openOrders; only mutate the cache
      .Subscribe();

  orders.AddOrUpdate(new Order(1, OrderStatus.Open, DateTimeOffset.UtcNow));
  ```

- **Source-redundancy note:** Avalonia guidance currently has lower source redundancy than the rest of this repository — the vetted bench is `nick-polyak` (Tier 1) plus watch-listed institutional sources (`avalonia-blog`, `awesome-avalonia`). His Dev.to MVVM coverage is still introductory (the deep Avalonia architecture series lives in his read-only CodeProject archive), so verify Avalonia-specific claims against the project docs.

<!-- Mobile practice opinions (lifecycle, offline/sync, store compliance) split out into their own file if the MAUI section outgrows this one -->
