# PLAN.md — Inlet Hub Subscription Authorization

**PR Title:** `Secure SignalR projection subscriptions with existing GenerateAuthorization system +semver: feature`

---

## Executive Summary

The Mississippi framework's SignalR hub (`InletHub`) currently has **zero authentication or authorization**. Any connected client can subscribe to any projection and receive real-time version-change notifications, leaking entity existence, activity timing, and change frequency. This plan extends the existing `[GenerateAuthorization]` attribute system — which already protects generated HTTP surfaces for **aggregates, projections, and sagas** — to also protect SignalR subscriptions.

**Key principles:**
- No new attributes. No domain code changes. Same global force mode toggle.
- Auth checked at the gateway (hub), never in grains.
- Generic denial messages — no path/policy leak in production.
- Preserve existing auth parity across aggregate/projection/saga generated HTTP controllers.
- Zero new concepts for domain developers.

---

## Current State

### What Works

- **HTTP endpoint auth (all generated APIs)**: `[GenerateAuthorization(Policy/Roles)]` on domain types → source generators emit `[Authorize]` on generated controllers for aggregate endpoints, projection endpoints, and saga endpoints. Global force mode (`GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints`) applies auth to all generated MVC controllers via `GeneratedApiAuthorizationConvention`.
- **Projection subscriptions**: Client calls `HubConnection.InvokeAsync("SubscribeAsync", path, entityId)` → `InletHub.SubscribeAsync` → `IInletSubscriptionGrain` → Orleans stream → version-change notifications via Aqueduct backplane.
- **Client error handling**: `InletSignalRActionEffect` catches all exceptions from `SubscribeAsync` and dispatches error actions to the Redux store.

### Cross-Surface Authorization Parity (Required)

| Surface | Existing Behavior | Required Outcome in This Plan |
|---------|-------------------|-------------------------------|
| Aggregate generated HTTP endpoints | `[GenerateAuthorization]`/`[GenerateAllowAnonymous]` handled by `AggregateControllerGenerator` + convention | No behavior regression |
| Saga generated HTTP endpoints | `[GenerateAuthorization]`/`[GenerateAllowAnonymous]` handled by `SagaControllerGenerator` + convention | No behavior regression |
| Projection generated HTTP endpoints | `[GenerateAuthorization]`/`[GenerateAllowAnonymous]` handled by `ProjectionEndpointsGenerator` + convention | No behavior regression |
| Projection SignalR subscriptions | Currently unprotected | New runtime auth enforcement with same attribute semantics |

### What's Missing

| Layer | Gap |
|-------|-----|
| Hub endpoint | No `[Authorize]` on `InletHub`. `MapInletHub()` doesn't chain `.RequireAuthorization()`. |
| Per-subscription | `SubscribeAsync` has no auth check. No projection auth registry exists. |
| Configuration | `GeneratedApiAuthorizationConvention` is MVC-only. Mode toggle doesn't affect SignalR. |
| Metadata | `ScanProjectionAssemblies()` reads `[ProjectionPath]` + `[BrookName]` but ignores `[GenerateAuthorization]` + `[GenerateAllowAnonymous]`. |

### Information Leak

SignalR only pushes `(path, entityId, newVersion)` (no data payload), but an unauthenticated subscriber can learn:
- That entity X exists
- When entity X changes (temporal patterns)
- How frequently entity X changes (activity patterns)
- Version progression (business activity volume)

---

## Target State

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Client (Blazor WASM)                         │
│  InletSignalRActionEffect.HandleSubscribeAsync                      │
│    └─ HubConnection.InvokeAsync("SubscribeAsync", path, entityId)   │
│       └─ on HubException → ProjectionActionFactory.CreateError()    │
└─────────────────────────┬───────────────────────────────────────────┘
                          │ SignalR WebSocket
                          ▼
┌─────────────────────────────────────────────────────────────────────┐
│                 Layer 1: Hub Endpoint Auth                           │
│  MapInletHub() conditionally chains .RequireAuthorization()          │
│  when Mode == RequireAuthorizationForAllGeneratedEndpoints           │
│  → Unauthenticated clients rejected at connection time               │
└─────────────────────────┬───────────────────────────────────────────┘
                          │ Authenticated connection
                          ▼
