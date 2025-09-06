<!-- Auto-generated suggested test cases for EventSourcing.Cosmos provider project -->
# Mississippi EventSourcing.Cosmos – Suggested Test Cases

## Project Metadata

- Assembly: `Mississippi.EventSourcing.Cosmos`
- Focus: Cosmos DB persistence, batching, retry, locking, recovery.

## Global Invariants

- No batch exceeds Cosmos 100 operation limit.
- Max request size constraint respected (`BrookStorageOptions.MaxRequestSizeBytes`).
- Recovery process either commits or rolls back orphaned pending heads; no partial state.
- Lock acquisition & renewal logic: never renew too frequently, handles lost lease.

---

### File: BrookStorageProvider.cs

#### Type: BrookStorageProvider

Test Cases:

| ID | Scenario | G/W/T | Edge | Pri | Type |
|----|----------|-------|------|-----|------|
| BSP1 | ReadHeadPosition delegates | call -> recoveryService invoked | Delegation | M | Unit |
| BSP2 | ReadEventsAsync forwards enumeration | yield all from reader | Delegation | M | Unit |
| BSP3 | AppendEvents validates non-empty | empty list -> ArgException | Validation | H | Unit |
| BSP4 | AppendEvents delegates | non-empty -> appender called | Delegation | H | Unit |

---

### File: Batching/BatchSizeEstimator.cs

#### Type: BatchSizeEstimator

Test Cases:

| ID | Scenario | Description | Edge | Pri | Type |
|----|----------|-------------|------|-----|------|
| BSE1 | EstimateEventSize small | simple event -> positive size > data length | Basic | M | Unit |
| BSE2 | Large data path | >10MB triggers estimation branch | Size | M | Unit |
| BSE3 | Serialization failure fallback | invalid data causing JsonException -> fallback path | Error | L | Unit |
| BSE4 | Batch aggregation sums | two events -> size >= overhead + sum | Aggregation | M | Unit |
| BSE5 | CreateSizeLimitedBatches splits by count | events exceed maxEventsPerBatch -> multiple batches | Count | H | Unit |
| BSE6 | CreateSizeLimitedBatches splits by size | craft event causing size exceed | Size | H | Unit |
| BSE7 | Oversize single event | event larger than limit -> InvalidOperationException | Validation | H | Unit |

---

### File: Locking/BlobDistributedLock.cs

#### Type: BlobDistributedLock

Test Cases:

| ID | Scenario | Description | Edge | Pri | Type |
|----|----------|-------------|------|-----|------|
| BDL1 | Renew under threshold no-op | timeSinceLastRenewal < threshold -> no call to lease.RenewAsync | Threshold | M | Unit |
| BDL2 | Renew above threshold calls lease | advance time -> renew called updates timestamp | Renewal | H | Unit |
| BDL3 | Lost lease maps to InvalidOperationException | leaseClient throws 409 | Conflict | H | Unit |
| BDL4 | Dispose releases lease | DisposeAsync -> ReleaseAsync invoked | Dispose | M | Unit |
| BDL5 | Double dispose safe | second DisposeAsync no exception | Idempotence | L | Unit |

---

### File: Locking/BlobDistributedLockManager.cs

#### Type: BlobDistributedLockManager

Test Cases:

| ID | Scenario | Description | Edge | Pri | Type |
|----|----------|-------------|------|-----|------|
| DLM1 | Acquire creates container if missing | first call -> CreateIfNotExists | Init | M | Unit |
| DLM2 | Acquire uploads placeholder if missing | missing blob -> UploadAsync called | Init | M | Unit |
| DLM3 | Conflict retries with backoff | 409 on first attempts -> eventually succeed | Retry | H | Unit |
| DLM4 | Exhaust retries -> InvalidOperationException | all attempts 409 | Failure | H | Unit |

---

### File: Retry/CosmosRetryPolicy.cs

#### Type: CosmosRetryPolicy

Test Cases:

| ID | Scenario | Description | Edge | Pri | Type |
|----|----------|-------------|------|-----|------|
| RP1 | Succeeds first attempt | operation returns value | Basic | H | Unit |
| RP2 | Retries 429 then success | first TooManyRequests then success | Backoff | H | Unit |
| RP3 | Retries transient 503 sequence | 503 then success | Transient | M | Unit |
| RP4 | Non-retry NotFound surfaces | NotFound thrown -> propagated | Pass-through | H | Unit |
| RP5 | RequestEntityTooLarge -> InvalidOperation | 413 -> wraps with message | Size | H | Unit |
| RP6 | Exhaust retries -> InvalidOperationException | transient repeated beyond limit | Retry | H | Unit |
| RP7 | Cancellation respected | token canceled between attempts -> TaskCanceledException | Cancellation | M | Unit |

---

### File: Storage/CosmosRepository.cs

#### Type: CosmosRepository

Test Cases:

| ID | Scenario | Description | Edge | Pri | Type |
|----|----------|-------------|------|-----|------|
| CR1 | GetHeadDocumentAsync hit | container returns doc -> mapped | Basic | H | Unit |
| CR2 | GetHeadDocumentAsync miss | 404 -> null | NotFound | H | Unit |
| CR3 | CreatePendingHeadAsync uses retry | inject failing first attempt -> retried | Retry | M | Unit |
| CR4 | CommitHeadPositionAsync success | batch success -> no exception | Batch | H | Unit |
| CR5 | CommitHeadPositionAsync failure | batch fail -> InvalidOperationException | Error | H | Unit |
| CR6 | EventExistsAsync hit/miss | existing & 404 paths | NotFound | M | Unit |
| CR7 | GetExistingEventPositionsAsync accumulates | multiple pages -> combined set | Paging | M | Unit |
| CR8 | AppendEventBatchAsync over op limit guard | >100 ops -> InvalidOperationException | Limit | H | Unit |
| CR9 | ExecuteTransactionalBatchAsync create head new | NotSet head -> CreateItem used | Branch | M | Unit |
| CR10 | ExecuteTransactionalBatchAsync replace head | existing head -> ReplaceItem used | Branch | M | Unit |
| CR11 | QueryEventsAsync yields ordered | positions ascending | Ordering | M | Unit |
| CR12 | Batch retry transient | first 429 in ExecuteBatchWithRetryAsync -> success | Retry | M | Unit |

