---
applyTo: 'eng/tests/**/*.ps1'
---

# Self-Taught Lessons: PowerShell

Governing thought: Preserve collection shape when test fixtures return optional results.

> Drift check: Check the fixture and its registered runner's strict-mode behavior before applying this lesson.

## Rules (RFC 2119)

- Test helpers that promise an optional collection **SHOULD** wrap conditional output in `@(...)` when callers need an empty array. Why: Seven cleanup workflow fixtures failed with `PropertyNotFoundException` on `.Count` because conditional assignment returned null under the full runner's strict mode.

## Scope and Audience

Agents maintaining PowerShell test helpers under `eng/tests/`.

## References

- [Cleanup workflow fixtures](../../eng/tests/agent-scripts/CleanupWorkflow.Tests.ps1)
- [PowerShell guidance](powershell.instructions.md)
- [Self-improvement governance](self-improvement.instructions.md)
