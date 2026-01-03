# ADR-0005: Package Dependency Strategy

**Status**: Accepted
**Date**: 2026-01-03

## Context

Ripples consists of 7 packages with different target frameworks and purposes. We need a clear strategy for:
1. Which packages depend on which
2. How consumers reference packages
3. Transitive dependency management

## Decision

We will use a **layered dependency model** where higher layers include lower layers transitively:

### Dependency Layers

```
Layer 4: Runtime (pick one)
├── Ripples.Server    ─┐
└── Ripples.Client    ─┼─► Layer 3
                       │
Layer 3: Blazor        │
└── Ripples.Blazor    ─┼─► Layer 2
                       │
Layer 2: Core          │
└── Ripples           ─┼─► Layer 1
                       │
Layer 1: Abstractions  │
└── Ripples.Abstractions

Layer 0: Build Tools (orthogonal)
├── Ripples.Generators
└── Ripples.Analyzers
```

### Consumer Package References

**Blazor Server App:**
```xml
<PackageReference Include="Mississippi.Ripples.Server" />
```
Transitively gets: `Ripples.Blazor` → `Ripples` → `Ripples.Abstractions`

**Blazor WASM App:**
```xml
<PackageReference Include="Mississippi.Ripples.Client" />
```
Transitively gets: `Ripples.Blazor` → `Ripples` → `Ripples.Abstractions`

**Domain Library (projections/aggregates):**
```xml
<PackageReference Include="Mississippi.Ripples.Abstractions" />
<PackageReference Include="Mississippi.Ripples.Generators" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
<PackageReference Include="Mississippi.Ripples.Analyzers" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```

### Build Tool Packaging

Generators and analyzers are packaged as analyzers in the runtime packages:

```xml
<!-- In Ripples.Server.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Ripples.Generators\Ripples.Generators.csproj" 
                    OutputItemType="Analyzer" 
                    ReferenceOutputAssembly="false" />
  <ProjectReference Include="..\Ripples.Analyzers\Ripples.Analyzers.csproj" 
                    OutputItemType="Analyzer" 
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

This means consumers of `Ripples.Server` or `Ripples.Client` automatically get generators and analyzers.

### Framework Dependencies

| Package | External Dependencies |
|---------|----------------------|
| Ripples.Abstractions | None |
| Ripples | Microsoft.Extensions.DependencyInjection.Abstractions |
| Ripples.Blazor | Microsoft.AspNetCore.Components |
| Ripples.Server | Orleans.Core.Abstractions, Microsoft.AspNetCore.SignalR |
| Ripples.Client | Microsoft.AspNetCore.SignalR.Client, System.Net.Http.Json |
| Ripples.Generators | Microsoft.CodeAnalysis.CSharp |
| Ripples.Analyzers | Microsoft.CodeAnalysis.CSharp |

### Mississippi Dependencies

| Package | Mississippi Dependencies |
|---------|-------------------------|
| Ripples.Abstractions | None |
| Ripples | None |
| Ripples.Blazor | None |
| Ripples.Server | UxProjections.Abstractions, UxProjections.SignalR |
| Ripples.Client | None (uses generated routes) |
| Ripples.Generators | UxProjections.Api (generates controllers inheriting base) |

## Consequences

### Positive

- Single package reference for most consumers
- Analyzers automatically included
- Clear layering prevents circular dependencies
- Domain libraries stay lightweight (only Abstractions)

### Negative

- Transitive dependencies may pull in unwanted packages
- Version alignment required across all packages
- Build tools must target netstandard2.0

### Neutral

- Central Package Management (CPM) handles versions
- All packages versioned together via GitVersion
