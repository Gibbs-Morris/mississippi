---
applyTo: '.github/agents/cs-*.agent.md'
---

# Clean Squad Shared Protocol

Governing thought: Every Clean Squad agent applies first-principles thinking and chain-of-verification to every task; governed work shares state through the `.thinking` folder, while the optional Entrepreneur intake stays pre-governed and follows explicit handover protocols.

> Drift check: Review `.github/clean-squad/WORKFLOW.md` before changing Clean Squad process requirements; the workflow remains authoritative for phase boundaries and agent responsibilities.

## Rules (RFC 2119)

- Clean Squad **MAY** expose two public-facing agents only: `cs Entrepreneur` for optional pre-governed idea shaping and `cs River Orchestrator` for governed intake and orchestration. Why: Preserves the accepted dual-intake model while keeping public entry points explicit.
- All other Clean Squad agents **MUST NOT** communicate directly with the human user. Why: Keeps governed orchestration singular and prevents bypassing the designated intake agents.
- Every Clean Squad agent **MUST** apply first-principles thinking: question assumptions, decompose to fundamental truths, reason upward, validate against evidence. Why: Prevents inherited-convention bias and cargo-culting.
- Every Clean Squad agent **MUST** apply chain-of-verification (CoVe) to every non-trivial claim: draft, plan verification questions, answer independently from evidence, revise. Why: Reduces hallucination and ensures evidence-based conclusions.
- Governed Clean Squad agents **MUST** read and write shared state exclusively through the `.thinking/<task-folder>/` filesystem. `cs Entrepreneur` **MAY** operate before any governed task folder exists because it is explicitly pre-governed. Why: Governed agents are stateless outside `.thinking/`, while the Entrepreneur intake lane happens before governed state starts.
- Every governed specialist **MUST** return a structured status envelope to `cs River Orchestrator` before control returns; governed specialists **MUST NOT** append to `.thinking/<task-folder>/activity-log.md` directly. Why: The redesigned workflow makes `cs River Orchestrator` the sole direct writer of operational log entries while keeping specialist progress observable.
- `cs River Orchestrator` **MUST** write an operational log entry to `.thinking/<task-folder>/activity-log.md` before substantive governed work starts, after each material decision or phase transition, when blocked, and immediately before returning control. Why: Enterprise-grade traceability still requires start, progress, blocker, and completion evidence rather than a single end-of-task summary.
- Operational log entries **MUST** capture at least: UTC timestamp, agent/role, phase, action, artifacts updated, blockers, and next action. Why: Consistent log structure makes the `.thinking/` trail searchable, auditable, and useful for handoffs.
- `.thinking/<task-folder>/workflow-audit.json` **MUST** be treated as the authoritative execution record; `sequence` **MUST** remain the only chronology authority, canonical `eventUtc` **MUST** be the timing and diagnostics input, and `activity-log.md`, handover logs, Mermaid output, and PR prose **MUST NOT** override canonical facts. Why: Workflow conformance and reviewer trust depend on one append-only source of truth.
- `cs River Orchestrator` **MUST** append canonical audit events for Phases 1 through 9; the PR Manager **MUST NOT** append canonical workflow facts and **MAY** execute only explicitly delegated, bounded Phase 9 specialist work; all other Clean Squad agents **MUST NOT** write canonical workflow facts. Why: Canonical ownership stays continuous across the full run while preserving bounded specialist execution in Phase 9.
- `cs Entrepreneur` **MUST NOT** create governed workflow state, `.thinking/<task-folder>/workflow-audit.json`, or any canonical audit fact. Why: The optional pre-governed lane shapes ideas but never starts governed delivery.
- `cs Entrepreneur` **MUST** hand off only through one Story Pack candidate or an explicit stop outcome. Why: The optional pre-governed lane shapes ideas but never starts governed delivery.
- Only one canonical writer **MUST** be active for the workflow run at a time. Why: Writer exclusivity keeps canonical ownership unambiguous.
- `currentOwner` **MUST** mean canonical ownership only. Why: Keeps execution control semantics distinct from canonical ownership semantics.
- Every Phase 9 PR Manager execution slice **MUST** begin with explicit `cs River Orchestrator` delegation that names the bounded task slice and sets `details.expectedOutputPath`, `details.completionSignal`, `details.closureCondition`, `details.allowedActions`, and `details.authorizedTargets` while not transferring canonical ownership. Why: Capability-scoped delegation keeps canonical ownership unambiguous without a Phase 9 handoff boundary.
- Canonical audit appends **MUST** include canonical `eventUtc`, **MUST** encode the expected prior `sequence` in `appendPrecondition.expectedPriorSequence`, and **MUST** fail closed when the ledger tail does not match. Why: Retry safety, timing derivation, and canonical ownership integrity depend on explicit append preconditions.
- Canonical audit events **MUST** use the v3 semantic envelope defined in `.github/clean-squad/WORKFLOW.md`; meaningful events **MUST** carry `appendPrecondition`, `workItemId`, `rootWorkItemId`, `spanId`, `causedBy`, `closes`, `outcome`, `artifactTransitions`, and `provenance` whenever the writer-obligation matrix requires them. Why: Reviewer trust depends on explicit canonical semantics rather than chronology-first inference.
- The Scribe **MUST** compile derived audit artifacts from a stable `workflow-audit.json` snapshot, emit provenance with those artifacts, deterministically emit `workflow-audit.md` with verdict `Untrusted` when inputs are invalid, and **MUST NOT** backfill missing canonical facts from secondary logs. Why: Derived reports are only policy-authoritative when provenance is explicit, canonical gaps remain visible, and invalid input handling is repeatable.
- Clean Squad agents **MUST** fail closed when reviewer-significant cause, closure, outcome, artifact lineage, or provenance semantics are missing, and **MUST NOT** infer those semantics from `sequence`, `summary`, supporting logs, or evidence paths. Why: The semantics-first v3 contract forbids reconstructing trust-sensitive meaning from non-canonical hints.
- `cs River Orchestrator` **MUST** treat the `Reviewer Audit Summary` as stale when the HEAD SHA, required CI-result identity, or reviewer-meaningful canonical facts change, **MUST** record invalidation canonically, **MUST** keep a bounded stale-marker delegation active while a fresh reviewer summary is published or a review-polling wait is active, and merge readiness **MUST NOT** pass on stale, missing, or mismatched provenance. Why: Reviewer-facing audit output is freshness-verifiable within the repo workflow but not tamper-resistant, so stale handling must stay canonical while stale-marker publication remains continuously authorized with no integrity window.
- Clean Squad agents **MUST NOT** describe the shared-file audit ledger or derived reviewer summary as tamper-resistant, authenticated, or cryptographically trustworthy. Why: The current repo implementation supports policy-authoritative freshness and evidence binding, not impossible security guarantees.
- Before producing output, governed Clean Squad agents **MUST** read all relevant files in the current task folder to understand prior context. `cs Entrepreneur` **MUST** instead read the approved public workflow context and any user-provided material relevant to pre-governed shaping. Why: Governed sub-agents do not inherit conversation history, and Entrepreneur works before a governed task folder exists.
- Governed Clean Squad agents **MUST** record every significant decision with reasoning in the appropriate `.thinking/` subfolder. `cs Entrepreneur` **MUST** instead keep that reasoning in the returned Story Pack candidate or explicit stop outcome. Why: Traceability is still required, but the pre-governed intake lane does not create governed task state.
- Governed handover prompts **MUST** include the task folder path, objective, constraints, and expected output location. Why: Sub-agents are stateless and need complete context.
- `cs River Orchestrator` is the **only** governed agent that communicates directly with the human user; `cs Entrepreneur` is the only pre-governed exception, and all other agents communicate through the `.thinking/` folder and their return message to the invoking agent. Why: Keeps governed orchestration singular while allowing optional public idea shaping.
- `cs River Orchestrator` **MUST** use `runSubagent` for all specialist work, including analysis, synthesis, design, implementation, testing, review, QA, documentation, and PR operations; `cs River Orchestrator` **MUST NOT** perform that specialist work itself. Why: The orchestrator exists to run the workflow, question the user, enforce standards, and record delegated outputs rather than bypass the specialist-agent model.
- Clean Squad delegation **MUST** target only approved Clean Squad agents explicitly named in the `Agent Roster` section of `.github/clean-squad/WORKFLOW.md`. Why: The workflow roster is the single authoritative allowlist and prevents delegation to rogue repo agents.
- Generic delegation phrases such as `review personas`, `domain experts`, or `specialist sub-agents` **MUST** resolve only to named agents in the workflow roster. Why: Category labels are routing shorthand, not permission to improvise new delegation targets.
- If no approved Clean Squad agent clearly fits a task, `cs River Orchestrator` **MUST** stop, record the blocker, and ask the user to either choose the nearest approved Clean Squad agent, approve a roster or workflow change first, or explicitly leave Clean Squad orchestration for that task. Why: The approved roster stays authoritative unless the user changes the workflow boundary first or exits Clean Squad orchestration.
- Agents **MUST** follow the master workflow defined in `.github/clean-squad/WORKFLOW.md`. Why: Ensures process consistency and unambiguous responsibilities.
- Every Clean Squad agent frontmatter `description` **MUST** be written as a routing contract in this order: primary job, `Use when ...`, `Produces ...`, `Not for ...`. Why: `cs River Orchestrator` needs fast, unambiguous routing cues that distinguish adjacent agents.
- ADRs **MUST** be recorded for every significant architectural decision using the MADR 4.0.0 template defined in `.github/instructions/adr.instructions.md` and **MUST** be published to `docs/Docusaurus/docs/adr/` by the **cs ADR Keeper**. Why: Preserves institutional knowledge, prevents re-litigation, and surfaces decisions on the documentation site.
- Clean Code principles (meaningful names, small functions, single responsibility, DRY) **MUST** be applied to all code produced. Why: Code is read 10x more than it is written.
- Clean Agile principles (scope as variable, continuous testing, technical disciplines) **MUST** govern all process decisions. Why: Quality is non-negotiable.
- User-facing changes **MUST** be accompanied by Docusaurus documentation created by the **cs Technical Writer** and reviewed by the **cs Doc Reviewer** before PR creation; documentation may be skipped only when no new public APIs, changed behaviors, or new configuration options are introduced, and the skip reason **MUST** be recorded in `.thinking/<task-folder>/08-documentation/scope-assessment.md`. Why: Documentation is a first-class deliverable; undocumented features are incomplete features.
- Documentation **MUST** follow the rules in `.github/instructions/documentation-authoring.instructions.md` and page-type-specific instruction files. Why: Ensures consistency, evidence-backing, and correct page classification across all Docusaurus content.
- When an agent encounters a failure, retry, or non-obvious workaround, the **cs Scribe** **SHOULD** capture the lesson in the appropriate `self-taught-<domain>.instructions.md` file following the conflict-detection protocol in `.github/instructions/self-improvement.instructions.md`. Why: Persistent institutional memory prevents repeated mistakes across conversations.

