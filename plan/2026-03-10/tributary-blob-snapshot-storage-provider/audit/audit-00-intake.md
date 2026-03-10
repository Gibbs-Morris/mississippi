# 00 Intake

## Objective
Plan the implementation of a Blob-native Tributary snapshot storage provider that matches the current Cosmos snapshot provider's consumer experience while adding Blob-specific behavior only where explicitly required by the issue.

## Non-goals
- Implementing the provider in this planning PR
- Adding cross-provider migration tooling
- Redesigning the shared snapshot abstraction unless implementation evidence later proves it unavoidable
- Introducing Blob access tier configuration in V1
- Producing migration guidance in this epic

## Constraints
- The parity baseline is the existing Cosmos snapshot provider and its tests.
- The provider must preserve one-blob-per-version layout and existing prune semantics.
- The durable blob path is part of the provider contract and must be based on `brookName`, `snapshotStorageName`, `entityId`, `reducersHash`, and `version`.
- Registration must follow existing Mississippi keyed-client and synchronous DI rules, with async bootstrap deferred to hosted startup work.
- V1 compression is opt-in and default-off.
- Verification must include Mississippi L0 coverage, a dedicated Mississippi L2 Blob/Azurite project, and a repeatable live Azure Blob smoke path.
- Documentation must include a Docusaurus provider page and at least one concrete runtime or sample wiring example.

## Assumptions
- A new provider package under `src/` plus matching test projects under `tests/` is acceptable if it follows existing Mississippi project naming and storage-provider patterns.
- The implementation should prefer host-provided keyed `BlobServiceClient` registrations, with convenience connection-string overloads added only as a secondary path.
- The existing snapshot envelope and reader/writer/provider abstractions should remain unchanged unless later evidence proves a contract gap.

## Open Questions
1. Which repo-consistent form should the required live Azure Blob smoke path take: CI workflow, manual script, or opt-in test project path?
2. Which runtime or sample should be the canonical wiring example for the provider documentation and validation path?

## CoV
- **Key claims**: The work must preserve parity with the Cosmos provider, keep the shared abstraction stable in V1, and add both emulator-backed and live-cloud verification.
- **Evidence**: Issue statement plus current `ISnapshotStorageProvider` and existing Cosmos provider structure in `src/Tributary.Runtime.Storage.Abstractions/ISnapshotStorageProvider.cs` and `src/Tributary.Runtime.Storage.Cosmos/SnapshotStorageProviderRegistrations.cs`.
- **Confidence**: High for scope and constraints; medium for the exact live-smoke delivery shape pending repository-pattern confirmation.
- **Impact**: The plan will optimize for parity-first implementation slices and reserve one planning decision for the live smoke path.
