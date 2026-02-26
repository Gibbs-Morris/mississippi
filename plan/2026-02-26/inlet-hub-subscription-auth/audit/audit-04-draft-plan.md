# 04 — Draft Plan: Inlet Hub Subscription Authorization

## Executive Summary

The Mississippi framework's SignalR hub (`InletHub`) currently has **zero authentication or authorization**. Any connected client can subscribe to any projection and receive real-time version-change notifications, leaking entity existence, activity timing, and change frequency. This plan extends the existing `[GenerateAuthorization]` attribute system — which already protects HTTP projection endpoints — to also protect SignalR subscriptions. **No new attributes. No domain code changes. Same global force mode toggle.** The implementation adds a projection authorization registry (paralleling the existing `IProjectionBrookRegistry`), hub-level auth when force mode is on, and per-subscription auth checks in `SubscribeAsync`.

---

## Current State

### What Works Today

- **HTTP endpoint auth**: `[GenerateAuthorization(Policy/Roles)]` on domain types → source generators emit `[Authorize]` on generated controllers. Global force mode (`GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints`) applies auth to all generated MVC controllers via `GeneratedApiAuthorizationConvention`.
- **Projection subscriptions**: Client calls `HubConnection.InvokeAsync("SubscribeAsync", path, entityId)` → `InletHub.SubscribeAsync` → `IInletSubscriptionGrain` → Orleans stream subscription → version-change notifications pushed via Aqueduct backplane.
- **Client error handling**: `InletSignalRActionEffect` catches all exceptions from `SubscribeAsync` and dispatches error actions to the Redux store.

### What's Missing

| Layer | Gap |
|-------|-----|
| Hub endpoint | No `[Authorize]` on `InletHub`. `MapInletHub()` doesn't chain `.RequireAuthorization()`. |
| Per-subscription | `SubscribeAsync` has no auth check. No projection auth registry exists. |
| Configuration | `GeneratedApiAuthorizationConvention` is MVC-only (`IApplicationModelConvention`). The mode toggle doesn't affect SignalR. |
| Metadata | `ScanProjectionAssemblies()` reads `[ProjectionPath]` + `[BrookName]` but ignores `[GenerateAuthorization]` + `[GenerateAllowAnonymous]`. |

### Information Leak

Even though SignalR only pushes `(path, entityId, newVersion)` (no data payload), an unauthenticated subscriber can learn:
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
│    1. Look up IProjectionAuthorizationRegistry for path              │
│    2a. Projection has [GenerateAuthorization] → evaluate policy      │
│        via IAuthorizationService against Context.User                │
│    2b. Projection has [GenerateAllowAnonymous] → skip auth check     │
│    2c. No auth metadata + force mode → apply default policy          │
│    2d. No auth metadata + mode disabled → allow (current behavior)   │
│    3. Auth fails → throw HubException("Access denied")               │
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

See `03-decisions.md` for full rationale and evidence.

1. **Same toggle controls both HTTP and hub auth** — no separate hub toggle.
2. **HubException on auth failure** — client already handles this gracefully.
3. **`[GenerateAllowAnonymous]` exempts subscriptions** — consistent with HTTP behavior, controlled by `AllowAnonymousOptOut`.
4. **Hub-level auth when force mode on** — defense-in-depth, fail-fast.
5. **Auth registry parallels `IProjectionBrookRegistry`** — interface in `Inlet.Runtime.Abstractions`, impl in `Inlet.Runtime`, populated during `ScanProjectionAssemblies()`.

---

## Public Contracts / APIs

### New Interface: `IProjectionAuthorizationRegistry`

**Location:** `src/Inlet.Runtime.Abstractions/`

```csharp
public interface IProjectionAuthorizationRegistry
{
    ProjectionAuthorizationEntry? GetAuthorizationEntry(string path);
    IReadOnlyCollection<string> GetAllPaths();
    void Register(string path, ProjectionAuthorizationEntry entry);
}
```

### New Record: `ProjectionAuthorizationEntry`

**Location:** `src/Inlet.Runtime.Abstractions/`

```csharp
public sealed record ProjectionAuthorizationEntry(
    string? Policy,
    string? Roles,
    string? AuthenticationSchemes,
    bool HasAuthorize,
    bool HasAllowAnonymous
);
```

### Modified Method: `MapInletHub`

**Location:** `src/Inlet.Gateway/InletServerRegistrations.cs`

Signature unchanged. Behavior change: when force mode is on, chains `.RequireAuthorization()` with default policy/roles/schemes from options.

