---
applyTo: '.github/agents/*planner*.agent.md,.github/agents/*build*.agent.md'
---

# Agent Planning Methodology

Governing thought: All planning and building agents share a common Chain-of-Verification loop, persona review roster, and artifact naming scheme so plans are consistent and interchangeable.

> Drift check: Open `flow-planner.agent.md` and `epic-planner.agent.md` before modifying; agents are authoritative for their own workflow tweaks.

## Rules (RFC 2119)

- Planning agents **MUST** apply the Chain-of-Verification (CoV) loop on every non-trivial claim: hypothesize → question → gather evidence → triangulate with a second independent source → conclude with confidence rating → record impact. Why: Prevents speculative plans.
- Each non-trivial claim **MUST** be verified against at least two independent sources (different files, modules, tests, docs, or configs); single-source claims **MUST** be labelled **Single-source** with a note on what would confirm them. Why: Reduces false assumptions.
- Planning agents **MUST** produce all required artifacts in the canonical order and naming convention listed below. Why: Enables plan interchangeability between `flow` and `epic` families.
- Persona reviews **MUST** total twelve: five enterprise generalist plus seven Mississippi framework specialist, each acting as if they only read the plan and the repository. Why: Ensures comprehensive coverage from business, architecture, and domain-specific perspectives.
- Review feedback items **MUST** include issue, why it matters, proposed change, evidence or clearly-marked inference, and confidence rating. Why: Makes feedback actionable and traceable.
- Synthesis **MUST** deduplicate across all twelve reviews and categorize items as Must / Should / Could / Won't, with Accept/Reject rationale and required edits for each. Why: Prevents duplicate rework and clarifies priority.
- Plans, sub-plans, and instruction extractions **MUST NOT** contain secrets, PII, or internal-only URLs. Why: Plan folders may be committed to `main` (in `epic` workflows) or reviewed by multiple agents.
- Artifact files **MUST** include a short CoV section (key claims, evidence, confidence) where applicable. Why: Maintains traceability throughout the planning trail.

## Scope and Audience

All planning agents (`flow Planner`, `epic Planner`) and building agents (`flow Builder`, `epic Builder`) in this repository.

## At-a-Glance Quick-Start

- Apply CoV loop on every significant claim: hypothesize → question → evidence → triangulate → conclude.
- Produce artifacts in order: intake → repo findings → clarifying questions → decisions → draft plan → 12 reviews → synthesis → final PLAN.md.
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
| 3 | `02-clarifying-questions.md` | (A) Answered from repo, (B) Questions for user with ranked options |
| 4 | `03-decisions.md` | Decision statement, chosen option, rationale, evidence, risks, confidence |
| 5 | `04-draft-plan.md` | Full solution-level plan (architecture, contracts, work breakdown, testing, observability, rollout) |
| 6 | `review-01` to `review-12` | Twelve persona reviews (see roster below) |
| 7 | `review-13-synthesis.md` | Deduplicated feedback: Must / Should / Could / Won't |
| 8 | `PLAN.md` | Standalone final plan (root-level artifact alongside any required epic root files such as `sub-plans/` and `dependencies.json`) |

At finalization, all non-root-required artifacts move to `audit/` with `audit-` prefix: for `flow` plans this means everything except `PLAN.md`; for `epic` plans, `sub-plans/`, `dependencies.json`, and other required epic execution artifacts remain at the folder root.

## Persona Review Roster

### Enterprise generalist personas (reviews 01-05)

| Review | Persona | Focus |
|--------|---------|-------|
| 01 | Marketing and Contracts | Public naming clarity, contract discoverability, package naming consistency, changelog/migration communication |
| 02 | Solution Engineering | Business adoption readiness, ecosystem compliance, onboarding friction, third-party integration |
| 03 | Principal Engineer | Repo consistency, maintainability, technical risk, SOLID adherence, test strategy, backwards compatibility |
| 04 | Technical Architect | Architecture soundness, module boundaries, dependency direction, abstraction layering, extensibility |
| 05 | Platform Engineer | Operability — telemetry, structured logging, distributed tracing, alerting, failure modes, deployment safety |

### Mississippi framework specialist personas (reviews 06-12)

| Review | Persona | Focus |
|--------|---------|-------|
| 06 | Distributed Systems Engineer | Orleans actor-model correctness — grain lifecycle, reentrancy, single-activation, placement, message ordering, turn-based concurrency |
| 07 | Event Sourcing and CQRS Specialist | Event schema evolution, storage-name immutability, reducer purity, aggregate invariants, projection rebuild, snapshot versioning, idempotency |
| 08 | Performance and Scalability Engineer | Hot-path allocations, grain activation cost, Cosmos RU consumption, serialization overhead, N+1 patterns, back-pressure, throughput |
| 09 | Developer Experience (DX) Reviewer | API ergonomics, pit-of-success design, error messages, IntelliSense completeness, registration ceremony, migration friction |
| 10 | Security Engineer | Auth model correctness, trust boundaries, claims validation, tenant isolation, input validation, serialization attack surface, OWASP alignment |
| 11 | Source Generator and Tooling Specialist | Roslyn incremental generator correctness, caching, diagnostics, compilation performance, analyzer interaction, IDE experience |
| 12 | Data Integrity and Storage Engineer | Cosmos partition key design, cross-partition cost, storage-name immutability, event stream consistency, snapshot correctness, idempotent writes |

## Core Principles

- Evidence over assumption: every claim cites repo paths or is marked Single-source.
- Plans are interchangeable: any builder agent can execute a plan from any planner agent.
- Reviews stress-test from twelve angles — five business/architecture, seven Mississippi domain.

## References

- Instruction authoring template: `.github/instructions/authoring.instructions.md`
- RFC keywords: `.github/instructions/rfc2119.instructions.md`
- Sync policy: `.github/instructions/instruction-mdc-sync.instructions.md`
