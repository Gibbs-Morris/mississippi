# Sub-Plan 09a: Custom-agent classification

## Context

- Master plan: `../PLAN.md`
- Issue: #557
- This is sub-plan 09a of 24.

## Dependencies

- Depends on: 01, 06; PR 1 and a #545 `go` are required.

## Objective

Give every current custom agent exactly one evidence-backed disposition.

## Scope

- all 85 `.github/agents/*.agent.md` files
- handoffs, tools, allowlists, user-invocable metadata, and consumer references
- matrix disposition and compatibility report

## Deployability

- Feature gate: classification only; no agent is deleted or redirected.
- Safe to deploy: current agent set remains unchanged.

## Implementation breakdown

1. Record purpose, trigger, tools, handoffs, context-isolation value, consumers,
   duplicated procedures, and host compatibility.
2. Classify each as retain, thin, merge, retire, or extract-to-skill.
3. Identify reviewer/planning families and redundant personas.
4. Map reusable procedures to approved/proposed skills and identify rollback.
5. Require retained agents to have unique picker metadata and a demonstrated
   isolation, persona, delegation, or tool-boundary reason.

## Testing strategy

Validate 85/85 rows, inbound references, handoff graph, allowlists, cycles,
user-invocable/argument metadata, and host discovery fixtures.

## Acceptance criteria

- [ ] Exactly one disposition per agent.
- [ ] No retirement without consumer and unique-behavior evidence.
- [ ] All hard-coded path/agent references are dispositioned.
- [ ] Compatibility implications for every claimed surface are recorded.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/09a-agent-classification`
- Title: `Classify custom-agent dispositions +semver: skip`
- Base: `main`
