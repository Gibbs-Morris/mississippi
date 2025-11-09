---
applyTo: "**/*Grain*.cs,**/*[Ss]erializ*.cs"
---

# Orleans Serialization

## Scope
`[GenerateSerializer]`, `[Id]`, `[Alias]` attributes only. Grain patterns in separate file.

## Quick-Start
```csharp
[GenerateSerializer]
[Alias("Company.Product.Customer")]
public class Customer {
  [Id(0)] public string Name { get; set; }
  [Id(1)] public string Email { get; set; }
}
```

## Core Principles
Every serializable type MUST have `[GenerateSerializer]`. All members MUST have unique `[Id(n)]` starting at 0. SHOULD have `[Alias]` for versioning. Build-time generation via `Microsoft.Orleans.CodeGenerator.MSBuild`.

## Versioning
Add new IDs, don't change existing. Widen types safely (int→long). Use nullable for optional fields.

## Anti-Patterns
❌ Missing `[Id]`. ❌ Duplicate IDs. ❌ Missing `[GenerateSerializer]`. ❌ Changing existing member IDs.

## Enforcement
ORLEANS analyzers treat violations as errors. PR reviews verify attributes present, IDs unique.
