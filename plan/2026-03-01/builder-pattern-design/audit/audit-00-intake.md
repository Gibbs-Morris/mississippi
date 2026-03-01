# 00 Intake

## Objective
Design a repo-wide builder-pattern architecture that improves developer experience by replacing complex direct `IServiceCollection` wiring with explicit fluent builders for three host surfaces and core domain features:

- `IClientBuilder`
- `IGatewayBuilder`
- `IRuntimeBuilder`
- `IAggregateBuilder`
- `ISagaBuilder`
- `IUxProjectionBuilder`
- `IFeatureStateBuilder` (Reservoir/Redux)

The immediate scope is design-first with evidence-backed planning, then documentation under `docs/Docusaurus/docs/` in a clearly marked “proposed / future design patterns” area (not active/live guidance yet).

## Non-goals
- Implementing production runtime behavior changes in this planning phase.
- Replacing existing registration APIs immediately across all packages.
- Publishing the proposal as current recommended usage before validation.

## Constraints
### User constraints
- Pull latest `main` before starting (done).
- Ask many design questions before locking architecture.
- Emphasize strong developer experience and reduced DI complexity.
- Prototype + tests can be used to prove concept before final docs strategy is decided.

### Repository constraints
- Existing architecture is extension-method DI-first (`AddXyz(...)`) and source-generator-driven composition.
- Service registration must remain synchronous and options-driven.
- Public contract changes should prefer `.Abstractions` projects when introducing stable API surfaces.
- Docs must follow Docusaurus + markdownlint + documentation structure rules.

## Initial assumptions (to validate)
- Assumption A1: New builder interfaces likely belong in `*.Abstractions` projects and concrete builders in runtime/client/gateway implementation projects.
- Assumption A2: Builders should initially wrap existing `AddXyz(...)` registration primitives (not replace internals immediately).
- Assumption A3: “Proposed/future” docs should live in a dedicated section under `docs/Docusaurus/docs/` with explicit non-live framing.
- Assumption A4: Prototype code may live temporarily and be removed before final merge, but this must be reconciled with repository quality gates and review traceability.

## Open questions
- Should builder APIs be additive wrappers over current registration APIs or become the new primary API surface quickly?
- Should generated registration methods remain directly visible to users or be consumed primarily through builders?
- How strict should compile-time guardrails be (required call ordering, missing registration failures)?
- Which host startup patterns must be first-class (Aspire AppHost, plain ASP.NET host, Blazor WASM/Server)?
- What compatibility expectations apply for existing sample applications and docs?

## CoV
- Claim: Work must be planning-first and question-heavy.
  - Evidence: user request in current conversation.
  - Confidence: High.
- Claim: Branch must be synced before planning.
  - Evidence: user request + successful `git pull --ff-only origin main` execution.
  - Confidence: High.
- Claim: Builder-pattern design must align to existing registration conventions, not invent unrelated architecture.
  - Evidence: `.github/instructions/service-registration.instructions.md`; `.github/instructions/framework-patterns.instructions.md`.
  - Confidence: High.