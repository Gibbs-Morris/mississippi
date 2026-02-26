# Review 02 — Solution Engineering

**Reviewer persona:** Solution Engineering — evaluating business adoption readiness, ecosystem/standards compliance, onboarding friction, integration patterns with third-party systems.

*This review is based solely on `04-draft-plan.md` and the repository structure.*

---

## Feedback

### 1. SignalR auth token transport not addressed

- **Issue:** The plan enables hub-level auth when force mode is on, but doesn't address how auth tokens reach the hub. Standard SignalR WebSocket connections send tokens via query string (`?access_token=...`) because WebSockets don't support custom headers after the initial handshake.
- **Why it matters:** Consumers using JWT bearer auth will need to configure `JwtBearerEvents.OnMessageReceived` to extract the token from the query string, or use the `HttpMessageHandler` approach. Without guidance, they'll get 401 at connection time and not know why.
- **Proposed change:** Add a note in the migration section about SignalR auth token transport. Reference ASP.NET Core's standard pattern for JWT + SignalR.
- **Evidence:** ASP.NET Core docs: "For JavaScript/Blazor clients, the token is provided as a query string parameter because WebSockets and Server-Sent Events can't set headers." This is a common gotcha.
- **Confidence:** High — this is a well-known friction point.

### 2. Third-party SignalR clients may not handle `HubException` consistently

- **Issue:** The plan assumes all clients handle `HubException` gracefully. Non-Blazor clients (React, Angular, mobile) using the SignalR JavaScript/Java/Swift client may handle errors differently.
- **Why it matters:** Mississippi is a framework — consumers may use non-Blazor clients. The auth denial behavior should be predictable across client stacks.
- **Proposed change:** Document the expected error behavior for non-Blazor clients. The HubException message is serialized as a string error in the SignalR protocol — standard across all clients. Note this in the plan.
- **Evidence:** SignalR protocol spec: HubException sends an `error` field in the Completion message. All official clients expose this.
- **Confidence:** Medium — the behavior is standard, but documentation prevents confusion.

### 3. `[GenerateAuthorization]` combines HTTP and hub auth implicitly

- **Issue:** A domain developer adding `[GenerateAuthorization(Policy = "admin")]` to a projection now gets auth on both HTTP GET and SignalR subscribe. There's no way to apply auth to one transport without the other.
- **Why it matters:** Some domains may want public HTTP reads but restricted subscriptions (or vice versa). The attribute is transport-agnostic which is elegant but not granular.
- **Proposed change:** This is acceptable for the initial implementation. Document the implicit coupling. If demand arises post-1.0, a transport-specific attribute could be added.
- **Evidence:** The plan's non-goals explicitly exclude per-transport granularity. Pre-1.0 allows iteration.
- **Confidence:** High — correct trade-off for now.

### 4. No guidance on custom `IHubFilter` for cross-cutting auth

- **Issue:** ASP.NET Core supports `IHubFilter` for cross-cutting concerns on hub method invocations. The plan inlines auth logic in `SubscribeAsync`. An alternative using `IHubFilter` would be more extensible.
- **Why it matters:** If consumers need to add custom auth logic (e.g., tenant isolation, rate limiting), a filter pipeline would be more composable.
- **Proposed change:** Consider mentioning `IHubFilter` as a future extensibility option. The initial implementation in `SubscribeAsync` is fine for framwork-controlled auth, but the plan should acknowledge the filter pattern exists.
- **Evidence:** ASP.NET Core docs: `IHubFilter.InvokeMethodAsync` wraps all hub method calls.
- **Confidence:** Low — nice-to-know, not blocking. Inline approach is simpler and sufficient.

### 5. Integration test gap

- **Issue:** The testing strategy only covers L0 and L1. No L2 integration test is planned to verify end-to-end SignalR auth with a real ASP.NET Core host.
- **Why it matters:** Unit tests mock `IAuthorizationService` but don't verify the actual middleware pipeline (JWT validation → SignalR negotiate → hub auth → subscribe auth).
- **Proposed change:** Add an L2 test recommendation using the Aspire AppHost pattern (consistent with `testing.instructions.md`). At minimum, verify that: (1) authenticated client can subscribe, (2) unauthenticated client is rejected when force mode is on.
- **Evidence:** `tests/Aqueduct.Gateway.L2Tests/` and `tests/Aqueduct.Gateway.L2Tests.AppHost/` show the Aspire L2 pattern is established.
- **Confidence:** High — L2 test is valuable but can be deferred after L0/L1 gate.
