# Clean Squad Subagent Contracts

This document summarizes the executable Clean Squad subagent contract. `WORKFLOW.md` remains authoritative; this file is the maintainer-friendly companion.

## Public boundary

- Only `cs Entrepreneur` and `cs River Orchestrator` are user-invocable.
- `cs Entrepreneur` is pre-governed and non-delegating.
- `cs River Orchestrator` is the only governed human-facing orchestrator and canonical writer.
- The only public handoff is `cs Entrepreneur` -> `cs River Orchestrator` with `send: false`.

## Protected workers

- Every internal Clean Squad agent sets `user-invocable: false`.
- Approved coordinator allowlists remain the supported control surface for invoking internal workers as subagents.
- Non-delegating agents use `agents: []`.

## Approved coordinators

| Coordinator | Allowed workers | Nested policy |
|---|---|---|
| `cs River Orchestrator` | All internal Clean Squad agents | allowlisted |
| `cs Three Amigos Synthesizer` | Business Analyst, Tech Lead, QA Analyst, Developer Evangelist | allowlisted |
| `cs Code Review Synthesizer` | Review personas and relevant allowlisted experts | allowlisted |
| `cs QA Synthesizer` | QA Lead, QA Exploratory, Test Engineer | allowlisted |
| `cs Documentation Scope Synthesizer` | Technical Writer, Doc Reviewer | allowlisted |

No recursive coordinators are approved in the current fleet.

## Deterministic batch contract

Every nested or parallel wave must have:

1. immutable batch or iteration ID
2. immutable input manifest
3. unique worker output paths
4. expected worker roster recorded up front
5. deterministic roster-order fan-in
6. terminal state for every expected worker
7. one explicit synthesis artifact
8. explicit degraded-mode recording when nested delegation is unavailable

## Tool strategy

- River and approved phase coordinators pin only a broad explicit core tool baseline because `agents:` requires the `agent` tool.
- Leaf workers inherit tools unless a stricter boundary is truly required.
- Tool sets are ergonomic only. The current VS Code implementation exposes user-profile `.toolsets.jsonc` files rather than a workspace-loaded contract, so the repo ships a template and guidance instead of a normative workspace tool-set authority surface.

## Skills and prompts

- Skills centralize repeated procedures.
- Prompt files provide bounded operator launchers.
- Neither skills nor prompts are authority surfaces for public-boundary or canonical-ownership rules.

## Hooks pilot

- The current pilot is a non-destructive `PostToolUse` reminder for customization edits.
- Hooks remain optional and the workflow must stay operable when they are disabled by policy or unavailable in the current environment.