┌─────────────────────────────────────────────────────────────────────┐
│                 Layer 2: Per-Subscription Auth                       │
│  InletHub.SubscribeAsync(path, entityId)                            │
│    → delegates to private AuthorizeSubscriptionAsync(path)           │
│    1. Look up IProjectionAuthorizationRegistry for path              │
│    2a. Has [GenerateAuthorization] → build AuthorizationPolicy       │
│        via AuthorizationPolicyBuilder, evaluate via                  │
│        IAuthorizationService against Context.User                    │
│    2b. Has [GenerateAllowAnonymous] + OptOut enabled → skip auth     │
│    2c. No auth metadata + force mode → apply default policy          │
│    2d. No auth metadata + mode disabled → allow (current behavior)   │
│    3. Auth fails → throw HubException("Subscription denied.")        │
│       (generic message; details logged server-side)                  │
│    4. Auth passes → delegate to IInletSubscriptionGrain              │
└─────────────────────────┬───────────────────────────────────────────┘
                          │ Authorized subscription
                          ▼
┌─────────────────────────────────────────────────────────────────────┐
│              IInletSubscriptionGrain (unchanged)                     │
│    → Orleans stream → Aqueduct backplane → ProjectionUpdatedAsync   │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Key Design Decisions

| # | Decision | Rationale |
|---|----------|-----------|
| D1 | Same toggle controls both HTTP and hub auth | Single mental model for consumers. One setting protects all generated endpoints. |
| D2 | Generic `HubException("Subscription denied.")` — no path/policy in message | OWASP: don't leak implementation details. Details logged server-side at Warning. |
| D3 | `[GenerateAllowAnonymous]` exempts subscriptions (consistent with HTTP) | Controlled by existing `AllowAnonymousOptOut` option. |
| D4 | Hub-level auth when force mode on | Defense-in-depth: reject unauthenticated connections before any subscription attempt. |
| D5 | Auth registry parallels `IProjectionBrookRegistry` | Consistent pattern. Interface in Abstractions, impl in Runtime, populated during scan. |
| D6 | Extract auth check into `AuthorizeSubscriptionAsync` private method | SRP: SubscribeAsync orchestrates; auth logic is isolated and testable. |
| D7 | Use `AuthorizationPolicyBuilder` for roles/schemes construction | Handles all combinations: named policy, roles-only, schemes-only, or mixed. |
| D8 | Rename to `ProjectionAuthorizationMetadata` (not "Entry") | Avoids confusion with ASP.NET Core `AuthorizationPolicy`. Describes declarative metadata. |
| D9 | Treat aggregate/projection/saga auth semantics as a single contract | Subscription auth extends the same attribute contract; generator semantics stay unchanged and are regression-tested. |

---

## Public Contracts / APIs

### New Record: `ProjectionAuthorizationMetadata`

**Location:** `src/Inlet.Runtime.Abstractions/ProjectionAuthorizationMetadata.cs`

```csharp
public sealed record ProjectionAuthorizationMetadata(
    string? Policy,
    string? Roles,
    string? AuthenticationSchemes,
    bool HasAuthorize,
    bool HasAllowAnonymous);
```

### New Interface: `IProjectionAuthorizationRegistry`

**Location:** `src/Inlet.Runtime.Abstractions/IProjectionAuthorizationRegistry.cs`

```csharp
public interface IProjectionAuthorizationRegistry
{
    ProjectionAuthorizationMetadata? GetAuthorizationMetadata(string path);
    IReadOnlyCollection<string> GetAllPaths();
    void Register(string path, ProjectionAuthorizationMetadata metadata);
}
```

### New Constant: `InletHubConstants.SubscriptionDeniedMessage`

**Location:** `src/Inlet.Gateway.Abstractions/InletHubConstants.cs`

```csharp
public const string SubscriptionDeniedMessage = "Subscription denied.";
```

### Modified Methods (signature unchanged, behavior changed)