### Modified Method: `InletHub.SubscribeAsync`

**Location:** `src/Inlet.Gateway/InletHub.cs`

Signature unchanged. Behavior change: evaluates per-projection auth before delegating to grain.

### Modified Method: `ScanProjectionAssemblies`

**Location:** `src/Inlet.Runtime/InletSiloRegistrations.cs`

Signature unchanged. Behavior change: also reads `[GenerateAuthorization]` and `[GenerateAllowAnonymous]` from scanned types and populates `IProjectionAuthorizationRegistry`.

### No New Attributes

`[GenerateAuthorization]` and `[GenerateAllowAnonymous]` are reused as-is.

---

## Architecture & Flow

### Auth Check Flow in `SubscribeAsync`

```
SubscribeAsync(path, entityId)
  │
  ├─ authRegistry.GetAuthorizationEntry(path)
  │
  ├─ entry is null AND mode == Disabled
  │   └─ ALLOW (no auth metadata, no force mode)
  │
  ├─ entry is null AND mode == RequireAuthorizationForAllGeneratedEndpoints
  │   └─ Apply default policy/roles/schemes from options
  │       ├─ IAuthorizationService.AuthorizeAsync(Context.User, defaultPolicy)
  │       ├─ Pass → ALLOW
  │       └─ Fail → throw HubException
  │
  ├─ entry.HasAllowAnonymous AND AllowAnonymousOptOut
  │   └─ ALLOW (projection opted out of auth)
  │
  ├─ entry.HasAllowAnonymous AND !AllowAnonymousOptOut
  │   └─ Strip AllowAnonymous, apply default policy (force mode behavior)
  │
  ├─ entry.HasAuthorize
  │   ├─ Build AuthorizationPolicy from entry.Policy/Roles/AuthenticationSchemes
  │   ├─ IAuthorizationService.AuthorizeAsync(Context.User, policy)
  │   ├─ Pass → ALLOW
  │   └─ Fail → throw HubException
  │
  └─ ALLOW (fallthrough — shouldn't reach here)
```

### Registration Flow

```
Program.cs
  │
  ├─ services.AddInletServer(options => { options.GeneratedApiAuthorization.Mode = ...; })
  │   └─ Registers IProjectionBrookRegistry (singleton)
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
          └─ Mode == RequireAuth → map hub + .RequireAuthorization(defaultPolicy/roles/schemes)
```

---

## Work Breakdown

### Phase 1: Auth Registry (Foundation)

**Outcome:** Auth metadata infrastructure exists and is populated during assembly scanning.

1. **Add project reference**: `Inlet.Runtime.csproj` → `Inlet.Generators.Abstractions.csproj`
2. **Create `ProjectionAuthorizationEntry`**: Immutable record in `src/Inlet.Runtime.Abstractions/`
3. **Create `IProjectionAuthorizationRegistry`**: Interface in `src/Inlet.Runtime.Abstractions/` with `GetAuthorizationEntry(path)`, `GetAllPaths()`, `Register(path, entry)`
4. **Create `ProjectionAuthorizationRegistry`**: `ConcurrentDictionary`-backed implementation in `src/Inlet.Runtime/`
5. **Modify `InletSiloRegistrations.AddInletSilo`**: Register `IProjectionAuthorizationRegistry` as singleton (mirrors `IProjectionBrookRegistry`)
6. **Modify `InletSiloRegistrations.ScanProjectionAssemblies`**: Read `[GenerateAuthorization]` + `[GenerateAllowAnonymous]` → populate auth registry

### Phase 2: Hub-Level Auth (Layer 1)

**Outcome:** When force mode is on, the hub endpoint requires authentication.

7. **Modify `InletServerRegistrations.MapInletHub`**: Resolve `IOptions<InletServerOptions>` from `IEndpointRouteBuilder.ServiceProvider`. When mode is `RequireAuthorizationForAllGeneratedEndpoints`, chain `.RequireAuthorization()` with default policy/roles/schemes.

### Phase 3: Per-Subscription Auth (Layer 2)

**Outcome:** Each `SubscribeAsync` call evaluates projection-specific auth.

8. **Modify `InletHub` constructor**: Add `IProjectionAuthorizationRegistry`, `IAuthorizationService`, `IOptions<InletServerOptions>` dependencies
9. **Modify `InletHub.SubscribeAsync`**: Add authorization check before grain delegation (see flow above)
10. **Add logging**: Add `[LoggerMessage]` entries to `InletHubLoggerExtensions` for auth success/failure

### Phase 4: Tests

