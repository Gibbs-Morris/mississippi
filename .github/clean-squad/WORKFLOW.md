# Clean Squad: End-to-End SDLC Workflow

The Clean Squad is a family of 39 GitHub Copilot agents that takes an idea from
initial request through to a merge-ready pull request. It offers two public
intake paths: **cs Entrepreneur** for optional pre-governed idea shaping and
**cs River Orchestrator** for direct governed intake. Once governed work begins,
**cs River Orchestrator** is the sole orchestrator who delegates to specialist
sub-agents and records authoritative workflow state. Every agent applies first-principles thinking and
chain-of-verification to every task.

## Foundational Principles (Embedded in Every Agent)

### Mandatory for All Agents (No Exceptions)

- **First Principles Thinking**: decompose problems to irreducible truths; never
  accept convention without examination; always ask why the question is being
  asked before solving it.
- **Chain of Verification (CoVe)**: every non-trivial claim follows the
  four-step process — draft, plan verification questions, answer independently,
  revise. Evidence over assumption.

### Applied Where Relevant

- **Clean Code**: meaningful names, small functions, single responsibility,
  DRY, command-query separation.
- **Clean Agile**: scope is the variable; continuous testing; technical
  disciplines are non-negotiable; sustainable pace.
- **Minto Pyramid**: answer first, then supporting arguments; SCQA framing
  for all significant communications.
- **Pull Request Best Practices**: single-responsibility PRs, self-review
  before submission, commit hygiene.
- **Architecture Decision Records**: immutable records of significant decisions
  with context, rationale, and consequences.
- **Mermaid Diagrams**: diagrams as code, version-controlled, diffable.
- **RFC 2119**: MUST, SHOULD, MAY for unambiguous obligations.

## Team Name

**Clean Squad** — reflecting Clean Agile and Clean Code principles.

## Entry Points

`@cs Entrepreneur` is an optional public-facing pre-governed intake path for
rough ideas that need shaping before governed work begins. It normally
produces one Story Pack candidate and MAY instead return an explicit stop
outcome (`CHANGES_REQUESTED`, `DEFERRED`, or `CANCELLED`) before G0. In all
cases it **MUST NOT** create governed workflow state or advance governed work.

`@cs River Orchestrator` is the direct governed intake path and the sole governed
orchestrator. `cs River Orchestrator` accepts either direct free-form intake or a
G0-approved Story Pack candidate, defaults to governed end-to-end execution, and
asks one fixed qualification round only when the intake appears partial or
ambiguous before discovery continues.

All governed Clean Squad delegation **MUST** target only approved Clean Squad
agents named in the `Agent Roster` section of this workflow. The user never
needs to invoke any other agent directly unless they intentionally choose the
optional Entrepreneur intake path before governed work starts.

If no approved Clean Squad agent fits a task, `cs River Orchestrator` **MUST** stop,
record the blocker, and ask the user to either choose the nearest approved
Clean Squad agent, approve a roster or workflow change first, or explicitly
leave Clean Squad orchestration for that task.

## Intake and Human Advancement Gates

### Intake Modes

- Direct governed intake: the human starts with **cs River Orchestrator** when the
  problem, intended value, and direction are already clear enough to begin
  governed discovery.
- Optional pre-governed shaping: the human starts with **cs Entrepreneur** when
  the idea is still fuzzy and needs pressure-testing before governed intake.

### Governed Intake Defaults and Qualification Contract

Canonical vocabulary for governed intake:

| Term | Meaning |
|------|---------|
| `execution scope` | Whether River treats the intake as `full-run`, `bounded-objective`, or `non-governed`. |
| `discovery mode` | Whether discovery proceeds as `autonomous-defaults` or `manual-refinement`. |
| `qualification round` | One fixed question-UI batch that captures execution scope and discovery mode together. |

Qualification rules:

- `cs River Orchestrator` **MUST** classify governed intake before Phase 1 discovery work proceeds.
- When intake is clear and governed end-to-end execution is obviously intended,
  River **MUST** skip the qualification round and default to `execution scope = full-run`
  plus `discovery mode = autonomous-defaults`.
- When intake appears partial, ambiguous, or otherwise unclear about whether the
  governed workflow should run end to end, River **MUST** ask exactly one
  qualification round through the question UI.
- That qualification round **MUST** capture exactly two decisions: execution scope
  and discovery mode.
- A G0-approved Story Pack candidate **MUST** follow the same rule: default to
  `full-run` plus `autonomous-defaults` unless the accompanying human instruction
  narrows the objective or leaves the downstream scope ambiguous.

Deterministic intake decision table:

| Intake pattern | Qualification round? | Execution scope result | Discovery mode default | Gate behavior |
|------|------|------|------|------|
| Clear end-to-end ask such as “implement this feature” or “run this governed task” | No | `full-run` | `autonomous-defaults` | G1/G2/G3 remain explicit |
| Ambiguous ask such as “handle this plan” or “do the docs” with unclear scope | Yes | human-selected | human-selected | G1/G2/G3 remain explicit |
| Human confirms a bounded governed slice | Yes | `bounded-objective` | human-selected | The applicable governed workflow still runs to completion for that bounded objective; G1/G2/G3 remain explicit whenever their artifact packages still apply |
| Resume or retry on a task folder that already records qualification or discovery facts | No new round by default | reuse recorded scope | reuse recorded mode | Resume from canonical facts |
| Advice-only, authority-widening, or non-governed ask | No governed progression | `non-governed` | none | River blocks or redirects per workflow rules |

### Autonomous Discovery Defaults Contract

- `cs River Orchestrator` **MUST NOT** generate autonomous discovery defaults directly.
  River remains the orchestrator and canonical writer.
- `cs Requirements Analyst` is the bounded specialist that **MUST** generate
  autonomous discovery defaults when discovery mode is `autonomous-defaults`.
- Autonomous discovery **MUST** reuse `01-discovery/questions-round-NN.md` rather
  than inventing a new artifact family.
- Each inferred answer in an autonomous discovery batch **MUST** include trust tier,
  source category, evidence reference(s), confidence, and
  `requiresHumanConfirmation`.
- Autonomous discovery precedence **MUST** be: confirmed human intent → approved
  governed artifacts → authoritative repo contract surfaces → existing repo
  patterns → framework defaults → explicit assumptions.
- Autonomous discovery **MUST** fail closed and require later human confirmation
  for low-confidence or conflicting evidence, security-sensitive or destructive
  choices, public API or contract changes, authority-widening requests, or
  unresolved high-impact domain ambiguity.
- Autonomous discovery **MUST** stay bounded to a hard cap of three rounds or
  fifteen inferred questions unless a future workflow revision explicitly narrows
  that bound further.
- When high-impact ambiguity remains after the autonomous bound, River and its
  delegated specialist **MUST** promote that ambiguity to explicit assumptions or
  open questions rather than generating additional autonomous discovery rounds.
- `01-discovery/requirements-synthesis.md` **MUST** keep inferred defaults separate
  from confirmed requirements until qualification or G1 later accepts them.

### Canonical Qualification, Resume, and Cutover Rules

- When River asks the qualification round, it **MUST** record the human wait
  boundaries canonically before and after the response.
- Qualification outcome **MUST** be recorded canonically as a bounded `completed`
  event whose `workItemId` and `rootWorkItemId` are `intake.qualification`, whose
  `details.executionScope` and `details.discoveryMode` capture the selected mode,
  and whose `details.qualificationReason` explains why the qualification round was
  needed.
- When River skips the qualification round because clear governed intent exists,
  it **MUST** still record the defaulted `execution scope` and `discovery mode`
  canonically before discovery proceeds.
- Autonomous discovery publication **MUST** be recorded canonically and **MUST**
  bind the generated `questions-round-NN.md` artifact(s).
- Pre-change task folders whose workflow fingerprint predates this contract and do
  not contain qualification or discovery-mode facts **MUST** preserve legacy
  manual-discovery semantics on resume.
- When a resumed task folder already contains qualification and discovery-mode
  facts, River **MUST** reuse them and **MUST NOT** ask a second qualification
  round unless the human later changes the scope explicitly.

### Story Pack Contract

When **cs Entrepreneur** is used and the idea is ready, it produces exactly one
Story Pack candidate for G0 human approval before River Orchestrator intake. The
Story Pack candidate **MUST** include:

- `storyPackId`
- idea title
- problem statement
- target user or beneficiary
- business value
- proposed capability
- assumptions
- open questions
- scope boundaries
- recommended downstream emphasis
- INVEST assessment
- agile story statement

When **cs Entrepreneur** determines that the idea is not ready for governed
intake, it MAY instead return one explicit stop outcome using the same closed
decision vocabulary as the human advancement gates except `APPROVED`:

- `CHANGES_REQUESTED`
- `DEFERRED`
- `CANCELLED`

Only a Story Pack candidate proceeds to G0. A stop outcome ends or pauses the
pre-governed intake path without creating governed workflow state.

### Human Advancement Gates

Generated artifacts are proposed working drafts until the responsible human
explicitly approves the bound artifact package. Every gate uses the same closed
decision set: `APPROVED`, `CHANGES_REQUESTED`, `DEFERRED`, or `CANCELLED`.
Only `APPROVED` authorizes progression.

#### G0 Intake Gate

Applies only when **cs Entrepreneur** is used and returns a Story Pack
candidate.

Bound artifact package:

- Story Pack candidate

Approved next step:

- **cs River Orchestrator** may create governed workflow state and start discovery.

#### G1 Scope Gate

Applies after discovery and Three Amigos synthesis.

Bound artifact package:

- `01-discovery/requirements-synthesis.md`
- `02-three-amigos/synthesis.md`

Approved next step:

- architecture and planning may proceed.

#### G2 Plan Gate

Applies before implementation starts.

Bound artifact package:

- `03-architecture/solution-design.md`
- binding C4 artifacts from `03-architecture/`
- binding ADR artifacts from `docs/Docusaurus/docs/adr/`
- `04-planning/final-plan.md`

Approved next step:

- implementation and downstream specialist work may proceed.

#### G3 Merge Gate

Applies after late-stage delivery evidence has been assembled.

Bound artifact package:

- `09-pr-merge/merge-readiness.md`
- current code-review conclusion and remediation status
- current QA conclusion
- current documentation conclusion

Approved next step:

- PR-ready and merge-ready progression may continue.

### Stale Approval Rule

If any artifact bound to an approved gate changes materially, that approval
becomes stale and **MUST** be replaced with a fresh explicit decision before the
workflow advances.

### Canonical Gate Recording

Every G1-G3 human gate decision **MUST** be recorded canonically in
`workflow-audit/` using the existing event contract:

- G0 is the pre-governed exception. Because it happens before governed intake
  begins, G0 **MUST NOT** require prior existence of `.thinking/<task>/` or
  `workflow-audit/`.
- When governed work starts from a G0-approved Story Pack candidate, `cs River Orchestrator`
  **MUST** carry the G0 approval evidence into governed state by
  capturing the approved Story Pack candidate and the human G0 approval in
  `00-intake.md` and by binding that evidence in the initial governed canonical
  event's `artifacts` or `provenance.evidence`.

- The responsible human reply **MUST** first be captured by a `wait-ended`
  event whose `details.waitKind` is `human` and whose provenance is
  `human-input`.
- The gate decision itself **MUST** then be recorded as a bounded `completed`
  event caused by that human-input `wait-ended` event.
- The gate-decision `completed` event **MUST** use a bounded `workItemId` and
  `rootWorkItemId` of `gate.g1-review`, `gate.g2-review`, or `gate.g3-review`
  as applicable.
- The gate-decision `completed` event **MUST** include `details.gateId` and
  `details.decision`, and **MUST** also include `details.gateName` and
  `details.decidedBy`. `details.notes` MAY be included when the responsible
  human supplies rationale. `gateId` is one of `G1`, `G2`, or `G3`; `decision`
  is one of `APPROVED`, `CHANGES_REQUESTED`, `DEFERRED`, or `CANCELLED`;
  `gateName` is the human-readable gate label; and `decidedBy` identifies the
  responsible human decider.
- The gate-decision `completed` event **MUST** bind the exact reviewed artifact
  package in `artifacts`, using at least one artifact with role
  `gate-package` plus `digestSha256` for local files or `immutableId` plus
  `uri` for immutable external resources.
- Gate decisions **MUST** map to canonical `outcome` values as follows:
  - `APPROVED` -> `succeeded`
  - `CHANGES_REQUESTED` -> `failed`
  - `DEFERRED` -> `cancelled`
  - `CANCELLED` -> `cancelled`
- When a gate decision records `CHANGES_REQUESTED`, `DEFERRED`, or
  `CANCELLED`, `cs River Orchestrator` **MUST** record any resulting deviation,
  stop, or resume semantics with the later canonical event that actually
  changes workflow execution state rather than overloading the gate-decision
  event itself.

## Shared State: The `.thinking` Folder

All governed Clean Squad agents share state through a filesystem folder once
governed work begins:

