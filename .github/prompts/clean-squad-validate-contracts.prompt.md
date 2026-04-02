---
name: "clean-squad-validate-contracts"
description: "Run Clean Squad customization parity and contract checks for maintainers."
argument-hint: "Optional focus, for example: full validation, public boundary only, or hooks diagnostics."
agent: "agent"
tools: ["clean-squad-reader", "clean-squad-delivery", "read", "search", "execute"]
---

Validate the current Clean Squad customization layer against the authoritative workflow.

1. Read [WORKFLOW](../clean-squad/WORKFLOW.md) and check the Agent Roster, frontmatter parity rules, and approved nested coordinators.
2. Inspect each `cs-*.agent.md` file under `.github/agents/` and verify frontmatter matches the workflow contract (user-invocable, agents allowlists, handoffs, description routing contract).
3. Summarize failures as actionable remediation guidance without widening scope beyond the declared Clean Squad customization surfaces.

Prompt tools override agent defaults. If the named Clean Squad tool sets are unavailable in this VS Code profile, continue with the remaining built-in tools.
