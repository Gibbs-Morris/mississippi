# ADR-0006: Baseline Analyzer Suppressions

## Status

Accepted

## Context

Mississippi enforces a strict zero-warnings policy with multiple analyzer packages:

- **StyleCop.Analyzers.Unstable** – Code style and documentation
- **Microsoft.CodeAnalysis.NetAnalyzers** – .NET best practices (CA rules)
- **SonarAnalyzer.CSharp** – Code quality and security
- **IDisposableAnalyzers** – Disposal correctness
- **Microsoft.VisualStudio.Threading.Analyzers** – Async/threading patterns

With `<AnalysisMode>All</AnalysisMode>` enabled, all rules are active by default. Some rules conflict with the project's architectural decisions, modern C# idioms, or Orleans/ASP.NET hosting patterns. Rather than scatter per-file suppressions, we establish a curated baseline in `Directory.Build.props`.

## Decision

The following analyzer rules are suppressed globally in `Directory.Build.props`. Each suppression has an explicit justification. **No other global suppressions are permitted without an ADR update.**

---

## Suppressed Rules

### SA1633 – File must have header

| Property | Value |
|----------|-------|
| Category | StyleCop.Documentation |
| Default | Warning |
| Suppression | Global |

**Rule requirement:** Every file must begin with a copyright/license header comment.

**Justification:** Mississippi uses repository-level licensing (`LICENSE` file at root). Per-file headers create maintenance burden, add noise to diffs, and risk becoming stale. The shared-policies instruction explicitly prohibits file-level copyright banners.

---

### SA1111 – Closing parenthesis must be on line of last parameter

| Property | Value |
|----------|-------|
| Category | StyleCop.Spacing |
| Default | Warning |
| Suppression | Global |

**Rule requirement:** When a method call spans multiple lines, the closing `)` must be on the same line as the last argument.

**Justification:** Mississippi's code style places closing parentheses on their own line for multi-line calls and declarations. This improves readability for method chains, LINQ expressions, and Orleans grain calls. The `.editorconfig` enforces this convention.

```csharp
// Mississippi style (SA1111 violation, but preferred)
await client.GetGrain<IOrderGrain>(orderId)
    .CreateAsync(
        request,
        cancellationToken
    );

// SA1111-compliant (harder to read for long argument lists)
await client.GetGrain<IOrderGrain>(orderId)
    .CreateAsync(
        request,
        cancellationToken);
```

---

### SA1200 – Using directives must be placed correctly

| Property | Value |
|----------|-------|
| Category | StyleCop.Ordering |
| Default | Warning |
| Suppression | Global |

**Rule requirement:** `using` directives must be inside the namespace declaration.

**Justification:** Mississippi places `using` directives outside (above) the namespace, following modern C# conventions and the default behavior of `dotnet new` templates since .NET 6. File-scoped namespaces (used throughout) make inside-namespace placement syntactically awkward.

```csharp
// Mississippi style (file-scoped namespace)
using System;
using Microsoft.Extensions.Logging;

namespace Mississippi.Ripples;

public class RippleStore { }
```

---

### SA1009 – Closing parenthesis must be followed by space

| Property | Value |
|----------|-------|
| Category | StyleCop.Spacing |
| Default | Warning |
| Suppression | Global |

**Rule requirement:** Closing parenthesis must be followed by a space in certain contexts.

**Justification:** Conflicts with SA1111 suppression. When closing parentheses appear on their own line (our style), spacing rules become irrelevant. Suppressing both rules together allows consistent multi-line formatting.

---

### SA1507 – Code must not contain multiple blank lines in a row

| Property | Value |
|----------|-------|
| Category | StyleCop.Layout |
| Default | Warning |
| Suppression | Global |

**Rule requirement:** No consecutive blank lines allowed anywhere in the file.

**Justification:** Readability preference. Double blank lines are permitted to visually separate major sections within files, making code easier to scan and navigate.

---

### SA1101 – Prefix local calls with this

| Property | Value |
|----------|-------|
| Category | StyleCop.Readability |
| Default | Warning |
| Suppression | Global |

**Rule requirement:** Instance members must be prefixed with `this.` when accessed.

**Justification:** Modern C# style omits `this.` except where necessary for disambiguation. The prefix adds visual noise without improving clarity. IDE tooling (colorization, navigation) distinguishes instance vs. local members effectively. This aligns with Microsoft's .NET runtime coding style.

```csharp
// Mississippi style
Logger.LogInformation("Processing {EntityId}", entityId);

// SA1101-compliant (unnecessarily verbose)
this.Logger.LogInformation("Processing {EntityId}", entityId);
```

---

### SA1202 – Elements should be ordered by access

| Property | Value |
|----------|-------|
| Category | StyleCop.Ordering |
| Default | Warning |
| Suppression | Global |

**Rule requirement:** Members must appear in order: `public`, `internal`, `protected internal`, `protected`, `private protected`, `private`.

**Justification:** Suppressed to align with ReSharper code cleanup output. The ReSharper formatting rules use a different member ordering strategy that conflicts with StyleCop defaults. Maintaining consistency with automated cleanup takes precedence.

> **Note:** This suppression is open to reconsideration if a unified ordering convention is established.

---

### SA1204 – Static elements should appear before instance elements

| Property | Value |
|----------|-------|
| Category | StyleCop.Ordering |
| Default | Warning |
| Suppression | Global |

**Rule requirement:** All static members must appear before all instance members within each access level.

**Justification:** Suppressed to align with ReSharper code cleanup output (same rationale as SA1202). The cleanup rules place members in an order that conflicts with this StyleCop rule.

> **Note:** This suppression is open to reconsideration if a unified ordering convention is established.

