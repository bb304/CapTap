# ADR-0001: Clean Architecture without MediatR

- Status: Accepted
- Date: 2026-08-05

## Context

CapTap’s backend must keep HTTP, business rules, and persistence separable so medication ownership rules and auth logic stay testable without tying use cases to ASP.NET or EF Core. MediatR / CQRS pipelines are common in .NET APIs but add indirection for a domain of this size.

## Decision

Use Clean Architecture with four projects:

- `CapTap.Domain` — entities, enums, domain exceptions (no infrastructure dependencies)
- `CapTap.Application` — services, DTOs, FluentValidation, repository/port interfaces
- `CapTap.Infrastructure` — EF Core, repositories, Argon2, JWT minting, SMTP, OpenFDA
- `CapTap.Api` — composition root, controllers, middleware, JWT bearer validation

Controllers call `I*Service` interfaces directly. No MediatR. An empty `Application/Behaviors/` folder may exist from scaffolding but is unused.

## Consequences

- **Pros:** Clear dependency rule; Application tests use in-memory fakes without Infrastructure; easy to explain in interviews.
- **Cons:** More explicit constructor injection than a single mediator pipeline; cross-cutting concerns (validation, logging) are called explicitly in services rather than pipeline behaviors.
- **Follow-up:** Revisit MediatR only if use-case count and shared behaviors justify it.
