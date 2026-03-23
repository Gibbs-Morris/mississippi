# Technical Lead Review - Cycle 2

## Verdict

Draft plan v2 closed most of the structural issues from cycle 1. The remaining gaps are narrower and mostly about turning the plan from a strong design summary into an executable delivery contract. The residual issues below are the items I would still ask the team to fix before treating the plan as implementation-ready.

## Must-Fix

| # | Remaining issue | Why it is still blocking | Required change |
|---|---|---|---|
| 1 | The contract-parity matrix still uses outcome labels such as "same contract-level outcome as Cosmos" instead of naming the exact observable result. | That wording is directionally right but still leaves room for incompatible interpretations during implementation and review. The plan needs explicit behavior for read-missing, delete-missing, duplicate-version write, corrupt blob read, and initialization failures. | Replace every parity item that currently relies on comparison wording with an explicit outcome statement such as `returns null`, `no-op success`, `throws duplicate-version conflict`, or `fails startup with actionable configuration error`. |
| 2 | The large-payload viability gate is still qualitative rather than measurable. | The core product promise is "materially larger than Cosmos-friendly payloads." The plan now asks for bounded buffering proof, but it still does not define the payload sizes, allocation budget, or pass-fail evidence needed to prove that claim. | Add a deterministic payload-size matrix, an explicit copy or buffering budget for write and read paths, and a named merge artifact for the evidence. The gate should make it obvious what would count as success or failure before implementation begins. |
| 3 | The serializer-resolution contract is still too implicit inside the draft plan itself. | Supporting documents define the right direction, but the implementation plan is still missing the exact startup rule that implementers and reviewers will hold the code to. That is risky because serializer identity is part of the stored compatibility contract. | In draft plan v2, state the exact rule: zero matches for the configured serializer format fails startup, multiple matches fail startup, and persisted serializer identity must be a concrete stable identifier rather than ambient DI order or a friendly label. |

## Should-Fix

| # | Remaining issue | Why it matters | Recommended change |
|---|---|---|---|
| 1 | The Crescent L2 trust slice is still carrying too many concerns in one mandatory scenario. | The slice is stronger than v1, but it still bundles registration, large payload, gzip, metadata inspection, restart, and a non-default path into one proof. That increases delivery risk and makes failures less diagnosable. | Define one mandatory L2 happy path around registration, write, restart, and read-back. Keep large-payload and non-default serializer or configuration assertions explicit, but mark any secondary maintenance or inspection extras as additive rather than part of the critical path. |
| 2 | The plan says there will be a canonical registration path, but it does not yet make that decision reviewable as an acceptance point. | Public setup shape is one of the main adoption risks. If the plan does not lock the canonical path early, implementation can drift into multiple equally promoted overloads and a muddier support story. | Add a planning checkpoint that names the single canonical registration path and explicitly classifies any alternate overloads as advanced or deferred. |
| 3 | The evidence package still under-specifies observability proof. | The plan now requires better diagnostics, but reviewers still do not know which failures must be proven via logs, telemetry, or exception assertions versus which ones are simply implementation details. | Add a small observability evidence matrix covering startup misconfiguration, duplicate-version conflicts, decode failures, and checksum failures, with the exact triage fields that must be present in proof artifacts. |
| 4 | The L2 wording allows a "non-default configuration or serializer path," which weakens the serializer trust story. | Serializer survival across restart is one of the highest-risk compatibility concerns. Allowing any non-default option to satisfy the L2 bar makes it easier to miss the most important case. | Make the non-default serializer path mandatory somewhere in the evidence package. If L2 cannot carry it, say so explicitly and require it at L0 with a documented reason. |

## Could-Fix

| # | Remaining issue | Why it is optional | Recommendation |
|---|---|---|---|
| 1 | Concurrency expectations for near-simultaneous writes are still not stated explicitly. | The duplicate-version rule is covered, so the most dangerous overwrite case is addressed. What remains is mostly clarification of adjacent-version or racing-write expectations. | Record whether concurrency semantics beyond duplicate-version conflict are part of the v1 contract or deliberately unspecified. |
| 2 | Optional Azure blob metadata duplication is still unresolved as a supportability choice. | The body header is correctly authoritative, so this is not a correctness blocker. It is only a question of operational convenience. | Decide only if the implementation can do it cheaply without creating a second source of truth; otherwise leave it out of v1. |

## Won't-Fix

| # | Deferred item | Rationale |
|---|---|---|
| 1 | Manifest, pointer, tag, or index-based lookup to avoid stream-local scans | v2 correctly treats stream-local linear scans as acceptable for v1, provided maintenance stays name-driven or header-light. |
| 2 | Mixed-provider orchestration or a new shared storage abstraction | The plan is now appropriately scoped to one Blob-backed provider behind the existing contract. |
| 3 | Real Azure scale certification as a release gate | The current confidence target is functional correctness plus bounded cost shape, not cloud-scale benchmarking. |
| 4 | Expanding compression or serializer features beyond `Off` or `Gzip` and explicit serializer selection | The plan is strongest when it stays narrow and predictable for the first release. |

## Overall Assessment

The plan is close. The remaining must-fix work is not about changing the architecture. It is about making three critical promises unambiguous before coding starts:

1. Exact observable contract outcomes.
2. Measurable proof for the large-payload promise.
3. Explicit serializer-resolution and persisted-identity rules.

If those are tightened, the rest of the remaining items are delivery-quality improvements rather than blockers.

## CoV

1. Verified against draft plan v2: the plan now includes parity, risk, evidence, and scoped increments, so this review only calls out what remains ambiguous rather than reopening resolved issues.
2. Verified against the solution design: the remaining must-fix items are all places where the architecture is stronger than the plan and should be pulled into the plan as explicit execution guidance.
3. Verified against review-cycle-01 synthesis: earlier blockers around sequencing, layering scope, and evidence structure were materially improved in v2 and are therefore not repeated here unless ambiguity still remains.