---

### File: Brooks/BrookRecoveryService.cs

#### Type: BrookRecoveryService

Test Cases:

| ID | Scenario | Description | Edge | Pri | Type |
|----|----------|-------------|------|-----|------|
| RCV1 | Head exists returns directly | head present -> no recovery | Fast path | H | Unit |
| RCV2 | Pending head commit path | head null + pending + all events exist -> commit -> new head position | Commit | H | Unit |
| RCV3 | Pending head rollback path | missing events -> rollback deletes and clears pending | Rollback | H | Unit |
| RCV4 | Recovery lock contention | first Acquire throws -> second read obtains head else exception | Concurrency | M | Unit |
| RCV5 | Large gap optimization | >10 events -> uses bulk check path | Branch | L | Unit |
| RCV6 | Exception acquiring recovery lock then still null | lock fail & head still null -> InvalidOperationException | Failure | M | Unit |

---

### File: Brooks/EventBrookAppender.cs

#### Type: EventBrookAppender

Test Cases:

| ID | Scenario | Description | Edge | Pri | Type |
|----|----------|-------------|------|-----|------|
| EBA1 | AppendEventsAsync empty list | throws ArgException | Validation | H | Unit |
| EBA2 | Too many events overflow guard | Count > int.MaxValue/2 -> ArgException | Overflow | H | Unit |
| EBA3 | Expected version mismatch | currentHead != expected -> OptimisticConcurrencyException | Concurrency | H | Unit |
| EBA4 | Position overflow guard | currentHead near long.MaxValue -> InvalidOperationException | Overflow | H | Unit |
| EBA5 | Single batch path | count within limits -> ExecuteTransactionalBatchAsync called | Branch | H | Unit |
| EBA6 | Large batch path | size/count exceed limit -> CreatePendingHead & multi-batch append | Branch | H | Unit |
| EBA7 | Large batch rollback on failure | forced failure mid-batches -> rollback deletes appended events | Rollback | H | Unit |
| EBA8 | Renew lock periodically | long batch triggers RenewAsync iterations | Lease | M | Unit |
| EBA9 | Logs information (optional) | verify informative log emitted | Logging | L | Unit |

---

### File: Brooks/EventBrookReader.cs

#### Type: EventBrookReader

Test Cases:

| ID | Scenario | Description | Edge | Pri | Type |
|----|----------|-------------|------|-----|------|
| EBR1 | Enumerates events | repository returns two storage models -> mapped events sequence | Mapping | H | Unit |
| EBR2 | Cancellation honored | cancel during enumeration -> TaskCanceled | Cancellation | M | Unit |

---

### File: Mapping/* Mappers

Test Cases (apply to each mapper):

| ID | Scenario | Description | Pri | Type |
|----|----------|-------------|-----|------|
| MAP1 | Map copies expected fields | sample input -> output matches | H | Unit |
| MAP2 | Nullables default fallback | null optional fields -> expected defaults (empty, etc.) | M | Unit |
| MAP3 | EventDocument -> Storage -> Domain round-trip | document -> storage -> domain (compare key fields) | M | Unit |
| MAP4 | HeadDocument -> Storage -> round-trip | doc -> storage -> doc (equivalent) | M | Unit |

---

### File: BrookStorageProviderRegistrations.cs

#### Type: BrookStorageProviderRegistrations

Test Cases:

| ID | Scenario | Description | Pri | Type |
|----|----------|-------------|-----|------|
| REGC1 | Registers required singletons | call AddCosmosBrookStorageProvider -> resolve writer/reader/recovery/appender/repos | H | Unit |
| REGC2 | Overload with options delegate | use Action\<BrookStorageOptions\> -> value applied | M | Unit |
| REGC3 | Overload with connection strings | supply strings -> CosmosClient & BlobServiceClient registered | M | Unit |
| REGC4 | Overload with IConfiguration | in-memory config -> options bound | M | Unit |

---

### File: BrookStorageOptions.cs

#### Type: BrookStorageOptions

Test Cases:

| ID | Scenario | Description | Pri | Type |
|----|----------|-------------|-----|------|
| OPTC1 | Defaults populated | new options -> defaults match documented constants | M | Unit |
| OPTC2 | MaxEventsPerBatch < 100 safeguard | ensure default 90 < 100 (mutation guard) | H | Unit |
| OPTC3 | LeaseRenewalThresholdSeconds < LeaseDurationSeconds | default 20 < 60 (guard) | H | Unit |

---

### File: OptimisticConcurrencyException.cs

#### Type: OptimisticConcurrencyException

Test Cases:

| ID | Scenario | Description | Pri | Type |
|----|----------|-------------|-----|------|
| OCE1 | Ctor message preserved | new(msg).Message == msg | M | Unit |
| OCE2 | Ctor inner preserved | new(msg, inner).InnerException==inner | M | Unit |
| OCE3 | Default ctor has generic message | new().Message not empty | L | Unit |

---

### File: Brooks/EventBrookAppender.cs (Rollback helper specifics)

Additional edge checks covered in EBA7.

## Deferred / Open Questions

- Full integration test against Azurite/Cosmos emulator.
- Performance tests for large batch append (stress / soak).
- Potential property-based tests for batch size estimator accuracy.