| Method | Change |
|--------|--------|
| `MapInletHub()` | Conditional `.RequireAuthorization()` when force mode on |
| `InletHub.SubscribeAsync()` | Auth check before grain delegation |
| `ScanProjectionAssemblies()` | Also reads `[GenerateAuthorization]` + `[GenerateAllowAnonymous]` |

### No New Attributes

`[GenerateAuthorization]` and `[GenerateAllowAnonymous]` are reused as-is. XML docs updated to mention SignalR scope.

---

## Architecture & Flow

### Auth Check Flow in `AuthorizeSubscriptionAsync`

```
AuthorizeSubscriptionAsync(path)
  │
  ├─ registry.GetAuthorizationMetadata(path)
  │
  ├─ metadata is null AND mode == Disabled
  │   └─ RETURN (no auth, no force = allow)
  │
  ├─ metadata is null AND mode == ForceAuth
  │   └─ Build default policy from options (DefaultPolicy/DefaultRoles/DefaultSchemes)
  │       ├─ AuthorizationPolicyBuilder → AuthorizationPolicy
  │       ├─ IAuthorizationService.AuthorizeAsync(Context.User, policy)
  │       ├─ Pass → RETURN
  │       └─ Fail → throw HubException(SubscriptionDeniedMessage)
  │
  ├─ metadata.HasAllowAnonymous AND AllowAnonymousOptOut
  │   └─ RETURN (opted out of auth)
  │
  ├─ metadata.HasAllowAnonymous AND !AllowAnonymousOptOut
  │   └─ Strip AllowAnonymous → apply default policy (same as above)
  │
  ├─ metadata.HasAuthorize
  │   ├─ Build policy from metadata.Policy/Roles/AuthenticationSchemes
  │   ├─ IAuthorizationService.AuthorizeAsync(Context.User, policy)
  │   ├─ Pass → RETURN
  │   └─ Fail → throw HubException(SubscriptionDeniedMessage)
  │
  └─ RETURN (metadata present but no auth flags — shouldn't happen, but safe fallthrough)
```

### Registration Flow

```
Program.cs
  │
  ├─ services.AddInletServer(options => { options.GeneratedApiAuthorization.Mode = ...; })
  │   └─ Registers IProjectionAuthorizationRegistry (singleton)
  │       Also registers IProjectionBrookRegistry (existing)
  │
  ├─ services.ScanProjectionAssemblies(assembly)  // MODIFIED
  │   └─ For each type with [ProjectionPath]:
  │       ├─ Read [BrookName] → register in IProjectionBrookRegistry (existing)
  │       ├─ Read [GenerateAuthorization] → register in IProjectionAuthorizationRegistry (NEW)
  │       └─ Read [GenerateAllowAnonymous] → register in IProjectionAuthorizationRegistry (NEW)
  │
  └─ app.MapInletHub()  // MODIFIED
      └─ Resolve IOptions<InletServerOptions>
          ├─ Mode == Disabled → map hub as-is (current behavior)
          └─ Mode == ForceAuth → map hub + .RequireAuthorization(policy/roles/schemes)
```

---

## Work Breakdown

### Phase 1: Auth Registry (Foundation)

**Outcome:** Auth metadata infrastructure exists and is populated during assembly scanning.

| # | Task | Files |
|---|------|-------|
| 1 | Add project reference: `Inlet.Runtime.csproj` → `Inlet.Generators.Abstractions`. Add csproj comment noting dual (generator + runtime) consumption. | `src/Inlet.Runtime/Inlet.Runtime.csproj` |
| 2 | Create `ProjectionAuthorizationMetadata` record | `src/Inlet.Runtime.Abstractions/ProjectionAuthorizationMetadata.cs` |
| 3 | Create `IProjectionAuthorizationRegistry` interface | `src/Inlet.Runtime.Abstractions/IProjectionAuthorizationRegistry.cs` |
| 4 | Create `ProjectionAuthorizationRegistry` (ConcurrentDictionary) | `src/Inlet.Runtime/ProjectionAuthorizationRegistry.cs` |
| 5 | Register `IProjectionAuthorizationRegistry` as singleton in `AddInletSilo` | `src/Inlet.Runtime/InletSiloRegistrations.cs` |
| 6 | Extend `ScanProjectionAssemblies` to read `[GenerateAuthorization]` + `[GenerateAllowAnonymous]` and populate auth registry | `src/Inlet.Runtime/InletSiloRegistrations.cs` |

