---
applyTo: '**/*.ps*'
---

# PowerShell contracts

Governing thought: Preserve applicable contracts through explicit scopes and verified source authority.

> Drift check: Root guidance remains effective. This profile covers PowerShell content and matching paths anywhere; it is not restricted to eng/. Inspect eng/src/agent-scripts/RepositoryAutomation.psm1 and current test scripts before changing patterns.

## Rules (RFC 2119)

### PowerShell

Scope: All **/*.ps* paths and PowerShell scripts/modules/examples authored, reviewed, or explained.

- PS1: Scripts **MUST** start with `#!/usr/bin/env pwsh`, `Set-StrictMode -Version Latest`, `$ErrorActionPreference='Stop'`.
- PS1.2: Script fail-fast settings **MUST NOT** be relaxed.
- PS2: Scripts **MUST** use explicit exit 0/nonzero codes.
- PS2.2: Scripts **MUST NOT** rely on implicit success.
- PS3: Hidden global state **MUST NOT** be introduced.
- PS3.2: Helpers **MUST** propagate errors, never swallow them.
- PS4: Parameters/outputs **SHOULD** be typed/validated.
- PS4.2: RepositoryAutomation.psm1 helpers **SHOULD** replace duplicate logic.
- PS5: Cross-platform Join-Path/Resolve-Path/Test-Path **SHOULD** be used.
- PS5.2: Structured data **SHOULD** serve automation.
- PS6: Retained template: shebang → CmdletBinding+param → strict mode → shared-helper imports → try/catch+explicit exit; validate inputs, avoid implicit output, keep module scope clean. Validate changes with `pwsh ./eng/tests/orchestrate-powershell-tests.ps1`; this check is separate from go.ps1.

## References

- eng/src/agent-scripts/RepositoryAutomation.psm1; eng/tests/orchestrate-powershell-tests.ps1 (current canonical definitions)
- .github/instructions/testing.instructions.md; .github/instructions/build-rules.instructions.md
