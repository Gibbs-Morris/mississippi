# Technical Perspective — Three Amigos

## First Principles: Technology Assessment

- The requested feature is feasible without changing Tributary's public storage contract. The existing contract is small and provider-oriented: read, write, delete, delete-all, prune, plus a format string. That is evidenced by src/Tributary.Runtime.Storage.Abstractions/ISnapshotStorageProvider.cs, src/Tributary.Runtime.Storage.Abstractions/ISnapshotStorageReader.cs, and src/Tributary.Runtime.Storage.Abstractions/ISnapshotStorageWriter.cs.
- The simplest approach is not a new generalized storage abstraction. It is a new provider project that mirrors the existing Cosmos provider structure closely, with Blob-specific internals hidden behind the same contract. That aligns with the confirmed requirement to preserve Cosmos-style registration and options patterns while allowing provider-specific internals.
- The serializer plug-in requirement should reuse the existing Brooks serialization abstraction rather than inventing a new payload codec surface. Tributary already depends on Mississippi.Brooks.Serialization.Abstractions.ISerializationProvider for snapshot state conversion in src/Tributary.Runtime/SnapshotStateConverter.cs, and JSON is already the repo default via src/Brooks.Serialization.Json/ServiceRegistration.cs and src/Brooks.Serialization.Json/JsonSerializationProvider.cs.
- Compression should remain a provider-local concern in v1. The current contract stores raw snapshot envelopes, and the confirmed requirement says serializer pluggability applies only to payload format while provider metadata stays in a fixed provider-owned envelope. That means compression belongs below the storage provider boundary, not in Tributary.Abstractions.
- Azure Blob Storage is the right backing technology for the stated problem because the problem is large payload persistence rather than queryability. Blob storage naturally fits one-logical-record-per-blob, and the confirmed lookup model is deliberately simple: prefix listing plus naming conventions.

## Feasibility Assessment

This can be built as described.

The repo already contains the main ingredients needed for a low-risk implementation:

- A clean storage-provider contract in Tributary abstractions.
- A proven provider shape in src/Tributary.Runtime.Storage.Cosmos built around options, DI registrations, a thin provider, a repository, mappers, diagnostics, and SDK-isolation through a single operations component.
- Existing Azure Blob client registration and usage patterns in src/Brooks.Runtime.Storage.Cosmos/BrookStorageProviderRegistrations.cs and src/Brooks.Runtime.Storage.Cosmos/Locking/BlobDistributedLockManager.cs.
- Existing Azurite-backed Crescent infrastructure in samples/Crescent/Crescent.AppHost/Program.cs and samples/Crescent/Crescent.L2Tests/CrescentFixture.cs.

The main technical refinement needed is that Blob storage should not be treated as a document store clone of Cosmos. The external behavior can match, but the internal model should be explicitly blob-native:

- One blob per snapshot version.
- Blob name derived from SnapshotStreamKey plus version using a deterministic naming strategy.
- A provider-owned stored envelope serialized as bytes into the blob body.
- Prefix listing for stream-level enumeration and prune discovery.
- Metadata required for compatibility carried in the serialized envelope, not split across Azure Blob metadata headers.

The largest feasibility caveat is not correctness of Blob IO. It is memory behavior for large payloads. The current Cosmos path copies payload bytes multiple times through SnapshotEnvelope, mapper conversions, and storage models. That is acceptable for Cosmos-sized payloads, but the stated business value of "materially larger artifacts" raises real memory amplification risk if the Blob provider repeats the same copy-heavy flow uncritically.

## Technical Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Large-payload memory amplification from repeated byte copies during read and write | High | High | Keep the provider contract unchanged but minimize intermediate allocations inside the Blob implementation; prefer stream-oriented serialization and compression internally, and only materialize the final envelope bytes at the boundary where the contract requires them. |
| Blob naming scheme fails to support safe prefix listing, latest-version discovery, or prune enumeration | Medium | High | Treat blob naming as a first-class design decision. Base it on SnapshotStreamKey and version with an unambiguous reversible format, and validate it against the current key shapes in src/Tributary.Abstractions/SnapshotStreamKey.cs and src/Tributary.Abstractions/SnapshotKey.cs. |
| Stored envelope versioning is underspecified, causing future serializer or compression evolution pain | Medium | High | Put storage-format version, serializer identity, compression algorithm, and payload metadata into a fixed provider-owned blob envelope from v1, as already confirmed in requirements synthesis. |
| Blob-specific transient failures and eventual consistency assumptions differ from Cosmos behavior | Medium | Medium | Isolate Azure SDK interactions behind a Blob operations component analogous to SnapshotContainerOperations, add a Blob-specific retry policy, and keep contract-level behavior stable even if internal failure handling differs. |
| Prefix listing becomes slow for streams with many snapshots | Medium | Medium | Accept as a v1 tradeoff because manifest and index designs were explicitly deferred, but keep naming and prefix scoping tight so enumeration stays stream-local rather than container-wide. |
| Serializer plugability creates ambiguous DI behavior when multiple ISerializationProvider implementations exist | Medium | Medium | Do not rely on "the default singleton" alone. Make provider options explicitly name or select the serializer for snapshot payloads, while defaulting to the existing JSON registration when only one provider is present. |
| Compression and serializer metadata become invisible to support and tests | Low | High | Ensure the blob body envelope is inspectable and assertable in L0 and L2 tests, including compression mode, serializer identity, and format version. |
| L2 test reliability suffers if Azurite setup proves the raw Blob SDK only, not the new provider behavior | Medium | Medium | Build the Crescent L2 around actual Mississippi configuration and snapshot lifecycle behavior, not isolated Blob SDK smoke tests. The current BlobStorageTests prove emulator availability but not provider correctness. |