### Phase 2: Hub-Level Auth (Layer 1)

**Outcome:** When force mode is on, the hub endpoint requires authentication at connection time.

| # | Task | Files |
|---|------|-------|
| 7 | Modify `MapInletHub` to conditionally chain `.RequireAuthorization()` with default policy/roles/schemes from options. Update XML doc. | `src/Inlet.Gateway/InletServerRegistrations.cs` |

### Phase 3: Per-Subscription Auth (Layer 2)

**Outcome:** Each `SubscribeAsync` call evaluates projection-specific auth before grain delegation.

| # | Task | Files |
|---|------|-------|
| 8 | Add `SubscriptionDeniedMessage` constant | `src/Inlet.Gateway.Abstractions/InletHubConstants.cs` |
| 9 | Add dependencies to `InletHub`: `IProjectionAuthorizationRegistry`, `IAuthorizationService`, `IOptions<InletServerOptions>` | `src/Inlet.Gateway/InletHub.cs` |
| 10 | Extract `AuthorizeSubscriptionAsync(path)` private method with full auth flow. Uses `AuthorizationPolicyBuilder` for policy construction. | `src/Inlet.Gateway/InletHub.cs` |
| 11 | Call `AuthorizeSubscriptionAsync` in `SubscribeAsync` before grain delegation | `src/Inlet.Gateway/InletHub.cs` |
| 12 | Add `[LoggerMessage]` entries for auth outcomes | `src/Inlet.Gateway/InletHubLoggerExtensions.cs` (or new partial class) |

### Phase 4: Documentation Updates

**Outcome:** XML docs reflect the dual-purpose nature of the auth system.

| # | Task | Files |
|---|------|-------|
| 13 | Update `[GenerateAuthorization]` XML doc to mention SignalR subscriptions | `src/Inlet.Generators.Abstractions/GenerateAuthorizationAttribute.cs` |
| 14 | Update `[GenerateAllowAnonymous]` XML doc similarly | `src/Inlet.Generators.Abstractions/GenerateAllowAnonymousAttribute.cs` |

### Phase 5: Tests

**Outcome:** Comprehensive L0 test coverage for subscription auth plus explicit no-regression coverage for aggregate/projection/saga generated HTTP auth semantics.

| # | Task | Files |
|---|------|-------|
| 15 | `ProjectionAuthorizationRegistryTests` — registration, lookup, unknown path, all-paths, multiple registrations | `tests/Inlet.Runtime.L0Tests/` |
| 16 | `ScanProjectionAssembliesAuthTests` — scanning assemblies with `[GenerateAuthorization]`, `[GenerateAllowAnonymous]`, neither | `tests/Inlet.Runtime.L0Tests/` |
| 17 | `InletHubAuthorizationTests` — see test scenarios below | `tests/Inlet.Gateway.L0Tests/` |
| 18 | `MapInletHubAuthTests` — hub endpoint auth when mode on/off | `tests/Inlet.Gateway.L0Tests/` |
| 19 | Verify existing `GeneratedApiAuthorizationConventionTests` still pass (no regressions) | `tests/Inlet.Gateway.L0Tests/` |
| 20 | Add/refresh generator parity tests to verify aggregate/projection/saga `[GenerateAuthorization]` and `[GenerateAllowAnonymous]` behavior remains unchanged | `tests/Inlet.Gateway.Generators.L0Tests/` |

### Phase 6: Sample Verification

**Outcome:** Spring sample demonstrates the feature without code changes.

| # | Task |
|---|------|
| 21 | Verify Spring.Server compiles — `ScanProjectionAssemblies` automatically scans auth attrs |
| 22 | Manual verification: `AuthProofProjection` subscription protected, others follow force mode |

### Phase 7: Release Communication

**Outcome:** Consumers get explicit migration visibility for the auth parity update.

| # | Task |
|---|------|
| 23 | Add release/changelog migration note covering: hub auth under force mode, projection subscription auth enforcement, and unchanged aggregate/projection/saga generated HTTP semantics |

---

