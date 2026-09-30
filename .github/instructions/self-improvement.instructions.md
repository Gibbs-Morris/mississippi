---
applyTo: '**'
---

# Self-Improvement Learning System

Governing thought: Agents record validated lessons from real-work failures into typed `self-taught-<domain>.instructions.md` files, creating persistent institutional memory that prevents repeated mistakes without conflicting with hand-authored rules.

> Drift check: Review `.github/agents/rules-manager.agent.md` for the conflict-detection process before adding lessons; open relevant domain instruction files to check for overlap.

## Rules (RFC 2119)

- Covered contributors **MUST** read and apply [the complete global policy](../agent-guidance/global-policy.md) under [root instruction loading](../../AGENTS.md#instruction-loading), retaining the audience and task conditions below. Why: These obligations remain mandatory independently of skill selection.
- For lesson admission, conflict assessment, or lesson writing, contributors **MUST** follow [capture-validated-lesson](../../.agents/skills/capture-validated-lesson/SKILL.md) with [its local binding](../agent-guidance/self-taught-format.md). Why: The procedure is explicit; policy obligations remain effective independently of skill activation.
- When applicable self-taught lessons conflict at equal authority, contributors **MUST** follow [peer-conflict recording and reconciliation](../agent-guidance/self-taught-format.md#peer-lesson-conflicts). Why: The all-agent route preserves existing lessons and leaves proposed lessons unwritten until an authorized resolution.

## Scope and Audience

All agents and contributors; applies whenever an agent encounters a recoverable failure, retry, or non-obvious workaround during any workflow.

The peer-lesson conflict route applies to all agents and contributors whenever
applicable self-taught lessons contradict each other, including outside Scribe
workflows or lesson-writing tasks.

## Conflict Detection Protocol

Apply [the lesson-admission and conflict policy](../agent-guidance/global-policy.md#learning-efficiency-and-communication) with the linked skill and local format before writing. Existing Scribe callers retain this entrypoint.
