# QA Lead Review Cycle 01

## Quality Assessment

- Overall quality: Not Ready. The draft plan identifies the right general risk areas, but it does not yet convert the highest-risk behaviors into explicit quality gates, sequencing rules, or required merge evidence.
- Risk coverage: Approximately 60% of identified high-risk behaviors have named test intent, but only a minority have explicit pass or fail evidence defined for merge readiness.
- Test confidence: Medium-Low. The plan is directionally sound, but the current sequencing still allows high-cost correctness defects to surface after multiple increments have already been built on top of them.

## Coverage Gate Status

| Gate | Target | Actual | Status |
|------|--------|--------|--------|
| Changed code coverage | >=100% | Not defined as an explicit merge gate in the plan | Fail |
| Solution coverage | >=80% | Not defined as an explicit merge gate in the plan | Fail |
| Mutation score (Mississippi) | Maintained or raised | Mentioned only as a final activity, not as required evidence per risk area | Fail |
| Zero warnings | 0 | Mentioned only in final quality pass, not as an incremental gate | Fail |
| Deterministic tests | Required | Stated directionally, but no explicit review checkpoint or evidence list exists | Fail |

## Test Strategy Alignment

- L0 tests: The plan correctly makes L0 the primary layer, but it defers too much of the highest-risk verification to a late "unit test completion" increment instead of landing those tests with the persisted-format and stream-safety decisions they validate.
- L1 tests: The plan allows targeted L1 only when needed, which is correct, but it does not yet say which risks must be proven without Azurite, especially paged listing behavior, partial failure behavior, and restart-style decode using persisted bytes.
- L2 tests: The Crescent path is valuable, but the current plan risks turning L2 into a broad integration checklist rather than a focused proof of restart-safe, stream-safe, non-default-configuration behavior.
- Gap analysis: The largest gaps are explicit merge evidence for data-loss prevention, wrong-latest selection, corrupt-data rejection, large-payload confidence, and mutation-strength on the branches most likely to regress silently.

## Risk Areas

| Risk Area | Severity | Test Coverage | Verdict |
|-----------|----------|---------------|---------|
| Wrong latest snapshot selected across version-ordering boundaries | High | Named in plan, but not yet tied to an early merge gate | Gap |
| Delete-all or prune affecting another stream through prefix collision | High | Named in QA analysis, but not explicit in plan acceptance or exit criteria | Gap |
| Restart or configuration change making existing blobs unreadable | High | Mentioned, but not yet required as a non-negotiable acceptance proof | Gap |
| Large-payload scenario passing only for modest sizes | High | Mentioned, but lacks explicit evidence expectations and timing | Gap |
| Corrupt or incompatible blobs returning unsafe results | High | Mentioned, but missing a required failure-path matrix in the plan | Gap |
| Duplicate-version safety under conditional create semantics | High | Named, but not yet promoted to a contract-parity gate | Gap |
| Non-default serializer path receiving shallow coverage | Medium | Mentioned, but not made mandatory at both lower and higher test layers | Gap |
| Azure diagnostics and failure observability | Medium | Mentioned in architecture, not yet reflected as reviewable evidence in the plan | Gap |

## Must-Fix

1. Re-sequence the plan so the highest-risk tests land in the same increments as the behaviors they protect. Naming, latest-selection logic, stream scoping, frame compatibility, serializer identity persistence, checksum or corruption rejection, and duplicate-version semantics are not "test completion" concerns. They are executable quality gates for the most expensive failure modes and must be planned as part of the foundational increments.

2. Add an explicit risk-to-test matrix to the plan before implementation starts. The current plan lists good intentions, but it does not yet prove that each high-severity risk has a required test layer, a required scenario, and a defined merge artifact. At minimum, the plan should map wrong-latest selection, cross-stream deletion, restart safety, corrupt-data rejection, duplicate-version conflict handling, large-payload viability, and non-default serializer survival to concrete L0, L1, and L2 expectations.

3. Promote repository quality gates from a final clean-up step to explicit merge criteria. The plan must state that merge readiness requires zero warnings, changed-code coverage at the repository target, solution coverage at the repository target, maintained or raised mutation score, and deterministic tests. Leaving these as end-of-plan activities creates too much room for late discovery and scope negotiation.

4. Define the minimum evidence package required before merge. The plan currently says to prepare merge-readiness evidence, but it does not define what counts. Require evidence for: contract-parity results, coverage results, mutation results, restart or reload proof, cross-stream safety proof, large-payload proof using representative deterministic sizes, and failure-path proof for corrupt or incompatible blobs.

