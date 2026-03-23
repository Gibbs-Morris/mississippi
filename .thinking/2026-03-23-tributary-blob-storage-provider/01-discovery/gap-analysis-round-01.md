# Requirements Gap Analysis - Round 1

## Prior Context Summary

The requested outcome is a new Tributary Azure Blob Storage provider that follows the same external contracts and configuration style as the existing Cosmos provider, but is intended to support larger persisted payloads. The first release is expected to preserve Cosmos-provider parity as closely as possible, target framework adopters who configure Mississippi directly, and meet a quality bar of comprehensive unit tests plus a Crescent L2 happy path if feasible. Compression is explicitly in scope, with gzip mentioned as the starting point, and serialization must default to JSON while remaining pluggable through options-based configuration.

## CoV: Requirements Completeness

1. Draft: the current requirements are directionally clear on business value, audience, and parity goals, but they are still incomplete for implementation because the most important storage-behavior decisions are unspecified.
2. Verification questions:
   - Has the storage layout in Blob Storage been defined strongly enough to preserve provider parity?
   - Are write correctness expectations clear enough to choose a Blob write strategy without inventing behavior?
   - Are compression and serialization boundaries defined well enough to avoid accidental lock-in?
   - Is the L2 scope specific enough to know what "happy path" means for first release?
3. Independent answers:
   - Intake confirms Blob Storage is requested because Cosmos has a practical size ceiling and that the new provider should follow the same contracts as the Cosmos provider.
   - Round 1 confirms strict Cosmos parity, direct framework consumers, and matching contracts/options patterns as the dominant constraints.
   - Neither file specifies Blob object layout, concurrency/error semantics, compression rules, serializer plug-in boundaries, or the exact Crescent L2 acceptance path.
4. Revised assessment: requirements completeness is partial. The product intent is clear, but implementation-ready functional requirements are still missing in five areas that would otherwise force architectural guesses.

## Critical Gaps (Must Resolve)

### Gap 1: Blob Storage Mapping Model

- **What is missing**: there is no defined mapping from Tributary persistence concepts to Blob Storage concepts, such as whether snapshots and event documents live in separate containers, whether each persisted record is one blob, and how blob names and metadata should be organized.
- **Why it matters**: this determines retrieval patterns, listing behavior, operational cost, and whether the Blob provider can actually preserve Cosmos-provider behavior without hidden incompatibilities.
- **Suggested question**: How should the Blob provider map Tributary persisted data into Azure Blob Storage for the first release?
  - A. Mirror Cosmos behavior at the record level: one logical persisted record per blob, with separate logical areas for snapshots and other persisted artifacts.
  - B. Use one blob per aggregate stream or snapshot family, appending or replacing content within that blob over time.
  - C. Store the main payload in blobs but keep lookup-oriented metadata in a separate index structure if needed.
  - X. I don't care - pick the best default.

### Gap 2: Write Correctness and Concurrency Semantics

- **What is missing**: the requirements do not state how strictly the Blob provider must match Cosmos-provider behavior for optimistic concurrency, overwrite protection, retries, and partial-failure handling.
- **Why it matters**: Blob Storage has different write primitives than Cosmos. Without a requirement here, the implementation would have to guess whether exact outward behavior is mandatory or whether Blob-native tradeoffs are acceptable.
- **Suggested question**: For first release, how strict must Blob-provider write behavior be relative to the Cosmos provider?
  - A. Match Cosmos-provider outward behavior exactly, including conflict/error semantics wherever the contracts expose them.
  - B. Match the public contract results, but Blob-specific internal behavior is acceptable if callers still see equivalent success and failure outcomes.
  - C. Optimize for Blob-native correctness and document any behavior differences that cannot be hidden cleanly.
  - X. I don't care - pick the best default.

### Gap 3: Compression Semantics

- **What is missing**: gzip is named as the starting point, but the requirements do not define whether compression is always on, optional per provider instance, conditional by payload size, or configurable per artifact type.
- **Why it matters**: compression policy affects read/write compatibility, performance, storage cost, metadata design, and future extensibility for other algorithms.
- **Suggested question**: What should compression behavior be in the first Blob provider release?
  - A. Provider-wide option: off or gzip, applied consistently to every persisted payload.
  - B. Automatic policy: gzip only when payload size crosses a configurable threshold.
  - C. Per-artifact policy: allow separate compression settings for different persisted artifact categories if the contracts support them.
  - X. I don't care - pick the best default.

### Gap 4: Serializer Plug-in Boundary

- **What is missing**: the request says serialization should default to JSON and stay pluggable, but it does not define whether pluggability should be limited to payload serialization, whether metadata/envelopes are also serializer-driven, or whether first release only needs one extension point compatible with the Cosmos provider pattern.
- **Why it matters**: this controls how much of the persistence format becomes a public contract and whether future serializers can be introduced without breaking stored-data expectations.
- **Suggested question**: For first release, what must be pluggable about serialization?
  - A. Only the persisted payload/body format; provider-owned metadata stays fixed and JSON remains the default serializer.
  - B. The full stored document shape, including payload and provider metadata, through a serializer abstraction.
  - C. JSON only for v1, but design the options model so a later serializer plug-in can be added without breaking the public configuration pattern.
  - X. I don't care - pick the best default.

### Gap 5: Crescent L2 Acceptance Scope

- **What is missing**: the L2 requirement is still ambiguous about environment fidelity and scenario breadth, such as whether the happy path should use Azurite or another emulator, and whether it must prove only round-trip persistence or also restart/reload behavior.
- **Why it matters**: L2 scope drives engineering effort, CI shape, and what "done" means for the provider slice. It is the main remaining ambiguity in the quality bar.
- **Suggested question**: What should the Crescent L2 happy path prove for the first Blob-provider release?
  - A. Minimal end-to-end round trip against a local Blob-compatible emulator: configure provider, write data, read data back successfully.
  - B. Round trip plus restart/reload validation to prove persisted data survives process restarts in the test environment.
  - C. Full realistic slice, including emulator-backed storage, compression enabled, and non-default serializer or configuration exercised once.
  - X. I don't care - pick the best default.

## Important Gaps (Should Resolve)

- Authentication and configuration surface for framework adopters is still unspecified: connection string only, Azure client injection, or both.
- Expected payload size range and performance targets are not defined, so there is no clear non-functional bar beyond "larger than Cosmos allows."
- Operational requirements are missing for container naming, lifecycle management, and cleanup behavior.
- Observability expectations are not stated for logging, metrics, or diagnostic metadata around compression and serialization choices.
- Cross-provider coexistence and migration expectations are unspecified, including whether users may run Cosmos and Blob providers side by side for different workloads.

## Nice-to-Know Gaps

- Whether the first release should expose Blob-specific advanced options immediately or keep them hidden behind conservative defaults.
- Whether future non-gzip compression algorithms should appear in the initial options contract or be deferred entirely.
- Whether documentation should include explicit guidance for when to choose Blob over Cosmos.

## Requirements Maturity Assessment

- Functional requirements: partial
- Non-functional requirements: insufficient
- Constraints: partial
- Edge cases: partially identified
- Overall readiness for Three Amigos: needs 1 more round