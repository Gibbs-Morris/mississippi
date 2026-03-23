# Performance Review Cycle 01

## Summary

- Hot paths affected: snapshot write, exact-version read, latest-read selection, prune, and delete-all for stream-local snapshot sets.
- Performance impact if shipped as currently planned: Medium-High. The design is directionally sound, but the plan still leaves the two main v1 performance risks under-specified: large-payload memory amplification and linear-scan behavior under realistic stream history sizes.
- Verdict: Not Ready from a performance-planning perspective.

## Must-Fix

1. Add an explicit large-payload allocation gate before repository behavior is considered complete. The plan says representative multi-megabyte tests should happen early, but it does not define what acceptable means. For this feature, acceptable v1 behavior is not "works with a big blob once." It is "does not multiply the payload into several full-size in-memory copies during encode, compress, upload, download, decompress, and decode." The revised plan should require a concrete copy-budget or pipeline statement for write and read paths, plus proof with representative payload sizes.

2. Lock down that latest selection, prune selection, and delete-all selection remain name-driven or header-light and never require downloading candidate payloads. The architecture correctly accepts stream-local linear listing in v1, but the plan does not explicitly forbid a later implementation from reading blob bodies during enumeration. That would turn O(n) listing into O(n) listing plus O(total payload bytes), which is the wrong cost profile for large snapshots. The plan should state that version choice and retention decisions must be derivable from blob name parsing and list results alone.

3. Add a scan-cost proof for v1 instead of treating "stream-local linear scan" as self-justifying. Linear per-stream enumeration is acceptable, but only if the team proves that prune and delete-all stay operationally reasonable for expected snapshot counts. The plan should require evidence with representative stream histories showing page counts, request counts, and the absence of quadratic behavior or repeated rescans.

4. Pull payload-size and buffering constraints into the persisted-format increment. The frame design is already careful about keeping the header small and uncompressed, but the plan does not require proof that checksum calculation, compression, and framing can be performed without simultaneously holding multiple full payload representations longer than necessary. That proof belongs with frame and codec work, not as a late validation exercise.

5. Define the minimum v1 performance evidence package before implementation starts. Right now the plan names tests, but not proof. Require merge evidence for: representative large-payload write and read behavior, gzip-on and gzip-off memory behavior, latest-read and prune scan behavior over representative stream sizes, and confirmation that no maintenance path downloads or decompresses non-selected snapshot payloads.

## Should-Fix

1. Make the intended complexity of each operation explicit in the plan. Exact-version read and write should stay O(1) relative to stream history size. Latest-read, prune, and delete-all may be O(n) in snapshots for a single stream. That should be written down so reviewers can reject accidental whole-container scans, repeated listings, or body-inspecting maintenance logic.

2. Add a bounded test matrix for large-payload sizes instead of a generic "multi-megabyte" statement. A small set of deterministic payload sizes is enough for v1, but they should be named up front so the team does not validate only a trivial case and claim coverage. The important outcome is proof across a modest range, not a single happy-path blob.

3. Add operational telemetry acceptance items earlier than the final quality pass. For this provider, bytes read and written, compression ratio, list page count, blobs enumerated per maintenance operation, and duplicate-version conflict counts are part of performance supportability, not optional polish.

4. Clarify the role of `ListPageSizeHint` as a tuning bound rather than a correctness assumption. The plan names the option, but it should also say that page size reduces per-page memory pressure and request burst size, while not changing the underlying linear complexity of maintenance scans.

5. Add one focused proof that restart-safe readability does not regress performance by forcing unnecessary fallback work. Persisted serializer identity is the right correctness decision, but the plan should state that decode chooses the stored serializer directly and fails fast on unknown values rather than probing multiple serializers or retrying alternate decode paths.

## Could-Fix

1. Add a short performance acceptance table to the plan that names each operation, expected complexity, large-payload sensitivity, and required evidence artifact. That would make review materially faster.

2. Add a small note that exact-version reads are the preferred fast path and that maintenance operations are intentionally more expensive. That will help keep future code reviews from over-optimizing startup or initialization while missing the real hot paths.

3. Consider a lightweight non-benchmark measurement harness for scan behavior if the team wants repeatable evidence without introducing a formal benchmark project. For v1, deterministic test-time measurements or counted request and page assertions are enough.

## Won't-Fix For V1

1. Do not require manifest blobs, pointer blobs, blob tags, or secondary indexes just to avoid linear stream-local scans. The accepted v1 design is linear per stream, not indexed.

2. Do not require cloud-scale benchmarking or real-Azure throughput certification before shipping v1. The missing proof here is basic cost-shape and large-payload viability, not production-scale performance tuning.

3. Do not ask for micro-optimization on cold paths such as container initialization or registration plumbing. The meaningful performance work is in payload movement and scan behavior.

4. Do not expand v1 into adaptive compression thresholds, per-snapshot codec choice, or speculative caching. Those add complexity before the baseline pipeline has been proven acceptable.

## Overall Assessment

The current draft is close to the right performance posture, but it still describes intent more than proof. The right v1 bar is not peak throughput. It is simpler and more defensible: payload movement stays bounded, scans stay linear and stream-local, maintenance logic never reads candidate payload bodies, and the team produces concrete evidence that representative large snapshots do not trigger obvious memory amplification.

## CoV: Performance Verification

1. Hot path identification is correct: the architecture and draft plan place large-payload risk on encode, compress, upload, download, decompress, and decode, with additional cost concentration on latest-read, prune, and delete-all because those rely on per-stream enumeration.

2. Complexity analysis is accurate: exact-version write and exact-version read can be O(1) with deterministic blob naming, while latest-read, prune, and delete-all are O(n) over snapshots in a single stream because they require prefix listing. That is acceptable for v1 only if enumeration stays stream-local and body-free.

3. Allocation concerns are verified, not assumed: the technical perspective already identifies repeated byte copies as the dominant feasibility caveat for this feature, and the solution design explicitly notes that large payloads increase sensitivity to redundant copies and partial-write ambiguity. The draft plan acknowledges representative multi-megabyte testing, but it does not yet require evidence strong enough to prove that the new provider avoids repeating the Cosmos path's copy-heavy behavior.
