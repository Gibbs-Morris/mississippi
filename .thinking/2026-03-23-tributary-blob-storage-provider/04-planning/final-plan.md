# Final Plan

## Executive Summary

Implement a new `Tributary.Runtime.Storage.Blob` provider that preserves the existing Tributary contract and closely mirrors Cosmos registration ergonomics while enabling materially larger payloads through Azure Blob Storage. The plan is deliberately proof-driven: the earliest increments must establish stable naming, a durable blob frame, deterministic serializer resolution, duplicate-version safety, and bounded large-payload behavior before broader repository integration is considered done.

## Product Promise

- Solve oversized Tributary snapshot persistence scenarios.
- Keep the Mississippi domain programming model unchanged.
- Let existing adopters move from Cosmos-shaped setup to Blob-shaped setup with minimal mental overhead.

## Scope Guardrails

- No changes to the Tributary public storage contract.
- No new shared storage abstractions or new abstractions project.
- No manifest/index/tag/latest-marker redesign in v1.
- No adaptive compression, additional codecs, or serializer probing fallback.
- Blob is an app-level alternative provider, not a mixed-provider orchestration feature.

## Canonical Registration Path

- The primary user path is a single canonical Blob registration flow that mirrors the Cosmos provider's main setup pattern as closely as Blob semantics allow.
- Any alternate overloads or keyed-client variations are advanced paths and must not obscure the default setup story.

## Exact Observable Contract Outcomes

| Scenario | Required consumer-visible outcome |
| --- | --- |
| Read exact version when blob exists | Returns the requested snapshot through the existing Tributary contract |
| Read exact version when blob does not exist | Returns `null`, without a misleading decode or serializer failure |
| Read latest when no snapshots exist for the stream | Returns `null` |
| Write new version when blob does not exist | Succeeds and persists a readable snapshot |
| Write duplicate version when blob already exists | Fails as a duplicate-version conflict with no silent overwrite |
| Delete missing stream or version | Remains idempotent and non-throwing, with no cross-stream side effects |
| Delete all for one stream | Removes only blobs for the target stream |
| Prune for one stream | Keeps and removes the correct versions for the target stream only |
| Startup with missing required Blob client or invalid container mode | Fails host startup deterministically with actionable configuration guidance |
| Read blob with unknown frame version, unknown serializer id, unknown compression mode, checksum failure, or corrupt/truncated payload | Fails the read with actionable unreadable-blob diagnostics and never masquerades as a valid snapshot |

## Serializer Contract

- JSON is the default configured payload serializer path.
- Persist the concrete `payloadSerializerId` used to produce the stored payload bytes.
- Duplicate-version writes are intentionally stricter than today's Cosmos upsert behavior and must surface a conflict rather than overwrite.
- Startup validation rules:
  - Zero matching serializers for the configured/default path: fail startup.
  - Multiple matching serializers for the resolved path: fail startup.
  - A resolved serializer id must be concrete, stable, and persisted in the stored header.
- Read validation rule:
  - Unknown persisted serializer identities fail read with actionable diagnostics; the provider must not probe multiple serializers opportunistically.
- Mandatory evidence:
  - A non-default serializer path must survive restart and still read back successfully.

## Large-Payload Viability Gate

The feature does not meet its purpose unless it proves materially larger payloads are handled without accidental whole-payload copy amplification.

### Deterministic payload-size matrix

- `256 KB`
- `1 MB`
- `5 MB`
- `16 MB`

### Required measurable evidence

- Named artifact: `large-payload-evidence.md` under the implementation increment that introduces the frame/codec path.
- For each payload size, record:
  - write success/failure
  - read success/failure
  - compression mode used
  - bytes uploaded/downloaded if measurable
  - whether the implementation stayed within the planned buffering model

### Buffering and copy-budget expectations

- The plan must make the encode/decode byte pipeline explicit.
- Whole-payload buffers should be bounded to the minimum unavoidable steps for the chosen Azure SDK interaction model.
- Maintenance paths must never download, decompress, or deserialize non-selected candidate payloads.

## Risk-To-Test Matrix

