# EventSourcing.Cosmos TODO

This document captures the outstanding improvements and fixes to make the
Cosmos provider production‑ready. Items are grouped by area, with rationale
and clear acceptance criteria for future pickup.

## Concurrency: ETag support for head updates

- **Problem**: `head` updates use unconditional replace; no ETag precondition
  → races can overwrite head.
- **Scope**: `src/EventSourcing.Cosmos/Storage/CosmosRepository.cs`,
  `src/EventSourcing.Cosmos/Storage/HeadDocument.cs`,
  `src/EventSourcing.Cosmos/Storage/HeadStorageModel.cs`,
  `src/EventSourcing.Cosmos/Mapping/HeadDocumentToStorageMapper.cs`,
  appender paths.
- **Approach**:
  - Add ETag fields: add `string? ETag` to `HeadDocument` (map `_etag`) and
    to `HeadStorageModel`.
  - Map ETag in `HeadDocumentToStorageMapper`.
  - In `GetHeadDocumentAsync`, capture `response.ETag` and/or rely on `_etag`
    to populate `HeadStorageModel.ETag`.
  - In `ExecuteTransactionalBatchAsync`, use
    `TransactionalBatchItemRequestOptions { IfMatchEtag = currentHead.ETag }`
    when replacing `head`.
  - In `CommitHeadPositionAsync`, read current `head` and conditionally update
    using ETag as above.
- **Acceptance**:
  - Concurrent append attempts with diverging expected head fail with
    412‑style behavior (surfaced as a clear exception).
  - Tests demonstrate ETag is round‑tripped and enforced.

## Pending head validation and recovery hardening

- **Problem**: `head-pending` is committed without verifying the `head` still
  equals `originalPosition`. Orphaned `head-pending` with existing `head`
  isn't handled.
- **Scope**: `CosmosRepository.CommitHeadPositionAsync`,
  `CosmosRepository.CreatePendingHeadAsync`, `BrookRecoveryService`,
  `EventBrookAppender.AppendLargeBatchAsync`.
- **Approach**:
  - `CreatePendingHeadAsync`: before create, read `head`; if present and not
    equal to `currentHead`, abort with a specific concurrency error.
  - `CommitHeadPositionAsync`: read `head` (and `head-pending`) first; only
    commit if `head.Position == pending.originalPosition` using ETags on both
    update and delete.
  - Recovery: if `head` exists AND `head-pending` exists, verify range events;
    if none or incomplete, clean up stale pending; otherwise complete commit.
  - Large append: if `CreatePendingHeadAsync` returns 409 due to existing
    pending, invoke recovery flow (or fail with actionable message) instead
    of proceeding.
- **Acceptance**:
  - Stale `head-pending` never blocks writes permanently; recovery resolves
    it or surfaces a clear remediation error.
  - Commit refuses to advance when `head` diverged during long batch.

## Locking improvements (distributed blob leases)

- **What's done**: Added jittered retries on 409 conflicts and improved
  renewal timestamping.
- **Remaining**:
  - Add configurable max acquire wait/backoff to avoid immediate failure
    under contention.
  - Replace broad catches in recovery with lock‑specific errors end‑to‑end
    (bubble a domain exception type from lock manager for clarity).
- **Acceptance**:
  - Under lock contention, writers backoff for a bounded time and either
    succeed or return a clear timeout error.

## Batch limits and request sizing safety

- **What's done**: Translate 413 to clear error in batch executor.
- **Remaining**:
  - Pre‑check op count in `AppendEventBatchAsync` and
    `ExecuteTransactionalBatchAsync` (fail early if events would exceed
    100 operations or configured `MaxEventsPerBatch`).
  - Add unit tests covering 100‑op and ~2MB request thresholds using
    `BatchSizeEstimator`.
- **Acceptance**:
  - Oversize requests fail fast with actionable errors; tests document this
    behavior.

## DI bootstrap: safe, async container initialization

- **Problem**: Service registration blocks synchronously and may delete
  containers with mismatched PK path.
- **What's done**: Deletion replaced with fail‑fast error.
- **Remaining**:
  - Move Cosmos database/container provisioning to an async hosted initializer
    (`IHostedService` or `IStartupFilter`), no `.GetAwaiter().GetResult()`.
  - Optionally provide a toggle (e.g., `BrookStorageOptions.AutoProvision`)
    to allow bootstrapping in dev only.
  - Use `ILogger` instead of `Console`.
- **Acceptance**:
  - No sync‑over‑async in DI; startup is async‑safe; no automatic destructive
    actions.

## Partition key consistency validation

- **Problem**: Writers use `BrookKey.ToString()` as partition key; readers use
  `brookRange.ToBrookCompositeKey().ToString()`.
- **Action**: Verify both produce identical strings for the same brook; if not,
  align the APIs and add tests.
- **Acceptance**: Unit test asserting PK identity for `BrookKey` and
  `BrookRangeKey.ToBrookCompositeKey()`.

## Mapper and data validation polish

- Add `ETag` support as above for head docs.
- Consider validating non‑empty `BrookEvent.Id` before mapping to storage to
  avoid accidental duplicate logical events (position id remains unique, but
  domain ID hygiene helps).
- **Acceptance**: Mapper round‑trips required fields; optional guardrails
  configurable via options.

## Recovery flow resilience and diagnostics

- Improve `BrookRecoveryService` logs to include brook id, positions, and
  decisions (commit vs rollback) via `ILogger`.
- Add retries around recovery operations (already using `IRetryPolicy` in
  places; ensure all calls are covered).
- **Acceptance**: Clear, structured logs and resilient recovery under transient
  errors.

## Tests to add

- ETag concurrency tests for single‑batch and large‑batch paths.
- Pending‑head stale recovery test (head present, pending present).
- Batch limit tests (count and size).
- Lock contention tests with simulated 409s.
- Partition key identity test.

## Nice‑to‑have

- Expose metrics around batch sizes, RU charges, retry attempts, and lock wait
  durations.
- Option to tune `LeaseDurationSeconds`/`LeaseRenewalThresholdSeconds` per
  workload.

## File pointers

- `src/EventSourcing.Cosmos/Storage/CosmosRepository.cs`
- `src/EventSourcing.Cosmos/Brooks/EventBrookAppender.cs`
- `src/EventSourcing.Cosmos/Brooks/BrookRecoveryService.cs`
- `src/EventSourcing.Cosmos/Storage/HeadDocument.cs`
- `src/EventSourcing.Cosmos/Storage/HeadStorageModel.cs`
- `src/EventSourcing.Cosmos/Mapping/HeadDocumentToStorageMapper.cs`
- `src/EventSourcing.Cosmos/BrookStorageProviderRegistrations.cs`
- `src/EventSourcing.Cosmos/Locking/*`
