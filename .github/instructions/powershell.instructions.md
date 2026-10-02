---
applyTo: '**/*.ps*'
---

# PowerShell Scripting

Governing thought: Scripts run with strict mode, explicit parameters, deterministic exit codes, and shared helpers—mirroring our C# quality bar.

> Drift check: Review `eng/src/agent-scripts/RepositoryAutomation.psm1` and test scripts before changing patterns.

## Rules (RFC 2119)

- Scripts **MUST** start with `#!/usr/bin/env pwsh`, `Set-StrictMode -Version Latest`, and `$ErrorActionPreference='Stop'`; these settings **MUST NOT** be relaxed. Why: Fail fast across platforms.
- Scripts **MUST** use explicit exit codes (`exit 0` success, non-zero failure) and **MUST NOT** rely on implicit success. Why: Reliable automation/CI.
- Scripts **MUST NOT** introduce hidden global state; helper functions **MUST** bubble errors (no swallowing). Why: Predictable composition.
- Parameters/outputs **SHOULD** be typed and validated; shared helpers from `RepositoryAutomation.psm1` **SHOULD** be used instead of duplicating logic. Why: Consistency and reuse.
- Cross-platform cmdlets (`Join-Path`, `Resolve-Path`, `Test-Path`) **SHOULD** be used, and structured data **SHOULD** be returned when automation consumes results. Why: Portability and machine readability.

### Native process inspection

Evidence links below identify historical fixtures rather than a shipped inspection tool.

- Agents SHOULD reject native inspection diagnostics even with a successful exit code. Why: The [PR #803 permission fixture](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) showed Git omitting unreadable content with exit zero.
- Agents SHOULD emit subprocess failure diagnostics without terminal formatting. Why: ANSI rendering wrapped metadata, timeout and permission messages in PR #803 CI; the [forced-rendering regression](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) verifies plain errors.
- Agents SHOULD bound the whole filesystem inspection, including probes, opening and reading. Why: The [PR #803 stall and FIFO fixtures](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) covered work outside individual Git/hash deadlines.
- Agents SHOULD retain process-group or job ownership for inspection children. Why: The [PR #803 descendant fixtures](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) required ownership that survives root exit to keep descendants within cleanup scope.
- Agents SHOULD confirm descendant termination before releasing inspection resources. Why: The PR #803 descendants retained pipes after root exit; an exit race required preserving verified PID/start-time identity after environment clearing until termination could be observed.
- Agents SHOULD cap captured subprocess bytes before parsing output. Why: The [PR #803 oversized-stream fixtures](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) exceeded the inspection budget before inventory processing.
- Agents SHOULD enforce native-child memory limits before inspecting untrusted compressed input. Why: The [PR #803 allocation fixture](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) used a tiny loose Git object declaring a 1-GiB tree; OS limits rejected its allocation before output limits could apply.

### Fixture isolation

- Agents SHOULD use case-sensitive predicates when asserting exact path spellings. Why: Pester `-Contain` confused uppercase and lowercase names in the [PR #803 case-folding fixture](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1).
- Agents SHOULD control index mtime in Git stat-cache tests. Why: The [PR #803 fixtures](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) unexpectedly rehashed tracked content until cached mtime preceded the index.
- Agents SHOULD control cached ctime precision in Git stat-cache tests. Why: The PR #803 fixtures varied across whole-second ctime boundaries until cached ctime was controlled independently of index mtime.
- Agents SHOULD apply optional-suite capability gates to standalone launches. Why: The [PR #803 simulated-runtime controls](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) needed discovery-time gating so direct execution could report an explicit skip when prerequisites were absent.
- Agents SHOULD make harness meta-test expectations capability-aware for optional suites. Why: The [PR #803 harness controls](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/PowerShellTestHarness.Tests.ps1) needed to accept the optional suite's skipped status while verifying execution of the other eleven suites.
- Agents SHOULD isolate global Git attribute discovery in controlled permission fixtures. Why: The [PR #803 unreadable-directory control](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) first encountered inaccessible `/root/.config/git/attributes`; child-only XDG discovery restored the intended traversal failure.
- Agents SHOULD restore absent fixture environment variables by removing their environment-provider entries. Why: The [PR #803 selector controls](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) bound a null string argument as empty on the exercised runtimes, leaving a selector present and breaking the next control.

## Scope and Audience

Authors/reviewers of PowerShell scripts/modules in this repo.

## At-a-Glance Quick-Start

- Template: shebang → `[CmdletBinding()]` + `param(...)` → strict mode → import helpers → try/catch with explicit exit.
- Validate parameters; avoid implicit output; keep module scope clean.
- Run `pwsh ./eng/tests/orchestrate-powershell-tests.ps1` to validate changes.

## Core Principles

- Fail fast, be explicit, stay cross-platform.
- Reuse shared automation instead of ad hoc scripts.

## References

- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
