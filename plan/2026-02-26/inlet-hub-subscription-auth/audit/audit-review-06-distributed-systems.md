# Review 06 — Distributed Systems Engineer

**Reviewer persona:** Distributed Systems Engineer — evaluating Orleans actor-model correctness, grain lifecycle, reentrancy, single-activation guarantees, message ordering, turn-based concurrency pitfalls.

*This review is based solely on `04-draft-plan.md` and the repository structure.*

---

## Feedback

### 1. Auth check in hub (gateway) keeps grains auth-free — correct placement

- **Issue:** None — positive feedback.
- **Why it matters:** Grains operate within the Orleans silo where there is no HTTP context, no `ClaimsPrincipal`, no auth middleware. Auth must be checked before crossing the gateway→silo boundary. The plan correctly checks in `InletHub.SubscribeAsync` before invoking `IInletSubscriptionGrain`.
- **Evidence:** `InletSubscriptionGrain` is keyed by `connectionId` and manages stream subscriptions. It has no access to user identity.
- **Confidence:** High.

### 2. No reentrancy concerns — hub auth is synchronous from the grain's perspective

- **Issue:** None — positive feedback.
- **Why it matters:** The auth check happens in the hub (ASP.NET Core), not in a grain. `IAuthorizationService.AuthorizeAsync` resolves before the grain call. The grain's single-threaded execution model is unaffected.
- **Evidence:** `InletHub.SubscribeAsync` awaits auth THEN calls grain. No interleaving.
- **Confidence:** High.

### 3. Race condition: authorization check timing vs. subscription lifetime

- **Issue:** A user's claims could change (e.g., role removed) between the `SubscribeAsync` auth check and subsequent `ProjectionUpdated` notifications. The subscription continues pushing notifications even though the user is no longer authorized.
- **Why it matters:** In a multi-silo deployment, a user could receive notifications for projections they've been de-authorized from.
- **Proposed change:** This is an inherent limitation of "subscribe-time auth" vs. "per-notification auth." Per-notification auth is prohibitively expensive (every stream event would need policy evaluation). Document this as a known trade-off. If needed later, a "reauthorize" sweep could periodically verify subscriptions — but this is a post-1.0 concern.
- **Evidence:** The HTTP endpoint pattern has the same limitation — once a request starts, mid-request credential revocation isn't enforced. Standard web security behavior.
- **Confidence:** High — accepted trade-off, not a bug.

### 4. Multi-silo deployment: auth registry consistency

- **Issue:** The `IProjectionAuthorizationRegistry` is populated per-silo via `ScanProjectionAssemblies`. In a multi-silo deployment, all silos scan the same assemblies and should produce identical registries.
- **Why it matters:** If the scanned assemblies differ between silos (misconfigured deployment), auth behavior would be inconsistent.
- **Proposed change:** No code change needed. This is the same situation as `IProjectionBrookRegistry` — all silos must scan the same assemblies. Document that this is a deployment requirement (already implicit).
- **Evidence:** `ScanProjectionAssemblies` is deterministic given the same input assemblies. `ProjectionBrookRegistry` has the same deployment requirement.
- **Confidence:** High — no action needed.

### 5. Hub auth check doesn't affect Orleans stream lifecycle

- **Issue:** None — verification that the plan doesn't break stream management.
- **Why it matters:** Orleans streams (via Aqueduct backplane) are subscribed/unsubscribed through the `IInletSubscriptionGrain`. The auth check is a gate before the grain call. If auth fails, the grain is never called — no stream subscription created.
- **Evidence:** Plan flow: SubscribeAsync → auth check → pass → grain.SubscribeAsync. Fail → throw HubException, grain never touched. Clean.
- **Confidence:** High.