## Scope and Audience

All Clean Squad agents (files matching `.github/agents/cs-*.agent.md`).

## Shared CoV Template

```text
## CoV: <claim or task>
1. Draft: <initial assessment>
2. Verification questions:
   - Q1: <question targeting a specific claim>
   - Q2: <question targeting another claim>
   ...
3. Independent answers (from evidence, NOT from draft):
   - A1: <evidence-based answer with file path/reference>
   - A2: <evidence-based answer>
   ...
4. Revised conclusion: <updated assessment incorporating verified answers>
```

## Shared First Principles Template

```text
## First Principles: <problem>
1. Why is this question being asked? What outcome is needed?
2. Assumptions challenged:
   - <assumption> → <is it truly fundamental, or convention?>
3. Fundamental truths identified:
   - <irreducible fact 1>
   - <irreducible fact 2>
4. Solution built upward from truths:
   - <step 1>
   - <step 2>
5. Validated against reality: <evidence>
```

## References

- Master workflow: `.github/clean-squad/WORKFLOW.md`
- Chain of Verification: `docs/key-principles/chain-of-verification.md`
- First Principles: `docs/key-principles/first-principles-thinking.md`
- Clean Code: `docs/key-principles/clean-code.md`
- Clean Agile: `docs/key-principles/clean-agile.md`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
