---
targets: [net10.0, csharp-14]
last-reviewed: 2026-09-11
sources:
  [
    ms-learn,
    dotnet-blog,
    andrew-lock,
    meziantou,
    steve-gordon,
    milan-jovanovic,
    code-with-mukesh,
    khalid,
  ]
---

# Security

Research into what this repository should say about security on .NET 10. Security has no opinion file of its own. It lives in two places: [aspnet-core.md](../opinions/aspnet-core.md) covers passkeys, API challenge responses, rate limiting, CSRF and the reverse-proxy contract, and [ci.md](../opinions/ci.md) covers SHA pinning, script injection, token permissions, pinned builds, signing and Renovate. This topic checks both against the roster and maps the ground between them.

**The existing coverage holds up, the gaps are larger than the coverage, and the templates carry one consequence that nobody chose.** Three findings lead, because each touches something the repository already ships:

1. **The house warnings rule now fails restore on every new advisory.** For `net10.0` projects NuGet Audit checks transitive packages by default, and [templates/Directory.Build.props](../templates/Directory.Build.props) sets `TreatWarningsAsErrors` unconditionally. Reproduced on SDK 10.0.400 against a fresh project: `error NU1903: Warning As Error: Package 'Newtonsoft.Json' 12.0.1 has a known high severity vulnerability`, with restore exiting 1. An advisory published against anything in the graph turns a green commit red with no change to the repository. The recommendation is to keep that behaviour and write it down ([NuGet Audit under the house warnings rule](#nuget-audit-under-the-house-warnings-rule)).
2. **ci.md's publishing advice has been overtaken.** It offers "trusted publishing / scoped API key stored as a repository secret" as equals. Trusted publishing has been GA since 2025-09-22, and from 2026-08-17 nuget.org caps new API keys at 30 days, with every older key expiring on 2026-11-01. The opinion should name trusted publishing and demote the key to a fallback ([Publishing credentials](#publishing-credentials)).
3. **The container-first hosting opinion walks into the Data Protection key ring.** aspnet-core.md says to ship a container image and says nothing about where the keys that protect authentication cookies and antiforgery tokens live. `ms-learn` says a container must keep them on a persistent volume or in an external store, and documents a trap: choosing an explicit location silently turns off encryption at rest ([Data Protection in containers](#data-protection-in-containers)). Of the application-security gaps, this is the one the repository's own advice leads straight into.

Everything else is a gap rather than a defect. Sections 2 to 5 map it, with the sources that could carry an opinion and the places where none can yet.

## Sources swept

| id                 | Tier                          | Used for                                                                                                                                                                                                     |
| ------------------ | ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `ms-learn`         | 1                             | ASP.NET Core security pages, NuGet auditing and source mapping, cryptography, containers, servicing. Review dates are each page's own `ms.date`, listed under [Freshness](#freshness)                        |
| `dotnet-blog`      | 1                             | .NET 10 post-quantum status, NuGet supply-chain posts, BinaryFormatter removal, support lifecycle, .NET 11 previews. The NuGet blog now redirects into this blog's NuGet category, so none of it is unvetted |
| `andrew-lock`      | 1                             | CSRF and Fetch Metadata, CVE-2025-55315, trusted publishing, SBOM and provenance attestations, Trusted Types                                                                                                 |
| `meziantou`        | 1                             | NuGet auditing, source mapping, lock files, deserialization. Most of the security catalogue is 2019–2020 and predates .NET 8                                                                                 |
| `steve-gordon`     | 1                             | AES-GCM nonce handling (2026). The rest of his security tag is from 2016                                                                                                                                     |
| `milan-jovanovic`  | 1, conflict of interest noted | Token storage, JWT validation, permissions, identity providers                                                                                                                                               |
| `code-with-mukesh` | 1, **Corroborate.**           | Token lifetime, API keys, rate limiting across replicas, permission revocation                                                                                                                               |
| `khalid`           | 1, pre-2025 posts only        | Sharing Data Protection keys across apps (2022)                                                                                                                                                              |
| `aspnet-blog`      | 1                             | Swept with `dotnet-blog`. No BFF or Data Protection post in 2024–2026 (negative result)                                                                                                                      |
| `jetbrains-dotnet` | 1                             | Discovery only: the September 2026 roundup linked out to Lock, Khalid and Damien Bowden. No original security writing (negative result)                                                                      |
| `jon-skeet`        | 1                             | Nothing on this ground (negative result)                                                                                                                                                                     |
| `chris-sainty`     | Watch list                    | Blazor auth series, 2018–2023. Nothing new, not cited                                                                                                                                                        |

Four unvetted sources carry claims below, each flagged **unvetted** where it appears: Barry Dorrans ([idunno.org](https://idunno.org/)), Damien Bowden ([damienbod.com](https://damienbod.com/)), Duende Software's [blog](https://duendesoftware.com/blog) and documentation, and the [OWASP Cheat Sheet Series](https://cheatsheetseries.owasp.org/). Scott Brady and Philippe De Ryck were checked and are unusable here; item 7 at the end gives the reasons.

## 1. What the repository already says, checked

**Passkeys first** holds, and nothing swept argues against it. One unvetted claim is worth carrying. Damien Bowden reports that a passkey sign-in on .NET 10 does not set the `amr` claim to `pop`, and that "At present, this is not implemented correctly in ASP.NET Core (.NET 10)." ([Bowden: Set the amr claim when using passkeys authentication in ASP.NET Core](https://damienbod.com/2026/01/05/set-the-amr-claim-when-using-passkeys-authentication-in-asp-net-core/), 2026-01-05, **unvetted**). If it holds, anything downstream that reads `amr` to decide whether a sign-in met a step-up or MFA bar will misjudge a passkey. No roster source mentions it and the editor has not reproduced it.

**Rate limiting** holds, and `code-with-mukesh` corroborates the per-node framing from the other direction: "If your deployment has more than one replica, your in-memory rate limiter is not enforcing what you think" ([Mukesh: Rate Limiting in ASP.NET Core (.NET 10)](https://codewithmukesh.com/blog/rate-limiting-aspnet-core/), 2026-05-21). He adds that partition keys "must be validated and bounded", because every distinct key allocates a limiter. `milan-jovanovic`'s older advice that "Rate limiting by IP address can be a good layer of security for unauthenticated users" ([Jovanović: Advanced Rate Limiting Use Cases In .NET](https://milanjovanovic.tech/blog/advanced-rate-limiting-use-cases-in-dotnet), 2023-08-19) reads against the opinion's "never by raw client-controlled input like spoofable IP headers", but the two are compatible. On the editor's reading, the connection's remote address is not client-controlled while a forwarded header is, and the forwarded-headers middleware that the hosting opinion requires is what decides which of them `RemoteIpAddress` reports. The opinion would be clearer for naming that.

**CSRF defence in depth, and its "Coming next" aside,** are accurate, and Lock's follow-up sharpens the aside. The .NET 11 middleware is on by default for apps built with `WebApplicationBuilder`. Minimal APIs and Blazor SSR can drop `UseAntiforgery()` and rely on it alone, whereas MVC and Razor Pages keep token validation: "There's basically no simple way today to opt into _only_ using the new approach." Cross-origin requests are rejected unless their origin is in the CORS allowed-origins list, and requests with no browser signals are allowed through. ([Lock: Automatic CSRF protection based on Fetch Metadata headers](https://andrewlock.net/exploring-the-dotnet-11-preview-6-automatic-csrf-protection-based-on-fetch-metadata-http-headers/), 2026-08-04). .NET 11 reached RC 1 on 2026-09-08 with GA expected in November 2026 ([Microsoft Learn: What's new in .NET 11](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/overview), reviewed 2026-09-08), so the aside stays preview-labelled for now. In .NET 11 the CORS policy therefore becomes part of the CSRF configuration, which [CORS](#cors) picks up.

**A .NET 8 rule belongs beside it and is missing.** Since ASP.NET Core 8, "Minimal API endpoints that bind a parameter from the form via IFormFile or IFormFileCollection require anti-forgery validation" ([Microsoft Learn: Minimal APIs antiforgery checks](https://learn.microsoft.com/aspnet/core/breaking-changes/8/antiforgery-checks), reviewed 2023-12-05).

**The hosting opinion credits SDK container publish with keeping "the base image patched with the SDK".** That is true at build time only. An image nobody rebuilds keeps the runtime it was built with; [Servicing](#3-servicing) takes up the cadence.

**ci.md's pinning, injection and permissions opinions** drew no new evidence either way. Its signing section is the second finding above.

## 2. Supply chain

### NuGet Audit under the house warnings rule

The mechanism, from `ms-learn`: audit runs during restore and is on by default, and "`NuGetAuditMode` defaults to `all` when a project targets `net10.0` or higher. Otherwise `NuGetAuditMode` defaults to `direct`." A multi-targeted project uses `all` if any of its frameworks selects it ([Microsoft Learn: Auditing package dependencies for security vulnerabilities](https://learn.microsoft.com/nuget/concepts/auditing-packages), reviewed 2025-10-01). Severities map to NU1901 (low) through NU1904 (critical). A .NET 9 preview briefly shipped `all` and the 9.0.101 SDK reverted it ([Microsoft Learn: NuGetAudit audits transitive packages](https://learn.microsoft.com/dotnet/core/compatibility/sdk/10.0/nugetaudit-transitive-packages), reviewed 2025-03-28).

`dotnet-blog` puts a number on the noise. .NET 10 also prunes platform-provided packages from the graph before auditing it, and "Projects with these defaults have 70% fewer transitive vulnerability reports compared to projects using the previous defaults" ([Kolev: NuGet Package Pruning: Cleaner Dependencies and Actionable Vulnerability Reports](https://devblogs.microsoft.com/dotnet/nuget-package-pruning-in-dotnet-10/), 2026-05-18). `meziantou` covered switching audit on while it was opt-in ([Meziantou: Enable NuGet auditing for your .NET projects](https://www.meziantou.net/enable-nuget-auditing-for-your-dotnet-projects.htm), 2024-07-15), which .NET 10 has made moot.

The interaction with this repository is documented rather than inferred: "If these warnings are causing restore to fail because you are using `TreatWarningsAsErrors`, you can add `<WarningsNotAsErrors>NU1901;NU1902;NU1903;NU1904</WarningsNotAsErrors>` to allow these codes to remain as warnings." ([Microsoft Learn: NuGet Warnings NU1901, NU1902, NU1903, NU1904](https://learn.microsoft.com/nuget/reference/errors-and-warnings/nu1901-nu1904), reviewed 2024-07-19). The reproduction above confirms it on the SDK this repository pins.

**Recommendation: keep NU1901–NU1904 as errors, and say so in the template.** The **House:** warnings rule exists so that a problem fails where it is introduced, and a known vulnerability in the shipped graph is such a problem. The escape valve already has the shape the house rule demands of every suppression: `NuGetAuditSuppress` takes an advisory URL ([Microsoft Learn: Excluding advisories](https://learn.microsoft.com/nuget/concepts/auditing-packages#excluding-advisories)), and an entry carrying its reason in a comment is what the rule already requires of `#pragma` and `<NoWarn>`. Two consequences should be written down with it, because they surprise people:

- A release branch that has not changed will stop restoring on the day an advisory lands against its graph. That is intended, but it means servicing a release branch starts with triage.
- `WarningsNotAsErrors` is the one-line way out, and it switches the house rule off for exactly the class of warning where failing loudly matters most.

`ms-learn` offers one more option, and the house rule rejects it outright: "If you would like to run NuGet Audit on developer machines, but disable it on CI pipelines, you can take advantage of MSBuild importing environment variables, and create a NuGetAudit environment variable set to `false` in your pipeline definition." That inverts ci.md's own argument that a check which fires in only one place fires a day late.

### Publishing credentials

Two `dotnet-blog` posts overtake ci.md's "trusted publishing / scoped API key stored as a repository secret":

- **Trusted publishing is GA.** A workflow uses "a short-lived GitHub OIDC token to request a temporary, single-use NuGet API key", and "These keys expire quickly (≈1 hour)" ([Tvorun and Iyer: New Trusted Publishing enhances security on NuGet.org](https://devblogs.microsoft.com/dotnet/enhanced-security-is-here-with-the-new-trust-publishing-on-nuget-org/), 2025-09-22).
- **Long-lived keys are going away.** "Starting August 17, 2026, new API keys will be limited to 30 days. All existing API keys created before that date will expire on November 1, 2026." The post strongly recommends moving to trusted publishing ([.NET Team: Strengthening NuGet Supply Chain Security: Reducing API Key Lifetime](https://devblogs.microsoft.com/dotnet/strengthening-nuget-supply-chain-security-reducing-api-key-lifetime/), 2026-08-03).

`andrew-lock` walks through the setup and states the reason plainly: "managing the lifecycle of long-lived secrets is notoriously difficult" ([Lock: Publishing NuGet packages from GitHub actions the easy way with Trusted Publishing](https://andrewlock.net/easily-publishing-nuget-packages-from-github-actions-with-trusted-publishing/), 2025-09-30). The `ms-learn` page carries two review dates, 2025-07-01 and 2026-07-29 ([Microsoft Learn: Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)).

**Recommendation: trusted publishing is the opinion.** An API key is the fallback for a host that cannot present an OIDC token, and it now needs an owner for rotation because it dies in 30 days. The ci.md sentence cites `meziantou` and `ms-learn` today; a rewrite would cite `dotnet-blog` and `andrew-lock`, so the file's `sources:` grows.

### Package source mapping and lock files

**Source mapping closes dependency confusion.** `meziantou` describes the attack: "If you have a package published on an internal feed... with the same name as a public package, the public package can be used instead." His remedy is "You can use package source mapping to prevent this issue." ([Meziantou: NuGet Packages: security risks and best practices](https://www.meziantou.net/nuget-packages-security-risks-and-best-practices.htm), 2024-12-02). `ms-learn` agrees and scopes it: mapping "can be used to improve your supply chain security, especially if you use a mix of public and private package sources", set up in one root `nuget.config` with `<clear/>` and a pattern covering every package ([Microsoft Learn: Package Source Mapping](https://learn.microsoft.com/nuget/consume-packages/package-source-mapping), reviewed 2026-07-16). It also names a limit: mapping filters restore, but not `dotnet package add` or the `--vulnerable` listing. The template set has no `nuget.config`, which is where this would land.

**Lock files are where the sources frame one feature three ways.** The template's comment presents `packages.lock.json` as optional protection "against feed drift". `ms-learn` presents lock files as repeatability for CI. `meziantou` presents them as integrity: "The lock file contains the package hash. If a corrupted or malicious package is downloaded, NuGet detects it and fails." ([Meziantou: Faster and Safer NuGet restore using Source Mapping and Lock files](https://www.meziantou.net/faster-and-safer-nuget-restore-using-source-mapping-and-lock-files.htm), 2022-08-01). His is a claim about mechanism and it is correct: CPM pins a version, and only the lock file pins content.

What the hash buys depends on the feeds. The editor's reading, not a sourced claim: a version published to nuget.org cannot be overwritten, so on nuget.org alone the hash adds little, whereas behind a mirror or a private feed it is the only check on content. That makes source mapping and lock files one decision for multi-feed repositories, and not a pressing one for single-feed repositories.

### SBOM and provenance

`andrew-lock` is the only roster source, across a three-post series: generating an SBOM for a NuGet package ([Lock: Creating a software bill of materials (SBOM) for an open-source NuGet package](https://andrewlock.net/creating-a-software-bill-of-materials-sbom-for-an-open-source-nuget-package/), 2025-03-25), provenance attestations ([Lock: Creating provenance attestations for NuGet packages in GitHub Actions](https://andrewlock.net/creating-provenance-attestations-for-nuget-packages-in-github-actions/), 2025-03-18), and signed SBOM attestations with `actions/attest-sbom` ([Lock: Creating SBOM attestations in GitHub Actions](https://andrewlock.net/creating-sbom-attestations-in-github-actions/), 2025-04-01). `ms-learn` has no page on SBOM generation for .NET builds (negative result). Barry Dorrans covers SBOMs in NuGet packages and package signing in GitHub Actions from inside Microsoft (December 2025, **unvetted**).

It fits ci.md's signing section, which already argues for provenance over author-signing ceremony. One Tier 1 author is thin for an opinion, though, and nothing swept says who consumes the SBOM.

### An SDK that reports its own vulnerabilities, documented but not yet shipped here

`ms-learn` documents a property that would pair with the pinned `global.json` the way Renovate pairs with pinned action SHAs: "To detect builds running on an unsupported SDK, set the `CheckSdkVulnerabilities` MSBuild property to `true` in your project. The build then emits warning NETSDK1239 when the resolved .NET SDK is end of life." ([Microsoft Learn: .NET releases, patches, and support](https://learn.microsoft.com/dotnet/core/releases-and-support), reviewed 2026-05-15). A search summary reports that the same property also enables NETSDK1238, for an SDK with known vulnerabilities, and NETSDK1240, for a discontinued feature band; the editor has not read that on a Learn page.

**It is not in the SDK this repository pins.** The installed 10.0.400 SDK contains no reference to `CheckSdkVulnerabilities` or to any of the three codes, and the Learn page does not say which feature band introduces it. Nothing to act on until it ships.

### Security analyzers

`ms-learn` documents `AnalysisModeSecurity`, which sets the analysis mode for the Security category alone and otherwise inherits `AnalysisMode` ([Microsoft Learn: MSBuild reference for .NET SDK projects](https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props), reviewed 2026-03-27). The template's `latest-recommended` level turns some security rules on, but the documentation does not list which, and points to the SDK's `analysislevel_*_recommended.globalconfig` instead. No source swept recommends a setting, so this is mechanism, not an opinion in waiting.

## 3. Servicing

Security fixes ship on Patch Tuesday, "always the second Tuesday of the month", and `ms-learn` says to "Regularly install servicing updates to ensure that your apps are in a secure and supported state" ([Microsoft Learn: .NET releases, patches, and support](https://learn.microsoft.com/dotnet/core/releases-and-support), reviewed 2026-05-15). After STS support was lengthened to 24 months, ".NET 8 and .NET 9 will reach end of support on the same day – November 10, 2026" ([Damkewala: .NET STS releases supported for 24 months](https://devblogs.microsoft.com/dotnet/dotnet-sts-releases-supported-for-24-months/), 2025-09-16). The repository already targets `net10.0`, supported to November 2028, so the date matters to readers bringing older projects in and to [verify-project](../skills/verify-project/SKILL.md), for which a target framework below `net10.0` is out of support in two months.

`andrew-lock`'s write-up of CVE-2025-55315 is the argument for cadence. It is a request-smuggling flaw in ASP.NET Core that "received Microsoft's highest-ever CVSS score of 9.9", and exploiting it could let an attacker "Login as a different user", "Make an internal request (SSRF)" or "Bypass CSRF checks" ([Lock: Understanding the worst .NET vulnerability ever: request smuggling and CVE-2025-55315](https://andrewlock.net/understanding-the-worst-dotnet-vulnerability-request-smuggling-and-cve-2025-55315/), 2025-10-28).

**Recommendation: tie the hosting opinion to the calendar.** A container image is patched when it is rebuilt from a patched base, so rebuild and redeploy after every Patch Tuesday whether or not the code changed. That is the editor's synthesis of the `ms-learn` servicing rule with the hosting opinion rather than a prescription any source states, and `resolve-research` should look for one that does.

## 4. Application security

### Authorization by default

`ms-learn` recommends a fallback policy that requires an authenticated user: "Setting the fallback authorization policy to require users to be authenticated protects newly added Razor Pages and controllers. Having authorization required by default is more secure than relying on new controllers and Razor Pages to include the `[Authorize]` attribute." ([Microsoft Learn: Create an ASP.NET Core web app with user data protected by authorization](https://learn.microsoft.com/aspnet/core/security/authorization/secure-data), reviewed 2026-05-11). Endpoints then opt out with `[AllowAnonymous]` or name a stricter policy.

```csharp
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());
```

It has the same shape as aspnet-core.md's validation rule: secure by default, with opt-outs per endpoint, rather than opting in endpoint by endpoint. `andrew-lock` covered the `DefaultPolicy`/`FallbackPolicy` split for ASP.NET Core 3.x, which is too old to cite for current behaviour. This is the strongest candidate for an opinion in the topic: single-sourced, but uncontested and mechanical.

### Browser clients: cookies, not tokens

For a browser front end `ms-learn` recommends cookies: "We recommend using cookies for browser-based applications, because, by default, the browser automatically handles them without exposing them to JavaScript." Tokens are for clients that cannot use cookies, and then "you are responsible for ensuring the tokens are kept secure" ([Microsoft Learn: Use Identity to secure a Web API backend for SPAs](https://learn.microsoft.com/aspnet/core/security/authentication/identity-api-authorization), reviewed 2026-03-23). For Blazor, `ms-learn` documents the Backend for Frontend (BFF) pattern with YARP proxying API calls "with the `access_token` stored in the authentication cookie", so the token never reaches the browser ([Microsoft Learn: Secure an ASP.NET Core Blazor Web App with OpenID Connect (OIDC)](https://learn.microsoft.com/aspnet/core/blazor/security/blazor-web-app-with-oidc?pivots=bff-pattern), reviewed 2025-12-18).

`milan-jovanovic` agrees from the attacker's side: "Avoid localStorage, which any XSS payload can read. HttpOnly cookies are safer for browser apps" ([Jovanović: JWT Authentication in ASP.NET Core](https://milanjovanovic.tech/blog/jwt-authentication-aspnetcore), 2026-07-04).

The deepest .NET writing on BFF is unvetted. Damien Bowden has implemented it for Angular, Blazor WebAssembly and OpenIddict since at least 2022, including a token-mediating design where "The downstream API does not accept the user delegated access tokens from the UI application" ([Bowden: BFF secured ASP.NET Core application using downstream API and an OAuth client credentials JWT](https://damienbod.com/2024/04/08/bff-secured-asp-net-core-application-using-downstream-api-and-an-oauth-client-credentials-jwt/), 2024-04-08, **unvetted**). Duende's documentation defines a reference architecture for it and sells the library that implements it (**unvetted**, vendor).

**Recommendation: browser apps authenticate with cookies.** A SPA or Blazor WebAssembly client that calls APIs does so through a BFF that holds the tokens on the server. Two roster sources carry it. The .NET 11 CSRF change makes it more attractive, because a same-origin BFF keeps API calls out of the new middleware's path. That last argument is Khalid's, in a 2026-09-01 post on Duende's blog that the roster cannot cite yet (item 7).

### Tokens for APIs

`milan-jovanovic` gives the validation floor, "Always validate issuer, audience, lifetime, and signing key", and adds "Rotate refresh tokens on every use (issue a new one, revoke the old)" (same post). `code-with-mukesh` puts a range on lifetime: "15 to 60 minutes is reasonable. Since you cannot revoke a JWT once it is issued, a short lifetime limits the damage if one leaks" ([Mukesh: JWT Authentication in ASP.NET Core](https://codewithmukesh.com/blog/jwt-authentication-in-aspnet-core/), 2026-06-07).

The two agree on where permissions live, and Mukesh measures the failure when they live in the token. Jovanović: "keep tokens lean and resolve permissions server-side with IClaimsTransformation" ([Jovanović: Building Secure APIs with Role-Based Access Control in ASP.NET Core](https://milanjovanovic.tech/blog/building-secure-apis-with-role-based-access-control-in-aspnetcore), 2025-09-27). Mukesh: "A revoked permission keeps working until the token expires, which I reproduced as a 201 that should have been a 403" ([Mukesh: Permission-Based Authorization in ASP.NET Core](https://codewithmukesh.com/blog/permission-based-authorization-in-aspnet-core/), 2026-08-06).

There is a tension inside `milan-jovanovic` that an opinion should settle rather than inherit. His Keycloak post argues for handing identity to "a battle-tested identity provider" ([Jovanović: Integrate Keycloak with ASP.NET Core Using OAuth 2.0](https://milanjovanovic.tech/blog/integrate-keycloak-with-aspnetcore-using-oauth-2), 2026-02-07), while his and Mukesh's most prominent posts teach issuing your own JWTs. The editor would put the opinion at the identity provider and treat self-issued tokens as the exception.

**API keys** have one source, and it is marked. Mukesh asks for "at least 128 bits of entropy, ideally 256 bits. Generated with RandomNumberGenerator.GetBytes()", storage "as SHA-256 hashes, never plaintext", and comparison with `CryptographicOperations.FixedTimeEquals` ([Mukesh: API Key Authentication in ASP.NET Core (.NET 10)](https://codewithmukesh.com/blog/api-key-authentication-aspnet-core/), 2026-05-08). Jovanović's 2023 post on the subject covers neither hashing nor comparison. The advice is sound, but a **Corroborate.** source cannot carry an opinion alone, so it is blocked until an unmarked source says the same.

### Secrets

`ms-learn` is unambiguous: "Never store passwords or other sensitive data in source code or configuration files. Production secrets shouldn't be used for development or test. Secrets shouldn't be deployed with the app. Production secrets should be accessed through a controlled means like Azure Key Vault." The Secret Manager "doesn't encrypt the stored secrets and shouldn't be treated as a trusted store. It's for development purposes only." ([Microsoft Learn: Safe storage of app secrets in development in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/app-secrets), reviewed 2026-05-13). The security overview adds managed identities as the most secure way to authenticate to Azure services.

`meziantou` covers the other way secrets leak, through logs and diagnostics ([Meziantou: Prevent accidental disclosure of configuration secrets](https://www.meziantou.net/prevent-accidental-disclosure-of-configuration-secrets.htm), 2023-02-13). .NET 10 closed one such path by default: EF Core now redacts inlined constants from its logs ([.NET Team: Announcing .NET 10](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/), 2025-11-11).

**Recommendation: user-secrets in development, a managed identity to a vault in production, and nothing secret in `appsettings*.json`.** Well sourced and uncontested. The redaction half cross-links to [logging.md](../opinions/logging.md).

### Data Protection in containers

The Data Protection key ring encrypts authentication cookies and antiforgery tokens. `khalid` shows why apps behind one proxy must share it ([Abuhakmeh: Sharing Auth Cookies With YARP, IdentityServer, and ASP.NET Core](https://khalidabuhakmeh.com/sharing-auth-cookies-with-yarp-identityserver-and-aspnet-core), 2022-08-23). `ms-learn` says where it must live in a container: "When hosting in a Docker container, keys should be maintained in either: A folder that's a Docker volume that persists beyond the container's lifetime... or An external provider, such as Azure Blob Storage... or Redis." ([Microsoft Learn: Configure ASP.NET Core Data Protection](https://learn.microsoft.com/aspnet/core/security/data-protection/configuration/overview), reviewed 2025-10-08).

The key-storage page adds two traps ([Microsoft Learn: Key storage providers in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/data-protection/implementation/key-storage-providers), reviewed 2025-11-07):

- "If you specify an explicit key persistence location, the data protection system deregisters the default key encryption at rest mechanism, so keys are no longer encrypted at rest." Persisting the ring without also calling one of the `ProtectKeysWith*` methods trades a restart bug for plaintext keys.
- "Redis doesn't persist data by default when restarting. This can cause Data Protection to issue new keys, invalidating previously protected data."

The configuration page adds that encrypting keys at rest "doesn't prevent cyberattackers from creating new keys", so the store's permissions matter as much as its encryption.

**Recommendation: wherever there is more than one instance or a container that restarts, persist the key ring to shared storage and protect it at rest, both explicitly.** It belongs next to the container hosting opinion, which as written produces exactly the deployment this failure needs. `andrew-lock`'s introduction to the system is from 2021 and predates .NET 8, so `ms-learn` carries this alone for now.

### CORS

`ms-learn`: "Specifying AllowAnyOrigin and AllowCredentials is an insecure configuration and can result in cross-site request forgery", and "Bypassing the built-in checks by using SetIsOriginAllowed(\_ => true) together with AllowCredentials is also an insecure configuration." Named policies applied with `[EnableCors]` give the finest control ([Microsoft Learn: Enable Cross-Origin Requests (CORS) in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/cors), reviewed 2026-05-12).

In .NET 11 the allowed-origins list will also decide which cross-origin requests the CSRF middleware admits (Lock, in section 1). A permissive CORS policy then becomes a CSRF hole as well as a CORS one. That is preview-dependent, but it is a reason to get a CORS opinion in before .NET 11 ships.

### HTTPS behind a proxy

`ms-learn` extends the hosting opinion's proxy contract to TLS: "If the proxy also handles HTTPS redirection, there's no need to use HTTPS redirection middleware. If the proxy server also handles writing HSTS headers... then the app doesn't require HSTS middleware." And `UseHsts` "isn't recommended in development because the HSTS settings are highly cacheable by browsers." ([Microsoft Learn: Enforce HTTPS in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/enforcing-ssl), reviewed 2026-07-29). Put simply, whichever layer terminates TLS owns redirection and HSTS. It is a sentence to add to the hosting opinion rather than an opinion of its own.

### Output encoding, CSP and security headers

Razor "automatically encodes all output sourced from variables, unless you work to prevent this behavior", and `HtmlString` "should never be used in combination with untrusted input because it exposes an XSS vulnerability" ([Microsoft Learn: Prevent Cross-Site Scripting (XSS) in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/cross-site-scripting), reviewed 2026-05-13). For Blazor, "CSP is recommended for Blazor apps". Blazor Web Apps on .NET 8 and later send `frame-ancestors 'self'` automatically, and on .NET 8 to 10 a strict policy without `unsafe-inline` styles breaks the `Virtualize` component, which .NET 11 fixes ([Microsoft Learn: Enforce a Content Security Policy for ASP.NET Core Blazor](https://learn.microsoft.com/aspnet/core/blazor/security/content-security-policy), reviewed 2026-08-18). `andrew-lock` adds Trusted Types for client-side script, under which "APIs like `innerHTML` must be passed a `TrustedHTML` object" ([Lock: Preventing client-side cross-site-scripting vulnerabilities with Trusted Types](https://andrewlock.net/preventing-client-side-cross-site-scripting-vulnerabilities-with-trusted-types/), 2025-02-11).

**A baseline of security headers is a gap no current source fills.** `ms-learn` has no page on `X-Content-Type-Options`, `Referrer-Policy` or a header baseline outside Blazor's CSP (negative result). `meziantou`'s post is from 2020 ([Meziantou: Security headers in ASP.NET Core](https://www.meziantou.net/security-headers-in-asp-net-core.htm), 2020-06-01). `andrew-lock` maintains NetEscapades.AspNetCore.SecurityHeaders, but its 1.0.0 release post is about the package's own supply chain rather than which headers to send ([Lock: NetEscapades.AspNetCore.SecurityHeaders 1.0.0 has been released](https://andrewlock.net/netescapades-aspnetcore-securityheaders-1-0-0-released/), 2025-04-15), and citing him for his own library would carry a conflict the roster does not yet record. OWASP's [.NET Security Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/DotNet_Security_Cheat_Sheet.html) lists headers (**unvetted**). Not ready.

### Open redirects

.NET 10 closed a minimal-API gap: `RedirectHttpResult.IsLocalUrl` works without MVC, as in `if (RedirectHttpResult.IsLocalUrl(url)) return Results.LocalRedirect(url);`, and `LocalRedirect` "throws an exception if a non-local URL is specified" ([Microsoft Learn: Prevent open redirect attacks in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/preventing-open-redirects), reviewed 2026-08-26). Small, mechanical and uncontested.

### SQL injection in EF Core

"The FromSql and FromSqlInterpolated methods are safe against SQL injection, and always integrate parameter data as a separate SQL parameter. However, the FromSqlRaw method can be vulnerable to SQL injection attacks, if improperly used." The same split holds for `ExecuteSql` against `ExecuteSqlRaw` and `SqlQuery` against `SqlQueryRaw` ([Microsoft Learn: SQL Queries](https://learn.microsoft.com/ef/core/querying/sql-queries), reviewed 2022-09-19). The review date is four years old and predates EF Core 10, but the API split has not changed since `FromSql` arrived in EF Core 7.

[data-access.md](../opinions/data-access.md) says nothing about raw SQL, and its TODO does not list it. One line would do: interpolated methods only, and a `*Raw` method never with input. It would also give that file its first `ms-learn` citation, which its source-redundancy note already points towards.

## 5. Runtime and cryptography

### Deserialization

`BinaryFormatter` is gone: "Starting in .NET 9, the in-box BinaryFormatter implementation throws exceptions on use, even with the settings that previously enabled its use." ([Microsoft Learn: BinaryFormatter disabled across all project types](https://learn.microsoft.com/dotnet/core/compatibility/serialization/9.0/binaryformatter-removal), reviewed 2024-08-06). An unsupported compatibility package keeps it alive for code that cannot migrate. `dotnet-blog` states the general principle: "Any deserializer, binary or text, that allows its input to carry information about the objects to be created is a security problem waiting to happen." ([Landwerth: BinaryFormatter removed from .NET 9](https://devblogs.microsoft.com/dotnet/binaryformatter-removed-from-dotnet-9/), 2024-08-28). `meziantou` shows the attack end to end, from a crafted payload to remote code execution ([Meziantou: Deserialization can be dangerous](https://www.meziantou.net/deserialization-can-be-dangerous.htm), 2020-01-05), which is old but describes a mechanism that has not changed. [ui-frameworks.md](../opinions/ui-frameworks.md) already mentions the removal as an Avalonia 12 migration cost.

**Recommendation: never reference the `System.Runtime.Serialization.Formatters` compatibility package, and never deserialize untrusted input with a format that names its own types.** Landwerth's sentence is the rule.

### Cryptography on GA APIs

`ms-learn`'s recommended-algorithms table names `Aes` for privacy, `HMACSHA256` or `HMACSHA512` for integrity, `ECDsa` or `RSA` for signatures, `RandomNumberGenerator` for random numbers and `Rfc2898DeriveBytes.Pbkdf2` for password-based key derivation ([Microsoft Learn: .NET cryptography model](https://learn.microsoft.com/dotnet/standard/security/cryptography-model), reviewed 2021-02-26). The page is five years old, gives no iteration count and does not mention AES-GCM, so it cannot carry an opinion alone. `steve-gordon` supplies the practical AES-GCM rule: the nonce must be unique per operation, filled from `RandomNumberGenerator`, and "There is, over time, a very small chance of generating the same nonce, which can render AES-GCM insecure" ([Gordon: Encrypting Properties with System.Text.Json and a TypeInfoResolver Modifier (Part 2)](https://www.stevejgordon.co.uk/encrypting-properties-with-system-text-json-and-a-typeinforesolver-modifier-part-2), 2026-02-05). `meziantou`'s cryptography and password-storage posts are from 2019.

For passwords, the passkeys-first opinion already routes around the question, and ASP.NET Core Identity hashes whatever passwords remain. Where a reader must derive a key from a password, `ms-learn` says PBKDF2, while OWASP's [Password Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html) prefers Argon2id (**unvetted**), which the BCL does not ship. It is recorded as a disagreement below.

### Post-quantum cryptography

**Experimental as of 2026-09-11, not yet an opinion,** for everything except ML-KEM and ML-DSA, and those only on some platforms. `dotnet-blog` is the primary source. "For `MLKem` and `MLDsa` we've removed `[Experimental]` from the classes, but it remains on a few methods": `ExportEncryptedPkcs8PrivateKey`, `ExportPkcs8PrivateKey`, `ExportSubjectPublicKeyInfo` and `ImportFromPem`, under diagnostic `SYSLIB5006`. Meanwhile "we've decided to release the `SlhDsa` and `CompositeMLDsa` classes with `[Experimental]`" ([Barton: Post-Quantum Cryptography in .NET](https://devblogs.microsoft.com/dotnet/post-quantum-cryptography-in-dotnet/), 2025-11-18). Linux needs OpenSSL 3.5 or later, and Apple platforms, Android and the browser have none of these algorithms ([Microsoft Learn: Cross-platform cryptography](https://learn.microsoft.com/dotnet/standard/security/cross-platform-cryptography), reviewed 2026-08-03). The two sources disagree on Windows; see below.

The honest guidance at GA is narrow: ML-KEM and ML-DSA are usable where the operating system provides them, and `IsSupported` must gate every use.

### Containers run as non-root

With SDK container publish, which the hosting opinion requires, "If you're targeting .NET 8 or higher and using the Microsoft runtime images, then: on Linux, the rootless user `app` is used" ([Microsoft Learn: Containerize a .NET app reference](https://learn.microsoft.com/dotnet/core/containers/publish-configuration), reviewed 2026-08-04). Setting `ContainerUser` to `root` undoes it. `andrew-lock` gives the reason: "if an attacker manages to compromise your container, they'll be able to do pretty much anything in the container" ([Lock: Updates to Docker images in .NET 8](https://andrewlock.net/exploring-the-dotnet-8-preview-updates-to-docker-images-in-dotnet-8/), 2023-10-17). The repository gets this for free, so the opinion is one line: never set `ContainerUser` to `root`. Chiseled images are documented on a page last reviewed 2024-08-28, which is too stale to cite for .NET 10 image choices.

## 6. Preview, not yet an opinion

Everything here is .NET 11, which reached RC 1 on 2026-09-08 with GA expected in November 2026 ([Microsoft Learn: What's new in .NET 11](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/overview), reviewed 2026-09-08).

- **Automatic CSRF protection from Fetch Metadata headers**, covered in section 1 and already in aspnet-core.md's aside.
- **`X25519DiffieHellman` and unpadded AES key wrap on `Aes`**, plus "TLS handshake hardening, certificate-validation alerts on Linux, channel binding validation on Unix".
- **A redesign of `unsafe` in C#** into a compiler-enforced contract between callers and API authors, an opt-in C# 16 feature previewing in .NET 11 and targeted for production in .NET 12 ([Lander: Improving C# Memory Safety](https://devblogs.microsoft.com/dotnet/improving-csharp-memory-safety/), 2026-05-21).
- **A CSP-compliant `Virtualize` component** in Blazor, which removes the .NET 8 to 10 conflict noted under output encoding.

## Where the sources disagree

1. **Post-quantum support on Windows.** `ms-learn`'s cross-platform page, reviewed 2026-08-03, still lists ML-KEM and ML-DSA on Windows as "Windows 11 Insiders (Latest)". `dotnet-blog` said on 2025-11-18 that "Windows support arrived this month". The blog is a later primary statement by the engineer who shipped the feature and the Learn table looks unrevised, so this topic follows the blog. A worker also surfaced the preview-era claim that `MLDsa` is `[Experimental]`, which the same post contradicts.
2. **What a lock file is for.** Integrity (`meziantou`), repeatability (`ms-learn`), or drift protection (this repository's template). All three are true of the same file. The template's framing is the weakest, and the only one that makes the file sound optional in every case.
3. **Where NuGet Audit runs.** `ms-learn` presents switching it off in CI as a supported choice. The **House:** rule says a check runs everywhere or fires late. House wins, and the sourced option would be recorded as the community default this repository declines.
4. **Password-based key derivation.** `ms-learn` recommends PBKDF2 through `Rfc2898DeriveBytes.Pbkdf2`, and OWASP prefers Argon2id (**unvetted**). Only the Learn position is citable, and it is the BCL's only option, so it would stand.
5. **Partitioning rate limits by IP.** `milan-jovanovic` (2023) endorses it for anonymous traffic, and aspnet-core.md forbids partitioning on spoofable headers. They are compatible once the opinion separates the connection address from a forwarded header (section 1).
6. **Issuing your own tokens.** `milan-jovanovic` argues for an identity provider in one post and teaches self-issued JWTs in another, and `code-with-mukesh` teaches the latter. This is less a disagreement between sources than one left unresolved inside a source.

## Freshness

`ms-learn` review dates are each page's `ms.date`, per the roster rule. The editor re-read four of these pages directly (NU1901–NU1904, releases and support, the Blazor OIDC page and cross-platform cryptography); the rest are `ms.date` values reported by worker agents.

| Page                                              | `ms.date`              |
| ------------------------------------------------- | ---------------------- |
| NuGet Warnings NU1901–NU1904                      | 2024-07-19             |
| Auditing package dependencies                     | 2025-10-01             |
| NuGetAudit audits transitive packages             | 2025-03-28             |
| Package Source Mapping                            | 2026-07-16             |
| Trusted Publishing                                | 2025-07-01, 2026-07-29 |
| MSBuild reference for .NET SDK projects           | 2026-03-27             |
| .NET releases, patches, and support               | 2026-05-15             |
| Minimal APIs antiforgery checks                   | 2023-12-05             |
| User data protected by authorization              | 2026-05-11             |
| Use Identity to secure a Web API backend for SPAs | 2026-03-23             |
| Blazor Web App with OIDC (BFF)                    | 2025-12-18             |
| Safe storage of app secrets                       | 2026-05-13             |
| Configure Data Protection                         | 2025-10-08             |
| Key storage providers                             | 2025-11-07             |
| CORS                                              | 2026-05-12             |
| Enforce HTTPS                                     | 2026-07-29             |
| Prevent XSS                                       | 2026-05-13             |
| Blazor Content Security Policy                    | 2026-08-18             |
| Prevent open redirect attacks                     | 2026-08-26             |
| EF Core SQL Queries                               | **2022-09-19**         |
| BinaryFormatter disabled                          | 2024-08-06             |
| .NET cryptography model                           | **2021-02-26**         |
| Cross-platform cryptography                       | 2026-08-03             |
| Containerize a .NET app reference                 | 2026-08-04             |
| Container images (chiseled)                       | **2024-08-28**         |
| What's new in .NET 11                             | 2026-09-08             |

The bold dates are the ones to weigh. The EF Core page describes an API that has not changed since EF Core 7, so its age does not weaken the claim. The cryptography model page is older than AES-GCM's prominence and every post-quantum type, and should not carry an opinion alone. The container-images page is why chiseled images get no recommendation here.

On the roster blogs, `meziantou`'s security catalogue is mostly 2019–2020, and only the NuGet posts from 2022 and 2024 are recent enough to lean on. `andrew-lock`'s security writing splits into a current run from 2025 to 2026 and a 2016–2023 back catalogue that predates .NET 8. `steve-gordon`'s AES-GCM series is from 2026. `khalid`'s citable post is from 2022 and describes a mechanism that has not changed.

Preview-gated: everything in section 6, plus `SlhDsa`, `CompositeMLDsa` and the experimental members of `MLKem` and `MLDsa`.

## What this reveals about the repository

1. **Security needs a home, and `resolve-research` should choose between two.** Supply chain fits [ci.md](../opinions/ci.md) and the templates and should go there. Application and runtime security fits no existing file. [aspnet-core.md](../opinions/aspnet-core.md) could absorb authorization defaults, cookies and BFF, Data Protection, CORS, HTTPS and open redirects, but secrets, deserialization and cryptography are not ASP.NET Core topics. The editor's recommendation is a new `opinions/security.md` for those, with aspnet-core.md keeping the HTTP-pipeline items and linking across. Either way the new file needs its README entries in the same pull request.

2. **The template's warnings rule needs its NuGet Audit consequence written down.** A comment in [templates/Directory.Build.props](../templates/Directory.Build.props) should say that NU1901–NU1904 fail restore on purpose, with `NuGetAuditSuppress` plus a stated reason as the only accepted escape. The house rule already implies this, so it is the rule applied rather than a new opinion, and `resolve-research` can weave it. `weave-house-opinion` is only needed if the owner wants the consequence recorded as its own house entry.

3. **ci.md's signing section is stale on credentials.** Trusted publishing is GA and nuget.org API keys now last 30 days. That is `harvest-sources` work, since `dotnet-blog` and `andrew-lock` published it, and it should land before 2026-11-01, when every older nuget.org key expires.

4. **aspnet-core.md's hosting opinion has two gaps of its own making.** It ships containers without saying where the Data Protection keys live, and it credits SDK container publish with patching that only happens on rebuild. Both come out of this topic and route through `resolve-research`.

5. **Two small opinions are ready now, each resting on one `ms-learn` page with nothing contesting it:** parameterised raw SQL for [data-access.md](../opinions/data-access.md), and `RedirectHttpResult.IsLocalUrl` for minimal-API redirects.

6. **For the next `refresh-dotnet-versions` run:** the .NET 11 GA in November 2026 turns the CSRF aside into an opinion and makes the CORS policy part of CSRF configuration. `CheckSdkVulnerabilities` becomes worth adopting once an SDK this repository pins carries it. And .NET 8 and 9 reach end of support on 2026-11-10, after which `verify-project` should report either as a finding.

7. **`vet-source` candidates, in order of what they would unlock:**
   - **Barry Dorrans ([idunno.org](https://idunno.org/))** is the strongest. Security is his specialism, he wrote _Beginning ASP.NET Security_, and he is publishing now, with posts from November 2025 to August 2026 on post-quantum support, package signing, SBOMs, SSRF and CVE triage. He works for Microsoft, which `vet-source` must weigh, but the `stephen-toub` precedent keeps full tier where there is depth with critical distance, and his writing is mechanism rather than adoption. His first-post date and his cadence before 2025 are unverified.
   - **Damien Bowden ([damienbod.com](https://damienbod.com/))** is independent and code-heavy, and has written about ASP.NET Core security since 2013, with an August 2026 post linked from the `jetbrains-dotnet` roundup. He would give the BFF recommendation a depth source, and his passkey `amr` claim could then be tested.
   - **`khalid`, re-scoped rather than admitted.** His row makes only pre-2025 posts citable because his blog went quiet. His writing has continued on Duende's blog, including the 2026-09-01 post on .NET 11 CSRF and identity flows. That is the dormancy exception's "writing continues elsewhere", with a vendor attachment the row already records. `vet-source` could widen his scope to his Duende posts, facing the same question the `avalonia-blog` widening did.
   - **Duende Software as a publication:** decline, or Tier 2 at most. It is product documentation for the library it sells, which is the independence cap's exact case.
   - **The OWASP Cheat Sheet Series:** discovery-only at most. It is institutional and actively revised, with commits through July 2026, but the .NET sheet mixes current and dated guidance, and it is not the published work of an awesome human in the roster's sense.
   - **Scott Brady:** blocked by dormancy. His site says he is no longer actively blogging, and his last substantial article is from April 2024.
   - **Philippe De Ryck:** out of scope. His articles are framework-agnostic or about JavaScript frameworks, with nothing .NET-specific, and none has appeared since 2022.

8. **Three gaps no current source fills:** a security-headers baseline, API-key handling (one **Corroborate.** source), and which analyzer security rules to switch on. They stay open rather than being filled from general knowledge.
