# 03 — Decisions

## Decision 1: Global Force Mode Toggle Scope

**Decision:** The same `GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints` toggle controls BOTH HTTP endpoint auth AND SignalR hub auth.

**Chosen option:** (A) Same toggle controls both.

**Rationale:** "All generated endpoints" should mean all — HTTP and SignalR. A single toggle is easier to reason about and avoids scenarios where a developer enables auth but forgets a separate hub toggle.

**Evidence:**
- `GeneratedApiAuthorizationOptions` already has `Mode`, `DefaultPolicy`, `DefaultRoles`, `DefaultAuthenticationSchemes`, `AllowAnonymousOptOut` — all reusable for hub auth.
- `GeneratedApiAuthorizationConvention` applies auth to MVC controllers when mode is on — same semantic should extend to hub.

**Risks:** None significant. If someone wants HTTP auth without hub auth (unusual), they have no toggle. Mitigated by: this is pre-1.0 and can be split later if needed.

**Confidence:** High.

---

## Decision 2: Subscribe Denial Communication

**Decision:** Throw `HubException` from `InletHub.SubscribeAsync()` when authorization fails.

**Chosen option:** (A) Throw HubException.

**Rationale:** The client already catches all exceptions from `HubConnection.InvokeAsync` and dispatches error actions to the Redux store. `HubException` is the standard SignalR error propagation mechanism. No client code changes needed.

**Evidence:**
- `InletSignalRActionEffect.HandleSubscribeAsync` wraps the invoke in try/catch, yields `ProjectionActionFactory.CreateError(projectionType, entityId, ex)`.
- ASP.NET Core SignalR: `HubException` message is serialized to the client; other exception types show generic "error invoking method" in production.

**Risks:** Error message content could leak information. Mitigated by: use a generic message like "Access denied for projection '{path}'." without exposing policy details.

**Confidence:** High.

---

## Decision 3: AllowAnonymous Exemption for Hub Subscriptions

**Decision:** `[GenerateAllowAnonymous]` on a projection exempts its SignalR subscription from per-subscription auth checks when force mode is on, mirroring HTTP behavior. Controlled by `AllowAnonymousOptOut`.

**Chosen option:** (A) Yes, consistent behavior.

**Rationale:** Domain developers should have one mental model — if a projection is marked `[GenerateAllowAnonymous]`, it's anonymous everywhere that generated endpoints exist (HTTP GET and SignalR subscription). `AllowAnonymousOptOut` already controls whether the exemption is honored.

**Evidence:**
- `GeneratedApiAuthorizationConvention.Apply()` checks `HasAllowAnonymous` + `AllowAnonymousOptOut` for each controller/action — same logic applied to subscriptions.

**Risks:** A projection marked as anonymous for HTTP could be a concern for subscription notifications. Mitigated by: notifications only contain `(path, entityId, newVersion)` — no data payload. The actual data fetch goes through the HTTP endpoint which has its own auth.

**Confidence:** High.

---

## Decision 4: Hub-Level Authentication When Force Mode Is On

**Decision:** When `Mode == RequireAuthorizationForAllGeneratedEndpoints`, `MapInletHub()` chains `.RequireAuthorization()` on the hub endpoint. Unauthenticated clients cannot connect.

**Chosen option:** (A) Hub-level auth when force mode on.

**Rationale:** Defense-in-depth. Prevents unauthenticated connection probing. Fail-fast — rejected at connection, not at first subscribe. Individual subscriptions still get projection-specific policy checks for differentiated access.

**Evidence:**
- `MapInletHub()` returns `HubEndpointConventionBuilder` which supports `.RequireAuthorization()`.
- `IEndpointRouteBuilder.ServiceProvider` allows resolving `IOptions<InletServerOptions>` inline.

**Risks:** When force mode is on but ALL projections are `[GenerateAllowAnonymous]`, the hub still requires auth. Edge case. Mitigated by: this configuration is contradictory and unlikely. If needed, disable force mode and use per-projection auth.

**Confidence:** High.

---

## Decision 5: Auth Registry Architecture (from repo findings)

**Decision:** Create `IProjectionAuthorizationRegistry` in `Inlet.Runtime.Abstractions` and `ProjectionAuthorizationRegistry` in `Inlet.Runtime`, populated during `ScanProjectionAssemblies()`. Add `Inlet.Generators.Abstractions` as a reference to `Inlet.Runtime`.

**Chosen option:** Follow the `IProjectionBrookRegistry` pattern exactly.

**Rationale:** Parallels an established, proven pattern. Strongly-typed attribute access (not string-based reflection). Zero API change for consumers — `ScanProjectionAssemblies()` call picks up auth metadata automatically.

**Evidence:**
- `IProjectionBrookRegistry` in `Inlet.Runtime.Abstractions`, `ProjectionBrookRegistry` in `Inlet.Runtime`.
- `ScanProjectionAssemblies` iterates types and reads attributes — trivial to add `[GenerateAuthorization]` / `[GenerateAllowAnonymous]` reads.
- `Inlet.Generators.Abstractions` is `netstandard2.0` with zero dependencies — safe to reference.

**Risks:** Adds a reference from `Inlet.Runtime` → `Inlet.Generators.Abstractions`. Minor coupling. Mitigated by: Generators.Abstractions is dependency-free and contains only attributes — it's effectively another abstractions package.

**Confidence:** High.
