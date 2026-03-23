# Performance Code Review

## Summary

- Hot paths affected: exact-version write, exact-version read, latest-read, prune, and delete-all.
- Performance impact: Medium.
- Verdict: CHANGES REQUESTED.

Draft plan v2 fixed most of the cycle 1 structural concerns. The remaining performance gaps are narrower and mostly about making the large-payload promise and the stream-scan cost shape measurable before implementation starts.

## Hot Path Analysis

1. Exact-version write and exact-version read are the real hot paths. They sit on the normal snapshot persistence and restore flow, so the main performance risk is payload-size amplification from extra buffers during encode, compress, checksum, upload, download, decompress, and decode.
2. Latest-read, prune, and delete-all are acceptable as stream-local O(n) operations in v1, but only if they stay list-driven and never turn candidate inspection into payload-body I/O. The algorithmic shape is fine; accidental remote reads or decompression on each candidate would not be.
3. Container initialization and startup validation are cold paths. They should stay clear and deterministic, but they do not justify optimization work beyond avoiding obviously wasteful retries or repeated container probes.

## Performance Concerns

### Must Fix

| # | Remaining issue | Complexity or allocation risk | Recommendation |
|---|---|---|---|
| 1 | The large-payload viability gate is still not measurable enough. | The plan asks for bounded buffering proof, but it still does not define a copy budget, allocation budget, or a deterministic payload-size matrix that would prove the Blob path does not multiply large payloads in memory. Without that, the main product promise can pass review without proving the real cost shape. | Add explicit payload sizes, a write-path and read-path copy budget, and a named evidence artifact. The plan should say how many full-payload materializations are acceptable on each path and what would count as failure. |
| 2 | The maintenance-path I/O contract is still too loose. | Latest-read, prune, and delete-all are correctly accepted as O(n) scans, but the plan still leaves room for implementations that download or open candidate blobs during enumeration. That would turn a name-scan into O(n * payload-bytes) network and allocation cost. | Lock the maintenance contract down further: candidate selection must be based on list results plus parsed blob names, with at most header-light work on the selected blob. Require proof that non-selected candidates never trigger payload download, decompression, or deserialization. |
| 3 | The encode and decode pipeline still does not define how checksum, compression, and upload compose without extra whole-payload buffers. | The codec design is directionally right, but the plan still does not say whether the implementation may build separate in-memory copies for header JSON, compressed payload, checksum input, and final upload body. On large snapshots that can create repeated large-object-heap pressure and unpredictable latency. | Add an explicit pipeline rule for how bytes move through encode and decode. Either require a streaming design or define a bounded buffering strategy, including where checksum calculation happens and how compressed and uncompressed lengths are captured without duplicating the payload more than the agreed budget allows. |

### Should Fix

| # | Remaining issue | Why it matters | Recommendation |
|---|---|---|---|
| 1 | The plan still mixes hot-path performance proof with the broader Crescent L2 trust slice. | L2 is useful for confidence, but it is a poor place to discover allocation amplification or per-operation byte inflation because restart, registration, and inspection concerns muddy the signal. | Keep the L2 slice focused on functional trust. Put the strongest performance proof in deterministic L0 or L1 evidence that can assert copy counts, request shape, and size-matrix behavior directly. |
| 2 | Page-wise scan memory behavior is still underdefined around `ListPageSizeHint`. | The plan correctly says `ListPageSizeHint` is a tuning bound, not a correctness feature, but it does not yet say that scan operations must process pages incrementally rather than accumulate a full listing before acting. That leaves room for avoidable memory growth on long streams. | State that latest-read, prune, and delete-all must process listings page-by-page and must not materialize the full stream listing unless the selected algorithm explicitly proves bounded memory for the target stream sizes. |
| 3 | Observability is improved, but the plan still does not make the performance triage fields mandatory enough. | If linear-scan costs or large-payload regressions appear later, reviewers and operators need candidate counts, page counts, bytes moved, and selected compression mode to explain the cost shape quickly. | Promote the metrics and diagnostics that expose list cost and byte movement into explicit evidence requirements rather than leaving them as best-effort implementation detail. |
| 4 | Concurrency cost expectations remain unstated beyond duplicate-version safety. | Correctness for duplicate writes is defined, but performance behavior under adjacent writes, concurrent latest-read calls, or prune overlap is still open to interpretation. That can lead to accidental contention or repeated scans being treated as bugs later. | Add one note stating whether v1 makes any throughput or contention guarantees beyond duplicate-version conflict safety, or whether those performance characteristics are intentionally unspecified. |

