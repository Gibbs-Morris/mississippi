# Unified Code Generation Developer Experience

## Status

**Decisions Made** → ✅ Architect Review APPROVED | Size: **Large**

**Architect Review:** ✅ APPROVED — See [architect-review.md](architect-review.md)

- **All Phases (0-5):** ✅ Approved — proceed with implementation
- **Cross-project generation:** ✅ Validated via POC using `PrivateAssets="all"`
- **Attribute naming:** ✅ Aligned — `Generate*` / `Define*` convention adopted

## Decisions (Confirmed)

| Decision | Choice | Rationale |
| -------- | ------ | --------- |
| Attribute model | **Separate** (agg vs proj) | SRP; framework should be pluggable |
| Attribute naming | **Generate*/Define*** | Explicit intent; Orleans-aligned |
| DI registration | **Compile-time** (source gen) | No runtime reflection; AOT support |
| Client action generation | **Opt-in** `[GenerateClientAction]` | Explicit control; future RBAC support |
| Client DTO generation | **Opt-in** `[GenerateClientDto]` | Same reasoning; RBAC extensibility |
| Future extensibility | **RBAC properties reserved** | Attributes will carry auth metadata |
| Project naming | **Mississippi.\<Area\>.\<Runtime\>** | Enforces boundary separation |

## Overview

Design a coherent approach to source generation where defining aggregate state and
projection state with attributes automatically generates:

1. Strongly typed services
2. HTTP APIs
3. Client DTOs (without Orleans dependencies)
4. Client-side Reservoir actions/effects for command dispatch

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
| [naming-taxonomy.md](naming-taxonomy.md) | Attribute and project naming conventions |
| [attribute-catalog.md](attribute-catalog.md) | Complete attribute inventory |
| [call-chain-mapping.md](call-chain-mapping.md) | Full request-response flow |
| [verification.md](verification.md) | Claims and verification questions |
| [implementation-plan.md](implementation-plan.md) | Step-by-step plan |
| [progress.md](progress.md) | Work log |
| [handoff.md](handoff.md) | Implementation handoff brief |
| [architect-review.md](architect-review.md) | Principal architect review |

## Next Steps

All major decisions confirmed. Ready for Phase 0 implementation:

1. **Phase 0:** Add `Generate*`/`Define*` attributes with legacy shims
2. **Phase 1:** Add `[GenerateAggregateService]` to Cascade aggregates
3. **Phase 2:** Verify `[GenerateProjectionApi]` produces correct DTOs
4. Continue through Phases 3-5 per [implementation-plan.md](implementation-plan.md)
