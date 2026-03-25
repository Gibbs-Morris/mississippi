# Clean Squad: End-to-End SDLC Workflow

The Clean Squad is a family of 32 GitHub Copilot agents that takes an idea from
initial request through to a merge-ready pull request. There is exactly one
entry point — the **cs Product Owner** — who orchestrates all work by delegating
to specialist sub-agents. Every agent applies first-principles thinking and
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

## Entry Point

`@cs Product Owner` is the **only** agent the human user invokes. All other
agents are sub-agents invoked programmatically via `runSubagent`. All Clean
Squad delegation **MUST** target only approved Clean Squad agents named in the
`Agent Roster` section of this workflow. The user never needs to invoke any
other agent directly.

If no approved Clean Squad agent fits a task, the Product Owner **MUST** stop,
record the blocker, and ask the user to either choose the nearest approved
Clean Squad agent, approve a roster or workflow change first, or explicitly
leave Clean Squad orchestration for that task.

## Shared State: The `.thinking` Folder

All agents share state through a filesystem folder:

```text
.thinking/
  <YYYY-MM-DD>-<task-slug>/        # One subfolder per task
    state.json                      # Workflow state (current phase, status)
    workflow-audit.json             # Canonical append-only execution ledger
    workflow-audit.md               # Derived detailed workflow audit report
    activity-log.md                 # Start/progress/blocker/completion log
    00-intake.md                    # Initial request & context
    01-discovery/
      questions-round-01.md         # First group of 5 questions + answers
      questions-round-02.md         # Next group + answers (adaptive)
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

Published ADRs live outside `.thinking/` under `docs/Docusaurus/docs/adr/` so
they remain part of the long-term documentation set after the task folder is
retired.

### Operational Logging Protocol

- Every Clean Squad agent MUST append an entry to `.thinking/<task>/activity-log.md` before substantive work starts.
- Every Clean Squad agent MUST append another entry after each material decision, delegation, blocker, or phase transition.
- Every Clean Squad agent MUST append a final entry before returning control, capturing outputs produced, status, blockers, and next action.
- The Product Owner MUST treat this log as mandatory operational telemetry, not an optional summary.
- Activity log entries SHOULD use a consistent structure: UTC timestamp, actor, phase, action, artifacts updated, blockers, and next action.

### Workflow Audit Contract

#### Canonical Artifacts and Authority

- `.thinking/<task>/workflow-audit.json` is the authoritative execution record for one Clean Squad run.
- `.thinking/<task>/workflow-audit.md` is a derived detailed audit compiled from a stable ledger snapshot.
- `09-pr-merge/pr-description.md` contains the derived `Reviewer Audit Summary` and MUST source it from current policy-authoritative audit inputs with matching freshness and evidence bindings.
- `sequence` is the only ordering authority for canonical workflow facts.
- Canonical `eventUtc` timestamps are mandatory for timing and diagnostics, but MUST NOT override `sequence` for chronology.
- `activity-log.md`, `handover-log.md`, Mermaid output, PR prose, and other narrative files are supporting or derived evidence only and MUST NOT override canonical sequence facts.
- Within this repository today, `workflow-audit.json` and its derived audit artifacts are policy-authoritative and freshness-verifiable, but they are NOT tamper-resistant, cryptographically authenticated, or proof of actor identity beyond the declared Clean Squad role recorded in the ledger.

#### Active Writer and Delegation Invariants

- The Product Owner writes canonical events for Phases 1 through 9.
- The PR Manager MUST NOT write canonical workflow facts and MAY execute only explicitly delegated, bounded Phase 9 specialist work.
- The Scribe MUST NOT write canonical workflow facts.
- Only one canonical writer may be active for the workflow run at a time.
- Every active Phase 9 PR Manager execution slice MUST begin with explicit Product Owner delegation whose `workItemId` names the bounded task slice and whose `details` name the expected artifact output or artifact bundle, completion signal, closure condition, `details.allowedActions`, and `details.authorizedTargets`.
- Stale-marker authority in Phase 9 MUST remain continuously delegated whenever a fresh `Reviewer Audit Summary` is published or a review-polling wait is active; that bounded stale-marker delegation MUST stay active until the Product Owner canonically records that the summary is stale, republished fresh, or no longer present on the PR surface.
- A Phase 9 delegation remains active only until the Product Owner records a later canonical event for the same `workItemId` whose `causedBy.logicalEventId` references that delegation and whose semantics satisfy its declared completion signal or closure condition.
- Blocked Phase 9 startup, tool acquisition, or recovery MUST NOT transfer canonical ownership away from the Product Owner.
- A materially new PR-surface objective in Phase 9 MUST use a new bounded delegation; Phase 9 delegation MUST NOT become umbrella authority.
- Every canonical append MUST declare the expected prior `sequence`.
- A canonical writer MUST fail closed if the ledger tail does not match the declared expected prior `sequence`.
- The first canonical append MUST write `sequence = 1` and declare expected prior `sequence = 0`.
- Recovery MUST rebuild operational state from `workflow-audit.json`, never the reverse.

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

#### Normative Canonical JSON Contract

`workflow-audit.json` MUST use this top-level shape:

```json
{
  "schemaVersion": "clean-squad-workflow-audit/v3",
  "workflowContractFingerprint": "<sha256 of UTF-8 bytes of .github/clean-squad/WORKFLOW.md>",
  "ledgerDigestAlgorithm": "sha256-canonical-json-v1",
  "events": []
}
```

Canonical JSON rules:

- Files MUST be UTF-8 encoded JSON with no comments or trailing commas.
- Canonical JSON MUST NOT include a UTF-8 BOM.
- Canonical JSON MUST NOT contain insignificant whitespace outside JSON string values; emit the digest form as minified JSON with no spaces, tabs, or line breaks between tokens.
- Canonical JSON MUST NOT end with a trailing newline; the digest is computed over the exact UTF-8 byte sequence of the canonical JSON text.
- Object property order MUST match the order shown in this contract wherever a digest is calculated.
- Nested object property order MUST match the order shown in this contract for `appendPrecondition`, `causedBy`, `closes`, `artifacts[]`, `artifactTransitions[]`, and `provenance`.
- Arrays MUST preserve the semantic order required by this contract; `events` remain ordered by `sequence`, and nested arrays keep the writer-emitted order unless this contract defines an explicit normalization rule.
- Every canonical event MUST emit the full top-level property set in the declared order even when some conditional semantics are absent.
- `appendPrecondition` MUST always be serialized and MUST encode the expected prior `sequence` used for fail-closed append validation.
- When a conditional scalar or object field is not semantically required, canonical JSON MUST emit that property as `null` rather than omitting it.
- When a conditional array field is not semantically required, canonical JSON MUST emit that property as `[]` rather than omitting it.
- When `details` has no event-type-specific members, canonical JSON MUST emit `details: {}`.
- `schemaVersion = clean-squad-workflow-audit/v3` is the current normative schema. Earlier schema versions remain historical snapshots and MUST NOT have missing v3 semantics or timing backfilled from secondary logs.
- `events` MUST be ordered by `sequence` ascending with no duplicate or skipped sequence values.
- `ledgerDigestAlgorithm = sha256-canonical-json-v1` means SHA-256 over the UTF-8 bytes of the canonical JSON form defined by these rules for the exact snapshot used for comparison or compilation.

Each canonical event MUST use this property order and shape:

```json
{
  "sequence": 1,
  "eventUtc": "2026-03-25T00:00:00.0000000Z",
  "logicalEventId": "phase-03-start",
  "actor": "cs Product Owner",
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
    "recordedBy": "cs Product Owner",
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
- Timing buckets MUST be derived only from canonical `eventUtc` values and explicit wait boundaries in `workflow-audit.json`.

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
- `completed` MUST be used only for terminal bounded work items that are not whole phases and not the overall workflow run, such as delegated review passes, remediation slices, validation attempts, commit creation, or PR-slice completion.
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
- `role` MUST describe the artifact purpose using a stable noun phrase such as `phase-output`, `review-synthesis`, `pr-description`, `thread-log`, `quality-gate-evidence`, or `scope-assessment`.
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
  "recordedBy": "cs Product Owner",
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

- `details` is required on every canonical event and MAY contain only event-type-specific keys defined by this contract. It MUST NOT be used to encode append-precondition semantics because those semantics live in `appendPrecondition`. When no event-type-specific keys apply, `details` MUST be `{}`:

 - `delegation-recorded`: `delegatedAgent`, `expectedOutputPath`, `completionSignal`, `closureCondition`, `allowedActions`, `authorizedTargets`
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
- `details.expectedOutputPath` MUST name the artifact output or artifact bundle the delegation authorizes.
- `details.completionSignal` MUST name the canonical evidence or event pattern the Product Owner expects to treat the delegated slice as successfully handed back.
- `details.closureCondition` MUST name the canonical condition that ends the delegation, including successful completion, block, cancellation, or supersession.
- `details.allowedActions` MUST enumerate the exact mutation classes and Phase 9 operations authorized within that delegation, using stable values such as `stale-marker`, `reviewer-summary-publish`, `thread-reply`, `thread-resolve`, `pr-description-update`, `ci-evidence-read`, or `poll-review-comments`.
- `details.authorizedTargets` MUST enumerate the exact resources the delegation covers, such as the PR number, summary section, freshness stamp, thread IDs, or CI identity set for the current HEAD SHA.
- Delegation validation MUST reject returned evidence for any action or target outside `details.allowedActions` and `details.authorizedTargets`, even when the delegated artifact bundle otherwise looks complete.
- The Phase 9 stale-marker delegation MUST be its own bounded capability slice whose `details.allowedActions` contains only `stale-marker` and whose `details.authorizedTargets` are limited to the current PR and reviewer-summary freshness marker.
- A Phase 9 delegation remains active only until the Product Owner records a later canonical event for the same `workItemId` whose `causedBy.logicalEventId` references that `delegation-recorded` event and whose semantics satisfy the recorded `completionSignal` or `closureCondition`. After that closure, any further PR Manager work MUST use a new `delegation-recorded` event.

Allowed `details.publicationState` values:

| Value | Meaning |
|-------|---------|
| `stale` | The previously published reviewer summary is no longer fresh for the current canonical facts or CI identity |
| `fresh` | The currently published reviewer summary is verified as current for the canonical facts and attached CI identity |

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
- Canonical meaning MUST NOT be inferred from prose, sequence order, secondary logs, or evidence bindings alone when structured v3 semantics are missing.

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

`state.json` is operational support state only. It MAY cache audit cursor data, but it MUST NOT replace or repair canonical facts from `workflow-audit.json`.

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
  "workflowContractFingerprint": "<same value used by workflow-audit.json>",
  "audit": {
    "currentSequence": 0,
    "currentOwner": "cs Product Owner|null",
    "openWait": null,
    "lastCompiledAtUtc": null
  },
  "discoveryRound": 0,
  "planReviewCycle": 0,
  "implementationIncrement": 0,
  "adrCount": 0
}
```

`state.json.audit.currentOwner` means canonical ownership only. For in-progress workflow states it MUST be `cs Product Owner`. `null` is allowed only when support state is absent or uninitialized, and `currentOwner` MUST NOT represent delegated execution ownership.

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
- The Product Owner verifies `workflow-audit.md` provenance and attaches or verifies the current normalized required CI-result identity set before publishing or directing publication of the `Reviewer Audit Summary`.
- The PR Manager MAY execute the PR-surface publication or stale-marker mutation only under explicit Product Owner delegation and MUST return the resulting evidence.
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

1. The Product Owner MUST invalidate immediately at first observation of a relevant change, including during any active 300-second review-polling wait, by recording the canonical invalidation fact and ensuring the `Reviewer Audit Summary` is marked stale on the PR surface with the stale reason and the last known freshness stamp. The 300-second review-polling wait MUST NOT delay stale-marker publication.
2. The Product Owner MUST maintain an active bounded stale-marker delegation for the current PR surface whenever a fresh `Reviewer Audit Summary` is published or a review-polling wait is active so the PR Manager can apply the stale marker immediately at first observation without waiting for a new delegation round-trip.
3. If HEAD SHA, the stable ledger snapshot, `workflowContractFingerprint`, or reviewer-meaningful canonical output changes, the Product Owner MUST obtain a fresh `workflow-audit.md` compilation from cs Scribe using a new stable ledger snapshot before republishing reviewer-facing audit output.
4. If only the required CI-result identity set changes for an unchanged HEAD SHA and unchanged reviewer-meaningful canonical facts, the Product Owner MUST refresh the `Reviewer Audit Summary` freshness stamp and merge-readiness evaluation without recompiling `workflow-audit.md`.
5. The Product Owner MUST verify that the regenerated or reused `workflow-audit.md` provenance matches the current HEAD SHA, ledger watermark, `ledgerDigest`, and `workflowContractFingerprint`, and that the attached normalized required CI-result identity set is current, before republishing.
6. The Product Owner MUST republish only when reviewer-meaningful content changes or merge-readiness validation requires a fresh publication. The PR Manager MAY apply the PR-surface update only under bounded delegation whose `allowedActions` and `authorizedTargets` cover that specific mutation.
7. Invalidation MUST be more granular than publication so the PR description does not churn on low-signal changes.

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

1. Timing buckets MUST be derived only from canonical `eventUtc` values in `workflow-audit.json`.
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

When freshness is broken, the PR surface MUST show a stale marker in place of a merge-ready reviewer summary until the Product Owner directs a fresh republication.

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

## Product Owner Execution Boundary

The Product Owner is an orchestrator, not an implementation agent.

- The Product Owner MUST ask the user questions, sequence the workflow, update shared state, synthesize sub-agent outputs, and enforce quality gates.
- The Product Owner MUST use `runSubagent` for specialist work including analysis, design, coding, testing, code review, QA validation, documentation, and PR management.
- Before every `runSubagent` call, the Product Owner MUST validate that the selected agent is explicitly named in the `Agent Roster` section of this workflow.
- Generic categories such as review personas and domain experts MUST resolve only to named agents in the `Agent Roster` section of this workflow.
- If no approved Clean Squad agent clearly fits, the Product Owner MUST stop, record the blocker, and ask the user to either choose the nearest approved Clean Squad agent, approve a roster or workflow change first, or explicitly leave Clean Squad orchestration for that task.
- The Product Owner MUST NOT bypass a specialist sub-agent by performing that specialist work directly.

### State File (`state.json`)

`state.json` MUST match the `State and Runtime Support Contract` above exactly. The example above is normative; duplicate or stale variants in agent prompts are invalid.

## Phase 1: Intake & Discovery

**Owner**: cs Product Owner (drives conversation with human user)
**Sub-agents**: cs Requirements Analyst

### Process

1. User describes their idea to the Product Owner.
2. Product Owner creates `.thinking/<date>-<slug>/` and writes `00-intake.md`.
3. Product Owner asks the first group of **5 questions** (using built-in
   persona knowledge — business, technical, QA perspectives).
4. After the user answers, Product Owner records answers and invokes
   **cs Requirements Analyst** to analyze gaps and suggest the next 5 questions.
5. Product Owner asks the next 5 questions to the user.
6. Repeat until requirements are sufficiently clear (Product Owner decides,
   typically 3-6 rounds = 15-30 questions).
7. Product Owner writes `requirements-synthesis.md`.

### Adaptive Questioning Rules

- Questions **MUST** be grouped in sets of exactly 5.
- After each set, the next 5 **MUST** be informed by answers already received.
- Questions **SHOULD** start broad (business value, user needs) and progressively
  narrow (technical constraints, edge cases, quality expectations).
- If the user is highly technical and mentions code quality, subsequent questions
  **MUST** reflect that (architecture patterns, testing strategy, naming).
- If the user is non-technical, questions **MUST** use plain language and focus
  on outcomes rather than implementation.
- Each question **MUST** include ranked options (A, B, C...) plus
  **(X) I don't care — pick the best default**.

## Phase 2: Three Amigos + Adoption

**Owner**: cs Product Owner
**Sub-agents**: cs Business Analyst, cs Tech Lead, cs QA Analyst, cs Developer Evangelist

### Process

1. Product Owner invokes each perspective sub-agent with the requirements
   synthesis, one at a time.
2. Each sub-agent reads requirements and produces their perspective document.
3. Product Owner writes `02-three-amigos/synthesis.md` combining all four.
4. If any sub-agent identifies critical gaps, Product Owner asks the user
   additional questions before proceeding.

### Perspective Outputs

| Perspective | Agent | Focus |
|-------------|-------|-------|
| Product | cs Business Analyst | User value, acceptance criteria, business rules |
| Development | cs Tech Lead | Technical feasibility, risks, architecture constraints |
| Quality | cs QA Analyst | Test strategy, edge cases, failure scenarios |
| Adoption | cs Developer Evangelist | Demo-ability, competitive positioning, conference potential, real-world relevance |

## Phase 3: Architecture & Design

**Owner**: cs Product Owner
**Sub-agents**: cs Solution Architect, cs C4 Diagrammer, cs ADR Keeper

### Process

1. Product Owner invokes **cs Solution Architect** with synthesized requirements.
2. Solution Architect produces `solution-design.md` with technology choices,
   component design, and integration points.
3. Product Owner invokes **cs C4 Diagrammer** to produce C4 model diagrams
  (Context and Container always; Component when a container has meaningful internal structure, otherwise an explicit omission rationale).
4. For each significant architectural decision, Product Owner invokes
  **cs ADR Keeper** to produce ADRs in `docs/Docusaurus/docs/adr/`.
5. Product Owner may invoke approved domain experts from the `Agent Roster`
   section (for example cs Expert Distributed, cs Expert Cloud, and
   cs Expert Serialization) for specialist input on architecture.

### ADR Protocol

- Every significant decision **MUST** be recorded as an ADR.
- ADRs **MUST** use the MADR 4.0.0 template defined in `.github/instructions/adr.instructions.md`.
- ADRs **MUST** be published to `docs/Docusaurus/docs/adr/` using the filename pattern `NNNN-title-with-dashes.md`.
- When a feature branch adds ADRs, the branch owner **MUST** treat those numbers as provisional and perform a final renumbering pass against the latest `main` during merge preparation, updating filenames, `ADR-NNNN` titles, `sidebar_position`, and relative ADR links for ADRs introduced by that branch.
- ADRs are immutable — superseded decisions get a new ADR referencing the old.
- ADRs **MUST** be consulted on subsequent changes to verify directional
  alignment.

## Phase 4: Planning & Review Cycles

**Owner**: cs Product Owner
**Sub-agents**: cs Plan Synthesizer, approved review personas from the Agent Roster

### Process

1. Product Owner combines architecture, requirements, and Three Amigos output
   into `draft-plan-v1.md`.
2. Product Owner runs **review cycle 1** by invoking each approved review
  persona from the `Agent Roster` section as a sub-agent.
3. Each reviewer reads the plan and produces feedback.
4. Product Owner invokes **cs Plan Synthesizer** to deduplicate and categorize
  feedback (Must / Should / Could / Won't).
5. Product Owner revises the plan.
6. Repeat for **3-5 review cycles** total.
7. After final cycle, Product Owner writes `final-plan.md`.

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

**Owner**: cs Product Owner
**Sub-agents**: cs Lead Developer, cs Test Engineer, cs Commit Guardian

### Process

1. Product Owner creates a feature branch from `main`.
2. For each increment:
   a. Product Owner invokes **cs Lead Developer** with the next slice of work
      from the plan.
   b. Lead Developer writes production code (small, focused increment).
  c. Lead Developer performs semantic consistency review for touched code
    elements (types or members),
    updates stale comments or XML documentation when needed, and records the
    reviewed-member evidence in `changes.md`.
  d. Product Owner invokes **cs Test Engineer** to write/validate tests and
    independently verify semantic consistency for touched code elements
    against the changed behavior, recording the result in `test-results.md`.
  e. Build is run and verified clean (zero warnings).
  f. Tests are run and verified passing.
  g. Product Owner invokes **cs Commit Guardian** to review the final staged
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

**Owner**: cs Product Owner
**Sub-agents**: Approved review personas and approved domain experts from the Agent Roster

### Process

1. Product Owner uses `git diff main...HEAD` to identify all changed files.
2. Product Owner invokes review personas in sequence:

   | Priority | Agent | Style |
   |----------|-------|-------|
   | 1 | cs Reviewer Pedantic | Line-by-line, naming, every detail |
   | 2 | cs Reviewer Strategic | Architecture, design patterns, big picture |
   | 3 | cs Reviewer Security | OWASP, attack surface, trust boundaries |
   | 4 | cs Reviewer DX | API ergonomics, discoverability, pit of success |
   | 5 | cs Reviewer Performance | Allocations, complexity, hot paths |
   | 6 | cs Developer Evangelist | Demo-ability, shareability, competitive positioning (public API changes) |

3. Product Owner invokes relevant approved domain experts from the `Agent Roster` section based on the change type.
4. Product Owner synthesizes all review output.
5. For each finding: fix it or document why it was declined.
6. Iterate until all reviewers are satisfied.

The phrases review personas and domain experts in this workflow refer only to
the named agents in the `Agent Roster` section below. They do not authorize
delegation to other repo agents.

### Review Coverage Rule

Every changed file **MUST** be reviewed by at least the Pedantic and Strategic
reviewers. Domain experts review files within their expertise.

## Phase 7: QA Validation

**Owner**: cs Product Owner
**Sub-agents**: cs QA Lead, cs QA Exploratory, cs Test Engineer

### Process

1. Product Owner invokes **cs QA Lead** to review test strategy and coverage.
2. Product Owner invokes **cs QA Exploratory** to apply exploratory testing
   perspective.
3. Product Owner invokes **cs Test Engineer** for mutation testing (Mississippi
   projects only).
4. Any gaps identified are fed back to implementation.

## Phase 8: Documentation

**Owner**: cs Product Owner
**Sub-agents**: cs Technical Writer, cs Doc Reviewer, cs Developer Evangelist

### Purpose

Ensure that every user-facing change is accompanied by accurate, evidence-backed
Docusaurus documentation before the PR is created. Documentation is a first-class
deliverable, not an afterthought.

### Process

1. Product Owner assesses documentation scope:
   - Run `git diff --name-status --find-renames main...HEAD` to identify all
     changed source files.
   - Identify new public APIs, changed behavior, new concepts, and affected
     existing doc pages.
   - If no user-facing changes exist (pure refactors, internal-only changes),
     record the skip reason in `.thinking/<task>/08-documentation/scope-assessment.md`
     and proceed to Phase 9.

2. Product Owner invokes **cs Technical Writer** to create/update documentation:
   - The writer reads all `.thinking/<task>/` artifacts and the branch diff.
   - The writer builds an evidence map, classifies page types, and drafts pages.
   - Draft pages are written to `.thinking/<task>/08-documentation/drafts/`.
   - Verified pages are published to `docs/Docusaurus/docs/`.

3. Product Owner runs a **documentation review cycle** (repeat 1-3 times):
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

4. Product Owner validates documentation quality gates:
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

**Owner**: cs Product Owner
**Entry condition**: Phase 8 is complete and any required Phase 9 delegation basis is recorded
**Sub-agents**: cs PR Manager, cs Scribe

### Process

1. The Product Owner remains the canonical Phase 9 owner and MUST record every reviewer-significant Phase 9 fact in `workflow-audit.json`.
2. The Product Owner delegates only bounded Phase 9 PR-surface specialist work to **cs PR Manager** and MUST NOT give open-ended Phase 9 umbrella authority; every such delegation MUST be capability-scoped through explicit `allowedActions` and `authorizedTargets`.
3. At Phase 9 startup or recovery, if PR-surface work cannot begin normally, the Product Owner records the blocked or resumed state canonically without transferring ownership.
4. The Product Owner invokes **cs Scribe** to compile `workflow-audit.md` and the condensed reviewer-flow inputs from a stable `workflow-audit.json` snapshot when Phase 9 audit compilation or recompilation is required.
5. The PR Manager creates or updates the PR, collects CI and review evidence, and performs delegated PR-surface mutations only within the active bounded delegation.
6. If freshness is already broken or later becomes broken, the Product Owner MUST immediately record the invalidation canonically and ensure the PR-surface reviewer summary is marked stale with the stale reason and the last known freshness stamp at first observation, even during the 300-second polling wait, then follow the freshness recovery rules before republishing.
7. The Product Owner MUST keep the bounded stale-marker delegation active for the current PR while a fresh reviewer summary is published or a review-polling wait is active so stale-marker publication has no integrity window.
8. The PR Manager monitors CI pipelines and handles review threads using the repository PR polling protocol only while the corresponding `delegation-recorded` event remains active for that `workItemId`, and only for actions and targets authorized in that delegation.
9. Review thread handling:
   - For each human review comment: read it, decide scope-appropriateness, fix it or push back with reasoning, reply to the thread, and either resolve it or leave it open with rationale.
10. `workflow-audit.md` and the `Reviewer Audit Summary` are derived artifacts only. Missing canonical facts MUST be fixed in `workflow-audit.json`; they MUST NOT be backfilled from `activity-log.md`, thread logs, or PR prose.
11. Merge readiness is confirmed only when the Product Owner evaluates current delegated evidence and records the conclusion canonically.
12. Merge readiness is confirmed when:

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
4. If a poll returns zero new unaddressed comments: end the polling loop and return the current evidence set to the Product Owner for merge-readiness evaluation.
5. If the iteration cap is reached: stop and report the remaining unresolved
  threads for human review.

A freshness-breaking observation interrupts the current 300-second wait. The Product
Owner MUST record the invalidation canonically and ensure the stale marker is published immediately, then resume or restart the
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

## Agent Roster (32 Agents)

This section is the single authoritative roster of approved Clean Squad
delegation targets. Any delegation term in this workflow, the shared Clean
Squad instruction, or the Product Owner prompt must resolve only to the named
agents in this roster. If no listed agent fits, the Product Owner must stop,
record the blocker, and ask the user to either choose the nearest approved
Clean Squad agent, approve a roster or workflow change first, or explicitly
leave Clean Squad orchestration for that task.

### Entry Point (1)

| Agent | Role |
|-------|------|
| cs Product Owner | Sole human interface; orchestrates the entire SDLC |

### Discovery & Requirements (3)

| Agent | Role |
|-------|------|
| cs Requirements Analyst | Deep requirements analysis, gap identification |
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

### PR & Records (2)

| Agent | Role |
|-------|------|
| cs Scribe | Records thinking, decisions, reasoning, handovers |
| cs PR Manager | PR lifecycle, thread management, merge readiness |

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
