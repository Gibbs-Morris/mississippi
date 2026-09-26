---
applyTo: '**/*.ps*'
---

# Self-Taught Lessons: PowerShell

Governing thought: Preserve explicit repository identity when PowerShell launches Git.

> Drift check: Before adding lessons, check overlapping instructions for conflicts and duplicates under [self-improvement](self-improvement.instructions.md).

## Rules (RFC 2119)

- Agents SHOULD reject or isolate inherited Git repository-selection overrides when inspecting an explicitly selected target. Why: A `GIT_WORK_TREE` override redirected `git -C` to an outer repository during the delivery-skill review; the [fixture regressions](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) cover rejection.
- Agents SHOULD disable executable filesystem-monitor configuration for Git inspection advertised as read-only. Why: The [PR #803 hook fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) showed ordinary `git status` executing `core.fsmonitor`; the snapshot override prevents execution.

## Scope and Audience

Agents writing or reviewing PowerShell that inspects Git repositories.

## References

- [PowerShell scripting](powershell.instructions.md)
- [Self-improvement governance](self-improvement.instructions.md)
