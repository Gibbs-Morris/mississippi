# Clean Squad Workflow Diagram

This document is a visual companion to [WORKFLOW.md](WORKFLOW.md).

- It MUST mirror the current workflow exactly.
- It MUST NOT add, remove, simplify, reinterpret, or improve any workflow step, loop, responsibility, or policy.
- If this diagram and `WORKFLOW.md` ever differ, `WORKFLOW.md` is authoritative.
- Where exact contract detail would make the diagram unreadable, this file MUST mirror that detail in explicit Markdown sections outside the Mermaid block without changing authority.

```mermaid
flowchart TD
    Authority["Authority note:<br/>WORKFLOW.md is authoritative.<br/>This Mermaid file is a visual companion only."]
    Principles["Cross-cutting note:<br/>All agents apply first principles and CoV.<br/>cs Entrepreneur is the optional pre-governed public intake agent.<br/>cs Product Owner is the sole governed orchestrator."]
    SharedState["Cross-cutting note:<br/>Governed agents share state through .thinking/&lt;task&gt;/.<br/>cs Entrepreneur is the explicit pre-governed exception and does not create or use this shared state.<br/>workflow-audit.json is the authoritative execution record.<br/>Activity-log and handover updates are mandatory secondary evidence."]
    AuditOwnership["Cross-cutting note:<br/>cs Product Owner writes canonical audit events for Phases 1-9.<br/>cs PR Manager executes only bounded delegated Phase 9 PR-surface work and does not write canonical facts.<br/>cs Scribe publishes derived audit output only."]
    AuditRules["Cross-cutting note:<br/>sequence is the only ordering authority.<br/>Canonical eventUtc timestamps are mandatory for timing and diagnostics only and never override sequence.<br/>Narrative logs, Mermaid, and PR prose are supporting or derived evidence only."]
    AuditDelegation["Cross-cutting note:<br/>Only one canonical writer may be active for the workflow run at a time.<br/>Phase 9 specialist work starts only after explicit Product Owner delegation that names the bounded task slice, expected artifact output or artifact bundle, completion signal, closure condition, details.allowedActions, and details.authorizedTargets without transferring canonical ownership.<br/>Blocked startup or recovery never transfers canonical ownership away from cs Product Owner.<br/>A bounded stale-marker delegation stays active while a fresh reviewer summary is published or a polling wait is active so stale publication has no integrity window.<br/>Every canonical append declares the expected prior sequence and fails closed on tail mismatch.<br/>The first canonical append uses sequence 1 with expected prior sequence 0."]
    AuditEventContract["Cross-cutting note:<br/>Every canonical event uses the v3 semantic envelope: sequence, eventUtc, logicalEventId, actor, phase, eventType,<br/>appendPrecondition, workItemId and rootWorkItemId when meaningful, spanId, causedBy, closes, outcome, summary, reasonCode,<br/>artifacts as evidence bindings, artifactTransitions for lifecycle meaning, iterationId, provenance, and details."]
    StateSupportContract["Cross-cutting note:<br/>state.json is runtime support only and mirrors the workflow state contract exactly.<br/>It includes workflowContractFingerprint, currentSequence, currentOwner, openWait, and lastCompiledAtUtc.<br/>currentOwner means canonical ownership only and never delegated execution ownership.<br/>It never repairs canonical facts from workflow-audit.json."]
    TrustModel["Cross-cutting note:<br/>Reviewer-facing audit output is policy-authoritative and freshness-verifiable within this repo,<br/>but not tamper-resistant or authenticated.<br/>Evidence-bearing artifacts require content digests or immutable external identities before publication."]
    ProvenanceContract["Cross-cutting note:<br/>Every detailed audit artifact binds to HEAD SHA, ledger watermark, ledgerDigest, workflowContractFingerprint,<br/>generation timestamp, and generator identity.<br/>When merge readiness depends on CI, cs Product Owner verifies and attaches the current normalized required CI-result identity set before publication or directed PR-surface publication.<br/>Required CI identity changes alone do not force detailed-audit recompilation, and merge readiness never passes with stale, missing, or mismatched provenance."]
    VerdictContract["Cross-cutting note:<br/>Verdicts are Conformant, ConformantWithDeviations, NonConformant, Blocked, or Untrusted.<br/>Missing, malformed, or policy-violating evidence never yields a conformant verdict from current policy-authoritative evidence."]
    EvidenceContract["Cross-cutting note:<br/>Major completion claims require sufficient artifact evidence.<br/>Schema validation is required where the contract defines canonical or derived metadata structure.<br/>Supporting logs may corroborate but never repair missing canonical facts.<br/>Missing evidence maps to NonConformant, Blocked, or Untrusted based on policy violation, incomplete run, or trust failure."]
    TimingContract["Cross-cutting note:<br/>Timing uses elapsed, active-agent, human-wait, and system-wait totals derived from canonical eventUtc values.<br/>Buckets are mutually exclusive; unmatched or overlapping waits and impossible totals invalidate trust."]
    SummaryContract["Cross-cutting note:<br/>Reviewer Audit Summary order is verdict, action, blockers, provenance stamp, condensed Mermaid flow,<br/>four-bucket timing, fixed timing sentence, deviations, then pointer to workflow-audit.md.<br/>Keep only current blockers, the condensed topology, and at most three deviations on the PR surface; overflow detail lives in workflow-audit.md.<br/>workflow-audit.md begins with a why-this-matters opener."]
    FailureMatrix["Cross-cutting note:<br/>Failure matrix cases and outcomes are mirrored from WORKFLOW.md:<br/>happy path, human clarification wait, and Phase 9 polling wait -> Conformant and publish or keep published only with current provenance.<br/>remediation loop, allowed skip, and declined review comment -> ConformantWithDeviations and republish only when reviewer-facing deviations change with refreshed provenance.<br/>blocked run -> Blocked and no merge-ready summary is sufficient until cleared and regenerated.<br/>stale summary, unmatched wait boundary, overlapping waits, impossible timing totals, and malformed provenance -> Untrusted and invalidate immediately until corrected and regenerated.<br/>duplicate logicalEventId, invalid chronology or delegation-basis violation, and unauthorized writer or delegated actor -> NonConformant and no trusted reviewer evidence publishes until repaired and regenerated.<br/>missing major-claim evidence -> Blocked when incomplete, otherwise Untrusted; invalidate or withhold publication until evidence exists and a fresh summary is generated."]
    Delegation["Cross-cutting note:<br/>Before every runSubagent, verify the approved Agent Roster in WORKFLOW.md.<br/>If no approved fit exists, stop, record the blocker, and ask the user how to proceed."]

    subgraph EntryPoint["Public Intake"]
        User([User request]) -->|rough idea| Entrepreneur["cs Entrepreneur shapes one Story Pack candidate before governed intake"]
        User -->|direct governed intake| ProductOwner["cs Product Owner starts governed work and remains the sole governed orchestrator"]
        Entrepreneur --> StoryPack["Story Pack candidate"]
        StoryPack --> G0{"G0 Intake Gate<br/>Story Pack candidate"}
        G0 -->|APPROVED| ProductOwner
        G0 -->|NOT APPROVED| G0NotApproved["Stop or rework outside governed workflow<br/>(see G0 gate outcomes in WORKFLOW.md)"]
    end

    subgraph Phase1["Phase 1: Intake & Discovery"]
        P1Setup["Create .thinking/&lt;date&gt;-&lt;task-slug&gt;/, state.json, workflow-audit.json, activity-log.md, and 00-intake.md"]
        P1Ask["Ask 5 discovery questions"]
        P1Clear{"Requirements sufficiently clear?"}
        P1Analyze["Invoke cs Requirements Analyst for gap analysis and the next 5 questions"]
        P1Record["Record answers and ask the next 5 questions"]
        P1Synthesis["Write 01-discovery/requirements-synthesis.md"]

        P1Setup --> P1Ask --> P1Clear
        P1Clear -- No --> P1Analyze --> P1Record --> P1Clear
        P1Clear -- Yes --> P1Synthesis
    end

    subgraph Phase2["Phase 2: Three Amigos + Adoption"]
        P2Invoke["Invoke cs Business Analyst, cs Tech Lead, cs QA Analyst, and cs Developer Evangelist one at a time"]
        P2Outputs["Each sub-agent writes its perspective document"]
        P2Synthesis["Write 02-three-amigos/synthesis.md"]
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
        P6Synthesis["Synthesize all review output"]
        P6Findings{"Review findings remain?"}
        P6Remediate["Fix each finding or document why it was declined"]

        P6Diff --> P6Review --> P6Experts --> P6Synthesis --> P6Findings
        P6Findings -- Yes --> P6Remediate --> P6Review
    end

    subgraph Phase7["Phase 7: QA Validation"]
        P7Lead["Invoke cs QA Lead to review test strategy and coverage"]
        P7Exploratory["Invoke cs QA Exploratory"]
        P7Mutation["Invoke cs Test Engineer for mutation testing validation"]
        P7Gaps{"QA gaps identified?"}
        P7Remediate["Feed QA gaps back for remediation of the current increment"]

        P7Lead --> P7Exploratory --> P7Mutation --> P7Gaps
        P7Gaps -- Yes --> P7Remediate
    end

    subgraph Phase8["Phase 8: Documentation"]
        P8Scope["Assess documentation scope from the branch diff and .thinking artifacts"]
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
        P9Manager["cs Product Owner remains the canonical Phase 9 owner and records every reviewer-significant Phase 9 fact"]
        P9Startup["Record a bounded delegation to cs PR Manager that names the bounded task slice, details.expectedOutputPath, completion signal, closure condition, details.allowedActions, and details.authorizedTargets; blocked startup or recovery never transfers ownership"]
        P9Scribe["cs Product Owner invokes cs Scribe when HEAD, ledger, workflow contract, or reviewer-meaningful canonical facts require a fresh stable-snapshot audit"]
        P9Execute["cs PR Manager executes only the delegated PR-surface work and returns artifacts and evidence"]
        P9Stale["At first observation, cs Product Owner records invalidation canonically and the continuously delegated stale-marker capability marks the PR surface stale immediately; do not let the 300-second wait delay invalidation"]
        P9Verify["cs Product Owner verifies workflow-audit provenance plus the current required CI identity set before republication or merge-readiness evaluation"]
        P9Publish["cs Product Owner decides publication or republication; cs PR Manager applies the PR-surface mutation only when delegated"]
        P9Wait["After pushing to an open PR, wait 300 seconds unless stale invalidation work preempts the wait"]
        P9Poll["Poll for unresolved review comments"]
        P9Comments{"New unaddressed comments?"}
        P9Scope{"Comment is in scope?"}
        P9Address["Fix, commit, push, reply with evidence, and resolve the thread"]
        P9OutOfScope["Reply with reasoned explanation and leave the thread open for the reviewer"]
        P9Record["cs Product Owner records the resulting canonical fact, closes or reissues the delegation, and decides whether publication or merge readiness changed"]
        P9Cap{"Iteration cap reached?"}
        P9Ready["cs Product Owner assembles the merge-readiness package and evaluates current evidence canonically"]
        G3{"G3 Merge Gate<br/>Merge-readiness package + rolled-up review, QA, and documentation conclusions"}
        P9Stop(["Stop and report remaining unresolved threads for human review"])
        P9Done([Done])

        P9Manager --> P9Startup --> P9Scribe --> P9Execute --> P9Stale --> P9Verify --> P9Publish --> P9Wait --> P9Poll --> P9Comments
        P9Comments -- Yes --> P9Scope
        P9Scope -- Yes --> P9Address --> P9Record --> P9Cap
        P9Scope -- No --> P9OutOfScope --> P9Record --> P9Cap
        P9Cap -- No --> P9Wait
        P9Cap -- Yes --> P9Stop
        P9Comments -- No --> G3
        G3 --|APPROVED| P9Ready --> P9Done
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

    P3AdrNote["Architecture note:<br/>For each significant architectural decision, invoke cs ADR Keeper to publish an ADR."]
    P3ExpertNote["Architecture note:<br/>The Product Owner may invoke approved domain experts from the Agent Roster when specialist architectural input is needed."]
    P4Note["Planning note:<br/>Repeat for 3 to 5 review cycles total.<br/>The reviewer subset varies by task complexity."]
    P5Note["Implementation note:<br/>Each increment is small enough to review like its own PR and includes relevant tests.<br/>TDD is used where practical."]
    P6Note["Code review note:<br/>Every changed file is reviewed by at least cs Reviewer Pedantic and cs Reviewer Strategic."]
    P8Note["Documentation note:<br/>Documentation may be skipped only when all documented skip criteria are true and the evidence is recorded."]
    P9Note["PR note:<br/>Merge readiness also requires a PR, green CI, no unresolved review comments, no open review threads,<br/>and a current Reviewer Audit Summary with verified provenance and freshness for the current HEAD and required CI identity set."]
    P9Timing["Timing note:<br/>Active agent time excludes human replies and system wait such as CI or review polling."]
    P9Derived["Audit note:<br/>workflow-audit.md and the Reviewer Audit Summary are derived only.<br/>Missing canonical facts are fixed in workflow-audit.json, not backfilled from secondary logs.<br/>Freshness breaks must show a stale marker before any refreshed summary is republished, and the 300-second wait never delays that stale marker.<br/>If only required CI identity changes, reuse the current detailed audit and refresh the PR-surface freshness stamp instead of recompiling workflow-audit.md."]

    P3AdrNote -.-> P3Adr
    P3ExpertNote -.-> P3Design
    P4Note -.-> P4Review
    P5Note -.-> P5Lead
    P6Note -.-> P6Review
    P8Note -.-> P8UserFacing
    P9Note -.-> P9Ready
    P9Timing -.-> P9Wait
    P9Derived -.-> P9Scribe
```