```text
.thinking/
  <YYYY-MM-DD>-<task-slug>/        # One subfolder per task
    state.json                      # Workflow state (current phase, status)
    workflow-audit/                 # Canonical append-only execution ledger
      meta.json                     # Immutable run header
      0000001.json                  # Immutable canonical event file
      0000002.json                  # Immutable canonical event file
    workflow-audit.md               # Derived detailed workflow audit report
    activity-log.md                 # Start/progress/blocker/completion log
    00-intake.md                    # Initial request & context
    01-discovery/
      questions-round-01.md         # Discovery batch (manual answers or autonomous defaults with provenance)
      questions-round-02.md         # Next discovery batch (adaptive manual follow-up or bounded autonomous continuation)
      ...
      requirements-synthesis.md     # Synthesized requirements
    02-three-amigos/
      business-perspective.md       # Business Analyst output
      technical-perspective.md      # Tech Lead output
      qa-perspective.md             # QA Analyst output
      adoption-perspective.md       # Developer Evangelist output
      synthesis.md                  # Unified requirements
    03-architecture/
      solution-design.md            # Solution Architecture
      c4-context.md                 # C4 Context diagram
      c4-container.md               # C4 Container diagram
      c4-component.md               # C4 Component diagram (if needed)
      c4-component-omitted.md       # Explicit rationale when a Level 3 view is not needed
      adr-notes.md                  # ADR candidate notes and rationale drafts
    04-planning/
      draft-plan-v1.md              # Initial plan
      review-cycle-01/
        review-<persona>.md         # Each reviewer's feedback
        synthesis.md                # Synthesized feedback
      review-cycle-02/              # Second review cycle
        ...
      final-plan.md                 # Finalized plan
    05-implementation/
      increment-01/
        changes.md                  # What was implemented
        commit-review.md            # Commit-level review results
        test-results.md             # Test execution results
      increment-02/
        ...
    06-code-review/
      review-pedantic.md            # Pedantic reviewer output
      review-strategic.md           # Strategic reviewer output
      review-security.md            # Security reviewer output
      review-dx.md                  # DX reviewer output
      review-performance.md         # Performance reviewer output
      domain-experts/
        review-<domain>.md          # Domain expert reviews
      synthesis.md                  # Synthesized review findings
      remediation-log.md            # Fix tracking
    07-qa/
      test-strategy-review.md       # QA Lead review
      exploratory-findings.md       # Exploratory testing
      coverage-report.md            # Coverage analysis
      mutation-report.md            # Mutation testing results
      qa-readiness.md               # Unified QA readiness conclusion
    08-documentation/
      scope-assessment.md           # Branch diff analysis for doc needs
      page-plan.md                  # Planned pages with types and paths
      evidence-map.md               # Evidence map for technical claims
      drafts/
        <page-name>.md              # Draft pages before publishing
      review-cycle-01/
        doc-review.md               # Doc Reviewer findings
        doc-story-review.md         # Developer Evangelist story-value findings
        remediation-log.md          # Fix tracking
      review-cycle-02/              # Second review cycle (if needed)
        ...
      publication-report.md         # Final pages published with verification
    09-pr-merge/
      pr-description.md             # PR description draft with Reviewer Audit Summary
      thread-log.md                 # Review thread tracking
      merge-readiness.md            # Merge readiness checklist
      polling-log.md                # Review polling rule log
    handover-log.md                 # All agent handovers
    decision-log.md                 # All decisions with reasoning
```

`cs Entrepreneur` is explicitly pre-governed and MUST NOT create or use this
shared state.

Published ADRs live outside `.thinking/` under `docs/Docusaurus/docs/adr/` so
they remain part of the long-term documentation set after the task folder is
retired.

### Operational Logging Protocol

- `cs River Orchestrator` MUST write every entry to `.thinking/<task>/activity-log.md`.
- Governed specialists MUST NOT append to `activity-log.md` directly.
- Every governed specialist MUST return a structured, metadata-sized status envelope that gives `cs River Orchestrator` enough detail to log start, progress, blocker, completion, artifacts updated, and next action; substantive delegated outputs MUST already be persisted to the declared artifact path or bundle path.
- `activity-log.md` remains mandatory operational telemetry, not an optional summary.
- Activity log entries SHOULD use a consistent structure: UTC timestamp, actor, phase, action, artifacts updated, blockers, and next action.

### Workflow Audit Contract

#### Canonical Artifacts and Authority

- `.thinking/<task>/workflow-audit/` is the authoritative execution record for one Clean Squad run.
- `.thinking/<task>/workflow-audit/meta.json` stores immutable run metadata for that ledger.
- `.thinking/<task>/workflow-audit/0000001.json`, `0000002.json`, and later seven-digit sequence files each store one immutable canonical event.
- `.thinking/<task>/workflow-audit.md` is a derived detailed audit compiled from a stable ledger snapshot.
- `09-pr-merge/pr-description.md` contains the derived `Reviewer Audit Summary` and MUST source it from current policy-authoritative audit inputs with matching freshness and evidence bindings.
- `sequence` is the only ordering authority for canonical workflow facts.
- Canonical `eventUtc` timestamps are mandatory for timing and diagnostics, but MUST NOT override `sequence` for chronology.
- `activity-log.md`, `handover-log.md`, Mermaid output, PR prose, and other narrative files are supporting or derived evidence only and MUST NOT override canonical sequence facts.
- Within this repository today, `workflow-audit/` and its derived audit artifacts are policy-authoritative and freshness-verifiable, but they are NOT tamper-resistant, cryptographically authenticated, or proof of actor identity beyond the declared Clean Squad role recorded in the ledger.

#### Active Writer and Delegation Invariants

- `cs River Orchestrator` writes canonical events for Phases 1 through 9.
- The PR Manager MUST NOT write canonical workflow facts and MAY execute only explicitly delegated, bounded Phase 9 specialist work.
- The Scribe MUST NOT write canonical workflow facts.
- Only one canonical writer may be active for the workflow run at a time.
- Every active Phase 9 PR Manager execution slice MUST begin with explicit `cs River Orchestrator` delegation whose `workItemId` names the bounded task slice and whose `details` name `details.expectedOutputPath` (the expected artifact output or artifact bundle), `details.completionSignal`, `details.closureCondition`, `details.allowedActions`, and `details.authorizedTargets`.
- `details.expectedOutputPath` MUST name either one fresh output path or one fresh bundle directory under `.thinking/<task>/` unless the bounded delegation explicitly authorizes a different target.
- Delegated specialists MUST write substantive outputs only to the declared path or bundle and MUST return only a concise summary, a metadata-sized status envelope, artifact paths, and blocker or next-action metadata.
- If a delegated artifact changes materially, the next pass MUST publish a new path instead of silently overwriting the previously handed-back delegated artifact in place.
- Before recording delegated completion canonically, `cs River Orchestrator` MUST verify that every returned artifact path exists and either equals the declared single expected path or is contained within the declared bundle directory, unless the bounded delegation explicitly authorizes a different target.
- Stale-marker authority in Phase 9 MUST remain continuously delegated whenever a fresh `Reviewer Audit Summary` is published or a review-polling wait is active; that bounded stale-marker delegation MUST stay active until `cs River Orchestrator` canonically records that the summary is stale, republished fresh, or no longer present on the PR surface.
- A Phase 9 delegation remains active only until `cs River Orchestrator` records a later canonical event for the same `workItemId` whose `causedBy.logicalEventId` references that delegation and whose semantics satisfy its declared `details.completionSignal` or `details.closureCondition`.
- Blocked Phase 9 startup, tool acquisition, or recovery MUST NOT transfer canonical ownership away from `cs River Orchestrator`.

- A materially new PR-surface objective in Phase 9 MUST use a new bounded delegation; Phase 9 delegation MUST NOT become umbrella authority.
- Every canonical append MUST declare the expected prior `sequence`.
- A canonical writer MUST fail closed if the ledger tail does not match the declared expected prior `sequence`.
- The first canonical append MUST write `sequence = 1` and declare expected prior `sequence = 0`.
- Recovery MUST rebuild operational state from `workflow-audit/`, never the reverse.

#### Canonical Event Contract

Canonical events MUST be append-only.

Reviewer-significant meaning MUST live in the structured canonical event envelope. Writers and validators MUST NOT infer direct cause, exact closure, terminal outcome, artifact lineage, or provenance from sequence order, `summary` prose, supporting logs, or path changes alone.

Every canonical event MUST include:

1. monotonic `sequence`
2. canonical UTC `eventUtc`
3. stable `logicalEventId` for retry safety
4. `actor`
5. `phase`
6. `eventType`
7. `appendPrecondition` with the expected prior `sequence`
8. human-readable `summary`
9. `reasonCode` when required
10. `artifacts` when applicable as evidence bindings
11. `artifactTransitions` when artifact lifecycle meaning is asserted
12. `iterationId` for loops, retries, or repeated review cycles when applicable
13. `provenance` for every meaningful event defined by this contract
14. `details` using `{}` when no event-type-specific members apply

Meaningful events MUST additionally carry `workItemId`, `rootWorkItemId`, `spanId`, `causedBy`, `closes`, and `outcome` whenever the writer-obligation matrix below marks them as required.

#### Normative Canonical Ledger File Contract

`workflow-audit/meta.json` MUST use this top-level shape:

```json
{
  "schemaVersion": "clean-squad-workflow-audit/v4",
  "workflowContractFingerprint": "<sha256 of UTF-8 bytes of .github/clean-squad/WORKFLOW.md>",
  "ledgerDigestAlgorithm": "sha256-canonical-ledger-files-v1",
  "sequenceFilePattern": "0000001.json (fixed-width seven digits)",
  "createdUtc": "<ISO-8601 UTC>"
}
```

Canonical JSON rules:

- `workflow-audit/` MUST be flat in this schema version; only `meta.json` and seven-digit zero-padded numeric event files ending in `.json` are permitted.
- `meta.json` and every canonical event file MUST be UTF-8 encoded JSON with no comments or trailing commas.
- Canonical JSON files MUST NOT include a UTF-8 BOM.
- Canonical JSON MUST NOT contain insignificant whitespace outside JSON string values; emit `meta.json` and each canonical event file in minified form with no spaces, tabs, or line breaks between tokens.
- Canonical JSON files MUST NOT end with a trailing newline; digest computation uses their exact UTF-8 byte sequences.
- `meta.json` property order MUST match the order shown in this contract wherever a digest is calculated.
- Event-file names MUST be derived solely from the next trusted integer `sequence` and MUST use fixed-width seven-digit zero-padded names such as `0000001.json`.
- Event-file property order MUST match the order shown in this contract wherever a digest is calculated.
- Nested object property order MUST match the order shown in this contract for `appendPrecondition`, `causedBy`, `closes`, `artifacts[]`, `artifactTransitions[]`, and `provenance`.
- Arrays MUST preserve the semantic order required by this contract, and nested arrays keep the writer-emitted order unless this contract defines an explicit normalization rule.
- Every canonical event MUST emit the full top-level property set in the declared order even when some conditional semantics are absent.
- `appendPrecondition` MUST always be serialized and MUST encode the expected prior `sequence` used for fail-closed append validation.
- When a conditional scalar or object field is not semantically required, canonical JSON MUST emit that property as `null` rather than omitting it.
- When a conditional array field is not semantically required, canonical JSON MUST emit that property as `[]` rather than omitting it.
- When `details` has no event-type-specific members, canonical JSON MUST emit `details: {}`.
- Unexpected sibling files, duplicate sequences, skipped sequences, or filename and payload mismatches make the ledger malformed.
- `schemaVersion = clean-squad-workflow-audit/v4` is the current normative schema. Earlier schema versions remain historical snapshots and MUST NOT have missing v4 semantics or timing backfilled from secondary logs.
- `ledgerDigestAlgorithm = sha256-canonical-ledger-files-v1` means SHA-256 over the exact stable directory snapshot defined by this contract.

Each immutable canonical event file MUST use this property order and shape:

```json
{
  "sequence": 1,
  "eventUtc": "2026-03-25T00:00:00.0000000Z",
  "logicalEventId": "phase-03-start",
  "actor": "cs River Orchestrator",
  "phase": "architecture",
  "eventType": "phase-started",
  "appendPrecondition": {
    "expectedPriorSequence": 0
  },
  "workItemId": "work.architecture.solution-design",
  "rootWorkItemId": "work.architecture.solution-design",
  "spanId": "span.architecture.solution-design.attempt-01",
  "causedBy": {
    "sequence": 12,
    "logicalEventId": "phase-02-complete",
    "relationship": "direct"
  },
  "closes": null,
  "outcome": null,
  "summary": "Architecture phase started.",
  "reasonCode": null,
  "artifacts": [],
  "artifactTransitions": [],
  "iterationId": null,
  "provenance": {
    "sourceKind": "system-triggered",
    "recordedBy": "cs River Orchestrator",
    "evidence": []
  },
  "details": {}
}
```

Field model:

In the `Required` column, `conditional` means the property is always serialized in canonical order but is semantically mandatory only for the event families named in the writer-obligation matrix.

| Field | Required | Meaning | Notes |
|-------|----------|---------|-------|
| `sequence` | yes | Canonical ordering authority | Remains the only chronology authority |
| `eventUtc` | yes | Canonical observation or emission time | Timing and diagnostics only |
| `logicalEventId` | yes | Retry-safe semantic identity | Reuse only for an exact retry of the same semantic event |
| `actor` | yes | Declared canonical writer | Must match the ownership boundary |
| `phase` | yes | Workflow phase | Existing phase vocabulary is preserved |
| `eventType` | yes | Coarse event taxonomy | Existing vocabulary is retained and strengthened |
| `appendPrecondition` | yes | Fail-closed append expectation | MUST encode the expected prior `sequence` for this append |
| `workItemId` | conditional | Stable canonical unit of work | Required for reviewer-significant and other meaningful events |
| `rootWorkItemId` | conditional | Stable lineage root across retries, repetitions, and supersessions | Equals `workItemId` on the first attempt |
| `spanId` | conditional | Specific bounded attempt or activity | Required when the event starts a span or names a bounded active attempt; terminal events identify the ended span through `closes` |
| `causedBy` | conditional | Single direct causal link | Required for meaningful derived events |
| `closes` | conditional | Exact start or span closed by this event | Required on terminal events |
| `outcome` | conditional | Terminal disposition | Required on terminal events |
| `summary` | yes | Human-readable explanation | Must align with structured fields and never override them |
| `reasonCode` | conditional | Controlled exception or rationale code | Required for deviations, blocks, omissions, and stale states |
| `artifacts` | conditional | Evidence bindings to files or immutable external references | Evidence only, not lifecycle semantics |
| `artifactTransitions` | conditional | Structured business-artifact state changes | Separate from evidence bindings |
| `iterationId` | conditional | Loop or retry grouping identifier | Supports repeated review cycles or attempts |
| `provenance` | conditional | Source and evidence binding for the claim | Required for every meaningful event defined by this contract |
| `details` | yes | Event-specific payload | MUST always be serialized; use `{}` when no event-type-specific members apply, and MUST NOT restate shared semantics inconsistently |

