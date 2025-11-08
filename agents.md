# Agents

Agents MUST read every GitHub instruction document before making changes in this repository.  
Why: Ensures all work follows the repository’s authoritative policies and conventions.

Required reading order:

1. .github/copilot-instructions.md
2. All instruction files under .github/instructions

You can review the instruction files with PowerShell:

```powershell
Get-ChildItem -Path .github -Recurse -Filter "*.instructions.md" |
    Sort-Object FullName |
    ForEach-Object { Get-Content -Path $_.FullName -Raw }
```

Agents MUST follow every rule and guideline in those documents when planning or writing code.  
Why: Keeps contributions consistent, reviewable, and compliant with quality gates.
