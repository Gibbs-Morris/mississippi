---
applyTo: '**/*.ps*'
---

# Self-Taught Lessons: PowerShell Testing

Governing thought: Preserve fixture isolation and capability-aware evidence in PowerShell checks.

> Drift check: Before adding lessons, check overlapping instructions for conflicts and duplicates under [self-improvement](self-improvement.instructions.md).

## Rules (RFC 2119)

- Agents SHOULD use case-sensitive predicates when asserting exact path spellings. Why: Pester `-Contain` confused uppercase and lowercase names in the [PR #803 case-folding fixture](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1).
- Agents SHOULD control index mtime and cached ctime precision in Git stat-cache tests. Why: The [PR #803 fixtures](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) rehashed until cached mtime preceded the index and crossed whole-second ctime boundaries until cached ctime was controlled.
- Agents SHOULD apply optional-suite capability gates to standalone launches and harness meta-tests. Why: The [PR #803 simulated-runtime controls](../../eng/tests/agent-scripts/PowerShellTestHarness.Tests.ps1) require explicit skipped results and preserve execution of the other eleven suites.
- Agents SHOULD isolate global Git attribute discovery in controlled permission fixtures. Why: The [PR #803 unreadable-directory control](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) first encountered inaccessible `/root/.config/git/attributes`; child-only XDG discovery restored the intended traversal failure.
- Agents SHOULD restore absent fixture environment variables by removing their environment-provider entries. Why: The [PR #803 selector controls](../../eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) bound a null string argument as empty on the exercised runtimes, leaving a selector present and breaking the next control.

## Scope and Audience

Agents writing or reviewing PowerShell checks, test fixtures and test runners.

## References

- [PowerShell scripting](powershell.instructions.md)
- [Self-improvement governance](self-improvement.instructions.md)
