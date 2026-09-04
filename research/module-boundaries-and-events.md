---
targets: [net10.0, csharp-14]
last-reviewed: 2026-09-04
sources:
  [
    ms-learn,
    mark-seemann,
    jeremy-miller,
    oskar-dudycz,
    derek-comartin,
    milan-jovanovic,
  ]
---

# Module boundaries and events

Research for the open TODO in [architecture.md](../opinions/architecture.md), which asks for corroboration from an independent Tier 1 source and then three extensions: messaging between modules, transactional boundaries, and when a module has earned a process boundary. Event sourcing and event-driven architecture sit in the same topic because the three extensions all reach for them.

This is the second pass. The first, on 2026-08-24, had one independent source and reported that half the TODO was blocked. The roster admitted four sources on 2026-09-03 in response, and this pass sweeps all four.

**The independent corroboration the TODO asked for now exists, and it does not confirm what the file assumed it would.** `mark-seemann` corroborates the boundary and coupling reasoning, says nothing at all about event mechanics, and contradicts the fourth opinion outright. Two of the other three admissions do corroborate the slice opinions, but they co-maintain the same product and so cannot count as two voices. And the modular-monolith-first claim, which the first pass recorded as the settled half, rests on a page that pass misdated by five years.

Discharging the TODO therefore means rewording three of the five opinions, not appending citations to them. On the three extensions the material is now strong enough to write opinions from. On event sourcing the answer is a firm "rarely, per module, and never by default", and it is the one claim in this topic that four sources reach independently.

## Sources swept

| id                | Tier                          | Used for                                                                                 |
| ----------------- | ----------------------------- | ---------------------------------------------------------------------------------------- |
| `ms-learn`        | 1, independent                | Corroboration, transactional boundaries, process boundaries, idempotency, event sourcing |
| `mark-seemann`    | 1, independent, unmarked      | Boundary explicitness, module contracts, coupling, layering                              |
| `jeremy-miller`   | 1, **Corroborate.**           | Modular monolith criteria, vertical slices, outbox, event sourcing scope                 |
| `oskar-dudycz`    | 1, **Corroborate.**           | Slices and modules, internal and external events, event sourcing scope, cutting services |
| `derek-comartin`  | 1, **Corroborate.**           | Boundary ownership, commands versus events, the cost of decoupling                       |
| `milan-jovanovic` | 1, conflict of interest noted | Module communication patterns, event sourcing framing (carried from the first pass)      |
| `andrew-lock`     | 1, independent                | Swept 2026-08-24, nothing on this ground (negative result, recorded below)               |

`jimmy-bogard` and `kamil-grzybek` remain unvetted and no claim here rests on them. Their status is settled in item 4 at the end.

## 1. Corroboration, opinion by opinion

The first pass answered the TODO at the level of the file. Four sources later that is too coarse, because they split the five opinions differently. Taken one at a time:

### "Default to a modular monolith, not microservices"

**Corroborated, but more weakly than the first pass recorded, and every voice hedges.**

