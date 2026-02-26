# Review 05 — Platform Engineer

**Reviewer persona:** Platform Engineer — evaluating operability, telemetry, structured logging, distributed tracing, alerting hooks, failure modes, night-time diagnosis, deployment rollout safety.

*This review is based solely on `04-draft-plan.md` and the repository structure.*

---

## Feedback

### 1. Logging strategy is solid but missing structured fields

- **Issue:** The plan proposes three log messages (Succeeded, Failed, Skipped) but doesn't specify structured log field names. For night-time diagnosis, operators need to query by `ConnectionId`, `Path`, `EntityId`, `Policy`, `UserId`, etc.
- **Why it matters:** Structured logging enables log aggregation queries. Without explicit field names, implementations may use inconsistent names.
- **Proposed change:** Specify the `[LoggerMessage]` parameter names explicitly. The existing `InletHubLoggerExtensions` already uses `connectionId`, `path`, `entityId` — maintain consistency. Add `policyName` or `reason` for auth-specific messages.
- **Evidence:** `InletHubLoggerExtensions.cs` already defines `ClientConnected(connectionId)`, `SubscribingToProjection(connectionId, path, entityId)`, etc. Same naming pattern should be used.
- **Confidence:** High.

### 2. No metrics or counters mentioned

- **Issue:** The observability section covers logging but no metrics. Auth denied/allowed counts are valuable for dashboards and alerting.
- **Why it matters:** Operators need to detect auth misconfiguration (e.g., sudden spike in denials after deployment) without reading logs.
- **Proposed change:** Consider adding optional metrics in a future iteration. Not blocking for initial implementation, but note it as a follow-up. Could use `System.Diagnostics.Metrics` counters for `inlet.hub.subscribe.auth.denied` and `inlet.hub.subscribe.auth.allowed`.
- **Evidence:** The framework doesn't currently use `System.Diagnostics.Metrics` — adding it would be a new pattern. Logging is sufficient for initial implementation.
- **Confidence:** Medium — defer to follow-up.

### 3. Failure mode table is good but missing cascading failure scenario

- **Issue:** The table covers individual failures but not cascading scenarios. Example: if `IAuthorizationService` is slow (e.g., external policy provider), every `SubscribeAsync` call will be delayed, potentially causing SignalR connection timeouts or backpressure.
- **Why it matters:** In production, auth service latency directly impacts subscription latency. SignalR has default timeouts (30s for invoke).
- **Proposed change:** Add a consideration for auth service latency. If using external policy providers, recommend configuring timeout/circuit-breaker at the policy provider level, not in the hub.
- **Evidence:** `InletHub.SubscribeAsync` is called per-subscription. If a user subscribes to 10 projections on page load, that's 10 sequential auth checks.
- **Confidence:** Medium — depends on auth service implementation. Worth noting.

### 4. Deployment rollout: feature flag/gradual rollout not mentioned

- **Issue:** The plan treats force mode as all-or-nothing. There's no gradual rollout strategy — when you deploy, all hub connections are instantly either open or authenticated.
- **Why it matters:** A misconfigured deployment could lock out all clients. Rollback requires redeployment.
- **Proposed change:** The existing `Mode` toggle IS the feature flag (default `Disabled`). Document that: deploy first, then enable mode in config. Config change triggers restart. This is safe for the typical ASP.NET Core config change flow.
- **Evidence:** `GeneratedApiAuthorizationMode` defaults to `Disabled`. Consumers opt in. This is already gradual.
- **Confidence:** High — no code change needed, just deployment guidance.

### 5. Health check / diagnostics endpoint for auth registry state

- **Issue:** No diagnostic endpoint to verify what auth entries are registered. If something goes wrong, operators have no way to inspect the registry state without attaching a debugger.
- **Why it matters:** Night-time diagnosis: "Why can't user X subscribe to projection Y?" → check if projection Y even has an auth entry → currently no way to do this from the running system.
- **Proposed change:** Defer to a future operability improvement. The `IProjectionAuthorizationRegistry.GetAllPaths()` method enables building a diagnostic endpoint later. Not blocking.
- **Evidence:** `IProjectionBrookRegistry.GetAllPaths()` exists but has no diagnostic endpoint either. Same pattern — registry is inspectable via DI but not exposed via HTTP.
- **Confidence:** Low — nice-to-have, not blocking.
