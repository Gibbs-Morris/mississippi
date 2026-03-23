# Requirements Gap Analysis - Round 2

## Prior Context Summary

The requested outcome remains a new Azure Blob Storage-backed Tributary snapshot provider that behaves like the existing Cosmos provider at the public contract level while supporting larger persisted payloads. Discovery rounds 1 and 2 resolved the biggest first-order questions: v1 should keep close parity with the Cosmos provider, use one logical persisted record per blob, preserve public contract outcomes while allowing Blob-specific internals, support provider-wide `off` or `gzip` compression, keep serializer pluggability limited to the payload/body, and add a Crescent L2 slice that exercises compression plus one non-default configuration.

Independent code review narrows the remaining uncertainty further. The existing Cosmos provider already exposes a public options-and-registration shape with connection-string, `IConfiguration`, and externally supplied keyed-client overloads; it auto-initializes storage at startup; and it emits snapshot metrics. The current snapshot abstraction also registers a single `ISnapshotStorageProvider` per application by default. That means the remaining gaps are no longer about whether Blob storage is needed, but about the exact public configuration shape, the internal lookup model required by one-blob-per-record storage, and the concrete proof points for test coverage.

## CoV: Requirements Completeness

1. Draft: the requirements are now strong enough to avoid major rework on compression and serializer scope, but they are still not implementation-ready because the remaining ambiguities sit directly on the Blob provider's architecture and public registration surface.
2. Verification questions:
   - Does the current abstraction assume a single snapshot provider, and if so, is Blob meant to be an alternative or a coexistence scenario?
   - Does one-record-per-blob storage implicitly leave latest-version lookup and pruning strategy unresolved?
   - Is the public registration shape for Blob already implied strongly enough by Cosmos parity, or does it still require an explicit product choice?
   - Is the stored blob metadata contract defined strongly enough to keep compression and serializer pluggability future-safe?
   - Is the Crescent L2 acceptance scope concrete enough to know what must be proven versus what is merely helpful?
3. Independent answers:
   - Discovery round 2 settles mapping granularity, public-outcome parity, compression mode, serializer boundary, and the desire for a realistic L2 slice.
   - The current snapshot abstraction registers a single `ISnapshotStorageProvider` via `TryAddSingleton`, so side-by-side provider selection is not implied by today's design.
   - The Cosmos provider exposes registration overloads for connection string, `IConfiguration`, and externally provided keyed clients, which means Blob parity could reasonably mirror that shape, but it is not yet explicitly required.
   - Cosmos snapshot persistence today has query-centric options such as `QueryBatchSize`; Blob storage does not provide the same lookup primitives, so latest/prune behavior still requires a deliberate strategy.
   - Crescent already has Azurite and `BlobServiceClient` support, so the missing test requirement is scenario depth, not basic emulator availability.
4. Revised assessment: requirements completeness is still partial. The business intent and several low-risk defaults are now settled, but five material gaps remain that will otherwise force architectural invention in the implementation.

## Critical Gaps (Must Resolve)

### Gap 1: Provider Selection Granularity

- **What is missing**: the phrase "for some cases" still leaves it unclear whether Blob is only a drop-in alternative to Cosmos at the application level, or whether the product owner expects the same application to support Blob and Cosmos side by side for different snapshot workloads.
- **Why it matters**: the current abstraction registers one `ISnapshotStorageProvider` by default. If side-by-side selection is required, this is not just a provider implementation detail; it changes the registration model, likely expands public APIs/options, and broadens both test and documentation scope.
- **Suggested question**: For v1, how should apps choose between Cosmos and Blob snapshot storage?
  - A. Blob is an alternative provider; each app chooses either Cosmos or Blob for snapshots, but not both at once.
  - B. The same app must be able to register both providers and choose between them per snapshot use case or type.
  - C. Blob should become the preferred default for new apps, with Cosmos remaining available separately.
  - X. I don't care - pick the best default.

### Gap 2: Latest/Prune Lookup Strategy For One-Blob-Per-Record Storage