## Canonical Event Contract Mirror

This section explicitly mirrors the v3 canonical event contract from [WORKFLOW.md](WORKFLOW.md). If any wording here and the authoritative workflow differ, [WORKFLOW.md](WORKFLOW.md) governs.

- `workflow-audit.json` now uses `schemaVersion = clean-squad-workflow-audit/v3`.
- Reviewer-significant meaning MUST be explicit in structured fields. Cause, closure, outcome, artifact lineage, and provenance MUST NOT be reconstructed from chronology, prose, secondary logs, or path changes alone.
- Every meaningful event carries `workItemId`, `rootWorkItemId`, `spanId`, `causedBy`, `closes`, `outcome`, `artifactTransitions`, and `provenance` whenever the writer-obligation matrix requires them.
- Every canonical event carries `appendPrecondition`, whose `expectedPriorSequence` encodes the fail-closed append expectation.
- Every Phase 9 `delegation-recorded` event carries capability-scoped `details.allowedActions` and `details.authorizedTargets`, and the stale-marker delegation stays active while a fresh reviewer summary is published or a polling wait is active.
- `artifacts` remains evidence binding only. Artifact lifecycle meaning lives in `artifactTransitions`.
- `details` is always serialized in canonical order and MUST use `{}` when no event-type-specific members apply.
- The first canonical append uses `sequence = 1` and expected prior `sequence = 0`.
- Canonical event serialization keeps the full top-level property set in the declared order; absent conditional scalars or objects use `null`, absent conditional arrays use `[]`, and empty `details` uses `{}`.
- The digest form is minified UTF-8 JSON with no BOM, no trailing newline, and no insignificant whitespace outside JSON string values.
- Nested object property order matches the shapes defined in `WORKFLOW.md` for `appendPrecondition`, `causedBy`, `closes`, `artifacts[]`, `artifactTransitions[]`, and `provenance`.
- `ledgerDigestAlgorithm = sha256-canonical-json-v1` hashes the UTF-8 bytes of that canonical JSON form for the exact snapshot being compared or compiled.