## Architecture Constraints

- Existing patterns that must be respected:
  - The provider should mirror the Cosmos provider layering: registrations, options, provider facade, repository, SDK operations wrapper, mappers, diagnostics, and focused L0 tests. Evidence: src/Tributary.Runtime.Storage.Cosmos/SnapshotStorageProviderRegistrations.cs, src/Tributary.Runtime.Storage.Cosmos/SnapshotStorageProvider.cs, src/Tributary.Runtime.Storage.Cosmos/Storage/SnapshotCosmosRepository.cs, and src/Tributary.Runtime.Storage.Cosmos/Storage/SnapshotContainerOperations.cs.
  - Public contracts already live in abstractions. The Blob provider should consume existing abstractions rather than add a new abstractions project unless a genuinely reusable public contract emerges. Evidence: src/Tributary.Runtime.Storage.Abstractions.
  - DI should follow the repo's keyed-service pattern for infrastructure clients. Evidence: src/Tributary.Runtime.Storage.Cosmos/SnapshotStorageProviderRegistrations.cs and src/Brooks.Runtime.Storage.Cosmos/BrookStorageProviderRegistrations.cs.
  - JSON default serialization should reuse the existing serialization provider pattern. Evidence: src/Brooks.Serialization.Abstractions/ISerializationProvider.cs and src/Brooks.Serialization.Json/ServiceRegistration.cs.

- Dependency direction constraints:
  - The new provider project should depend on Tributary.Runtime.Storage.Abstractions and existing common abstractions, not the reverse.
  - Serializer reuse should point toward Brooks.Serialization.Abstractions and optionally Brooks.Serialization.Json for default behavior, not a new Tributary-specific serializer abstraction.
  - Provider metadata and compression should stay internal to the provider project unless a second provider proves the abstraction is broadly needed.

- Integration points:
  - DI registration used by Mississippi adopters configuring Tributary snapshot storage.
  - Existing Tributary runtime snapshot persistence and reload behavior through SnapshotEnvelope.
  - Existing Brooks serialization registrations for default JSON behavior and optional alternate serializers.
  - Crescent AppHost and L2 test fixture for Azurite-backed end-to-end validation.

- Important contract constraints from the current model:
  - ISnapshotStorageProvider operates on SnapshotEnvelope, not typed state.
  - SnapshotEnvelope already contains Data, DataContentType, DataSizeBytes, and ReducerHash. Evidence: src/Tributary.Abstractions/SnapshotEnvelope.cs.
  - SnapshotStreamKey uses a pipe-delimited composite string. That string is safe as a blob name, but it is not sufficient by itself for lexical version ordering. Evidence: src/Tributary.Abstractions/SnapshotStreamKey.cs and src/Tributary.Abstractions/SnapshotKey.cs.

## Technology Choices

| Decision | Recommendation | Rationale | Alternative Considered |
|----------|---------------|-----------|----------------------|
| Provider structure | Mirror the Cosmos provider structure in a new Tributary Blob provider project | Lowest cognitive load for adopters and maintainers; aligns with confirmed parity requirement | Generalized cross-provider refactor now; rejected because it broadens scope before proving a second provider pattern |
| Payload serializer plug-in | Reuse Mississippi.Brooks.Serialization.Abstractions.ISerializationProvider | Already used by Tributary snapshot conversion and already has a JSON default implementation | Create a Blob-provider-specific serializer interface; rejected because it duplicates an existing repo-wide abstraction |
| Compression model | Provider-wide off or gzip option | Matches confirmed requirements and keeps compatibility simple | Threshold-based or per-artifact compression; explicitly out of scope |
| Stored format | Provider-owned binary or JSON envelope inside the blob body | Keeps metadata portable, inspectable, and versionable independent of Azure Blob metadata semantics | Split metadata between blob headers and body; rejected because it complicates compatibility and testing |
| Blob enumeration | Prefix listing with deterministic name parsing | Matches confirmed requirements and is sufficient for v1 prune and delete-all behavior | Manifest blob, secondary index, or blob tags; explicitly deferred |
| Retry handling | Add a Blob-specific retry component behind an operations wrapper | Keeps SDK concerns isolated and consistent with existing Cosmos provider design | Call Blob SDK directly from repository and provider; rejected because it reduces testability and makes transport concerns leak upward |
| L2 environment | Reuse Crescent Azurite-backed AppHost and fixture | The emulator is already provisioned and the sample suite already has blob connectivity coverage | New standalone L2 host; rejected because it duplicates existing sample infrastructure |

