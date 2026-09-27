---
applyTo: '**/*.ps*'
---

# Self-Taught Lessons: PowerShell

Governing thought: Preserve explicit repository identity when PowerShell launches Git.

> Drift check: Before adding lessons, check overlapping instructions for conflicts and duplicates under [self-improvement](self-improvement.instructions.md).

## Rules (RFC 2119)

- Agents SHOULD reject or isolate inherited Git repository/configuration overrides when inspecting an explicitly selected target. Why: `GIT_WORK_TREE` redirected inspection and `GIT_CONFIG` concealed a local executable filter from the guard in the [fixture regressions](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1).
- Agents SHOULD disable filesystem-monitor hooks for Git inspection advertised as read-only. Why: The [PR #803 monitor fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) showed `git status` executing `core.fsmonitor`; the snapshot overrides that setting.
- Agents SHOULD reject executable content filters for Git inspection advertised as read-only. Why: The [PR #803 filter fixtures](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) showed `git status` executing clean/process drivers despite disabling the monitor; the snapshot rejects those configurations.
- Agents SHOULD verify effective executable transport settings before remote Git access. Why: The [PR #803 transport finding](https://github.com/Gibbs-Morris/mississippi/pull/803#discussion_r4113816395) and marker fixture showed a local SSH command executing before networking.
- Agents SHOULD verify hook, signing-program and custom merge-driver provenance before Git mutations. Why: Owned PR #803 marker controls executed default hooks, configured signing and attribute-selected merge programs before subsequent validation could run.
- Agents SHOULD select one resolved application before treating `Get-Command` output as an executable path. Why: Two `git.exe` resolutions became one invalid command string in the [snapshot mutation fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1); selecting the first matches normal command resolution.
- Agents SHOULD verify literal Unix filenames when using filesystem cmdlets. Why: `-LiteralPath` normalized backslashes in the [PR #803 fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1).
- Agents SHOULD account for Git filename folding in status and inventory queries. Why: The [PR #803 case-folding fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) hid lowercase guidance with `core.ignoreCase=true` on a case-sensitive filesystem.
- Agents SHOULD use case-sensitive predicates when asserting exact path spellings. Why: Pester `-Contain` confused uppercase and lowercase names in the [PR #803 case-folding fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1).
- Agents SHOULD avoid index-mtime races in Git stat-cache tests. Why: The [PR #803 fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) rehashed content until its cached mtime preceded the index.
- Agents SHOULD account for ctime precision in Git stat-cache tests. Why: The [PR #803 fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) varied across whole-second ctime boundaries until its cached ctime was controlled.
- Agents SHOULD reject native inspection diagnostics even with a successful exit code. Why: The [PR #803 permission fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) showed Git omitting unreadable content with exit zero.
- Agents SHOULD emit subprocess failure diagnostics without terminal formatting. Why: ANSI rendering wrapped metadata, timeout and permission messages in PR #803 CI; the [forced-rendering regression](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) verifies plain errors.
- Agents SHOULD bound the whole filesystem inspection, including probes, opening and reading. Why: The [PR #803 stall and FIFO fixtures](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) covered work outside individual Git/hash deadlines.
- Agents SHOULD retain process-group or job ownership and confirm descendant termination before releasing inspection resources. Why: The [PR #803 descendant fixtures](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) retained pipes after their root exited; root waiting alone could not establish cleanup.
- Agents SHOULD cap captured subprocess bytes before parsing output. Why: The [PR #803 oversized-stream fixtures](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) exceeded the inspection budget before inventory processing.
- Agents SHOULD validate metadata and hash content through the same open handle. Why: The [PR #803 handle-race fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) replaced the pathname after validation.
- Agents SHOULD reject alternate Git object stores before attributing commit ancestry. Why: The [PR #803 shared-clone fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) reported clean status with objects supplied by another repository.
- Agents SHOULD reject legacy Git grafts before attributing commit ancestry. Why: The [PR #803 graft fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) reported clean status with unrelated parent ancestry.
- Agents SHOULD reconcile shallow boundaries and effective replacement refs before attributing ordinary Git ancestry or content. Why: The [PR #803 provenance fixtures](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) concealed parent history or changed the ordinary view despite clean status.
- Agents SHOULD disable or verify Git commit graphs when inspecting staged state. Why: The [PR #803 forged-graph fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) concealed a staged payload behind a cached root tree.
- Agents SHOULD disable or verify Git's untracked cache when inspecting dirty state. Why: The [PR #803 forged-cache fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) concealed a new file while both observations agreed.
- Agents SHOULD verify cached index trees against staged entries before accepting commit readiness. Why: The [PR #803 cache-tree fixtures](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) reported benign staged files while a subsequent tree write consumed a forged root or child.
- Agents SHOULD verify actual tracked content independently of cached stat fields while preserving built-in Git normalization. Why: The [PR #803 forged-stat fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) concealed unselected content despite default status checks; CRLF controls also needed a renormalized baseline.
- Agents SHOULD inspect pending Git operation and lock markers before treating porcelain status as readiness evidence. Why: The [PR #803 operation fixtures](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) retained merge, bisect or index-lock state while reporting empty status.
- Agents SHOULD reject opaque Git inventory directories or discover their guidance independently. Why: The [PR #803 embedded-repository fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) hid ignored scoped instructions behind one directory entry.
- Agents SHOULD validate discovered instruction entry types and live link targets before returning readable paths. Why: The [PR #803 inventory fixtures](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) returned tracked or linked guidance targeting a FIFO or socket before the guard.

## Scope and Audience

Agents writing or reviewing PowerShell that inspects Git repositories.

## References

- [PowerShell scripting](powershell.instructions.md)
- [Self-improvement governance](self-improvement.instructions.md)
