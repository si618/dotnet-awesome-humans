---
targets: [net10.0, csharp-14]
last-reviewed: 2026-08-24
sources: [ms-learn, milan-jovanovic]
---

# Module boundaries and events

Research for the open TODO in [architecture.md](../opinions/architecture.md), which asks for corroboration from an independent Tier 1 source and then three extensions: messaging between modules, transactional boundaries, and when a module has earned a process boundary. Event sourcing and event-driven architecture are added to the same topic because they turn out to be the mechanism the three extensions all reach for.

**The short answer: `ms-learn` corroborates the half of `architecture.md` that argues about deployment, and is silent on the half that argues about code layout.** The modular-monolith-first opinion is independently confirmed, and confirmed by a page reviewed as recently as 2026-07-08. The vertical-slice opinions are not: Microsoft's architecture e-books never use the term, and where they do prescribe an internal structure they prescribe solution-level Clean Architecture, which is what the repository's fourth opinion argues against. The TODO is therefore **half-dischargeable today**. The other half stays blocked until an independent source covers the inside of a module, and item 4 below says what the 2026-08-24 admission of `oskar-dudycz` does and does not change about that.

On the extensions the material is strong enough to write opinions from. On event sourcing the answer is a clear "rarely, and never by default".

## Sources swept

| id                | Tier            | Used for                                                                    |
| ----------------- | --------------- | --------------------------------------------------------------------------- |
| `ms-learn`        | 1, independent  | Corroboration, transactional boundaries, process boundaries, event sourcing |
| `milan-jovanovic` | 1, COI in Notes | Module communication patterns, event sourcing framing                       |
| `andrew-lock`     | 1, independent  | Swept, nothing on this ground (negative result, recorded below)             |

Non-roster sources named at the end as `vet-source` candidates are flagged **unvetted** and no claim above rests on them.

## 1. Corroboration: what `ms-learn` does and does not confirm

**Confirmed: start monolithic, and treat the process boundary as a later choice.** The independent Tier 1 statement the TODO asked for exists, and it is close to verbatim:

> Early in the development of an application, you might not have a clear idea where the natural functional boundaries are. As you develop a minimum viable product, the natural separation might not yet have emerged. [...] You might start by creating a monolithic application, and later separate some features to be developed and deployed as microservices.

The same page adds the cost side: "If you can't deliver independent feature slices of the application, separating it only adds complexity", and notes that separating processes forces you to build "event bus handling, message resiliency and retries, eventual consistency, and more". ([Microsoft Learn: Common web application architectures](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures), reviewed 2026-07-08)