## Test Scenarios (Phase 5 Detail)

### Registry Tests

1. Register metadata for path → retrieve → correct fields returned
2. Lookup unknown path → returns null
3. `GetAllPaths()` returns all registered paths
4. Register multiple paths → all retrievable independently
5. Register with null/empty policy → ok (roles/schemes may be present)

### Scanning Tests

6. Type with `[ProjectionPath]` + `[GenerateAuthorization(Policy="x")]` → auth registry has entry with Policy="x"
7. Type with `[ProjectionPath]` + `[GenerateAuthorization(Roles="admin")]` → entry has Roles="admin"
8. Type with `[ProjectionPath]` + `[GenerateAllowAnonymous]` → entry has HasAllowAnonymous=true
9. Type with `[ProjectionPath]` but NO auth attributes → brook registry populated, auth registry NOT populated for that path
10. Type without `[ProjectionPath]` but WITH `[GenerateAuthorization]` → ignored (no path = no registration)

### Hub Auth Tests

11. Mode disabled + no auth metadata → subscribe succeeds (current behavior preserved)
12. Mode disabled + `[GenerateAuthorization(Policy)]` + user has claim → subscribe succeeds
13. Mode disabled + `[GenerateAuthorization(Policy)]` + user lacks claim → HubException with generic message
14. Mode disabled + `[GenerateAuthorization(Roles)]` + user in role → subscribe succeeds
15. Mode disabled + `[GenerateAuthorization(Roles)]` + user not in role → HubException
16. Force mode + no auth metadata → default policy applied → user has claim → succeeds
17. Force mode + no auth metadata → default policy applied → user lacks claim → HubException
18. Force mode + `[GenerateAuthorization(Policy)]` → specific policy evaluated (not default)
19. Force mode + `[GenerateAllowAnonymous]` + AllowAnonymousOptOut=true → subscribe succeeds without auth
20. Force mode + `[GenerateAllowAnonymous]` + AllowAnonymousOptOut=false → default policy applied
21. Auth failure message is exactly `InletHubConstants.SubscriptionDeniedMessage` (no path/policy leak)

### MapInletHub Tests

22. Mode disabled → hub mapped without `.RequireAuthorization()`
23. Force mode → hub mapped with `.RequireAuthorization()` using default policy/roles/schemes

### Generator Parity Regression Tests (Aggregate / Projection / Saga)

24. `AggregateControllerGeneratorTests` still emit expected `[Authorize]`/`[AllowAnonymous]` metadata for aggregate and command actions
25. `ProjectionEndpointsGeneratorTests` still emit expected `[Authorize]`/`[AllowAnonymous]` metadata for projection controllers
26. `SagaControllerGeneratorTests` still emit expected `[Authorize]`/`[AllowAnonymous]` metadata for saga controllers
27. `GeneratedApiAuthorizationConventionTests` continue preserving/removing `AllowAnonymous` based on `AllowAnonymousOptOut`

---

## Observability

### Logging (via LoggerExtensions / `[LoggerMessage]`)

| Message | Level | Parameters |
|---------|-------|------------|
| `SubscriptionAuthorizationSucceeded` | Debug | `string connectionId, string path, string entityId, string? userId` |
| `SubscriptionAuthorizationDenied` | Warning | `string connectionId, string path, string entityId, string? userId, string? policyName` |
| `SubscriptionAuthorizationSkipped` | Debug | `string connectionId, string path, string entityId, string reason` |

Note: `policyName` appears in server-side logs only, never in the `HubException` message sent to the client.

### Failure Modes

| Failure | Detection | User Impact | Recovery |
|---------|-----------|-------------|----------|
| Auth service unavailable | `IAuthorizationService` throws | Subscribe fails with error | Client retries or user refreshes |
| Auth registry empty (missing `ScanProjectionAssemblies`) | All subscriptions have no metadata | Force mode: default policy applied. Disabled: all allowed | Call `ScanProjectionAssemblies` (existing requirement) |
| Policy not registered in auth config | `IAuthorizationService` returns failure | Subscribe denied | Register policy in auth configuration |
| Hub auth blocks valid user | Connection rejected at WebSocket level | No subscriptions possible | Check auth config, tokens, and JWT transport (see migration) |
| Auth service slow (external policy provider) | Subscribe latency increases | Subscription delays | Configure timeout/circuit-breaker at policy provider level |

