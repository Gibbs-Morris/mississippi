---
applyTo: 'eng/tests/**/*.ps1,eng/src/agent-scripts/*issue-reference*.ps1'
---

# Self-Taught Lessons: PowerShell

Governing thought: Preserve collection shape and repository ownership when parsing optional results.

> Drift check: Check the fixture, parser, registered runner, and overlapping guidance before applying these lessons.

## Rules (RFC 2119)

- Test helpers that promise an optional collection **SHOULD** wrap conditional output in `@(...)` when callers need an empty array. Why: Seven cleanup workflow fixtures failed with `PropertyNotFoundException` on `.Count` because conditional assignment returned null under the full runner's strict mode.
- Issue-reference parsers **MUST** strip non-rendered code before extracting HTML links. Why: Regression cases showed code anchors hiding valid tracking or incorrectly satisfying traceability.
- Issue-reference parsers **MUST** determine repository ownership from the link destination rather than its label. Why: PR #1022's upstream labels caused false local issue lookups.

## Scope and Audience

Agents maintaining PowerShell test helpers under `eng/tests/` and issue-reference validators under `eng/src/agent-scripts/`.

## References

- [Cleanup workflow fixtures](../../eng/tests/agent-scripts/CleanupWorkflow.Tests.ps1)
- [Issue-reference validator](../../eng/src/agent-scripts/validate-pr-issue-reference.ps1)
- [PowerShell guidance](powershell.instructions.md)
- [Traceability](issue-tracking.instructions.md)
- [Self-improvement governance](self-improvement.instructions.md)
