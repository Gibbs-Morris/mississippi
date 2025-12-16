---
applyTo: '**/*.ps*'
---

# PowerShell Scripting Best Practices

Governing thought: Write deterministic, strict, cross-platform scripts with explicit contracts and outputs.

## Rules (RFC 2119)

- Scripts **MUST** start with `#!/usr/bin/env pwsh`, `Set-StrictMode -Version Latest`, and `$ErrorActionPreference = 'Stop'`; these settings **MUST NOT** be relaxed. Scripts **MUST** emit deterministic exit codes (`exit 0` success, non-zero on failure) and **MUST NOT** rely on implicit success.
- Hidden global state **MUST NOT** be introduced; helper functions **MUST** bubble errors instead of swallowing them. Module scope **MUST** stay clean.
- Scripts **MUST** declare typed `param` blocks (with `[CmdletBinding()]`), prefer built-in cmdlets for path/OS safety, and **SHOULD** import shared helpers from `eng/src/agent-scripts/RepositoryAutomation.psm1` instead of duplicating logic.
- Structured data **SHOULD** be returned for automation; use `Write-Host` for status lines only.
- Scripts **SHOULD** include or update tests in `eng/tests/agent-scripts/` and follow cross-platform patterns.

## Quick Start

- Scaffold scripts with strict mode + explicit params; wrap orchestration in `try/catch` that exits non-zero on failure.
- Use RepositoryAutomation functions for build/test/cleanup instead of bespoke shelling.
- Validate via `pwsh ./eng/tests/orchestrate-powershell-tests.ps1` when behavior changes.

## Review Checklist

- [ ] Shebang/strict mode/ErrorAction set; deterministic exits present.
- [ ] No hidden globals; helpers bubble errors; module scope clean.
- [ ] Params typed, shared helpers used, cross-platform cmdlets applied.
- [ ] Structured output provided when needed; tests updated as appropriate.
