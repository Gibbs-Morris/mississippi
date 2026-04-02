---
name: clean-squad-subagent-orchestration
description: 'Nested coordinator guidance for Clean Squad. Use when River or an approved phase coordinator needs allowlisted subagents, deterministic parallel batches, or disabled-mode fallback.'
user-invocable: false
---

# Clean Squad Subagent Orchestration

This skill captures the bounded nested-subagent rules for Clean Squad.

## Preconditions

1. The active agent is River or an approved phase coordinator.
2. The agent has an explicit `agents:` allowlist for the intended worker set.
3. The `chat.subagents.allowInvocationsFromSubagents` setting is enabled before a nested coordinator attempts fan-out.

## Deterministic batch procedure

1. Create one immutable batch or iteration ID.
2. Create one immutable input manifest with artifact paths and, when practical, content digests.
3. Assign each worker a unique output path or bundle path.
4. Record the expected worker roster before execution starts.
5. Cap concurrency explicitly for the phase.
6. Fan in by declared roster order, not completion order.
7. Produce exactly one synthesis or join artifact for the parent coordinator.

## Disabled-mode fallback

When nested subagents are disabled, blocked, or unavailable:

1. Do not widen the allowlist.
2. Fall back to direct River delegation or existing artifacts only.
3. Keep the same declared worker roster and output paths where possible.
4. Record the degraded mode explicitly in the returned artifact and status envelope.

## Boundaries

- Non-coordinators do not invent new delegation paths.
- Recursive delegation is not approved for the current Clean Squad fleet.
- The VS Code maximum nesting depth of 5 is an upper bound, not a target.