---

## Rollout and Migration

### Breaking Changes

- **When force mode is on**: SignalR hub now requires authentication. Previously open connections from unauthenticated clients will be rejected. This is **intentional** — explicit in the mode name.
- **When force mode is off**: No behavioral change for undecorated projections. Projections with `[GenerateAuthorization]` will now have subscriptions checked (previously unprotected). This is a **security fix**, not a regression.

### Migration Steps for Consumers

1. **JWT + SignalR transport**: If using JWT bearer auth, ensure `JwtBearerEvents.OnMessageReceived` extracts the token from the query string (`?access_token=...`). WebSocket connections cannot send custom headers after the initial handshake. This is standard ASP.NET Core SignalR auth configuration.
2. **Force mode users**: Ensure auth tokens are sent with SignalR connections. If using cookie auth, this works automatically. If using bearer tokens, see step 1.
3. **`[GenerateAuthorization]` on aggregates/projections/sagas**: Existing generated HTTP behavior is unchanged. Projection subscriptions are now additionally protected.
4. **`[GenerateAllowAnonymous]` on aggregates/projections/sagas**: Existing generated HTTP behavior remains unchanged. Projection subscriptions follow the same opt-out semantics (controlled by `AllowAnonymousOptOut`).

### No Consumer Code Changes Required

All existing API signatures are unchanged:
- `ScanProjectionAssemblies()` — automatically starts scanning auth attributes
- `MapInletHub()` — automatically respects force mode for hub-level auth
- Domain types — `[GenerateAuthorization]` already present

---

## Known Limitations

1. **Subscribe-time auth only**: User claims evaluated at subscribe time. If claims change (e.g., role revoked) after subscription, notifications continue until the connection is closed/reconnected. This matches HTTP endpoint behavior — mid-request credential revocation is not enforced. Per-notification auth is prohibitively expensive.
2. **Path-level auth only**: Auth is per projection type, not per entity. In multi-tenant systems, User A can subscribe to another tenant's entity if they pass the path-level policy. Per-entity auth is a future extension (see Evolution).
3. **Token expiry**: JWT tokens validated at connection time may expire during a long-lived WebSocket connection. Standard SignalR limitation. Mitigation: configure token lifetime shorter than expected session length.

---

## Evolution (Future)

- **`ISubscriptionAuthorizationService`**: Extract the private `AuthorizeSubscriptionAsync` method into a dedicated service interface. Enables per-entity auth, custom tenant isolation, or pluggable auth strategies without modifying the hub.
- **`IHubFilter` pipeline**: For cross-cutting auth concerns (rate limiting, audit logging), an `IHubFilter` implementation could wrap all hub method invocations.
- **Metrics**: Add `System.Diagnostics.Metrics` counters (`inlet.hub.subscribe.auth.denied`, `inlet.hub.subscribe.auth.allowed`) for dashboards and alerting.
- **L2 integration test**: End-to-end test with Aspire AppHost verifying JWT → SignalR negotiate → hub auth → subscribe auth pipeline.
- **Generator-emitted registration**: Move auth metadata resolution from runtime reflection to compile-time source generation for trimming safety.

---

## Files Changed (Complete Manifest)

### New Files

| File | Purpose |
|------|---------|
| `src/Inlet.Runtime.Abstractions/ProjectionAuthorizationMetadata.cs` | Immutable auth metadata record |
| `src/Inlet.Runtime.Abstractions/IProjectionAuthorizationRegistry.cs` | Registry interface |
| `src/Inlet.Runtime/ProjectionAuthorizationRegistry.cs` | ConcurrentDictionary implementation |
| `tests/Inlet.Runtime.L0Tests/.../ProjectionAuthorizationRegistryTests.cs` | Registry L0 tests |
| `tests/Inlet.Runtime.L0Tests/.../ScanProjectionAssembliesAuthTests.cs` | Assembly scanning auth tests |
| `tests/Inlet.Gateway.L0Tests/.../InletHubAuthorizationTests.cs` | Hub subscribe auth tests |
| `tests/Inlet.Gateway.L0Tests/.../MapInletHubAuthTests.cs` | Hub endpoint auth tests |
| `tests/Inlet.Gateway.Generators.L0Tests/...` | Generator parity regression tests for aggregate/projection/saga auth emission |