## Complexity Estimate

- T-shirt size: M
- Justification: The contract is already small, the repo already has the required DI and serializer patterns, and Crescent already provisions Azurite. The work is substantial but contained if it stays within the existing provider shape.
- Key complexity drivers:
  - Defining a stable stored envelope format that captures version, compression, and serializer metadata.
  - Designing deterministic blob names and prefix-scoped enumeration for delete-all and prune.
  - Preventing large-payload memory amplification.
  - Making serializer selection explicit without making adoption awkward.
  - Producing an L2 that validates real provider behavior rather than only emulator connectivity.

This becomes an L-sized effort if the scope expands into generic shared blob infrastructure, multi-provider coexistence, automatic compression heuristics, or a generalized cross-provider refactor.

## Implementation Approach

1. Preserve the current public storage contract and external registration shape; do not start by redesigning abstractions.
2. Define the stored blob envelope and blob naming strategy early, because both drive compatibility, lookup semantics, and test design.
3. Keep Azure SDK access behind a narrow Blob operations abstraction so repository tests stay at the same level of isolation as the Cosmos provider tests.
4. Reuse the existing serialization provider abstraction and make JSON the default path.
5. Treat large-payload allocation behavior as a design constraint from the start rather than a later optimization pass.
6. Use Crescent L2 to prove restart or reload compatibility and metadata inspectability, not just round-trip success.

## CoV: Feasibility Verification

1. Existing-pattern claims are verified against current repo evidence. The provider facade and repository split is present in src/Tributary.Runtime.Storage.Cosmos/SnapshotStorageProvider.cs. SDK isolation is present in src/Tributary.Runtime.Storage.Cosmos/Storage/SnapshotContainerOperations.cs. Keyed DI registration patterns are present in src/Tributary.Runtime.Storage.Cosmos/SnapshotStorageProviderRegistrations.cs and src/Brooks.Runtime.Storage.Cosmos/BrookStorageProviderRegistrations.cs. Existing Azure Blob usage in framework code is present in src/Brooks.Runtime.Storage.Cosmos/Locking/BlobDistributedLockManager.cs. Existing Azurite-backed sample infrastructure is present in samples/Crescent/Crescent.AppHost/Program.cs and samples/Crescent/Crescent.L2Tests/CrescentFixture.cs.

1. Serializer-reuse claims are verified against current repo evidence. Tributary already uses ISerializationProvider for snapshot state conversion in src/Tributary.Runtime/SnapshotStateConverter.cs. JSON default registration exists in src/Brooks.Serialization.Json/ServiceRegistration.cs.

1. Key-format and enumeration-risk claims are verified against current repo evidence. SnapshotStreamKey is a pipe-delimited composite string in src/Tributary.Abstractions/SnapshotStreamKey.cs. SnapshotKey contains a numeric version, but its default string layout is not optimized for lexical ordering in blob listings in src/Tributary.Abstractions/SnapshotKey.cs.

1. Memory-copy-risk claims are verified against current repo evidence. SnapshotEnvelope stores an immutable byte array in src/Tributary.Abstractions/SnapshotEnvelope.cs. Current mapper flow copies bytes between immutable arrays and byte arrays in src/Tributary.Runtime.Storage.Cosmos/Mapping/SnapshotWriteModelToStorageMapper.cs and src/Tributary.Runtime.Storage.Cosmos/Mapping/SnapshotStorageToEnvelopeMapper.cs. That copy pattern is not inherently wrong for Cosmos-sized payloads, but it is a real technical risk for the larger-payload use case that motivated Blob storage.

1. Final assessment: feasibility is strong. The dominant technical risks are naming and enumeration correctness, stored-format compatibility, and large-payload memory behavior. The dominant architecture constraint is to preserve the existing Tributary provider contract and Mississippi DI and serialization patterns while keeping Blob-specific concerns internal.
