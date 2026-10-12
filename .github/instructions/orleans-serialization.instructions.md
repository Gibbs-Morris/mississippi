---
applyTo: '**/*.cs'
---

# Orleans Serialization

Governing thought: Use explicit `[GenerateSerializer]`, `[Id]`, and `[Alias]` with progressive versioning to keep Orleans payloads compatible across deployments.

> Drift check: Review Orleans analyzer settings and package references (`Microsoft.Orleans.Sdk`, `Microsoft.Orleans.CodeGenerator.MSBuild`) before changes.

## Rules (RFC 2119)

- Covered contributors **MUST** read the complete policy files for [Orleans contracts](../../src/AGENTS.md#orleans) and apply their clauses within this instruction's original path, content, and audience scope. Why: Relocation and optional skill selection do not narrow these obligations.

## Scope and Audience

Developers creating or changing Orleans-serialized types.
