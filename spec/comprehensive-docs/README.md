# Comprehensive Documentation Update

**Status**: In Progress  
**Size**: Large  
**Approval Checkpoint**: No (documentation only, no code changes)

## Overview

Systematic documentation of Mississippi framework concepts in the Docusaurus site. Each topic will be documented by:
1. Scanning source code to understand the concept
2. Looking at usage patterns in samples
3. Building comprehensive documentation pages

## Topics to Document

### Reservoir (Client-Side State Management)

| # | Topic | Status | Doc Path |
|---|-------|--------|----------|
| 1 | Reservoir Overview | ✅ Exists | `docs/reservoir/index.md` |
| 2 | Actions | ✅ Exists | `docs/reservoir/actions.md` |
| 3 | Reducers | ✅ Exists | `docs/reservoir/reducers.md` |
| 4 | Effects | ✅ Exists | `docs/reservoir/effects.md` |
| 5 | Store | ✅ Exists | `docs/reservoir/store.md` |
| 6 | State & Registration | 🔄 Needs expansion | `docs/reservoir/state.md` |

### Platform (Event Sourcing Core)

| # | Topic | Status | Doc Path |
|---|-------|--------|----------|
| 7 | Domain Model (Aggregates) | 🔄 Needs expansion | `docs/platform/aggregates.md` |
| 8 | Commands & Command Handlers | ⏳ Pending | `docs/platform/commands.md` |
| 9 | Domain Events & Event Reducers | ⏳ Pending | `docs/platform/events.md` |
| 10 | Projections | 🔄 Needs expansion | `docs/platform/ux-projections.md` |
| 11 | Brooks (Writer/Reader) | 🔄 Needs expansion | `docs/platform/brooks.md` |
| 12 | Custom Event Storage Provider | ⏳ Pending | `docs/platform/custom-event-storage.md` |
| 13 | Snapshots | ⏳ Pending | `docs/platform/snapshots.md` |
| 14 | Custom Snapshot Storage Provider | ⏳ Pending | `docs/platform/custom-snapshot-storage.md` |

### Aqueduct (Standalone Orleans Stream Processing)

| # | Topic | Status | Doc Path |
|---|-------|--------|----------|
| 15 | Aqueduct Overview | ⏳ Pending | `docs/platform/aqueduct.md` |
| 16 | Aqueduct Standalone Usage | ⏳ Pending | `docs/platform/aqueduct-standalone.md` |
| 17 | Aqueduct Registration | ⏳ Pending | (in aqueduct.md) |

### Inlet (Client-Server Bridge with Source Generation)

| # | Topic | Status | Doc Path |
|---|-------|--------|----------|
| 18 | Inlet Overview | 🔄 Needs expansion | `docs/platform/inlet.md` |
| 19 | Source Generators (Client/Server/Silo) | ⏳ Pending | `docs/platform/inlet-generators.md` |
| 20 | WASM Client Integration | ⏳ Pending | `docs/platform/inlet-wasm.md` |
| 21 | Live Updates & SignalR | ⏳ Pending | `docs/platform/inlet-live-updates.md` |

### SDK Reference Packages

| # | Topic | Status | Doc Path |
|---|-------|--------|----------|
| 22 | SDK Overview | ⏳ Pending | `docs/platform/sdk.md` |
| 23 | Sdk.Client | ⏳ Pending | (in sdk.md) |
| 24 | Sdk.Server | ⏳ Pending | (in sdk.md) |
| 25 | Sdk.Silo | ⏳ Pending | (in sdk.md) |

### Architecture & Design Guidance

| # | Topic | Status | Doc Path |
|---|-------|--------|----------|
| 26 | Design Patterns & Anti-Patterns | ⏳ Pending | `docs/platform/design-patterns.md` |
| 27 | Aggregate Design (Orleans Threading) | ⏳ Pending | (in design-patterns.md) |
| 28 | UX Projection Design (Atomic/Composable) | ⏳ Pending | (in design-patterns.md) |
| 29 | Scaling & Bottleneck Avoidance | ⏳ Pending | (in design-patterns.md) |

## Links

- [Learned Facts](learned.md)
- [RFC](rfc.md)
- [Verification](verification.md)
- [Implementation Plan](implementation-plan.md)
- [Progress Log](progress.md)
