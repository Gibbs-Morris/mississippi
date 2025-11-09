---
applyTo: "**/*.cs"
---

# Naming & Documentation Standards

## Scope
C# identifiers and XML docs only. StyleCop SA13xx (naming) and SA16xx (docs) enforced.

## Quick-Start
```csharp
namespace Company.Product.Feature;
/// <summary>Processes invoices for billing operations.</summary>
public sealed class InvoiceProcessor {
  /// <summary>Gets a value indicating whether processing is active.</summary>
  public bool IsActive { get; private set; }
  
  private ILogger<InvoiceProcessor> Logger { get; }
  
  /// <param name="logger">Logger for diagnostics.</param>
  public InvoiceProcessor(ILogger<InvoiceProcessor> logger) => Logger = logger;
}
// ✅ GOOD: PascalCase type/properties, camelCase params, DI property, XML docs
// ❌ BAD: underscore prefixes, missing XML, wrong casing
```

## Core Principles
Namespaces: `Company.Product.Feature[.SubFeature]`, max 5 segments, PascalCase. Types: PascalCase noun, approved suffixes (`Exception`, `Attribute`, `Options`, `Provider`, `Handler`, `Controller`, `Dto`). Interfaces: `I` prefix. Enums: singular PascalCase. Methods: PascalCase verb. Properties: PascalCase noun. Booleans: `Is`/`Has`/`Can`/`Should`. DI properties: `private Type Name { get; }`. Fields/locals: camelCase, NO underscores. Constants: PascalCase. Generics: `T` + descriptor.

## XML Docs (REQUIRED)
Public symbols MUST have `<summary>` (1-3 sentences), `<param>` for all params, `<returns>` for non-void, `<typeparam>` for generics. Imperative voice, present tense. Domain vocabulary preferred.

## StyleCop Rules
SA1300-SA1314 (naming), SA1600-SA1619 (docs) treated as errors. No suppressions without approval.

## Anti-Patterns
❌ Underscores in identifiers. ❌ Generic names. ❌ Missing XML docs. ❌ Placeholder/TODO comments.

## Enforcement
Builds fail on violations. Code reviews verify: naming consistency, XML completeness, domain terminology used.
