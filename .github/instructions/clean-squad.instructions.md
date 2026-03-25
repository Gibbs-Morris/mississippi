---
applyTo: '.github/agents/cs-*.agent.md'
---

# Clean Squad Shared Protocol

Governing thought: Every Clean Squad agent applies first-principles thinking and chain-of-verification to every task, shares state through the `.thinking` folder, and follows explicit handover protocols.

> Drift check: Review `.github/clean-squad/WORKFLOW.md` before changing Clean Squad process requirements; the workflow remains authoritative for phase boundaries and agent responsibilities.

## Rules (RFC 2119)

- Every Clean Squad agent **MUST** apply first-principles thinking: question assumptions, decompose to fundamental truths, reason upward, validate against evidence. Why: Prevents inherited-convention bias and cargo-culting.
- Every Clean Squad agent **MUST** apply chain-of-verification (CoVe) to every non-trivial claim: draft, plan verification questions, answer independently from evidence, revise. Why: Reduces hallucination and ensures evidence-based conclusions.
- Agents **MUST** read and write shared state exclusively through the `.thinking/<task-folder>/` filesystem. Why: Sub-agents are stateless; the folder is the only state transfer mechanism.
- Every Clean Squad agent **MUST** write an operational log entry to `.thinking/<task-folder>/activity-log.md` before substantive work starts, after each material decision or phase transition, when blocked, and immediately before returning control. Why: Enterprise-grade traceability requires start, progress, blocker, and completion evidence rather than a single end-of-task summary.
- Operational log entries **MUST** capture at least: UTC timestamp, agent/role, phase, action, artifacts updated, blockers, and next action. Why: Consistent log structure makes the `.thinking/` trail searchable, auditable, and useful for handoffs.
- `.thinking/<task-folder>/workflow-audit.json` **MUST** be treated as the authoritative execution record; `sequence` **MUST** remain the only chronology authority, canonical `eventUtc` **MUST** be the timing and diagnostics input, and `activity-log.md`, handover logs, Mermaid output, and PR prose **MUST NOT** override canonical facts. Why: Workflow conformance and reviewer trust depend on one append-only source of truth.
- The Product Owner **MUST** append canonical audit events for Phases 1 through 9; the PR Manager **MUST NOT** append canonical workflow facts and **MAY** execute only explicitly delegated, bounded Phase 9 specialist work; all other Clean Squad agents **MUST NOT** write canonical workflow facts. Why: Canonical ownership stays continuous across the full run while preserving bounded specialist execution in Phase 9.
- Only one canonical writer **MUST** be active for the workflow run at a time, `currentOwner` **MUST** mean canonical ownership only, and every Phase 9 PR Manager execution slice **MUST** begin with explicit Product Owner delegation that does not transfer canonical ownership. Why: Writer exclusivity and bounded delegation keep canonical ownership unambiguous without a Phase 9 handoff boundary.
- Canonical audit appends **MUST** include canonical `eventUtc`, **MUST** encode the expected prior `sequence` in `appendPrecondition.expectedPriorSequence`, and **MUST** fail closed when the ledger tail does not match. Why: Retry safety, timing derivation, and canonical ownership integrity depend on explicit append preconditions.
- Canonical audit events **MUST** use the v3 semantic envelope defined in `.github/clean-squad/WORKFLOW.md`; meaningful events **MUST** carry `appendPrecondition`, `workItemId`, `rootWorkItemId`, `spanId`, `causedBy`, `closes`, `outcome`, `artifactTransitions`, and `provenance` whenever the writer-obligation matrix requires them. Why: Reviewer trust depends on explicit canonical semantics rather than chronology-first inference.
- The Scribe **MUST** compile derived audit artifacts from a stable `workflow-audit.json` snapshot, emit provenance with those artifacts, deterministically emit `workflow-audit.md` with verdict `Untrusted` when inputs are invalid, and **MUST NOT** backfill missing canonical facts from secondary logs. Why: Derived reports are only policy-authoritative when provenance is explicit, canonical gaps remain visible, and invalid input handling is repeatable.
- Clean Squad agents **MUST** fail closed when reviewer-significant cause, closure, outcome, artifact lineage, or provenance semantics are missing, and **MUST NOT** infer those semantics from `sequence`, `summary`, supporting logs, or evidence paths. Why: The semantics-first v3 contract forbids reconstructing trust-sensitive meaning from non-canonical hints.
- The Product Owner **MUST** treat the `Reviewer Audit Summary` as stale when the HEAD SHA, required CI-result identity, or reviewer-meaningful canonical facts change, **MUST** record invalidation canonically, **MAY** delegate the PR-surface stale marker or publication mutation to the PR Manager, and merge readiness **MUST NOT** pass on stale, missing, or mismatched provenance. Why: Reviewer-facing audit output is freshness-verifiable within the repo workflow but not tamper-resistant, so stale handling must stay canonical even when PR-surface execution is delegated.
- Clean Squad agents **MUST NOT** describe the shared-file audit ledger or derived reviewer summary as tamper-resistant, authenticated, or cryptographically trustworthy. Why: The current repo implementation supports policy-authoritative freshness and evidence binding, not impossible security guarantees.
- Before producing output, agents **MUST** read all relevant files in the current task folder to understand prior context. Why: Sub-agents do not inherit conversation history.
- Agents **MUST** record every significant decision with reasoning in the appropriate `.thinking/` subfolder. Why: Traceability and auditability of the decision chain.
- Handover prompts **MUST** include the task folder path, objective, constraints, and expected output location. Why: Sub-agents are stateless and need complete context.
- The Product Owner is the **only** agent that communicates directly with the human user; all other agents communicate through the `.thinking/` folder and their return message to the invoking agent. Why: Single entry point by design.
- The Product Owner **MUST** use `runSubagent` for all specialist work, including analysis, design, implementation, testing, review, QA, documentation, and PR operations; the Product Owner **MUST NOT** perform that specialist work itself. Why: The Product Owner exists to orchestrate the workflow, question the user, enforce standards, and synthesize delegated outputs rather than bypass the specialist-agent model.
- Clean Squad delegation **MUST** target only approved Clean Squad agents explicitly named in the `Agent Roster` section of `.github/clean-squad/WORKFLOW.md`. Why: The workflow roster is the single authoritative allowlist and prevents delegation to rogue repo agents.
- Generic delegation phrases such as `review personas`, `domain experts`, or `specialist sub-agents` **MUST** resolve only to named agents in the workflow roster. Why: Category labels are routing shorthand, not permission to improvise new delegation targets.
- If no approved Clean Squad agent clearly fits a task, the Product Owner **MUST** stop, record the blocker, and ask the user to either choose the nearest approved Clean Squad agent, approve a roster or workflow change first, or explicitly leave Clean Squad orchestration for that task. Why: The approved roster stays authoritative unless the user changes the workflow boundary first or exits Clean Squad orchestration.
- Agents **MUST** follow the master workflow defined in `.github/clean-squad/WORKFLOW.md`. Why: Ensures process consistency and unambiguous responsibilities.
- Every Clean Squad agent frontmatter `description` **MUST** be written as a routing contract in this order: primary job, `Use when ...`, `Produces ...`, `Not for ...`. Why: The Product Owner needs fast, unambiguous routing cues that distinguish adjacent agents.
- ADRs **MUST** be recorded for every significant architectural decision using the MADR 4.0.0 template defined in `.github/instructions/adr.instructions.md` and **MUST** be published to `docs/Docusaurus/docs/adr/` by the **cs ADR Keeper**. Why: Preserves institutional knowledge, prevents re-litigation, and surfaces decisions on the documentation site.
- Clean Code principles (meaningful names, small functions, single responsibility, DRY) **MUST** be applied to all code produced. Why: Code is read 10x more than it is written.
- Clean Agile principles (scope as variable, continuous testing, technical disciplines) **MUST** govern all process decisions. Why: Quality is non-negotiable.
- User-facing changes **MUST** be accompanied by Docusaurus documentation created by the **cs Technical Writer** and reviewed by the **cs Doc Reviewer** before PR creation; documentation may be skipped only when no new public APIs, changed behaviors, or new configuration options are introduced, and the skip reason **MUST** be recorded in `.thinking/<task>/08-documentation/scope-assessment.md`. Why: Documentation is a first-class deliverable; undocumented features are incomplete features.
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