`eventUtc` rules:

- `eventUtc` MUST be an ISO-8601 UTC timestamp string with a trailing `Z`.
- `eventUtc` records when the canonical writer authoritatively observed or emitted the event.
- Timing buckets MUST be derived only from canonical `eventUtc` values and explicit wait boundaries recorded in immutable event files under `workflow-audit/`.

`logicalEventId` retry rule:

- Reuse the same `logicalEventId` only when retrying the same semantic event after a failed append or transient write failure.
- If the event meaning, lineage, cause, closure, outcome, reason, artifact set, provenance, or details change materially, a new `logicalEventId` MUST be generated.
- Duplicate `logicalEventId` values with materially different content are invalid chronology.

`appendPrecondition` MUST use this shape:

```json
{
  "expectedPriorSequence": 0
}
```

Append-precondition rules:

- `appendPrecondition.expectedPriorSequence` MUST equal the actual ledger-tail `sequence` immediately before the append is attempted.
- Writers MUST fail closed when the ledger tail does not match `appendPrecondition.expectedPriorSequence`.
- The first canonical append MUST use `sequence = 1` with `appendPrecondition.expectedPriorSequence = 0`.

Canonical append order:

1. Derive the authoritative ledger-tail `sequence` by scanning `workflow-audit/` for the highest contiguous seven-digit event file, starting from `0000001.json`, and use `0` when only `meta.json` exists.
2. If `state.json.audit.currentSequence` is present, verify that it equals the authoritative ledger-tail `sequence`; otherwise fail closed and require manual repair before any append.
3. Compute the next `sequence` as authoritative ledger tail + 1.
4. Serialize the new canonical event to a temporary file inside `workflow-audit/`.
5. Flush and close the temporary file.
6. Atomically rename the temporary file to the final seven-digit sequence filename.
7. Only after the final event file exists, update `state.json.audit.currentSequence` to mirror the new authoritative ledger tail.

If any step fails, fail closed and do not advance `state.json.audit.currentSequence`.

Stable snapshot and digest rules:

- A stable snapshot at watermark `N` consists of `meta.json` plus every event file from `0000001.json` through the file for watermark `N` in strict ascending order, after the watermark is captured first.
- Files created after watermark capture are not part of that snapshot.
- Gaps, duplicates, malformed event files, filename or payload mismatches, or unexpected sibling files fail closed.
- Secondary artifacts such as `activity-log.md` or `state.json` MUST NOT repair canonical gaps.
- `ledgerDigestAlgorithm = sha256-canonical-ledger-files-v1` hashes the exact UTF-8 bytes of canonical `meta.json`, then a single line-feed separator, then the exact UTF-8 bytes of each canonical event file in ascending sequence order separated by a single line-feed.
- Digest computation MUST begin only after the contiguous `1..N` file set has been verified for the captured watermark.

Allowed `phase` values:

| Value | Meaning |
|-------|---------|
| `discovery` | Phase 1 intake and discovery |
| `three-amigos` | Phase 2 business, technical, QA, and adoption analysis |
| `architecture` | Phase 3 solution design, C4, and ADR work |
| `planning` | Phase 4 plan drafting and review cycles |
| `implementation` | Phase 5 implementation and commit gating |
| `code-review` | Phase 6 review and remediation |
| `qa-validation` | Phase 7 QA validation |
| `documentation` | Phase 8 documentation and doc review |
| `pr-merge` | Phase 9 PR creation, CI, polling, and merge readiness |

Allowed `eventType` values:

| Event Type | Semantic Family | Starts Span | Terminal | Requires Provenance | Allows Artifact Transitions | Reviewer Meaningful |
|------------|-----------------|-------------|----------|---------------------|-----------------------------|---------------------|
| `phase-started` | lifecycle-start | yes | no | yes | no | yes |
| `phase-completed` | lifecycle-terminal | no | yes | yes | no | yes |
| `completed` | lifecycle-terminal | no | yes | yes | no | yes |
| `delegation-recorded` | causal-link | no | no | yes | no | yes |
| `artifact-published` | artifact-transition | no | no | yes | yes | yes |
| `wait-started` | lifecycle-start | yes | no | yes | no | yes |
| `wait-ended` | lifecycle-terminal | no | yes | yes | no | yes |
| `deviation-recorded` | deviation | optional | optional | yes | optional | yes |
| `blocked` | interruption | no | no | yes | optional | yes |
| `handoff-recorded` | ownership | no | no | yes | no | yes |
| `ci-identity-bound` | publication-support | no | no | yes | no | yes |
| `review-thread-updated` | review-progress | optional | optional | yes | optional | yes |
| `reviewer-summary-invalidated` | publication-state | no | no | yes | yes | yes |
| `reviewer-summary-published` | publication-state | no | no | yes | yes | yes |
| `merge-readiness-evaluated` | publication-support | no | no | yes | no | yes |
| `run-completed` | run-terminal | no | yes | yes | no | yes |

Event-type usage rules:

- `phase-completed` MUST be used only to close one of the nine top-level workflow phases and MUST correspond to a span opened by `phase-started`.
- `completed` MUST be used only for terminal bounded work items that are not whole phases and not the overall workflow run, such as gate decisions, delegated review passes, remediation slices, validation attempts, commit creation, or PR-slice completion.
- `run-completed` MUST be reserved for the final terminal event that records the disposition of the overall Clean Squad run and MUST NOT be used for individual phases or subordinate work items.

Outcome vocabulary:

| Value | Meaning |
|-------|---------|
| `succeeded` | The closed span completed successfully |
| `failed` | The closed span ended unsuccessfully |
| `skipped` | The span was intentionally not executed |
| `cancelled` | The span was ended without completion |
| `superseded` | The span was replaced by a later attempt or decision |

Allowed `reasonCode` values:

| Value | Meaning |
|-------|---------|
| `allowed-skip` | A workflow step was intentionally skipped with approval |
| `declined-finding` | A review or QA finding was declined with rationale |
| `declined-review-comment` | A PR review comment was declined with rationale |
| `blocked-external` | Work is blocked by a human, service, or dependency outside the agent boundary |
| `blocked-validation` | Work is blocked by missing or failing validation evidence |
| `stale-head` | Freshness broke because HEAD changed |
| `stale-ci-identity` | Freshness broke because required CI identity changed |
| `stale-reviewer-meaningful-change` | Freshness broke because reviewer-meaningful canonical facts changed |
| `ledger-tail-mismatch` | Append failed because the expected prior sequence did not match |
| `unauthorized-writer` | A canonical write was attempted by the wrong owner |
| `invalid-provenance` | Provenance was missing, malformed, or mismatched |
| `missing-evidence` | Required evidence or artifact binding was missing |
| `malformed-ledger` | The canonical ledger shape or ordering was malformed |
| `invalid-chronology` | Sequence, delegation basis, or logical-event chronology was invalid |
| `unmatched-wait` | A wait interval was opened or closed without a matching boundary |
| `overlapping-wait` | Wait intervals overlapped in an invalid way |
| `impossible-timing` | Timing totals were impossible |
| `iteration-cap-reached` | A bounded retry or polling loop hit its cap |
| `not-applicable` | The event is intentionally recorded as not applicable |

`artifacts` MUST be an array of objects with this shape:

```json
{
  "path": ".thinking/<task>/01-discovery/requirements-synthesis.md",
  "role": "phase-output",
  "digestSha256": "<sha256 of exact file contents>"
}
```

Artifact binding rules:

- `path` values MUST be workspace-relative paths when the artifact exists in the repository or task folder.
- `role` MUST describe the artifact purpose using a stable noun phrase such as `phase-output`, `review-synthesis`, `pr-description`, `thread-log`, `quality-gate-evidence`, `gate-package`, or `scope-assessment`.
- Evidence-bearing artifacts MUST bind either `digestSha256` for local files or `immutableId` plus `uri` for immutable external resources.
- Path-only references are insufficient for reviewer-facing audit publication.

`artifactTransitions` MUST be an array of objects with this shape:

```json
{
  "artifactId": "artifact.workflow-audit.md",
  "transition": "revised",
  "bindingPath": ".thinking/<task>/workflow-audit.md",
  "predecessorArtifactId": "artifact.workflow-audit.md",
  "predecessorSequence": 41,
  "reasonCode": null
}
```

Artifact transition rules:

- `artifactTransitions` capture business-artifact lifecycle meaning and MUST NOT be replaced by path-only inference from `artifacts`.
- `artifactId` MUST remain stable across revisions of the same business artifact.
- `predecessorArtifactId` and `predecessorSequence` MUST be present when the transition asserts revision, replacement, or supersession lineage.
- Allowed `transition` values are `created`, `revised`, `accepted`, `rejected`, `replaced`, `superseded`, and `omitted`.

`provenance` MUST use this shape:

```json
{
  "sourceKind": "system-triggered",
  "recordedBy": "cs River Orchestrator",
  "evidence": []
}
```

Provenance rules:

- `provenance` is required for every meaningful event in this contract and missing provenance is a validation failure.
- `recordedBy` MUST match the canonical writer that authoritatively recorded the event.
- `evidence` MUST use the same evidence-binding shape as `artifacts` when evidence exists.
- Provenance MAY corroborate a claim, but it MUST NOT be used to reconstruct missing cause, closure, lineage, or outcome semantics.

Allowed `provenance.sourceKind` values:

| Value | Meaning |
|-------|---------|
| `human-input` | The canonical claim records a user answer, approval, or other directly captured human input |
| `tool-output` | The canonical claim is bound to deterministic tool, CI, or API output |
| `system-triggered` | The canonical claim records an internally observed workflow transition or automation-triggered state change |

Relationship semantics:

- `causedBy` is single-parent only. If an event has multiple antecedents, the writer MUST first record a synthetic decision or synthesis event and then point the later event at that single canonical cause.
- `causedBy.relationship` MUST use one of: `direct`, `derived`, `human-input`, `tool-output`, or `system-triggered`.
- `rootWorkItemId` preserves lineage across retries, supersessions, and review loops.
- `closes` MUST carry both the referenced `spanId` and the start-event identity it closes.
- `blocked` is not a terminal outcome. It is an interruption signal that MUST either be followed later by a terminal close for the same span or be paired immediately with a terminal event that records the actual disposition.

`closes` MUST use this shape:

```json
{
  "spanId": "span.architecture.solution-design.attempt-01",
  "sequence": 13,
  "logicalEventId": "phase-03-start"
}
```

`details` is required on every canonical event and MAY contain only event-type-specific keys defined by this contract. It MUST NOT be used to encode append-precondition semantics because those semantics live in `appendPrecondition`. When no event-type-specific keys apply, `details` MUST be `{}`:

- `delegation-recorded`: `delegatedAgent`, `expectedOutputPath`, `completionSignal`, `closureCondition`, `allowedActions`, `authorizedTargets`
- `completed`: either `gateId`, `gateName`, `decision`, `decidedBy`, and optional `notes` when the completed event records a human gate decision, or `executionScope`, `discoveryMode`, `qualificationReason`, and optional `notes` when the completed event records the governed intake qualification outcome
- `wait-started` and `wait-ended`: `waitKind` with allowed values `human` or `system`
- `deviation-recorded`: `deviationClass`, `rationalePath`
- `blocked`: `blockerKind`, `blockedOn`
- `handoff-recorded`: `fromActor`, `toActor` (historical-only legacy ownership event; the active Phase 9 flow uses bounded `delegation-recorded` events instead)
- `ci-identity-bound`: `requiredCiIdentitySet`
- `review-thread-updated`: `threadId`, `action`, `commitSha`
- `reviewer-summary-invalidated` and `reviewer-summary-published`: `publicationState`
- `merge-readiness-evaluated`: `decision`, `blockingReasons`

Delegation lifecycle rules:

- For `delegation-recorded`, `workItemId` MUST name the bounded Phase 9 task slice.
- `details.expectedOutputPath` MUST name the fresh single artifact path or fresh bundle directory the delegation authorizes.
- `details.completionSignal` MUST name the canonical evidence or event pattern `cs River Orchestrator` expects to treat the delegated slice as successfully handed back.
- `details.closureCondition` MUST name the canonical condition that ends the delegation, including successful completion, block, cancellation, or supersession.
- `details.allowedActions` MUST enumerate the exact mutation classes and Phase 9 operations authorized within that delegation, using stable values such as `stale-marker`, `reviewer-summary-publish`, `thread-reply`, `thread-resolve`, `pr-description-update`, `ci-evidence-read`, or `poll-review-comments`.
- `details.authorizedTargets` MUST enumerate the exact resources the delegation covers, such as the PR number, summary section, freshness stamp, thread IDs, or CI identity set for the current HEAD SHA.
- Delegation validation MUST reject returned evidence for any action or target outside `details.allowedActions` and `details.authorizedTargets`, even when the delegated artifact bundle otherwise looks complete.
- Material revisions to previously handed-back delegated artifacts SHOULD be expressed through `artifactTransitions` with stable `artifactId` plus predecessor linkage, and they MUST publish a new artifact path instead of silently overwriting the earlier delegated output.
- The Phase 9 stale-marker delegation MUST be its own bounded capability slice whose `details.allowedActions` contains only `stale-marker` and whose `details.authorizedTargets` are limited to the current PR and reviewer-summary freshness marker.
- A Phase 9 delegation remains active only until `cs River Orchestrator` records a later canonical event for the same `workItemId` whose `causedBy.logicalEventId` references that `delegation-recorded` event and whose semantics satisfy the recorded `completionSignal` or `closureCondition`. After that closure, any further PR Manager work MUST use a new `delegation-recorded` event.

