---
applyTo: '.github/agents/cs-*.agent.md,.github/agents/*planner*.agent.md,.github/agents/*build*.agent.md,**/*.instructions.md,**/*.instructions.md,.github/copilot-instructions.md,AGENTS.md'
---

# Guidance and agent workflow contracts

Governing thought: Preserve applicable contracts through explicit scopes and verified source authority.

> Drift check: Root guidance remains effective. Formal-authoring, planner/builder, and Clean Squad sections are conditional contracts, not permission to activate those workflows. Custom agent files remain unchanged. Inspect current workflow/agent definitions before changing process requirements.

## Rules (RFC 2119)

### Formal authoring

Scope: Instruction creation/review (**/*.instructions.md), plus formal RFC wording in .github/copilot-instructions.md and root AGENTS.md; maintain original scope union and source-specific requirements.

- F-A1: Instruction files **MUST** include applyTo YAML frontmatter, H1, one-sentence governing thought, near-top Drift check, and one consolidated Rules (RFC2119) section, enabling predictable parsing.
- F-A1.2: Retain the standard instruction order: frontmatter → title/governing thought → Drift check → Rules → Scope and Audience → Quick-Start → Core Principles → optional Procedures/Examples/References.
- F-A2: RFC keywords **MUST** be uppercase. Keyword set: `MUST`, `MUST NOT`, `SHOULD`, `SHOULD NOT`, `MAY`, `REQUIRED`, `SHALL`, `SHALL NOT`.
- F-A2.2: RFC keywords **MUST** be used only for normative requirements.
- F-A2.3: In *.instructions.md files, RFC keywords **MUST NOT** appear outside Rules except quoted examples.
- F-A2.4: In formal .github/copilot-instructions.md and root AGENTS.md requirements, RFC keywords **SHOULD NOT** appear outside Rules except quoted examples.
- F-A2.5: Each instruction **MUST** contain one consolidated Rules (RFC2119) section near the top, simplifying parsing.
- F-A3: *.instructions.md files **MUST** live in .github/instructions/ with kebab-case topic names.
- F-A3.2: Each *.instructions.md file **SHOULD** cover one cohesive topic.
- F-A4: Instruction authoring **MUST** use concise factual US English.
- F-A4.2: Each rule bullet **MUST** contain a single requirement, preventing chained/conflicting rules.
- F-A4.3: Each rule bullet **MUST** include a short rationale when not obvious, preventing ambiguity.
- F-A5: Command examples **MUST** use real scripts/tools. Inspect canonical scripts/configuration before quoting behavior rather than duplicating details.
- F-A5.2: Secrets **MUST NOT** appear in instruction content or examples.
- F-A6: Instruction changes **MUST** follow repository review policy.
- F-A6.2: Instruction changes **MUST** remain in canonical Markdown.
- F-A7: Audience/scope **MUST** be explicit.
- F-A7.2: Conflicting requirement levels **MUST NOT** remain.
- F-A7.3: Authors **MUST** resolve conflicting requirement levels before merge.
- F-A8: For realistic exceptions, authors **SHOULD** use SHOULD/SHOULD NOT rather than weaken MUST.
- F-A8.2: Requirement scope **MUST** be clear.
- F-A9: Ordinary conversation, review, and PR comments use root plain-English guidance; quoting a rule or explicitly stating a formal requirement retains its wording. Ordinary messages do not require capitalized keywords.

### Planning and building agents

Scope: Files .github/agents/*planner*.agent.md and *build*.agent.md; active flow/epic Planner/Builder roles. These role requirements do not apply to every ordinary implementation plan.

- P-A1: Plans **MUST** define ordered PR outcomes, dependency/bases, estimated lines, per-layer validation, and separate/grouped landing intent under root stack policy.
- P-A2: Builders **MUST** complete current-layer advancement before implementing dependent successors, using gh stack and its linked skill; a completed plan cannot override CI/review readiness.
- P-A3: Planning agents **MUST** use CoV for every nontrivial claim: hypothesis → verification questions → repo evidence with paths/line ranges where possible → second independent source → High/Medium/Low conclusion and what raises confidence → plan impact.
- P-A3.2: Each nontrivial claim **MUST** be verified against two independent files/modules/tests/docs/config sources.
- P-A3.3: Single-source claims **MUST** be labelled Single-source with the confirmation needed.
- P-A4: Planning agents **MUST** produce all canonical artifacts in the order and with the filenames/content listed below, enabling flow/epic plan interchangeability.
- P-A4.2: At planning finalization, move non-root-required artifacts into audit/ with audit- prefix; flow retains PLAN.md, epic also retains sub-plans/, dependencies.json, and other execution artifacts required by epic-planner.agent.md.
- P-A5: Persona reviews **MUST** total12 (five enterprise generalists, seven framework specialists), each assuming only plan/repository access.
- P-A6: Feedback **MUST** include issue, impact, proposed change, evidence or marked inference, and confidence.
- P-A7: Synthesis **MUST** deduplicate all12, classify Must/Should/Could/Won't, and record Accept/Reject rationale and required edits.
- P-A8: Plans/sub-plans/instruction extractions **MUST NOT** contain secrets, PII, or internal-only URLs.
- P-A8.2: Artifact files **MUST** have applicable short CoV sections with claims/evidence/confidence.
- P-A9: The canonical twelve-review roster and its focus areas are listed below; the first five personas are enterprise generalists and the last seven are Mississippi framework specialists.

#### Canonical planning artifacts

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

#### Canonical review roster

| Review | Persona | Focus |
|--------|---------|-------|
| 01 | Marketing and Contracts | Public naming clarity, contract discoverability, package naming consistency, changelog/migration communication |
| 02 | Solution Engineering | Business adoption readiness, ecosystem compliance, onboarding friction, third-party integration |
| 03 | Principal Engineer | Repo consistency, maintainability, technical risk, SOLID adherence, test strategy, backwards compatibility |
| 04 | Technical Architect | Architecture soundness, module boundaries, dependency direction, abstraction layering, extensibility |
| 05 | Platform Engineer | Operability — telemetry, structured logging, distributed tracing, alerting, failure modes, deployment safety |
| 06 | Distributed Systems Engineer | Orleans actor-model correctness — grain lifecycle, reentrancy, single-activation, placement, message ordering, turn-based concurrency |
| 07 | Event Sourcing and CQRS Specialist | Event schema evolution, storage-name immutability, reducer purity, aggregate invariants, projection rebuild, snapshot versioning, idempotency |
| 08 | Performance and Scalability Engineer | Hot-path allocations, grain activation cost, Cosmos RU consumption, serialization overhead, N+1 patterns, back-pressure, throughput |
| 09 | Developer Experience (DX) Reviewer | API ergonomics, pit-of-success design, error messages, IntelliSense completeness, registration ceremony, migration friction |
| 10 | Security Engineer | Auth model correctness, trust boundaries, claims validation, tenant isolation, input validation, serialization attack surface, OWASP alignment |
| 11 | Source Generator and Tooling Specialist | Roslyn incremental generator correctness, caching, diagnostics, compilation performance, analyzer interaction, IDE experience |
| 12 | Data Integrity and Storage Engineer | Cosmos partition key design, cross-partition cost, storage-name immutability, event stream consistency, snapshot correctness, idempotent writes |

### Clean Squad

Scope: Only .github/agents/cs-*.agent.md and active Clean Squad work. Master .github/clean-squad/WORKFLOW.md remains authoritative; no agent file is changed or activated by this consolidation.

- CS-WORKFLOW: Agents in this Clean Squad scope **MUST** read the complete [.github/clean-squad/WORKFLOW.md](clean-squad/WORKFLOW.md), making its operational contract available.
- CS1: Only cs Entrepreneur (optional pre-governed shaping) and cs Product Owner (governed intake/orchestration) **MAY** be public.
- CS1.2: All other agents **MUST NOT** communicate directly with the user; they communicate through .thinking/ and return to their invoker. Product Owner alone speaks for governed delivery.
- CS2: Every agent **MUST** apply first principles (question assumptions, fundamental truths, upward reasoning, evidence validation), avoiding convention bias.
- CS2.2: Every agent **MUST** apply CoV to nontrivial claims (draft, verification questions, independent evidence answers, revision), avoiding hallucination. Preserve the shared reasoning templates in .github/agent-guidance/clean-squad-reasoning-examples.md.
- CS3: Governed agents **MUST** share state exclusively through `.thinking/<task-folder>/`.
- CS3.2: Governed agents **MUST** read relevant task files before output.
- CS3.3: Governed agents **MUST** record significant decisions/reasoning in `.thinking/<task-folder>/`.
- CS3.4: Entrepreneur **MAY** work before a task folder.
- CS3.5: Pre-governed Entrepreneur **MUST** read approved public workflow/user material and put reasoning in one returned Story Pack candidate or explicit stop.
- CS3.6: Governed handovers **MUST** carry task path/objective/constraints/output location.
- CS4.2: Each activity-log.md entry **MUST** include UTC time, agent/role, phase, action, changed artifacts, blockers, next action.
- CS5: workflow-audit.json **MUST** be authoritative: sequence alone orders chronology and canonical eventUtc drives timing/diagnostics.
- CS5.3: Only one canonical writer **MUST** be active for the workflow run at a time.
- CS6: Product Owner **MUST** append canonical events for Phases 1–9.
- CS6.4: All other agents **MUST NOT** write canonical facts.
- CS6.5: Entrepreneur **MUST NOT** create governed state/ledger/canonical facts.
- CS6.6: Entrepreneur **MUST** return one Story Pack candidate or explicit stop.
- CS10.3: Scribe **MUST** deterministically emit workflow-audit.md verdict Untrusted for invalid input.
- CS11: Agents **MUST** fail closed on absent reviewer-significant cause/closure/outcome/artifact lineage/provenance.
- CS11.2: Agents **MUST NOT** infer reviewer-significant semantics from sequence/summary/logs/evidence paths.
- CS13: Agents **MUST NOT** call the ledger/derived summary tamper-resistant, authenticated, or cryptographically trustworthy.
- CS15: Product Owner **MUST** perform issue intake/updates as orchestration bookkeeping before implementation; that does not grant specialist or Phase9 PR-management authority.
- CS16: Delegation **MUST** target only named agents in WORKFLOW.md Agent Roster.
- CS17: Agents in this Clean Squad scope **MUST** follow the complete [.github/clean-squad/WORKFLOW.md](clean-squad/WORKFLOW.md) contract, including phase boundaries and responsibilities, under its declared phase, role, and governed/pre-governed conditions.
- CS18: Agent description frontmatter **MUST** order primary job, Use when..., Produces..., Not for..., for unambiguous routing.
- CS19: Every significant architectural decision **MUST** get MADR4.0.0 ADR policy.
- CS19.2: Every significant architectural decision **MUST** be published in docs/Docusaurus/docs/adr/ by cs ADR Keeper.
- CS20: All produced code **MUST** apply meaningful names/small functions/single responsibility/DRY.
- CS20.2: All process decisions **MUST** follow Clean Agile scope-as-variable/continuous testing/technical discipline.
- CS21: User-facing changes **MUST** have Docusaurus docs by cs Technical Writer, reviewed by cs Doc Reviewer before PR creation.
- CS21.2: When documentation is skipped only because there are no new APIs/changed behaviors/config options, agents **MUST** record the reason at `.thinking/<task-folder>/08-documentation/scope-assessment.md`.
- CS21.3: Documentation **MUST** apply public-doc Rules and selected skill contract/local guide.
- CS22: For encountered failures/retries/nonobvious workarounds, cs Scribe **SHOULD** capture an admitted lesson in `self-taught-<domain>.instructions.md` using capture-validated-lesson skill, root self-improvement policy, and .github/agent-guidance/self-taught-format.md.

## References

- .github/clean-squad/WORKFLOW.md (phase boundaries, Agent Roster, v3 semantic/writer matrix authority)
- Unchanged .github/agents/{flow-planner,epic-planner}.agent.md govern their workflow tweaks
- docs/key-principles/{chain-of-verification,first-principles-thinking,clean-code,clean-agile}.md
- docs/Docusaurus/docs/adr/AGENTS.md; docs/Docusaurus/docs/AGENTS.md; root issue/stack/lesson policies
