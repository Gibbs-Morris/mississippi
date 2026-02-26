# Review 04 — Technical Architect

**Reviewer persona:** Technical Architect — evaluating architecture soundness, module boundaries, dependency direction, abstraction layering, evolution and extensibility strategy.

*This review is based solely on `04-draft-plan.md` and the repository structure.*

---

## Feedback

### 1. Dependency direction is correct: Gateway → Runtime → Abstractions

- **Issue:** None — positive feedback.
- **Why it matters:** The plan places the interface in `Inlet.Runtime.Abstractions`, implementation in `Inlet.Runtime`, consumption in `Inlet.Gateway`. This matches the existing dependency graph exactly.
- **Evidence:** `Inlet.Gateway.csproj` → `Inlet.Runtime.csproj` → `Inlet.Runtime.Abstractions.csproj`. Auth registry follows the same flow.
- **Confidence:** High.

### 2. Auth check in the hub layer (not the grain layer) is architecturally correct

- **Issue:** None — positive feedback.
- **Why it matters:** Auth belongs at the edge (gateway), not deep in the Orleans grain tier. The grain should not be aware of HTTP/SignalR auth context. This keeps grains transport-agnostic.
- **Evidence:** `InletSubscriptionGrain` has no auth logic — it manages stream subscriptions only. The hub is the gateway edge.
- **Confidence:** High.

### 3. The `IAuthorizationService` dependency in the hub may not respect hub DI scope correctly

- **Issue:** SignalR hubs are transient by default — created per invocation. `IAuthorizationService` is typically scoped. Injecting scoped services into transient hubs requires `IServiceScopeFactory` or `IServiceProvider` to create per-call scopes.
- **Why it matters:** If `IAuthorizationService` resolves from the wrong scope, it could share state across invocations or fail with scope validation errors.
- **Proposed change:** Verify whether ASP.NET Core's hub activation already creates a scope per invocation (it does — hubs get a scoped service provider per call). If so, direct injection is safe. Document this in the plan.
- **Evidence:** ASP.NET Core docs: "A new Hub instance is created for each hub method invocation... Services are obtained from the DI scope created for each invocation." This means `IAuthorizationService` injection is safe.
- **Confidence:** High — ASP.NET Core handles scoping. No action needed, but worth noting.

### 4. Policy evaluation may require `AuthorizationPolicy` building from string fields

- **Issue:** The plan shows `IAuthorizationService.AuthorizeAsync(Context.User, defaultPolicy)` but `IAuthorizationService` works with `AuthorizationPolicy` objects or policy names. When using roles/schemes (not just named policies), you can't just pass a policy name — you need to build an `AuthorizationPolicy` from the components.
- **Why it matters:** If a projection has `[GenerateAuthorization(Roles = "admin")]` with no policy name, `IAuthorizationService` needs a constructed policy, not a string lookup.
- **Proposed change:** Use `IAuthorizationPolicyProvider` to build policies, or construct `AuthorizationPolicy` inline using `AuthorizationPolicyBuilder`. The implementation should handle: (1) named policy only, (2) roles only, (3) auth schemes only, (4) combination of all three. The `GeneratedApiAuthorizationConvention` does this with `AuthorizeAttribute` — similar logic needed.
- **Evidence:** `GeneratedApiAuthorizationConvention.CreateDefaultAuthorizeFilter()` builds `AuthorizeAttribute` with roles/policy/schemes. The hub auth check needs equivalent logic for `IAuthorizationService`.
- **Confidence:** High — this is an implementation detail the plan should acknowledge.

### 5. Evolution path: per-entity authorization

- **Issue:** The plan explicitly marks per-entity authorization as a non-goal. But the architecture should not preclude it.
- **Why it matters:** Future requirements like "user X can only subscribe to their own account" will need per-entity checks. The auth registry is path-level — entity-level would need a different mechanism.
- **Proposed change:** No change needed now. The plan's design is clean enough that per-entity auth could be added as an `ISubscriptionAuthorizationService` injected into the hub, evaluated after the path-level check. Worth noting this extensibility in the plan.
- **Evidence:** The plan proposes extracting auth logic (review-03 feedback) — an `ISubscriptionAuthorizationService` interface would naturally support this extension.
- **Confidence:** Medium — future-proofing assessment, not a current issue.

### 6. Consider whether `UnsubscribeAsync` needs auth checks

- **Issue:** The plan only authorizes `SubscribeAsync`. `UnsubscribeAsync` is unguarded — but it takes a `subscriptionId` that was returned from `SubscribeAsync`.
- **Why it matters:** A malicious client could try to unsubscribe others' subscriptions by guessing subscription IDs.
- **Proposed change:** Verify that the subscription grain validates the connection ID owns the subscription before unsubscribing. If it does (which is likely given the grain is keyed by connection ID), no hub-level auth is needed for unsubscribe.
- **Evidence:** `InletHub.UnsubscribeAsync` calls `GrainFactory.GetGrain<IInletSubscriptionGrain>(Context.ConnectionId)` — the grain is per-connection, so a client can only interact with its own subscriptions. This is safe.
- **Confidence:** High — no change needed. Subscription grain isolation handles this.
