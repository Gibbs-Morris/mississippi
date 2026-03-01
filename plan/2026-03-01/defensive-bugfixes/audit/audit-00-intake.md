# 00 — Intake

## Objective

Investigate and fix 7 potential bugs identified across the Mississippi framework's core abstractions, storage, and Reservoir (Redux-like store) subsystems. Each fix should be the smallest safe edit, accompanied by comprehensive tests.

## Bugs

| # | Bug | Severity | Module |
|---|-----|----------|--------|
| 1 | Composite key structs returning null from `ToString()`/implicit operator when default-constructed | High | Brooks.Abstractions, DomainModeling.Abstractions, Aqueduct.Abstractions, Tributary.Abstractions |
| 2 | Store listener exceptions breaking entire dispatch chain | High | Reservoir.Core |
| 3 | EventTypeRegistry / SnapshotTypeRegistry allowing duplicate name registrations | Medium | DomainModeling.Runtime |
| 4 | `default(OperationResult)` representing failure instead of success | Medium | DomainModeling.Abstractions |
| 5 | `BrookAsyncReaderKey.Parse(null)` throwing wrong exception type | Low | Brooks.Abstractions |
| 6 | `CosmosRetryPolicy` accepting negative `maxRetries` | Low | Common.Runtime.Storage.Cosmos |
| 7 | `default(BrookPosition)` ambiguity (Value=0) | Doc-only | Brooks.Abstractions |

## Non-Goals

- No large refactors; each fix is the smallest safe edit.
- No new public APIs beyond what's needed to fix the bugs.
- No backwards-compatibility shims (pre-1.0 per GitVersion `next-version: 0.0.1`).

## Constraints

- Zero warnings policy (no `NoWarn`, `#pragma`, `[SuppressMessage]`).
- Central Package Management — no `Version` in `PackageReference`.
- Must pass build, cleanup, unit tests, and mutation tests for Mississippi projects.
- C# 14 / .NET 10 SDK (LangVersion 14.0, `global.json` SDK 10.0.102).

## Assumptions

- `field` keyword (C# 14) is available for property backing field access.
- Pre-1.0 means breaking changes to public API (e.g., changing `OperationResult.Success` semantics) are permitted.
- Existing tests may need updates when behavior changes (e.g., registry now throws on duplicate).

## Open Questions

- Q1: Should composite key structs use `field` keyword with `?? string.Empty` or a different null-safety mechanism?
- Q2: For registry duplicate detection, should it throw or silently skip (current TryAdd behavior)?
- Q3: For `OperationResult`, should `default` be success (requires IsDefault sentinel) or should we keep current semantics and document?
