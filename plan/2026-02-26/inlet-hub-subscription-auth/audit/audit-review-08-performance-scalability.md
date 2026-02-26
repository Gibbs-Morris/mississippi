# Review 08 — Performance & Scalability Engineer

**Reviewer persona:** Performance & Scalability Engineer — evaluating hot-path allocation budgets, grain activation cost, serialization overhead, N+1 patterns, throughput bottlenecks, memory pressure.

*This review is based solely on `04-draft-plan.md` and the repository structure.*

---

## Feedback

### 1. Auth check adds latency to every `SubscribeAsync` call

- **Issue:** Each `SubscribeAsync` now performs: (1) ConcurrentDictionary lookup, (2) `IAuthorizationService.AuthorizeAsync` (which may invoke policy handlers). This is on the subscription hot path.
- **Why it matters:** A Blazor page loading 5-10 projections simultaneously will make 5-10 sequential auth checks. If using external policy providers (e.g., OPA, Auth0), each check could be 5-50ms.
- **Proposed change:** For in-memory policy evaluation (standard ASP.NET Core), overhead is negligible (~microseconds). Document that external policy providers should cache decisions if latency is a concern. The `ConcurrentDictionary.TryGetValue` is O(1).
- **Evidence:** `IProjectionBrookRegistry` uses `ConcurrentDictionary` — same performance characteristics. `IAuthorizationService` with default ASP.NET Core policy providers is in-memory.
- **Confidence:** High — not a real concern for standard setups. Worth noting for exotic auth providers.

### 2. `ProjectionAuthorizationEntry` record allocation per registry lookup

- **Issue:** The plan uses an immutable record. If `GetAuthorizationEntry` returns a new object per call, this adds GC pressure.
- **Why it matters:** Subscription calls are frequent during page loads.
- **Proposed change:** The registry stores pre-allocated `ProjectionAuthorizationEntry` instances — `ConcurrentDictionary.TryGetValue` returns the reference, no new allocation. Verify the implementation returns the stored instance, not a copy. Records with value semantics (no mutable state) are safe to share.
- **Evidence:** `ProjectionBrookRegistry` returns `string` values — no allocation. Records are reference types in C# — stored once, returned by reference.
- **Confidence:** High — no issue if implemented correctly (store once, return reference).

### 3. No N+1 pattern — single registry lookup per subscription

- **Issue:** None — positive feedback.
- **Why it matters:** The plan does one `ConcurrentDictionary` lookup per `SubscribeAsync` call. Not N lookups.
- **Evidence:** Auth flow: `authRegistry.GetAuthorizationEntry(path)` → single call.
- **Confidence:** High.

### 4. No serialization overhead — auth metadata stays server-side

- **Issue:** None — positive feedback.
- **Why it matters:** `ProjectionAuthorizationEntry` never crosses the wire. It's server-only, in-memory. No serialization/deserialization cost.
- **Evidence:** The entry is used in `InletHub` (ASP.NET Core) and registered in `InletSiloRegistrations` (silo). Both are server-side. No Orleans grain state or SignalR message includes auth metadata.
- **Confidence:** High.

### 5. Hub-level `.RequireAuthorization()` has zero hot-path cost

- **Issue:** None — positive feedback.
- **Why it matters:** The `RequireAuthorization()` call on the endpoint is middleware config at startup, not per-request. The auth middleware evaluates once during the WebSocket upgrade request, not per hub method call.
- **Evidence:** ASP.NET Core endpoint auth: authorization middleware runs during `HttpContext` pipeline for the initial connection, not per SignalR frame.
- **Confidence:** High.

### 6. Memory footprint of auth registry is negligible

- **Issue:** None — verification.
- **Why it matters:** The registry stores one `ProjectionAuthorizationEntry` per projection path. A typical application has 5-50 projections.
- **Proposed change:** None.
- **Evidence:** 50 entries × ~100 bytes per record = ~5KB. `ConcurrentDictionary` overhead per entry is ~100 bytes. Total: ~10KB for a large application. Negligible.
- **Confidence:** High.

---

Overall: No performance concerns. The auth check is O(1) dictionary lookup + in-memory policy evaluation. No allocations, serialization, or N+1 patterns introduced.
