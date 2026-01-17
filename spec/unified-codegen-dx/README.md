# Unified Code Generation Developer Experience

## Status

**Complete (Pending Decision)** | Size: **Large** | Decision Checkpoint: **Yes**

## Overview

Design a coherent approach to source generation where defining aggregate state and
projection state with attributes automatically generates:

1. Strongly typed services
2. HTTP APIs
3. Client DTOs (without Orleans dependencies)
4. Client-side Fluxor actions/effects for command dispatch

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

## Current State

The repository has two separate generators:

- `AggregateServiceGenerator` — generates services + controllers from `[AggregateService]`
- `ProjectionApiGenerator` — generates DTOs + controllers from `[UxProjection]`

Client DTOs in `Cascade.Contracts` are manually maintained and duplicated from domain
projections. Client-side command dispatch is done via raw `HttpClient.PostAsJsonAsync`
calls with manually constructed URLs.

## Problem Statement

1. Manual DTO duplication between Domain projections and Contracts (WASM-safe)
2. No unified attribute model across aggregates and projections
3. Orleans dependencies leak if domain types are directly referenced from WASM
4. Service registrations are verbose and repetitive (332 lines, 80+ calls)
5. Client command dispatch is manual HTTP calls without type safety

## Goal

Define aggregate/projection state once with attributes → generate everything needed
for server and client layers without dependency leakage.

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

## User Preferences (Confirmed)

1. **Generated project approach** — DTOs and actions go into separate `.Generated` projects
2. **Strict Orleans isolation** — No Orleans packages in WASM/client projects
3. **SignalR for notifications, HTTP for data** — Keep this separation

## Remaining Decisions

1. Should `[AggregateService]` and `[UxProjection]` share a unified attribute model?
2. How should DI registrations be generated (source gen vs runtime reflection)?
3. Should client-side command actions be generated via opt-in `[GenerateClientAction]` attribute?
