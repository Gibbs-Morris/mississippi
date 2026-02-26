# 00 — Intake

## Objective

Extend Mississippi's generated API authorization system to cover SignalR hub subscriptions, so that the same `[GenerateAuthorization]` attribute that already protects HTTP projection endpoints also protects real-time SignalR subscription notifications. The existing global force mode (`GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints`) should also apply to the hub.

## Non-Goals

- Changing how HTTP endpoint authorization works (already working).
- Introducing a new attribute for SignalR-specific auth — we reuse `[GenerateAuthorization]`.
- Per-entity authorization (e.g., "user X can only see account Y") — this is projection-path-level only.
- Encryption or payload-level security beyond what ASP.NET Core provides.
- Client-side auth flow changes (the client already sends auth headers via `AuthSimulationHeadersHandler`).

## Constraints

### User Constraints

- Developer experience must remain attribute-driven and declarative. Domain developers should not need to know about SignalR.
- Must work with and without `[GenerateAuthorization]` — no-auth projections stay open.
- Must work with and without the global force mode toggle.
- The solution must be in the framework (`src/`), not the sample (`samples/`). Domain code shouldn't change.

### Repository Constraints

- Pre-1.0: breaking changes are permitted freely (backwards-compatibility.instructions.md).
- Zero warnings policy.
- Central Package Management — no inline `Version` attributes.
- DI: private get-only properties, no service locator, constructor injection only.
- Logging via `[LoggerMessage]` through LoggerExtensions.
- Source generators use Roslyn incremental generator pattern.
- `.slnx` is canonical; `.sln` is auto-generated.

## Initial Assumptions (to validate)

1. The `ProjectionEndpointsGenerator` already has access to `[GenerateAuthorization]` metadata for projections.
2. The `IProjectionBrookRegistry` in `Inlet.Runtime` maps projection paths to brook names — we can augment or parallel it with auth metadata.
3. `InletHub` has access to `Context.User` (standard SignalR behavior).
4. ASP.NET Core's `IAuthorizationService` can be injected into the hub for policy evaluation.
5. The `MapInletHub()` method can read `InletServerOptions` to conditionally add `.RequireAuthorization()`.

## Open Questions

1. Should hub-level auth (Layer 1) be tied to the same `GeneratedApiAuthorizationMode` or have its own toggle?
2. Where should the projection auth registry live — `Inlet.Gateway.Abstractions` or `Inlet.Gateway`?
3. Should the generator emit the registry population code, or should it be reflection-based at startup?
4. How should subscribe denial be communicated to the client — `HubException`, error return value, or silent rejection?
5. Should `[GenerateAllowAnonymous]` on a projection opt it out of hub-level auth when force mode is on?
