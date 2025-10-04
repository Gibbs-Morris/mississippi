---
applyTo: '**/*.cs'
---

# Orleans Serialization Best Practices

This document defines the Orleans serialization standards and best practices for the Mississippi Framework. All serializable types used in Orleans must follow these guidelines to ensure version tolerance, performance, and compatibility across deployments.

## Core Principles

### Explicit Serialization Strategy
- **Always use explicit serialization** with `[GenerateSerializer]` and `[Id]` attributes
- **Never rely on implicit serialization** - Orleans requires explicit marking for version tolerance
- **Use `[Alias]` attributes** for type and method stability across renames and refactoring
- **Start member IDs at zero** for each type in the inheritance hierarchy
- **Follow progressive versioning** - maintain backward compatibility when evolving types

### Build-Time Code Generation
- **Enable Orleans source generation** by referencing appropriate NuGet packages
- **Use MSBuild package** `Microsoft.Orleans.CodeGenerator.MSBuild` for build-time generation
- **Let Orleans auto-detect** serializable types in grain interfaces and state classes
- **Generate comprehensive serializers** for all types crossing grain boundaries

## Quick-Start Checklist

### Required Package References
```xml
<!-- Core Orleans packages with serialization support -->
<PackageReference Include="Microsoft.Orleans.Sdk" />
<PackageReference Include="Microsoft.Orleans.CodeGenerator.MSBuild" />
```

### Basic Attribute Usage
```csharp
[GenerateSerializer]
[Alias("MyCompany.MyProduct.MyType")]
public class CustomerData
{
    [Id(0)] public string Name { get; set; }
    [Id(1)] public string Email { get; set; }
    [Id(2)] public DateTime CreatedAt { get; set; }
}
```

## Orleans Serialization Attributes Reference

| Attribute | Purpose | Scope | Required | Notes |
|-----------|---------|-------|----------|-------|
| `[GenerateSerializer]` | Marks type for code generation | Class, Record, Struct | Yes | Must be on every serializable type |
| `[Id(n)]` | Assigns unique member identifier | Property, Field | Yes | Start from 0, unique per inheritance level |
| `[Alias("name")]` | Provides stable type/method name | Class, Interface, Method | Recommended | Enables renames without breaking compatibility |
| `[NonSerialized]` | Excludes member from serialization | Property, Field | Optional | Use for computed or transient properties |

## Worked Example: HeadStorageModel

```csharp
/// <summary>
/// Storage model for brook head position information.
/// Demonstrates proper Orleans serialization attribute usage.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Cosmos.Storage.HeadStorageModel")]
internal class HeadStorageModel
{
    /// <summary>
    /// Gets or sets the current position of the brook head.
    /// </summary>
    [Id(0)]
    public BrookPosition Position { get; set; }

    /// <summary>
    /// Gets or sets the original position of the brook head before any updates.
    /// Note: Nullable types are fully supported in Orleans serialization.
    /// </summary>
    [Id(1)]
    public BrookPosition? OriginalPosition { get; set; }
}
```

### Key Points from Example

1. **`[GenerateSerializer]`** tells Orleans to generate serialization code
2. **`[Alias]`** provides a stable name independent of namespace/class renames
3. **`[Id(0)]` and `[Id(1)]`** assign unique identifiers starting from zero
4. **Nullable types** are fully supported without special handling
5. **XML documentation** follows the project's naming conventions

## Versioning Guidelines

| Scenario | ✅ Safe Operations | ❌ Breaking Changes |
|----------|-------------------|-------------------|
| **Adding Members** | Add new properties with unused ID numbers | Change existing member IDs |
| **Removing Members** | Mark with `[NonSerialized]` or `[Obsolete]` | Delete properties immediately |
| **Type Changes** | Widen numeric types (`int` → `long`) | Change signedness (`int` → `uint`) |
| **Inheritance** | Add new subclasses | Insert new base classes |
| **Nullability** | Make non-nullable properties nullable | Make nullable properties non-nullable |
| **Records vs Classes** | Add members to existing type | Convert between `record` and `class` |

### Version Evolution Example

```csharp
// Version 1.0
[GenerateSerializer]
[Alias("MyApp.Customer")]
public class Customer
{
    [Id(0)] public string Name { get; set; }
    [Id(1)] public string Email { get; set; }
}

// Version 2.0 - Safe additions
[GenerateSerializer]
[Alias("MyApp.Customer")]
public class Customer
{
    [Id(0)] public string Name { get; set; }
    [Id(1)] public string Email { get; set; }
    [Id(2)] public DateTime? CreatedAt { get; set; }     // New nullable field
    [Id(3)] public string? PhoneNumber { get; set; }    // New optional field
}
```

## Special Cases and Advanced Patterns

### Record Types
```csharp
[GenerateSerializer]
[Alias("MyApp.OrderEvent")]
public record OrderCreated(
    [property: Id(0)] string OrderId,
    [property: Id(1)] decimal Amount,
    [property: Id(2)] DateTime Timestamp)
{
    // Body members don't clash with primary constructor IDs
    [Id(0)] public string? Notes { get; init; }
}
```

### Inheritance Hierarchies
```csharp
[GenerateSerializer]
[Alias("MyApp.Publication")]
public class Publication
{
    [Id(0)] public string Title { get; set; }
    [Id(1)] public DateTime PublishedDate { get; set; }
}

[GenerateSerializer]
[Alias("MyApp.Book")]
public class Book : Publication
{
    // IDs restart at 0 for each inheritance level
    [Id(0)] public string ISBN { get; set; }
    [Id(1)] public int PageCount { get; set; }
}
```

