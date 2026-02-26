# Review 12 — Data Integrity & Storage Engineer

**Reviewer persona:** Data Integrity & Storage Engineer — evaluating Cosmos DB partition key design, storage-name contract immutability, event stream consistency, snapshot correctness, idempotent writes, and data migration strategy.

*This review is based solely on `04-draft-plan.md` and the repository structure.*

---

## Feedback

### 1. No storage-name or event schema changes — no data integrity risk

- **Issue:** None — strong positive feedback.
- **Why it matters:** The plan modifies ONLY the subscription authorization path (SignalR hub + runtime registry). No events, snapshots, storage names, partition keys, or serialization formats are touched. There is zero risk of data migration, orphaned events, or partition key mismatches.
- **Evidence:** "Files Changed" lists no files in `Brooks.Runtime.Storage.*`, `Tributary.Runtime.Storage.*`, or `Common.Runtime.Storage.*`.
- **Confidence:** High.

### 2. Projection auth registry is in-memory only — no persistence concern

- **Issue:** None — verification.
- **Why it matters:** The `IProjectionAuthorizationRegistry` is populated at startup from assembly reflection (same as `IProjectionBrookRegistry`). It's a `ConcurrentDictionary<string, ProjectionAuthorizationEntry>` stored in a singleton. No data is persisted to Cosmos, blob storage, or any durable store. Loss on restart is harmless because it's rebuilt from assembly metadata.
- **Evidence:** Plan Phase 1 describes `ProjectionAuthorizationRegistry` as an in-memory registry, following `ProjectionBrookRegistry` pattern.
- **Confidence:** High.

### 3. No Cosmos RU impact

- **Issue:** None — verification.
- **Why it matters:** Auth checks happen BEFORE any stream/storage operations. A denied subscription never reaches the event store or projection store. An allowed subscription proceeds with the same RU cost as before.
- **Evidence:** Plan Phase 3 describes auth check in `InletHub.SubscribeAsync` which runs before the `IInletSubscriptionGrain` call. No new Cosmos queries.
- **Confidence:** High.

### 4. No conflict resolution or TTL concerns

- **Issue:** None — verification.
- **Why it matters:** No new writes to any store. The auth check is a pure authorization gate on an existing SignalR message flow.
- **Confidence:** High.

### 5. Event stream consistency maintained

- **Issue:** None — verification.
- **Why it matters:** Denying a subscription doesn't affect the event stream. Events continue to be appended by aggregate grains regardless of subscription auth. Projection rebuilds are unaffected because they read from the event store directly, not through SignalR.
- **Evidence:** Review 07 (Event Sourcing/CQRS) confirmed same finding — auth is on the notification path, not the event path.
- **Confidence:** High.

### 6. No data migration required

- **Issue:** None — strong positive feedback.
- **Why it matters:** This is a purely additive runtime feature. No existing data formats change. No migration scripts needed. Deployments can enable the feature via configuration (auth mode toggle) without touching stored data.
- **Evidence:** Plan Phase 5 (Spring sample) only changes `Program.cs` configuration and adds test types — no storage schema changes.
- **Confidence:** High.
