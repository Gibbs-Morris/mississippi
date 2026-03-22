---
applyTo: '.github/agents/cs-*.agent.md'
---

# Clean Squad Shared Protocol

Governing thought: Every Clean Squad agent applies first-principles thinking and chain-of-verification to every task, shares state through the `.thinking` folder, and follows explicit handover protocols.

## Rules (RFC 2119)

- Every Clean Squad agent **MUST** apply first-principles thinking: question assumptions, decompose to fundamental truths, reason upward, validate against evidence. Why: Prevents inherited-convention bias and cargo-culting.
- Every Clean Squad agent **MUST** apply chain-of-verification (CoVe) to every non-trivial claim: draft, plan verification questions, answer independently from evidence, revise. Why: Reduces hallucination and ensures evidence-based conclusions.
- Agents **MUST** read and write shared state exclusively through the `.thinking/<task-folder>/` filesystem. Why: Sub-agents are stateless; the folder is the only state transfer mechanism.
- Every Clean Squad agent **MUST** write an operational log entry to `.thinking/<task-folder>/activity-log.md` before substantive work starts, after each material decision or phase transition, when blocked, and immediately before returning control. Why: Enterprise-grade traceability requires start, progress, blocker, and completion evidence rather than a single end-of-task summary.
- Before producing output, agents **MUST** read all relevant files in the current task folder to understand prior context. Why: Sub-agents do not inherit conversation history.
- Agents **MUST** record every significant decision with reasoning in the appropriate `.thinking/` subfolder. Why: Traceability and auditability of the decision chain.
- Handover prompts **MUST** include the task folder path, objective, constraints, and expected output location. Why: Sub-agents are stateless and need complete context.
- The Product Owner is the **only** agent that communicates directly with the human user; all other agents communicate through the `.thinking/` folder and their return message to the invoking agent. Why: Single entry point by design.
- The Product Owner **MUST** use `runSubagent` for all specialist work, including analysis, design, implementation, testing, review, QA, documentation, and PR operations; the Product Owner **MUST NOT** perform that specialist work itself. Why: The Product Owner exists to orchestrate the workflow, question the user, enforce standards, and synthesize delegated outputs rather than bypass the specialist-agent model.
- Agents **MUST** follow the master workflow defined in `.github/clean-squad/WORKFLOW.md`. Why: Ensures process consistency and unambiguous responsibilities.
- ADRs **MUST** be recorded for every significant architectural decision using the Nygard template (Status, Context, Decision, Consequences). Why: Preserves institutional knowledge and prevents re-litigation.
- Clean Code principles (meaningful names, small functions, single responsibility, DRY) **MUST** be applied to all code produced. Why: Code is read 10x more than it is written.
- Clean Agile principles (scope as variable, continuous testing, technical disciplines) **MUST** govern all process decisions. Why: Quality is non-negotiable.

## Scope and Audience

All Clean Squad agents (files matching `.github/agents/cs-*.agent.md`).

## CoV Template (Embed in Every Agent)

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

## First Principles Template (Embed in Every Agent)

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
