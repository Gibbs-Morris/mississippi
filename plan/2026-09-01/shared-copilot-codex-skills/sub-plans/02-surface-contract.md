# Sub-Plan 02: Surface contract and root cutover

## Context

- Master plan: `../PLAN.md`
- Issue: #540
- This is sub-plan 02 of 24.

## Dependencies

- Depends on: 01
- Plan approval/PR 1 is required before execution.

## Objective

Define and validate canonical discovery, ownership, precedence, fallback, and
support guarantees before the pilot can write a shared skill.

## Scope

- `.github/agents/CoV-mississippi-skill-builder.agent.md`
- `.github/agents/rules-manager.agent.md`
- `docs/key-principles/github-copilot-agents.md`
- `docs/key-principles/markdown.md`
- `AGENTS.md`
- `docs/Docusaurus/docs/contributing/agent-customization.md`
- matrix/validator contract references

## Deployability

- Feature gate: compatibility behavior remains active; this is contract/docs
  work before a skill redirect.
- Safe to deploy: old guidance remains available and the cutover is validated
  before #544.

## Implementation breakdown

1. Finalize `.agents/skills/` versus `.github/skills` ownership and adapter
   policy; reject an accidental second root.
2. Specify host rows for CLI, Codex, Copilot app, VS Code, VS Code Local,
   cloud agent, code review, Visual Studio, and JetBrains.
3. Define root/nested `AGENTS.md`, skill search, instruction-glob, duplicate,
   conflict, casing, reload, and fallback semantics.
4. Remove the root instruction to read every instruction file; preserve only
   always-on invariants and resolution rules.
5. Add copy/paste onboarding, surface limitations, and migration references.

## Testing strategy

Use cold/warm/restart discovery fixtures, duplicate-root fixtures, case-folded
collisions, and host-specific smoke probes. Run the stale `.github/skills`
reference scan before allowing the pilot.

## Acceptance criteria

- [ ] One owned skill root and no unapproved stale references.
- [ ] Every claimed surface has guarantee, evidence, limitation, fallback, and
  owner.
- [ ] Existing skill-builder/rules-manager contracts can author the pilot.
- [ ] Nested guidance does not broaden Orleans rules accidentally.
- [ ] Contract documentation is published and links resolve.

## PR metadata

- Branch: `epic/shared-copilot-codex-skills/02-surface-contract`
- Title: `Define Copilot and Codex surface contract +semver: skip`
- Base: `main`
