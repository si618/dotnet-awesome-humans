---
targets: [net10.0, csharp-14]
last-reviewed: 2026-08-14
last-used: 2026-08-20
sources: [milan-jovanovic, code-with-mukesh]
---

# Application architecture

Start with a modular monolith. Organise the code inside each module as vertical slices, and reach for layered (Clean) architecture only inside the slices that have real domain rules to protect.

<!-- Seeded 2026-08-14 by harvest-sources from newly admitted sources; see AWESOME-HUMANS.md decision log -->

## Opinions

- **Default to a modular monolith, not microservices.** Modules draw the macro boundaries — where the business boundaries are, who owns which data, how modules talk, and what public API each one exposes. That is the decision worth making early; a process boundary is a deployment choice you can make later, once a module has earned it. ([Jovanović: Modular monolith architecture in .NET](https://milanjovanovic.tech/blog/modular-monolith-architecture-dotnet))
- **Organise the inside of a module as vertical slices.** One use case owns its endpoint, request, validation, business logic, and data access together, so a feature changes in one place instead of across a controller, a service, and a repository that exist only to separate technologies. ([Jovanović: Vertical slice architecture in .NET](https://milanjovanovic.tech/blog/vertical-slice-architecture-dotnet))
- **Vertical slices are not modules.** Slices organise behaviour within a boundary; modules _are_ the boundary. Treating a slice as a module gives you a folder structure with none of the ownership guarantees. ([Jovanović: Where vertical slices fit inside the modular monolith](https://milanjovanovic.tech/blog/where-vertical-slices-fit-inside-the-modular-monolith-architecture))
- **Apply Clean Architecture per slice, not per solution.** Layering controls dependency direction, which is worth paying for where domain rules must stay independent of infrastructure — and pure ceremony in a slice that reads a row and returns it. Force every slice through identical layers and you get four projects to change a `GET`. ([Jovanović: Vertical slice architecture in .NET](https://milanjovanovic.tech/blog/vertical-slice-architecture-dotnet), [Mukesh: Clean Architecture in .NET 10](https://codewithmukesh.com/blog/clean-architecture-dotnet/))
- **Let the folder structure name the feature, not the pattern.** `Orders/Cancel/` beats `Controllers/`, `Services/`, `Repositories/` — the repository layout opinions in [project-structure.md](project-structure.md) stop at the project boundary; inside a project, features are the top-level grouping.

## Source redundancy

Both sources here were admitted on 2026-08-14 under the lowered longevity bars (`milan-jovanovic` Tier 1 with a conflict-of-interest note, `code-with-mukesh` Tier 2 capped on depth), and both sell templates and courses built on exactly these patterns. The guidance above is the uncontroversial core the two agree on; treat anything stronger — prescribed folder names, mediator libraries, per-slice project counts — as unsourced until a third, independent source corroborates it.

<!-- TODO: corroborate from an independent Tier 1 source, then extend with messaging between modules, transactional boundaries, and when a module has earned a process boundary -->
