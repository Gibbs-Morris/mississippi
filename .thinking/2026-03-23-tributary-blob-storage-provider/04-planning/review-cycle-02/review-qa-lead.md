# QA Validation Report

## Quality Assessment

- Overall quality: Not Ready. Draft plan v2 closed most of the cycle 1 structural gaps, but a few high-risk items are still too ambiguous to treat the plan as execution-ready.
- Risk coverage: About 85% of identified plan-level risks are now covered with explicit intent, but some of the highest-risk behaviors still lack precise pass-fail definitions.
- Test confidence: Medium. The plan has a much stronger L0-first posture, but the remaining gaps are concentrated in exact observable outcomes, measurable large-payload proof, and mandatory evidence for restart-safe serializer behavior.

## Remaining Must-Fix

| # | Remaining issue | Why it is still blocking | Required change |
|---|---|---|---|
| 1 | The contract-parity matrix still leaves key observable outcomes implicit. | QA cannot sign off a test strategy against phrases such as "same contract-level outcome as Cosmos" because they do not lock the expected result for not-found, delete-missing, duplicate-version write, corrupt read, or initialization failure paths. | Replace comparison wording with exact observable outcomes for every parity item so tests can assert one unambiguous behavior. |
| 2 | The large-payload viability proof is still not measurable enough to act as a release gate. | The product promise is larger-payload support. Without explicit payload sizes, buffering or copy expectations, and named evidence artifacts, the team could produce a passing implementation that still does not prove the motivating scenario. | Add a deterministic payload-size matrix, explicit buffering or copy-budget expectations for write and read paths, and a named evidence artifact that determines pass or fail before implementation begins. |
| 3 | The plan still does not lock the exact startup and read-failure rules for serializer identity strongly enough. | Persisted serializer identity is part of the stored compatibility contract. If startup validation and read failure semantics remain partially implicit, restart safety can regress even if happy-path tests pass. | State directly in the plan that zero serializer matches fail startup, multiple matches fail startup, and unknown persisted serializer identities fail read deterministically with an actionable error. |
| 4 | The evidence package does not yet make the non-default serializer restart path mandatory enough. | This is one of the highest-risk compatibility paths and one of the easiest to accidentally under-test if a broader "non-default configuration or serializer path" can satisfy the gate. | Require the non-default serializer restart-survival scenario explicitly in the merge evidence package, not as an interchangeable optional path. |

## Remaining Should-Fix

| # | Remaining issue | Why it matters | Recommended change |
|---|---|---|---|
| 1 | The Crescent L2 trust slice still bundles too many assertions into one mandatory scenario. | Registration, large payload, gzip, metadata inspection, restart, and non-default-path proof in one scenario will make failures harder to diagnose and can slow delivery. | Define one mandatory L2 trust slice around registration, write, restart, and read-back, then classify large-payload and metadata-inspection extras as additive unless they are required elsewhere in the evidence package. |
| 2 | Observability expectations are present, but the proof requirements are still under-specified. | QA needs to know which failures must be proven through exception assertions, diagnostics, or both. Otherwise supportability can become subjective during review. | Add a small observability evidence matrix for startup misconfiguration, duplicate-version conflict, decode failure, checksum failure, and unreadable blob cases, including the triage fields that must be present. |
| 3 | Concurrency expectations remain unstated. | The duplicate-version overwrite case is covered, but adjacent-version or near-simultaneous write behavior is still ambiguous enough to create mismatched assumptions across implementation and tests. | Explicitly state whether concurrency semantics beyond duplicate-version conflict are part of the v1 contract or deliberately unspecified. |
| 4 | The plan does not yet clearly say where the strongest proof for maintenance safety must live. | L0 coverage for cross-stream delete and prune safety is good, but the current wording still leaves room for debate about whether any real Blob-backed maintenance proof is expected before release. | State that stream-safety for delete-all and prune is mandatory at L0, and say explicitly whether L2 maintenance proof is additive or required. |

## Remaining Could-Fix

| # | Remaining issue | Why it is optional | Recommendation |
|---|---|---|---|
| 1 | Inspectability of stored metadata versus Azure blob metadata duplication is still not fully closed. | The draft is correct to keep the blob body as the source of truth, so this is now a supportability decision rather than a correctness blocker. | Decide only if the team can add optional metadata duplication cheaply without creating a second source of truth; otherwise leave it out of v1. |
| 2 | The plan could make deterministic failure-injection expectations more explicit for pre-L2 tests. | The risk matrix implies this direction already, so the gap is about sharpening execution guidance rather than changing scope. | Note that corruption, truncated gzip, unknown version, and unknown compression proofs should be done with direct deterministic fixtures rather than environment-dependent fault reproduction. |

## Remaining Won't-Fix

| # | Deferred item | Rationale |
|---|---|---|
| 1 | Real-Azure scale or throughput benchmarking as a release gate | The correct v1 quality bar is functional correctness plus bounded cost shape, not production-scale certification. |
| 2 | Manifest, pointer, or index-based lookup to avoid stream-local scans | The plan now correctly accepts stream-local linear scans in v1 as long as maintenance paths stay name-driven or header-light. |
| 3 | Mixed-provider orchestration or a new shared abstraction layer | The revised plan is appropriately scoped to one Blob-backed provider behind the existing Tributary contract. |
| 4 | Compression and serializer feature expansion beyond the narrow v1 boundary | Keeping the first release explicit and predictable is the better quality choice than expanding codecs or fallback behavior. |

## Outstanding Concerns

- The plan is close, but QA still cannot convert all high-risk behaviors into unambiguous acceptance tests without more precise contract wording.
- The largest remaining risk is a false sense of confidence on the motivating large-payload scenario because the gate is still more descriptive than measurable.
- The second largest remaining risk is restart compatibility drift if non-default serializer survival is not elevated into a mandatory proof item.

## CoV: Quality Verification

1. Verified against the revised plan: most cycle 1 issues were materially addressed, so this review only retains residual gaps that still affect risk coverage or testability.
2. Verified against the prior synthesis: earlier blockers around sequencing, scope boundaries, and risk framing are no longer raised because draft plan v2 substantially improved them.
3. Verified against the QA perspective: the remaining blockers are concentrated in exact observable outcomes, deterministic large-payload evidence, restart-safe serializer proof, and explicit supportability assertions.
4. Verified for determinism: the plan is directionally strong on deterministic testing, but it should sharpen failure-proof expectations so corrupt and incompatible blob cases are demonstrably testable without environment-sensitive setup.