**Outcome:** Comprehensive L0 test coverage for all auth paths.

11. **`ProjectionAuthorizationRegistryTests`**: Registration, lookup, unknown path, all-paths enumeration
12. **`ScanProjectionAssembliesAuthTests`**: Verify assembly scanning populates auth registry from test types with `[GenerateAuthorization]` / `[GenerateAllowAnonymous]`
13. **`InletHubAuthorizationTests`**: Test subscribe with:
    - No auth metadata, mode disabled → allow
    - No auth metadata, force mode → apply default policy
    - `[GenerateAuthorization(Policy)]` → evaluate specific policy
    - `[GenerateAuthorization(Roles)]` → evaluate specific roles
    - `[GenerateAllowAnonymous]` + force mode + opt-out enabled → allow
    - `[GenerateAllowAnonymous]` + force mode + opt-out disabled → enforce default
    - Auth failure → HubException thrown
14. **`MapInletHubAuthTests`**: Test hub endpoint requires auth when mode is on, doesn't when disabled
15. **Update existing `GeneratedApiAuthorizationConventionTests`**: Ensure no regressions

### Phase 5: Sample Update

**Outcome:** Spring sample demonstrates the feature.

16. **Verify Spring.Server**: No code changes needed — `ScanProjectionAssemblies` already called, force mode already configurable. `AuthProofProjection` subscription will automatically be protected.
17. **Manual verification**: Validate that auth-proof projection subscription is denied for unauthenticated users, and allowed for users with the right policy claim.

---

## Testing Strategy

### Test Levels

| Level | Scope | What's Tested |
|-------|-------|---------------|
| L0 | Pure unit | Registry CRUD, auth check logic, hub auth flow with mocked `IAuthorizationService` |
| L1 | Light infra | `ScanProjectionAssemblies` with real assemblies containing test projection types |

### Test Projects

- `tests/Inlet.Runtime.Abstractions.L0Tests/` — `ProjectionAuthorizationEntry` record tests (if behavior beyond record equality)
- `tests/Inlet.Runtime.L0Tests/` — `ProjectionAuthorizationRegistry` tests, `ScanProjectionAssemblies` auth scanning tests
- `tests/Inlet.Gateway.L0Tests/` — `InletHub` auth tests, `MapInletHub` auth tests

### Coverage Target

- Changed code: 100% line coverage
- Mutation score: maintain or raise for Mississippi projects

### Key Test Scenarios

1. **Registry**: Register projection with policy → retrieve → correct. Unknown path → null. Multiple registrations → all paths returned.
2. **Scanning**: Assembly with `[GenerateAuthorization(Policy="x")]` projection → registry contains entry with Policy="x". Assembly with `[GenerateAllowAnonymous]` → entry has `HasAllowAnonymous=true`. Assembly with neither → no auth entry (but brook entry still registered).
3. **Hub auth (force mode on)**: Subscribe to undecorated projection → default policy checked. Subscribe with policy claim → success. Subscribe without claim → HubException.
4. **Hub auth (force mode off)**: Subscribe to undecorated projection → no auth check. Subscribe to `[GenerateAuthorization]` projection without claim → HubException.
5. **AllowAnonymous**: Force mode on + `[GenerateAllowAnonymous]` + opt-out enabled → no auth check. Force mode on + `[GenerateAllowAnonymous]` + opt-out disabled → default policy checked.

---

## Observability

### Logging (via LoggerExtensions)

Add to `InletHubLoggerExtensions.cs`:
- `SubscriptionAuthorizationSucceeded(connectionId, path, entityId)` — Debug level
- `SubscriptionAuthorizationFailed(connectionId, path, entityId)` — Warning level
- `SubscriptionAuthorizationSkipped(connectionId, path, entityId, reason)` — Debug level (for AllowAnonymous/no-metadata cases)

### Failure Modes

| Failure | Detection | User Impact | Recovery |
|---------|-----------|-------------|----------|
| Auth service unavailable | `IAuthorizationService` throws | Subscribe fails with error | Client retries (existing behavior) or user refreshes |
| Auth registry empty (missing `ScanProjectionAssemblies` call) | All subscriptions have no auth entry | Force mode: default policy applied. Disabled mode: all allowed | Developer must call `ScanProjectionAssemblies` (existing requirement) |
| Policy not registered | `IAuthorizationService` returns failure | Subscribe denied | Developer must register policy in auth configuration |
| Hub auth blocks valid user | Connection rejected | No subscriptions possible | Check auth configuration and tokens |

### Diagnostics

