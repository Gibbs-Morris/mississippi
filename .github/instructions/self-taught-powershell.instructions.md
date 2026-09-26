---
applyTo: '**/*.ps*'
---

# Self-Taught Lessons: PowerShell

Governing thought: Preserve explicit repository identity when PowerShell launches Git.

> Drift check: Before adding lessons, check overlapping instructions for conflicts and duplicates under [self-improvement](self-improvement.instructions.md).

## Rules (RFC 2119)

- Agents SHOULD reject or isolate inherited Git repository-selection overrides when inspecting an explicitly selected target. Why: A `GIT_WORK_TREE` override redirected `git -C` to an outer repository during the delivery-skill review; the [fixture regressions](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) cover rejection.
- Agents SHOULD disable filesystem-monitor hooks and reject executable content filters for Git inspection advertised as read-only. Why: The [PR #803 fixtures](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) showed `git status` executing `core.fsmonitor` and clean/process drivers; the snapshot blocks these paths.
- Agents SHOULD select one resolved application before treating `Get-Command` output as an executable path. Why: Two `git.exe` resolutions became one invalid command string in the [snapshot mutation fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1); selecting the first matches normal command resolution.

## Scope and Audience

Agents writing or reviewing PowerShell that inspects Git repositories.

## References

- [PowerShell scripting](powershell.instructions.md)
- [Self-improvement governance](self-improvement.instructions.md)
