---
applyTo: '**'
---

# Agents

Governing thought: Agents follow repository instructions and iterate from correctness to quality to performance.

> Drift check: Review `.github/copilot-instructions.md` and `.github/instructions/*.instructions.md` before relying on this summary.

## Rules (RFC 2119)

- Agents MUST read `.github/copilot-instructions.md` first, then all instruction files under `.github/instructions/`, before making changes. Why: Ensures all work follows the repository's authoritative policies and conventions.
- Agents MUST follow every rule and guideline in those documents when planning or writing code. Why: Keeps contributions consistent, reviewable, and compliant with quality gates.
- Agents MUST follow the "make it work, make it right, make it fast" loop: get tests passing first, then refactor for clarity and correctness, then optimize only where needed. Why: Surfaces issues early and avoids premature optimization.

## Scope and Audience

All agents working in this repository.

## At-a-Glance Quick-Start

- Read `.github/copilot-instructions.md`, then all `.github/instructions/*.instructions.md`.
- Prioritize correctness first, cleanup next, and performance improvements last.

## Procedures

Use PowerShell to review instruction files:

```powershell
Get-ChildItem -Path .github -Recurse -Filter "*.instructions.md" |
    Sort-Object FullName |
    ForEach-Object { Get-Content -Path $_.FullName -Raw }
```

## Core Principles

- Authoritative instructions come first.
- Iterative delivery reduces risk.
