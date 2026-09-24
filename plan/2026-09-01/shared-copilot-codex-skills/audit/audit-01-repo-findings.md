# Repository findings

## Baseline and completed pre-work

| Snapshot | Instructions | Exact global | Instruction lines | Agents | Agent lines | Cursor mirrors | Skills |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Initial issue baseline | 52 | 16 | approximately 3,270 / 950 global | 85 | approximately 11,286 | 52 | 0 |
| `ba4d1b93` after #541/#563 | 51 | 15 | 3,231 / 913 global | 85 | 11,285 | 0 | 0 |

Evidence: issue #532/#539; clean-checkout scans of
`.github/instructions/*.instructions.md`, `.github/agents/*.agent.md`, and
retired Cursor paths; `GitVersion.yml:56-63`. The final report must preserve
both rows and explain the delta through PR #529 and PR #530.

## Current placement evidence

- `AGENTS.md` is root always-on guidance but currently directs agents to read
  all `.github/instructions` files, which defeats scoped loading.
- `.github/copilot-instructions.md` is exact-global Copilot guidance and
  contains build/cleanup/package/SOLID/README/scratchpad procedures.
- 15 exact-global instruction files mix invariants, workflows, review process,
  and deterministic requirements.
- 85 agents split into 7 CoV, 33 Clean Squad, 36 VFE, and 9 other definitions.
- `.github/agents/CoV-mississippi-skill-builder.agent.md`,
  `rules-manager.agent.md`, and key-principles docs still hard-code
  `.github/skills`.
- #541 corrected discovery-risk filenames; #563 removed Cursor mirrors,
  synchronization instructions, generator, and references.

## Required ownership model

| Concern | Owner | Allowed consumers |
| --- | --- | --- |
| Mandatory invariant | Root/nested `AGENTS.md` | All applicable hosts |
| Outcome workflow | `.agents/skills/<id>/SKILL.md` | Codex/Copilot and adapters |
| Persona/isolation/delegation/tool boundary | Host-specific agent | Its host/orchestrator |
| Deterministic check | CI/analyzer/hook/canonical script | All workflows |
| Detailed reference | Skill `references/` or Docusaurus | Skills/docs |
| Copilot-only routing | Thin `.github` adapter | Copilot surfaces |
| Validation and maintenance | Validator, matrix, CODEOWNERS | Contributors/CI |

Dependencies point from canonical content to adapters. An adapter may route to
or project canonical content, but cannot become a second normative source.

## Runtime-contract preservation

The inventory must separately register Orleans wire contracts, persisted event
and snapshot identities, command/event boundaries, reducer and aggregate
invariants, snapshot version/reducer-hash behavior, registry collisions, and
retry/idempotency semantics. This is a preservation gate, not authorization to
change runtime code. Persisted `[EventStorageName]` and
`[SnapshotStorageName]` values and computed alias-derived names are immutable.

## CoV

1. **Claim:** Existing guidance can be classified by destination without
   losing behavior.
2. **Verification:** Compare file scans with issue baselines, inspect actual
   frontmatter/scopes, inspect agents and consumer docs, and inspect completed
   pre-work history.
3. **Triangulation:** Counts are independently supported by issue text and
   clean-checkout scans; placement is supported by current files and official
   host documentation.
4. **Conclusion:** The matrix must be row-level and commit-anchored, and must
   include hard-coded maintenance consumers beyond the 51/85 file counts.
5. **Confidence/impact:** High; #539 is the first executable sub-plan.
