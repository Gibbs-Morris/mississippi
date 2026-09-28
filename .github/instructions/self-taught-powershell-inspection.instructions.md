---
applyTo: '**/*.ps*'
---

# Self-Taught Lessons: PowerShell Inspection

Governing thought: Keep native inspection bounded and recoverable.

> Drift check: Check overlapping instructions for conflicts and duplicates under [self-improvement](self-improvement.instructions.md).

## Rules (RFC 2119)

Evidence links identify historical fixtures, not a currently shipped inspection tool.

- Agents SHOULD reject native inspection diagnostics even with a successful exit code. Why: The [PR #803 permission fixture](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) showed Git omitting unreadable content with exit zero.
- Agents SHOULD emit subprocess failure diagnostics without terminal formatting. Why: ANSI rendering wrapped metadata, timeout and permission messages in PR #803 CI; the [forced-rendering regression](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) verifies plain errors.
- Agents SHOULD bound the whole filesystem inspection, including probes, opening and reading. Why: The [PR #803 stall and FIFO fixtures](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) covered work outside individual Git/hash deadlines.
- Agents SHOULD retain process-group or job ownership for inspection children. Why: The [PR #803 descendant fixtures](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) required ownership that survives root exit to keep descendants within cleanup scope.
- Agents SHOULD confirm descendant termination before releasing inspection resources. Why: The PR #803 descendants retained pipes after root exit; an exit race required preserving verified PID/start-time identity after environment clearing until termination could be observed.
- Agents SHOULD cap captured subprocess bytes before parsing output. Why: The [PR #803 oversized-stream fixtures](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) exceeded the inspection budget before inventory processing.
- Agents SHOULD enforce native-child memory limits before inspecting untrusted compressed input. Why: The [PR #803 allocation fixture](https://github.com/Gibbs-Morris/mississippi/blob/4c3d002eb4581045ea4d0221579981b180e114d4/eng/tests/agent-scripts/DecomposeDelivery.Tests.ps1) used a tiny loose Git object declaring a 1-GiB tree; OS limits rejected its allocation before output limits could apply.

## Scope and Audience

Agents writing or reviewing PowerShell inspection runtimes that launch native children.

## References

- [PowerShell scripting](powershell.instructions.md)
- [Self-improvement governance](self-improvement.instructions.md)
