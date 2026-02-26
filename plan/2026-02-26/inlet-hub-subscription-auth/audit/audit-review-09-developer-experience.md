# Review 09 — Developer Experience (DX) Reviewer

**Reviewer persona:** Developer Experience — evaluating API ergonomics, discoverability, pit-of-success design, error message quality, IntelliSense/doc-comments, registration ceremony, concepts to learn, migration friction.

*This review is based solely on `04-draft-plan.md` and the repository structure.*

---

## Feedback

### 1. Zero new concepts for domain developers — excellent pit-of-success

- **Issue:** None — strong positive feedback.
- **Why it matters:** Domain developers already know `[GenerateAuthorization]` and `[GenerateProjectionEndpoints]`. The plan adds zero new attributes, zero new configuration, zero domain code changes. Existing decorated projections automatically get subscription auth.
- **Evidence:** `AuthProofProjection` with `[GenerateAuthorization(Policy = "spring.auth-proof.claim")]` — after implementation, its subscription is protected. Developer did nothing new.
- **Confidence:** High.

### 2. Error messages from `HubException` should be actionable

- **Issue:** The plan says "throw HubException('Access denied for projection {path}')" but doesn't specify whether the required policy/role is included. A developer debugging a 403-equivalent error needs to know WHAT authorization is required.
- **Why it matters:** "Access denied" without context leads to trial-and-error debugging. "Access denied: policy 'spring.auth-proof.claim' required" is immediately actionable.
- **Proposed change:** Include the policy/role in the error message for development environments. Consider using `IHostEnvironment.IsDevelopment()` to switch between detailed and generic messages. In production, generic is fine for security.
- **Evidence:** ASP.NET Core's default 403 responses don't include policy details for security. But in development, more detail helps. The hub could follow the same pattern.
- **Confidence:** Medium — worth discussing. Development-only detail is a good compromise.

### 3. IntelliSense should explain the dual purpose of `[GenerateAuthorization]`

- **Issue:** The attribute's XML doc says "Emits an ASP.NET Core [Authorize] attribute on generated HTTP APIs." After this change, it also protects SignalR subscriptions — but the doc doesn't mention that.
- **Why it matters:** A developer reading the attribute's doc in IntelliSense should understand the full scope of what it protects.
- **Proposed change:** Update `GenerateAuthorizationAttribute` XML doc to mention both HTTP endpoints and SignalR subscriptions.
- **Evidence:** Current doc: "Apply this attribute to aggregate, command, projection, or saga types that participate in source-generated HTTP API surfaces." Should also say "and SignalR projection subscriptions."
- **Confidence:** High — documentation update is essential for discoverability.

### 4. `MapInletHub` silent behavior change may surprise developers

- **Issue:** `MapInletHub()` currently returns an unprotected hub endpoint. After the change, when force mode is on, it silently adds `.RequireAuthorization()`. The method signature doesn't change — the behavior change is implicit based on options.
- **Why it matters:** A developer calling `MapInletHub()` after upgrading may not realize their hub now requires auth. Their clients break with no compile-time warning.
- **Proposed change:** Consider adding an XML doc remark to `MapInletHub` explaining the conditional auth behavior. The behavior is correct and desirable, but should be discoverable.
- **Evidence:** `GeneratedApiAuthorizationConvention` has the same implicit behavior (MVC controllers are silently authorized when mode is on). This is consistent — but worth documenting.
- **Confidence:** High — doc update, not code change.

### 5. Registration ceremony unchanged — no new chaining required

- **Issue:** None — positive feedback.
- **Why it matters:** Developers don't need to add `.AddProjectionAuth()` or `.UseSubscriptionAuth()`. The existing `AddInletServer()` + `ScanProjectionAssemblies()` + `MapInletHub()` calls handle everything.
- **Evidence:** Plan shows no API signature changes on these methods.
- **Confidence:** High.

### 6. Client-side error experience should be considered

- **Issue:** When `SubscribeAsync` throws `HubException`, the client dispatches `ProjectionActionFactory.CreateError()` to the Redux store. What does the UI show? The current error handling is likely a silent failure or console log.
- **Why it matters:** An unauthenticated user subscribing to a protected projection should see something meaningful, not a silent failure.
- **Proposed change:** This is a sample-level concern, not a framework concern. The framework provides the error action — the sample should handle it in the UI. Consider adding a note in the migration guide about handling `SubscribeError` actions in the UI.
- **Evidence:** The plan's non-goals exclude client-side changes. The error action is dispatched to the store — UI rendering is the sample's responsibility.
- **Confidence:** Medium — not blocking for framework implementation.