### Could Fix

| # | Remaining issue | Why it is optional | Recommendation |
|---|---|---|---|
| 1 | Optional Azure metadata duplication is still open as a supportability choice. | It is not required for correctness, and the body header is rightly authoritative. The only performance case for it is reducing the need for a header read during some diagnostic or maintenance flows. | Only add Azure metadata duplication if it is demonstrably cheap on writes and clearly non-authoritative. Otherwise keep the single source of truth in the blob body. |
| 2 | The plan could be more explicit about avoiding convenience-heavy collection transforms on scan paths. | This is an implementation-detail concern, not an architectural blocker, but stream scans are exactly where avoidable LINQ chains, full-list sorts, and `ToList()` materializations tend to sneak in. | Add a small implementation note that scan paths should prefer single-pass page processing and avoid whole-stream materialization unless required by a proven algorithmic need. |

### Won't Fix

| # | Deferred item | Rationale |
|---|---|---|
| 1 | Real-Azure throughput or latency benchmarking as a release gate | The correct v1 bar is bounded cost shape and functional correctness, not production-scale certification. |
| 2 | Manifest, pointer, tag, or index-based lookup to eliminate O(n) stream scans | The plan already made the right trade-off for v1: accept O(n) stream-local scans and prevent them from turning into payload-heavy operations. |
| 3 | Adaptive compression, per-snapshot codec selection, or broader codec expansion | Those features add complexity and more performance surface area before the simple `Off` and `Gzip` shape is proven. |
| 4 | Micro-optimizing cold paths such as startup initialization or one-time registration logic | The hot-path and large-payload risks matter more. Cold-path clarity is the right trade-off here. |

## Allocation Profile

- New allocations per invocation: still underdefined for large payload paths; the main remaining risk is multiple full-payload buffers on write and read.
- Boxing occurrences: no plan-level blocker identified.
- LINQ on hot paths: no explicit plan issue, but scan paths should avoid whole-list materialization and layered iterator chains over blob listings.

## Complexity Assessment

| Operation | Expected complexity | Acceptable? | Notes |
|-----------|---------------------|-------------|-------|
| Exact-version write | O(1) Blob operation plus payload processing | Yes, if copy and buffering limits are explicit | The complexity is fine; the remaining risk is allocation amplification on large payloads. |
| Exact-version read | O(1) Blob operation plus payload processing | Yes, if copy and buffering limits are explicit | Same issue as write: the algorithm is fine, the byte-movement contract is not yet tight enough. |
| Latest-read | O(n) stream-local scan | Yes | Acceptable in v1 only if non-selected candidates never trigger payload-body I/O. |
| Prune and delete-all | O(n) scan plus delete calls | Yes | Acceptable if listings are processed page-by-page and maintenance stays name-driven or header-light. |
| Startup initialization | O(1) per provider setup | Yes | Cold path; optimize for determinism and actionable failure messages rather than throughput. |

## Positive Performance Choices

- Hashed stream prefixes plus zero-padded version tokens are the right v1 shape for bounded naming and lexical latest selection without an index.
- Keeping the header uncompressed is a good trade-off for inspectability and avoids abusing blob-level content encoding for a partially compressed custom frame.
- Treating latest-read, prune, and delete-all as acceptable O(n) stream-local scans is the correct first-principles decision for v1.
- The plan explicitly rejects manifest, pointer, and adaptive-compression scope growth, which avoids premature complexity.

## CoV: Performance Verification

1. Hot path identification is correct: verified from the revised plan. Exact-version read and write are the steady-state per-snapshot operations; listing-based maintenance paths are less frequent but can become expensive if they read bodies instead of names.
2. Complexity analysis is accurate: verified. The plan intentionally accepts O(n) listing on a per-stream basis, which is reasonable for v1. The remaining concern is not big-O complexity but accidental byte-heavy work inside those scans.
3. Allocation concerns are verified, not assumed: verified from what the plan still omits. The draft now asks for bounded buffering proof, but it still does not define the actual byte-movement or copy limits that would make the large-payload promise reviewable.
