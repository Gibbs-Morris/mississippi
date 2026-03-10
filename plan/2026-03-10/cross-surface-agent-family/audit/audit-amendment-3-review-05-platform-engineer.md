# Amendment 3 Review — Platform Engineer

## Persona

Platform Engineer — operability, telemetry, structured logging, distributed tracing, alerting hooks, failure modes, night-time diagnosis, deployment rollout safety.

## Findings

### 1. FLAW — No observability of the agent workflow itself

- **Issue**: The plan meticulously describes observability for the *code being built* (via the observability specialist) but says nothing about observability of the *agent workflow*. When a VFE workflow takes 3 hours and involves 12 specialist invocations, there's no structured way to see what happened, how long each phase took, or where time was spent.
- **Why it matters**: Diagnosing why a VFE workflow failed or was slow is impossible without structured logs of the workflow itself.
- **Proposed change**: Add to the Working Directory Contract: "Each specialist invocation must be logged in the build log or review summary with: specialist name, purpose, key findings count, and outcome. `09-handoff.md` must include a brief timeline of phases completed."
- **Evidence**: Working Directory Artifact Expectations during Execution mentions logging specialist reviews but doesn't require structured timing or invocation tracking.
- **Confidence**: Medium — this is a "nice to have" for v1 but becomes essential at scale.

### 2. GAP — Working directory retention rule is a non-answer

- **Issue**: The retention rule says "The implementation should make clear when it is expected to remain for review and when a later delivery workflow may archive or remove it." This is a requirement to write a requirement. It doesn't actually define a retention policy.
- **Why it matters**: Working directories will accumulate. Without a clear lifecycle, `/plan/` becomes a dumping ground.
- **Proposed change**: Define a concrete retention policy: "Working directories remain until the associated PR is merged or closed. After merge, archival or deletion is at the team's discretion. The manifest should note this lifecycle."
- **Evidence**: Retention Rule section in Working Directory Contract.
- **Confidence**: High.

### 3. GAP — No resource-cost awareness

- **Issue**: The plan invokes up to 18 specialists per round, potentially multiple rounds per phase, across three phases. At GPT-5.4 rates, a single VFE workflow could consume significant token budgets. The plan doesn't acknowledge or mitigate this.
- **Why it matters**: Enterprise teams have budgets. An uncapped workflow that burns tokens without progress (see circular handoff issue) is a cost incident.
- **Proposed change**: Add a note in the Guardrails section of the prompt template: "Be cost-aware. Invoke only the specialists relevant to the current scope. Prefer narrowing the specialist set when the task is small or well-understood."
- **Evidence**: The conditional invocation guidance section exists and partially addresses this, but it's in the specialist section, not in the prompt template guardrails where the agent would see it.
- **Confidence**: Medium.

### 4. MINOR — No way to diagnose a stuck workflow without reading all files

- **Issue**: If a user returns to a stuck workflow, they'd need to read `00-intake.md` through `09-handoff.md` to figure out what happened. There's no summary file or dashboard.
- **Why it matters**: Discoverability of workflow state.
- **Proposed change**: Require `09-handoff.md` to always include a "Current Status Summary" section at the top: phase, blockers, last action, next expected action.
- **Evidence**: Handoff Rule says "reference specific files the next agent should read first" but doesn't require a status summary.
- **Confidence**: Medium.
