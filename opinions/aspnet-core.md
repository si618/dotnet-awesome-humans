---
targets: [net10.0, csharp-14]
last-reviewed: 2026-08-12
last-used: 2026-08-12
sources: [dotnet-blog, aspnet-blog, ms-learn, meziantou, andrew-lock]
---

# ASP.NET Core

Target ASP.NET Core 10. Minimal APIs are the default for new HTTP APIs.

## Opinions

### Hosting and deployment

**Run Kestrel directly, packaged as a container image built by `dotnet publish /t:PublishContainer` — no Dockerfile.** Kestrel as the edge server is a fully supported configuration; when you do sit behind a proxy or ingress (for port sharing, TLS termination, or load balancing), enable host filtering and forwarded-headers handling — that is the proxy's contract, not optional hardening. The SDK's container publish builds an OCI image from MSBuild properties (`ContainerRepository`, `ContainerRegistry`), keeps the base image patched with the SDK, and needs no daemon to produce a tarball for scanning pipelines. Hand-written Dockerfiles drift; use them only when you genuinely need custom image layers. ([When to use a reverse proxy with Kestrel](https://learn.microsoft.com/aspnet/core/fundamentals/servers/kestrel/when-to-use-a-reverse-proxy), [Containerize an app with dotnet publish](https://learn.microsoft.com/dotnet/core/containers/sdk-publish))

### Caching with `HybridCache`

**Use `HybridCache` instead of hand-rolled cache-aside over `IMemoryCache`/`IDistributedCache`.** `GetOrCreateAsync()` handles stampede protection, L1 (in-memory) + L2 (distributed) tiering, and tag-based invalidation. Package: `Microsoft.Extensions.Caching.Hybrid`.

```csharp
builder.Services.AddHybridCache();
```

```csharp
app.MapGet("/api/orders/{id}", async (string id, HybridCache cache, CancellationToken ct) =>
    await cache.GetOrCreateAsync(
        $"order:{id}",
        async token => await LoadOrderAsync(id, token),
        cancellationToken: ct));
```

One factory call per key under concurrency, and a distributed backend (Redis, Postgres) plugs in via the existing `IDistributedCache` registration without touching call sites. ([Hello HybridCache!](https://devblogs.microsoft.com/dotnet/hybrid-cache-is-now-ga/), [tiered caching with Postgres](https://devblogs.microsoft.com/dotnet/high-performance-distributed-caching-dotnet-postgres-azure/))

### Request validation

**Use minimal-API validation (`AddValidation()` + data annotations) for request validation** — invalid requests return 400 with problem details automatically, and the validation APIs live in `Microsoft.Extensions.Validation`, reusable outside HTTP.

```csharp
builder.Services.AddValidation();
```

```csharp
app.MapPost("/api/orders", (CreateOrder order) =>
    TypedResults.Created($"/api/orders/1", order));

record CreateOrder(
    [property: Required] string Sku,
    [property: Range(1, 100)] int Quantity);
```

Opt individual endpoints out with `.DisableValidation()` rather than opting the app in piecemeal. ([What's new in ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0))

### OpenAPI and API versioning

**Treat OpenAPI as a build artefact, not an afterthought.** OpenAPI 3.1 generation is in the box and on by default in the Web API template; set `<GenerateDocumentationFile>true</GenerateDocumentationFile>` so the source generator populates XML doc comments into the document (name your handlers — lambdas lose their comments). Version APIs with `Asp.Versioning` v10 (`Asp.Versioning.Http`, `Asp.Versioning.Mvc.ApiExplorer`, `Asp.Versioning.OpenApi`) and `WithDocumentPerVersion()` for one document per version:

```csharp
builder.Services.AddOpenApi();
builder.Services
    .AddApiVersioning()
    .AddApiExplorer(options => options.GroupNameFormat = "'v'VVV")
    .AddOpenApi(); // the Asp.Versioning.OpenApi overload

var app = builder.Build();

app.MapOpenApi().WithDocumentPerVersion(); // /openapi/v1.json, /openapi/v2.json, ...

var orders = app.NewVersionedApi("Orders");
var v1 = orders.MapGroup("/api/orders").HasApiVersion(1.0);
```

([API versioning with OpenAPI](https://devblogs.microsoft.com/dotnet/api-versioning-in-dotnet-10-applications/), [What's new in ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0))

### Typed clients

**Generate typed clients from your own OpenAPI document at build time (Kiota)** rather than hand-writing HttpClient wrappers — the contract stays versioned and the client cannot drift. ([Meziantou — Kiota client at build time](https://www.meziantou.net/generate-a-kiota-client-at-build-time-from-an-asp-net-core-openapi-file.htm))

### HTTP caching defaults

**APIs send `Cache-Control: no-cache, no-store, must-revalidate` by default**; opt individual endpoints into caching deliberately. Stale-data bugs and cache-poisoning surprises come from the opposite default. ([Meziantou — Disable HTTP caching by default](https://www.meziantou.net/disable-http-caching-by-default-in-asp-net-core-apis.htm))

### Server push

**Use Server-Sent Events (`TypedResults.ServerSentEvents`) for one-way server push** before reaching for SignalR/WebSockets. ([What's new in ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0))

### Authentication: passkeys first

**Use ASP.NET Core Identity's built-in passkey (WebAuthn/FIDO2) support for user sign-in instead of passwords or a third-party FIDO library.** Passkey management and login ship in Identity and the Blazor Web App template out of the box in .NET 10 — phishing-resistant, nothing server-side to leak, and no extra dependency to vet. Keep a second factor or recovery path for account recovery, but new apps should not be growing a password table in 2026. ([What's new in ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0), [Passkeys in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/authentication/passkeys/))

**Return 401/403 from API endpoints, never login redirects** — ASP.NET Core 10 avoids cookie redirects for known API endpoints; align custom auth handlers with that. ([What's new in ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0))

### Rate limiting

**Put named `Microsoft.AspNetCore.RateLimiting` policies on your public endpoints, rejecting with 429.** The default rejection status is 503 — always override it. Partition by a stable identity (authenticated user, API key tier), never by raw client-controlled input like spoofable IP headers. Choose fixed window as the default algorithm, concurrency limiter for expensive endpoints where in-flight work is the real constraint.

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromMinutes(1);
    });
});

var app = builder.Build();
app.UseRateLimiter();

app.MapGet("/api/orders", () => Results.Ok()).RequireRateLimiting("api");
```

This is a per-node fairness and abuse-control tool, not DDoS protection — that belongs at the edge (WAF/CDN). Load-test limits before shipping them. ([Rate limiting middleware in ASP.NET Core](https://learn.microsoft.com/aspnet/core/performance/rate-limit))

### Observability: propagate trace context across async boundaries

**When work crosses a process or time boundary — message queues, background jobs, outbox tables, scheduled work — capture the OpenTelemetry context explicitly and restore it on the consumer side; automatic propagation only covers synchronous HTTP.** Serialize `Activity.Current`'s context with `Propagators.DefaultTextMapPropagator` and store it alongside the message; on consumption, start the new activity with the extracted `ActivityContext` as parent for linear flows, or attach it as an `ActivityLink` when many messages fan into one operation. Do not propagate `Baggage` by default — it bloats payloads and leaks whatever anyone upstream stuffed into it. ([Meziantou — Propagating OpenTelemetry context in .NET](https://www.meziantou.net/propagating-opentelemetry-context-in-dotnet.htm))

### CSRF defence in depth

**Layer CSRF defence with Fetch Metadata headers (`Sec-Fetch-Site` and friends)** alongside token-based antiforgery; .NET 11 will automate this. ([Lock — Understanding the Fetch Metadata headers](https://andrewlock.net/understanding-the-fetch-metadata-http-headers-sec-fetch-site-and-friends/))

## Coming next (preview — not yet the opinion)

.NET 11 previews add automatic Fetch-Metadata-based CSRF protection (removing `UseAntiforgery()` for minimal APIs/Blazor SSR). ([Lock — .NET 11 preview 6](https://andrewlock.net/exploring-the-dotnet-11-preview-6-automatic-csrf-protection-based-on-fetch-metadata-http-headers/))
