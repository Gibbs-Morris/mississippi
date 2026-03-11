# Tributary Blob Snapshot Storage Provider Epic Plan

## Overview

This plan delivers a Blob-native Tributary snapshot storage provider that matches the current Cosmos provider's consumer experience, preserves the shared V1 snapshot abstraction, adds Mississippi-owned verification, and documents both framework-host and sample-host wiring.

## Objective

Implement `Mississippi.Tributary.Runtime.Storage.Blob` as an additive sibling to the Cosmos provider so applications can persist Tributary snapshots in Azure Blob Storage using the same repository-standard registration, diagnostics, and test conventions.

## Final decisions

- **Emulator-backed verification**: Aspire with Azurite.
- **Live Azure smoke path**: Option A — a dedicated opt-in Mississippi live/L2-style verification path that runs only when Azure credentials and target storage settings are supplied.
- **Documentation/wiring parity**: Option C — document both a framework-host example and a sample-host example, matching the Cosmos-provider precedent.
- **Abstraction scope**: Keep `ISnapshotStorageProvider` unchanged in V1 unless implementation evidence proves a contract gap.
- **Compression**: Include opt-in compression only; default remains off.

## Repository-grounded findings

1. **Cosmos provider is the parity baseline.**
   - Evidence: `audit/audit-01-repo-findings.md` Finding 1, citing `src/Tributary.Runtime.Storage.Cosmos/SnapshotStorageProvider.cs`, `src/Tributary.Runtime.Storage.Cosmos/SnapshotStorageProviderRegistrations.cs`, and Cosmos L0 tests.
2. **The shared abstraction can remain unchanged in V1.**
   - Evidence: `audit/audit-01-repo-findings.md` Finding 2, citing `src/Tributary.Runtime.Storage.Abstractions/ISnapshotStorageProvider.cs` and `SnapshotStorageProviderExtensions.cs`.
3. **Keyed clients plus hosted startup initialization are the required registration model.**
   - Evidence: `audit/audit-01-repo-findings.md` Finding 3 plus `.github/instructions/keyed-services.instructions.md` and `.github/instructions/service-registration.instructions.md`.
4. **Aspire + Azurite already exists as the blob emulator pattern.**
   - Evidence: `audit/audit-01-repo-findings.md` Finding 4, citing `samples/Crescent/Crescent.AppHost/Program.cs`, `samples/Crescent/Crescent.L2Tests/CrescentFixture.cs`, and `samples/Crescent/Crescent.L2Tests/BlobStorageTests.cs`.
5. **Mississippi L2 + AppHost testing is the repository-consistent integration-test shape.**
   - Evidence: `audit/audit-01-repo-findings.md` Finding 5 and `.github/instructions/testing.instructions.md`.
6. **Tributary storage-provider docs already have the right home and page model.**
   - Evidence: `audit/audit-01-repo-findings.md` Finding 6, citing `docs/Docusaurus/docs/tributary/storage-providers/index.md`, `docs/Docusaurus/docs/tributary/storage-providers/cosmos.md`, and `docs/Docusaurus/docs/contributing/documentation-guide.md`.

## Solution shape

### Package and code layout

The implementation should add a Blob provider package under `src/` and matching test projects under `tests/`, following the same role and naming conventions as the Cosmos provider.

Planned package/test surface:

- `src/Tributary.Runtime.Storage.Blob/`
- `tests/Tributary.Runtime.Storage.Blob.L0Tests/`
- `tests/Tributary.Runtime.Storage.Blob.L2Tests/`
- `tests/Tributary.Runtime.Storage.Blob.L2Tests.AppHost/`

### Core implementation responsibilities

The Blob provider slice should include:

- Blob-native repository implementation for snapshot read, write, list, and prune operations
- Deterministic blob path generation using `brookName`, `snapshotStorageName`, `entityId`, `reducersHash`, and `version`
- Provider facade implementing `ISnapshotStorageProvider` with parity logging and metrics shape
- Registration overloads mirroring the Cosmos provider family
- Keyed `BlobServiceClient` resolution via module-owned defaults
- Async startup/initialization deferred to hosted services rather than DI registration
- Opt-in compression option with default-off behavior
- Blob-specific optimistic concurrency handled internally without widening the shared abstraction

