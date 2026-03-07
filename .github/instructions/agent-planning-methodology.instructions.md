---
applyTo: '.github/agents/*planner*.agent.md,.github/agents/*build*.agent.md'
---

# Agent Planning Methodology

Governing thought: All planning and building agents share a common Chain-of-Verification loop, persona clarification model, review roster, and artifact naming scheme so plans are consistent, reusable, and implementation-ready.

> Drift check: Open `flow-planner.agent.md` and `epic-planner.agent.md` before modifying; agents are authoritative for their own workflow tweaks.

## Rules (RFC 2119)

- Planning agents **MUST** apply the Chain-of-Verification (CoV) loop on every non-trivial claim: hypothesize → question → gather evidence → triangulate with a second independent source → conclude with confidence rating → record impact. Why: Prevents speculative plans.
- Each non-trivial claim **MUST** be verified against at least two independent sources (different files, modules, tests, docs, or configs); single-source claims **MUST** be labelled **Single-source** with a note on what would confirm them. Why: Reduces false assumptions.
- Planning agents **MUST** produce all required artifacts in the canonical order and naming convention listed below. Why: Enables plan interchangeability between `flow` and `epic` families.
- Planning agents **MUST** infer as much as possible from the repository before asking the user anything, and when a user decision is optional they **SHOULD** default toward the best long-term developer experience that remains repo-consistent. Why: Good plans should minimize avoidable user effort without sacrificing correctness.
- All user-facing clarification questions **MUST** be asked through the built-in interactive question UI/tooling, not as ad-hoc prose in chat. Why: Structured capture makes answers explicit and reusable across personas.
- Planning agents **MUST** run clarification in two stages: an initial planner-owned clarification pass, then persona-specific clarification passes after the repo findings and initial answers are in hand. Why: The first pass captures obvious unknowns; persona passes surface deeper risks from specialized viewpoints.
- Persona clarification passes **MUST** check repository evidence, prior decisions, previous persona logs, and answers already given in chat before asking any new question. Why: Prevents duplicate questioning and rewards good repo-grounded inference.
- Each persona clarification file **MUST** end in one of these statuses: `Not Applicable`, `Passed - Already Answered From Repo`, `Passed - Already Answered In Chat`, `Passed - No Further Questions`, `Resolved After Questions`, or `Escalate`. Why: Makes completion state explicit without forcing every persona to ask something.
- Persona reviews **MUST** total twenty-two: the original Mississippi planning core plus additional cross-cutting personas for release, cost, accessibility, privacy, documentation, workflow, requirements traceability, product scope, test strategy, and supply-chain governance. Each persona acts as if it only read the plan and the repository. Why: The repository now spans framework runtime, cloud cost, docs, WebAssembly UX, release automation, and package/tooling concerns that are not covered well enough by the original twelve alone.
- Review feedback items **MUST** include issue, why it matters, proposed change, evidence or clearly-marked inference, and confidence rating. Why: Makes feedback actionable and traceable.
- Synthesis **MUST** deduplicate across all twenty-two reviews and categorize items as Must / Should / Could / Won't, with Accept/Reject rationale and required edits for each. Why: Prevents duplicate rework and clarifies priority.
- During synthesis, planning agents **MUST** accept and incorporate changes that materially improve correctness, completeness, operability, developer experience, or long-term maintainability when those changes remain within the task's true scope. They **MUST NOT** reject an in-scope improvement merely to move faster or preserve a shortcut-based plan. Why: Planning time is cheaper than rework, and the current sprint window is the right time to finish the task properly.
- Planning agents **MUST** run three full review-and-fix rounds over the draft plan unless the task is explicitly trivial. Each round **MUST** re-run the twenty-two persona reviews against the updated draft, create a new synthesis, and apply accepted changes before the next round begins. Why: The second and third rounds catch gaps introduced by earlier fixes and raise plan quality materially.
- Plans, sub-plans, and instruction extractions **MUST NOT** contain secrets, PII, or internal-only URLs. Why: Plan folders may be committed to `main` (in `epic` workflows) or reviewed by multiple agents.
- Artifact files **MUST** include a short CoV section (key claims, evidence, confidence) where applicable. Why: Maintains traceability throughout the planning trail.

## Scope and Audience

All planning agents (`flow Planner`, `epic Planner`) and building agents (`flow Builder`, `epic Builder`) in this repository.

## At-a-Glance Quick-Start

- Apply CoV loop on every significant claim: hypothesize → question → evidence → triangulate → conclude.
- Produce artifacts in order: intake → repo findings → initial clarifications → persona registry → persona clarifications → decisions → draft plan → three review rounds → final PLAN.md.
- At finalization, move the remaining audit trail artifacts into `audit/` with the `audit-` prefix, keeping only `PLAN.md` plus any epic-specific execution artifacts (currently `sub-plans/` and `dependencies.json` required by `epic-planner.agent.md`) at the plan root.

## Chain-of-Verification (CoV) Loop

For each step and each important claim, run and record:

