# Intake: shared Copilot and Codex skills

## Objective

Use a strangler migration to reduce duplicated Mississippi repository AI
guidance into portable, outcome-oriented Agent Skills while preserving
always-on engineering invariants, host-specific agent value, deterministic
quality gates, and the supported Copilot surfaces.

## Scope

- Inventory and classify all current instruction and custom-agent content.
- Define the Copilot/Codex customization contract and portable skill standard.
- Build repeatable structural, activation, output, security, and portability
  evaluations.
- Pilot build-failure remediation, then migrate approved engineering and
  documentation workflows.
- Rationalize custom-agent shells and consolidate mandatory guidance.
- Publish final support, maintenance, rollback, and baseline metrics.

## Non-goals

- No application/runtime feature work.
- No one-skill-per-file or one-skill-per-persona conversion.
- No mandatory invariant whose only home is an implicitly activated skill.
- No reintroduction of Cursor mirrors or synchronization tooling.
- No deletion of old guidance before replacement evidence and rollback exist.

## Constraints and assumptions

- The repository is pre-1.0 (`GitVersion.yml:61`), but persisted storage
  identities remain immutable.
- `.agents/skills/` is the proposed portable target; #540 must finalize the
  cross-surface contract before #544 can implement the pilot.
- Copilot CLI and Codex are primary conformance targets. Other named Copilot
  surfaces remain supported with explicitly stated, lower-tier guarantees.
- Plan-only delivery is requested; no repository files, skills, agents, or
  issues are changed in this planning turn.

## CoV

1. **Claim:** The programme can reduce duplication without treating CLI as the
   only supported host.
2. **Questions:** Which paths are currently canonical? Which hosts discover
   each path? Which content must remain always-on? What evidence is required
   before deletion?
3. **Evidence:** Issue #532 and #533–#564; root `AGENTS.md`;
   `.github/copilot-instructions.md`; 51 instruction files; 85 agent files;
   `GitVersion.yml`; official GitHub Agent Skills and OpenAI Codex guidance.
4. **Conclusion:** Use a portable shared layer plus explicit host adapters and
   deterministic enforcement; defer any hard-cutover claim to #540.
5. **Confidence/impact:** High confidence; all later work is gated on the
   contract and pilot.