Canonical top-level shape mirror:

```json
{
    "schemaVersion": "clean-squad-workflow-audit/v3",
    "workflowContractFingerprint": "<sha256 of UTF-8 bytes of .github/clean-squad/WORKFLOW.md>",
    "ledgerDigestAlgorithm": "sha256-canonical-json-v1",
    "events": []
}
```

Canonical event property-order mirror:

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

## Human Advancement Gates Mirror

This section mirrors the sparse human advancement gates from
[WORKFLOW.md](WORKFLOW.md). If any wording here and the authoritative workflow
differ, [WORKFLOW.md](WORKFLOW.md) governs.

- `G0` applies only when `cs Entrepreneur` is used and binds the Story Pack candidate before governed intake starts.
- `G1` binds `01-discovery/requirements-synthesis.md` and `02-three-amigos/synthesis.md` before architecture and planning proceed.
- `G2` binds `03-architecture/solution-design.md`, the binding C4 artifacts, the binding ADR artifacts, and `04-planning/final-plan.md` before implementation proceeds.
- `G3` binds `09-pr-merge/merge-readiness.md` plus the current rolled-up code-review, QA, and documentation conclusions before PR-ready or merge-ready progression continues.
- Every gate uses the same outcomes: `APPROVED`, `CHANGES_REQUESTED`, `DEFERRED`, and `CANCELLED`.
- Any material change to a bound artifact makes the prior approval stale and requires a fresh explicit decision before advancement.

