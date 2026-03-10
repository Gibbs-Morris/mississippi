# Amendment 3 Review — Principal Engineer

## Persona

Principal Engineer — repo consistency, maintainability, technical risk, SOLID adherence, test strategy adequacy, backwards compatibility.

## Findings

### 1. FLAW — No error recovery or failure protocol

- **Issue**: The plan describes the happy path exhaustively but is completely silent on what happens when things go wrong. What if:
  - A specialist returns output that violates its remit?
  - Build fails repeatedly and the agent can't fix it?
  - A working-directory file is missing or corrupted?
  - A handoff file references artifacts that don't exist?
  - The plan-amendment review loop fails to converge?
- **Why it matters**: In production use, failures are the norm. Without defined recovery behavior, agents will either hallucinate recovery steps or get stuck.
- **Proposed change**: Add a "Failure Protocol" section to the plan that defines:
  1. **Specialist remit violation**: Discard out-of-scope directives, log the violation as an observation.
  2. **Repeated build failure**: After N attempts (suggest 5, consistent with repo's build-issue-remediation protocol), stop the slice, record the blocker in `03-build-log.md`, and update `09-handoff.md` with context for human intervention.
  3. **Missing/corrupt artifacts**: Attempt reconstruction from remaining files; if impossible, ask the user.
  4. **Non-converging review loop**: Cap plan-amendment review reruns at 3 cycles per amendment, then escalate to user.
- **Evidence**: No "failure", "error", "recovery", or "fallback" language appears anywhere in PLAN.md. The repo's build-issue-remediation.instructions.md caps attempts at 5, but the VFE plan doesn't reference this.
- **Confidence**: High.

### 2. FLAW — Circular handoff potential with no termination guarantee

- **Issue**: Plan → Build → Review → Plan → Build → ... is allowed with no cycle cap. The plan-amendment rule compounds this: Build finds a problem → stops → reruns planning review → reviews find more issues → Build restarts → finds another problem.
- **Why it matters**: An infinite planning-review loop burns tokens and time without progress.
- **Proposed change**: Define a circuit-breaker: "If a task returns to Plan from Build or Review more than twice, the agent must escalate to the user with a summary of what's preventing progress rather than starting another cycle."
- **Evidence**: Handoff sections for all three entry agents show bidirectional handoffs. No cycle limit exists.
- **Confidence**: High.

### 3. FLAW — Specialist conflict resolution undefined

- **Issue**: When multiple specialists review the same plan concurrently, their findings may conflict. The adjudication step says "accept / reject / defer / needs-decision" but doesn't define the adjudicator's decision framework. Who wins when the security specialist says "add a validation layer" and the performance specialist says "that layer adds unacceptable latency"?
- **Why it matters**: Without a decision framework, adjudication is arbitrary.
- **Proposed change**: Add a priority framework to the Internal Specialist Requirements section: "When specialist findings conflict, the adjudicating entry agent must prioritize: (1) correctness and data integrity, (2) security, (3) repo policy compliance, (4) operability, (5) performance, (6) developer experience. Conflicts must be recorded in `08-decisions.md` with rationale."
- **Evidence**: Internal Specialist Requirements defines adjudication categories but not resolution priority.
- **Confidence**: High.

### 4. GAP — No explicit relationship with git operations

- **Issue**: VFE Build talks about "commits," "push," "create or update PR," "commit and push," "commit plan," "shape commits." But it never specifies how the agent should interact with git. Terminal commands? MCP GitHub tools? VS Code SCM interface?
- **Why it matters**: The plan is implementation-grade but leaves a fundamental implementation question open.
- **Proposed change**: Add a brief guideline: "Build should use available git tools (terminal, MCP, or SCM as appropriate to the surface). The plan does not prescribe a specific git interface — the agent should use what is available and functional."
- **Evidence**: VFE Build workflow references commits, pushes, and PRs throughout but never mentions git, terminal, MCP, or SCM.
- **Confidence**: Medium — arguably this is an implementation detail the builder can figure out, but it's a notable omission for an "implementation-grade" plan.

### 5. MINOR — Plan Amendment materiality threshold is subjective

- **Issue**: "Any material change to the approved plan" triggers the full review rerun. "Material" is not defined. A typo fix in the plan text would technically be a "change" — should it trigger 18 specialist reruns?
- **Why it matters**: Without a threshold, the rule is either over-triggered (wasteful) or under-triggered (defeats the purpose).
- **Proposed change**: Define materiality: "A change is material if it affects architecture, public contracts, slice scope, acceptance criteria, or NFRs. Wording clarifications, typo fixes, and reordering within a slice are not material."
- **Evidence**: Plan Amendment Review Rule says "any material change" without defining material.
- **Confidence**: High.
