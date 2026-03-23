# Cloud Infrastructure Review

## Overall Verdict

The proposed design is broadly sound for v1, but only if it is tightened around a few Azure-specific behaviors that matter in production:

1. Writes must use conditional create semantics so a duplicate version cannot silently overwrite an existing blob.
2. Prefix listing must be treated as an $O(n)$ per-stream operation, not a cheap latest-record lookup strategy at scale.
3. Delete and prune semantics must explicitly account for storage-account features such as soft delete and blob versioning.
4. Azurite must be treated as a functional emulator, not proof of production retry, throughput, or error-shape behavior.

The current design makes good v1 calls on one-record-per-blob, hashed stream prefixes, fixed-width version tokens, and keeping the provider envelope self-describing. The main gaps are operational correctness and Azure service assumptions, not the core storage model.

## Service Assessment

| Service | Usage | Concern | Recommendation |
|---------|-------|---------|----------------|
| Azure Blob Storage block blobs | One logical snapshot per blob | Good fit for large payloads and simple durability, but operational semantics differ from Cosmos | Keep block blobs. Do not introduce append blobs or page blobs for v1. |
| Blob naming | `{prefix}{sha256(streamKey)}/v{version:D20}.snapshot` | Good bounded naming, but hash-only paths reduce operator discoverability | Keep the hash-based prefix. Optionally duplicate a minimal human-readable hint into blob metadata for diagnostics, but keep the body header authoritative. |
| Prefix listing | Used for prune, delete-all, and any latest-by-stream discovery | Azure lists alphabetically, but only in ascending order; latest lookup still requires walking the stream prefix to the end | Accept for v1 only if operations stay stream-local and infrequent. Document that latest-by-prefix is linear in snapshots per stream. |
| Provider metadata placement | All compatibility metadata stored in the uncompressed body header | Correct for portability and versioning, but poor for portal and Storage Explorer triage because metadata is not visible without reading blob content | Keep body-header metadata as the source of truth. Consider duplicating a very small subset into blob metadata such as stream hash, snapshot version, compression, payload format, and written time for ops visibility. |
| Blob metadata and tags | Explicitly not used for lookup in v1 | Correct. Tags add permissions, cost, and eventual-consistency lookup behavior; metadata has an 8 KB limit and is not queryable | Do not move core compatibility metadata to tags or metadata. If metadata is added, use it only as an operational aid. |
| Azure SDK usage | Single operations layer around Azure.Storage.Blobs | Sound boundary, but the design is silent on access conditions, transfer tuning, and client lifetime | Use singleton `BlobServiceClient` and container clients, Azure SDK built-in retries, and explicit request conditions on writes. |
| Azurite | L2 happy-path and restart validation | Sound for functional tests, but Azurite is not full Azure parity for scalability, exact errors, or all advanced features | Use Azurite only for happy-path persistence and restart tests. Do not use it to validate throttling, retry timing, or precise service behavior. |

## Strongest Azure-Specific Findings

### 1. Conditional writes are required for correctness

The largest cloud-specific gap is write behavior. Azure Blob upload overwrites an existing blob by default and replaces existing metadata. In this design, a blob name is deterministic from stream plus version. That means any duplicate write of the same logical snapshot version can silently replace prior content unless the provider sets a creation precondition.

Recommendation:

- Use `If-None-Match = *` on the write path for versioned blobs.
- Treat precondition failure as a domain-relevant conflict, not a transient retry candidate.
- Do not rely on naming uniqueness alone for correctness.

Without this, the provider is vulnerable to duplicate version writes, race conditions across hosts, and replay bugs that Cosmos-style optimistic expectations would normally surface.

### 2. Listing is strongly consistent, but still linear work

Azure Blob Storage is strongly consistent, and the REST list operation returns blobs in alphabetical order. That makes the zero-padded version suffix a valid ordering mechanism. However, Azure does not provide a reverse prefix scan, so a latest-record query still requires enumerating the full stream prefix or maintaining a separate index.

Recommendation:

- Treat list-based latest lookup as acceptable only for v1 and only when streams are expected to remain reasonably bounded.
- Keep all listing operations scoped to one stream hash prefix.
- Avoid language in the architecture that implies prefix listing is cheap enough to behave like an index.
- Keep the manifest or index-blob option explicitly open for v2 if stream histories become long-lived.

This is a scaling concern more than a correctness concern, but it will become visible in both transaction cost and tail latency.

### 3. Body-header metadata is the right source of truth

From an Azure perspective, keeping compatibility metadata inside the blob body is the right default. Blob metadata has an 8 KB total limit, uses HTTP-header rules, is case-insensitive, and is not queryable. Blob index tags are queryable, but they are limited to 10 tags, incur additional cost, require extra permissions, and tag-based find operations use an eventually consistent secondary index.

Recommendation:

- Keep the uncompressed provider header in the body as the authoritative metadata store.
- Do not depend on blob metadata or tags for restore correctness.
- If operator visibility matters, duplicate only a tiny subset into blob metadata, not tags.
- Do not set `Content-Encoding: gzip` if only the payload segment is compressed. That header describes the full blob representation and would mislead downstream tooling.

That last point matters operationally. If the blob body is a custom frame with only the payload segment compressed, Azure system properties must not advertise the blob itself as gzip-compressed.

### 4. Delete semantics depend on account features, not only provider logic

The proposed delete, delete-all, and prune flows are logically correct against the blob namespace, but the architecture currently reads as though a delete removes storage immediately. In Azure, that is only true when account-level features such as soft delete and blob versioning are disabled. If those features are enabled, deletes may leave soft-deleted or versioned artifacts billable and recoverable.

