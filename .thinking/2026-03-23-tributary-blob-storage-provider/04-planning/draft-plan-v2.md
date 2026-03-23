# Draft Plan V2

## Executive Summary

Deliver a new Blob-backed Tributary snapshot storage provider that preserves the current contract and familiar setup shape while removing the practical Cosmos payload-size ceiling. The implementation plan is organized around early proof of the highest-risk behaviors: canonical naming, stable stored frame, deterministic serializer resolution, payload-only compression, duplicate-version safety, and large-payload viability.

## Problem-First Outcome

- User problem: some Tributary scenarios need to persist artifacts too large for the practical Cosmos-backed path.
- Product promise: switch storage backend, keep the same domain programming model.
- Trust proof: configure the Blob provider, write a large snapshot, inspect stored metadata, restart, and read back without domain changes.

## Scope Guardrails

- Keep the Tributary public contract unchanged.
- Mirror Cosmos provider ergonomics only at the public registration/options layer.
- Do not create new shared storage abstractions.
- Do not introduce manifest/index blobs, adaptive compression, or generic serializer fallback in v1.
- Treat Blob as an app-level alternative provider, not a mixed-provider orchestration feature.

## Contract-Parity Matrix

| Concern | Expected Blob v1 behavior |
| --- | --- |
| Public DI/options shape | Closely matches Cosmos provider patterns |
| Write duplicate version | Must fail without silent overwrite |
| Read missing stream/version | Returns the same contract-level outcome as Cosmos |
| Read latest | Stream-local list scan selects the correct latest version |
| Delete all | Stream-scoped only; no cross-stream bleed |
| Prune | Retains/removes the correct versions for the target stream only |
| Startup initialization | Deterministic create-or-validate behavior with actionable failures |

## Risk-To-Test Matrix

| Risk | Required layer | Required proof |
| --- | --- | --- |
| Wrong latest selection | L0 | Version-order tests including lexical traps and stream collisions |
| Cross-stream delete/prune bleed | L0 | Stream-scope naming and maintenance tests |
| Ambiguous serializer resolution | L0 | Multiple-serializer configuration validation and persisted serializer id proof |
| Unreadable restart due to ambient config | L0 and L2 | Self-describing metadata and restart/reload proof |
| Duplicate-version overwrite | L0 | Conditional-create conflict tests |
| Corrupt or incompatible blobs | L0 | Unknown version, unknown serializer, unknown compression, checksum failure, truncated gzip |
| Large-payload viability | L0 or L1 plus L2 smoke | Deterministic size matrix and bounded buffering proof |
| Over-eager maintenance downloads | L0 | Proof that enumeration paths do not download candidate payload bodies |

## Merge Evidence Package

- Zero-warning build and cleanup.
- Contract-parity proof against the matrix above.
- Large-payload proof over a deterministic payload-size matrix.
- Restart/reload proof using self-describing metadata.
- Failure-path proof for corrupt or incompatible blobs.
- Non-default serializer proof.
- Focused Crescent L2 proof slice.

## Work Breakdown

### Increment 1: Project wiring, scope gates, and parity scaffolding

- Create the new provider project and wire references according to existing Mississippi layering rules.
- Add the public registration/options surface to mirror Cosmos ergonomics where appropriate.
- Define the canonical registration path and position any alternate overloads as advanced variants.
- Add the contract-parity checklist and startup validation/failure-semantics checklist to the implementation notes.
- Add L0 scaffolding for parity, startup validation, and serializer-resolution behavior.

### Increment 2: Naming, canonical identity, and low-level Blob operations

- Implement canonical stream identity and hashed/zero-padded blob naming.
- Implement stream-local enumeration primitives and conditional-create write primitives.
- Prove no cross-stream collisions and no silent duplicate overwrites through L0 tests in the same increment.
- Capture operation complexity expectations and linear-scan caveats in diagnostics/docs notes.

### Increment 3: Stored frame contract, serializer identity, and checksum rules

- Implement the fixed blob frame prelude, bounded header, frame versioning rules, concrete payload serializer id, compression mode, and payload checksum.
- Implement tolerant header evolution and fail-fast rules for unsupported frame versions, serializers, and compression modes.
- Add L0 encode/decode, corruption, checksum, and serializer-identity tests in the same increment.
- Add the deterministic large-payload size matrix and early buffering/allocation proof here.

### Increment 4: Repository behavior integration

- Implement write, exact-version read, latest-read, delete-all, and prune using the established naming and frame primitives.
- Integrate provider-wide compression with `Off` and `Gzip` only.
- Ensure maintenance paths remain name-driven or header-light and never download candidate payload bodies unnecessarily.
- Add L0 tests for latest selection, prune correctness, delete scope, missing reads, duplicate conflicts, and non-default serializer reads.

### Increment 5: Initialization, diagnostics, and supportability

- Implement container initialization modes: create-if-missing and validate-exists.
- Add actionable validation errors for missing Blob client registrations, serializer misconfiguration, initialization mode failures, and unreadable blobs.
- Verify diagnostics for duplicate conflicts, decode/checksum failures, and relevant Azure triage fields.
- Add focused tests for startup behavior and user-facing failures.

### Increment 6: Crescent L2 trust slice

- Add one focused Azurite-backed Crescent scenario that proves: registration, large snapshot write, gzip path, non-default configuration or serializer path, metadata inspection path, restart/reload compatibility, and successful read-back.
- Keep L2 scoped to functional trust proof, not production-scale Azure certification.

### Increment 7: Documentation and final quality gates

- Add or update documentation for the canonical registration path, minimal setup snippet, Cosmos-to-Blob setup translation, Blob-versus-Cosmos guidance, and metadata inspection walkthrough.
- Run build, cleanup, unit tests, mutation tests where required, and final review evidence packaging.

## Performance and Scale Expectations

- Exact-version write and read should behave like direct blob operations for a known name.
- Latest-read, prune, and delete-all are acceptable as O(n) stream-local scans in v1.
- `ListPageSizeHint` is a tuning bound only, not a correctness mechanism.
- Maintenance operations must not decompress or deserialize non-selected payloads.

## Crescent L2 Exit Criteria

- Uses the canonical registration path.
- Exercises gzip and one non-default path.
- Demonstrates the provider stores self-describing data that survives restart.
- Shows the user problem clearly: larger payload storage without domain-code changes.

## Operational Boundaries To Document

- Prefix listing is linear per stream.
- Azurite proves functional confidence, not production-scale Azure behavior.
- Azure soft delete and blob versioning affect physical purge semantics outside the provider's logical contract.
- Blob body metadata is the source of truth; Azure blob metadata duplication is optional and not required for v1.