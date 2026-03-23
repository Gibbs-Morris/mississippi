# Technical Lead Review Cycle 01

## Must-Fix

1. Move foundational L0 tests forward into the same increments as naming, framing, and repository behavior instead of deferring them to Increment 4. The blob name contract, frame prelude, header rules, checksum handling, and duplicate-write semantics are the highest-risk persisted behaviors. Treating them as a later test-completion phase invites late rework after multiple layers depend on the wrong format or failure behavior.

2. Add an explicit memory-behavior design gate before full repository implementation. The feature exists to support materially larger payloads, but the current plan does not require any early validation that the Blob path avoids the Cosmos provider's copy-heavy behavior. Before Increment 3 is considered complete, the plan should require a concrete allocation strategy for encode, compress, upload, download, decompress, and decode, plus representative multi-megabyte tests that prove the design is viable.

3. Pull serializer resolution and persisted serializer identity decisions earlier. The plan currently treats serializer integration as part of Increment 3, but `payloadSerializerId`, JSON defaulting, and ambiguous multi-provider resolution are part of the persisted format contract, startup validation, and decode failure model. If this stays late, the team risks building the frame and repository flow around an underspecified identity model and then changing stored-header semantics afterward.

## Should-Fix

1. Add a contract-parity checklist against the existing Cosmos provider before implementation starts. The plan says the Blob provider should preserve contract-level behavior, but it does not define the concrete parity set. At minimum, lock down expected behavior for read-missing, delete-missing, duplicate-version writes, prune scope, delete-all scope, and the meaning of `Format` so the team is not relying on informal similarity.

2. Tighten the meaning of "mirror the Cosmos provider" so it applies to public adoption shape, not to internal implementation mechanics. Reusing options, DI shape, hosted initialization, and logging conventions is correct. Reproducing Cosmos-specific mapper layering or allocation patterns would be a mistake for the larger-payload use case. The plan should say that public parity is required, but Blob internals should remain blob-native and allocation-aware.

3. Add explicit operational acceptance items for Azure-specific semantics. The architecture already identified that prefix listing is a stream-local linear scan and that account features such as soft delete or blob versioning change physical delete behavior. The plan should carry those constraints forward so documentation, tests, and PR validation do not accidentally overpromise maintenance cost or purge behavior.

4. Narrow the Crescent L2 scope to one valuable vertical slice with clear exit criteria. Right now the plan lists registration, non-default configuration, compression, metadata visibility, restart, reload compatibility, and maintenance behavior in one increment. That is directionally right but operationally loose. The first L2 bar should be: real provider registration, write and read through Mississippi, restart-safe reload, and one non-default configuration path. Maintenance behavior can stay in scope only if it does not delay landing the core proof.

## Could-Fix

1. Add a short pre-implementation checkpoint that confirms project placement, package dependencies, and keyed client registration match existing repository conventions before the team starts coding. This is probably straightforward, but an early check keeps the new provider from drifting into the wrong layer or taking an unnecessary dependency on a new abstractions package.

2. Add an explicit observability checkpoint in the plan, not just in the architecture. For a new storage provider, logging and metrics are part of supportability, especially around duplicate-version conflicts, checksum failures, decode failures, list costs, and large-payload write or read latencies.

3. Consider duplicating a minimal non-authoritative metadata subset to blob metadata for operations visibility if the team decides supportability needs it. This is not required for correctness, but deciding it during implementation is cheaper than retrofitting it after early adopter feedback.

## Won't-Fix For V1

1. Do not broaden this plan into a generic multi-provider storage abstraction exercise. A second provider does not justify a cross-provider redesign yet.

2. Do not add manifest blobs, pointer blobs, blob tags, or index-based latest lookup in this delivery. The linear stream-local listing tradeoff is already understood and accepted for v1.

3. Do not introduce per-snapshot compression heuristics, automatic thresholds, or codec proliferation. Provider-wide `Off` and `Gzip` is the right v1 boundary.

4. Do not let Azurite L2 become a proxy for production-scale Azure validation. It is a functional confidence tool, not proof of throughput, retry behavior, or exact cloud error shapes.

## Overall Assessment

The plan is feasible, but the sequencing is too optimistic around the persisted-format contract and the large-payload risk that justified the feature in the first place. If the team front-loads executable format tests, allocation discipline, and serializer-identity decisions, the rest of the delivery can stay within the existing Cosmos-shaped provider model without accumulating avoidable rework.

## CoV Notes

1. The must-fix points are grounded in the existing solution design and expert reviews, which already identify persisted-frame correctness, serializer identity, conditional writes, and memory amplification as the dominant technical risks.

2. The architecture-alignment feedback is grounded in the live Cosmos provider shape: public registration, keyed client resolution, hosted initialization, and a thin provider plus repository split are existing patterns worth preserving.

3. The scope-control feedback is grounded in the confirmed product direction that Blob is a v1 alternative provider for larger payloads, not a broader storage-abstraction redesign.
