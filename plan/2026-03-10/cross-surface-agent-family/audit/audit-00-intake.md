# Intake

## Objective

Design a new repository-level family of GitHub Copilot custom agents under `.github/agents/` for a verification-first enterprise software delivery workflow that works in both VS Code and GitHub.com.

The family must provide exactly three human entry points:

- Plan
- Build
- Review

All remaining agents must be internal specialists that support the entry agents programmatically when the surface supports that workflow.

## Non-goals

- Do not modify or replace the existing agent families unless compatibility or naming conflicts require it.
- Do not introduce extra human entry agents beyond Plan, Build, and Review.
- Do not rely on surface-specific frontmatter for correctness on GitHub.com.
- Do not create repository files outside `.github/agents/` for the final implementation.
- Do not optimize the workflow for minimal implementation effort when a stronger in-scope enterprise pattern is available.

## Constraints

### User constraints

- Verify the current official custom-agent schema and behavior from GitHub Docs and VS Code Docs before creating or changing files.
- Optimize for VS Code first while preserving GitHub.com compatibility.
- Prefer one shared `.agent.md` file per agent unless official docs show a split is materially better.
- Prefer `GPT-5.4 (copilot)` where a per-agent model setting is valid.
- Do not unnecessarily constrain tools.
- Prefer VS Code `askQuestions` behavior for clarification when supported.
- Use guided handoffs only between the three entry agents if safe.
- Place all created files under `.github/agents/`.
- Use a unique short prefix across filenames, agent names, and manifest references.
- Include the required manifest, three entry agents, and the exact internal specialist set requested.

### Repository constraints

- Agent work must follow repository instructions first, then `.github/instructions/*.instructions.md`.
- Markdown must comply with repository markdown rules.
- Shared engineering guidance prefers deterministic enforcement and zero-warning quality gates.

## Initial assumptions

- None accepted without verification.
- Working assumption to validate: shared `.agent.md` files can satisfy both VS Code and GitHub.com for this family.
- Working assumption to validate: `target` can remain unset for cross-surface agents.
- Working assumption to validate: entry-agent handoffs can be added safely because GitHub.com ignores unsupported `handoffs` rather than failing.
- Working assumption to validate: internal specialists should be hidden with `user-invocable: false` and remain callable as subagents where supported.

## Open questions

- No blocking user questions identified after initial intake. The current request is specific enough to proceed with repo-grounded defaults unless later verification exposes a conflict.

## CoV

- Claim: the task is to create a cross-surface agent family, not a one-off agent. Evidence: user requirements explicitly mandate a manifest, three entry agents, and a fixed internal specialist roster. Confidence: High.
- Claim: the plan should optimize for VS Code first without breaking GitHub.com. Evidence: user requirements explicitly set an 80/20 VS Code/GitHub.com usage expectation and require compatibility. Confidence: High.
- Impact: design decisions must favor the documented common subset of agent frontmatter and use VS Code-only affordances only when GitHub.com safely ignores them.