Allowed `details.publicationState` values:

| Value | Meaning |
|-------|---------|
| `stale` | The previously published reviewer summary is no longer fresh for the current canonical facts or CI identity |
| `fresh` | The currently published reviewer summary is verified as current for the canonical facts and attached CI identity |

Allowed `details.executionScope` values:

| Value | Meaning |
|-------|---------|
| `full-run` | Governed delivery proceeds end to end by default. |
| `bounded-objective` | Governed delivery proceeds only for the explicitly bounded objective while still honoring the applicable gates. |
| `non-governed` | The request is blocked or redirected outside governed Clean Squad execution. |

Allowed `details.discoveryMode` values:

| Value | Meaning |
|-------|---------|
| `autonomous-defaults` | Discovery defaults are generated by the bounded specialist from evidence, repo patterns, and framework defaults. |
| `manual-refinement` | River continues the human interview using five-question batches. |

Writer-obligation matrix:

| Event Family | Required Fields | Optional Fields | Invalid If Missing |
|-------------|-----------------|-----------------|--------------------|
| lifecycle-start | `workItemId`, `rootWorkItemId`, `spanId`, `provenance` | `causedBy`, `iterationId` | the span cannot be tracked or closed |
| lifecycle-terminal | `workItemId`, `rootWorkItemId`, `closes`, `outcome`, `provenance` | `causedBy`, `artifacts`, `artifactTransitions`, `iterationId` | exact closure and disposition become ambiguous |
| causal-link | `workItemId`, `rootWorkItemId`, `causedBy`, `provenance` | `artifacts`, `details`, `iterationId` | the derived event looks chronology-driven |
| artifact-transition | `workItemId`, `rootWorkItemId`, `artifactTransitions`, `provenance` | `causedBy`, `artifacts`, `details`, `iterationId` | artifact history becomes narrative-only |
| deviation | `workItemId`, `rootWorkItemId`, `causedBy`, `reasonCode`, `provenance`, `details` | `spanId`, `artifactTransitions`, `iterationId` | the deviation basis and remediation path become prose-only |
| interruption | `workItemId`, `rootWorkItemId`, `causedBy`, `reasonCode`, `provenance`, `details` | `spanId`, `artifactTransitions`, `iterationId` | the interruption cannot be tied to the affected work or blocker |
| ownership | `workItemId`, `rootWorkItemId`, `causedBy`, `provenance`, `details` | `artifacts`, `iterationId` | canonical writer or delegated-execution ownership becomes ambiguous |
| publication-support | `workItemId`, `rootWorkItemId`, `causedBy`, `provenance`, `details` | `artifacts`, `iterationId` | the CI or merge-readiness basis cannot be audited deterministically |
| review-progress | `workItemId`, `rootWorkItemId`, `causedBy`, `provenance`, `details` | `spanId`, `artifacts`, `artifactTransitions`, `iterationId`, `reasonCode` | the exact review-thread action cannot be audited deterministically |
| publication-state | `workItemId`, `rootWorkItemId`, `provenance`, `details` | `artifactTransitions`, `causedBy`, `artifacts`, `iterationId` | reviewer-summary freshness becomes untrustworthy |
| run-terminal | `workItemId`, `rootWorkItemId`, `closes`, `outcome`, `provenance` | `causedBy`, `artifacts`, `artifactTransitions`, `iterationId` | run completion cannot be tied to the exact ended span or disposition |

Invariant catalog:

- Every meaningful started span MUST close exactly once.
- Every event that requires causality MUST declare exactly one direct cause.
- Every terminal event MUST declare an explicit outcome from the approved vocabulary.
- Artifact transition lineage MUST use stable identity and predecessor linkage where applicable.
- Provenance MUST exist for every meaningful event defined by this contract.
- Reviewer-summary freshness MUST invalidate immediately on reviewer-meaningful canonical changes.
- Canonical meaning MUST NOT be inferred from prose, sequence order, secondary logs, or evidence bindings alone when structured v4 semantics are missing.

#### Required CI Identity Normalization

When merge readiness depends on CI, provenance MUST bind this normalized structure:

```json
{
  "headSha": "<current HEAD SHA>",
  "checks": [
    {
      "provider": "github-actions",
      "workflow": "CI",
      "job": "build",
      "runId": "123456789",
      "attempt": 1,
      "conclusion": "success"
    }
  ]
}
```

Normalization rules:

- Include only checks required for merge readiness for the current HEAD SHA.
- Sort `checks` by `provider`, then `workflow`, then `job`, then `runId`, then `attempt`.
- Remove exact duplicates before comparison.
- Compare the normalized structure as parsed JSON when validating freshness or provenance; do not rely on raw byte-for-byte equality for object property order or whitespace.

#### State and Runtime Support Contract

`state.json` is operational support state only. It MAY cache audit cursor data, but it MUST NOT replace or repair canonical facts from `workflow-audit/`.

`state.json` MUST use this shape:

```json
{
  "task": "<task-slug>",
  "createdUtc": "<ISO-8601 UTC>",
  "lastUpdatedUtc": "<ISO-8601 UTC>",
  "currentPhase": "discovery|three-amigos|architecture|planning|implementation|code-review|qa-validation|documentation|pr-merge",
  "status": "in-progress|blocked|complete",
  "branch": "<branch-name-or-null>",
  "prNumber": null,
  "lastCommitSha": null,
  "lastCommitTimeUtc": null,
  "workflowContractFingerprint": "<same value used by workflow-audit/meta.json>",
  "audit": {
    "currentSequence": 0,
    "currentOwner": "cs River Orchestrator|null",
    "openWait": null,
    "lastCompiledAtUtc": null
  },
  "discoveryRound": 0,
  "planReviewCycle": 0,
  "implementationIncrement": 0,
  "adrCount": 0
}
```

`state.json.audit.currentOwner` means canonical ownership only. For in-progress workflow states it MUST be `cs River Orchestrator`. `null` is allowed only when support state is absent or uninitialized, and `currentOwner` MUST NOT represent delegated execution ownership.

`state.json.audit.currentSequence` MUST equal the highest durable seven-digit event file in `workflow-audit/`, or `0` when only `meta.json` exists and no canonical event files have been appended yet.

Resume or recovery MUST fail closed when `workflow-audit/` is missing `meta.json`, contains duplicate or skipped sequence files, contains an unexpected sibling file, contains a filename or payload mismatch, or when `state.json.audit.currentSequence` disagrees with the highest durable canonical event file.

### Hard Cutover for Legacy Governed Runs

- This redesign is a breaking governed-workflow contract change.
- Governed task folders whose canonical owner is not `cs River Orchestrator` are historical evidence only after rollout.
- Pre-cutover governed runs MUST NOT be resumed or migrated in place.
- Any attempted resume of a pre-cutover governed run or single-file `workflow-audit.json` ledger MUST fail closed and instruct restart under `cs River Orchestrator`.

If `audit.openWait` is not null, it MUST use this shape:

```json
{
  "kind": "human|system",
  "startedBy": "<actor>",
  "startedSequence": 0,
  "reasonCode": "<allowed reasonCode>"
}
```

#### Provenance Contract

Every derived audit artifact MUST bind to a provenance envelope containing at least:

1. current HEAD SHA
2. ledger watermark or max `sequence` included
3. `ledgerDigest`
4. `workflowContractFingerprint`
5. generation timestamp
6. generator identity
7. for the `Reviewer Audit Summary` and merge-readiness evaluation, the current normalized required CI-result identity set for that HEAD SHA when merge readiness depends on CI

Additional provenance rules:

- `ledgerDigest` MUST be the SHA-256 digest of the exact stable ledger snapshot used to compile or publish the artifact.
- `workflowContractFingerprint` MUST be the SHA-256 digest of the exact `WORKFLOW.md` contents used during compilation or publication.
- `workflow-audit.md` MUST bind only to the stable ledger snapshot and `workflowContractFingerprint` used for compilation; a required CI-result identity change alone MUST NOT force recompilation of `workflow-audit.md`.
- The `Reviewer Audit Summary` freshness stamp MUST attach the current normalized required CI-result identity set when merge readiness depends on CI.
- Reviewer-facing audit output MUST bind to evidence-bearing artifacts through `digestSha256` or immutable external identities before it may be treated as current policy-authoritative reviewer evidence.

Responsibilities:

- The Scribe emits provenance for `workflow-audit.md`.
- `cs River Orchestrator` verifies `workflow-audit.md` provenance and attaches or verifies the current normalized required CI-result identity set before publishing or directing publication of the `Reviewer Audit Summary`.
- The PR Manager MAY execute the PR-surface publication or stale-marker mutation only under explicit `cs River Orchestrator` delegation and MUST return the resulting evidence.
- Merge readiness MUST NOT pass when provenance is stale, missing, or mismatched.

#### Trust and Freshness Contract

Freshness and merge readiness MUST bind to the current HEAD SHA and the required CI-result identity set for that SHA.

Reviewer-facing audit output becomes stale on any of these events:

1. commit or push that changes HEAD SHA
2. force-push or rebase
3. required check rerun
4. replaced or superseded check result
5. required-check state change
6. canonical ledger event that changes reviewer-meaningful output

Publication and recovery rules:

1. `cs River Orchestrator` MUST invalidate immediately at first observation of a relevant change, including during any active 300-second review-polling wait.
2. `cs River Orchestrator` MUST record the canonical invalidation fact when invalidating the `Reviewer Audit Summary`.
3. `cs River Orchestrator` MUST ensure the `Reviewer Audit Summary` is marked stale on the PR surface with the stale reason and the last known freshness stamp when freshness is invalidated.
4. The 300-second review-polling wait MUST NOT delay stale-marker publication.
5. `cs River Orchestrator` MUST maintain an active bounded stale-marker delegation for the current PR surface whenever a fresh `Reviewer Audit Summary` is published or a review-polling wait is active so the PR Manager can apply the stale marker immediately at first observation without waiting for a new delegation round-trip.
6. If HEAD SHA, the stable ledger snapshot, `workflowContractFingerprint`, or reviewer-meaningful canonical output changes, `cs River Orchestrator` MUST obtain a fresh `workflow-audit.md` compilation from cs Scribe using a new stable ledger snapshot before republishing reviewer-facing audit output.
7. If only the required CI-result identity set changes for an unchanged HEAD SHA and unchanged reviewer-meaningful canonical facts, `cs River Orchestrator` MUST refresh the `Reviewer Audit Summary` freshness stamp and merge-readiness evaluation without recompiling `workflow-audit.md`.
8. `cs River Orchestrator` MUST verify that the regenerated or reused `workflow-audit.md` provenance matches the current HEAD SHA, ledger watermark, `ledgerDigest`, and `workflowContractFingerprint`, and that the attached normalized required CI-result identity set is current, before republishing.
9. `cs River Orchestrator` MUST republish only when reviewer-meaningful content changes or merge-readiness validation requires a fresh publication. The PR Manager MAY apply the PR-surface update only under bounded delegation whose `details.allowedActions` and `details.authorizedTargets` cover that specific mutation.
10. Invalidation MUST be more granular than publication so the PR description does not churn on low-signal changes.

#### Verdict Model

| Verdict | Meaning | Reviewer Action |
|---------|---------|-----------------|
| `Conformant` | Required workflow obligations were satisfied and current policy-authoritative evidence supports the run | Continue review normally |
| `ConformantWithDeviations` | Workflow obligations were satisfied, deviations were allowed and explained, and current policy-authoritative evidence supports the run | Review deviations, then continue if acceptable |
| `NonConformant` | Current policy-authoritative evidence shows the workflow contract was violated | Treat as a policy failure and do not accept the workflow as complete |
| `Blocked` | The run ended before merge-ready completion | Do not treat the workflow as complete |
| `Untrusted` | Evidence is stale, malformed, incomplete, provenance-broken, or otherwise not reliable | Stop and require audit regeneration or correction |

#### Evidence Sufficiency Rules

- Major completion claims MUST include sufficient artifact evidence.
- Missing, malformed, or policy-violating evidence MUST NOT yield `Conformant` or `ConformantWithDeviations`.
- Schema validation is required wherever the contract defines canonical or derived metadata structure.
- Supporting logs MAY corroborate but MUST NOT repair missing canonical facts.
- Missing evidence MUST map to `NonConformant`, `Blocked`, or `Untrusted` based on whether the failure is a policy violation, an incomplete run, or a trust failure.

#### Timing Contract

Required timing buckets:

1. elapsed total
2. active agent total
3. human-wait total
4. system-wait total

Timing invariants:

1. Timing buckets MUST be derived only from canonical `eventUtc` values recorded in immutable event files under `workflow-audit/`.
2. Human-wait and system-wait never count as active agent time.
3. Timing buckets MUST be mutually exclusive.
4. Unmatched, overlapping, or impossible timing intervals invalidate trust.
5. Supporting logs MAY corroborate timing analysis, but MUST NOT supply missing canonical timing boundaries.
6. Timing interpretation on the PR surface MUST render this exact sentence: `Active agent time excludes human replies and system wait such as CI or review polling.`

#### Reviewer Audit Summary Contract

The PR-facing artifact is named `Reviewer Audit Summary` and SHOULD fit within one screen on a typical desktop review view.

It MUST present content in this exact order:

