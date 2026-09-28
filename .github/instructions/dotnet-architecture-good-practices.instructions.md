---
description: "DDD and .NET architecture guidelines"
applyTo: '**/*.cs,**/*.csproj,**/Program.cs,**/*.razor'
---

# DDD and Architecture Checklist

Governing thought: Start every domain change with explicit DDD/SOLID analysis, keep logic in the right layer, and verify tests/observability before shipping.

> Drift check: When citing scripts or configs (build/test/logging), open the referenced files first; they remain authoritative.

## Rules (RFC 2119)

- Covered contributors **MUST** read the complete policy files for [DDD analysis contracts](../../src/AGENTS.md#ddd-analysis) and apply their clauses within this instruction's original path, content, and audience scope. Why: Relocation and optional skill selection do not narrow these obligations.

## Scope and Audience

Engineers modifying domain/application/infrastructure/UI shells where DDD or SOLID choices matter.
