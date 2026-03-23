# Plan Review Synthesis - Cycle 1

## Summary

- Total unique concerns: 14
- Must: 5 | Should: 4 | Could: 1 | Won't: 4
- Genuine conflicts requiring resolution: 2
- Positive observations: 5

## Action Items

### Must (blocking - plan cannot proceed without these)

| # | Concern | Raised By | Action Required |
|---|---------|-----------|-----------------|
| 1 | The plan is sequenced around coarse implementation increments instead of the dependency-ordered architecture decisions and their executable checks. | Technical Lead, Solution Architect, QA Lead, Performance | Re-sequence the plan around the architecture dependency chain: hashed naming, blob frame, serializer identity, payload-only compression plus checksum, conditional write plus maintenance semantics, then container initialization. Land the corresponding L0 tests in the same increment as each decision instead of deferring them to a later test-completion phase. |
| 2 | Contract parity, risk coverage, and merge evidence are still implied instead of defined. | Technical Lead, Solution Architect, QA Lead, Developer Experience, Performance | Add a single parity and evidence matrix before implementation starts. It should lock expected behavior for read-missing, delete-missing, duplicate-version writes, latest selection, prune scope, delete-all scope, restart safety, corrupt-data rejection, non-default serializer survival, and large-payload viability. For each item, name the proving test layer and the required merge artifact. |
| 3 | Serializer resolution, persisted serializer identity, startup validation, and failure semantics are still underspecified even though they are part of the stored contract. | Technical Lead, Solution Architect, QA Lead, Developer Experience, Performance | Pull serializer rules into the earliest persisted-format checkpoint. State the exact resolution rule for PayloadSerializerFormat, require fail-fast startup validation for zero or multiple matches, and define the expected failure semantics for unknown serializer identity, unsupported compression, unknown frame version, duplicate-version conflicts, and corrupt or unreadable blobs. |
| 4 | The plan does not yet prove that the Blob path is viable for materially larger payloads without obvious memory amplification or body-heavy maintenance operations. | Technical Lead, QA Lead, Performance | Add an explicit large-payload performance gate. Define a copy-budget or allocation strategy for encode, compress, upload, download, decompress, and decode. Require representative deterministic payload sizes, prove that latest-read, prune, and delete-all stay name-driven or header-light, and forbid maintenance logic from downloading or decompressing candidate payloads during enumeration. |
| 5 | Scope and layering boundaries are not yet explicit enough to prevent the implementation from drifting into shared-abstraction work or Cosmos-internal mirroring. | Technical Lead, Solution Architect | Add a plan-level layering gate that says Cosmos parity applies to public adoption shape only. Blob naming, framing, compression, checksum handling, and Azure SDK operations stay internal to Tributary.Runtime.Storage.Blob. Do not introduce a new abstractions package, shared helper package, or cross-provider redesign in v1. |

### Should (important - significant improvement)

| # | Concern | Raised By | Action Required |
|---|---------|-----------|-----------------|
| 1 | The Crescent L2 slice is too broad to act as a crisp trust-building proof. | Technical Lead, QA Lead, Developer Experience, Developer Evangelist | Recast the L2 increment as one focused vertical slice with explicit exit criteria: provider registration, write and read through the existing contract, restart-safe reload, and one non-default configuration path. Keep extra maintenance checks secondary unless they are cheap and do not delay the core proof. |
| 2 | Diagnostics and supportability are mentioned, but not yet treated as required planned outcomes. | Technical Lead, Solution Architect, QA Lead, Developer Experience, Performance | Promote observability into the plan before the final quality pass. Require actionable startup errors, duplicate-version conflict diagnostics, decode and checksum failure diagnostics, and enough telemetry or assertions to make bytes moved, list cost, and failure causes reviewable. |
| 3 | The plan still reads as implementation-first instead of adoption-first for existing Mississippi users. | Developer Experience, Developer Evangelist | Add explicit adoption assets to the plan scope: a problem-first opening, one canonical registration path, one minimal Blob setup example, one Cosmos-to-Blob translation snippet, and one decision guide that explains when to choose Blob instead of Cosmos. Protect the no-domain-changes message in those assets. |
| 4 | Important operational boundaries are present in the reviews and architecture, but not yet carried into acceptance or documentation scope. | Technical Lead, Solution Architect, QA Lead, Performance | Add explicit acceptance and messaging for these boundaries: maintenance remains a stream-local linear scan, Azurite is functional confidence only, storage-account features such as soft delete or blob versioning can change physical purge behavior, and ListPageSizeHint is a tuning bound rather than a correctness assumption. |

### Could (nice to have - consider if time permits)

