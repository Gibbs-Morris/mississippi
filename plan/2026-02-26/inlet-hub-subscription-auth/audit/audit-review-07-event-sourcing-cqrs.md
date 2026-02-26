# Review 07 — Event Sourcing & CQRS Specialist

**Reviewer persona:** Event Sourcing & CQRS Specialist — evaluating event schema evolution, storage-name immutability, reducer purity, aggregate invariant enforcement, projection rebuild-ability, command/event separation, idempotency.

*This review is based solely on `04-draft-plan.md` and the repository structure.*

---

## Feedback

### 1. Auth metadata is read-side only — correct CQRS boundary

- **Issue:** None — positive feedback.
- **Why it matters:** The plan adds authorization to the read (projection subscription) path only. Write-side (commands) already have their own auth via generated controllers. The CQRS boundary is respected: read auth and write auth are independent.
- **Evidence:** `[GenerateAuthorization]` on projections → affects HTTP GET + SignalR subscription. `[GenerateAuthorization]` on commands → affects HTTP POST. Different generated controllers. No coupling.
- **Confidence:** High.

### 2. No impact on event schema, storage names, or reducers

- **Issue:** None — verification that the plan is purely transport-layer.
- **Why it matters:** Event sourcing data integrity requires immutable event schemas and storage names. The plan introduces no new events, no new storage, no reducer changes.
- **Evidence:** The plan modifies hub behavior and assembly scanning only. No files in `DomainModeling.*`, `Brooks.*`, or `Tributary.*` are touched.
- **Confidence:** High.

### 3. Projection rebuild-ability unaffected

- **Issue:** None — verification.
- **Why it matters:** Projections must be rebuildable from event history. Auth metadata is a transport concern, not a projection concern. Rebuilding projections doesn't interact with subscription auth.
- **Evidence:** Auth registry is populated at startup from attributes, not from event state. Projection rebuild reads events → applies reducers → produces state. Auth is checked only when subscribing to updates, not during rebuild.
- **Confidence:** High.

### 4. Subscription notifications remain idempotent

- **Issue:** None — verification.
- **Why it matters:** The `ProjectionUpdated(path, entityId, newVersion)` notification is inherently idempotent — receiving it twice just causes an extra HTTP GET which returns the same state. Auth checks don't change this.
- **Evidence:** Client `InletSignalRActionEffect` fetches the projection via HTTP after receiving a notification. HTTP auth is already in place. Double notification → double fetch → same result.
- **Confidence:** High.

### 5. No saga compensation concerns

- **Issue:** None — no sagas involved.
- **Why it matters:** The plan is purely about subscription authorization. No saga flows, compensation events, or distributed transactions are affected.
- **Evidence:** `SagaControllerGenerator` is mentioned in `GeneratedApiAuthorizationConvention` for MVC auth but is unrelated to subscription auth.
- **Confidence:** High.

---

Overall: This plan has minimal event sourcing impact. It's entirely a gateway/transport concern. No concerns from an event sourcing perspective.