---

### SA1201 – Elements should appear in the correct order

| Property | Value |
|----------|-------|
| Category | StyleCop.Ordering |
| Default | Warning |
| Suppression | Global |

**Rule requirement:** Members must appear in a specific order: fields, constructors, finalizers, delegates, events, enums, interfaces, properties, indexers, methods, structs, classes.

**Justification:** Suppressed to align with ReSharper code cleanup output (same rationale as SA1202/SA1204). The DI property pattern (constructor → properties → methods) used throughout the codebase also conflicts with this rule.

```csharp
public sealed class OrderGrain : Grain, IOrderGrain
{
    public OrderGrain(
        IOrderRepository repository,
        ILogger<OrderGrain> logger)
    {
        Repository = repository;
        Logger = logger;
    }

    // Properties immediately follow constructor (SA1201 violation)
    private IOrderRepository Repository { get; }
    private ILogger<OrderGrain> Logger { get; }

    // Methods follow properties
    public async Task<Order> GetAsync() { }
}
```

> **Note:** This suppression is open to reconsideration if a unified ordering convention is established.

---

### CA1014 – Mark assemblies with CLSCompliant

| Property | Value |
|----------|-------|
| Category | .NET Design |
| Default | Warning |
| Suppression | Global |

**Rule requirement:** Assemblies should be marked with `[assembly: CLSCompliant(true)]`.

**Justification:** CLS compliance is a legacy interop concern for libraries consumed by VB.NET, F#, or other .NET languages. Mississippi is a pure C# project with no cross-language consumption requirement. Enforcing CLS compliance would restrict valid C# features (unsigned integers, pointer types in spans) without benefit.

---

### CA2007 – Consider calling ConfigureAwait on awaited task

| Property | Value |
|----------|-------|
| Category | .NET Reliability |
| Default | Warning |
| Suppression | Global |

**Rule requirement:** All `await` expressions should use `.ConfigureAwait(false)` or `.ConfigureAwait(true)`.

**Justification:** Mississippi runs exclusively on ASP.NET Core and Orleans, which do not have a `SynchronizationContext`. In these environments, `ConfigureAwait(false)` has no effect—there is no context to capture. Adding it everywhere creates noise without behavioral impact. Library code consumed by WPF/WinForms would need this, but Mississippi is an application framework.

---

### VSTHRD111 – Use .ConfigureAwait(bool)

| Property | Value |
|----------|-------|
| Category | VS Threading |
| Default | Warning |
| Suppression | Global |

**Rule requirement:** Same as CA2007, from the Visual Studio threading analyzer.

**Justification:** Duplicate of CA2007. Both are suppressed for the same reason: no `SynchronizationContext` in ASP.NET Core/Orleans hosting.

---

### CA1040 – Avoid empty interfaces

| Property | Value |
|----------|-------|
| Category | .NET Design |
| Default | Warning |
| Suppression | Global |

**Rule requirement:** Interfaces should declare at least one member.

**Justification:** Mississippi uses marker interfaces for Orleans grain polymorphism, generic type constraints, and Redux-like dispatch patterns. Key use cases include:

- **Orleans marker grains** – Grains using generic types often require marker interfaces for factory patterns and polymorphic activation
- **Action/Projection markers** – `IAction`, `IProjection`, and aggregate markers enable type-safe dispatch and DI registration

```csharp
// Marker interface for Orleans grain factory (CA1040 violation, but required)
public interface IOrderGrainMarker { }

// Enables generic grain factory
TGrain GetGrain<TGrain>(string key) where TGrain : IGrainWithStringKey;

// Marker interface for Redux-like actions
public interface IAction { }
```

---

### CA1812 – Avoid uninstantiated internal classes

| Property | Value |
|----------|-------|
| Category | .NET Performance |
| Default | Warning |
| Suppression | Global |

**Rule requirement:** Internal classes that are never instantiated should be removed.

**Justification:** Mississippi uses DI and Orleans activation extensively. Many internal classes (grains, handlers, hosted services) are instantiated by the runtime via reflection or DI container, not by explicit `new` calls. The analyzer cannot detect these activations and produces false positives.

---

### CA1303 – Do not pass literals as localized parameters

| Property | Value |
|----------|-------|
| Category | .NET Globalization |
| Default | Warning |
| Suppression | Global |

**Rule requirement:** String literals passed to exception messages or logging should come from resource files for localization.

**Justification:** Mississippi is an internal/English-only framework with no localization requirement. Log messages and exception strings use structured logging with semantic templates. Moving these to `.resx` files would add complexity without user-facing benefit.

---

## Consequences

### Positive

- **Clean diffs:** No per-file suppressions scattered across the codebase
- **Consistent style:** All projects inherit the same baseline
- **Reduced noise:** Developers focus on meaningful warnings, not style debates
- **Modern C# idioms:** Code follows contemporary patterns rather than legacy StyleCop defaults

### Negative

- **Onboarding:** New contributors must understand the rationale (this ADR)
- **Tooling mismatch:** Some IDE templates may generate code that violates our conventions
- **Cross-project reuse:** Extracted packages may need their own `.editorconfig` adjustments

### Neutral

- Individual files may still use `#pragma` or `[SuppressMessage]` for truly exceptional cases, following the build-rules instructions (approval required, minimal scope)

---

## References

- [Directory.Build.props](../../Directory.Build.props) – The source of truth for suppressions
- [.github/instructions/shared-policies.instructions.md](../../.github/instructions/shared-policies.instructions.md) – Zero-warnings policy
- [StyleCop Rules Documentation](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1633.md)
- [.NET Code Analysis Rules](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/)
