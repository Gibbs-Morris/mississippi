# QA Perspective — Three Amigos

## First Principles: Failure Analysis

- The feature only has value if a snapshot written through the Blob provider can be read back with the same contract-level meaning as the existing provider. The primary failure mode is not "Blob write failed". It is "the system accepted persisted state that it cannot later find, decode, or trust."
- The most likely failures are deterministic ones introduced by naming, listing, metadata, and configuration boundaries rather than raw Azure availability. Prefix-based lookup, version ordering, serializer selection, and compression selection are the most failure-prone seams because they directly affect correctness on every read and write.
- The most costly failures are silent correctness failures: reading the wrong latest snapshot, pruning the wrong blob, accepting corrupt envelope metadata, or failing after restart because the configured serializer or compression mode no longer matches stored records.
- Large payload handling is a second-order but still high-risk area. The product reason for this provider is larger-than-Cosmos payloads, so any failure mode that turns large snapshots into excessive memory pressure, timeout sensitivity, or non-deterministic test behavior directly undermines the feature's purpose.
- The main quality bar is therefore:
  - Contract parity with the existing provider at the observable behavior level.
  - Durable compatibility of the stored envelope across reload and restart.
  - Safe behavior for large payloads, not only small happy-path examples.
  - Deterministic delete, delete-all, latest-read, and prune behavior under prefix listing.

### Most Likely Failures

1. Latest snapshot selection is wrong because blob naming or listing order does not match numeric version ordering.
2. Compression or serializer metadata is missing, stale, or inconsistent with the payload bytes.
3. Delete-all or prune uses an over-broad prefix and removes blobs from another stream.
4. Restart/reload fails because stored records depend on current in-memory configuration rather than self-describing metadata.
5. Large payload reads or writes cause excessive buffering, timeout sensitivity, or out-of-memory conditions.

### Most Costly Failures

1. Silent read of the wrong snapshot version.
2. Irrecoverable inability to deserialize previously stored data after configuration change or app restart.
3. Data loss from incorrect prune or delete-all enumeration.
4. Production-only failures on large payload sizes that were not represented in tests.

## Test Strategy

Repository standards point to L0-first, deterministic tests, then a focused real-infrastructure L2. L1 should exist only where light infrastructure adds signal without paying the full Azurite or app-host cost.

| Level | Scope | Count Estimate | Key Scenarios |
|-------|-------|----------------|---------------|
| L0 (Unit) | Pure provider logic, envelope mapping, naming, ordering, config validation, failure translation, prune decisions, serializer/compression selection | 30-40 | Default JSON path, gzip on/off, non-default serializer selection, envelope round-trip, blob-name ordering, empty and large payload boundaries, corrupt metadata handling, delete/prune correctness, retryable vs non-retryable failure mapping |
| L1 (Light infra) | Repository or operations tests with controlled doubles/fakes and real streams but no external Azurite dependency | 8-12 | Stream-based large-payload round trip, injected partial-read or partial-write failures, deterministic listing pages, restart-like reload using stored bytes, envelope inspection without app host |
| L2 (Integration) | Crescent plus Azurite-backed realistic slice using actual Mississippi configuration | 5-8 | App-level provider registration, default JSON path, gzip path, one non-default serializer/configuration path, persisted metadata inspection, restart/reload compatibility, delete/prune behavior against real Blob APIs |
| L3 (E2E) | Optional release-confidence scenario, not default PR gate | 1-2 | Full app lifecycle across process restart with realistic large payload and operational diagnostics review |

### Test Prioritization

1. L0 correctness around naming, latest-read, envelope metadata, and delete/prune behavior.
2. L0 and L1 failure-path tests for corrupt blobs, missing blobs, and transient Blob failures.
3. L2 restart/reload plus metadata assertions in Crescent.
4. Large-payload tests that prove the feature's motivating scenario instead of only tiny records.
5. Optional L3 only if there is uncertainty left after L2.

## Edge Case Catalogue