- `IProjectionAuthorizationRegistry.GetAllPaths()` enables listing registered auth entries for debugging
- Logging at Debug level for auth decisions enables production troubleshooting without noise

---

## Rollout and Migration

### Breaking Changes

- **When force mode is on**: SignalR hub now requires authentication. Previously open hub connections from unauthenticated clients will be rejected. This is **intentional** — the force mode name explicitly says "require authorization for all generated endpoints."
- **When force mode is off**: No behavioral change for undecorated projections. Projections with `[GenerateAuthorization]` will now have their subscriptions checked (previously unprotected). This is a **security fix**, not a regression.

### Migration Steps for Consumers

1. If using force mode: ensure auth tokens are sent with SignalR connections (standard ASP.NET Core SignalR auth — bearer token in query string or headers).
2. If using `[GenerateAuthorization]` on projections: no changes needed. Subscriptions are now protected.
3. If using `[GenerateAllowAnonymous]` on projections: behavior matches HTTP — anonymous access to subscription allowed (when `AllowAnonymousOptOut` is true).

### No Consumer Code Changes Required

- `ScanProjectionAssemblies()` API signature unchanged — automatically starts scanning auth attributes.
- `MapInletHub()` API signature unchanged — automatically respects force mode.
- Domain types unchanged — `[GenerateAuthorization]` already present.

---

## Acceptance Criteria

- [ ] `IProjectionAuthorizationRegistry` interface exists in `Inlet.Runtime.Abstractions`
- [ ] `ProjectionAuthorizationRegistry` implementation exists in `Inlet.Runtime`
- [ ] `ScanProjectionAssemblies` populates both brook and auth registries
- [ ] `MapInletHub` chains `.RequireAuthorization()` when force mode is on
- [ ] `InletHub.SubscribeAsync` checks per-projection auth before delegating to grain
- [ ] `[GenerateAuthorization]` projection → subscription requires matching policy/roles
- [ ] `[GenerateAllowAnonymous]` projection + force mode + opt-out → subscription allowed without auth
- [ ] No auth metadata + force mode → default policy applied to subscription
- [ ] No auth metadata + mode disabled → subscription allowed (current behavior preserved)
- [ ] Auth failure → `HubException` thrown → client catches and dispatches error action
- [ ] All new code has `[LoggerMessage]` logging via LoggerExtensions
- [ ] Zero compiler/analyzer warnings
- [ ] L0 tests for registry, scanning, hub auth — 100% coverage on changed code
- [ ] Mutation score maintained or raised
- [ ] Spring sample works without code changes — `AuthProofProjection` subscription protected automatically
- [ ] No domain project code changes required

---

## Files Changed (Expected)

### New Files

| File | Purpose |
|------|---------|
| `src/Inlet.Runtime.Abstractions/ProjectionAuthorizationEntry.cs` | Immutable auth metadata record |
| `src/Inlet.Runtime.Abstractions/IProjectionAuthorizationRegistry.cs` | Registry interface |
| `src/Inlet.Runtime/ProjectionAuthorizationRegistry.cs` | ConcurrentDictionary implementation |
| `tests/Inlet.Runtime.L0Tests/ProjectionAuthorizationRegistryTests.cs` | Registry L0 tests |
| `tests/Inlet.Runtime.L0Tests/ScanProjectionAssembliesAuthTests.cs` | Assembly scanning auth tests |
| `tests/Inlet.Gateway.L0Tests/InletHubAuthorizationTests.cs` | Hub subscribe auth tests |
| `tests/Inlet.Gateway.L0Tests/MapInletHubAuthTests.cs` | Hub endpoint auth tests |

### Modified Files

| File | Change |
|------|--------|
| `src/Inlet.Runtime/Inlet.Runtime.csproj` | Add `Inlet.Generators.Abstractions` reference |
| `src/Inlet.Runtime/InletSiloRegistrations.cs` | Register auth registry; scan `[GenerateAuthorization]` + `[GenerateAllowAnonymous]` |
| `src/Inlet.Gateway/InletHub.cs` | Add auth dependencies; auth check in `SubscribeAsync` |
| `src/Inlet.Gateway/InletHubLoggerExtensions.cs` | Add auth-related `[LoggerMessage]` entries |
| `src/Inlet.Gateway/InletServerRegistrations.cs` | `MapInletHub` conditional `.RequireAuthorization()` |

---

## Mandatory Final Step for flow Builder

**The flow Builder's final commit MUST delete `/plan/2026-02-26/inlet-hub-subscription-auth/` so planning artifacts do not land on `main`.**