### Surrogate Patterns for External Types
```csharp
// For types you cannot modify
[GenerateSerializer]
[Alias("MyApp.UriSurrogate")]
public struct UriSurrogate
{
    [Id(0)] public string Value { get; set; }
    
    public static implicit operator Uri(UriSurrogate surrogate) => new(surrogate.Value);
    public static implicit operator UriSurrogate(Uri uri) => new() { Value = uri.ToString() };
}
```

## Common Build-Time Analyzers

| Analyzer ID | Severity | Trigger | Solution |
|-------------|----------|---------|----------|
| `ORLEANS0004` | Error | Missing `[Id]` attributes on serializable members | Add `[Id(n)]` to all properties/fields |
| `ORLEANS0005` | Info | `[Serializable]` without `[GenerateSerializer]` | Replace with `[GenerateSerializer]` |
| `ORLEANS0011` | Error | Duplicate `[Alias]` values | Ensure all aliases are globally unique |
| `ORLEANS0012` | Error | Duplicate `[Id]` values within type | Assign unique ID numbers per inheritance level |

### Fixing Common Analyzer Issues

```csharp
// ORLEANS0004: Add missing [Id] attributes
[GenerateSerializer]
public class BadExample
{
    public string Name { get; set; }        // ❌ Missing [Id]
    public string Email { get; set; }       // ❌ Missing [Id]
}

[GenerateSerializer]
public class GoodExample
{
    [Id(0)] public string Name { get; set; }    // ✅ Has [Id]
    [Id(1)] public string Email { get; set; }   // ✅ Has [Id]
}

// ORLEANS0011: Fix duplicate aliases
[GenerateSerializer]
[Alias("Customer")]        // ❌ Duplicate alias
public class CustomerV1 { }

[GenerateSerializer]
[Alias("Customer")]        // ❌ Same alias as above
public class CustomerV2 { }

// Fixed version
[GenerateSerializer]
[Alias("Customer.V1")]     // ✅ Unique alias
public class CustomerV1 { }

[GenerateSerializer]
[Alias("Customer.V2")]     // ✅ Unique alias
public class CustomerV2 { }
```

## Pull Request Safety Checklist

When reviewing Orleans serialization changes, verify:

- [ ] All serializable types have `[GenerateSerializer]` attribute
- [ ] All serializable members have unique `[Id(n)]` attributes within their inheritance level
- [ ] All types have `[Alias]` attributes with globally unique names
- [ ] New members use previously unused ID numbers
- [ ] No existing member IDs were changed
- [ ] No breaking type conversions (e.g., `record` ↔ `class`)
- [ ] Build succeeds without ORLEANS analyzer warnings
- [ ] Version compatibility maintained for rolling deployments
- [ ] XML documentation follows project naming conventions
- [ ] For any analyzer violations or missing attributes discovered, open focused `.scratchpad/tasks/pending` items per type to schedule remediation if not fixed immediately (see Agent Scratchpad)

## Enforcement

These serialization standards should be enforced through:

1. **Build Pipeline**: Orleans analyzers enabled with `--warnaserror` to fail builds on violations
2. **Code Reviews**: All serialization changes must be reviewed for version compatibility
3. **Static Analysis**: ORLEANS0004, ORLEANS0005, ORLEANS0011, ORLEANS0012 treated as build errors
4. **Testing**: Serialization round-trip tests for all new serializable types
5. **Documentation**: Keep these guidelines updated with new patterns and examples

## Further Reading

- Orleans serialization overview: [https://learn.microsoft.com/dotnet/orleans/serialization](https://learn.microsoft.com/dotnet/orleans/serialization)
- Serialization attributes: [https://learn.microsoft.com/dotnet/orleans/serialization/attributes](https://learn.microsoft.com/dotnet/orleans/serialization/attributes)
- MSBuild source generator: [https://www.nuget.org/packages/Microsoft.Orleans.CodeGenerator.MSBuild](https://www.nuget.org/packages/Microsoft.Orleans.CodeGenerator.MSBuild)
- Proto3 versioning guidelines: [https://protobuf.dev/programming-guides/proto3/#updating](https://protobuf.dev/programming-guides/proto3/#updating)
- Orleans best practices: [https://learn.microsoft.com/dotnet/orleans/resources/best-practices](https://learn.microsoft.com/dotnet/orleans/resources/best-practices)

## Related Guidelines

This document should be read in conjunction with:

- **C# General Development Best Practices** (`.github/instructions/csharp.instructions.md`) - For SOLID principles, dependency injection patterns, and immutable object preferences
- **Service Registration and Configuration** (`.github/instructions/service-registration.instructions.md`) - For service registration patterns when registering Orleans services and serialization providers
- **Orleans Best Practices** (`.github/instructions/orleans.instructions.md`) - For POCO grain patterns and Orleans-specific development guidelines
- **Logging Rules** (`.github/instructions/logging-rules.instructions.md`) - For logging patterns when serialization operations need to be logged
- **Naming Conventions** (`.github/instructions/naming.instructions.md`) - For consistent naming of types, properties, and XML documentation
- **Build Rules** (`.github/instructions/build-rules.instructions.md`) - For quality standards and zero warnings policy enforcement
