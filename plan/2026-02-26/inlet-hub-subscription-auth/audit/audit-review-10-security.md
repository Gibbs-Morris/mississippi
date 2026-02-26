# Review 10 — Security Engineer

**Reviewer persona:** Security Engineer — evaluating authentication/authorization model correctness, trust boundary enforcement, claims validation, tenant isolation, input validation, serialization attack surface, secret handling, OWASP alignment.

*This review is based solely on `04-draft-plan.md` and the repository structure.*

---

## Feedback

### 1. Hub-level auth closes the unauthenticated connection attack surface — good

- **Issue:** None — positive feedback.
- **Why it matters:** Currently, any client can establish a WebSocket connection. Even without subscribing, the connection consumes server resources (memory for the connection, grain activation for cleanup on disconnect). Hub-level auth when force mode is on prevents resource exhaustion from unauthenticated connections.
- **Evidence:** `InletHub.OnDisconnectedAsync` calls `subscriptionGrain.ClearAllAsync()` — even a connect/disconnect cycle activates a grain.
- **Confidence:** High.

### 2. `HubException` message should NOT leak policy names in production

- **Issue:** The plan proposes "Access denied for projection '{path}'" in the error message. This reveals which projection paths exist and that they're protected — an information leak.
- **Why it matters:** An attacker can enumerate protected projection paths by attempting subscriptions and observing error messages. Even the path itself confirms existence.
- **Proposed change:** Use a generic message: "Subscription denied." Do not include the projection path, policy name, or any identifying information. Log the details server-side at Warning level for debugging. In development environments (via `IHostEnvironment.IsDevelopment()`), a more detailed message is acceptable.
- **Evidence:** OWASP guidelines: "Error messages should not reveal implementation details." ASP.NET Core's default 403 handler returns no body.
- **Confidence:** High — this is a must-fix.

### 3. Rate limiting on `SubscribeAsync` not addressed

- **Issue:** An authenticated user could rapidly call `SubscribeAsync` with different paths to probe which projections exist and which they're authorized for.
- **Why it matters:** Even with auth, enumeration attacks are possible. Each subscribe attempt triggers a registry lookup + auth evaluation + logging.
- **Proposed change:** This is a general hub security concern, not specific to this feature. Rate limiting can be applied via ASP.NET Core's rate limiting middleware or a hub filter. Document as out-of-scope but note the recommendation.
- **Evidence:** The plan's non-goals don't mention rate limiting. This is a cross-cutting concern applicable to all hub methods.
- **Confidence:** Medium — valid concern but out of scope for this feature.

### 4. Token refresh: authenticated connections may outlive token expiry

- **Issue:** SignalR WebSocket connections are long-lived. A JWT token valid at connection time may expire while the connection is still active. The hub-level auth check happens at connection establishment, not per-message.
- **Why it matters:** A user whose token expires (or whose access is revoked) continues receiving notifications until they disconnect/reconnect.
- **Proposed change:** This is a known limitation of SignalR auth. Mitigations: (1) Configure token expiry shorter than expected session length. (2) Use SignalR's `OnConnectedAsync`/`OnDisconnectedAsync` to track connections and force disconnect on revocation. (3) Use `IHubFilter` with per-invocation token validation. All are expensive — document as a known trade-off.
- **Evidence:** Same limitation applies to HTTP long-polling. ASP.NET Core docs: "The token is validated at connection time." This is standard SignalR behavior, not a plan deficiency.
- **Confidence:** High — document as known limitation, not blocking.

### 5. `SubscribeAsync` input validation prevents injection

- **Issue:** None — existing validation is adequate.
- **Why it matters:** `path` and `entityId` are passed to `IInletSubscriptionGrain` and used as dictionary keys. `ArgumentException.ThrowIfNullOrEmpty` prevents null/empty injection. The values are strings used for lookup — no SQL, no template rendering.
- **Evidence:** `InletHub.SubscribeAsync` already calls `ArgumentException.ThrowIfNullOrEmpty(path)` and `ArgumentException.ThrowIfNullOrEmpty(entityId)`.
- **Confidence:** High.

### 6. AllowAnonymous opt-out is a security-positive default

- **Issue:** None — positive feedback.
- **Why it matters:** `AllowAnonymousOptOut` defaults to `true`, meaning `[GenerateAllowAnonymous]` is respected. When set to `false`, it OVERRIDES AllowAnonymous and forces auth. This gives operators a hard security override regardless of developer annotations.
- **Evidence:** `GeneratedApiAuthorizationOptions.AllowAnonymousOptOut` default is `true`. When `false`, even `[GenerateAllowAnonymous]` projections require the default policy.
- **Confidence:** High.

### 7. Cross-tenant isolation is out of scope but should be flagged

- **Issue:** The plan is projection-path-level auth, not entity-level. In a multi-tenant system, User A can subscribe to `accounts/{tenantB-entityId}` if they pass the path-level policy.
- **Why it matters:** For SaaS applications, path-level auth is insufficient — tenant isolation requires entity-level checks.
- **Proposed change:** Explicitly mark per-entity auth as a post-1.0 concern. The architecture (inlining auth in hub → extracting to `ISubscriptionAuthorizationService`) supports this evolution.
- **Evidence:** Non-goals section already states: "Per-entity authorization (e.g., 'user X can only see account Y') — this is projection-path-level only."
- **Confidence:** High — correctly scoped. Document the tenant isolation caveat.
