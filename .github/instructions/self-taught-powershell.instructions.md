---
applyTo: '**/*.ps*'
---

# Self-Taught Lessons: PowerShell

Governing thought: Preserve explicit repository identity when PowerShell launches Git.

> Drift check: Before adding lessons, check overlapping instructions for conflicts and duplicates under [self-improvement](self-improvement.instructions.md).

## Rules (RFC 2119)

- Agents SHOULD reject or isolate inherited Git repository-selection overrides when inspecting an explicitly selected target. Why: A `GIT_WORK_TREE` override redirected `git -C` to an outer repository during the delivery-skill review; the [fixture regressions](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) cover rejection.
- Agents SHOULD disable filesystem-monitor hooks for Git inspection advertised as read-only. Why: The [PR #803 monitor fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) showed `git status` executing `core.fsmonitor`; the snapshot overrides that setting.
- Agents SHOULD reject executable content filters for Git inspection advertised as read-only. Why: The [PR #803 filter fixtures](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) showed `git status` executing clean/process drivers despite disabling the monitor; the snapshot rejects those configurations.
- Agents SHOULD select one resolved application before treating `Get-Command` output as an executable path. Why: Two `git.exe` resolutions became one invalid command string in the [snapshot mutation fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1); selecting the first matches normal command resolution.
- Agents SHOULD verify literal Unix filenames when using filesystem cmdlets. Why: `-LiteralPath` normalized backslashes in the [PR #803 fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1).
- Agents SHOULD avoid index-mtime races in Git stat-cache tests. Why: The [PR #803 fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) rehashed content until its cached mtime preceded the index.
- Agents SHOULD account for ctime precision in Git stat-cache tests. Why: The [PR #803 fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) varied across whole-second ctime boundaries until its cached ctime was controlled.
- Agents SHOULD reject native inspection diagnostics even with a successful exit code. Why: The [PR #803 permission fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) showed Git omitting unreadable content with exit zero.
- Agents SHOULD bound the whole filesystem inspection, including probes, opening and reading. Why: The [PR #803 stall and FIFO fixtures](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) covered work outside individual Git/hash deadlines.
- Agents SHOULD validate metadata and hash content through the same open handle. Why: The [PR #803 handle-race fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) replaced the pathname after validation.
- Agents SHOULD reject alternate Git object stores before attributing commits to a selected repository. Why: The [PR #803 shared-clone fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) had clean status while borrowing HEAD and ancestry.

## Scope and Audience

Agents writing or reviewing PowerShell that inspects Git repositories.

## References

- [PowerShell scripting](powershell.instructions.md)
- [Self-improvement governance](self-improvement.instructions.md)