## Writer Obligation Matrix Mirror

This section explicitly mirrors the semantic-family obligations from [WORKFLOW.md](WORKFLOW.md). If any wording here and the authoritative workflow differ, [WORKFLOW.md](WORKFLOW.md) governs.

| Event Family | Required Fields | Optional Fields | Invalid If Missing |
|-------------|-----------------|-----------------|--------------------|
| lifecycle-start | `workItemId`, `rootWorkItemId`, `spanId`, `provenance` | `causedBy`, `iterationId` | The span cannot be tracked or closed. |
| lifecycle-terminal | `workItemId`, `rootWorkItemId`, `closes`, `outcome`, `provenance` | `causedBy`, `artifacts`, `artifactTransitions`, `iterationId` | Exact closure and disposition become ambiguous. |
| causal-link | `workItemId`, `rootWorkItemId`, `causedBy`, `provenance` | `artifacts`, `details`, `iterationId` | The derived event looks chronology-driven rather than canonically caused. |
| artifact-transition | `workItemId`, `rootWorkItemId`, `artifactTransitions`, `provenance` | `causedBy`, `artifacts`, `details`, `iterationId` | Artifact history becomes narrative-only. |
| deviation | `workItemId`, `rootWorkItemId`, `causedBy`, `reasonCode`, `provenance`, `details` | `spanId`, `artifactTransitions`, `iterationId` | The deviation basis and remediation path become prose-only. |
| interruption | `workItemId`, `rootWorkItemId`, `causedBy`, `reasonCode`, `provenance`, `details` | `spanId`, `artifactTransitions`, `iterationId` | The interruption cannot be tied to the affected work or blocker. |
| ownership | `workItemId`, `rootWorkItemId`, `causedBy`, `provenance`, `details` | `artifacts`, `iterationId` | Canonical writer or delegated-execution ownership becomes ambiguous. |
| publication-support | `workItemId`, `rootWorkItemId`, `causedBy`, `provenance`, `details` | `artifacts`, `iterationId` | The CI or merge-readiness basis cannot be audited deterministically. |
| review-progress | `workItemId`, `rootWorkItemId`, `causedBy`, `provenance`, `details` | `spanId`, `artifacts`, `artifactTransitions`, `iterationId`, `reasonCode` | The exact review-thread action cannot be audited deterministically. |
| publication-state | `workItemId`, `rootWorkItemId`, `provenance`, `details` | `artifactTransitions`, `causedBy`, `artifacts`, `iterationId` | Reviewer-summary freshness becomes untrustworthy. |
| run-terminal | `workItemId`, `rootWorkItemId`, `closes`, `outcome`, `provenance` | `causedBy`, `artifacts`, `artifactTransitions`, `iterationId` | Run completion cannot be tied to the exact ended span or disposition. |

