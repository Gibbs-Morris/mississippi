# Unified Code Generation Developer Experience

## Status

**Complete (Pending Decision)** | Size: **Large** | Decision Checkpoint: **Yes**

## Overview

Design a coherent approach to source generation where defining aggregate state and projection state with attributes automatically generates:

1. Strongly typed services
2. HTTP APIs
3. Client DTOs (without Orleans dependencies)

## Current State

The repository has two separate generators:

- `AggregateServiceGenerator` - generates services + controllers from `[AggregateService]` on aggregates
- `ProjectionApiGenerator` - generates DTOs + controllers from `[UxProjection]` on projections

Client DTOs in `Cascade.Contracts` are manually maintained and duplicated from domain projections.

## Problem Statement

1. Manual DTO duplication between Domain projections and Contracts (WASM-safe)
2. No unified attribute model across aggregates and projections
3. Orleans dependencies leak if domain types are directly referenced from WASM
4. Service registrations are verbose and repetitive

## Goal

Define aggregate/projection state once with attributes → generate everything needed for server and client layers without dependency leakage.

## Spec Files

| File                       | Purpose                                    |
| -------------------------- | ------------------------------------------ |
| [learned.md](learned.md)   | Verified repository facts                  |
| [rfc.md](rfc.md)           | RFC-style design document                  |
| [verification.md](verification.md) | Claims and verification questions |
| [implementation-plan.md](implementation-plan.md) | Step-by-step plan |
| [progress.md](progress.md) | Work log                                   |
| [handoff.md](handoff.md)   | Implementation handoff brief               |

## Key Decisions Required

1. Where should generated client DTOs live (Contracts project vs a new generated assembly)?
2. Should we consolidate `[AggregateService]` and `[UxProjection]` into a unified attribute model?
3. How should DI registrations be generated (source gen vs runtime reflection)?
