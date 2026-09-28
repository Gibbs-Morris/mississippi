---
applyTo: '**/*.ps*'
---

# Self-Taught Lessons: PowerShell Testing

Governing thought: Preserve fixture isolation and capability-aware evidence in PowerShell checks.

> Drift check: Before adding lessons, check overlapping instructions for conflicts and duplicates under [self-improvement](self-improvement.instructions.md).

## Rules (RFC 2119)

Evidence links identify historical fixtures, not a currently shipped inspection tool.

- Agents SHOULD use case-sensitive predicates when asserting exact path spellings. Why: Pester `-Contain` confused uppercase and lowercase names in the [PR #803 case-folding fixture](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1).
- Agents SHOULD control index mtime in Git stat-cache tests. Why: The [PR #803 fixtures](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) unexpectedly rehashed tracked content until cached mtime preceded the index.
- Agents SHOULD control cached ctime precision in Git stat-cache tests. Why: The PR #803 fixtures varied across whole-second ctime boundaries until cached ctime was controlled independently of index mtime.
- Agents SHOULD apply optional-suite capability gates to standalone launches. Why: The [PR #803 simulated-runtime controls](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) needed discovery-time gating so direct execution could report an explicit skip when prerequisites were absent.
- Agents SHOULD make harness meta-test expectations capability-aware for optional suites. Why: The [PR #803 harness controls](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/PowerShellTestHarness.Tests.ps1) needed to accept the optional suite's skipped status while verifying execution of the other eleven suites.
- Agents SHOULD isolate global Git attribute discovery in controlled permission fixtures. Why: The [PR #803 unreadable-directory control](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) first encountered inaccessible `/root/.config/git/attributes`; child-only XDG discovery restored the intended traversal failure.
- Agents SHOULD restore absent fixture environment variables by removing their environment-provider entries. Why: The [PR #803 selector controls](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) bound a null string argument as empty on the exercised runtimes, leaving a selector present and breaking the next control.

## Scope and Audience

Agents writing or reviewing PowerShell checks, test fixtures and test runners.

## References

- [PowerShell scripting](powershell.instructions.md)
- [Self-improvement governance](self-improvement.instructions.md)