## Trust and Freshness Mirror

This section explicitly mirrors the trust and freshness contract from [WORKFLOW.md](WORKFLOW.md). If any wording here and the authoritative workflow differ, [WORKFLOW.md](WORKFLOW.md) governs.

Freshness and merge readiness MUST bind to the current HEAD SHA and the required CI-result identity set for that SHA.

The required CI-result identity set comparison is structural: normalize ordering and deduplicate exact duplicates, then compare the parsed JSON structure rather than relying on raw byte-for-byte object serialization.

Reviewer-facing audit output becomes stale on any of these events:

1. commit or push that changes HEAD SHA
2. force-push or rebase
3. required check rerun
4. replaced or superseded check result
5. required-check state change
6. canonical ledger event that changes reviewer-meaningful output

Publication and recovery rules:

1. The Product Owner MUST invalidate immediately at first observation of a relevant change, including during any active 300-second review-polling wait, by recording the canonical invalidation fact and ensuring the Reviewer Audit Summary is marked stale on the PR surface with the stale reason and the last known freshness stamp.
2. The Product Owner MUST keep a bounded stale-marker delegation active for the current PR surface whenever a fresh Reviewer Audit Summary is published or a review-polling wait is active so stale-marker publication never waits on a new delegation round-trip.
3. The Product Owner MUST regenerate `workflow-audit.md` from a fresh stable ledger snapshot only when HEAD, ledger snapshot, workflow contract fingerprint, or reviewer-meaningful canonical output changed.
4. When only the required CI-result identity set changes for unchanged HEAD and unchanged reviewer-meaningful canonical facts, the Product Owner MUST refresh the Reviewer Audit Summary freshness stamp and merge-readiness evaluation without regenerating `workflow-audit.md`.
5. The Product Owner MUST republish only after `workflow-audit.md` provenance matches the current HEAD SHA, ledger watermark, `ledgerDigest`, `workflowContractFingerprint`, and any attached required CI-result identity set.
6. Keep invalidation more granular than publication so the PR description does not churn on low-signal changes.

## State and Runtime Support Mirror

This section explicitly mirrors the `state.json` runtime support contract from [WORKFLOW.md](WORKFLOW.md). If any wording here and the authoritative workflow differ, [WORKFLOW.md](WORKFLOW.md) governs.

`state.json` is runtime support only and MUST match this shape:

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

## Required Failure Matrix Mirror

This section explicitly mirrors the per-case failure matrix from [WORKFLOW.md](WORKFLOW.md) because rendering all cases at equivalent specificity inside the Mermaid flow would make the diagram materially less readable. The authoritative source remains [WORKFLOW.md](WORKFLOW.md).

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