| # | Scenario | Input/State | Expected Behavior |
|---|----------|-------------|-------------------|
| 1 | Empty payload body | Valid envelope with zero-length payload | Write and read succeed if zero-length payload is valid for the contract; no spurious compression or deserialization failure |
| 2 | Payload just below Cosmos pain point | Payload near the motivating threshold but still moderate | Provider behaves normally and proves the new provider is not limited to only tiny records |
| 3 | Payload materially larger than Cosmos-friendly size | Multi-megabyte payload | Round trip succeeds within deterministic test bounds without truncation or corruption |
| 4 | Version ordering boundary | Snapshots for versions 9, 10, 11, 100 | Latest-read returns the numerically latest snapshot, not the lexically last blob name |
| 5 | Stream key with delimiter-heavy or unusual characters | SnapshotStreamKey segments include pipes, dashes, long values, or shared prefixes | Blob naming and prefix listing isolate the correct logical stream |
| 6 | Prefix collision across streams | Two stream keys share a prefix | Delete-all, latest-read, and prune only affect the target stream |
| 7 | Compression off | Provider option set to off | Metadata says no compression and payload bytes remain readable via the configured serializer |
| 8 | Compression gzip | Provider option set to gzip | Metadata identifies gzip and round trip succeeds consistently |
| 9 | Default serializer | No custom serializer configured | Stored metadata identifies JSON and round trip succeeds |
| 10 | Non-default serializer | Alternate serializer configured | Payload uses the configured serializer while provider metadata remains fixed and readable |
| 11 | Unknown envelope version | Blob contains unsupported format version | Read fails predictably with a clear provider-level failure rather than silent misread |
| 12 | Unknown compression algorithm metadata | Blob metadata names unsupported compression | Read fails safely and does not attempt an invalid decode |
| 13 | Unknown serializer identity | Stored metadata references unavailable serializer | Read fails deterministically with actionable error behavior |
| 14 | Corrupt compressed payload | Gzip metadata present but payload bytes are truncated or invalid | Read fails safely without returning partial or incorrect data |
| 15 | Missing blob for requested version | Requested snapshot version is absent | Provider returns the contract-appropriate not-found outcome |
| 16 | No blobs for a stream | Empty stream prefix | Latest-read and prune behave as empty/no-op outcomes |
| 17 | Single snapshot prune | Stream has one retained snapshot | Prune does not remove the only valid snapshot when policy should retain it |
| 18 | Many snapshots under one stream | Large prefix listing across multiple pages | Latest-read and prune remain correct across paged enumeration |
| 19 | Restart with changed defaults | App restarts with different default serializer registration order | Existing blobs remain readable because stored metadata is self-describing |
| 20 | Delete after partial prior failure | Blob write succeeded but subsequent operation path failed | Later read, delete, and retry behavior is deterministic and does not hide orphaned state |

## Failure Scenarios

| # | Failure | Trigger | Expected System Response |
|---|---------|---------|-------------------------|
| 1 | Transient Blob service failure during write | Timeout, throttling, temporary unavailable | Provider surfaces a predictable failure path and does not report success unless durable write completed |
| 2 | Transient failure during read | Network interruption or service timeout | Provider fails deterministically; callers do not receive partially decoded state |
| 3 | Failure during prefix listing | Enumeration interrupted mid-page | Latest-read, prune, and delete-all fail safely rather than acting on incomplete listings |
| 4 | Corrupt blob body | Manual corruption or partial upload | Read fails safely with no silent fallback to incorrect data |
| 5 | Metadata/payload mismatch | Metadata says gzip or serializer X but bytes reflect something else | Provider rejects the blob as incompatible or corrupt |
| 6 | Serializer missing at runtime | Blob references serializer no longer registered | Read fails predictably and points to configuration incompatibility |
| 7 | Restart after config change | App restarts with different default compression or serializer choices | Existing blobs remain readable based on stored metadata, not current defaults alone |
| 8 | Wrong latest version chosen | Listing returns lexical rather than numeric order | Tests detect mismatch; provider must not return an older snapshot as latest |
| 9 | Over-broad prune/delete-all | Prefix generation matches another stream's blobs | Operation must be scoped to the intended stream only |
| 10 | Large-payload memory exhaustion | Write or read path buffers too aggressively | Operation should fail predictably under limits; tests should expose memory-sensitive paths before release |
| 11 | Concurrent writes to adjacent versions | Two writes happen close together | Observable contract behavior remains coherent; later reads do not return malformed or partially written data |
| 12 | Blob exists but envelope version is newer than the code understands | Forward-compatibility boundary | Read fails clearly instead of attempting unsafe interpretation |

