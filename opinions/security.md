---
targets: [net10.0, csharp-14]
last-reviewed: 2026-09-25
last-used: 2026-09-25
sources:
  [andrew-lock, damien-bowden, dotnet-blog, meziantou, ms-learn, steve-gordon]
---

# Security

The security rules that are not about the HTTP pipeline: secrets, deserialization, cryptography and servicing cadence. The pipeline's own rules live in [aspnet-core.md](aspnet-core.md), which covers authorization defaults, cookies and BFF, tokens, the Data Protection key ring, CORS, HTTPS, security headers and open redirects. Supply-chain rules are in [ci.md](ci.md), and parameterised SQL is in [data-access.md](data-access.md).

## Opinions

### Secrets

**Use the Secret Manager in development, a managed identity to a vault in production, and keep secrets out of `appsettings*.json` at every stage.** The Secret Manager "doesn't encrypt the stored secrets and shouldn't be treated as a trusted store. It's for development purposes only", and production secrets "should be accessed through a controlled means like Azure Key Vault" ([Microsoft Learn: Safe storage of app secrets in development in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/app-secrets)). A managed identity is what removes the last secret from the deployment, because there is then no vault credential to store either.

```csharp
// Azure.Extensions.AspNetCore.Configuration.Secrets + Azure.Identity.
// The managed identity works in the deployed app and the second credential is the
// developer's own sign-in, so one code path covers both. Pass the id explicitly:
// the parameterless ManagedIdentityCredential constructor is obsolete as of
// Azure.Identity 1.21.0, and CS0618 is an error under the warnings rule.
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{builder.Configuration["VaultName"]}.vault.azure.net/"),
    new ChainedTokenCredential(
        new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned),
        new AzureCliCredential()));
```