### Verification strategy

#### L0

Add focused Mississippi L0 tests covering:

- `Format` identity
- registration overloads and service graph shape
- path computation and storage-model mapping
- provider facade argument validation and repository delegation
- repository read/write/list/prune semantics
- conflict/ETag handling and Blob-specific failure translation
- compression option behavior with default-off baseline

#### L2 emulator

Add a dedicated Mississippi L2 project plus companion Aspire AppHost that:

- provisions Azure Storage via Azurite
- registers the Blob provider with keyed `BlobServiceClient`
- exercises end-to-end snapshot write, read, version listing, and prune flows
- validates hosted initialization and container bootstrap behavior

#### Live Azure smoke path

Extend the Mississippi L2 surface with a dedicated live-cloud verification path that:

- is opt-in only
- activates when required Azure configuration/credentials are present
- runs against a real Azure Blob account
- verifies the same core snapshot lifecycle with minimal live-cloud cost
- is safe to skip automatically when configuration is absent

### Documentation strategy

Add and update Docusaurus docs under `docs/Docusaurus/docs/tributary/storage-providers/`:

- add `blob.md` mirroring the Cosmos provider page shape
- update `index.md` to list Blob alongside Cosmos
- include a verified framework-host registration example
- include a verified sample-host/Aspire wiring example
- document Azurite-backed local verification plus the opt-in live Azure smoke prerequisites

## Work breakdown

1. Build the provider package and L0 coverage to establish parity with Cosmos.
2. Add Mississippi L2/AppHost Azurite verification.
3. Add the opt-in live Azure Blob smoke path inside the L2 verification surface.
4. Publish the Blob provider docs and both wiring examples.

## Deployability and rollout

- The work is additive: no existing provider or consumer behavior changes.
- The new package is safe to merge incrementally because nothing changes until a host explicitly references and registers the Blob provider.
- No cross-sub-plan partial grain or event/reducer contracts are involved.
- Storage-name immutability is preserved because the provider adds new implementation assets rather than mutating existing persisted type identities.
- Feature gating is not required for runtime safety because the new behavior is opt-in by package adoption and service registration; the live-cloud verification path is additionally opt-in by configuration presence.

## Quality gates

Each implementation PR must:

- build with zero warnings
- follow keyed-service and hosted-initialization patterns
- add or update Mississippi tests at the appropriate level
- preserve current provider abstractions unless implementation evidence proves otherwise
- avoid introducing unverified documentation claims
- keep Azure live verification opt-in and safe to skip when configuration is absent

## Instruction extraction result

No reusable instruction update is required. Existing instructions already cover:

- keyed services
- service registration
- Mississippi L2/AppHost testing
- Aspire Blob emulator usage
- Docusaurus authoring and page structure

## Dependency graph

```mermaid
graph LR
    PR1[PR 1: Plan] --> SP01[01: blob-provider-core]
    SP01 --> SP02[02: azurite-l2-verification]
    SP02 --> SP03[03: live-azure-smoke]
    SP03 --> SP04[04: docs-and-wiring]
    SP04 --> PRZ[PR Z: Cleanup]
```

## Sub-plan summary

| ID | Title | Depends on | Outcome |
| --- | --- | --- | --- |
| 01 | Blob provider core | none | Additive Blob provider package, repository, registrations, and L0 coverage |
| 02 | Azurite L2 verification | 01 | Mississippi L2 + AppHost end-to-end verification using Aspire + Azurite |
| 03 | Live Azure smoke | 02 | Opt-in live-cloud verification path using real Azure Blob configuration |
| 04 | Docs and wiring | 03 | Blob provider docs plus verified framework-host and sample-host examples |

## CoV

- **Key claims**: The plan preserves Cosmos-provider parity, keeps the shared abstraction stable, uses repo-standard testing patterns, and now fully reflects explicit user decisions.
- **Evidence**: `audit/audit-00-intake.md`, `audit/audit-01-repo-findings.md`, `audit/audit-02-clarifying-questions.md`, `audit/audit-03-decisions.md`, and repository source/doc files cited above.
- **Confidence**: High.
- **Impact**: The epic is ready for implementation handoff in small, deployable slices beginning with sub-plan 01.