- **What is missing**: discovery settled that each logical persisted record should map to one blob, but it did not define how the provider finds the latest snapshot for a stream or efficiently enumerates candidates for pruning.
- **Why it matters**: Blob storage does not offer Cosmos-style document querying. This gap determines the internal architecture, complexity, cost profile, and whether the provider stays simple or grows an index/manifest subsystem.
- **Suggested question**: How should the Blob provider discover the latest snapshot version and the set of blobs eligible for pruning?
  - A. Use deterministic blob names plus prefix listing per stream and accept linear listing within that stream.
  - B. Maintain a provider-owned manifest or index blob per stream to track versions and the latest snapshot.
  - C. Use blob tags or metadata as the primary query surface instead of a separate index.
  - X. I don't care - pick the best default.

### Gap 3: Public Registration And Client Acquisition Shape

- **What is missing**: there is still no explicit decision on whether Blob registration should mirror Cosmos exactly with connection-string, configuration-binding, and externally supplied client overloads, or whether Blob should prefer a different DI shape.
- **Why it matters**: this is a public API decision. It affects adopter ergonomics, consistency with existing provider patterns, sample code shape, and the level of startup infrastructure the implementation is allowed to own.
- **Suggested question**: What public configuration/registration shape should the Blob snapshot provider expose in v1?
  - A. Mirror Cosmos registration closely: support connection-string, `IConfiguration`, and externally supplied keyed `BlobServiceClient` overloads plus Blob-specific options.
  - B. Prefer externally supplied keyed `BlobServiceClient` registrations and keep helper overloads minimal.
  - C. Let Blob options carry enough connection details that the provider constructs its own clients internally.
  - X. I don't care - pick the best default.

### Gap 4: Persisted Blob Metadata Contract

- **What is missing**: while payload serialization is settled as the pluggable boundary, the persisted metadata contract is still undefined. There is no decision on where the provider records serializer identity, compression algorithm, content type, size information, and storage-format version.
- **Why it matters**: this shapes forward compatibility, operability, debugging, and whether future serializers/compression algorithms can coexist safely without guessing how older blobs were written.
- **Suggested question**: How should the provider record compression and serialization metadata for each blob?
  - A. Use a fixed provider-owned metadata contract with explicit storage-format version, serializer identity/content type, compression algorithm, and size fields.
  - B. Put the provider envelope inside the blob body and keep blob metadata or headers minimal.
  - C. Rely mostly on blob HTTP headers and naming conventions, with only minimal custom metadata.
  - X. I don't care - pick the best default.

### Gap 5: Crescent L2 Acceptance Depth

- **What is missing**: round 2 established that L2 should be a realistic slice with compression and one non-default configuration, but it still does not define whether the test should validate only framework-level behavior or also inspect the underlying blobs and restart behavior.
- **Why it matters**: this gap sets the real testing scope, fixture complexity, and what "done" means. It also determines whether metadata and initialization choices must be directly asserted in integration coverage.
- **Suggested question**: What exactly should the Crescent L2 prove for the Blob provider's first release?
  - A. Through Tributary APIs against Azurite, prove write/read/prune behavior with gzip and one non-default serializer or configuration choice.
  - B. Do everything in A and also inspect the underlying blob/container state to verify naming and metadata are written as intended.
  - C. Do everything in B and also prove blobs remain readable after host restart or reinitialization.
  - X. I don't care - pick the best default.

## Important Gaps (Should Resolve)

- Container ownership and initialization behavior are still not explicit: should the provider auto-create containers at startup like Cosmos does, create lazily on first use, or require pre-provisioned containers.
- Expected payload envelope is still undefined beyond "larger than Cosmos allows": there is no target size range, timeout expectation, or explicit "not for huge multi-GB objects" boundary.
- Observability parity is unspecified: it is still unclear whether Blob v1 must emit the same class of metrics/logging as Cosmos and whether compression ratio should be observable.
- Migration and interoperability expectations are not stated: there is no answer on whether tooling or guidance is expected for moving snapshots between Cosmos and Blob.
- Failure-mode expectations remain thin for emulator versus real Azure behavior, especially around transient failures, retries, and authorization errors.

## Nice-to-Know Gaps

- Whether future compression algorithms beyond gzip should appear in the initial options enum or remain an internal extension later.
- Whether Blob-specific advanced settings such as access tier, parallel upload tuning, or checksum behavior should stay hidden in v1.
- Whether user-facing docs should explicitly recommend when to choose Blob over Cosmos versus treating the provider choice as purely infrastructural.

## Requirements Maturity Assessment

- Functional requirements: partial
- Non-functional requirements: partial
- Constraints: partial
- Edge cases: partially identified
- Overall readiness for Three Amigos: needs 1 more round