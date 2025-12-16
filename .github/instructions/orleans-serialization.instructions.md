---
applyTo: '**/*.cs'
---

# Orleans Serialization Best Practices

Governing thought: Use explicit Orleans serialization attributes with stable IDs/aliases and version-tolerant changes.

## Rules (RFC 2119)

- Serializable types **MUST** use `[GenerateSerializer]` and `[Alias]`; serializable members **MUST** have unique `[Id(n)]` starting at 0 per inheritance level. Implicit serialization **MUST NOT** be used.
- Aliases **MUST** be globally unique; member IDs **MUST NOT** change; new members **MUST** use unused IDs. Projects **MUST** reference `Microsoft.Orleans.Sdk` and `Microsoft.Orleans.CodeGenerator.MSBuild`.
- Breaking conversions (e.g., `record` ↔ `class`, renumbering IDs) **MUST NOT** occur; removed members **SHOULD** be marked `[NonSerialized]`/`[Obsolete]`. Numeric changes **SHOULD** widen; adding optionality **SHOULD** use nullable types.
- Builds **MUST** treat Orleans analyzer warnings as errors; reviewers **SHOULD** verify version compatibility and attribute completeness. Missing attributes/violations **SHOULD** be tracked in `.scratchpad/tasks` if not fixed immediately.

## Quick Start

- Annotate types with `[GenerateSerializer]` and `[Alias]`; number members from zero per inheritance level with `[Id(n)]`.
- Keep existing IDs stable; add new members with new IDs and safe nullable/widened types.
- Run builds with analyzers and add serialization tests where appropriate.

## Review Checklist

- [ ] `[GenerateSerializer]`, `[Alias]`, and unique `[Id]` attributes present and stable; IDs start at 0 per inheritance level.
- [ ] No breaking type conversions; new members use unused IDs; removed members handled safely.
- [ ] Required Orleans packages present; analyzer warnings clean; deferred fixes tracked.
