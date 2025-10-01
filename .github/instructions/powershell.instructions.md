---
applyTo: '**/*.ps1'
---

# PowerShell Script Engineering Standards

These rules govern every PowerShell script (.ps1) in the Mississippi Framework repos. They align with Microsoft guidance, enforce the same engineering discipline we expect in C#, and keep automation production-ready.

## Core Principles

- **Treat scripts like code** — apply SOLID-style thinking; every script/function has a single responsibility and is unit-testable.
- **Prefer functions over loose script blocks** — encapsulate logic in advanced functions and expose a single orchestrator (for example `Invoke-Main`).
- **Stay cross-platform** — target PowerShell 7+, avoid Windows-only cmdlets unless explicitly required, and gate platform-specific logic.
- **Immutability-first mindset** — favour read-only variables and avoid mutating global state; pass data explicitly between functions.
- **No hidden dependencies** — require external tools/modules via parameters or `#Requires`; never assume machine state.

## Script Skeleton Requirements

```powershell
#Requires -Version 7.2
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$InputPath,

    [Parameter()]
    [switch]$DryRun
)

Import-Module MyCompany.Tools -ErrorAction Stop

function Invoke-Main {
    [CmdletBinding()]
    param(
        [string]$InputPath,
        [bool]$DryRun
    )

    if ($PSCmdlet.ShouldProcess($InputPath, 'Summarise data')) {
        $items = Get-ProcessedItems -Path $InputPath
        Publish-Results -Items $items -DryRun:$DryRun
    }
}

Invoke-Main @PSBoundParameters
```

## Script Layout & Naming

- **Files** — adopt `Verb-Noun.ps1` using approved PowerShell verbs (for example `Get-`, `Invoke-`, `Test-`).
- **Functions** — PascalCase with approved verbs; private helpers prefixed consistently (for example `InvokeInternalFoo`).
- **Top matter** — always include `#Requires -Version` and any module requirements.
- **Ordering** — constants, module imports, public functions, private helpers, then execution tail (`Invoke-Main @PSBoundParameters`).
- **Comment blocks** — use PowerShell comment-based help for public entry points; keep comments purposeful.

## Parameters & Binding

- **Advanced functions only** — decorate entry points with `[CmdletBinding()]` to enable `SupportsShouldProcess`, `Verbose`, and `ErrorAction` semantics.
- **Strong typing** — use .NET types or `[ValidateScript()]` to enforce inputs; avoid untyped parameters.
- **Mandatory clarity** — mark required parameters as `[Parameter(Mandatory)]`; provide defaults only when deterministic.
- **Validation** — prefer `ValidateSet`, `ValidateRange`, `ValidatePattern`, or `ArgumentCompletions` for discoverability.
- **Dependency injection mindset** — pass services, paths, or credentials rather than reading from global variables or environment state.

## Output & Pipeline Discipline

- **Return objects, not strings** — emit structured `PSCustomObject` instances or .NET types; leave formatting to the caller.
- **Avoid `Write-Host`** — use `Write-Verbose`, `Write-Information`, `Write-Debug`, or shared logging helpers consistent with repo logging rules.
- **Pure functions** — default to side-effect-free functions; isolate I/O in thin wrappers.
- **Support piping** — accept pipeline input via `[Parameter(ValueFromPipeline = $true)]` where it improves usability; implement `begin`, `process`, and `end` blocks when streaming.

## Error Handling & Reliability

- **Fail fast** — set `$ErrorActionPreference = 'Stop'` and rely on terminating errors.
- **Structured try/catch** — catch only when you can add context or recover; rethrow preserving the original exception.
- **No silent failures** — never swallow errors; use `throw` or `Write-Error -ErrorAction Stop`.
- **Input validation** — verify file paths, URLs, and environment values before use; surface actionable error messages.
- **Idempotency** — make reruns safe; support `-WhatIf` and `-Confirm` when mutating external state.

## Logging & Telemetry

- **Leverage existing logging** — when interoperating with .NET services, route messages through established LoggerExtensions or shared logging modules.
- **Verbose by default** — instrument major steps with `Write-Verbose`; expose the built-in `-Verbose` switch for diagnostics.
- **Correlation IDs** — accept or generate correlation tokens when interacting with services; include them in logs and external calls.
- **Audit critical operations** — log data mutations, external service calls, and long-running tasks in alignment with enterprise logging requirements.

## Security & Compliance

- **Credential hygiene** — accept credentials via `PSCredential` parameters or managed identity; never hard-code secrets.
- **Secure defaults** — prefer TLS-secured endpoints, signed scripts, and least-privilege permissions for invoked tooling.
- **Code signing ready** — structure scripts to support signing; avoid dynamic code generation.
- **Review downloads** — disallow unsanctioned `Invoke-WebRequest`/`Invoke-Expression`; package dependencies instead.

## Testing & Tooling

- **Pester coverage** — provide Pester tests for public functions; treat them like C# unit tests.
- **Static analysis** — run `Invoke-ScriptAnalyzer` with the team ruleset; remediate all warnings.
- **CI integration** — ensure scripts run non-interactively and return meaningful exit codes.
- **Mock external dependencies** — mirror C# testing discipline; isolate side effects using Pester mocks.

## Source Control & Distribution

- **Module-first** — when scripts grow beyond a single responsibility, convert them to modules (`.psm1`) with a manifest.
- **Doc updates** — include usage documentation alongside scripts; keep README snippets synchronized.
- **Versioning** — embed semantic version metadata in comment-based help or manifests when scripts are distributed.

## When Interfacing with C# Services

- **Consistent contracts** — honour the same DTOs and naming rules defined in C# guidelines; prefer JSON serialization via `System.Text.Json`.
- **Reuse abstractions** — if a C# service exposes DI-friendly abstractions (queues, storage), call them via CLI/HTTP wrappers that respect those contracts.
- **Shared validation** — align validation logic with matching C# services to avoid divergent business rules.

Adhering to these standards keeps PowerShell automation as maintainable, observable, and testable as our core C# services.