1. verdict
2. reviewer action guidance
3. current blocker(s)
4. provenance and freshness stamp
5. condensed top-to-bottom Mermaid flow
6. four-bucket timing summary
7. the fixed timing interpretation sentence
8. plain-language major deviations
9. pointer to `workflow-audit.md`

Overflow policy:

- The PR surface MUST keep only current blockers, the condensed Mermaid topology, and at most three plain-language deviations.
- If the summary would exceed one screen, overflow detail MUST move to `workflow-audit.md` and the PR surface MUST keep only a short pointer to that detail.

When freshness is broken, the PR surface MUST show a stale marker in place of a merge-ready reviewer summary until `cs River Orchestrator` directs a fresh republication.

#### Detailed Audit Opening Contract

`workflow-audit.md` MUST begin with a short why-this-matters opener that explains why the reader should trust or question the run before reading chronology.

#### Required Failure Matrix

Implementation and review MUST explicitly cover at least these cases with the expected verdict and reviewer-summary publication behavior.

| Case | Expected verdict | Reviewer-summary publication behavior |
|------|------------------|--------------------------------------|
| clean happy path | `Conformant` | Publish or keep published when provenance is current. |
| human clarification wait | `Conformant` | Publish or keep published when the human-wait interval is explicitly bounded and provenance is current. |
| Phase 9 polling wait | `Conformant` | Publish or keep published when the system-wait interval is explicitly bounded and provenance is current. |
| remediation loop | `ConformantWithDeviations` | Republish only when the reviewer-meaningful deviation summary changes and provenance is refreshed. |
| allowed skip with valid reason | `ConformantWithDeviations` | Publish or republish with the allowed skip recorded as a plain-language deviation and provenance refreshed. |
| declined review comment with rationale | `ConformantWithDeviations` | Publish or republish with the decline rationale reflected in reviewer-facing deviations and provenance refreshed. |
| blocked run | `Blocked` | Do not publish a merge-ready summary; any existing reviewer summary MUST be treated as not sufficient for merge readiness until the block is cleared and a fresh summary is generated. |
| stale reviewer summary | `Untrusted` | Invalidate immediately and require regeneration before merge readiness can pass. |
| unmatched wait boundary | `Untrusted` | Invalidate immediately and require audit correction before republishing. |
| overlapping wait intervals | `Untrusted` | Invalidate immediately and require audit correction before republishing. |
| impossible timing totals | `Untrusted` | Invalidate immediately and require audit correction before republishing. |
| duplicate logical event identity | `NonConformant` | Do not publish as trusted reviewer evidence until the canonical ledger is repaired and a fresh summary is generated. |
| invalid chronology or delegation-basis violation | `NonConformant` | Do not publish as trusted reviewer evidence until the chronology or delegation-basis violation is corrected and a fresh summary is generated. |
| unauthorized writer or unauthorized delegated actor | `NonConformant` | Do not publish as trusted reviewer evidence until the unauthorized action is corrected and a fresh summary is generated. |
| missing required evidence for a major completion claim | `Blocked` when the run is incomplete; otherwise `Untrusted` when a completion claim lacks trustworthy evidence | Invalidate or withhold publication until the missing evidence is supplied and a fresh summary is generated. |
| malformed canonical or derived provenance metadata | `Untrusted` | Invalidate immediately and require provenance repair before republishing. |

## River Orchestrator Execution Boundary

`cs River Orchestrator` is an orchestrator and workflow recorder, not a specialist implementation agent.

- `cs River Orchestrator` MUST ask the user questions, sequence the workflow, update shared state, append canonical workflow facts, write `activity-log.md`, and enforce quality gates.
- `cs River Orchestrator` MUST use `runSubagent` for specialist work including analysis, synthesis, design, coding, testing, code review, QA validation, documentation, and PR management.
- Before every `runSubagent` call, `cs River Orchestrator` MUST validate that the selected agent is explicitly named in the `Agent Roster` section of this workflow.
- Generic categories such as review personas and domain experts MUST resolve only to named agents in the `Agent Roster` section of this workflow.
- If no approved Clean Squad agent clearly fits, `cs River Orchestrator` MUST stop, record the blocker, and ask the user to either choose the nearest approved Clean Squad agent, approve a roster or workflow change first, or explicitly leave Clean Squad orchestration for that task.
- `cs River Orchestrator` MUST NOT bypass a specialist sub-agent by performing that specialist work directly.
- `cs River Orchestrator` is the only governed agent that may communicate directly with the human user.

### State File (`state.json`)

`state.json` MUST match the `State and Runtime Support Contract` above exactly. The example above is normative; duplicate or stale variants in agent prompts are invalid.

## Phase 1: Intake & Discovery

**Owner**: cs River Orchestrator (drives conversation with human user)
**Sub-agents**: cs Requirements Analyst, cs Discovery Synthesizer

### Process

1. The human reaches `cs River Orchestrator` either through direct free-form intake
  or with a G0-approved Story Pack candidate from **cs Entrepreneur**.
2. `cs River Orchestrator` creates `.thinking/<date>-<slug>/` and writes `00-intake.md`.
3. `cs River Orchestrator` classifies intake against the deterministic intake decision table.
4. If the intake appears partial or ambiguous, `cs River Orchestrator` asks exactly
   one qualification round through the question UI to capture execution scope and
   discovery mode, then records the outcome canonically.
5. If no qualification round is needed, `cs River Orchestrator` records the defaulted
   `execution scope = full-run` and `discovery mode = autonomous-defaults` canonically.
6. If discovery mode is `manual-refinement`, `cs River Orchestrator` asks the human
   discovery questions in batches of exactly 5, records answers in
   `questions-round-NN.md`, and invokes **cs Requirements Analyst** to analyze gaps
   and suggest the next 5 questions.
7. If discovery mode is `autonomous-defaults`, `cs River Orchestrator` invokes
   **cs Requirements Analyst** to write evidence-backed autonomous discovery batches
   into `questions-round-NN.md` using repo patterns and framework defaults with the
   required trust and provenance labels.
8. `cs River Orchestrator` decides when requirements are sufficiently clear,
   respecting the autonomous discovery bound when autonomous mode is active.
9. `cs River Orchestrator` invokes **cs Discovery Synthesizer** to write
   `requirements-synthesis.md` from the gathered discovery evidence, preserving
   confirmed requirements separately from inferred defaults and unresolved questions.

### Discovery Execution Rules

- The qualification round **MUST** happen at most once per governed scope version
  and **MUST** capture exactly two decisions: execution scope and discovery mode.
- In `manual-refinement`, questions **MUST** be grouped in sets of exactly 5.
- In `manual-refinement`, after each set, the next 5 **MUST** be informed by
  answers already received.
- Manual questions **SHOULD** start broad (business value, user needs) and
  progressively narrow (technical constraints, edge cases, quality expectations).
- If the user is highly technical and mentions code quality, subsequent manual
  questions **MUST** reflect that (architecture patterns, testing strategy, naming).
- If the user is non-technical, manual questions **MUST** use plain language and
  focus on outcomes rather than implementation.
- Each manual discovery question **MUST** include ranked options (A, B, C...) plus
  **(X) I don't care — pick the best default**.
- In `autonomous-defaults`, `cs Requirements Analyst` **MUST NOT** ask the human
  additional discovery questions directly and **MUST** instead publish up to 5
  inferred answers per autonomous round in `questions-round-NN.md`.
- Every autonomous discovery batch **MUST** record trust tier, source category,
  evidence reference(s), confidence, and `requiresHumanConfirmation` for each
  inferred answer.
- In `autonomous-defaults`, the combined discovery loop **MUST NOT** exceed three
  autonomous rounds or fifteen inferred questions.
- In `autonomous-defaults`, unresolved high-impact ambiguity **MUST** become an
  explicit open question or assumption rather than causing an unbounded loop of
  additional inferred rounds.

## Phase 2: Three Amigos + Adoption

**Owner**: cs River Orchestrator
**Sub-agents**: cs Business Analyst, cs Tech Lead, cs QA Analyst, cs Developer Evangelist, cs Three Amigos Synthesizer

### Process

1. `cs River Orchestrator` invokes each perspective sub-agent with the requirements
   synthesis, one at a time.
2. Each sub-agent reads requirements and produces their perspective document.
3. `cs River Orchestrator` invokes **cs Three Amigos Synthesizer** to write `02-three-amigos/synthesis.md` combining all four.
4. If any sub-agent identifies critical gaps, `cs River Orchestrator` asks the user
   additional questions before proceeding.
5. Before Phase 3 begins, `cs River Orchestrator` **MUST** obtain G1 approval for
  `01-discovery/requirements-synthesis.md` and
  `02-three-amigos/synthesis.md`.

### Perspective Outputs

| Perspective | Agent | Focus |
|-------------|-------|-------|
| Product | cs Business Analyst | User value, acceptance criteria, business rules |
| Development | cs Tech Lead | Technical feasibility, risks, architecture constraints |
| Quality | cs QA Analyst | Test strategy, edge cases, failure scenarios |
| Adoption | cs Developer Evangelist | Demo-ability, competitive positioning, conference potential, real-world relevance |

## Phase 3: Architecture & Design

**Owner**: cs River Orchestrator
**Sub-agents**: cs Solution Architect, cs C4 Diagrammer, cs ADR Keeper

### Process

1. `cs River Orchestrator` invokes **cs Solution Architect** with synthesized requirements.
2. Solution Architect produces `solution-design.md` with technology choices,
   component design, and integration points.
3. `cs River Orchestrator` invokes **cs C4 Diagrammer** to produce C4 model diagrams
  (Context and Container always; Component when a container has meaningful internal structure, otherwise an explicit omission rationale).
4. For each significant architectural decision, `cs River Orchestrator` invokes
  **cs ADR Keeper** to produce ADRs in `docs/Docusaurus/docs/adr/`.
5. `cs River Orchestrator` may invoke approved domain experts from the `Agent Roster`
   section (for example cs Expert Distributed, cs Expert Cloud, and
   cs Expert Serialization) for specialist input on architecture.

### ADR Protocol

- Every significant decision **MUST** be recorded as an ADR.
- ADRs **MUST** use the canonical frontmatter and MADR-based structure defined in `.github/instructions/adr.instructions.md`.
- ADRs **MUST** be published to `docs/Docusaurus/docs/adr/` using the sequential `NNNN-title-with-dashes.md` filename pattern and frontmatter identity rules defined in `.github/instructions/adr.instructions.md`.
- Branches **MAY** use provisional sequential ADR numbering during development, but final ADR numbering and any renumbering **MUST** follow `.github/instructions/adr.instructions.md` before merge.
- ADRs are immutable — superseded decisions get a new ADR referencing the old.
- ADRs **MUST** be consulted on subsequent changes to verify directional
  alignment.

## Phase 4: Planning & Review Cycles

**Owner**: cs River Orchestrator
**Sub-agents**: cs Plan Synthesizer, approved review personas from the Agent Roster

### Process

1. `cs River Orchestrator` combines architecture, requirements, and Three Amigos output
   into `draft-plan-v1.md`.
2. `cs River Orchestrator` runs **review cycle 1** by invoking each approved review
  persona from the `Agent Roster` section as a sub-agent.