| # | Concern | Raised By | Action Required |
|---|---------|-----------|-----------------|
| 1 | Concurrency expectations for adjacent or near-simultaneous writes remain ambiguous in the current draft. | QA Lead | Decide whether concurrent write behavior is part of the v1 contract or explicitly out of scope. Record that decision in the parity matrix so reviewers do not infer guarantees that the implementation does not intend to make. |

### Won't (acknowledged but out of scope)

| # | Concern | Raised By | Rationale for Deferral |
|---|---------|-----------|----------------------|
| 1 | Generic multi-provider storage abstraction redesign or early extraction into shared abstractions. | Technical Lead, Solution Architect, QA Lead | Multiple reviewers explicitly rejected widening this feature into a broader storage-abstraction program. The v1 goal is one Blob-backed provider that preserves the existing Tributary contract. |
| 2 | Indexing or indirection features to avoid stream-local scans, including manifest blobs, pointer blobs, blob tags, latest markers, or human-readable path redesign. | Technical Lead, Solution Architect, Performance, Developer Experience | Reviewers consistently accepted stream-local linear scans for v1 as long as the cost shape is proven and payload bodies are not read during maintenance. Reopening naming and indexing now would expand scope instead of de-risking delivery. |
| 3 | Compression or serializer feature expansion beyond the narrow v1 boundary, including adaptive thresholds, per-snapshot codec choice, extra codecs, or automatic serializer fallback. | Technical Lead, Solution Architect, Performance, Developer Experience | The repeated guidance was to keep v1 explicit and predictable: provider-wide Off or Gzip and one configured serializer-selection rule with persisted serializer identity. |
| 4 | Claims or validation beyond the agreed v1 confidence bar, including real-Azure scale certification, throughput benchmarking as a release gate, broad migration tooling, or positioning Blob as generally superior to Cosmos. | Technical Lead, Solution Architect, QA Lead, Performance, Developer Evangelist | Reviewers agreed that v1 should prove functional correctness, restart safety, and bounded cost shape. It should not broaden into cloud-scale certification, data-migration tooling, or overstated product positioning. |

## Conflicts Requiring Resolution

### Conflict 1: Metadata visibility versus keeping Azure metadata non-authoritative

- Position A (Developer Experience, Developer Evangelist): the rollout needs an inspectability proof so adopters can quickly confirm serializer and compression choices and trust the stored artifact.
- Position B (Solution Architect, QA Lead): Azure metadata duplication should not quietly become a v1 correctness requirement because the authoritative design keeps the blob frame as the source of truth.
- Recommendation: require inspectability of the stored blob frame and a documented inspection path, but do not make Azure blob metadata duplication a core v1 exit criterion unless the team deliberately promotes it to optional supportability scope.

### Conflict 2: Whether maintenance behavior belongs in the first Crescent proof slice

- Position A (QA Lead): include one stream-safety maintenance scenario in L2 so the trust story covers more than happy-path write and read.
- Position B (Technical Lead): keep the first L2 bar narrowly focused on core proof and only include maintenance if it does not delay landing the feature.
- Recommendation: make cross-stream safety and maintenance correctness mandatory at lower layers, then include one L2 maintenance proof only if it can be added without expanding the core L2 slice or delaying the main restart-safe story.

## Positive Observations

- Keeping the public Tributary contract unchanged while reusing the familiar Cosmos-style registration shape is the right adoption baseline.
- Persisting the concrete serializer identity was repeatedly seen as a strong restart-safety and trust decision.
- Keeping the blob frame self-describing and leaving the header uncompressed was consistently viewed as good for inspectability and recovery.
- The CreateIfMissing versus ValidateExists split was recognized as a useful fit for both local development and least-privilege production environments.
- Reviewers broadly agreed with the narrow v1 boundary: one new provider, provider-wide Off or Gzip compression, and stream-local linear scans accepted if the cost shape is proven.

## CoV: Synthesis Verification

1. Every action item traces to reviewer feedback: verified.
2. No reviewer concern was silently dropped: verified. Repeated themes were deduplicated into single action items, and narrower concerns were either preserved in Could or captured in the conflict section.
3. MoSCoW categorization is justified: verified. Must items block architectural correctness, contract definition, or feature viability; Should items materially improve confidence, supportability, and adoption after blockers are resolved; Could items close remaining ambiguity without blocking the next review cycle; Won't items were explicitly and repeatedly deferred by reviewers.
4. Conflicts are genuine, not wording differences: verified. The metadata dispute is about whether Azure metadata duplication is required, and the L2 dispute is about whether maintenance behavior belongs in the first public proof slice.