1. **Claims / hypotheses**: what you believe is true or needs to be decided.
2. **Verification questions**: what must be true for the claim to hold.
3. **Evidence gathering**: search repo; capture file paths and line ranges when possible.
4. **Triangulation**: confirm with a second independent source (or label Single-source).
5. **Conclusion + confidence**: High / Medium / Low, plus what would raise confidence.
6. **Impact**: how this affects the plan.

## Canonical Artifact Order and Naming

| Order | Filename | Content |
|-------|----------|---------|
| 1 | `00-intake.md` | Objective, non-goals, constraints, assumptions, open questions |
| 2 | `01-repo-findings.md` | Repo evidence with two-source verification per finding |
| 3 | `02-clarifying-questions.md` | (A) Answered from repo, (B) Questions for user with ranked options, (C) answers already provided in chat |
| 4 | `03-persona-registry.md` | Reusable roster of all twenty-two personas, their remit, and what each one must validate or question |
| 5 | `04-persona-clarifications/` | One file per persona capturing repo-answered items, chat-answered items, new questions asked through interactive UI, answers, and final pass/escalation status |
| 6 | `05-decisions.md` | Decision statement, chosen option, rationale, evidence, risks, confidence |
| 7 | `06-draft-plan.md` | Full solution-level plan (architecture, contracts, work breakdown, testing, observability, rollout) |
| 8 | `07-review-round-1/` | `review-01` to `review-22` plus `review-23-synthesis.md` for round 1 |
| 9 | `08-review-round-2/` | `review-01` to `review-22` plus `review-23-synthesis.md` for round 2 |
| 10 | `09-review-round-3/` | `review-01` to `review-22` plus `review-23-synthesis.md` for round 3 |
| 11 | `PLAN.md` | Standalone final plan (root-level artifact alongside any required epic root files such as `sub-plans/` and `dependencies.json`) |

At finalization, all non-root-required artifacts move to `audit/` with `audit-` prefix: for `flow` plans this means everything except `PLAN.md`; for `epic` plans, `sub-plans/`, `dependencies.json`, and other required epic execution artifacts remain at the folder root.

### Persona clarification file template

Each file under `04-persona-clarifications/` **MUST** use this structure:

1. Persona name and remit
2. Repo-grounded answers already resolved from evidence
3. Answers already resolved from chat
4. Open uncertainties checked against prior persona files and decisions
5. New interactive questions asked to the user, if any
6. User answers and resulting updates
7. Final status (`Not Applicable`, `Passed - Already Answered From Repo`, `Passed - Already Answered In Chat`, `Passed - No Further Questions`, `Resolved After Questions`, or `Escalate`)
8. CoV note: key claims, evidence, confidence, impact on plan

### Review round protocol

Each review round **MUST** follow this sequence:

1. Run all twenty-two persona reviews against the current draft plan.
2. Create that round's `review-23-synthesis.md`.
3. Apply accepted changes to the draft plan and decisions log.
4. Record what changed because of the round.
5. Re-run the next round against the updated draft, not the original draft.

Synthesis acceptance rule:

- If a proposed change makes the system materially better and is genuinely within the requested task's scope, accept it and update the plan.
- Do not reject an in-scope improvement because it adds work, reduces implementation speed, or removes a tempting shortcut.
- Reject only when the proposal is out of scope, contradicts stronger evidence, conflicts with repo rules, or introduces unjustified risk.

Round goals:

- **Round 1**: Catch missing scope, architecture, contract, and verification gaps.
- **Round 2**: Validate that round-1 fixes did not create new contradictions, rollout issues, or DX regressions.
- **Round 3**: Final polish pass focused on completeness, consistency, and builder-readiness.

## Persona Review Roster

## Persona Boundary Rules

The personas are intentionally specialized. Planning agents **MUST** keep their scopes distinct:

- **Principal Engineer** covers broad code-health and maintainability tradeoffs, not detailed release engineering, privacy governance, or documentation quality.
- **Technical Architect** covers dependency direction and evolution shape, not deployment operations, release mechanics, or cost governance.
- **Platform Engineer** covers runtime operability, observability, and failure handling in live environments, not build/release policy design.
- **Performance and Scalability Engineer** covers throughput, latency, allocations, and resource pressure, not cloud cost optimization as a business discipline.
- **Security Engineer** covers confidentiality, integrity, authentication, authorization, and exploit paths, not broader privacy governance, retention, or data minimization policy.
- **Developer Experience (DX) Reviewer** covers developer ergonomics and API usability, not end-user workflow design, accessibility compliance, or documentation information architecture.
- **Technical Writer and Documentation IA Reviewer** covers wording, examples, structure, and discoverability of docs and guidance, not feature prioritization or product scope.
- **UX and Workflow Reviewer** covers end-user and operator flows, states, and friction, while **Accessibility and Inclusive Design Reviewer** covers inclusive access and WCAG-style concerns.
- **Business Systems Analyst** covers traceability from problem to requirement and business rules, while **Product Owner** covers value, sequencing, MVP discipline, and measurable outcomes.
- **Quality Engineering and Test Strategy Reviewer** covers verification depth, determinism, flake resistance, and test design, while **Release Engineering Reviewer** covers build/release reproducibility, rollout, rollback, and configuration integrity.
- **Supply Chain and Dependency Governance Reviewer** covers package/tool provenance, SBOM, license posture, and dependency governance, not general security threat modeling.

