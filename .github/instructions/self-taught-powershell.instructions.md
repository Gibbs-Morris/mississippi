---
applyTo: '**/*.ps*'
---

# Self-Taught Lessons: PowerShell

Governing thought: Preserve explicit repository identity when PowerShell launches Git.

> Drift check: Before adding lessons, check overlapping instructions for conflicts and duplicates under [self-improvement](self-improvement.instructions.md).

## Rules (RFC 2119)

Evidence links identify historical fixtures, not a currently shipped inspection tool.

- Agents SHOULD reject or isolate inherited Git inspection selectors when inspecting an explicitly selected target. Why: The [fixture regressions](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) show worktree/configuration/history redirection, literal pathspecs hiding ignored guidance and an older attribute source concealing tracked differences.
- Agents SHOULD disable filesystem-monitor hooks for Git inspection advertised as read-only. Why: The [PR #803 monitor fixture](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) showed `git status` executing `core.fsmonitor`; the snapshot overrides that setting.
- Agents SHOULD reject executable content filters for Git inspection advertised as read-only. Why: The [PR #803 filter fixtures](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) showed `git status` executing clean/process drivers despite disabling the monitor; the snapshot rejects those configurations.
- Agents SHOULD verify effective executable transport settings before remote Git access. Why: The [PR #803 transport finding](https://github.com/Gibbs-Morris/mississippi/pull/803#discussion_r4113816395) and marker fixture showed a local SSH command executing before networking.
- Agents SHOULD verify hook provenance before Git mutations. Why: The PR #803 owned marker control executed a default hook before subsequent validation could run.
- Agents SHOULD verify signing-program provenance before signed Git mutations. Why: The PR #803 owned marker control executed the configured signing program during a commit.
- Agents SHOULD verify custom merge-driver provenance before Git integration. Why: The PR #803 owned marker control executed an attribute-selected merge program during integration.
- Agents SHOULD select one resolved application before treating `Get-Command` output as an executable path. Why: Two `git.exe` resolutions became one invalid command string in the [snapshot mutation fixture](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1); selecting the first matches normal command resolution.
- Agents SHOULD verify literal Unix filenames when using filesystem cmdlets. Why: `-LiteralPath` normalized backslashes in the [PR #803 fixture](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1).
- Agents SHOULD account for Git filename folding in status and inventory queries. Why: The [PR #803 case-folding fixture](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) hid lowercase guidance with `core.ignoreCase=true` on a case-sensitive filesystem.
- Agents SHOULD validate metadata and hash content through the same open handle. Why: The [PR #803 handle-race fixture](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) replaced the pathname after validation.
- Agents SHOULD reject alternate Git object stores before attributing commit ancestry. Why: The [PR #803 shared-clone fixture](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) reported clean status with objects supplied by another repository.
- Agents SHOULD reject legacy Git grafts before attributing commit ancestry. Why: The [PR #803 graft fixture](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) reported clean status with unrelated parent ancestry.
- Agents SHOULD reconcile shallow boundaries before attributing Git ancestry. Why: The [PR #803 shallow fixture](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) concealed parent history despite clean status.
- Agents SHOULD reconcile effective replacement refs before attributing Git ancestry or content. Why: The [PR #803 replacement fixtures](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) changed the ordinary view despite clean status.
- Agents SHOULD disable or verify Git commit graphs when inspecting staged state. Why: The [PR #803 forged-graph fixture](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) concealed a staged payload behind a cached root tree.
- Agents SHOULD disable or verify Git's untracked cache when inspecting dirty state. Why: The [PR #803 forged-cache fixture](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) concealed a new file while both observations agreed.
- Agents SHOULD verify cached index trees against staged entries before accepting commit readiness. Why: The [PR #803 cache-tree fixtures](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) reported benign staged files while a subsequent tree write consumed a forged root or child.
- Agents SHOULD verify actual tracked content beyond Git's cached-stat and normalized-object identities. Why: The [PR #803 content fixtures](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) concealed regular edits, symlink text, and an executable `ident` expansion despite clean status; tracked links and `ident` attributes require manual inspection.
- Agents SHOULD inspect pending Git operations before treating porcelain status as readiness evidence. Why: The [PR #803 operation fixtures](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) retained merge or bisect state despite empty status; recovery must reconcile the active operation.
- Agents SHOULD inspect Git lock markers before treating porcelain status as mutation-readiness evidence. Why: The PR #803 index, HEAD and ref-lock fixtures blocked later mutations despite empty status; lock ownership needs reconciliation independently of operation state.
- Agents SHOULD reject opaque Git inventory directories or discover their guidance independently. Why: The [PR #803 embedded-repository fixture](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) hid ignored scoped instructions behind one directory entry.
- Agents SHOULD validate a discovered instruction entry's filesystem type before returning it as a readable path. Why: The [PR #803 inventory fixtures](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) exposed FIFO or socket entries that cannot be treated as ordinary instruction files.
- Agents SHOULD validate a discovered instruction link's live target before returning a readable path. Why: The PR #803 linked-guidance fixtures resolved to a FIFO or socket despite discovery of the link entry itself.
- Agents SHOULD reconcile private Git exclusion sources before accepting a clean inventory. Why: The [PR #803 exclude fixtures](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) concealed an untracked build input through `info/exclude` or `core.excludesFile` while status remained clean.

## Scope and Audience

Agents writing or reviewing PowerShell that inspects Git repositories.

## References

- [PowerShell scripting](powershell.instructions.md)
- [Self-improvement governance](self-improvement.instructions.md)