Recommendation:

- Explicitly document the provider's contract as deleting the current named blob, not guaranteeing irreversible physical purge.
- State the expected storage-account settings for production if predictable prune cost and irreversible deletion semantics matter.
- Avoid promising exact storage reclamation timing in docs or tests.

This is an operational correctness issue, not just documentation polish. Teams will otherwise misread prune behavior and storage bills.

### 5. Azurite is suitable for functional validation, not behavior parity

Azurite is a good fit for L2 happy-path testing here because the design does not rely on blob-tag queries or other advanced lookup features. That said, Azurite's own documentation is explicit that it is not Azure for scalability, throughput, error messages, or full behavior parity.

Recommendation:

- Use Azurite for end-to-end persistence, restart/reload, delete-all, and prune happy paths.
- Do not use Azurite to validate retry policy tuning, timeouts under load, throttling behavior, or exact `RequestFailedException` shapes.
- Keep any future tag-based design out of required L2 coverage unless the team is willing to accept emulator drift.

That boundary is important. Emulator-passing tests should not be mistaken for cloud-operational proof.

## Cost Analysis

The design is cost-reasonable for v1, with a few predictable Blob Storage cost drivers:

- Reads by exact blob name are cheap and efficient.
- Prune and delete-all are list-heavy and therefore transaction-heavy as stream history grows.
- Hash-based stream prefixes do not materially change cost, but they do keep operations tightly scoped to a single logical stream.
- Avoiding blob index tags in v1 is the right cost choice because tags add per-tag cost and additional tag operations.

Estimated cost implications and optimization opportunities:

- Current design is acceptable when prune and delete-all are background or infrequent maintenance operations.
- If latest-read-by-stream becomes hot-path behavior, a manifest blob or pointer blob will be the first meaningful optimization.
- If operators want portal-visible diagnostics, a few metadata headers are cheaper than tag-based lookup and operationally simpler.

## Resilience Assessment

- Failure modes identified:
  - Duplicate version write overwrites existing blob.
  - Long list operations increase latency and transaction count for old streams.
  - Corrupt envelope or unsupported compression blocks restore.
  - Storage account settings such as soft delete alter delete semantics.
  - Transient storage failures trigger retries at the SDK layer.
- Mitigation in place:
  - Strong consistency for blob reads and lists.
  - Self-describing body header with storage format and compression metadata.
  - Azure SDK retry capability.
  - Stream-local prefix enumeration rather than container-wide scans.
- Gaps:
  - No explicit conditional write protection yet.
  - No stated timeout or transfer tuning strategy for large payloads.
  - No explicit stance on storage account features that change lifecycle semantics.
  - No separation between emulator confidence and cloud confidence in the test strategy.

## Operational Readiness

- Health checks: Partial. Startup container validation is useful, but consider whether production should create the container or only validate it exists. Pre-provisioned containers via IaC are usually safer in locked-down environments.
- Observability: Mostly adequate. The proposed logs and metrics are appropriate, but add ETag, request ID, and retry count where feasible because those are the first fields needed during storage incident triage.
- Zero-downtime deployment: Possible. The format-versioned body header supports rolling deployment, but only if readers tolerate known older formats and writes remain append-by-version rather than overwrite-in-place.

## Recommendations for the Architecture Document

1. Add an explicit requirement that writes use Azure blob access conditions to prevent overwriting an existing versioned blob.
2. Rephrase latest-record discovery so it is described as a stream-local linear scan, not an indexed lookup.
3. Add a short operational note on how Azure soft delete and blob versioning change delete and prune expectations.
4. State that the provider must not set blob `Content-Encoding` when only the payload segment is compressed.
5. Clarify that any duplicated blob metadata is diagnostic only and never authoritative.
6. Clarify that Azurite L2 proves functional persistence, not cloud-scale or cloud-error parity.
7. Consider making container creation optional, with a mode that validates existence instead of creating it, for principle-of-least-privilege deployments.

## Soundness for v1

Yes, with conditions.

The proposal is sound for v1 if the team accepts list-based maintenance costs and addresses the Azure-specific correctness gap around conditional writes. The naming model, envelope strategy, and metadata-in-body decision are all defensible in Azure Blob Storage. The design becomes risky only if it is implemented with implicit overwrite behavior, if list-based latest lookup is treated as scalable, or if production documentation ignores account-level delete semantics.

## CoV: Cloud Verification

1. Service recommendations based on actual Azure service capabilities: verified against Microsoft documentation for blob listing order, metadata limits, tag behavior, retry configuration, and upload overwrite semantics.
2. Cost estimates grounded in Azure pricing behavior: verified qualitatively from list-transaction and tag-cost behavior; no speculative dollar estimates provided.
3. Resilience patterns match actual failure modes: verified against Azure guidance for transient faults, SDK retries, and operational differences documented by Azurite.

## Sources Checked

- Microsoft Learn: List Blobs REST remarks, including alphabetical ordering and tag-return behavior.
- Microsoft Learn: Blob metadata rules and 8 KB total metadata limit.
- Microsoft Learn: Blob index tags behavior, limits, permissions, pricing, and eventual consistency of tag queries.
- Microsoft Learn: Azure Storage .NET retry configuration and default retry settings.
- Microsoft Learn: Blob upload behavior showing overwrite semantics unless access conditions are set.
- Azurite support matrix and documented differences from Azure Storage.