## Persona Review Roster

### Core planning personas (reviews 01-12)

| Review | Persona | Focus |
|--------|---------|-------|
| 01 | Marketing and Contracts | Public naming clarity, contract discoverability, package naming consistency, migration/changelog communication, and external positioning of framework capabilities |
| 02 | Solution Engineering | Business adoption readiness, ecosystem compliance, onboarding friction, integration patterns with third-party systems, and implementability in real customer solutions |
| 03 | Principal Engineer | Repo consistency, maintainability, technical risk, SOLID adherence, code-health tradeoffs, and broad implementation risk across the plan |
| 04 | Technical Architect | Architecture soundness, module boundaries, dependency direction, abstraction layering, extension seams, and long-term structural evolution |
| 05 | Platform Engineer | Runtime operability, telemetry, structured logging, distributed tracing, failure modes, diagnosis quality, deployment safety, and day-2 operations |
| 06 | Distributed Systems Engineer | Orleans actor-model correctness — grain lifecycle, reentrancy, single-activation, placement, message ordering, turn-based concurrency, and distributed failure semantics |
| 07 | Event Sourcing and CQRS Specialist | Event schema evolution, storage-name immutability, reducer purity, aggregate invariants, projection rebuild, snapshot versioning, idempotency, and command/event separation discipline |
| 08 | Performance and Scalability Engineer | Hot-path allocations, grain activation cost, Cosmos RU consumption as a performance signal, serialization overhead, N+1 patterns, back-pressure, throughput, and scale bottlenecks |
| 09 | Developer Experience (DX) Reviewer | API ergonomics, pit-of-success design, error messages, IntelliSense completeness, registration ceremony, migration friction, and sample alignment for consuming developers |
| 10 | Security Engineer | Authentication, authorization, trust boundaries, claims validation, tenant isolation, input validation, exploit paths, and secure-by-default posture |
| 11 | Source Generator and Tooling Specialist | Roslyn incremental generator correctness, caching, diagnostics, compilation performance, analyzer interaction, generated code readability, and IDE experience |
| 12 | Data Integrity and Storage Engineer | Cosmos partition key design, cross-partition cost as a storage concern, storage-name immutability, event stream consistency, snapshot correctness, idempotent writes, retention, and migration safety |

### Cross-cutting expansion personas (reviews 13-22)

| Review | Persona | Focus |
|--------|---------|-------|
| 13 | Release Engineering Reviewer | Build reproducibility, hermeticity, CI/CD fit, packaging/versioning strategy, rollout/rollback shape, release branching implications, and configuration-release coupling |
| 14 | FinOps and Cost Optimization Reviewer | Cloud spend drivers, rightsizing, cost visibility, waste reduction, unit-economics awareness, and cost-aware service or storage design |
| 15 | Accessibility and Inclusive Design Reviewer | WCAG-style accessibility, keyboard and screen-reader usability, semantic markup, contrast/state clarity, accessible samples, and documentation accessibility |
| 16 | Privacy and Data Governance Reviewer | Data minimization, purpose limitation, retention/deletion expectations, PII and claims exposure, audit/telemetry privacy, and privacy-risk tradeoffs distinct from security |
| 17 | Technical Writer and Documentation IA Reviewer | Terminology consistency, example quality, migration guidance, information architecture, discoverability, and whether the docs/supporting guidance will actually teach the feature correctly |
| 18 | UX and Workflow Reviewer | User, operator, and developer workflow coherence; loading/empty/error states; interaction friction; and whether the plan produces a coherent experience instead of just a working implementation |
| 19 | Business Systems Analyst | Traceability from problem to requirement, actor/system boundaries, business rules, acceptance outcome completeness, and requirement coverage |
| 20 | Product Owner | Outcome fit, MVP discipline, sequencing, value versus complexity, prioritization clarity, and measurable success signals |
| 21 | Quality Engineering and Test Strategy Reviewer | Test-level selection, determinism, flake risk, contract/regression coverage, mutation-strength thinking, and whether verification is strong enough for the proposed change |
| 22 | Supply Chain and Dependency Governance Reviewer | Dependency provenance, package/tool licensing posture, SBOM and artifact traceability implications, analyzer/toolchain governance, and dependency upgrade or introduction risk |

## Core Principles

- Evidence over assumption: every claim cites repo paths or is marked Single-source.
- Plans are interchangeable: any builder agent can execute a plan from any planner agent.
- Reviews stress-test from twenty-two angles — the original Mississippi planning core plus additional cross-cutting disciplines needed for framework, docs, UI, cost, release, privacy, and dependency governance work.

## References

- Instruction authoring template: `.github/instructions/authoring.instructions.md`
- RFC keywords: `.github/instructions/rfc2119.instructions.md`
- Sync policy: `.github/instructions/instruction-mdc-sync.instructions.md`