## Testability Concerns

| Concern | Why It Is Hard to Test | Recommended Approach |
|---------|----------------------|----------------------|
| Large payload behavior | Easy to accidentally test only small payloads and miss allocation or timeout issues | Add representative multi-megabyte fixtures at L0/L1 with deterministic size ranges and explicit assertions for full round-trip correctness |
| Prefix-listing correctness | Real Blob listings can be paged and ordering-sensitive | Use L0/L1 fake listings that simulate out-of-order and paged results, then prove the same behavior in one real Azurite L2 path |
| Stored-envelope compatibility | Failures only appear after restart or when defaults change | Persist bytes, rebuild configuration, then reread in separate test phases or fresh fixture instances |
| Corruption handling | Production corruption is hard to reproduce organically | Inject malformed envelope bytes, mismatched metadata, truncated gzip payloads, and unsupported versions directly in L0/L1 tests |
| Failure translation | Azure exceptions are numerous and transport-specific | Centralize failure injection through operation doubles so tests assert provider-level outcomes rather than SDK-specific wording |
| Delete/prune safety | Data-loss bugs can hide behind simple happy paths | Use multi-stream datasets with overlapping prefixes and verify exact survivors after each operation |
| Alternate serializer coverage | Non-default path can be forgotten if JSON dominates all tests | Make one full acceptance scenario require a non-default serializer in both L0 and L2 |
| Deterministic restart testing | Cross-process behavior is more expensive than unit tests | Use fixture recreation or fresh service-provider creation rather than true process orchestration for most tests |

## Acceptance Test Scenarios

### ATS-1: Familiar app-level registration works
Given an application configured to use the Blob provider as its snapshot storage provider, when the app starts with standard Mississippi registration patterns, then the provider resolves successfully and is usable without requiring a different operational model than the existing provider family.

### ATS-2: Default JSON round trip succeeds
Given default provider configuration with compression off and no custom serializer, when a snapshot is written and later read, then the read result matches the written snapshot and the stored metadata identifies JSON and no compression.

### ATS-3: Gzip round trip succeeds
Given provider-wide gzip compression is enabled, when a snapshot is written and later read, then the payload round trips correctly and the stored metadata identifies gzip.

### ATS-4: Non-default serializer survives restart
Given a non-default serializer is configured and a snapshot is written, when the application is restarted and the snapshot is read back, then the provider loads the snapshot successfully using the stored serializer identity rather than relying on ambient defaults.

### ATS-5: Latest snapshot selection is correct across ordering boundaries
Given snapshots exist for versions that challenge lexical ordering, when the latest snapshot is requested, then the numerically newest snapshot is returned.

### ATS-6: Delete-all is stream-scoped
Given multiple streams with overlapping key prefixes exist in the same container, when delete-all is executed for one stream, then only that stream's blobs are removed.

### ATS-7: Prune retains the correct snapshots
Given a stream with multiple versions that require pruning, when prune is executed, then only the intended retained snapshots remain and the latest valid snapshot is still readable.

### ATS-8: Corrupt blob fails safely
Given a blob exists with invalid envelope metadata or invalid compressed payload bytes, when it is read, then the provider fails predictably without returning incorrect snapshot data.

### ATS-9: Large payload motivating scenario works
Given a snapshot materially larger than the practical Cosmos-friendly size range, when it is written and read through the Blob provider, then the round trip succeeds and proves the feature's motivating user outcome.