([Bowden: Using ASP.NET Core with Azure Key Vault](https://damienbod.com/2024/12/02/using-asp-net-core-with-azure-key-vault/))

The other way a secret leaves the process is through a log or a diagnostic dump, which is a separate review of what the application writes ([Meziantou: Prevent accidental disclosure of configuration secrets](https://www.meziantou.net/prevent-accidental-disclosure-of-configuration-secrets.htm)). .NET 10 closed one of those paths: EF Core now redacts inlined constants from the SQL it logs, and the logging rules are in [logging.md](logging.md) ([.NET Team: Announcing .NET 10](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/)).

### Deserialization

**Never reference the `System.Runtime.Serialization.Formatters` compatibility package, and never deserialize untrusted input with a format that names its own types.** `BinaryFormatter` is gone from the platform: "Starting in .NET 9, the in-box BinaryFormatter implementation throws exceptions on use, even with the settings that previously enabled its use." ([Microsoft Learn: BinaryFormatter disabled across all project types](https://learn.microsoft.com/dotnet/core/compatibility/serialization/9.0/binaryformatter-removal)) The compatibility package brings the behaviour back, which makes referencing it a decision to keep the vulnerability.

The rule generalises past that one type, because the flaw is type information in the payload rather than the binary encoding: "Any deserializer, binary or text, that allows its input to carry information about the objects to be created is a security problem waiting to happen." ([Landwerth: BinaryFormatter removed from .NET 9](https://devblogs.microsoft.com/dotnet/binaryformatter-removed-from-dotnet-9/)) So keep `System.Text.Json` on its default contract-based binding, and never switch a converter or a polymorphic setting to a type name the caller supplies. A crafted payload reaching a type-naming deserializer is remote code execution, demonstrated end to end in [Meziantou: Deserialization can be dangerous](https://www.meziantou.net/deserialization-can-be-dangerous.htm).

### Cryptography: stay on the GA algorithm set

**Pick the algorithm from the platform's recommended set, and never implement a primitive yourself.** The set below is Microsoft's ([Microsoft Learn: .NET cryptography model](https://learn.microsoft.com/dotnet/standard/security/cryptography-model)); that page was last reviewed in 2021 and does not reach AES-GCM or password parameters, so those two rows carry their own current sources.

| Purpose                       | API                                                          |
| ----------------------------- | ------------------------------------------------------------ |
| Symmetric encryption          | `Aes`, or `AesGcm` where the ciphertext needs authenticating |
| Integrity                     | `HMACSHA256` or `HMACSHA512`                                 |
| Signatures                    | `ECDsa` or `RSA`                                             |
| Random values                 | `RandomNumberGenerator`                                      |
| Password-based key derivation | `Rfc2898DeriveBytes.Pbkdf2`                                  |

**A GCM nonce is used once.** Generate it per operation from `RandomNumberGenerator`, store it beside the ciphertext, and never derive it from a counter you might reset: "There is, over time, a very small chance of generating the same nonce, which can render AES-GCM insecure" ([Gordon: Encrypting Properties with System.Text.Json and a TypeInfoResolver Modifier (Part 2)](https://www.stevejgordon.co.uk/encrypting-properties-with-system-text-json-and-a-typeinforesolver-modifier-part-2)). A key that encrypts enough data to make a 12-byte collision plausible is one to rotate rather than to keep feeding.

**Let ASP.NET Core Identity hash passwords.** Its `PasswordHasher` runs PBKDF2 at an `IterationCount` whose "Default is 100,000" ([Microsoft Learn: PasswordHasherOptions.IterationCount](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.identity.passwordhasheroptions.iterationcount)), which is an order of magnitude above the floor a hand-rolled derivation usually gets, and the passkeys-first opinion in [aspnet-core.md](aspnet-core.md#authentication-passkeys-first) means most new apps never hash one. Call `Rfc2898DeriveBytes.Pbkdf2` directly only to derive a key from a password outside Identity, with a salt of at least 8 bytes from `RandomNumberGenerator` ([Bowden: Creating hashes in .NET](https://damienbod.com/2024/07/01/creating-hashes-in-net/)). Argon2id lost because the BCL does not ship it. Bowden's 2024 floor of "more than 10000 iterations" is a floor and not a target, and no current source states a figure for .NET 10, so treat the count as a number to justify per application rather than one this repository has settled.

### Post-quantum: `IsSupported` gates every use

**`MLKem` and `MLDsa` are usable, and every call site checks `IsSupported` first**, because the algorithms come from the operating system rather than from the runtime: Linux needs OpenSSL 3.5 or later, and Apple platforms, Android and the browser have none of them ([Microsoft Learn: Cross-platform cryptography in .NET](https://learn.microsoft.com/dotnet/standard/security/cross-platform-cryptography)). Both classes shipped without `[Experimental]`, while four export and import members keep it under diagnostic `SYSLIB5006`, so a key that has to leave the process still opts into an experimental API ([Barton: Post-Quantum Cryptography in .NET](https://devblogs.microsoft.com/dotnet/post-quantum-cryptography-in-dotnet/)). Windows support arrived with that post, and the Learn compatibility table has not been revised to match.

### Servicing: take the monthly patch

**Rebuild and redeploy after every Patch Tuesday, whether or not your code changed.** Security fixes ship on "always the second Tuesday of the month", and the platform's own instruction is to "Regularly install servicing updates to ensure that your apps are in a secure and supported state" ([Microsoft Learn: .NET releases, patches, and support](https://learn.microsoft.com/dotnet/core/releases-and-support)). A container image is the case that needs saying out loud, because it carries the runtime it was built with until something rebuilds it ([aspnet-core.md: Hosting and deployment](aspnet-core.md#hosting-and-deployment)).

The cost of a slow cadence is measurable. CVE-2025-55315 is a request-smuggling flaw in ASP.NET Core that "received Microsoft's highest-ever CVSS score of 9.9", and exploiting it could let an attacker "Login as a different user", "Make an internal request (SSRF)" or "Bypass CSRF checks" ([Lock: Understanding the worst .NET vulnerability ever: request smuggling and CVE-2025-55315](https://andrewlock.net/understanding-the-worst-dotnet-vulnerability-request-smuggling-and-cve-2025-55315/)). A fix for that was available on a Tuesday; whether it was deployed was a property of the pipeline.

**Run a supported target framework.** .NET 8 and .NET 9 both reach end of support on 2026-11-10 ([Damkewala: .NET STS releases supported for 24 months](https://devblogs.microsoft.com/dotnet/dotnet-sts-releases-supported-for-24-months/)), after which a project on either stops receiving these patches at all. `net10.0` is supported to November 2028.

## Coming next (preview, not yet the opinion)

.NET 11 adds `X25519DiffieHellman`, unpadded AES key wrap on `Aes`, TLS handshake hardening, certificate-validation alerts on Linux and channel binding validation on Unix. `SlhDsa` and `CompositeMLDsa` ship `[Experimental]` and stay off the GA set ([Barton: Post-Quantum Cryptography in .NET](https://devblogs.microsoft.com/dotnet/post-quantum-cryptography-in-dotnet/)). C# 16 previews a redesign of `unsafe` into a compiler-enforced contract between callers and API authors, targeted for production use in .NET 12 ([Lander: Improving C# Memory Safety](https://devblogs.microsoft.com/dotnet/improving-csharp-memory-safety/)).