The first pass led on `ms-learn` "confirmed by a page reviewed as recently as 2026-07-08". That date is wrong. It is the page's `updated_at`, a docset build timestamp; the page's own review date is the `ms.date` field, and it reads 2021-12-12. The text descends from the 2018-era e-book and is authored by `ardalis`, who maintains the Clean Architecture solution template it links to. The [Freshness](#freshness) section below carries the full correction, which affects every `ms-learn` citation in this file.

The substance survives the date. `ms-learn` still says to start monolithic and treat separation as a later choice, and still names the cost: "If you can't deliver independent feature slices of the application, separating it only adds complexity." ([Microsoft Learn: Common web application architectures](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures), reviewed 2021-12-12)

What is new is that neither architect admitted for this gap will state the opinion as flatly as `architecture.md` does.

`mark-seemann` declines to prescribe a starting point at all. He reads the whole monolith-versus-services question as fashion: "One decade, service-oriented architecture (SOA) is cool; the next, consolidation sets in; then it's micro-services; and, as far as I can tell, monoliths are on the way in again, although I'm sure that we'll find something else to call them." ([Seemann: Pendulum swings](https://blog.ploeh.dk/2021/02/22/pendulum-swings/), 2021-02-22)

He goes further in the one place he does give advice, and it points away from the monolith. Describing his own book's sample code, he attributes its decoupling to years of F# and Haskell discipline rather than to any structure a reader could copy:

> I'm confident that the sample code is nicely decoupled, even though it's packaged as a monolith. But unless you have a similar experience, I recommend that you separate your code base into multiple packages.

([Seemann: Decomposing CTFiYH's sample code base](https://blog.ploeh.dk/2023/09/04/decomposing-ctfiyhs-sample-code-base/), 2023-09-04)

That is a recommendation to enforce module boundaries physically, at the package level, precisely because most teams will not hold them by convention. It is compatible with a modular monolith and it is not the same opinion.

`jeremy-miller` is more sceptical still, and says so twice: "I'm still dubious that the modular monolith idea is going to be a panacea" ([Miller: Actually Talking about Modular Monoliths](https://jeremydmiller.com/2024/04/08/actually-talking-about-modular-monoliths/), 2024-04-08), and "I'm unfortunately dubious about the mechanics and whether or not this is just some 'modular' lipstick on the old 'monolith' pig" ([Miller: Thoughts on "Modular Monoliths"](https://jeremydmiller.com/2024/04/01/thoughts-on-modular-monoliths/), 2024-04-01). He remains "bullish on the long term usefulness of micro-services" for genuine bounded contexts.

His diagnosis of why old monoliths hurt is worth recording, because it does not blame the monolith: the pain came from "The usage of prescriptive architectures like Clean Architecture or Onion Architecture solution templates". That is an argument for the first opinion by way of contradicting the fourth.

**What to do with it.** The opinion holds. The confidence should come down. The honest form is that a modular monolith is the cheaper default because the process boundary is reversible and the module boundary is not, rather than that the industry has settled on it.

### "Organise the inside of a module as vertical slices"

**Corroborated by two sources, contradicted in framing by the one independent source.**

`jeremy-miller` states it structurally and recently: a slice "organizes code around features instead of technical layers. A slice owns its whole pathway: take the input, do the work, produce the output, all in one place." ([Miller: The Codebase Is the Prompt](https://jeremydmiller.com/2026/06/04/the-codebase-is-the-prompt-wolverine-vertical-slices-and-ai-assisted-development/), 2026-06-04). He calls himself "a proponent of 'Vertical Slice Architecture' code organization and a harsh critic of layered architecture approaches (Clean/Onion/Hexagonal) as they are commonly practiced", and attributes the term to Jimmy Bogard rather than claiming it.

`oskar-dudycz` agrees and sharpens the unit: "A vertical slice is one piece of functionality, cut through the whole application. For me, a slice is more a function than an entity." ([Dudycz: Vertical slices, their ownership and external dependencies](https://event-driven.io/en/vertical-slices-and-dependencies/), 2026-08-10)

`mark-seemann` uses the words and means something else by them. Vertical slicing appears in his writing as a way of working, not a way of arranging code:

> As I describe in Code That Fits in Your Head, I usually develop (vertical) feature slices one at a time, utilising an outside-in TDD process, during which I also figure out how to save or retrieve data from persistent storage.

([Seemann: Do ORMs reduce the need for mapping?](https://blog.ploeh.dk/2023/09/18/do-orms-reduce-the-need-for-mapping/), 2023-09-18)

Two sentences later in the same post he names the structure that code sits in, and it is not slices: "the architecture is Ports and Adapters, or, if you will, Clean Architecture." Across 832 posts he never uses the capitalised term "Vertical Slice Architecture".

**What to do with it.** The opinion holds, on `jeremy-miller` and `oskar-dudycz`, both marked **Corroborate.** and so admissible only together. The file should stop implying the framing is uncontested, because the one unmarked source in the sweep reads the same word as a delivery process.

### "Vertical slices are not modules"

**Corroborated, and this is the cleanest result in the sweep.**

`oskar-dudycz` states the containment relation directly: "A module is a logical grouping of slices." He then draws the boundary rule that follows from it, and it is stricter than `architecture.md` currently says: "Everything outside the slice is external, whether it's the next folder or another system." On persistence he separates the two units explicitly: "Database schemas go per module... A slice is a feature, not a persistence boundary." (same post, 2026-08-10)

`jeremy-miller` confirms the distinction by complaining that the field ignores it: "there's a ton of disagreement about what the hell it is that 'vertical slice architecture' actually means and a lot of folks conflating that with bounded contexts or micro-services." ([Miller: We Don't Need No Stinkin' Repositories](https://jeremydmiller.com/2025/02/27/we-dont-need-no-stinkin-repositories-and-other-observations-on-dotnetrocks/), 2025-02-27)

`derek-comartin` supplies the ownership test that makes a module a module: "Logical boundaries are all about ownership. Who owns the data? Who owns the business rules? Who owns the invariants you need to enforce?" ([Comartin: Modular Monolith Boundaries Done Wrong](https://codeopinion.com/modular-monolith-boundaries/), 2026-06-02). His enforcement test is the query log rather than the namespace: "Your code might say Sales and Warehouse. But what do your queries say?" ([Comartin: Stop Joining Tables In Your "Modular" Monolith](https://codeopinion.com/stop-joining-tables-in-your-modular-monolith/), 2026-05-27)

**What to do with it.** Keep the opinion and add the persistence rule. Schema per module, not per slice, is the concrete form of the boundary and it is testable.

### "Apply Clean Architecture per slice, not per solution"

**Contradicted from both directions. No source in the sweep holds this position.**

`ms-learn` contradicts it upward, prescribing the layering solution-wide and calling that arrangement "the most appropriate way to structure non-trivial monolithic applications".

The two new architecture sources contradict it downward, arguing the layering should not be applied at either scale. `mark-seemann` is the more absolute, and this is his most recent statement on layering:

> I usually don't abstract application behaviour from frameworks. I don't create 'application layers', 'use-case classes', 'mediators', or similar. This is a deliberate architecture decision.

([Seemann: Ports and fat adapters](https://blog.ploeh.dk/2025/04/01/ports-and-fat-adapters/), 2025-04-01)

He also denies that the named architectures are distinct things to choose between, which removes the ground the opinion stands on. "If you apply the Dependency Inversion Principle to Layered Architecture, you end up with Ports and Adapters." ([Seemann: Layers, Onions, Ports, Adapters: it's all the same](https://blog.ploeh.dk/2013/12/03/layers-onions-ports-adapters-its-all-the-same/), 2013-12-03). Eleven years later he reports the same thing from consulting: "Today, most organizations that I consult with will tell me that they've decided on Ports and Adapters. Even so, if you do it right, it's the same architecture." ([Seemann: Three data architectures for the server](https://blog.ploeh.dk/2024/07/25/three-data-architectures-for-the-server/), 2024-07-25)

`jeremy-miller` attacks the specific benefit the layering is sold on: "I have almost never needed to reason about a system's entire data access layer in isolation even though that's held up as an advantage of Clean/Onion/Hexagonal layering approaches." He extends it to the repository abstraction, across tools: "I would generally recommend against using wrapping repository abstractions around low level persistence tooling like Marten, EF Core, or Dapper in systems in most cases", because such an interface "does pretty well nothing to add any value", pushes teams to a least-common-denominator API, and does not deliver the swappability it promises, which is "patently not true". (2025-02-27)

He also names the runtime cost, which is the strongest form of the argument because it is observable rather than aesthetic: "Big call stacks of a controller calling a mediator tool that calls one service that calls other services that call different repository abstractions that all make database queries is a common source of chattiness because it's hard to even see where all the chattiness is coming from by reading the code." ([Miller: Network Round Trips are Evil](https://jeremydmiller.com/2024/07/08/network-round-trips-are-evil/), 2024-07-08)

**What to do with it.** The headline claim is unsupported and should go. The body of the opinion survives, because what the body actually says is "layering is worth paying for where domain rules must stay independent of infrastructure, and pure ceremony elsewhere". Both sources would accept that; neither would accept "apply Clean Architecture per slice" as the way to say it. Rewrite the opinion to lead with the conditional and drop the pattern name from the headline.

### "Let the folder structure name the feature, not the pattern"

**Corroborated, by two sources reaching it in different vocabulary.**

`jeremy-miller`: "Organize code around the 'verbs' of the system more than the 'nouns' (entities) of the system", with an explicit carve-out that matches the repository's own instinct: "if you're truly building a CRUD system, I think you can ignore everything I've said and just go bang out code." (2025-02-27)

`oskar-dudycz` reaches the same rule from the slice definition: "a slice is more a function than an entity" (2026-08-10).

Verbs over nouns and functions over entities are the same claim. Both sources are marked **Corroborate.**, so together they can carry the opinion, subject to the independence problem in item 2 below.

### Negative result worth keeping

`andrew-lock` was swept on 2026-08-24 as the other obvious independent Tier 1 candidate and carries nothing on module boundaries, messaging or the outbox. His background-work catalogue is Quartz.NET and `IHostedService` mechanics, which is hosting rather than architecture. Do not re-sweep him for this topic.

## 2. Messaging between modules

**The choice between an interface call and a message is a coupling decision, and the sweep now agrees on which coupling you are buying.**

- **Synchronous, via a public interface resolved through DI.** "Method calls are in-memory, so they are fast, easy to implement, and add no indirection." The calling module fails when the called module does. ([Jovanović: Modular monolith communication patterns](https://milanjovanovic.tech/blog/modular-monolith-communication-patterns), 2023-08-05)
- **Asynchronous, via messages.** "Messaging gives you loose coupling and high availability, since the receiving module does not need to be available when a message is sent." The cost is infrastructure. (same source)

**The rule that outranks the choice: do not build chains of synchronous calls across boundaries.** `ms-learn` names where that ends:

> If your internal microservices are communicating by creating chains of HTTP requests as described, it could be argued that you have a monolithic application, but one based on HTTP between processes instead of intra-process communication mechanisms.

([Microsoft Learn: Challenges and solutions for distributed data management](https://learn.microsoft.com/dotnet/architecture/microservices/architect-microservice-container-applications/distributed-data-management), reviewed 2018-09-20)

`derek-comartin` names the mechanism: "Blocking synchronous calls, such as HTTP, from service to service can provide issues with latency, fault tolerance, and availability, all because of temporal coupling." ([Comartin: Distributed Tracing to discover a Distributed BIG BALL of MUD](https://codeopinion.com/distributed-tracing-to-discover-a-distributed-big-ball-of-mud/), 2022-08-03)

This reads across to a modular monolith directly. A chain of in-process interface calls between modules has the coupling and the failure cascade without the latency, which makes it the cheapest version of the mistake to make and the hardest to see.

### A boundary you can hide is not a boundary

The most useful thing `mark-seemann` contributes to this topic is not a pattern but a failure mode, and it is the one a modular monolith is most exposed to. His four tenets of SOA series is his most recent sustained architecture writing and it is C# throughout.

He starts from the observation that tooling has always undone boundary explicitness: "So much for the principle that boundaries are explicit. They're not, and it bothered me twenty years ago, as it bothers me today." The consequence is the one every module-crossing helper method produces: "When the boundary is not explicit, you may inadvertently write client code that makes network calls, and you may not be aware of it."

He then works through the obvious fix and rejects it. Mandating that every cross-boundary operation be a command "is essentially an asynchronous messaging architecture", and it forces queries into Request-Reply with correlation identifiers. That works, and then somebody wraps it in a helper:

> We're back where we started. Boundaries are no longer explicit. Equivalent to how good names are only skin-deep, this attempt to make boundaries explicit can't resist programmers' natural tendency to make things easier for themselves.

His answer is to pick an abstraction that cannot be wrapped away: "People often complain that async code is contagious. By that they mean that once a piece of code is asynchronous, the caller must also be asynchronous. This effect is transitive, and while this is often lamented as a problem, this is exactly what we need. Amplify the essential. Make boundaries explicit." ([Seemann: Boundaries are explicit](https://blog.ploeh.dk/2024/03/11/boundaries-are-explicit/), 2024-03-11)

For a modular monolith this is the sharpest available argument for messaging over interfaces, and it does not rest on availability or infrastructure. A message contract cannot be collapsed into a property access; an injected interface can.

The companion tenet gives the contract rule: "The third SOA tenet emphasizes that only data travels over service boundaries. In order to communicate effectively, services must agree on the shape of data, and which operations are legal when. While they exchange data, however, they don't share address space, or even internal representation." ([Seemann: Services share schema and contract, not class](https://blog.ploeh.dk/2024/04/15/services-share-schema-and-contract-not-class/), 2024-04-15)

That converges with `oskar-dudycz` from a different direction: "Everything outside the slice is external, whether it's the next folder or another system." Two sources, one unmarked and one marked, agree that a module boundary should be treated with the discipline of a network boundary even when it is a folder.

### Domain events, integration events, and whether those are the right names

`ms-learn` separates them cleanly. A **domain event** is "something that happened in the domain that you want other parts of the same domain (in-process) to be aware of", dispatched in-process and possibly synchronously. An **integration event** propagates a committed change outward and "should occur only if the entity is successfully persisted, otherwise it's as if the entire operation never happened"; integration events "must be based on asynchronous communication". ([Microsoft Learn: Domain events, design and implementation](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation), reviewed 2018-10-08)

**`oskar-dudycz` rejects the vocabulary while keeping the distinction.**

> Why am I not using domain event and integration event terms? Because they're highly misleading.

His grounds are that the two names describe one kind of thing seen from two places, and that naming them separately hides it: "All of them should be domain events. They are just used in different contexts. Internal (or private) is understandable in the module context, and external (or public) is understandable in the whole system context."

The same post carries the only treatment of event granularity found in the sweep, and it is the answer to what a module should expose. Publishing internal events outward is what welds modules together: "If we expose our internal events, we must communicate and consult each change we make with other teams. We have to assume that the event is being used by someone else." His alternative is a deliberately separate event shaped for consumers, the Summary Event, and he distinguishes it from the pattern it is usually confused with:

> Summary Event can be easily mistaken with Event Carried State Transfer or Snapshot, yet they're not the same. Summary Event still gathers business information about the business fact that has happened. We can not only replicate the state (as with Event Carried State Transfer) but also trigger the workflow's next steps.

The framing that follows is the one worth carrying into an opinion: "Events should be treated as such. Yes, API." A module's external events are its public interface and change like one. ([Dudycz: Internal and external events, or how to design event-driven API](https://event-driven.io/en/internal_external_events/), 2023-10-15). The post uses C# and Marten, which is a direct conflict of interest and is flagged here rather than relied on.

`jeremy-miller` never uses "integration events" anywhere on his site. His vocabulary is domain events, cascading messages, or just events, with the in-process and forwarded cases distinguished by mechanism rather than by name.

**The practical consequence is unchanged by the naming argument.** An event dispatched inside a module and an event published across a module boundary are different things with different delivery guarantees, and conflating them is what produces a distributed monolith. Whether the repository writes that as domain versus integration or internal versus external is a wording decision for `resolve-research`. `ms-learn` is the more citable of the two on the distinction; `oskar-dudycz` is the better argument for why the usual names mislead, and the only source with anything to say about which events a module should publish.

### Commands or events across a boundary, where the sources genuinely differ

`derek-comartin`: "Generally, avoid crossing boundaries with commands." ([Comartin: Commands or Events: Which One for Workflow?](https://codeopinion.com/commands-or-events-which-one-for-workflow/), 2025-03-25)

`oskar-dudycz` gives the opposite-facing rule, as an anti-pattern: "If we'll always have a single consumer for an event that needs to run the specific logic and expect to get the particular event back, then it should be a command." ([Dudycz: Anti-patterns in event modelling - Passive-Aggressive Events](https://event-driven.io/en/passive_aggressive_events/), 2026-04-13)

These are reconcilable and the reconciliation is the useful part. Comartin is arguing against a module telling another module what to do. Dudycz is arguing against dressing a command up as an event to look decoupled while keeping a single known consumer and an expected reply. The shared rule underneath: the message type should match the actual coupling, and publishing an event to one known consumer that must answer is a command with extra steps. See [Where the sources disagree](#where-the-sources-disagree) for how to weigh them.

### The limit of the whole approach

`derek-comartin` supplies the caveat the repository should carry alongside any messaging opinion, because it is the one thing every vendor-adjacent source omits: "Event-driven architecture isn't some magical silver bullet that removes coupling; it doesn't." ([Comartin: Event-Driven Architecture lost its way](https://codeopinion.com/event-driven-architecture-lost-its-way/), 2024-03-07). And on the accounting: "You did not change the amount of complexity in the business process. You moved the complexity somewhere else." ([Comartin: Decoupling in Software Architecture Moves Complexity](https://codeopinion.com/decoupling-in-software-architecture-moves-complexity/), 2026-08-06)

Both posts carry the Particular Software sponsorship in the article body, which is noted in the roster row. The claims cut against the sponsor's product category rather than for it, which is the direction that matters.

## 3. Transactional boundaries

**The aggregate is the consistency boundary, and one transaction should normally cover one aggregate.** `ms-learn` quotes Evans, "Any rule that spans Aggregates will not be expected to be up-to-date at all times", and Vernon, "if executing a command on one aggregate instance requires that additional business rules execute on one or more aggregates, use eventual consistency". The reason given is lock contention at scale. (Domain events page, reviewed 2018-10-08)

**`ms-learn` does not follow that rule itself, and is explicit about the trade:**

> the initial deferred approach—raising the events before committing, so you use a single transaction—is the simplest approach when using EF Core and a relational database. It's easier to implement and valid in many business cases.

The mechanism is one line with a large consequence. Domain events accumulate on the entity and dispatch either side of `SaveChangesAsync`:

```csharp
public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
{
    // Before SaveChanges: handlers share the scoped DbContext, so their writes
    // join the same transaction and roll back with it.
    await _mediator.DispatchDomainEventsAsync(this);

    var result = await base.SaveChangesAsync(cancellationToken);
    // After SaveChanges instead: separate transactions, and you owe the system
    // eventual consistency plus compensating actions on failure.
    return result > 0;
}
```

The sample keeps the eShopOnContainers shape. `DispatchDomainEventsAsync` is a hand-rolled extension method in that repository rather than a MediatR API, and `architecture.md` still treats mediator libraries as unsourced, so the dispatch call is the mechanism to copy rather than the library. `ms-learn` names the cost of the other choice plainly: "if there's an issue and the event handlers cannot commit their side effects, you'll have inconsistencies between aggregates", recoverable only by storing the events and running a reconciliation batch.

**`jeremy-miller` supplies the objection to this exact pattern, and it is the one the repository needs to hear.** He describes the classic .NET domain-events implementation, an entity base class collecting events dispatched through a mediator on `SaveChangesAsync`, and then says: "I have always hated this Domain Events pattern and much prefer the full 'Critter Stack' approach with the Decider pattern and event sourcing." ([Miller: "Classic" .NET Domain Events with Wolverine and EF Core](https://jeremydmiller.com/2025/12/04/classic-net-domain-events-with-wolverine-and-ef-core/), 2025-12-04)

That preference is for his own product's pattern and is not citable as a verdict. The tool-independent point inside it is: mediator-based dispatch carries no outbox, and a domain-event handler that touches anything outside the transaction is unsafe without one. He states the general version elsewhere: "a lot of shops I think are naively opting into MediatR as a core tool without realizing the important functionality it is completely missing in order to build a resilient system — like a transactional outbox." He names MassTransit alongside his own tool as a valid answer, which is worth recording because it is evidence he is not arguing purely to his own catalogue. ([Miller: Build Resilient Systems with Wolverine's Transactional Outbox](https://jeremydmiller.com/2024/12/08/build-resilient-systems-with-wolverines-transactional-outbox/), 2024-12-08)

**The outbox is what makes a cross-boundary event honest.** `ms-learn` describes it as "a transactional database table as a message queue", with the event row written "atomically with the original database operation, using a local transaction against the same database". `jeremy-miller` gives the same definition tool-independently: "The general idea of an 'outbox' is to obviate the lack of true 2 phase commits by ensuring that outgoing messages are held until the database transaction is successful, then somehow guaranteeing that the messages will be sent out afterward." ([Miller: Transactional Outbox/Inbox with Wolverine and why you care](https://jeremydmiller.com/2022/12/15/transactional-outbox-inbox-with-wolverine-and-why-you-care/), 2022-12-15)

He also states the delivery guarantee honestly. Nothing in his writing claims exactly-once; the guarantee is that a message "will be delivered at least once as long as the database transaction to save the new event succeeds".

### The consume side, which the first pass flagged as missing

At-least-once delivery makes idempotent consumption mandatory, and the first pass recorded that no page it had read covered the mechanics. The Azure Architecture Center page now in scope does.

> The durable solution isn't to eliminate duplicate delivery. It's to make the consumer tolerate it.

It names the inbox as "the consume-side companion to the Transactional Outbox pattern": record each processed message identifier in the same transaction as the work, so a redelivery finds the record and stops. Four points are worth carrying into an opinion:

- **Prefer natural idempotency first.** "Design for natural idempotency first." An operation that sets a value rather than incrementing one needs no bookkeeping.
- **Enforce with a unique constraint, not a check-then-set.** Reading for an existing record and then inserting is a race between concurrent consumers. "This approach makes the database the single arbiter of the race."
- **Retention must outlast redelivery.** The processed-message table has to be kept longer than the broker's redelivery window, including dead-letter resubmission, or a late duplicate finds nothing and runs twice.
- **Message identity has to be stable.** A producer-assigned identifier survives redelivery; one generated at consume time does not.

([Microsoft Learn: Idempotent Consumer pattern](https://learn.microsoft.com/azure/architecture/patterns/idempotent-consumer), reviewed 2026-08-13)

This page carries `ai-usage: ai-assisted` in its metadata. The Event Sourcing page does not. That is not disqualifying, and it is worth recording on any citation of it.

### A defensible opinion for this repository

Inside a module, dispatch domain events before `SaveChanges` and take the single transaction. A modular monolith has one database, and the lock-contention argument is a scale problem most modules never reach. Across modules, never share a transaction: publish through an outbox, consume idempotently, and accept eventual consistency. That puts the transactional boundary on exactly the line the repository already draws for ownership, which is the property worth having.

`ms-learn` supports the second half without qualification: "No microservice should ever include tables/storage owned by another microservice in its own transactions, not even in direct queries", and rules out two-phase commit as "against microservices principles". `derek-comartin` supplies the modular-monolith version of the same rule, which is the one that actually gets broken: stop joining tables across module boundaries, and check the queries rather than the namespaces.

Two existing files touch this and should be cross-linked rather than duplicated. [data-access.md](../opinions/data-access.md) warns that `ExecuteUpdateAsync` and `ExecuteDeleteAsync` bypass the change tracker, so "`SaveChanges`-based audit and outbox logic doesn't run", which is precisely how an outbox stops working silently. [aspnet-core.md](../opinions/aspnet-core.md) already covers propagating OpenTelemetry context across outbox tables and queues.

## 4. When a module has earned a process boundary

Four sources give criteria and they compose into one list rather than competing.

**From `ms-learn`, three signals, all observations about an existing module rather than predictions.**

- **Scaling that cloning cannot serve.** "Many applications, when they need to scale beyond a single instance, can do so through the relatively simple process of cloning that entire instance." Until one module's load profile genuinely diverges, the process boundary buys nothing.
- **Independent delivery.** "If you can't deliver independent feature slices of the application, separating it only adds complexity." A module that always ships alongside its neighbour has not earned anything.
- **The chattiness test, which runs both ways.** Validate that "there are no chatty calls between services, and if splitting functionality into two services causes them to be overly chatty, it might indicate those functions belong in the same service". `ms-learn` turns it into a merge signal too: a design that "involves constantly aggregating information from multiple microservices for complex queries" may be "a reason to merge microservices."

**From `jeremy-miller`, the same chattiness test with co-change added, which is the better version.** He states it twice in nearly the same words:

> if two or more services/modules are chatty between themselves and very frequently have to be modified at the same time, they're best described as a single bounded context and should probably be combined into a single service or module.

([Miller: Modular Monoliths and the "Critter Stack"](https://jeremydmiller.com/2024/04/15/modular-monoliths-and-the-critter-stack/), 2024-04-15; restated 2025-02-27)

Co-change frequency is the addition worth taking. It is measurable from git history, it needs no instrumentation, and it catches a bad boundary before any traffic exists.

He also gives the reason the decision is worth deferring, sourced to community experience rather than his own tools: "In every single experience report you'll ever find about a team trying to break up and modernize a large monolithic application the authors will invariably say that breaking apart the database was the single most challenging task." The shared-database failure he names the "Pond Scum Anti-Pattern": many modules over one murky schema.

His planning stance is Citadel and Outpost, which he credits to Glenn Henriksen rather than claiming: "I actually want to think a little bit upfront about having a path to easily break out modules into separate processes or services later." (2024-04-08)

**From `oskar-dudycz`, criteria that are about operations rather than code.** He lists differential traffic and workload, independent horizontal scaling, tenant isolation, and multi-region deployment as reasons to cut, and rejects two common ones: chasing total availability, and splitting because a module wants a different storage technology. ([Dudycz: How (not) to cut microservices](https://event-driven.io/en/how_to_cut_microservices/), 2021-01-13)

**From `derek-comartin`, the failure mode to watch for after cutting.** Temporal coupling through synchronous calls turns a set of services into a distributed big ball of mud, and distributed tracing is how you find out that is what you built.

**The cheapest predictor is already within reach.** A module that talks to its neighbours only through asynchronous messages can be extracted without rewriting its communication, which is `milan-jovanovic`'s stated reason for preferring messaging early: such modules are "much easier" to migrate. A module reached by direct interface calls has not been prepared, and extracting it is a rewrite rather than a move. `mark-seemann`'s async-contagion argument is the same point made structurally: if the boundary was never hideable, moving it costs nothing at the call sites.

`mark-seemann` also supplies the warning about what happens when the boundary is drawn without autonomy: "Many organizations inadvertently create distributed monoliths. I think that this often stems from a failure of heeding the tenet that services are autonomous." ([Seemann: Services are autonomous](https://blog.ploeh.dk/2024/03/25/services-are-autonomous/), 2024-03-25)

## 5. Event sourcing and event-driven architecture

**Event-driven between modules: yes, with the caveat from `derek-comartin` attached.** Integration events over an outbox with idempotent consumers is the shape every source describes. Nothing in the sweep argues against it for a modular monolith. What the sweep does add is that it moves complexity rather than removing it, and that it does not by itself remove coupling.

**Event sourcing as a persistence model: rarely, per module, and never by default.** This is the strongest result in the topic, because four sources reach it and two of them build event-sourcing tools for a living.

`ms-learn` leads the pattern page with the warning:

> Event sourcing is a complex pattern that introduces significant trade-offs. It changes how you store data, handle concurrency, evolve schemas, and query state. It's costly to migrate to or from an event sourcing solution, and after you adopt the pattern, it constrains future design decisions in the parts of the system that use it. [...] For most systems and most parts of a system, traditional data management is sufficient.

Five situations where it does not fit: straightforward CRUD with no audit requirement, prototypes and short-lived systems, anything needing real-time consistent views, mostly static reference data, and teams without event-driven experience, where "adopting it without the foundational knowledge increases the risk of antipatterns that are costly to reverse". ([Microsoft Learn: Event Sourcing pattern](https://learn.microsoft.com/azure/architecture/patterns/event-sourcing), reviewed 2026-03-27)

The costs it enumerates are concrete and mostly permanent: replay cost requiring snapshots, schema evolution needing upcasters or tolerant deserialization, no SQL-shaped querying, at-least-once delivery forcing idempotent consumers, and a collision between an immutable log and the right to erasure, mitigated only by keeping personal data outside the log or crypto-shredding by per-subject key.

**`oskar-dudycz` says the same thing against his own commercial interest**, which is the most weight any single quote in this topic carries:

> I'm always saying that a well-done CRUD is much better than a poorly done Event Sourcing.

and, decisively for this repository:

> Event Sourcing is not a system-wide architecture concept. It should be considered at the module level.

([Dudycz: When not to use Event Sourcing?](https://event-driven.io/en/when_not_to_use_event_sourcing/), 2021-06-23). He co-maintains Marten and sells Event Sourcing workshops, so a post titled this way is the opposite of a sales argument.

**`jeremy-miller` reaches the module-level rule independently of the page but not independently of the person**, which matters and is picked up in item 2 below:

> one of our current JasperFx Software clients has a large monolithic application where some workflow-centric modules would be a good fit for an event sourcing approach, while other modules are more CRUD centric or reporting-centric where a straight up RDBMS approach is probably much more appropriate.

(2024-04-15; the same point appears at 2024-04-08). He has no standalone essay arguing when not to use event sourcing; the position exists only as an aside inside the modular-monolith posts.

**`ms-learn`'s own closing advice is the opinion this repository would write:** "Event sourcing doesn't have to be an all-or-nothing decision for your entire system. Apply it selectively to the parts of your system that it benefits the most, such as a payment ledger or order-processing pipeline."

Two details are commonly got wrong and worth keeping:

- **An event store is not a broker.** `ms-learn`: "Message brokers such as Apache Kafka typically lack per-entity stream queries and optimistic concurrency. They work well as a distribution layer [...] but they aren't a substitute for an event store." `oskar-dudycz` says it independently and more bluntly: "It's a common mistake to use tools like Kafka and Pulsar for event stores, but they are not." ([Dudycz: Event Streaming is not Event Sourcing!](https://event-driven.io/en/event_streaming_is_not_event_sourcing/), 2021-12-01). `jeremy-miller` uses the same shape from the other side, framing Kafka as a downstream relay out of an event store rather than an alternative to one. This is corroboration across a real independence line, Microsoft and Dudycz, and it is the safest claim in the section.
- **Design events for intent, not for resulting state.** `ms-learn`: "an event that records _two seats were reserved_ is more valuable than an event that records _remaining seats changed to 42_." State-shaped events "reduce the event store to a change log that has no business meaning".

**Negative results across the whole sweep, which are load-bearing for what stays unsourced.** No source swept treats sagas as a design question of choreography versus orchestration. Event granularity and event-carried state transfer are covered by `oskar-dudycz` alone, in the internal-and-external post above, and he is marked **Corroborate.** and writing about tooling he maintains, so that material cannot carry an opinion until something corroborates it. `mark-seemann` has no post on domain events, integration events, the outbox, idempotency or event sourcing at all: across 832 posts on the current blog, the only event-store entry is a library of his own from 2014, and the messaging posts date from 2011 to 2013. `jeremy-miller` has no idempotent-consumer essay; his outbox writing is producer-side. `derek-comartin` has no dedicated domain-versus-integration-events post and none on when not to use CQRS.

## Where the sources disagree

**Layering: solution-wide, per slice, or not at all.** `ms-learn` prescribes it solution-wide and calls that "the most appropriate way to structure non-trivial monolithic applications". `architecture.md` says per slice. `mark-seemann` and `jeremy-miller` say the distinction between the named layered architectures is not real and the application layer should not exist.

Weighing them: the `ms-learn` page is 2021 text descending from a 2018 e-book, authored by the maintainer of the Clean Architecture solution template it links to, and it is describing a monolith that has not been modularised, where layers are the only boundary available. It is not addressing a codebase whose modules already do the encapsulation. **Weigh towards `mark-seemann` and `jeremy-miller`** on the layering question specifically: they are more recent, one is unmarked and independent, and their criticism is of a design neither of them sells. The repository's position should be restated as the conditional it already contains in its body, and should stop naming a pattern in its headline.

**Commands across boundaries: avoid them, or insist on them.** `derek-comartin` says generally avoid crossing boundaries with commands. `oskar-dudycz` says an event with one known consumer that expects a reply should have been a command. Neither is wrong and the disagreement is about which failure is more common. Comartin is guarding against modules directing each other; Dudycz is guarding against theatre, where an event is used to look decoupled while the coupling is intact.

**Weigh them by direction of travel.** If you are choosing a message type for a new interaction, take Comartin's rule and publish an event. If you are auditing an existing interaction, take Dudycz's test: one consumer plus an expected response means you already have a command and should stop pretending. Both sources are marked **Corroborate.** and neither can carry this alone, so an opinion here needs a third voice.

**Event sourcing: cautionary or enthusiastic.** `ms-learn` leads with cost. `milan-jovanovic`'s introduction leads with benefit: reconstructing state at any point, historical data already present when a feature needs it, and named use cases in e-commerce, finance and IoT. ([Jovanović: Introduction to Event Sourcing for .NET Developers](https://www.milanjovanovic.tech/blog/introduction-to-event-sourcing-for-net-developers), 2024-08-31) **Weigh towards `ms-learn`:** it is independent, its page is genuinely recent at 2026-03-27, and it enumerates the exit costs. `oskar-dudycz` settles it, because he sells the workshops and still says a well-done CRUD beats a poorly done event store.

**Modular monolith: default, or fashion.** `architecture.md` and `milan-jovanovic` state it as a default. `mark-seemann` treats it as a swing of the pendulum and recommends physical package separation for teams without functional-programming discipline. `jeremy-miller` is openly dubious it is a panacea. No source in the sweep contradicts the opinion, and two decline to endorse it as stated. That is not grounds to drop it; it is grounds to state it as the cheaper reversible default rather than as consensus.

## Freshness

**Correction carried from the first pass, affecting every `ms-learn` citation.** The roster rule for `ms-learn` is to quote the page's own review date. The page's own review date is the `ms.date` metadata field, which the author sets. The first pass quoted `updated_at`, which is a docset build timestamp and is shared across every page in a bulk republish.

| Page                                                     | First pass claimed | Actual `ms.date` | Author             |
| -------------------------------------------------------- | ------------------ | ---------------- | ------------------ |
| Common web application architectures                     | 2026-07-08         | **2021-12-12**   | `ardalis`          |
| Challenges and solutions for distributed data management | 2023-10-12         | **2018-09-20**   | `jamesmontemagno`  |
| Domain events, design and implementation                 | 2024-01-03         | **2018-10-08**   | `jamesmontemagno`  |
| Applying simplified CQRS and DDD patterns                | 2023-05-27         | **2021-01-13**   | `jamesmontemagno`  |
| Event Sourcing pattern                                   | 2026-08-15         | **2026-03-27**   | `claytonsiemens77` |
| Idempotent Consumer pattern                              | not read           | **2026-08-13**   | `claytonsiemens77` |

The quoted field is demonstrably a build stamp: Event Sourcing and Idempotent Consumer share both `updated_at: 2026-08-15T05:02:00Z` and `git_commit_id: d73cd1632ffa489eff78a16bba4d101c510a1810`. One commit, two pages, one timestamp.

**What follows from the correction.** The microservices e-book material is 2018 and 2021 text, not 2023 and 2024. The first pass's argument that age is not disqualifying still holds, because the reasoning is durable and the DDD citations it rests on are older still, but it now has to hold against 2018. Two consequences carry into any opinion: the e-book is framed for containerised microservices rather than modular monoliths, so every claim above has been read across a boundary the source did not write for; and it predates the modular monolith becoming a named pattern, which is part of why it is silent on slices. Only the two Azure Architecture Center pattern pages are genuinely current.

**On the other sources.** `mark-seemann`'s layering position is 2025-04-01 and his boundary series is 2024, both within tolerance and both C#. His slice remark is 2023-09-18. `jeremy-miller`'s design essays run 2024-04 to 2026-06, all C#. `oskar-dudycz`'s slice post is 2026-08-10 but its examples are TypeScript, which the roster row requires flagging; his event-sourcing posts are 2021 and his internal-and-external-events post is 2023 and uses Marten. `derek-comartin`'s material is 2022 to 2026 and carries no code by design.

Nothing in this topic is preview-gated. All of it is GA guidance.

## What this reveals about the repository

1. **The TODO in `architecture.md` is now dischargeable, and discharging it changes three opinions rather than citing five.** Opinions one, three and five gain corroboration. Opinion two gains corroboration from two marked sources while the one unmarked source reads its central term differently. Opinion four is contradicted by every source swept, from both directions, and its headline should be replaced by the conditional its own body already states. Route through `resolve-research`; the weaving is `harvest-sources` work that decision-log row 139 already anticipated.

2. **Two of the four new sources are not independent of each other, and the roster does not say so.** `jeremy-miller` created Marten; `oskar-dudycz` co-maintains it. Both rows record a conflict of interest with the vendor, and neither records the conflict with the other. Their agreement on the strongest claim in this topic, that event sourcing is a module-level decision, is one voice rather than two. Both are marked **Corroborate.**, so a naive reading would treat them as satisfying each other's marking. They cannot. Worth a roster note on both rows, because the same pair will co-occur on every event-sourcing and messaging topic this repository ever writes.

3. **The `ms-learn` row's citation rule names a date without naming a field, and that let a systematic error through undetected for eleven days.** "Quote the page's own review date" was followed in good faith and produced six wrong dates, one of which became the lead claim of the previous pass. The rule should say `ms.date`, and should say that `updated_at` is a build timestamp. This is a cheap fix with a real error already attached to it.

4. **The `vet-source` candidate list from the first pass is now resolved, and one item needs correcting.**
   - **Mark Seemann, Jeremy D Miller, Derek Comartin:** admitted 2026-09-03. Swept here.
   - **Oskar Dudycz:** admitted 2026-08-24 and left unswept by the first pass. Swept here. One correction to that pass: his "Monolith-First - are you sure?" post is on `architecture-weekly.com`, a Substack, not on the `event-driven.io` domain the roster Source cell names. It is out of scope and no claim here rests on it.
   - **Jimmy Bogard:** activity is now checked, and the answer complicates the candidacy. He is active to 2026-09-01, but the recent output is AutoMapper and MediatR release posts plus promotion of his own commercial Vertical Slice Architecture webinar (2026-09-01 and 2026-07-23). He would therefore carry both the release-notes depth concern that `jeremy-miller`'s marking exists for and a direct commercial interest in precisely the opinion this topic is trying to corroborate. He originated the vertical-slice framing and the deferred domain-events pattern `ms-learn` quotes, so the archive matters; the case for admission is weaker than the first pass assumed.
   - **Kamil Grzybek:** unchanged. Watch list, not admission. Nothing published since 2023-12-05.
   - **Udi Dahan:** unchanged. Watch-listed 2026-08-24, dormant since 2016-02-19, discovery and cross-checking only.

5. **`derek-comartin`'s sponsorship is broader than his roster row describes.** The row says to "flag the sponsorship on anything touching message queues". In this sweep the Particular Software block appeared in the body of every post fetched, including the modular-monolith and coupling posts that have nothing to do with queues. The flag should be unconditional on that source rather than topic-scoped.

6. **The roster gap has moved and shrunk, and what is left of it is a single-source problem.** Saga style, choreography versus orchestration, returned nothing across four sources and the documentation set. Event granularity and event-carried state transfer returned exactly one source, `oskar-dudycz`, who is marked **Corroborate.** and cannot carry them alone. So the gap is no longer "nothing independent covers the inside of a module", which is what the first pass recorded and which is now closed. It is that the messaging vocabulary this repository will need has one voice behind it, and that voice maintains the tooling.

7. **Cross-links to add when these become opinions:** the outbox interaction in [data-access.md](../opinions/data-access.md) and the context-propagation note in [aspnet-core.md](../opinions/aspnet-core.md) both already exist and should be linked rather than restated.