**Confirmed: boundaries are drawn from the domain, not from technology.** "Focus on the application's logical domain models and related data. Try to identify decoupled islands of data and different contexts within the same application." ([Microsoft Learn: Challenges and solutions for distributed data management](https://learn.microsoft.com/dotnet/architecture/microservices/architect-microservice-container-applications/distributed-data-management), reviewed 2023-10-12)

**Confirmed in spirit: do not apply the same heavy pattern everywhere.** The CQRS chapter keeps the query side deliberately outside the domain patterns, "independent from restrictions and constraints coming from DDD patterns that only make sense for transactions and updates". ([Microsoft Learn: Applying simplified CQRS and DDD patterns in a microservice](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/apply-simplified-microservice-cqrs-ddd-patterns), reviewed 2023-05-27)

**Not confirmed, and partly contradicted: the vertical slice opinions.** Three of the five opinions in `architecture.md` describe the inside of a module. `ms-learn` is silent on two of them and contradicts the third. It never uses "vertical slice" or "modular monolith" and offers no position on slices versus modules, which is the ground of the second and third opinions. On the fourth, its worked example organises a solution by layer into Application Core, Infrastructure and UI projects, and it calls that arrangement "the most appropriate way to structure non-trivial monolithic applications": a solution-wide prescription where `architecture.md` argues for a per-slice one. The conflict is weighed in [Where the sources disagree](#where-the-sources-disagree) below.

**Negative result worth recording:** `andrew-lock` was swept as the other obvious independent Tier 1 candidate and carries nothing on module boundaries, messaging patterns or the outbox. His background-work catalogue is Quartz.NET and `IHostedService` mechanics, which is hosting rather than architecture. Do not re-sweep him for this topic.

## 2. Messaging between modules

**Two styles, and the choice is a coupling decision rather than a technology one.** A module exposes either a public interface or a message contract, and whichever it exposes becomes the thing other modules depend on.

- **Synchronous, via a public interface resolved through DI.** "Method calls are in-memory, so they are fast, easy to implement, and add no indirection." The cost is that the calling module fails when the called module does. ([Jovanović: Modular monolith communication patterns](https://milanjovanovic.tech/blog/modular-monolith-communication-patterns), 2023-08-05)
- **Asynchronous, via messages.** "Messaging gives you loose coupling and high availability, since the receiving module does not need to be available when a message is sent." The cost is infrastructure, and a broker that becomes a single point of failure. (same source)

**The rule that matters more than either: do not build chains of synchronous calls across boundaries.** `ms-learn` is blunt about where that ends up:

> If your internal microservices are communicating by creating chains of HTTP requests as described, it could be argued that you have a monolithic application, but one based on HTTP between processes instead of intra-process communication mechanisms.

It lists three failure modes: blocking latency that compounds along the chain, coupling that makes autonomy impossible, and a chain that fails entirely when any link fails. ([Microsoft Learn: Challenges and solutions for distributed data management](https://learn.microsoft.com/dotnet/architecture/microservices/architect-microservice-container-applications/distributed-data-management))

This reads across to a modular monolith directly. A chain of in-process interface calls between modules has the coupling problem and the failure-cascade problem without the latency one, which makes it the cheapest version of the mistake to make and the hardest to notice.

**Domain events and integration events are different things and the distinction is load-bearing.** `ms-learn` separates them cleanly:

- A **domain event** is "something that happened in the domain that you want other parts of the same domain (in-process) to be aware of". It is dispatched in-process, and it may be synchronous.
- An **integration event** propagates a committed change outward, and "should occur only if the entity is successfully persisted, otherwise it's as if the entire operation never happened". Integration events "must be based on asynchronous communication".

([Microsoft Learn: Domain events, design and implementation](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation), reviewed 2024-01-03)

The practical consequence for a modular monolith: a domain event belongs inside one module and must not cross a module boundary. What crosses the boundary is an integration event, and the moment it does, the receiving module needs the delivery guarantees that come with it.

**The outbox is what makes an integration event honest.** The pattern is "a transactional database table as a message queue"; the event row is written "atomically with the original database operation, using a local transaction against the same database", so a rollback takes the message with it. Both sources describe it the same way. ([Microsoft Learn: Challenges and solutions for distributed data management](https://learn.microsoft.com/dotnet/architecture/microservices/architect-microservice-container-applications/distributed-data-management); [Jovanović: Modular monolith communication patterns](https://milanjovanovic.tech/blog/modular-monolith-communication-patterns))

Two existing files already touch this and should be cross-linked rather than duplicated. [data-access.md](../opinions/data-access.md) warns that `ExecuteUpdateAsync`/`ExecuteDeleteAsync` bypass the change tracker, so "`SaveChanges`-based audit and outbox logic doesn't run" — which is precisely how an outbox silently stops working. [aspnet-core.md](../opinions/aspnet-core.md) already covers propagating OpenTelemetry context across outbox tables and queues.

## 3. Transactional boundaries

**The aggregate is the consistency boundary, and one transaction should normally cover one aggregate.** `ms-learn` quotes Evans directly: "Any rule that spans Aggregates will not be expected to be up-to-date at all times", and Vernon: "if executing a command on one aggregate instance requires that additional business rules execute on one or more aggregates, use eventual consistency". The reason given is lock contention at scale. ([Microsoft Learn: Domain events, design and implementation](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation))

**But `ms-learn` does not follow that rule itself, and says so.** Its own recommendation for EF Core against a relational database is the opposite, and it is explicit about the trade:

> the initial deferred approach—raising the events before committing, so you use a single transaction—is the simplest approach when using EF Core and a relational database. It's easier to implement and valid in many business cases.

The mechanism is a one-line decision with a large consequence. Domain events accumulate on the entity and are dispatched either side of `SaveChangesAsync`:

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

The sample keeps the eShopOnContainers shape. `DispatchDomainEventsAsync` there is a hand-rolled extension method in that repository, not a MediatR API, and `architecture.md` still treats mediator libraries as unsourced, so the dispatch call is the mechanism to copy rather than the library. `ms-learn` names the cost of the other choice plainly: "if there's an issue and the event handlers cannot commit their side effects, you'll have inconsistencies between aggregates", recoverable only by storing the events and running a reconciliation batch.

**A defensible opinion for this repository:** inside a module, dispatch domain events before `SaveChanges` and take the single transaction, because a modular monolith has one database and the lock-contention argument is a scale problem most modules never reach. Across modules, never share a transaction; publish an integration event through an outbox and accept eventual consistency. That draws the transactional boundary at exactly the same line the repository already draws the ownership boundary, which is the property worth having.

`ms-learn` supports the second half without qualification: "No microservice should ever include tables/storage owned by another microservice in its own transactions, not even in direct queries", and two-phase commit is ruled out as "against microservices principles" and unsupported by most NoSQL stores.

## 4. When a module has earned a process boundary

`ms-learn` gives three signals, and all three are observations about an existing module rather than predictions.

- **Independent scaling that cloning cannot serve.** "An application might not yet need to scale features independently. Many applications, when they need to scale beyond a single instance, can do so through the relatively simple process of cloning that entire instance." The worked example is an e-commerce catalogue read path being browsed far more than the payment pipeline is used. Until one module's load profile genuinely diverges, the process boundary buys nothing. ([Microsoft Learn: Common web application architectures](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures))
- **Independent delivery.** "If you can't deliver independent feature slices of the application, separating it only adds complexity." A module that always ships in the same release as its neighbour has not earned anything. (same source)
- **The chattiness test, which runs the other way too.** After identifying a candidate service, validate "there are no chatty calls between services, and if splitting functionality into two services causes them to be overly chatty, it might indicate those functions belong in the same service". `ms-learn` extends this into a merge signal: if the design "involves constantly aggregating information from multiple microservices for complex queries, it might be a symptom of a bad design [...] Having this problem often might be a reason to merge microservices." ([Microsoft Learn: Challenges and solutions for distributed data management](https://learn.microsoft.com/dotnet/architecture/microservices/architect-microservice-container-applications/distributed-data-management))

**The cheapest predictor is already in the repository's grasp.** A module that already talks to its neighbours only through asynchronous messages can be extracted without rewriting its communication, which is Jovanović's stated reason for preferring messaging early: modules that communicate that way are "much easier" to migrate. A module still reached by direct interface calls has not been prepared, and extracting it is a rewrite rather than a move. ([Jovanović: Modular monolith communication patterns](https://milanjovanovic.tech/blog/modular-monolith-communication-patterns))

## 5. Event sourcing and event-driven architecture

**Event-driven architecture between modules: yes, and the repository is already most of the way to recommending it.** Integration events over an outbox, with idempotent consumers, is the shape both sources describe. Nothing in the sweep argues against it for a modular monolith, and `ms-learn` treats eventual consistency through publish-subscribe as the default answer to cross-boundary consistency.

**Event sourcing as a persistence model: no, other than in narrowly chosen places.** This is the strongest anti-recommendation found in the whole sweep, and it is recent. Microsoft leads the pattern page with a warning rather than a description:

> Event sourcing is a complex pattern that introduces significant trade-offs. It changes how you store data, handle concurrency, evolve schemas, and query state. It's costly to migrate to or from an event sourcing solution, and after you adopt the pattern, it constrains future design decisions in the parts of the system that use it. [...] For most systems and most parts of a system, traditional data management is sufficient.

The page lists five situations where it does not fit: straightforward CRUD without an audit requirement; prototypes and short-lived systems; anything needing real-time consistent views; mostly static reference data; and teams without event-driven experience, where "adopting it without the foundational knowledge increases the risk of antipatterns that are costly to reverse". ([Microsoft Learn: Event Sourcing pattern](https://learn.microsoft.com/azure/architecture/patterns/event-sourcing), reviewed 2026-08-15)

The costs it enumerates are concrete and mostly permanent: replay cost requiring snapshots, schema evolution needing upcasters or tolerant deserialisation, no SQL-shaped querying, at-least-once delivery forcing idempotent consumers, and a direct collision between an immutable log and the right to erasure, for which the mitigations are storing personal data outside the log or crypto-shredding by per-subject key.

Two details worth keeping if this ever becomes an opinion, because both are commonly got wrong:

- **An event store is not a broker.** "Message brokers such as Apache Kafka typically lack per-entity stream queries and optimistic concurrency. They work well as a distribution layer [...] but they aren't a substitute for an event store."
- **Design events for intent, not for resulting state.** "an event that records _two seats were reserved_ is more valuable than an event that records _remaining seats changed to 42_." State-shaped events "reduce the event store to a change log that has no business meaning".

The page's own closing advice is the opinion this repository would write: "Event sourcing doesn't have to be an all-or-nothing decision for your entire system. Apply it selectively to the parts of your system that it benefits the most, such as a payment ledger or order-processing pipeline."

**Scope note on this citation.** The Event Sourcing pattern page sits under the Azure Architecture Center, inside the `ms-learn` scope widened on 2026-08-24 with this topic as the trigger. The roster row records the widening and the two citation rules that come with it, and the citation above follows both.

The widening also closes a gap this topic would otherwise leave open. Idempotent consumption is mandatory under at-least-once delivery and no page read here covers its .NET mechanics; the Azure Architecture Center carries a dedicated Idempotent Consumer pattern, alongside CQRS, Publisher-Subscriber and Competing Consumers. Those are now citable and should be read before the messaging opinion is written.

## Where the sources disagree

**Clean Architecture: solution-wide or per slice.** `ms-learn` calls layered Clean Architecture "the most appropriate way to structure non-trivial monolithic applications" and organises its reference solution that way. `architecture.md` says to apply it per slice, because forcing every slice through identical layers gives you "four projects to change a `GET`".

Weighing them: the `ms-learn` page is describing a monolith that has not been modularised, where layers are the only boundary available, and its advice is sound for that case. It is not addressing a codebase that already has module boundaries doing the encapsulation work. The repository's position survives, but it should be stated as a refinement of the `ms-learn` position rather than as agreement with it, and the file should stop implying that the layering question is settled in the community. Note also that this page is authored by Steve Smith, who maintains the Clean Architecture solution template it links to, so it is not a disinterested source on the question.

**Event sourcing: cautionary or enthusiastic.** Microsoft leads with the cost. Jovanović's introduction leads with the benefits: reconstructing state at any point, having historical data already present when a new feature needs it, and named use cases in e-commerce, finance and IoT. ([Jovanović: Introduction to Event Sourcing for .NET Developers](https://www.milanjovanovic.tech/blog/introduction-to-event-sourcing-for-net-developers), 2024-08-31) The two are not contradictory, but the emphasis differs sharply, and the repository's existing note in `architecture.md` about both current sources selling courses over exactly this ground applies here with force. **Weigh towards `ms-learn`:** it is independent, it is more recent on this specific page, and it is the one enumerating the exit costs.

## Freshness

Most of the `ms-learn` material is the 2018-era .NET microservices e-book, carrying review dates of 2023-10-12 and 2024-01-03. The reasoning is durable and the DDD citations are older still, so age is not disqualifying. Two things follow from it: the e-book is framed for containerised microservices rather than modular monoliths, so every claim above has been read across a boundary the source did not write for, and it predates the modular monolith becoming a named pattern, which is part of why it is silent on slices. The two most recent pages, Common web application architectures (2026-07-08) and Event Sourcing (2026-08-15), carry the most weight.

Nothing in this topic is preview-gated. All of it is GA guidance.

## What this reveals about the repository

1. **The TODO in `architecture.md` cannot be fully discharged from the current roster, and should be rewritten to say which half is blocked.** Corroboration exists for the modular-monolith-first opinion and does not exist for the vertical-slice opinions. Leaving one undifferentiated TODO hides that.
2. **There is enough here for three new opinions**, on inter-module communication, transactional boundaries, and the process-boundary signals. All three rest on an unmarked independent Tier 1 source, which is what the file currently lacks. Route through `resolve-research`.
3. **The roster's architecture gap is narrower than it was, but not closed.** Even after the 2026-08-24 admission, nothing independent covers the inside of a module. The two specialists over this ground, `milan-jovanovic` and `oskar-dudycz`, both carry a recorded conflict of interest, and the documentation set stops at the module boundary. Udi Dahan was raised alongside `oskar-dudycz` and watch-listed the same day: his blog has been dormant since 2016-02-19, and `ms-learn` already cites him second-hand on domain events, so the archive is for cross-checking only. Remaining candidates for `vet-source`, all **unvetted**, with activity checked on 2026-08-24 and everything else left for the vetting itself:
   - **Mark Seemann** ([blog.ploeh.dk](https://blog.ploeh.dk/)): the strongest candidate on the criteria themselves: publishing since 2009, active to 2026-08-13, independent, and deep. Off-centre for this topic, so admit him for the architecture and design gap generally rather than to close this TODO.
   - **Jeremy D Miller** ([jeremydmiller.com](https://jeremydmiller.com/)): publishing since roughly 2004 and active to 2026-07-15. Maintains Wolverine and Marten, which is messaging and event sourcing in one source. Two things for the vetting to weigh: the conflict of interest is direct, and much of the recent output is release-shaped, which is the "paraphrased release notes" depth concern rather than the independence one.
   - **Jimmy Bogard:** originator of the vertical slice framing and of the deferred domain-events pattern `ms-learn` quotes above, so currently reached only second-hand through Microsoft's citations. Activity not checked.
   - **Kamil Grzybek** ([kamilgrzybek.com](https://www.kamilgrzybek.com/blog/series/modular-monolith)): **watch list, not admission.** The Modular Monolith series is the best-targeted writing found for this TODO, and the blog has published nothing since 2023-12-05. That is the same dormancy that blocks `chris-sainty`, and the 2026-08-23 narrowing removes the repository-activity escape. Worth watch-listing so the archive stays available for cross-checking, and re-vetting if he writes again.
   - **Derek Comartin** ([codeopinion.com](https://codeopinion.com/)): active to 2026-08-19 with on-topic material, but the posts are companion text for YouTube videos rather than standalone articles. The 2026-08-23 narrowing to published writing puts the videos themselves out of scope, so the question for vetting is whether individual posts stand on their own as writing.
4. **The uncorroborated half needs a second sweep, and that sweep cannot close the TODO by itself.** `oskar-dudycz` was admitted on 2026-08-24 (Tier 1, marked **Corroborate.**, conflict of interest recorded) off the back of this topic. His "Vertical slices, their ownership and external dependencies" of 2026-08-10 lands directly on the two opinions `ms-learn` is silent about, so he is the obvious next sweep. He was not swept here, and the slice opinions stay uncorroborated in this topic. Two limits apply when the sweep happens. The marking means he can never carry an opinion alone, and the TODO asks for an independent source, which a Marten maintainer who sells event sourcing workshops is not. The sweep can add a second voice to the slice opinions; the independence requirement still waits on one of the candidates in item 3. Note also that his vertical-slices example is TypeScript.
5. **Cross-links to add when these become opinions:** the outbox interaction in [data-access.md](../opinions/data-access.md) and the context-propagation note in [aspnet-core.md](../opinions/aspnet-core.md) both already exist and should be linked rather than restated.