### Modified Files

| File | Change |
|------|--------|
| `src/Inlet.Runtime/Inlet.Runtime.csproj` | Add `Inlet.Generators.Abstractions` reference + doc comment |
| `src/Inlet.Runtime/InletSiloRegistrations.cs` | Register auth registry singleton; extend `ScanProjectionAssemblies` to read auth attributes |
| `src/Inlet.Gateway/InletHub.cs` | Add auth dependencies; add `AuthorizeSubscriptionAsync` private method; call in `SubscribeAsync` |
| `src/Inlet.Gateway/InletHubLoggerExtensions.cs` | Add auth-related `[LoggerMessage]` entries |
| `src/Inlet.Gateway/InletServerRegistrations.cs` | `MapInletHub` conditional `.RequireAuthorization()` + XML doc update |
| `src/Inlet.Gateway.Abstractions/InletHubConstants.cs` | Add `SubscriptionDeniedMessage` constant |
| `src/Inlet.Generators.Abstractions/GenerateAuthorizationAttribute.cs` | Update XML doc to mention SignalR scope |
| `src/Inlet.Generators.Abstractions/GenerateAllowAnonymousAttribute.cs` | Update XML doc to mention SignalR scope |

---

## Acceptance Criteria

- [ ] `IProjectionAuthorizationRegistry` interface exists in `Inlet.Runtime.Abstractions`
- [ ] `ProjectionAuthorizationMetadata` record exists in `Inlet.Runtime.Abstractions`
- [ ] `ProjectionAuthorizationRegistry` implementation exists in `Inlet.Runtime`
- [ ] `ScanProjectionAssemblies` populates both brook and auth registries
- [ ] `MapInletHub` chains `.RequireAuthorization()` when force mode is on
- [ ] `InletHub.SubscribeAsync` calls `AuthorizeSubscriptionAsync` before grain delegation
- [ ] `AuthorizeSubscriptionAsync` uses `AuthorizationPolicyBuilder` for roles/schemes
- [ ] `[GenerateAuthorization]` projection → subscription requires matching policy/roles
- [ ] `[GenerateAllowAnonymous]` projection + force mode + opt-out → subscription allowed
- [ ] No auth metadata + force mode → default policy applied to subscription
- [ ] No auth metadata + mode disabled → subscription allowed (current behavior preserved)
- [ ] Auth failure → `HubException(SubscriptionDeniedMessage)` — no path/policy leak
- [ ] All `[LoggerMessage]` entries use explicit structured field names
- [ ] XML docs updated for `[GenerateAuthorization]`, `[GenerateAllowAnonymous]`, and `MapInletHub`
- [ ] Aggregate/projection/saga generated HTTP authorization behavior has no regressions
- [ ] Generator parity tests pass for aggregate/projection/saga authorization metadata emission
- [ ] Release/changelog migration note is published for cross-surface auth parity update
- [ ] Zero compiler/analyzer warnings
- [ ] L0 tests for registry, scanning, hub auth — 100% coverage on changed code
- [ ] Mutation score maintained or raised
- [ ] Spring sample works without code changes — `AuthProofProjection` subscription protected automatically
- [ ] No domain project code changes required

---

## Quality Gates (flow Builder Must Execute)

1. `pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1`
2. `pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1`
3. `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1`
4. `pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1` (wait for completion)
5. `pwsh ./eng/src/agent-scripts/build-sample-solution.ps1`
6. `pwsh ./eng/src/agent-scripts/clean-up-sample-solution.ps1`
7. `pwsh ./eng/src/agent-scripts/unit-test-sample-solution.ps1`

Or equivalently: `pwsh ./go.ps1`

---

## Mandatory Final Step

**The flow Builder's final commit MUST delete `/plan/2026-02-26/inlet-hub-subscription-auth/` so planning artifacts do not land on `main`.**