5. Make failure-path coverage a first-class planning concern rather than an implied extension of happy-path tests. The current plan is still too optimistic around negative behavior. The reviewable plan should explicitly require tests for unknown frame version, unknown serializer identity, unsupported compression metadata, checksum or payload corruption, incomplete listing or enumeration failure, and missing or conflicting version scenarios.

6. Require an explicit determinism statement for every non-L0 layer. The plan says Azurite is not proof of production-scale Azure behavior, which is correct, but it does not yet say how L1 and L2 will stay reliable. The revised plan should require deterministic payload sizes, deterministic dataset shapes, deterministic restart fixtures, and no timing-sensitive assertions as acceptance conditions for the test strategy itself.

## Should-Fix

1. Narrow the Crescent L2 increment to a smaller number of decisive proofs. The strongest initial L2 bar is provider registration, one default JSON path, one gzip path, one non-default serializer path, restart-safe reload, and one stream-safety maintenance scenario. A broader checklist risks breadth without enough semantic depth.

2. Add an explicit contract-parity checklist against the existing Cosmos-backed provider. The plan says behavior should match at the contract level, but that is still too interpretive. The revision should lock expected outcomes for read-missing, delete-missing, duplicate-version write attempts, latest-read selection, prune retention behavior, and delete-all scope.

3. Add a quality checkpoint for diagnostics and supportability evidence. The architecture calls out structured diagnostics for decode failures, duplicate-version conflicts, and storage-operation visibility, but the plan does not say whether any of that must be asserted. If operational diagnosability matters for this provider, the plan should require at least targeted verification that those signals exist for the highest-risk failure paths.

4. Require the non-default serializer path at more than one test layer. One integration scenario is not enough for a path that can break restart compatibility. The plan should require lower-layer proof plus one end-to-end proof so serializer identity remains a protected invariant instead of a lightly sampled option.

5. Decide whether concurrency expectations are part of the v1 quality bar or explicitly out of scope. The QA analysis identifies adjacent or near-simultaneous writes as a risk area, but the plan is silent on whether the contract has an observable expectation here. That ambiguity should be closed before implementation starts.

## Could-Fix

1. Add a simple evidence ledger to the plan that names the exact report or artifact expected from each gate. This would make review faster and reduce ambiguity about what constitutes completion.

2. Add a mutation-focused note that the highest-value assertions are the ones that distinguish latest from not-latest, target stream from shared prefix, valid metadata from invalid metadata, and supported format from unsupported format. That will help prevent a high-coverage but low-signal test suite.

3. Add a lightweight pre-merge checklist that confirms Azurite findings are being treated only as functional confidence and not as proof of cloud-scale throughput, memory stability, or Azure-specific retry parity.

## Won't-Fix For V1

1. Do not require real-Azure scale, throughput, or retry-parity validation as part of this plan review. The quality bar for v1 should focus on functional correctness, deterministic safety, and repository gates, not cloud-scale certification.

2. Do not expand the test plan into a generic multi-provider abstraction program. The QA concern is correctness of this provider against the existing contract, not redesign of the storage model.

3. Do not add L3 or long-running operational soak tests to the default PR quality bar. They may be useful later, but they are not the critical missing evidence in the current draft.

4. Do not treat optional Azure blob metadata duplication as required v1 correctness evidence unless the team first promotes it from operational convenience to explicit product behavior.

## Outstanding Concerns

- The current plan still leaves too much room for the team to claim progress based on implementation increments without proving the failure modes most likely to cause silent correctness defects.
- Large-payload validation is stated, but not yet bounded as a concrete quality gate with reviewable evidence.
- Restart-safe readability is central to the provider value proposition and should be promoted from a desirable scenario to a required merge proof.

## Overall Assessment

The draft is directionally correct, but it is still an implementation-led plan more than a quality-gated plan. Before work starts, the team should revise it so the highest-risk invariants are paired with early executable tests, each major risk has an explicit evidence requirement, and repository quality gates are treated as merge criteria rather than late cleanup tasks.

## CoV: Quality Verification

1. Coverage numbers from actual reports: not yet available. The gap is that the plan does not yet define which coverage artifacts will be required before merge.
2. Mutation scores from actual Stryker runs: not yet available. The gap is that mutation is listed as a final activity rather than being tied to the high-risk behaviors most likely to survive weak assertions.
3. Risk areas matched against requirements: partially verified. The QA perspective and architecture identify the right failure modes, but the draft plan does not yet convert all of them into required evidence.
4. Test determinism verified: not yet verified. The plan states the right intent, but lacks explicit determinism checkpoints for L1 and L2.
