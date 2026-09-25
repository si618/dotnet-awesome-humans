---
targets: [net10.0, csharp-14]
last-reviewed: 2026-09-25
last-used: 2026-09-25
sources:
  [
    dotnet-blog,
    aspnet-blog,
    ms-learn,
    meziantou,
    andrew-lock,
    milan-jovanovic,
    damien-bowden,
    khalid,
    code-with-mukesh,
  ]
---

# ASP.NET Core

Target ASP.NET Core 10. Minimal APIs are the default for new HTTP APIs.

## Opinions

### Hosting and deployment

**Run Kestrel directly, packaged as a container image built by `dotnet publish /t:PublishContainer` with no Dockerfile.** Kestrel as the edge server is a fully supported configuration; when you do sit behind a proxy or ingress (for port sharing, TLS termination, or load balancing), enable host filtering and forwarded-headers handling. That is the proxy's contract, not optional hardening. The SDK's container publish builds an OCI image from MSBuild properties (`ContainerRepository`, `ContainerRegistry`), keeps the base image patched with the SDK, and needs no daemon to produce a tarball for scanning pipelines. Hand-written Dockerfiles drift; use them only when you need custom image layers. ([Microsoft Learn: When to use a reverse proxy with Kestrel](https://learn.microsoft.com/aspnet/core/fundamentals/servers/kestrel/when-to-use-a-reverse-proxy), [Microsoft Learn: Containerize an app with dotnet publish](https://learn.microsoft.com/dotnet/core/containers/sdk-publish))

Never set `ContainerUser` to `root`: the Microsoft runtime images already run as the rootless `app` user on Linux ([Microsoft Learn: Containerize a .NET app reference](https://learn.microsoft.com/dotnet/core/containers/publish-configuration)), and undoing that means "if an attacker manages to compromise your container, they'll be able to do pretty much anything in the container" ([Lock: Updates to Docker images in .NET 8](https://andrewlock.net/exploring-the-dotnet-8-preview-updates-to-docker-images-in-dotnet-8/)). The base image is patched at build time only, so the image keeps the runtime it was built with until a rebuild replaces it, and [security.md: Servicing](security.md#servicing-take-the-monthly-patch) sets that cadence. And whichever layer terminates TLS owns redirection and HSTS: "If the proxy also handles HTTPS redirection, there's no need to use HTTPS redirection middleware", the same applies to the HSTS header, and `UseHsts` "isn't recommended in development because the HSTS settings are highly cacheable by browsers" ([Microsoft Learn: Enforce HTTPS in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/enforcing-ssl)).

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

One factory call per key under concurrency, and a distributed backend (Redis, Postgres) plugs in via the existing `IDistributedCache` registration without touching call sites. ([.NET Blog: Hello HybridCache! Streamlining Cache Management for ASP.NET Core Applications](https://devblogs.microsoft.com/dotnet/hybrid-cache-is-now-ga/), [.NET Blog: High-Performance Distributed Caching with .NET and Postgres on Azure](https://devblogs.microsoft.com/dotnet/high-performance-distributed-caching-dotnet-postgres-azure/))

### Request validation

**Use minimal-API validation (`AddValidation()` + data annotations) for request validation.** Invalid requests return 400 with problem details automatically, and the validation APIs live in `Microsoft.Extensions.Validation`, reusable outside HTTP.

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

Opt individual endpoints out with `.DisableValidation()` rather than opting the app in piecemeal. ([Microsoft Learn: What's new in ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0))

### OpenAPI and API versioning

**Generate the OpenAPI document at build time from Microsoft's own package.** OpenAPI 3.1 generation comes from Microsoft's own `Microsoft.AspNetCore.OpenApi`, which the Web API template references and calls for you; set `<GenerateDocumentationFile>true</GenerateDocumentationFile>` so the source generator populates XML doc comments into the document. Name your handlers, because lambdas lose their comments. Version APIs with `Asp.Versioning` v10 (`Asp.Versioning.Http`, `Asp.Versioning.Mvc.ApiExplorer`, `Asp.Versioning.OpenApi`) and `WithDocumentPerVersion()` for one document per version:

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

([.NET Blog: Combining API versioning with OpenAPI in .NET 10 applications](https://devblogs.microsoft.com/dotnet/api-versioning-in-dotnet-10-applications/), [Microsoft Learn: What's new in ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0))

### Typed clients

**Generate typed clients from your own OpenAPI document at build time (Kiota)** rather than hand-writing HttpClient wrappers. The contract stays versioned and the client cannot drift. ([Meziantou: Kiota client at build time](https://www.meziantou.net/generate-a-kiota-client-at-build-time-from-an-asp-net-core-openapi-file.htm))

### HTTP caching defaults

**APIs send `Cache-Control: no-cache, no-store, must-revalidate` by default**; opt individual endpoints into caching deliberately. Stale-data bugs and cache-poisoning surprises come from the opposite default. ([Meziantou: Disable HTTP caching by default](https://www.meziantou.net/disable-http-caching-by-default-in-asp-net-core-apis.htm))

### Server push

**Use Server-Sent Events (`TypedResults.ServerSentEvents`) for one-way server push.** ([Microsoft Learn: What's new in ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0))

### Authentication: passkeys first

**Use ASP.NET Core Identity's built-in passkey (WebAuthn/FIDO2) support for user sign-in instead of passwords or a third-party FIDO library.** Passkey management and login ship in Identity and the Blazor Web App template in .NET 10. They resist phishing, leave nothing server-side to leak, and add no dependency to vet. Keep a second factor or recovery path for account recovery, but new apps should not be growing a password table in 2026. ([Microsoft Learn: What's new in ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0), [Microsoft Learn: Passkeys in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/authentication/passkeys/))

**Return 401/403 from API endpoints, never login redirects:** ASP.NET Core 10 avoids cookie redirects for known API endpoints; align custom auth handlers with that. ([Microsoft Learn: What's new in ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0))

**Review by 2027-03-31.** A passkey sign-in currently describes itself wrongly: the `amr` claim is not set to `pop`, the RFC 8176 value for proof-of-possession, and that "is not implemented correctly in ASP.NET Core (.NET 10)" ([Bowden: Set the amr claim when using passkeys authentication in ASP.NET Core](https://damienbod.com/2026/01/05/set-the-amr-claim-when-using-passkeys-authentication-in-asp-net-core/)). It is filed as [dotnet/aspnetcore#64881](https://github.com/dotnet/aspnetcore/issues/64881) and still open. Authorization that reads `amr` to decide whether a sign-in met a step-up or multi-factor bar will therefore misjudge a passkey, so either take the credential type from your own sign-in path or re-issue the claim, which means signing out and back in through `SignInWithClaimsAsync()` because the existing claims collection cannot be mutated in place. Drop this note once the issue closes in a shipped release.

### Authorization by default

**Set a fallback policy requiring an authenticated user, and let endpoints opt out of it.** "Having authorization required by default is more secure than relying on new controllers and Razor Pages to include the `[Authorize]` attribute" ([Microsoft Learn: Create an ASP.NET Core web app with user data protected by authorization](https://learn.microsoft.com/aspnet/core/security/authorization/secure-data)). A new endpoint is then private until someone says otherwise, the same default as request validation above.

```csharp
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());
```

Public endpoints then carry `[AllowAnonymous]`, or `.AllowAnonymous()` on a minimal-API endpoint or group, and the ones that need more name a stricter policy.

### Browser clients: cookies, with a BFF holding the tokens

**A browser app authenticates with a cookie, and a SPA or Blazor WebAssembly client reaches its APIs through a backend for frontend that keeps the tokens server-side.** "We recommend using cookies for browser-based applications, because, by default, the browser automatically handles them without exposing them to JavaScript", with tokens reserved for clients that cannot use cookies ([Microsoft Learn: Use Identity to secure a Web API backend for SPAs](https://learn.microsoft.com/aspnet/core/security/authentication/identity-api-authorization)). The reason is the blast radius of one XSS bug: a token in `localStorage` is readable by any injected script ([Jovanović: JWT Authentication in ASP.NET Core](https://milanjovanovic.tech/blog/jwt-authentication-aspnetcore)), whereas "`HttpOnly` cookies cannot be read by JavaScript, so even a successful XSS attack cannot exfiltrate the user's access or refresh token" ([Abuhakmeh: The Cookie Apocalypse Already Happened](https://duendesoftware.com/blog/20260414-the-cookie-apocalypse-already-happened)). Bowden puts the client-side half plainly: "No authentication security logic should be implemented in a client application running in the browser" ([Bowden: Implement BFF using Auth0, Angular and ASP.NET Core](https://damienbod.com/2026/08/10/implement-bff-using-auth0-angular-and-asp-net-core/)).

The arrangement is three parts. The session rides in a `__Host-` prefixed cookie marked `HttpOnly` and `Secure`, with `SameSite=Strict` where the flow allows it, since such cookies "are not sent on cross-site requests" (Abuhakmeh, above). That is the cookie's contribution to the CSRF defence below rather than a replacement for it. A reverse proxy puts the front end and the API on one origin. And the BFF gets its own token for the downstream call rather than forwarding what the browser holds, because "The downstream API does not accept the user delegated access tokens from the UI application" ([Bowden: BFF secured ASP.NET Core application using downstream API and an OAuth client credentials JWT](https://damienbod.com/2024/04/08/bff-secured-asp-net-core-application-using-downstream-api-and-an-oauth-client-credentials-jwt/)). Blazor's documented BFF pattern is the same shape, proxying API calls "with the `access_token` stored in the authentication cookie" ([Microsoft Learn: Secure an ASP.NET Core Blazor Web App with OpenID Connect (OIDC)](https://learn.microsoft.com/aspnet/core/blazor/security/blazor-web-app-with-oidc?pivots=bff-pattern)).

Keep the cookie a key into server state rather than a container for it. Browsers cap a cookie at about 4 KB, and "The cleanest fix is to stop storing session data in the cookie entirely... The cookie becomes a thin key that points to the server state rather than containing that state itself" ([Abuhakmeh: ASP.NET Core Cookie Size Limits in Production](https://duendesoftware.com/blog/20260429-aspnet-core-cookie-size-limits)).

### Tokens for APIs

**Take tokens from an identity provider, validate issuer, audience, lifetime and signing key, and keep permissions out of the token.** Hand identity to "a battle-tested identity provider" ([Jovanović: Integrate Keycloak with ASP.NET Core Using OAuth 2.0](https://milanjovanovic.tech/blog/integrate-keycloak-with-aspnetcore-using-oauth-2)) rather than issuing your own JWTs, which puts your service in the key-management and revocation business. Resolve permissions server-side with `IClaimsTransformation` and keep tokens lean ([Jovanović: Building Secure APIs with Role-Based Access Control in ASP.NET Core](https://milanjovanovic.tech/blog/building-secure-apis-with-role-based-access-control-in-aspnetcore)), because a permission baked into a token outlives its own revocation: "A revoked permission keeps working until the token expires, which I reproduced as a 201 that should have been a 403" ([Mukesh: Permission-Based Authorization in ASP.NET Core](https://codewithmukesh.com/blog/permission-based-authorization-in-aspnet-core/)). Keep access-token lifetimes short for the same reason, since a JWT cannot be revoked once issued ([Mukesh: JWT Authentication in ASP.NET Core](https://codewithmukesh.com/blog/jwt-authentication-in-aspnet-core/)), and rotate refresh tokens on every use, issuing a new one and revoking the old (Jovanović, above).

**Bind the token to the client, and replace the client secret with a key.** Whoever holds a bearer token can spend it, so bind it: OAuth DPoP ties the token to a key the client proves it holds, and "The access tokens should only be used for what the access tokens are intended for. OAuth DPoP helps force this." ([Bowden: Securing APIs using ASP.NET Core and OAuth 2.0 DPoP](https://damienbod.com/2023/08/14/securing-apis-using-asp-net-core-and-oauth-2-0-dpop/)) Move the authorization request parameters out of the browser redirect with pushed authorization requests, configured `PushedAuthorizationBehavior.Require` and available since .NET 9 ([Bowden: Implement ASP.NET Core OpenID Connect OAuth PAR client with Keycloak using .NET Aspire](https://damienbod.com/2024/09/02/implement-asp-net-core-openid-connect-oauth-par-client-with-keycloak-using-net-aspire/)). And authenticate a confidential client with a private-key JWT assertion instead of a shared secret, which removes the one long-lived credential such a client would otherwise keep in configuration ([Bowden: Use client assertions in ASP.NET Core using OpenID Connect, OAuth DPoP and OAuth PAR](https://damienbod.com/2026/02/02/use-client-assertions-in-asp-net-core-using-openid-connect-oauth-dpop-and-oauth-par/)). All three need provider support, so check what yours implements before designing around them.

### The Data Protection key ring outlives the container

**Where there is more than one instance, or a container that restarts, persist the key ring to shared storage and protect it at rest, and configure both explicitly.** The ring encrypts authentication cookies and antiforgery tokens, so a ring that does not survive a restart signs everyone out, and one that is not shared means two replicas cannot read each other's cookies. In a container the keys belong in "A folder that's a Docker volume that persists beyond the container's lifetime... or An external provider, such as Azure Blob Storage... or Redis" ([Microsoft Learn: Configure ASP.NET Core Data Protection](https://learn.microsoft.com/aspnet/core/security/data-protection/configuration/overview)), which is what the container-first hosting opinion above leads into.

```csharp
// Azure.Extensions.AspNetCore.DataProtection.Blobs and .Keys; both extension
// methods live in the Microsoft.AspNetCore.DataProtection namespace.
builder.Services.AddDataProtection()
    .PersistKeysToAzureBlobStorage(blobUri, credential)
    .ProtectKeysWithAzureKeyVault(keyIdentifier, credential);
```

Both calls, because naming a location turns the default protection off: "If you specify an explicit key persistence location, the data protection system deregisters the default key encryption at rest mechanism, so keys are no longer encrypted at rest." Redis needs one more check, since it "doesn't persist data by default when restarting", which hands the app new keys and invalidates everything the old ones protected ([Microsoft Learn: Key storage providers in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/data-protection/implementation/key-storage-providers)). Encryption at rest is not access control either, as it "doesn't prevent cyberattackers from creating new keys", so the store's permissions carry as much weight as its cipher. Sharing one ring between applications behind a single proxy is a deliberate configuration rather than something to arrive at by accident ([Abuhakmeh: Data Protection for ASP.NET Core Developers](https://duendesoftware.com/blog/20250313-data-protection-aspnetcore-duende-identityserver)).

### Request timeouts

**Give every endpoint a deadline, and flow the cancellation token into the work it starts.** ASP.NET Core applies no application timeout of its own, so a stalled query or dependency keeps consuming resources long after the response stops being useful, and whatever sits in front of you ends up owning both the deadline and the response body. `AddRequestTimeouts` only registers the services; the limit comes from a named policy or `WithRequestTimeout` on the endpoint. The middleware is cooperative, which is the part that catches people out: it cancels `HttpContext.RequestAborted` and then waits. A handler that never passes the token on runs to completion and returns 200, and no 504 appears, because nothing threw. So a 504 tells you cancellation reached the middleware, not that the database stopped working. Give reads and exports separate policies rather than one global number, and opt streaming responses out with `DisableRequestTimeout()`, since a response that has already begun cannot be replaced with a clean 504. ([Microsoft Learn: Request timeouts middleware](https://learn.microsoft.com/aspnet/core/performance/timeouts), [Jovanović: Your ASP.NET Core endpoints don't have a timeout](https://www.milanjovanovic.tech/blog/your-aspnetcore-endpoints-dont-have-a-timeout))

```csharp
builder.Services.AddRequestTimeouts(options =>
{
    options.AddPolicy("api-read", TimeSpan.FromSeconds(3));
    options.AddPolicy("report-export", TimeSpan.FromSeconds(30));
});

app.UseRequestTimeouts();

// The token reaches EF Core, so the timeout actually stops the query.
app.MapGet("/orders/{id:guid}", async (Guid id, AppDbContext db, CancellationToken cancellationToken) =>
        await db.Orders.AsNoTracking().SingleOrDefaultAsync(o => o.Id == id, cancellationToken))
    .WithRequestTimeout("api-read");

app.MapGet("/events", StreamEvents).DisableRequestTimeout();
```

Timeouts do not fire while a debugger is attached, so verify this one without.

### Rate limiting

**Put named `Microsoft.AspNetCore.RateLimiting` policies on your public endpoints, rejecting with 429.** The default rejection status is 503, so always override it. ([Microsoft Learn: RateLimiterOptions.RejectionStatusCode](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.ratelimiting.ratelimiteroptions.rejectionstatuscode)) Partition by a stable identity (authenticated user, API key tier), never by raw client-controlled input like spoofable IP headers. `HttpContext.Connection.RemoteIpAddress` is not client-controlled and a forwarded header is, and the forwarded-headers handling the hosting contract above requires is what decides which of the two that property reports, so partitioning anonymous traffic by the connection address is sound while partitioning it by `X-Forwarded-For` is not. Bound the number of partitions as well, because every distinct key allocates its own limiter ([Mukesh: Rate Limiting in ASP.NET Core (.NET 10)](https://codewithmukesh.com/blog/rate-limiting-aspnet-core/)). Use the concurrency limiter for expensive endpoints where in-flight work is the real constraint.

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

This is a per-node fairness and abuse-control tool, not DDoS protection, which belongs at the edge in a WAF or CDN. Load-test limits before shipping them. ([Microsoft Learn: Rate limiting middleware in ASP.NET Core](https://learn.microsoft.com/aspnet/core/performance/rate-limit))

### Observability: propagate trace context across async boundaries

**When work crosses a process or time boundary, capture the OpenTelemetry context explicitly and restore it on the consumer side; automatic propagation only covers synchronous HTTP.** Message queues, background jobs, outbox tables and scheduled work all cross one. Serialize `Activity.Current`'s context with `Propagators.DefaultTextMapPropagator` and store it alongside the message; on consumption, start the new activity with the extracted `ActivityContext` as parent for linear flows, or attach it as an `ActivityLink` when many messages fan into one operation. Do not propagate `Baggage` by default: it bloats payloads and leaks whatever anyone upstream stuffed into it. ([Meziantou: Propagating OpenTelemetry context in .NET](https://www.meziantou.net/propagating-opentelemetry-context-in-dotnet.htm))

### CSRF defence in depth

**Layer CSRF defence with Fetch Metadata headers (`Sec-Fetch-Site` and friends)** alongside token-based antiforgery; .NET 11 will automate this. ([Lock: Understanding the Fetch Metadata headers](https://andrewlock.net/understanding-the-fetch-metadata-http-headers-sec-fetch-site-and-friends/))

**A minimal API that binds a form needs antiforgery validation in the pipeline.** Since ASP.NET Core 8, "Minimal API endpoints that bind a parameter from the form via IFormFile or IFormFileCollection require anti-forgery validation" ([Microsoft Learn: Minimal APIs antiforgery checks](https://learn.microsoft.com/aspnet/core/breaking-changes/8/antiforgery-checks)), so an upload endpoint that worked on .NET 7 returns 400 until `UseAntiforgery()` is registered and the caller sends the token.

### CORS

**Name every policy, and never pair a wildcard origin with credentials.** "Specifying AllowAnyOrigin and AllowCredentials is an insecure configuration and can result in cross-site request forgery", and so is "Bypassing the built-in checks by using SetIsOriginAllowed(\_ => true) together with AllowCredentials" ([Microsoft Learn: Enable Cross-Origin Requests (CORS) in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/cors)). Apply named policies per endpoint or group with `[EnableCors]` instead of one permissive default: a policy widened for a single endpoint otherwise widens what the whole application accepts, and in .NET 11 the allowed-origins list also decides which cross-origin requests the CSRF middleware admits (see [Coming next](#coming-next-preview-not-yet-the-opinion)).

### Security headers and output encoding

**Send a header baseline with `NetEscapades.AspNetCore.SecurityHeaders`, and give the CSP a nonce rather than `unsafe-inline`.** The baseline: `X-Frame-Options` deny, `X-Content-Type-Options` nosniff, `Referrer-Policy` strict-origin-when-cross-origin, the three `Cross-Origin-*` policies, `Permissions-Policy`, and HSTS outside development ([Bowden: Implement a secure Blazor Web application using OpenID Connect and security headers](https://damienbod.com/2024/04/15/implement-a-secure-blazor-web-application-using-openid-connect-and-security-headers/)). For an interactive Blazor circuit the nonce is the approach to prefer, with hashes as the fallback ([Bowden: Revisiting using a Content Security Policy (CSP) nonce in Blazor](https://damienbod.com/2025/05/26/revisiting-using-a-content-security-policy-csp-nonce-in-blazor/)).

Razor already encodes output "sourced from variables, unless you work to prevent this behavior", and `HtmlString` "should never be used in combination with untrusted input because it exposes an XSS vulnerability" ([Microsoft Learn: Prevent Cross-Site Scripting (XSS) in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/cross-site-scripting)). Blazor Web Apps send `frame-ancestors 'self'` on their own, and on .NET 8 to 10 a policy without `unsafe-inline` styles breaks the `Virtualize` component, which .NET 11 fixes ([Microsoft Learn: Enforce a Content Security Policy for ASP.NET Core Blazor](https://learn.microsoft.com/aspnet/core/blazor/security/content-security-policy)). Where client-side script writes to the DOM, Trusted Types moves the check into the browser, so "APIs like `innerHTML` must be passed a `TrustedHTML` object" ([Lock: Preventing client-side cross-site-scripting vulnerabilities with Trusted Types](https://andrewlock.net/preventing-client-side-cross-site-scripting-vulnerabilities-with-trusted-types/)).

### Open redirects

**Never redirect to a URL taken from the request without checking that it is local.** `RedirectHttpResult.IsLocalUrl` works outside MVC on .NET 10, and `LocalRedirect` "throws an exception if a non-local URL is specified" ([Microsoft Learn: Prevent open redirect attacks in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/preventing-open-redirects)).

```csharp
app.MapGet("/redirect", (string url) =>
    RedirectHttpResult.IsLocalUrl(url)
        ? Results.LocalRedirect(url)
        : Results.BadRequest());
```

### Localization

**Register `AddLocalization` and run `UseRequestLocalization` early, driving culture from a cookie rather than `Accept-Language`.** The browser header reflects the user's OS, not a choice they made. Provider order, the `SupportedCultures`/`SupportedUICultures` split, and resource-key policy are in [globalization.md](globalization.md).

## Coming next (preview, not yet the opinion)

.NET 11 previews add automatic Fetch-Metadata-based CSRF protection (removing `UseAntiforgery()` for minimal APIs/Blazor SSR), on by default for apps built with `WebApplicationBuilder`. MVC and Razor Pages keep token validation either way: "There's basically no simple way today to opt into _only_ using the new approach." Cross-origin requests are rejected unless their origin is in the CORS allowed-origins list, and requests carrying no browser signals pass through ([Lock: Automatic CSRF protection based on Fetch Metadata headers](https://andrewlock.net/exploring-the-dotnet-11-preview-6-automatic-csrf-protection-based-on-fetch-metadata-http-headers/)).

That rejection lands on one identity flow in particular. A SPA that exchanges an authorization code at the token endpoint is making a cross-origin POST: "It's cross-origin. It's a POST. It will be rejected." Whether a provider callback using `response_mode=form_post` counts as legitimate is not settled yet ([Abuhakmeh: Understanding .NET 11 Automatic CSRF Protection](https://duendesoftware.com/blog/20260901-understanding-dotnet-11-automatic-csrf-protection)). A same-origin BFF, the arrangement recommended above, does not make that request from the browser at all.
