---
applyTo: "**/*.{ps1,psm1}"
---

# PowerShell Standards

## Scope
Script structure, error handling, params. See global for script precedence and command index.

## Quick-Start
```powershell
#!/usr/bin/env pwsh
[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$TaskId)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
try { DoWork; exit 0 } catch { Write-Error $_; exit 1 }
```

## Core Principles
Shebang + `[CmdletBinding()]` + typed params. Strict mode + stop-on-error. Explicit exit codes (0=success). Cross-platform (`Join-Path`, `Test-Path`). Import shared helpers from `RepositoryAutomation.psm1`.

## Anti-Patterns
❌ Missing strict mode. ❌ Swallowing errors. ❌ Hard-coded paths. ❌ Implicit exit codes.

## Enforcement
Code reviews: strict mode present, error handling correct, exit codes explicit, cross-platform cmdlets used.
