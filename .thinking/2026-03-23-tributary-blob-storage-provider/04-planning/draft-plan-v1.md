# Draft Plan V1

## Executive Summary

Implement a new Blob-backed Tributary snapshot storage provider that mirrors the current Cosmos provider's public usage model while supporting materially larger payloads. The provider will store one logical snapshot per blob, use a provider-owned self-describing blob frame, support provider-wide gzip compression, and persist the concrete payload serializer identity with JSON as the default configured path.

## Current State

- Tributary currently has a Cosmos-based storage provider and no Blob-backed equivalent for larger payload scenarios.
- Existing adopters already understand the Cosmos contract and registration patterns.
- The repository already contains Azure Blob patterns, serialization infrastructure, Azurite-backed testing patterns, and ADRs for the new provider design.

## Target State

- A new `Tributary.Runtime.Storage.Blob` provider exists with public registration and options patterns closely aligned to `Tributary.Runtime.Storage.Cosmos`.
- The provider preserves existing Tributary contracts and contract-level behavior.
- Payload bytes can be persisted using JSON by default or another configured serializer provider.
- Stored blobs carry a stable, self-describing frame with version, serializer identity, compression mode, and payload integrity information.
- Comprehensive L0 coverage exists, and Crescent exercises a real Blob-backed end-to-end scenario with compression, metadata validation, and restart/reload compatibility.

## Design Decisions To Honor

- Do not change the Tributary public storage contract.
- Use canonical stream identity plus hashed blob naming with stream-local listing.
- Use conditional blob creation for duplicate-version safety.
- Store a provider-owned frame inside the blob body, not across Azure metadata as the source of truth.
- Compress payload bytes only, never the entire blob frame.
- Persist the concrete payload serializer identity.
- Keep container initialization configurable for create-if-missing versus validate-exists.

## Work Breakdown

### Increment 1: Provider skeleton and project wiring

- Create the new provider project and wire solution/project references using existing Mississippi conventions.
- Mirror the Cosmos provider surface: options type, DI registration, initialization path, logging surface, and diagnostics skeleton.
- Add any required ADR/doc references if the new project structure introduces visible user-facing setup changes.

### Increment 2: Blob frame, naming, and low-level storage operations

- Implement canonical stream identity, hashed blob naming, and low-level Blob operations wrapper.
- Implement the provider-owned blob frame with fixed prelude, bounded header, serializer identity, compression mode, and payload checksum.
- Implement conditional create semantics and read/list/delete primitives.

### Increment 3: Repository behavior and provider contract integration

- Implement write, read-latest, delete-all, and prune behavior so contract-level outcomes match the Cosmos provider.
- Integrate serializer selection and payload compression.
- Implement container initialization modes and relevant diagnostics.

### Increment 4: Unit test completion

- Add strong L0 coverage for naming, frame encode/decode, serializer identity handling, gzip on/off, checksum validation, duplicate-version conflict handling, latest selection, prune scope, delete scope, and corrupt-data rejection.
- Add any targeted L1 tests only if a behavior is awkward to prove at L0.

### Increment 5: Crescent L2 integration

- Add a Blob-backed Crescent scenario using Azurite.
- Validate registration, non-default configuration, compression, metadata visibility, persisted reload after restart, and basic maintenance behavior.

### Increment 6: Final quality and documentation

- Run build, cleanup, unit tests, and mutation tests where required.
- Update user-facing documentation if the new provider introduces public setup or behavioral guidance.
- Prepare PR artifacts and merge-readiness evidence.

## Testing Strategy

- L0 is the primary quality layer.
- Include representative multi-megabyte payload tests early, not only in the final integration pass.
- Use Crescent L2 for functional Blob persistence confidence only; do not treat Azurite as proof of production-scale Azure behavior.
- Ensure tests cover restart safety via self-describing metadata rather than ambient configuration.

## Acceptance Criteria

- A framework adopter can configure the Blob provider through a familiar Mississippi setup surface.
- Larger payload snapshots can be written and read through the existing Tributary contract.
- Compression can be configured provider-wide as `Off` or `Gzip`.
- JSON is the default serializer path, and a non-default serializer path can be exercised.
- Duplicate snapshot versions do not silently overwrite.
- Latest-read, prune, and delete behaviors are correct and stream-scoped.
- Stored blobs are self-describing and restart-safe.
- Comprehensive unit tests pass.
- Crescent L2 proves the configured Blob path end to end.

## Risks To Review In Planning

- Project/package placement and dependency direction.
- Risk of accidental abstraction leakage into Tributary contracts.
- Whether any common logic should be reused from Cosmos versus kept provider-local.
- The smallest viable Crescent L2 slice that still proves durable value.