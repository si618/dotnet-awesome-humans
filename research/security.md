---
targets: [net10.0, csharp-14]
last-reviewed: 2026-09-25
sources: [andrew-lock, barry-dorrans, code-with-mukesh, ms-learn]
---

# Security: the blocked remainder

**Partially promoted on 2026-09-25.** The two passes of this topic (2026-09-11 and 2026-09-12) are in git history at `research/security.md`; everything they sourced has become opinion, and what is left here is what no citable source can carry yet. Each item below says what would unblock it.

## What promoted

| Went to                                                               | What                                                                                                                                                                                                                                                                                               |
| --------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [opinions/security.md](../opinions/security.md), new file             | Secrets, deserialization, the GA cryptography set with AES-GCM nonce handling, password hashing through Identity, post-quantum gated on `IsSupported`, servicing cadence                                                                                                                           |
| [opinions/aspnet-core.md](../opinions/aspnet-core.md)                 | The passkey `amr` caveat, authorization fallback policy, cookies and BFF, tokens for APIs including DPoP, PAR and client assertions, the Data Protection key ring, CORS, security headers, open redirects, the form-binding antiforgery rule, non-root containers and TLS ownership behind a proxy |
| [opinions/ci.md](../opinions/ci.md)                                   | NuGet Audit under the warnings rule, package source mapping, trusted publishing in place of an API key                                                                                                                                                                                             |
| [opinions/data-access.md](../opinions/data-access.md)                 | Parameterised raw SQL: interpolated methods only, never a `*Raw` one with input                                                                                                                                                                                                                    |
| [templates/Directory.Build.props](../templates/Directory.Build.props) | NU1901–NU1904 failing restore on purpose, and the lock file as a content pin rather than drift protection                                                                                                                                                                                          |

## Still blocked on a source

**API-key authentication.** `code-with-mukesh` asks for "at least 128 bits of entropy, ideally 256 bits. Generated with RandomNumberGenerator.GetBytes()", storage "as SHA-256 hashes, never plaintext", and comparison with `CryptographicOperations.FixedTimeEquals` ([Mukesh: API Key Authentication in ASP.NET Core (.NET 10)](https://codewithmukesh.com/blog/api-key-authentication-aspnet-core/), 2026-05-08). The advice is mechanical and looks right, and that id is marked **Corroborate.**, so it cannot carry the claim alone. `milan-jovanovic`'s 2023 post covers neither hashing nor comparison, and `damien-bowden` does not write about custom API-key authentication (negative result, 2026-09-12). **Unblocked by:** a second citable source on hashing and comparing API keys.

**Which security analyzer rules to enable.** `AnalysisModeSecurity` sets the analysis mode for the Security category alone and otherwise inherits `AnalysisMode` ([Microsoft Learn: MSBuild reference for .NET SDK projects](https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props), reviewed 2026-03-27). The template's `latest-recommended` turns some security rules on, and the documentation does not list which, pointing at the SDK's `analysislevel_*_recommended.globalconfig` instead. No source swept in either pass recommends a setting. **Unblocked by:** a source recommending a level or naming the rules, rather than this repository inventing one.

**SBOM and provenance.** `andrew-lock` is the only citable coverage, across three posts on generating an SBOM, provenance attestations and signed SBOM attestations with `actions/attest-sbom` (2025-03 to 2025-04). `ms-learn` has no page on SBOM generation for .NET builds and `damien-bowden` does not cover supply chain (both negative results). Nothing swept says who consumes the SBOM, which is the question an opinion would have to answer. `barry-dorrans` writes exactly this ground and is watch-listed until idunno.org reaches two years as a blog on 2027-11-18. **Unblocked by:** that date, or another citable voice.

**The PBKDF2 iteration count.** [opinions/security.md](../opinions/security.md) routes password hashing to Identity's own hasher at its 100,000-iteration default and leaves the count for a hand-rolled derivation to be justified per application, because `damien-bowden`'s 2024 floor of "more than 10000 iterations" is an order of magnitude below that and no roster source states a current figure for .NET 10. The OWASP Password Storage Cheat Sheet prefers Argon2id (**unvetted**, and not in the BCL), so it changed nothing. **Unblocked by:** a roster source stating a figure for .NET 10.

## Waiting on a release rather than a source

- **.NET 11 GA, expected November 2026.** At GA the CSRF aside in aspnet-core.md becomes the opinion, the CORS allowed-origins list becomes part of CSRF configuration, and the CSP-compliant `Virtualize` component removes the .NET 8 to 10 conflict recorded under security headers. For `refresh-dotnet-versions`.
- **`CheckSdkVulnerabilities`.** `ms-learn` documents the property and warning NETSDK1239 for an end-of-life SDK ([Microsoft Learn: .NET releases, patches, and support](https://learn.microsoft.com/dotnet/core/releases-and-support), reviewed 2026-05-15), and the installed 10.0.400 SDK contains no reference to it or to NETSDK1238 and NETSDK1240. Adopt it once an SDK this repository pins carries it.
- **.NET 8 and 9 end of support, 2026-11-10.** Recorded in [opinions/security.md](../opinions/security.md); after that date `verify-project` should report either target framework as a finding.

## A lead for `vet-source`

The passkey and antiforgery deep-dives on the Duende blog carry Maarten Balliauw's byline rather than Khalid's, so the widened `khalid` scope does not reach them. He has a long personal publishing record nobody has examined here, and passkeys is an opinion this repository already holds on a single source. Worth a pass on the same terms that admitted Khalid's byline rather than the publication.
