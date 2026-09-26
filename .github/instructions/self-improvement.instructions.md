---
applyTo: '**'
---

# Self-Improvement Learning System

Governing thought: Agents record validated lessons from real-work failures into typed `self-taught-<domain>.instructions.md` files, creating persistent institutional memory that prevents repeated mistakes without conflicting with hand-authored rules.

> Drift check: Review `.github/agents/rules-manager.agent.md` for the conflict-detection process before adding lessons; open relevant domain instruction files to check for overlap.

## Rules (RFC 2119)

- When an agent encounters an issue that required retry, rework, or a non-obvious workaround, it **SHOULD** capture a concise lesson in the appropriate `self-taught-<domain>.instructions.md` file. Why: Prevents the same mistake in future conversations.
- When resolving a problem reveals a failure mode, workaround, or process gap that could recur across multiple tasks—not merely the same task's exact code or data—the agent **MUST** record the validated lesson in the appropriate `self-taught-<domain>.instructions.md` file after resolution, once the conflict and duplicate checks in this policy pass. Why: Generalizable learning belongs in the shared instruction context while hand-authored rules and existing lessons remain authoritative.
- Self-taught files **MUST** live in `.github/instructions/` and follow the naming pattern `self-taught-<domain>.instructions.md` using kebab-case domain names. Why: Consistent discoverability alongside hand-authored instructions.
- Self-taught files **MUST** follow the standard authoring template (YAML front matter with `applyTo`, H1, governing thought, drift check, Rules section, references). Why: Parseable by humans and automation.
- Each lesson **MUST** be a single, concise bullet in the Rules section using an RFC 2119 keyword and a "Why:" suffix citing the observed failure (error message, build output, or specific scenario). Why: Token economy requires density; evidence anchors credibility.
- Before adding a lesson, the agent **MUST** read all existing instruction files whose `applyTo` scope overlaps with the target self-taught file and verify the new lesson does not contradict any hand-authored rule. Why: Hand-authored rules are the source of truth; self-taught lessons supplement but never override.
- If a proposed lesson conflicts with a hand-authored rule, the agent **MUST NOT** add the lesson and **SHOULD** instead record a note in `.thinking/` (if a task folder is active) explaining the conflict for human review. Why: Prevents institutional memory from corrupting authoritative policy.
- Lessons in self-taught files **MUST** be treated as supplementary guidance; when any conflict exists between a self-taught lesson and a hand-authored instruction, the hand-authored instruction **MUST** take precedence. Why: Establishes a clear authority hierarchy.
- Self-taught files **SHOULD** stay under 30 items per file; when a file approaches this limit, lessons **SHOULD** be reviewed and either promoted to the relevant hand-authored instruction file (with human approval) or retired if no longer relevant. Why: Keeps token footprint manageable per Rules Manager token-economy principles.
- Duplicate or near-duplicate lessons **MUST NOT** be added; agents **MUST** check the existing lessons in the target file before writing. Why: Prevents bloat and redundancy.
- Self-taught files **MUST NOT** contain opinions, preferences, or speculative guidance; every lesson **MUST** trace to a concrete, observed failure or inefficiency. Why: Empirical evidence only; no cargo-culting.
- New domains **MAY** be created when no existing domain covers the lesson; the agent **MUST** select the `applyTo` pattern that best matches the domain scope. Why: Categories grow organically with the codebase.

## Scope and Audience

All agents and contributors; applies whenever an agent encounters a recoverable failure, retry, or non-obvious workaround during any workflow.

## Lesson capture workflow

Use [capture-validated-lesson](../../.agents/skills/capture-validated-lesson/SKILL.md)
for lesson admission, conflict assessment, and bounded writing. Read the
[local self-taught format](../agent-guidance/self-taught-format.md) when
selecting a scope or creating a file. If automatic skill discovery is
unavailable, read both linked files directly. The twelve rules above remain
effective whether or not the skill is loaded.

## Conflict Detection Protocol

Compatibility entrypoint for unchanged callers such as Scribe: apply the
[rules above](#rules-rfc-2119), then read the linked lesson-capture skill and
local format before conflict checks or lesson writing. This anchor preserves
the existing consumer reference without duplicating the procedure.

## References

- Rules Manager agent: `.github/agents/rules-manager.agent.md`
- Instruction authoring: `.github/instructions/authoring.instructions.md`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
- Codex instruction discovery: `AGENTS.md#instruction-loading`
