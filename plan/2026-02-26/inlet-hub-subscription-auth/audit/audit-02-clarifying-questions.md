# 02 — Clarifying Questions

## (A) Answered from Repository Evidence

### Q1: Can `ScanProjectionAssemblies` read `[GenerateAuthorization]` at runtime?

**Answer:** Yes, but `Inlet.Runtime` does not currently reference `Inlet.Generators.Abstractions`.

**Evidence:**
- Domain assemblies (e.g., `Spring.Domain.csproj` line 20) reference `Inlet.Generators.Abstractions`, so the attribute type IS in the loaded assembly at runtime.
- `Inlet.Generators.Abstractions` is `netstandard2.0` with zero dependencies — adding it as a reference to `Inlet.Runtime` is safe and adds no transitive baggage.

**Triangulation:**
- `Inlet.Runtime` already references `Inlet.Abstractions` (contains `ProjectionPathAttribute`) and `Brooks.Abstractions` (contains `BrookNameAttribute`) — both are the same pattern: lightweight attribute containers.

**Conclusion:** Add `Inlet.Generators.Abstractions` as a project reference to `Inlet.Runtime`. This allows strongly-typed `GetCustomAttribute<GenerateAuthorizationAttribute>()` in `ScanProjectionAssemblies`, matching the existing pattern for `ProjectionPathAttribute` and `BrookNameAttribute`. **Confidence: High.**

---

### Q2: Does `InletHub` have access to `Context.User` for authorization checks?

**Answer:** Yes, standard SignalR `Hub` base class.

**Evidence:**
- `InletHub.cs` line 31: `Hub<IInletHubClient>` — inherits `Hub.Context.User` (`ClaimsPrincipal`).
- `InletHub` already uses `Context.ConnectionId` (lines 59, 72, 97, 111), confirming standard hub context access.

**Triangulation:**
- ASP.NET Core SignalR documentation confirms `Context.User` is populated from the authentication middleware.

**Conclusion:** `Context.User` is available. `IAuthorizationService` can be injected into the hub constructor to evaluate policies. **Confidence: High.**

---

### Q3: Can `MapInletHub` resolve options from the service provider?

**Answer:** Yes, `IEndpointRouteBuilder.ServiceProvider` is available.

**Evidence:**
- `IEndpointRouteBuilder` exposes `ServiceProvider` property — standard ASP.NET Core.
- The method currently returns `endpoints.MapHub<InletHub>(pattern)` — we can resolve `IOptions<InletServerOptions>` from `endpoints.ServiceProvider` before the return.

**Triangulation:**
- This pattern is used in other ASP.NET Core endpoint configuration extensions (e.g., Minimal APIs).

**Conclusion:** `MapInletHub` can read auth options and conditionally chain `.RequireAuthorization()`. **Confidence: High.**

---

### Q4: Should the auth registry interface follow `IProjectionBrookRegistry`'s placement?

**Answer (from architecture):** Yes, `Inlet.Runtime.Abstractions`.

**Evidence:**
- `IProjectionBrookRegistry` lives in `src/Inlet.Runtime.Abstractions/` — this is the abstractions project for silo/runtime concerns.
- Both `Inlet.Runtime` and `Inlet.Gateway` reference `Inlet.Runtime.Abstractions`.

**Triangulation:**
- The registry is populated in `Inlet.Runtime` (silo registration) and consumed in `Inlet.Gateway` (hub auth check). Both reference `Inlet.Runtime.Abstractions`. Same as `IProjectionBrookRegistry`.

**Conclusion:** Interface in `Inlet.Runtime.Abstractions`, implementation in `Inlet.Runtime`. Parallels the existing brook registry exactly. **Confidence: High.**

---

## (B) Questions for User

### Q5: Should the global force mode toggle control both HTTP AND hub auth, or should the hub have a separate toggle?

**Context:** `GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints` currently forces auth on all generated MVC controllers. Should turning this on also force auth on the SignalR hub?

| Option | Description | Impact |
|--------|-------------|--------|
| **(A) Same toggle controls both (recommended)** | When `Mode == RequireAuthorizationForAllGeneratedEndpoints`, both MVC endpoints AND SignalR subscriptions require auth. `AllowAnonymousOptOut` also applies to subscriptions. | Simpler config. Single source of truth. Matches user expectation that "require auth for all generated endpoints" includes SignalR. |
| **(B) Separate `HubAuthorizationMode` toggle** | Add a new property to `GeneratedApiAuthorizationOptions` like `HubMode` that independently controls hub auth. | More granular control. More config surface. Risk of confusion ("I enabled auth but subscriptions are still open"). |
| **(X) Pick best default** | → Option A. | |

---

### Q6: How should subscribe denial be communicated to the client?

**Context:** When `SubscribeAsync` fails auth, what should the client receive?

| Option | Description | Impact |
|--------|-------------|--------|
| **(A) Throw HubException (recommended)** | Throw `HubException("Access denied")` from `SubscribeAsync`. Client already catches exceptions and dispatches error actions via `InletSignalRActionEffect`. | Clean. No protocol changes. Client handles it today. Error message in Redux store. |
| **(B) Return error value** | Return a special string like `"error:access-denied"` instead of the subscription ID. | Requires client to parse error codes. More complex. No standard. |
| **(C) Silent rejection** | Return a valid-looking subscription ID but never send updates. | Security through obscurity. Violates principle of least surprise. Bad DX. |
| **(X) Pick best default** | → Option A. | |

---

### Q7: Should `[GenerateAllowAnonymous]` on a projection exempt its subscription from hub-level forced auth?

**Context:** When force mode is on, projections with `[GenerateAllowAnonymous]` currently get `[AllowAnonymous]` on their HTTP endpoints (if `AllowAnonymousOptOut` is true). Should the same exemption apply to SignalR subscriptions?

| Option | Description | Impact |
|--------|-------------|--------|
| **(A) Yes, consistent behavior (recommended)** | `[GenerateAllowAnonymous]` exempts the projection's subscription from hub-level auth check, mirroring HTTP behavior. `AllowAnonymousOptOut` controls whether the exemption is honored. | Consistent. One mental model for domain developers. |
| **(B) No, hub auth is always enforced** | Even with `[GenerateAllowAnonymous]`, subscribing requires authentication. Only the HTTP GET is anonymous. | Different behavior between HTTP and SignalR for the same projection. Confusing. |
| **(X) Pick best default** | → Option A. | |

---

### Q8: Should unauthenticated connections be allowed to connect to the hub at all when force mode is off?

**Context:** When `Mode == Disabled`, the hub is open (current behavior). When force mode is on, should the hub endpoint itself require authentication, or should only individual subscriptions be checked?

| Option | Description | Impact |
|--------|-------------|--------|
| **(A) Hub-level auth when force mode is on (recommended)** | When force mode is on, `MapInletHub()` chains `.RequireAuthorization()`. Unauthenticated clients can't even connect. Individual subscriptions still checked for projection-specific policies. | Defense in depth. Fail-fast. Reduces attack surface. Still allows projection-specific policies for differentiated access. |
| **(B) Hub always open, auth only per-subscription** | Hub accepts all connections. Auth checked only in `SubscribeAsync`. | Allows connection probing. Unauthenticated users see "connected" then get errors on subscribe. |
| **(C) Hub-level auth always (even when mode is disabled)** | Hub always requires authentication regardless of mode. | Breaks existing unauthenticated setups. Too aggressive. |
| **(X) Pick best default** | → Option A. | |