3. Each reviewer reads the plan and produces feedback.
4. `cs River Orchestrator` invokes **cs Plan Synthesizer** to deduplicate and categorize
  feedback (Must / Should / Could / Won't).
5. `cs River Orchestrator` revises the plan.
6. Repeat for **3-5 review cycles** total.
7. After final cycle, `cs River Orchestrator` writes `final-plan.md`.
8. Before Phase 5 begins, `cs River Orchestrator` **MUST** obtain G2 approval for
  `03-architecture/solution-design.md`, the binding C4 artifacts, the binding
  ADR artifacts, and `04-planning/final-plan.md`.

### Review Personas for Planning

Each review cycle invokes these personas (subset varies by task complexity):

| Persona | Agent | Focus |
|---------|-------|-------|
| Technical Architect | cs Solution Architect | Architecture soundness |
| Tech Lead | cs Tech Lead | Feasibility, risks |
| Security | cs Reviewer Security | Security implications |
| DX | cs Reviewer DX | Developer experience |
| QA | cs QA Lead | Test strategy adequacy |
| Cloud | cs Expert Cloud | Infrastructure implications |
| Distributed Systems | cs Expert Distributed | Distributed concerns |
| Performance | cs Reviewer Performance | Performance implications |
| Adoption | cs Developer Evangelist | Demo-ability, competitive positioning, content hooks |

## Phase 5: Implementation

**Owner**: cs River Orchestrator
**Sub-agents**: cs Lead Developer, cs Test Engineer, cs Commit Guardian

### Process

1. `cs River Orchestrator` creates a feature branch from `main`.
2. For each increment:
  a. `cs River Orchestrator` invokes **cs Lead Developer** with the next slice of work
      from the plan.
   b. Lead Developer writes production code (small, focused increment).
  c. Lead Developer performs semantic consistency review for touched code
    elements (types or members),
    updates stale comments or XML documentation when needed, and records the
    reviewed-member evidence in `changes.md`.
  d. `cs River Orchestrator` invokes **cs Test Engineer** to write/validate tests and
    independently verify semantic consistency for touched code elements
    against the changed behavior, recording the result in `test-results.md`.
  e. Build is run and verified clean (zero warnings).
  f. Tests are run and verified passing.
  g. `cs River Orchestrator` invokes **cs Commit Guardian** to review the final staged
    diff, validate the semantic consistency evidence chain, and emit a
    `PASS`, `WARNING`, or `BLOCKER` verdict in `commit-review.md`.
  h. If semantic drift or missing semantic-review evidence is found, Lead
    Developer fixes it and the semantic consistency gate is rerun against the
    final staged diff.
  i. Commit is made with a properly scoped message.
3. Repeat until all plan items are implemented.

Touched semantic-review scope includes any touched code element (type or
member) whose behavior, signature, or name changes, and any touched code
element that already has comments or XML documentation. Semantic drift on a
touched code element is a must-fix blocker before commit. Missing comments or
XML documentation are not universally required, but when a touched code element
clearly needs explanation or follows an established documented-public-API
pattern, the absence must be flagged as `BLOCKER` if it would leave a
materially false, incomplete, or undocumented contract; otherwise it must be
flagged as `WARNING`. This semantic consistency review is a Phase 5
code-comment/XML-doc gate, not Phase 8 product documentation.

Evidence of semantic consistency review must be recorded in `changes.md`,
`test-results.md`, and `commit-review.md`. Each artifact must identify the
reviewed touched code elements or explicitly state why no touched code
elements were in semantic-review scope.

### Incremental Discipline Rules

- Each increment **MUST** be small enough to review as if it were its own PR.
- Each increment **MUST** include relevant tests.
- The build **MUST** be clean after each increment.
- All tests **MUST** pass after each increment.
- Semantic consistency review evidence **MUST** be current for the final staged
  diff before commit approval.
- Any code change after semantic consistency review evidence is recorded
  **MUST** invalidate prior approval and **MUST** rerun the gate against the
  final staged diff before commit.
- Each commit **MUST** represent one logical step.
- The Commit Guardian reviews each commit before the next begins.

### Implementation Order

1. Write a failing test (TDD where practical).
2. Write the minimal production code to pass it.
3. Refactor if needed.
4. Run build + tests.
5. Review the final staged diff, including the semantic consistency gate.
6. Commit.
7. Next increment.

## Phase 6: Comprehensive Code Review

**Owner**: cs River Orchestrator
**Sub-agents**: Approved review personas, approved domain experts from the Agent Roster, cs Code Review Synthesizer

### Process

1. `cs River Orchestrator` uses `git diff main...HEAD` to identify all changed files.
2. `cs River Orchestrator` invokes review personas in sequence:

   | Priority | Agent | Style |
   |----------|-------|-------|
   | 1 | cs Reviewer Pedantic | Line-by-line, naming, every detail |
   | 2 | cs Reviewer Strategic | Architecture, design patterns, big picture |
   | 3 | cs Reviewer Security | OWASP, attack surface, trust boundaries |
   | 4 | cs Reviewer DX | API ergonomics, discoverability, pit of success |
   | 5 | cs Reviewer Performance | Allocations, complexity, hot paths |
   | 6 | cs Developer Evangelist | Demo-ability, shareability, competitive positioning (public API changes) |

3. `cs River Orchestrator` invokes relevant approved domain experts from the `Agent Roster` section based on the change type.
4. `cs River Orchestrator` invokes **cs Code Review Synthesizer** to synthesize all review output.
5. For each finding: fix it or document why it was declined.
6. Iterate until all reviewers are satisfied.

The phrases review personas and domain experts in this workflow refer only to
the named agents in the `Agent Roster` section below. They do not authorize
delegation to other repo agents.

### Review Coverage Rule

Every changed file **MUST** be reviewed by at least the Pedantic and Strategic
reviewers. Domain experts review files within their expertise.

## Phase 7: QA Validation

**Owner**: cs River Orchestrator
**Sub-agents**: cs QA Lead, cs QA Exploratory, cs Test Engineer, cs QA Synthesizer

### Process

1. `cs River Orchestrator` invokes **cs QA Lead** to review test strategy and coverage.
2. `cs River Orchestrator` invokes **cs QA Exploratory** to apply exploratory testing
   perspective.
3. `cs River Orchestrator` invokes **cs Test Engineer** for mutation testing (Mississippi
   projects only).
4. `cs River Orchestrator` invokes **cs QA Synthesizer** to produce `07-qa/qa-readiness.md`.
5. Any gaps identified are fed back to implementation.

## Phase 8: Documentation

**Owner**: cs River Orchestrator
**Sub-agents**: cs Documentation Scope Synthesizer, cs Technical Writer, cs Doc Reviewer, cs Developer Evangelist

### Purpose

Ensure that every user-facing change is accompanied by accurate, evidence-backed
Docusaurus documentation before the PR is created. Documentation is a first-class
deliverable, not an afterthought.

### Process

1. `cs River Orchestrator` invokes **cs Documentation Scope Synthesizer** to assess documentation scope:
   - Run `git diff --name-status --find-renames main...HEAD` to identify all
     changed source files.
   - Identify new public APIs, changed behavior, new concepts, and affected
     existing doc pages.
   - If no user-facing changes exist (pure refactors, internal-only changes),
     record the skip reason in `.thinking/<task>/08-documentation/scope-assessment.md`
     and proceed to Phase 9.

2. `cs River Orchestrator` invokes **cs Technical Writer** to create/update documentation:
   - The writer reads all `.thinking/<task>/` artifacts and the branch diff.
   - The writer builds an evidence map, classifies page types, and drafts pages.
   - Draft pages are written to `.thinking/<task>/08-documentation/drafts/`.
   - Verified pages are published to `docs/Docusaurus/docs/`.

3. `cs River Orchestrator` runs a **documentation review cycle** (repeat 1-3 times):
   a. Invoke **cs Doc Reviewer** to independently review every new or updated
      doc page against source code and tests.
   b. Doc Reviewer writes findings to
      `.thinking/<task>/08-documentation/review-cycle-NN/doc-review.md`.
   c. Invoke **cs Developer Evangelist** to review documentation for story
      value, content potential, and adoption narrative.
   d. Developer Evangelist writes findings to
      `.thinking/<task>/08-documentation/review-cycle-NN/doc-story-review.md`.
   e. For each Must Fix or Should Fix finding:
      - Re-invoke **cs Technical Writer** with the specific finding to fix.
      - Record the fix in the remediation log.
   f. Repeat until the Doc Reviewer returns no Must Fix findings.

4. `cs River Orchestrator` validates documentation quality gates:
   - [ ] All new public APIs have documentation
   - [ ] All changed behaviors reflected in existing docs
   - [ ] Page types are correct
   - [ ] Frontmatter is complete
   - [ ] Internal links resolve
   - [ ] Code examples are verified
   - [ ] Claims are evidence-backed
   - [ ] Adjacent pages are cross-linked
   - [ ] No Must Fix findings remain from Doc Reviewer

5. Update `.thinking/<task>/activity-log.md` with documentation outcomes.

### Documentation Skip Criteria

Documentation may be skipped **only** when ALL of these are true:

- No new public types, methods, or extension points were introduced
- No existing public behavior was changed
- No new configuration options were added
- No existing documentation is invalidated by the change

The skip reason **MUST** be recorded in `scope-assessment.md` with evidence.

## Phase 9: PR Creation & Merge Readiness

**Owner**: cs River Orchestrator
**Entry condition**: Phase 8 is complete and any required Phase 9 delegation basis is recorded
**Sub-agents**: cs PR Manager, cs Scribe, cs Merge Readiness Evaluator

### Process

1. `cs River Orchestrator` remains the canonical Phase 9 owner and MUST record every reviewer-significant Phase 9 fact in immutable event files under `workflow-audit/`.
2. `cs River Orchestrator` delegates only bounded Phase 9 PR-surface specialist work to **cs PR Manager** and MUST NOT give open-ended Phase 9 umbrella authority; every such delegation MUST be capability-scoped through explicit `details.allowedActions` and `details.authorizedTargets`.
3. At Phase 9 startup or recovery, if PR-surface work cannot begin normally, `cs River Orchestrator` records the blocked or resumed state canonically without transferring ownership.
4. `cs River Orchestrator` invokes **cs Scribe** to compile `workflow-audit.md` and the condensed reviewer-flow inputs from a stable `workflow-audit/` snapshot when Phase 9 audit compilation or recompilation is required.
5. The PR Manager creates or updates the PR, collects CI and review evidence, and performs delegated PR-surface mutations only within the active bounded delegation.
6. If freshness is already broken or later becomes broken, `cs River Orchestrator` MUST immediately record the invalidation canonically and ensure the PR-surface reviewer summary is marked stale with the stale reason and the last known freshness stamp at first observation, even during the 300-second polling wait, then follow the freshness recovery rules before republishing.
7. `cs River Orchestrator` MUST keep the bounded stale-marker delegation active for the current PR while a fresh reviewer summary is published or a review-polling wait is active so stale-marker publication has no integrity window.
8. The PR Manager monitors CI pipelines and handles review threads using the repository PR polling protocol only while the corresponding `delegation-recorded` event remains active for that `workItemId`, and only for actions and targets authorized in that delegation.
9. Review thread handling:
   - For each human review comment: read it, decide scope-appropriateness, fix it or push back with reasoning, reply to the thread, and either resolve it or leave it open with rationale.
10. `workflow-audit.md` and the `Reviewer Audit Summary` are derived artifacts only. Missing canonical facts MUST be fixed in `workflow-audit/`; they MUST NOT be backfilled from `activity-log.md`, thread logs, or PR prose.
11. `cs River Orchestrator` invokes **cs Merge Readiness Evaluator** to produce `09-pr-merge/merge-readiness.md` from the current PR, QA, documentation, and review evidence.
12. Before PR-ready or merge-ready progression continues, `cs River Orchestrator`
  **MUST** obtain G3 approval for `09-pr-merge/merge-readiness.md`, the
  current code-review conclusion, the current QA conclusion, and the current
  documentation conclusion.
13. Merge readiness is confirmed only when `cs River Orchestrator` evaluates current delegated evidence and records the conclusion canonically.
14. Merge readiness is confirmed when:

- [ ] PR exists
- [ ] All CI pipelines are green
- [ ] No unresolved review comments
- [ ] No open review threads
- [ ] Review polling rule satisfied
- [ ] Reviewer Audit Summary provenance is current for the HEAD SHA, ledger watermark, ledger digest, workflow contract fingerprint, and required CI-result identity set

### Review Polling Rule

A pushed PR enters a **300-second poll loop** for review comments. A single
quiet interval is not enough; the loop ends only when a poll returns no new
unaddressed comments or the configured iteration cap is reached.

Protocol:

1. After pushing to an open PR, wait 300 seconds.
2. Poll for unresolved review comments.
3. If new comments exist: address them one-at-a-time, push the fixes, then
   restart the 300-second wait.
4. If a poll returns zero new unaddressed comments: end the polling loop and return the current evidence set to `cs River Orchestrator` for merge-readiness evaluation.
5. If the iteration cap is reached: stop and report the remaining unresolved
  threads for human review.

A freshness-breaking observation interrupts the current 300-second wait. `cs River Orchestrator`
MUST record the invalidation canonically and ensure the stale marker is published immediately, then resume or restart the
polling loop only after the required freshness recovery work is complete.

Poll waits and CI waits are `system-wait` time and MUST NOT count as active
agent time.

### Review Thread Handling

- Use GitHub MCP or GitHub CLI to read, reply to, and resolve threads.
- For each comment:
  - Read and understand it.
  - Determine if it is in scope.
  - If in scope: fix, commit, push, reply with evidence, resolve.
  - If out of scope: reply with reasoned explanation, leave open for reviewer.
- Resolving threads is **critical** — the PR cannot merge with open threads.
- One comment = one commit = one reply = one resolution.

## Handover Protocol

Every handover between agents **MUST** include:

1. **Context**: what has been done so far (file paths in `.thinking/`)
2. **Objective**: what the receiving agent must do
3. **Constraints**: what the receiving agent must not do
4. **Evidence**: relevant file paths, code references, test results
5. **Expected Output**: what the receiving agent must produce and where

Handovers are logged in `.thinking/<task>/handover-log.md`:

```markdown
## Handover: <from-agent> → <to-agent>
- **Time**: <ISO-8601 UTC>
- **Phase**: <current phase>
- **Context**: <summary + file paths>
- **Objective**: <what to do>
- **Output**: <expected result and location>
```

## Representative Walkthrough Metrics

These are contract-level before/after comparisons for the redesign, not runtime performance benchmarks.

| Flow | Before | After | Simplification signal |
|------|--------|-------|-----------------------|
| Direct governed intake | The previous governed orchestrator owned governed intake, wrote canonical facts, and also directly authored discovery synthesis while every governed specialist could append `activity-log.md`. | `cs River Orchestrator` owns governed intake, writes canonical facts, delegates discovery synthesis to `cs Discovery Synthesizer`, and is the sole direct `activity-log.md` writer. | One governed human-facing orchestrator remains, direct artifact ownership is explicit, and operational log ownership drops from many governed writers to one. |
| `cs Entrepreneur` → governed handoff | An approved Story Pack moved from `cs Entrepreneur` to the previous governed orchestrator, after which canonical ownership and supporting-log behavior diverged immediately because governed specialists could all append `activity-log.md`. | An approved Story Pack moves from `cs Entrepreneur` to `cs River Orchestrator`, which owns `workflow-audit/`, `state.json`, and `activity-log.md` from the first governed append onward. | The public handoff still uses one pre-governed step, but governed writer identity is singular from the first canonical event onward. |
| Phase 9 stale-summary recovery | The previous governed orchestrator coordinated invalidation, delegated PR-surface mutation, requested Scribe recompilation when needed, and evaluated merge readiness directly. | `cs River Orchestrator` coordinates invalidation, keeps stale-marker delegation alive, invokes `cs Scribe` for recompilation, invokes `cs PR Manager` for bounded PR-surface mutation, and invokes `cs Merge Readiness Evaluator` for the readiness artifact. | One canonical owner remains, while PR mutation, derived-audit compilation, and readiness evaluation are split into bounded leaves with one owned output each. |

## Scenario Coverage Matrix

The redesign is intended to stay easy to reason about during fresh starts, resumptions, and late-stage recovery.

| Scenario | Governing section(s) |
|----------|----------------------|
| Fresh governed intake through `cs River Orchestrator` | `Mandatory First Action`, `Phase 1: Intake & Discovery` |
| `cs Entrepreneur` handoff after G0 approval | `Public Entry Paths`, `Human Advancement Gates`, `Phase 1: Intake & Discovery` |
| Blocked/resume flow under River-owned canonical state | `Workflow Audit Contract`, `State File (state.json)`, `Phase 9: PR Creation & Merge Readiness` |
| Legacy-run reset / hard cutover for pre-cutover governed runs | `Hard Cutover for Legacy Governed Runs` |
| Reviewer-summary stale invalidation and republication | `Trust and Freshness Contract`, `Phase 9: PR Creation & Merge Readiness`, `Review Polling Rule in Phase 9` |
| Merge-readiness evaluation from current late-stage evidence | `Phase 9: PR Creation & Merge Readiness`, `Agent Roster` (`cs Merge Readiness Evaluator`) |

## Agent Roster (39 Agents)

This section is the single authoritative roster of approved Clean Squad
delegation targets. Any delegation term in this workflow, the shared Clean
Squad instruction, or the River Orchestrator prompt must resolve only to the named
agents in this roster. If no listed agent fits, `cs River Orchestrator` must stop,
record the blocker, and ask the user to either choose the nearest approved
Clean Squad agent, approve a roster or workflow change first, or explicitly
leave Clean Squad orchestration for that task.

### Public Intake (2)

| Agent | Role |
|-------|------|
| cs Entrepreneur | Optional public-facing pre-governed idea shaper; produces one Story Pack candidate and never opens governed workflow state |
| cs River Orchestrator | Direct governed intake path; sole governed orchestrator and direct workflow writer for the full SDLC |

### Discovery & Requirements (4)

| Agent | Role |
|-------|------|
| cs Requirements Analyst | Deep requirements analysis, gap identification |
| cs Discovery Synthesizer | Discovery evidence synthesizer; produces requirements synthesis from gathered intake evidence |
| cs Business Analyst | Business value, user needs, acceptance criteria |
| cs QA Analyst | Testability, edge cases, quality scenarios |

### Architecture (4)

| Agent | Role |
|-------|------|
| cs Tech Lead | Technical feasibility, risk assessment, architecture review |
| cs Solution Architect | Solution design, technology choices, component design |
| cs C4 Diagrammer | C4 architecture diagrams (Context, Container, Component) |
| cs ADR Keeper | Architecture Decision Records management |

### Planning (1)

| Agent | Role |
|-------|------|
| cs Plan Synthesizer | Synthesizes multi-persona review feedback |

### Workflow Synthesis (4)

| Agent | Role |
|-------|------|
| cs Three Amigos Synthesizer | Synthesizes business, technical, QA, and adoption perspectives into one bounded artifact |
| cs Code Review Synthesizer | Deduplicates and synthesizes code review findings into remediation-ready guidance |
| cs QA Synthesizer | Produces QA readiness conclusions from QA evidence and findings |
| cs Documentation Scope Synthesizer | Produces documentation scope and impact assessment from diff and task evidence |

### Implementation (3)

| Agent | Role |
|-------|------|
| cs Lead Developer | Production code, clean code, incremental implementation |
| cs Test Engineer | Tests, coverage, mutation testing |
| cs Commit Guardian | Commit-level review, enforces discipline |

### Code Review (5)

| Agent | Role |
|-------|------|
| cs Reviewer Pedantic | Line-by-line, naming, every minor detail |
| cs Reviewer Strategic | Architecture, design patterns, big-picture risks |
| cs Reviewer Security | OWASP, attack surface, trust boundaries |
| cs Reviewer DX | API ergonomics, developer experience |
| cs Reviewer Performance | Allocations, complexity, hot paths |

### Domain Experts (7)

| Agent | Role |
|-------|------|
| cs Expert CSharp | C# language idioms, runtime, type system |
| cs Expert Python | Python perspective, cross-ecosystem analysis |
| cs Expert Java | Java/enterprise patterns, type-safety perspective |
| cs Expert Serialization | JSON, wire formats, versioning, transport |
| cs Expert Cloud | Azure, AWS, cloud infrastructure, cost |
| cs Expert Distributed | Distributed systems, consensus, CAP theorem |
| cs Expert UX | User experience, accessibility, interaction design |

### Adoption (1)

| Agent | Role |
|-------|------|
| cs Developer Evangelist | Conference talks, competitive positioning, demo-ability, real-world adoption |

### QA (2)

| Agent | Role |
|-------|------|
| cs QA Lead | QA strategy, coverage analysis, shift-left advocacy |
| cs QA Exploratory | Exploratory testing, creative scenario discovery |

### DevOps (1)

| Agent | Role |
|-------|------|
| cs DevOps Engineer | CI/CD, pipelines, deployment, observability |

### Documentation (2)

| Agent | Role |
|-------|------|
| cs Technical Writer | Docusaurus docs authoring, evidence-backed pages |
| cs Doc Reviewer | Documentation accuracy, completeness, and navigation review |

### PR & Records (3)

| Agent | Role |
|-------|------|
| cs Scribe | Records thinking, decisions, reasoning, handovers |
| cs PR Manager | PR lifecycle, thread management, merge readiness |
| cs Merge Readiness Evaluator | Produces merge-readiness recommendation artifacts from current review, QA, docs, and PR evidence |

## Executable Customization Contract

The workflow remains the authoritative Clean Squad contract. The executable customization layer under `.github/agents/`, `.github/prompts/`, `.github/skills/`, and `.github/hooks/` exists to enforce and validate this workflow, not to replace it.

### Frontmatter parity rules

- `cs Entrepreneur` and `cs River Orchestrator` are the only user-invocable Clean Squad agents.
- `cs Entrepreneur` exposes the only public handoff, and that handoff targets `cs River Orchestrator` only, with explicit `send: false`.
- All internal Clean Squad agents use `user-invocable: false`.
- Approved phase coordinators use explicit `agents:` allowlists.
- Non-delegating Clean Squad agents use `agents: []`.
- `cs River Orchestrator` uses one explicit allowlist containing all approved internal Clean Squad agents and no non-Clean-Squad agents.

### Approved nested coordinators

Nested subagent waves are approved only for these bounded coordinators:

- `cs Three Amigos Synthesizer`
- `cs Code Review Synthesizer`
- `cs QA Synthesizer`
- `cs Documentation Scope Synthesizer`

All other Clean Squad agents remain flat delegators or non-delegators unless this workflow is explicitly changed first.

### Deterministic batch contract

Before River or an approved nested coordinator enables a parallel or nested wave, all of the following must hold:

1. One immutable batch or iteration ID exists.
2. One immutable input manifest records the delegated artifact set.
3. Every worker gets a unique output path or bundle path.
4. Every worker returns the same batch ID in its status envelope.
5. Sibling workers do not consume each other's in-flight outputs.
6. The expected worker roster is recorded before execution starts.
7. Every expected worker reaches one explicit terminal state: `succeeded`, `failed`, `cancelled`, or `timed out`.
8. Fan-in ordering follows declared roster order, not completion order.
9. Downstream work consumes one explicit synthesis or join artifact.
10. Partial synthesis is allowed only as an explicit degraded mode recorded by the active coordinator.
11. The active coordinator records the concurrency cap for the phase.
12. Nested coordinators record `parentBatchId`, `parentCoordinator`, and current depth.

### Skills, prompts, tool sets, and hooks

- Skills are reusable procedure packs only; governance authority remains here and in the agent contracts.
- Prompt files are bounded operator launchers; they do not create new public agents or bypass workflow gates.
- Tool sets are an ergonomic layer only. The current VS Code platform exposes them through profile-level `.toolsets.jsonc` files, so the repo ships a template and guidance rather than treating tool sets as a workspace authority surface.
- Hooks are a cautious preview pilot only. They must remain non-destructive by default and the workflow must remain operable when hooks are disabled, unavailable, or blocked by policy.

## Quality Bar

This system builds mission-critical applications to the highest enterprise
standard:

- No shortcuts.
- No quality compromises.
- Naming conventions matter.
- Developer experience matters.
- The "easier" approach is not chosen unless it is also the correct approach.
- Every decision is documented with reasoning.
- Every conclusion is verified through CoV.
- The goal is **exceptional, error-free output** — consistently.

## Workflow Diagram

The following Mermaid diagram is a visual companion to this workflow specification. Where the diagram and the prose differ, the prose sections above are authoritative.

```mermaid
flowchart TD
    Authority["Authority note:<br/>WORKFLOW.md is authoritative.<br/>This diagram is a visual companion only."]
    Principles["Cross-cutting note:<br/>All agents apply first principles and CoV.<br/>cs Entrepreneur is the optional pre-governed public intake agent.<br/>cs River Orchestrator is the sole governed orchestrator."]
    SharedState["Cross-cutting note:<br/>Governed agents share state through .thinking/&lt;task&gt;/.<br/>cs Entrepreneur is the explicit pre-governed exception and does not create or use this shared state.<br/>workflow-audit/ with immutable meta.json plus immutable seven-digit event files is the authoritative execution record.<br/>Activity-log and handover updates are mandatory secondary evidence."]
    AuditOwnership["Cross-cutting note:<br/>cs River Orchestrator writes canonical audit events for Phases 1-9.<br/>cs PR Manager executes only bounded delegated Phase 9 PR-surface work and does not write canonical facts.<br/>cs Scribe publishes derived audit output only."]
    AuditRules["Cross-cutting note:<br/>sequence is the only ordering authority.<br/>Canonical eventUtc timestamps are mandatory for timing and diagnostics only and never override sequence.<br/>Narrative logs, Mermaid, and PR prose are supporting or derived evidence only."]
    AuditDelegation["Cross-cutting note:<br/>Only one canonical writer may be active for the workflow run at a time.<br/>Phase 9 specialist work starts only after explicit cs River Orchestrator delegation that names the bounded task slice, a fresh expected artifact output or artifact bundle, completion signal, closure condition, details.allowedActions, and details.authorizedTargets without transferring canonical ownership.<br/>Delegated pass-back is file-first, and cs River Orchestrator validates returned artifact existence and containment before canonical completion.<br/>Blocked startup or recovery never transfers canonical ownership away from cs River Orchestrator.<br/>A bounded stale-marker delegation stays active while a fresh reviewer summary is published or a polling wait is active so stale publication has no integrity window.<br/>Every canonical append declares the expected prior sequence and fails closed on tail mismatch.<br/>The first canonical append uses sequence 1 with expected prior sequence 0."]
    AuditEventContract["Cross-cutting note:<br/>Every canonical event uses the v4 semantic envelope and is stored as one immutable event file: sequence, eventUtc, logicalEventId, actor, phase, eventType,<br/>appendPrecondition, workItemId and rootWorkItemId when meaningful, spanId, causedBy, closes, outcome, summary, reasonCode,<br/>artifacts as evidence bindings, artifactTransitions for lifecycle meaning, iterationId, provenance, and details."]
    StateSupportContract["Cross-cutting note:<br/>state.json is runtime support only and mirrors the workflow state contract exactly.<br/>It includes workflowContractFingerprint, currentSequence, currentOwner, openWait, and lastCompiledAtUtc.<br/>currentSequence matches the highest durable event file or 0 when only meta.json exists.<br/>currentOwner means canonical ownership only and never delegated execution ownership.<br/>It never repairs canonical facts from workflow-audit/."]
    TrustModel["Cross-cutting note:<br/>Reviewer-facing audit output is policy-authoritative and freshness-verifiable within this repo,<br/>but not tamper-resistant or authenticated.<br/>Evidence-bearing artifacts require content digests or immutable external identities before publication."]
    ProvenanceContract["Cross-cutting note:<br/>Every detailed audit artifact binds to HEAD SHA, ledger watermark, ledgerDigest, workflowContractFingerprint,<br/>generation timestamp, and generator identity.<br/>When merge readiness depends on CI, cs River Orchestrator verifies and attaches the current normalized required CI-result identity set before publication or directed PR-surface publication.<br/>Required CI identity changes alone do not force detailed-audit recompilation, and merge readiness never passes with stale, missing, or mismatched provenance."]
    VerdictContract["Cross-cutting note:<br/>Verdicts are Conformant, ConformantWithDeviations, NonConformant, Blocked, or Untrusted.<br/>Missing, malformed, or policy-violating evidence never yields a conformant verdict from current policy-authoritative evidence."]
    EvidenceContract["Cross-cutting note:<br/>Major completion claims require sufficient artifact evidence.<br/>Schema validation is required where the contract defines canonical or derived metadata structure.<br/>Supporting logs may corroborate but never repair missing canonical facts.<br/>Missing evidence maps to NonConformant, Blocked, or Untrusted based on policy violation, incomplete run, or trust failure."]
    TimingContract["Cross-cutting note:<br/>Timing uses elapsed, active-agent, human-wait, and system-wait totals derived from canonical eventUtc values.<br/>Buckets are mutually exclusive; unmatched or overlapping waits and impossible totals invalidate trust."]
    SummaryContract["Cross-cutting note:<br/>Reviewer Audit Summary order is verdict, action, blockers, provenance stamp, condensed Mermaid flow,<br/>four-bucket timing, fixed timing sentence, deviations, then pointer to workflow-audit.md.<br/>Keep only current blockers, the condensed topology, and at most three deviations on the PR surface; overflow detail lives in workflow-audit.md.<br/>workflow-audit.md begins with a why-this-matters opener."]
    FailureMatrix["Cross-cutting note:<br/>Failure matrix cases and outcomes are mirrored from WORKFLOW.md:<br/>happy path, human clarification wait, and Phase 9 polling wait -> Conformant and publish or keep published only with current provenance.<br/>remediation loop, allowed skip, and declined review comment -> ConformantWithDeviations and republish only when reviewer-facing deviations change with refreshed provenance.<br/>blocked run -> Blocked and no merge-ready summary is sufficient until cleared and regenerated.<br/>stale summary, unmatched wait boundary, overlapping waits, impossible timing totals, and malformed provenance -> Untrusted and invalidate immediately until corrected and regenerated.<br/>duplicate logicalEventId, invalid chronology or delegation-basis violation, and unauthorized writer or delegated actor -> NonConformant and no trusted reviewer evidence publishes until repaired and regenerated.<br/>missing major-claim evidence -> Blocked when incomplete, otherwise Untrusted; invalidate or withhold publication until evidence exists and a fresh summary is generated."]
    Delegation["Cross-cutting note:<br/>Before every runSubagent, verify the approved Agent Roster in WORKFLOW.md.<br/>If no approved fit exists, stop, record the blocker, and ask the user how to proceed."]

    subgraph EntryPoint["Public Intake"]
        User([User request]) -->|rough idea| Entrepreneur["cs Entrepreneur shapes one Story Pack candidate before governed intake"]
        User -->|direct governed intake| ProductOwner["cs River Orchestrator starts governed work and remains the sole governed orchestrator"]
        Entrepreneur --> StoryPack["Story Pack candidate"]
        StoryPack --> G0{"G0 Intake Gate<br/>Story Pack candidate"}
        G0 -->|APPROVED| ProductOwner
        G0 -->|NOT APPROVED| G0NotApproved["Stop or rework outside governed workflow<br/>(see G0 gate outcomes in WORKFLOW.md)"]
    end

    subgraph Phase1["Phase 1: Intake & Discovery"]
        P1Setup["Create .thinking/&lt;date&gt;-&lt;task-slug&gt;/, state.json, workflow-audit/ with meta.json and 0000001.json, activity-log.md, and 00-intake.md"]
        P1Classify["Classify intake against the governed intake decision table"]
        P1Ambiguous{"Partial or ambiguous?"}
        P1Qualify["Ask one qualification round for execution scope + discovery mode"]
        P1Default["Default full-run + autonomous-defaults"]
        P1Record["Record qualification or default outcome canonically"]
        P1Mode{"Discovery mode"}
        P1Manual["Ask 5 human discovery questions and record questions-round-NN.md"]
        P1ManualClear{"Requirements sufficiently clear?"}
        P1Analyze["Invoke cs Requirements Analyst for gap analysis and the next 5 questions"]
        P1Auto["Invoke cs Requirements Analyst for bounded autonomous discovery defaults"]
        P1AutoPublish["Write questions-round-NN.md with trust tier, provenance, confidence, and requiresHumanConfirmation"]
        P1AutoClear{"Requirements sufficiently clear or autonomous bound reached?"}
        P1Synthesis["Invoke cs Discovery Synthesizer to write 01-discovery/requirements-synthesis.md"]

        P1Setup --> P1Classify --> P1Ambiguous
        P1Ambiguous -- No --> P1Default --> P1Record --> P1Mode
        P1Ambiguous -- Yes --> P1Qualify --> P1Record --> P1Mode
        P1Mode -- manual-refinement --> P1Manual --> P1ManualClear
        P1ManualClear -- No --> P1Analyze --> P1Manual
        P1ManualClear -- Yes --> P1Synthesis
        P1Mode -- autonomous-defaults --> P1Auto --> P1AutoPublish --> P1AutoClear
        P1AutoClear -- No --> P1Auto
        P1AutoClear -- Yes --> P1Synthesis
    end

    subgraph Phase2["Phase 2: Three Amigos + Adoption"]
        P2Invoke["Invoke cs Business Analyst, cs Tech Lead, cs QA Analyst, and cs Developer Evangelist one at a time"]
        P2Outputs["Each sub-agent writes its perspective document"]
        P2Synthesis["Invoke cs Three Amigos Synthesizer to write 02-three-amigos/synthesis.md"]
        P2Gaps{"Critical gaps identified?"}
        P2Questions["Ask the user additional questions before proceeding"]

        P2Invoke --> P2Outputs --> P2Synthesis --> P2Gaps
        P2Gaps -- Yes --> P2Questions
    end

    subgraph Phase3["Phase 3: Architecture & Design"]
        P3Architect["Invoke cs Solution Architect"]
        P3Design["Produce 03-architecture/solution-design.md"]
        P3C4["Invoke cs C4 Diagrammer"]
        P3Diagrams["Produce Context and Container diagrams, plus Component or omission rationale"]
        P3Adr["Invoke cs ADR Keeper"]

        P3Architect --> P3Design --> P3C4 --> P3Diagrams --> P3Adr
    end

    subgraph Phase4["Phase 4: Planning & Review Cycles"]
        P4Draft["Combine outputs into 04-planning/draft-plan-v1.md"]
        P4Review["Invoke approved planning reviewers from the Agent Roster"]
        P4Feedback["Each reviewer reads the plan and produces feedback"]
        P4Synth["Invoke cs Plan Synthesizer to categorize feedback"]
        P4Revise["Revise the plan"]
        P4More{"More review cycles needed?"}
        P4Final["Write 04-planning/final-plan.md"]

        P4Draft --> P4Review --> P4Feedback --> P4Synth --> P4Revise --> P4More
        P4More -- Yes --> P4Review
        P4More -- No --> P4Final
    end

    subgraph Phase5["Phase 5: Implementation"]
        P5Branch["Create a feature branch from main"]
        P5Lead["Invoke cs Lead Developer with the next slice of work"]
        P5Code["cs Lead Developer writes a small, focused increment"]
        P5Tests["Invoke cs Test Engineer to write or validate tests"]
        P5Build["Run the build and verify zero warnings"]
        P5RunTests["Run tests and verify they pass"]
        P5Guard["Invoke cs Commit Guardian"]
        P5Issues{"cs Commit Guardian issues found?"}
        P5Remediate["cs Lead Developer remediates cs Commit Guardian findings in the current increment"]
        P5Commit["Commit with a scoped message and record increment artifacts"]
        P5More{"More plan items to implement?"}
        P5Full["After all increments, run the full build, full tests, and mutation tests if Mississippi"]

        P5Branch --> P5Lead --> P5Code --> P5Tests --> P5Build --> P5RunTests --> P5Guard --> P5Issues
        P5Issues -- Yes --> P5Remediate --> P5Tests
        P5Issues -- No --> P5Commit --> P5More
        P5More -- Yes --> P5Lead
        P5More -- No --> P5Full
    end

    subgraph Phase6["Phase 6: Comprehensive Code Review"]
        P6Diff["Use git diff main...HEAD to identify changed files"]
        P6Review["Invoke cs Reviewer Pedantic, cs Reviewer Strategic, cs Reviewer Security, cs Reviewer DX, cs Reviewer Performance, and cs Developer Evangelist in sequence"]
        P6Experts["Invoke relevant approved domain experts from the Agent Roster"]
        P6Synthesis["Invoke cs Code Review Synthesizer to deduplicate and prioritize all review output"]
        P6Findings{"Review findings remain?"}
        P6Remediate["Fix each finding or document why it was declined"]

        P6Diff --> P6Review --> P6Experts --> P6Synthesis --> P6Findings
        P6Findings -- Yes --> P6Remediate --> P6Review
    end

    subgraph Phase7["Phase 7: QA Validation"]
        P7Lead["Invoke cs QA Lead to review test strategy and coverage"]
        P7Exploratory["Invoke cs QA Exploratory"]
        P7Mutation["Invoke cs Test Engineer for mutation testing validation"]
        P7Synthesis["Invoke cs QA Synthesizer to write 07-qa/qa-readiness.md"]
        P7Gaps{"QA gaps identified?"}
        P7Remediate["Feed QA gaps back for remediation of the current increment"]

        P7Lead --> P7Exploratory --> P7Mutation --> P7Synthesis --> P7Gaps
        P7Gaps -- Yes --> P7Remediate
    end

    subgraph Phase8["Phase 8: Documentation"]
        P8Scope["Invoke cs Documentation Scope Synthesizer to assess documentation scope from the branch diff and .thinking artifacts"]
        P8UserFacing{"User-facing changes exist?"}
        P8Skip["Record the documentation skip reason in scope-assessment.md"]
        P8Writer["Invoke cs Technical Writer"]
        P8Drafts["Create the evidence map, classify page types, draft pages, and publish verified pages"]
        P8Review["Run the documentation review cycle with cs Doc Reviewer and cs Developer Evangelist"]
        P8MustFix{"cs Doc Reviewer Must Fix findings remain?"}
        P8Fixes["Re-invoke cs Technical Writer for each Must Fix or Should Fix finding and record remediation"]
        P8Validate["Validate the documentation quality gates"]

        P8Scope --> P8UserFacing
        P8UserFacing -- No --> P8Skip
        P8UserFacing -- Yes --> P8Writer --> P8Drafts --> P8Review --> P8MustFix
        P8MustFix -- Yes --> P8Fixes --> P8Review
        P8MustFix -- No --> P8Validate
    end

    subgraph Phase9["Phase 9: PR Creation & Merge Readiness"]
        P9Manager["cs River Orchestrator remains the canonical Phase 9 owner and records every reviewer-significant Phase 9 fact"]
        P9Startup["Record a bounded delegation to cs PR Manager that names the bounded task slice, a fresh details.expectedOutputPath, completion signal, closure condition, details.allowedActions, and details.authorizedTargets; blocked startup or recovery never transfers ownership"]
        P9Scribe["cs River Orchestrator invokes cs Scribe when HEAD, ledger, workflow contract, or reviewer-meaningful canonical facts require a fresh stable-snapshot audit"]
        P9Execute["cs PR Manager executes only the delegated PR-surface work and returns artifacts and evidence"]
        P9Stale["At first observation, cs River Orchestrator records invalidation canonically and the continuously delegated stale-marker capability marks the PR surface stale immediately; do not let the 300-second wait delay invalidation"]
        P9Verify["cs River Orchestrator verifies workflow-audit provenance plus the current required CI identity set before republication or merge-readiness evaluation"]
        P9Publish["cs River Orchestrator decides publication or republication; cs PR Manager applies the PR-surface mutation only when delegated"]
        P9Wait["After pushing to an open PR, wait 300 seconds unless stale invalidation work preempts the wait"]
        P9Poll["Poll for unresolved review comments"]
        P9Comments{"New unaddressed comments?"}
        P9Scope{"Comment is in scope?"}
        P9Address["Fix, commit, push, reply with evidence, and resolve the thread"]
        P9OutOfScope["Reply with reasoned explanation and leave the thread open for the reviewer"]
        P9Record["cs River Orchestrator records the resulting canonical fact, closes or reissues the delegation, and decides whether publication or merge readiness changed"]
        P9Cap{"Iteration cap reached?"}
        P9Ready["cs River Orchestrator invokes cs Merge Readiness Evaluator and evaluates current evidence canonically"]
        G3{"G3 Merge Gate<br/>Merge-readiness package + rolled-up review, QA, and documentation conclusions"}
        P9Stop(["Stop and report remaining unresolved threads for human review"])
        P9Done([Done])

        P9Manager --> P9Startup --> P9Scribe --> P9Execute --> P9Stale --> P9Verify --> P9Publish --> P9Wait --> P9Poll --> P9Comments
        P9Comments -- Yes --> P9Scope
        P9Scope -- Yes --> P9Address --> P9Record --> P9Cap
        P9Scope -- No --> P9OutOfScope --> P9Record --> P9Cap
        P9Cap -- No --> P9Wait
        P9Cap -- Yes --> P9Stop
            P9Comments -- No --> P9Ready --> G3
            G3 --|APPROVED| P9Done
        G3 --|CHANGES_REQUESTED| ProductOwner
        G3 --|DEFERRED or CANCELLED| Stop3(["Stop or hold after late-stage review"])
    end

    G1{"G1 Scope Gate<br/>requirements-synthesis.md + synthesis.md"}
    G2{"G2 Plan Gate<br/>solution-design.md + binding C4/ADR artifacts + final-plan.md"}

    P2Gaps -- No --> G1
    P2Questions --> G1
    G1 --|APPROVED| P3Architect
    G1 --|CHANGES_REQUESTED| ProductOwner
    G1 --|DEFERRED or CANCELLED| Stop1(["Stop or hold after scope review"])
    P1Synthesis --> P2Invoke
    P3Adr --> P4Draft
    P4Final --> G2
    G2 --|APPROVED| P5Branch
    G2 --|CHANGES_REQUESTED| ProductOwner
    G2 --|DEFERRED or CANCELLED| Stop2(["Stop or hold before implementation"])
    P5Full --> P6Diff
    P6Findings -- No --> P7Lead
    P7Remediate --> P5Tests
    P7Gaps -- No --> P8Scope
    P8Skip --> P9Manager
    P8Validate --> P9Manager

    Authority -.-> ProductOwner
    Principles -.-> ProductOwner
    SharedState -.-> P1Setup
    AuditOwnership -.-> ProductOwner
    AuditDelegation -.-> P9Manager
    AuditEventContract -.-> P1Setup
    StateSupportContract -.-> P1Setup
    TrustModel -.-> P9Publish
    ProvenanceContract -.-> P9Scribe
    VerdictContract -.-> P9Ready
    EvidenceContract -.-> P9Scribe
    TimingContract -.-> P9Wait
    SummaryContract -.-> P9Scribe
    FailureMatrix -.-> P9Manager
    AuditRules -.-> P9Manager
    Delegation -.-> ProductOwner
```