| Risk | Required layer | Required proof |
| --- | --- | --- |
| Wrong latest selection | L0 | Version-order tests including lexical traps, zero-padding correctness, and cross-stream collisions |
| Cross-stream delete/prune bleed | L0 | Stream-scope naming and maintenance tests |
| Duplicate-version overwrite | L0 | Conditional-create conflict tests |
| Unknown/corrupt/unreadable blobs | L0 | Unknown frame version, unknown serializer id, unknown compression mode, checksum failure, truncated gzip, and malformed header tests |
| Ambiguous serializer resolution | L0 | Zero-match and multi-match startup validation tests |
| Restart depends on ambient config | L0 and L2 | Self-describing metadata plus restart/reload proof, including non-default serializer restart survival |
| Large-payload copy amplification | L0 or L1 plus evidence artifact | Deterministic payload-size matrix with explicit buffering/copy assessment |
| Over-eager maintenance downloads | L0 | Proof that enumeration paths stay name-driven or header-light |

## Observability Evidence Matrix

| Event | Required evidence |
| --- | --- |
| Startup misconfiguration | Actionable error path tested and documented |
| Duplicate-version conflict | Conflict surfaced with reviewable diagnostics |
| Unknown serializer or compression id | Unreadable-blob diagnostics tested |
| Checksum or decode failure | Unreadable-blob diagnostics tested |
| Initialization mode failure | Actionable startup diagnostics tested |

## Implementation Increments

### Increment 1: Project wiring and public setup surface

- Create the new provider project and references with correct layer boundaries.
- Add the canonical registration path and classify alternate overloads as advanced.
- Add startup validation scaffolding and tests for missing Blob client registrations and ambiguous serializer resolution.
- Produce parity notes for public setup and initialization behavior.

### Increment 2: Naming, conditional writes, and stream-local enumeration

- Implement canonical stream identity and hashed/zero-padded blob naming.
- Implement conditional-create semantics and paged, stream-local listing primitives.
- Add L0 tests for duplicate-version conflicts, cross-stream isolation, and correct version selection.

### Increment 3: Stored frame, serializer identity, checksum, and payload gate

- Implement the versioned blob frame with fixed prelude, bounded header, concrete serializer id, compression mode, and checksum.
- Implement fail-fast rules for unknown frame versions, serializers, and compression modes.
- Add L0 encode/decode and corruption tests.
- Produce `large-payload-evidence.md` using the deterministic size matrix.

### Increment 4: Repository behavior integration

- Implement write, exact read, latest read, delete all, and prune on top of the established primitives.
- Integrate provider-wide `Off`/`Gzip` compression.
- Prove maintenance paths never download non-selected candidate payload bodies.
- Add L0 tests for missing reads, delete-missing, prune correctness, and non-default serializer reads.

### Increment 5: Initialization, diagnostics, and user-facing failures

- Implement `CreateIfMissing` and `ValidateExists` container initialization modes.
- Add actionable diagnostics for configuration failures, duplicate conflicts, and unreadable blobs.
- Verify the observability matrix through tests.

### Increment 6: Crescent L2 trust slice

- Add one focused Azurite-backed scenario that proves:
  - canonical registration path
  - large snapshot write
  - restart
  - read-back
  - gzip enabled
  - non-default serializer restart survival
- Other checks remain additive only if they are cheap and non-fragile.

### Increment 7: Documentation and quality gates

- Add setup guidance for the canonical path.
- Add a Cosmos-to-Blob setup translation snippet.
- Add a short Blob-versus-Cosmos decision guide.
- Add a metadata inspection walkthrough grounded in the persisted blob frame.
- Run build, cleanup, unit tests, mutation tests where required, and prepare PR evidence.

## Performance and Operational Boundaries

- Exact-version reads and writes are direct-name operations.
- Latest-read, prune, and delete-all are acceptable as O(n) stream-local scans in v1.
- Listing must be processed incrementally page by page.
- `ListPageSizeHint` is a tuning knob, not a correctness feature.
- Azurite is a functional confidence tool only, not a real-Azure scale or retry-behavior gate.
- Azure soft delete and blob versioning affect physical purge semantics outside the provider's logical contract.

## Merge Evidence Package

- Zero-warning build and cleanup.
- Contract-outcome proof for the exact observable outcomes table.
- `large-payload-evidence.md` proving the deterministic size matrix.
- Non-default serializer restart-survival proof.
- Failure-path proof for unreadable blobs.
- Focused Crescent L2 trust proof.
- Documentation updates for setup and positioning.