## Quality Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Wrong latest snapshot due to blob-name ordering or parsing bug | High | High | Exhaustive L0 ordering tests plus one L2 ordering boundary scenario |
| Data loss from incorrect prefix scoping in delete-all or prune | Medium | High | Multi-stream safety datasets in L0 and at least one real-container L2 validation |
| Stored data becomes unreadable after restart or configuration changes | Medium | High | Restart-style acceptance tests and direct stored-envelope assertions |
| Large payload scenario passes functionally but fails operationally under memory pressure | High | High | Representative payload-size tests early in L0/L1 and explicit release sign-off on large-payload evidence |
| Alternate serializer path receives only shallow coverage | Medium | Medium | Make a non-default serializer path a required acceptance scenario, not optional stretch coverage |
| Corruption paths are undefined and surface as opaque runtime failures | Medium | Medium | Add direct malformed-envelope and malformed-compression tests at L0 |
| Real Azurite L2 covers only connectivity, not provider semantics | Medium | Medium | Require L2 assertions on metadata, restart compatibility, and stream-scoped operations |
| Transient Blob failures produce inconsistent observable behavior | Medium | Medium | Failure-injection tests that assert stable provider-level outcomes |

## Shift-Left Opportunities

- Define provider invariants before implementation starts: latest-version selection rules, stream-scoping rules for listing and deletion, and stored-envelope minimum metadata.
- Write L0 test cases for ordering, corruption, and prefix collisions before any Blob SDK wiring. Those are logic risks, not infrastructure risks.
- Add deterministic test fixtures for multi-megabyte payload sizes early so the team does not accidentally optimize around tiny payloads only.
- Make a review checklist item for every acceptance criterion in the business perspective so no criterion is left as an implicit assumption.
- Add mutation-focused assertions to L0 scenarios that differentiate "latest" from "not latest", "target stream" from "shared prefix", and "supported" from "unsupported" metadata.
- Treat L2 as behavior proof, not smoke test proof. The Crescent scenario should be specified up front with exact assertions on metadata, restart compatibility, and non-default configuration.

## CoV: Test Coverage Verification

1. Draft coverage assessment for stated requirements
   - US-1 is covered by registration and app-level provider-selection acceptance tests.
   - US-2 is covered by L0/L1 serializer and compression tests, large-payload tests, and metadata validation tests.
   - US-3 is covered by the overall test layering, especially the Crescent restart/reload scenario and corruption/failure-path coverage.

2. Verification: is every acceptance criterion covered by at least one test?
   - Familiar registration and app-level alternative behavior: covered by ATS-1 and L2 startup validation.
   - Contract-level persistence behavior for larger payloads: covered by ATS-9 plus L0/L1 large-payload round trips.
   - Compression off and gzip: covered by ATS-2 and ATS-3 plus L0 metadata assertions.
   - JSON default and non-default serializer: covered by ATS-2 and ATS-4 plus direct L0 serializer-selection tests.
   - Fixed provider metadata with version, serializer identity, and compression: covered by ATS-2, ATS-3, ATS-4, and ATS-8.
   - Comprehensive unit coverage and meaningful Crescent L2: covered by the proposed layered strategy and ATS-1 through ATS-9.

3. Evidence cross-reference with business acceptance criteria
   - Business ACs about larger payload support map to ATS-9 and large-payload L0/L1 scenarios.
   - Business ACs about compression and serializer pluggability map to ATS-2 through ATS-4.
   - Business ACs about metadata visibility and restart compatibility map to ATS-4 and ATS-8.
   - Business ACs about trustworthy automated validation map to the L0-first strategy plus focused Crescent L2.

4. Revised assessment with gaps identified
   - The highest residual gap is operational confidence for very large payload sizes under realistic memory constraints. A purely functional L2 is not enough if it uses only modest payload sizes.
   - The second residual gap is ambiguity around concurrent write expectations. If the contract has specific concurrency semantics, they should be explicitly turned into tests before implementation starts.
   - The third residual gap is failure observability. If the provider is expected to emit structured diagnostics for corrupt or incompatible blobs, those expectations should be stated now so tests can assert them.