# Cascade Chat Sample – Agile Tracker

**Goal**: Build a Slack-style chat application using the Mississippi event-sourcing framework, adding real-time projection subscriptions to the framework and validating with a Blazor Server sample.

## Phase Overview

| Phase | Focus | Status |
| ------- | ------- | -------- |
| [Phase 1](./phase-1-framework/README.md) | Framework – Real-Time Projection Subscriptions | ✅ Complete |
| [Phase 2](./phase-2-domain/README.md) | Domain – Cascade Chat Aggregates | ✅ Complete |
| [Phase 3](./phase-3-infrastructure/README.md) | Infrastructure – Aspire & Blazor Host | ✅ Complete |
| [Phase 4](./phase-4-ux/README.md) | UX – Blazor Components | ✅ Complete |
| [Phase 5](./phase-5-e2e-tests/README.md) | E2E Tests (L2) – Playwright | ✅ Complete |
| [Phase 6](./phase-6-review/README.md) | Implementation Review | ⬜ Not Started |
| [Phase 7](./phase-7-ripples/README.md) | Ripples – State Management Framework | 🔵 Design Complete |

## Completion Criteria

- [x] All framework real-time subscription infrastructure implemented with L0 tests
- [x] Cascade domain aggregates and projections complete with L0 tests
- [x] Aspire AppHost orchestrating Azurite + Cosmos Emulator
- [x] Blazor Server app with real-time message updates
- [x] Playwright L2 tests validating multi-user scenarios
- [ ] Implementation review completed (Phase 6)
- [ ] Ripples state management framework implemented (Phase 7)
- [ ] `./go.ps1` passes for both mississippi.sln and samples.sln

## Key Decisions

1. **Aspire Version**: 13.1.0 (latest stable, .NET 9 compatible)
2. **Custom Orleans Backplane (Build This)**: Build our own Orleans-based SignalR backplane inspired by [SignalR.Orleans](https://github.com/OrleansContrib/SignalR.Orleans) patterns. All stateful grains (`UxClientGrain`, `UxGroupGrain`, `UxServerDirectoryGrain`) are **aggregate grains** with persisted state. Grains can push directly to clients via `HubContext<THub>`. No external dependencies (Redis, Azure SignalR). See [Phase 1 Integration Strategy](./phase-1-framework/00-signalr-orleans-integration.md).
3. **Orleans Stream as Hop (Fallback)**: If custom backplane implementation encounters blockers, fall back to per-connection output streams with a bridge service. Same code path works cohosted or distributed.
4. **One Grain Per Connection**: Each SignalR connection gets one `UxProjectionSubscriptionGrain` (keyed by ConnectionId) that tracks projection subscriptions.
5. **Reconnect Strategy**: Client maintains subscription list; on reconnect, calls `Resubscribe(subscriptionList)` to restore all subscriptions atomically.
6. **Real-Time Flow**: Version notifications via SignalR → HTTP GET for data (no large objects over WebSocket).
7. **Framework-First**: Maximize reusable framework code, minimize sample-specific code. If it can be generic, it goes in the framework.
8. **Command Dispatcher**: Standard framework pattern for sending commands from Blazor UX to aggregate grains with consistent error handling and optimistic UI support.
9. **Atomic Design for Blazor**: Components follow atomic design (atoms → molecules → organisms → templates → pages). State flows down, commands flow up.
10. **Ripples Library**: Mississippi.Ripples is a Redux-like state management library with built-in backend integration for both Blazor Server and Blazor WebAssembly.
11. **Dual Hosting**: Same `IRipple<T>` interface works in Server (direct grain) and WASM (HTTP + SignalR).
12. **Composable Projections**: List projections contain only IDs; detail projections load per-row via `IRipplePool<T>` with HOT/WARM/COLD tiering.
13. **Source Generators**: Auto-generate projection controllers, aggregate controllers, and route registries from grain attributes.

## Guiding Principles

- All code must follow repository instruction files (`.github/instructions/*.instructions.md`)
- Zero warnings policy applies to all new code
- TDD approach: write failing tests first
- Framework code in `src/`, sample-specific code in `samples/`

## TDD Workflow

Each task follows Red-Green-Refactor:

1. **Red**: Write failing test(s) capturing the acceptance criteria
2. **Green**: Implement minimal code to pass tests
3. **Refactor**: Clean up while keeping tests green

## Cleanup

Once all phases complete and `./go.ps1` passes, delete the entire `./agile/` folder.
