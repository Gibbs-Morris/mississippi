# Unified Code Generation Developer Experience

## Status

**Decisions Made** → ✅ Architect Review APPROVED | Size: **Large**

**Architect Review:** ✅ APPROVED — See [architect-review.md](architect-review.md)

- **All Phases (1-5):** ✅ Approved — proceed with implementation
- **Cross-project generation:** ✅ Validated via POC using `PrivateAssets="all"`
- **Attribute naming:** 🟡 Advisory — consider `[ClientVisible]`/`[ClientDispatchable]`

## Decisions (Confirmed)

| Decision | Choice | Rationale |
| -------- | ------ | --------- |
| Attribute model | **Separate** (agg vs proj) | SRP; framework should be pluggable |
| DI registration | **Compile-time** (source gen) | No runtime reflection; AOT support |
| Client action generation | **Opt-in** `[GenerateClientAction]` | Explicit control; future RBAC support |
| Client DTO generation | **Opt-in** `[GenerateClientDto]` | Same reasoning; RBAC extensibility |
| Future extensibility | **RBAC properties reserved** | Attributes will carry auth metadata |

## Overview

Design a coherent approach to source generation where defining aggregate state and
projection state with attributes automatically generates:

1. Strongly typed services
2. HTTP APIs
3. Client DTOs (without Orleans dependencies)
4. Client-side Fluxor actions/effects for command dispatch

## Pluggable Architecture

Aggregates and projections are kept as **separate concerns**:

- Users can swap projection backends (Cosmos, SQL, Redis) without affecting
  aggregate infrastructure.
- Each concern opts into client generation independently.
- Future RBAC properties on attributes will control authorization.

## Critical Constraint: Orleans Isolation

**User's Key Concern:** "The biggest concern is the Orleans attributes leaking
into client code and HTTP code, as WASM/ASP.NET/Orleans are 3 different things
running in different pods/envs."

**Solution:** Generated project approach:

- `Cascade.Contracts.Generated` — DTOs without `[Id]`, `[GenerateSerializer]`
- `Cascade.Client.Generated` — Actions/effects without Orleans dependencies
- Build fails if any `.Generated` project references Orleans packages

**Key Architectural Insight:** SignalR sends notifications only (path, entityId,
version). HTTP fetches actual projection data. This separation means client DTOs
only need JSON serialization—no Orleans.

## Spec Files

| File | Purpose |
| ---- | ------- |
| [learned.md](learned.md) | Verified repository facts |
| [rfc.md](rfc.md) | RFC-style design document |
| [attribute-catalog.md](attribute-catalog.md) | Complete attribute inventory |
| [call-chain-mapping.md](call-chain-mapping.md) | Full request-response flow |
| [verification.md](verification.md) | Claims and verification questions |
| [implementation-plan.md](implementation-plan.md) | Step-by-step plan |
| [progress.md](progress.md) | Work log |
| [handoff.md](handoff.md) | Implementation handoff brief |

## Next Steps

All major decisions confirmed. Ready for Phase 1 implementation:

1. Add `[AggregateService]` to `ChannelAggregate` and `ConversationAggregate`
2. Wire generated services in `Cascade.Server`
3. Run validation builds
