# Amendment 3 Review — Solution Engineering

## Persona

Solution Engineering — business adoption readiness, ecosystem/standards compliance, onboarding friction, integration patterns with third-party systems.

## Findings

### 1. FLAW — No triviality threshold for working directory creation

- **Issue**: The plan says "Every non-trivial `vfe` workflow must use a persistent working directory" but never defines trivial vs. non-trivial. A quick single-file code review or a one-question clarification doesn't need 10 Markdown files.
- **Why it matters**: Onboarding friction. If a user picks `VFE Review` to review one small commit and the agent immediately creates 10 files, the ceremony-to-value ratio is terrible. Users will abandon the family.
- **Proposed change**: Define a triviality test. Example: "If the task can complete in a single turn with no specialist invocation and no handoff, working-directory creation is optional. The entry agent should ask or infer." Add this to the Working Directory Contract section.
- **Evidence**: Working Directory Contract says "every non-trivial vfe workflow" with no definition of "non-trivial."
- **Confidence**: High.

### 2. FLAW — No explicit plan approval marker

- **Issue**: VFE Build's mission says "Implement an approved plan" and its workflow says "Read approved plan artifacts." But there is no explicit mechanism in the working directory to mark a plan as approved vs. draft vs. rejected. `01-plan.md` exists in both states.
- **Why it matters**: A Build agent could pick up a draft plan that Plan hasn't finished reviewing. There's no status gate.
- **Proposed change**: Require `01-plan.md` to have a `## Status` header at the top with values like `draft`, `in-review`, `approved`, `superseded`. VFE Build must check this status before proceeding.
- **Evidence**: VFE Build workflow step 1 says "Read approved plan artifacts" but no file or field marks the plan as approved.
- **Confidence**: High.

### 3. GAP — No guidance on resuming interrupted workflows

- **Issue**: The plan describes how workflows start and hand off but doesn't describe what happens when a user returns to a partially-completed workflow after a session break. Which files should the agent read? How does it reconstruct state?
- **Why it matters**: Enterprise workflows routinely span multiple sessions. If the agent can't resume from the working directory alone, the durability promise is hollow.
- **Proposed change**: Add a "Resumption Protocol" to the Working Directory Contract: "When an entry agent is invoked and a working directory already exists for the task, it must read `09-handoff.md` first to understand current state, then inspect artifact statuses before proceeding."
- **Evidence**: The handoff rule says update `09-handoff.md` but never describes the reverse — reading it to reconstruct state.
- **Confidence**: High.

### 4. GAP — No working directory uniqueness or collision handling

- **Issue**: The path format is `./plan/YYYY-MM-DD/<task-slug>/`. If two tasks start on the same day with similar names, the slugs could collide.
- **Why it matters**: Overwriting another task's artifacts is a data-integrity issue.
- **Proposed change**: Add a collision-avoidance rule: "If the directory already exists and contains artifacts from a different task, the agent must append a disambiguator (e.g., `-2`) or ask the user."
- **Evidence**: Working Directory Contract defines the path pattern but has no collision handling.
- **Confidence**: Medium.

### 5. MINOR — No third-party integration guidance

- **Issue**: The plan is silent on how the VFE family interacts with external tools beyond MCP. For example, what if a user wants VFE Build to integrate with Jira, Azure DevOps, or a ticketing system?
- **Why it matters**: Enterprise adoption often requires integration with existing workflows.
- **Proposed change**: This is acceptable for v1 — the family inherits all available tools. Add a brief note in the manifest requirements that integration with external systems is via MCP servers and available tools, not hard-coded.
- **Evidence**: Tools note says "Keep tools unset… agents inherit all available tools."
- **Confidence**: Low — likely fine for v1, just worth noting.
