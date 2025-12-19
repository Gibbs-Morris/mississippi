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
