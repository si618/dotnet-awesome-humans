---
targets: [net10.0, csharp-14]
last-reviewed: 2026-09-25
sources:
  [
    dotnet-blog,
    jetbrains-dotnet,
    andrew-lock,
    mark-seemann,
    jeremy-miller,
    ardalis,
    house,
  ]
---

# AI usage in .NET development

Treat a coding agent as a fast, untrusted contributor, and let the build you already have be what it answers to.
The gates this repository already recommends do most of the work: warnings as errors, nullable warnings as errors, analyzers in the build, pinned packages and a real test suite.
Three things belong on top of them.
Ground the agent in first-party skills and live package data rather than its training data.
Never accept tests the agent wrote after the code as evidence the code works.
Run an agent given unattended permissions inside an isolation boundary.

The roster only partly supports this topic, and that finding matters as much as the guidance.
The citable writing on it comes from vendors describing their own tooling (`dotnet-blog`, `jetbrains-dotnet`) or from sources whose notes point citations elsewhere: `mark-seemann` and `ardalis` for their language-agnostic 2026 AI essays, and `jeremy-miller` as **Corroborate.** with a live conflict.
The most concrete .NET-specific practitioner writing found is Aaron Stannard's, who is **unvetted**.
That is why vetting him is part of this topic (see [Vetting Aaron Stannard](#vetting-aaron-stannard)).

Scope: this topic covers using agents to write and maintain .NET code.
Building AI features into .NET applications is a separate question, noted at the end.

## What the repository already says

Nothing under `opinions/` is about AI, but three opinions already carry most of the guardrails:

- **[ci.md](../opinions/ci.md)** opens with "Supply-chain hygiene is not optional in the agentic era": SHA-pinned actions, a pinned SDK and pinned packages, and restricting which package assets may run at build time.
- **House:** warnings are errors everywhere, and every suppression carries an inline comment saying why ([ci.md](../opinions/ci.md)). This is local convention, not community consensus, but it is the rule most directly aimed at an agent that silences a warning to get a green build.
- **[csharp.md](../opinions/csharp.md)** makes nullable warnings errors and turns analyzers up to `latest-recommended` with `EnforceCodeStyleInBuild`, so the compiler rejects more of what an agent gets wrong before a reviewer has to catch it.
- **[testing.md](../opinions/testing.md)** holds tests to the same review bar as production code, which is exactly the bar agent-written tests tend to miss (below).

## Grounding: first-party skills and live data over training data

**Prefer skills published by the platform or package owner, and give agents live package data.**
A model's knowledge of .NET is months stale at best. It is why an unguided agent reaches for xUnit v2, `Newtonsoft.Json` or a package version that no longer exists.

- The .NET team publishes agent skills at [dotnet/skills](https://github.com/dotnet/skills), described as workflows from "the team who is building the platform itself". Each skill is scored with evaluators against a no-skill baseline, explicitly not assuming "more context always yields better results". ([.NET Blog: Extend your coding agent with .NET Skills](https://devblogs.microsoft.com/dotnet/extend-your-coding-agent-with-dotnet-skills/), 2026-03-09, `dotnet-blog`)
- The NuGet MCP server gives agents current package data, private feeds included, and resolves vulnerable or conflicting dependencies to the lowest compatible safe version. **It was announced in preview on 2025-08-14 and is not an opinion.** The worker sweep found Microsoft Learn documenting it as built into Visual Studio 2026, but its GA status as of 2026-09-25 is unverified. ([.NET Blog: Announcing the NuGet MCP Server Preview](https://devblogs.microsoft.com/dotnet/nuget-mcp-server-preview/), `dotnet-blog`)
- JetBrains measured what a tool-backed skill buys over source-reading. Across 80 runs, handing the agent dotTrace snapshots through a `dottrace-analyze` skill lifted average diagnosis accuracy from 4.71 to 8.15 out of 10 and more than doubled perfect root-cause identifications, from 20 to 48. In one scenario it also cut cost from $3.74 to $2.58 per run, because the agent stopped exploring. ([JetBrains: Your AI Agent Keeps Missing The Real Bottleneck](https://blog.jetbrains.com/dotnet/2026/06/25/performance-profiling-agent-skill-in-rider/), 2026-06-25, `jetbrains-dotnet`) The same logic drives the `refactoring-code` skill, which has the agent call Rider's refactoring engine instead of editing text freehand. ([JetBrains: Rider Hands AI Agents The Keys To Its Refactoring Engine](https://blog.jetbrains.com/dotnet/2026/08/19/rider-refactoring-code-skill/), 2026-08-19)
  Both are a vendor measuring its own paid tool. The roster records no independence limit on `jetbrains-dotnet`, but read the numbers as one evaluation rather than a replicated result.
- Package maintainers are starting to ship skills too, with JasperFx's Critter Stack AI Skills as the roster example. It is a commercial product sold by the author of the posts announcing it ([Miller: Introducing the Critter Stack AI Skills](https://jeremydmiller.com/2026/05/20/introducing-the-critter-stack-ai-skills/), 2026-05-20, `jeremy-miller`, **Corroborate.**, conflict recorded), so it shows a trend and makes no recommendation.

**Skills from a third party are someone else's opinions.** They are not neutral grounding.
Before installing a skill pack, check it against this repository's opinions, as [the dotnet-skills comparison below](#dotnet-skills-against-this-repositorys-opinions) does. That pack contradicts `csharp.md` and `testing.md` in several places, and one of its samples does not compile.

## Verification: the build is the reviewer, and agent-written tests are not evidence

**Gate agent output on checks it cannot talk its way past, and do not count tests the agent wrote after the code.**

- Mark Seemann argues that LLM-generated tests for code that already exists are "tests as ceremony, rather than tests as an application of the scientific method". A test's evidential value comes from having seen it fail, and a retrofitted test never has. His remedies are to break the code deliberately, confirm the test fails, then revert. Better still, invert the flow: write the tests first and let the model implement against them. ([Seemann: AI-generated tests as ceremony](https://blog.ploeh.dk/2026/01/26/ai-generated-tests-as-ceremony/), 2026-01-26)
  In ["The hailo effect"](https://blog.ploeh.dk/2026/04/06/the-hailo-effect/) (2026-04-06) he warns that a model's agreeable manner earns it trust its output has not: "LLM friendliness does not entail competency".
  **Limit:** `mark-seemann`'s notes say to cite his C#/.NET posts, and both of these are language-agnostic. They carry the argument here; promoting it needs a .NET-specific citable source alongside.
- Jeremy Miller reports the productive side of the same coin, after two weeks of Claude Code across Marten and Wolverine. It "has been great when you have very detailed compliance test frameworks that the AI tools can use to verify the completion of the work". ([Miller: 2 Weeks of Claude Code for Me](https://jeremydmiller.com/2026/02/14/2-weeks-of-claude-code-for-me/), 2026-02-14, `jeremy-miller`, **Corroborate.**)
  He and Seemann do not actually disagree. Both put the guardrail in tests the agent did not write, and Miller had years of them already. Where they part is temperament: Miller is enthusiastic about throughput, and Seemann distrusts the trust itself.
- **Unvetted:** Aaron Stannard reached the same finding on a greenfield .NET project. The agent "authored lots of anemic tests that checked the 'has test coverage' box without _really_ testing the functionality thoroughly". What held was warnings as errors, Aspire integration tests the agent could drive through Playwright, and Verify snapshots whose approval files sit in source control. ([Stannard: Software 2.0: Planning and Verifying a Greenfield Project](https://aaronstannard.com/software-2.0-case-study-textforge/), 2026-03-13)
  He also built [dotnet-slopwatch](https://github.com/Aaronontheweb/dotnet-slopwatch), which flags the shortcuts an agent takes to get to green: skipped tests, `#pragma warning disable`, `<NoWarn>`, empty `catch` blocks, `Task.Delay` in tests and inline versions that bypass central package management. Its target overlaps the **House:** suppression rule, with one difference: the house allows a suppression that carries its reason, while slopwatch flags every one against a baseline.

**One disagreement with the repository's own opinion.**
Stannard found that `.editorconfig` style checks in the build "burned millions of tokens over 1-2 months on trivial issues" as the agent looped on formatting diagnostics.
`csharp.md` recommends `EnforceCodeStyleInBuild`, and `ci.md` runs `dotnet format --verify-no-changes` as its own step.
The two are reconcilable: have the agent run `dotnet format` before building, rather than dropping the rule.
But this is one unvetted report, so it is a question for `resolve-research`, and not yet a change.

## Isolation and supply chain

- **Run an agent with bypassed permission prompts only inside a microVM.** Andrew Lock recommends Docker Sandboxes because "unlike containers, which share the host kernel, each sandbox has its own kernel". Network traffic goes through a proxy that blocks host access and injects credentials, so the agent never holds them. He pairs each sandbox with its own git worktree, which matches how this repository already works. ([Andrew Lock: Running AI agents safely in a microVM using docker sandbox](https://andrewlock.net/running-ai-agents-safely-in-a-microvm-using-docker-sandbox/), 2026-04-07, `andrew-lock`) The post does not cover a .NET SDK or NuGet configuration inside the sandbox.
- **Hallucinated package names are an attack surface.** A USENIX Security 2025 study found 19.7% of package references in 576,000 generated code samples named packages that do not exist, and attackers register the names models repeatedly invent ("slopsquatting"). **Unvetted:** the figure is widely repeated, but this sweep reached it through secondary coverage only, and nothing on the roster yet discusses it for NuGet. The mitigations are already in `ci.md`: central package management, no floating versions, and a human choosing each new dependency.

## Cost

**Unvetted and roster-limited, recorded for the decision rather than as guidance.**
Steve Smith argues that flat-rate agent subscriptions are an "open bar" that agentic usage breaks, and predicts prices 10 to 100 times their January 2026 level by the end of 2027. His advice is to treat AI tooling as a portfolio bet and keep the team's own skills sharp as a hedge. ([Smith: AI Benefits - But at What Cost?](https://ardalis.com/ai-benefits---but-at-what-cost/), 2026-03-19, `ardalis`, **Corroborate.** The source's notes point citations at its .NET posts rather than its 2026 AI run.)
Stannard makes a related market argument in ["Escaping the LLM Coding Rat Race"](https://aaronstannard.com/escaping-ai-coding-ratrace/) (2026-07-27, **unvetted**): "There's only one winner in the AI coding rat race: your inference provider."

## Vetting Aaron Stannard

The evidence gathered here is for `vet-source`, which makes the decision on its own branch.
The reading below is preliminary.

| Criterion        | Evidence                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| ---------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Longevity**    | [aaronstannard.com](https://aaronstannard.com/) paginates back to 2010-05-14 across 19 archive pages, and he also writes on the [Petabridge blog](https://petabridge.com/blog/). Both are active: 2026-08-21 and 2026-09-23 respectively, each roughly monthly. Not verified: the Wayback cross-check of the first post, and whether 2013–2020 holds a gap of over a year.                                                                                                                                                                                                         |
| **Depth**        | Strong where he builds things: [NBench](https://aaronstannard.com/introducing-nbench/) (2015) starts from an undetected throughput regression in Akka.NET. The TextForge case study and ["A Security-Critical Project Where I Don't Read the Code"](https://aaronstannard.com/a-security-critical-project-where-i-dont-read-the-code/) (2026-08-10) are worked experience reports with concrete verification pipelines. Weaker on the essays, which argue from experience more than measurement.                                                                                   |
| **Accuracy**     | No corrections found, and nothing found left standing wrong, but the search was shallow against roughly 190 posts, so this is absence of evidence only.                                                                                                                                                                                                                                                                                                                                                                                                                            |
| **Independence** | CEO of Petabridge, which sells Akka.NET support and training and the Phobos monitoring product through Sdkbin, his own marketplace. [The Petabridge retrospective](https://petabridge.com/blog/10-years-of-petabridge/) mixes engineering lessons with a Phobos plug. The AI essays promote his own free tools (dotnet-skills, slopwatch, Netclaw) and a few third-party ones. Against that, [removing FluentAssertions from Akka.NET](https://petabridge.com/blog/why-akkadotnet-remove-fluentassertions/) (2026-06-10) was a licensing decision against a convenient dependency. |

**Preliminary reading:** he likely clears the bar on longevity and depth, with a **conflict of interest** to record on Akka.NET, actor-model and observability topics, following the `jeremy-miller` precedent.
Whether he also needs **Corroborate.** turns on the unsampled 2013–2024 back catalogue.
A split worth writing into his notes, however he is admitted: **cite the blog, not dotnet-skills.**
The repository counts as authored documentation under the admission rules, but the comparison below shows it lagging his own writing.

### dotnet-skills against this repository's opinions

[Aaronontheweb/dotnet-skills](https://github.com/Aaronontheweb/dotnet-skills) (MIT, created 2025-11-12, 1,187 stars, last push 2026-09-17) is a plugin of 30-odd skills and 5 subagents for Claude Code, Codex, Copilot and OpenCode.
Its [`csharp-coding-standards`](https://github.com/Aaronontheweb/dotnet-skills/blob/master/skills/csharp-coding-standards/SKILL.md) skill agrees with this repository on direction: nullable enabled, `ArgumentNullException.ThrowIfNull` at boundaries, switch expressions, and `TreatWarningsAsErrors`.
The specifics are another matter.
Each row was checked on 2026-09-25, and the three code rows were compiled or run against SDK 10.0.400:

| dotnet-skills says                                                                                                                                                                                   | This repository says                                                                                                                                                 | Verified                                                                                                                      |
| ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| Records with `IReadOnlyList<T>` members (`ShoppingCart`, `Order`), and `record` for "domain entities"                                                                                                | [csharp.md: Records](../opinions/csharp.md#records): a collection member breaks value equality                                                                       | Two carts with identical contents compare unequal                                                                             |
| `record EmailAddress` validates in its constructor, but exposes `Value { get; init; }`                                                                                                               | [csharp.md: Records](../opinions/csharp.md#records): `with` does not re-run the constructor                                                                          | `new EmailAddress("a@b.c") with { Value = "junk" }` succeeds                                                                  |
| `readonly record struct OrderId(string Value)` plus a validating `OrderId(string value)` constructor                                                                                                 | Code samples must compile ([AGENTS.md](../AGENTS.md#writing-style-for-opinions))                                                                                     | Fails with CS0111: the constructor duplicates the primary one. And `default(OrderId)` skips any validation a struct can hold. |
| xUnit `2.9.x` and FluentAssertions `6.12.0`/`7.0.0` in [`package-management`](https://github.com/Aaronontheweb/dotnet-skills/blob/master/skills/package-management/SKILL.md) and `project-structure` | [testing.md](../opinions/testing.md): xUnit v3 on Microsoft.Testing.Platform. His own team removed FluentAssertions from Akka.NET on 2026-06-10                      | Read from the skill files                                                                                                     |
| `<PackageReference Include="xunit" Version="*" />` in `testcontainers` and `playwright-blazor`                                                                                                       | [ci.md](../opinions/ci.md): CI must not float versions the repository did not choose. His own slopwatch flags inline versions as a central package management bypass | Read from the skill files                                                                                                     |

The pattern is a pack that states current principles and ships stale or broken examples, which is the failure grounding is meant to fix.
An agent follows the example, not the principle.

## Building AI into .NET applications (out of scope here)

Noted so a follow-up topic has somewhere to start, with nothing weighed:

- `Microsoft.Extensions.AI` (`IChatClient`) reached GA in 2025. The worker sweep dated it around May 2025 from a search snippet, so the date is unverified.
- Microsoft Agent Framework reached 1.0 on 2026-04-03, bringing together Semantic Kernel and AutoGen ([Microsoft Agent Framework Version 1.0](https://devblogs.microsoft.com/agent-framework/microsoft-agent-framework-version-1-0/), the package's own release post, so a reference).
- The C# MCP SDK and a `dotnet new mcpserver` template are official ([Microsoft Learn: Create a minimal MCP server](https://learn.microsoft.com/dotnet/ai/quickstarts/build-mcp-server)).

## Closing the loop

- **No opinion covers this topic.** When the roster can carry it, run `resolve-research`. It will likely fold into existing files rather than open a new one: grounding and isolation into `ci.md`, retrofitted agent tests into `testing.md`. Today the unmarked citable support is vendor-only, which is thin for a promotion.
- **Run `vet-source` on Aaron Stannard** (`vet/aaron-stannard`). The evidence above covers most of it. What is left: the 2013–2024 sample, the Wayback check and a pass for corrections.
- **Other `vet-source` candidates this sweep surfaced, all unvetted:** Rockford Lhotka ([blog.lhotka.net](https://blog.lhotka.net/)), whose 2026 series on building agents in .NET includes "Tools and Skills: Better Together", and Oskar Dudycz's "Vibing, Harness and OODA loop" (2026-04-26, `oskar-dudycz` is already citable, so this is a harvest lead rather than a vetting one).
- **Open question for the owner:** whether the `.editorconfig`-in-build cost Stannard reports should shape `csharp.md`, or whether a pre-build `dotnet format` answers it. This could be a **House:** call.
- **Not verified in this pass:** the NuGet MCP server's GA status, `Microsoft.Extensions.AI`'s GA date, and the primary source for the slopsquatting figure.
