---
applyTo: '**/*.ps*'
---

# PowerShell Scripting

Governing thought: Scripts run with strict mode, explicit parameters, deterministic exit codes, and shared helpers—mirroring our C# quality bar.

> Drift check: Review `eng/src/agent-scripts/RepositoryAutomation.psm1` and test scripts before changing patterns.

## Rules (RFC 2119)

- Covered contributors **MUST** read the complete policy in [PowerShell contracts](../../eng/AGENTS.md#powershell) and apply its clauses within this instruction's original path, content, and audience scope. Why: Relocation does not narrow these obligations.

## Scope and Audience

Authors/reviewers of PowerShell scripts/modules in this repo.

## References

- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
- Canonical PowerShell test runner: `eng/tests/orchestrate-powershell-tests.ps1`
