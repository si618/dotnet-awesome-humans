---
targets: [net10.0, csharp-14]
last-reviewed: 2026-08-12
last-used: 2026-08-12
sources: [dotnet-blog, aspnet-blog, ms-learn, meziantou, andrew-lock]
---

# ASP.NET Core

Target ASP.NET Core 10. Minimal APIs are the default for new HTTP APIs.

## Opinions

- **Use `HybridCache` instead of hand-rolled cache-aside over `IMemoryCache`/`IDistributedCache`.** `GetOrCreateAsync()` handles stampede protection, L1 (in-memory) + L2 (distributed) tiering, and tag-based invalidation. ([Hello HybridCache!](https://devblogs.microsoft.com/dotnet/hybrid-cache-is-now-ga/), [tiered caching with Postgres](https://devblogs.microsoft.com/dotnet/high-performance-distributed-caching-dotnet-postgres-azure/)) <!-- TODO: code example -->
- **Use minimal-API validation (`AddValidation()` + data annotations) for request validation** — it returns 400 automatically and the validation APIs now live in `Microsoft.Extensions.Validation`, reusable outside HTTP. ([What's new in ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0))
- **Treat OpenAPI as a build artefact, not an afterthought.** OpenAPI 3.1 generation is in the box and on by default in the Web API template; auto-populate XML doc comments into the document; version APIs with `Asp.Versioning` v10 and `WithDocumentPerVersion()` for one document per version. ([API versioning with OpenAPI](https://devblogs.microsoft.com/dotnet/api-versioning-in-dotnet-10-applications/)) <!-- TODO: code example -->
- **Generate typed clients from your own OpenAPI document at build time (Kiota)** rather than hand-writing HttpClient wrappers — the contract stays versioned and the client cannot drift. ([Meziantou — Kiota client at build time](https://www.meziantou.net/generate-a-kiota-client-at-build-time-from-an-asp-net-core-openapi-file.htm))
- **APIs send `Cache-Control: no-cache, no-store, must-revalidate` by default**; opt individual endpoints into caching deliberately. Stale-data bugs and cache-poisoning surprises come from the opposite default. ([Meziantou — Disable HTTP caching by default](https://www.meziantou.net/disable-http-caching-by-default-in-asp-net-core-apis.htm))
- **Use Server-Sent Events (`TypedResults.ServerSentEvents`) for one-way server push** before reaching for SignalR/WebSockets. ([What's new in ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0))
- **Return 401/403 from API endpoints, never login redirects** — ASP.NET Core 10 avoids cookie redirects for known API endpoints; align custom auth handlers with that. ([What's new in ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0))
- **Layer CSRF defence with Fetch Metadata headers (`Sec-Fetch-Site` and friends)** alongside token-based antiforgery; .NET 11 will automate this. ([Lock — Understanding the Fetch Metadata headers](https://andrewlock.net/understanding-the-fetch-metadata-http-headers-sec-fetch-site-and-friends/))

## Coming next (preview — not yet the opinion)

.NET 11 previews add automatic Fetch-Metadata-based CSRF protection (removing `UseAntiforgery()` for minimal APIs/Blazor SSR). ([Lock — .NET 11 preview 6](https://andrewlock.net/exploring-the-dotnet-11-preview-6-automatic-csrf-protection-based-on-fetch-metadata-http-headers/))

<!-- TODO: full treatment — hosting/deployment, auth (passkeys/WebAuthn in Identity), observability (OTel propagation per meziantou), rate limiting -->
