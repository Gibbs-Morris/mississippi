# Naming and Taxonomy

This document consolidates the project and attribute naming conventions for the
unified code generation plan.

## Attribute Naming Convention

**Pattern:** `Generate*` for source generation triggers, `Define*` for identity/metadata.

### Generator Triggers (source generation intent)

| Current Name | Proposed Name | Purpose |
|--------------|---------------|---------|
| `[AggregateService]` | `[GenerateAggregateService]` | Generates service + optional API |
| `[UxProjection]` | `[GenerateProjectionApi]` | Generates projection API endpoints |
| — (new) | `[GenerateClientDto]` | Generates Orleans-free client DTO |
| — (new) | `[GenerateClientAction]` | Generates Reservoir action/effect |

### Identity/Metadata Markers (explicit assignment intent)

| Current Name | Proposed Name | Purpose |
|--------------|---------------|---------|
| `[ProjectionPath]` | `[DefineProjectionPath]` | Assigns path for API + SignalR |
| `[BrookName]` | `[DefineBrookName]` | Assigns brook identity |
| `[EventStorageName]` | `[DefineEventTypeStorageName]` | Assigns persisted event type name |
| `[SnapshotStorageName]` | `[DefineSnapshotTypeStorageName]` | Assigns persisted snapshot type name |

### Why This Convention?

1. **Explicit intent** — `Generate*` clearly signals "this triggers code gen"
2. **Orleans alignment** — follows Orleans `[GenerateSerializer]` pattern
3. **Discoverability** — IntelliSense groups related attributes together
4. **Separation of concerns** — generation vs metadata are distinct categories

### Migration Approach

Since this is **pre-production**, we do a **clean rename** without backward
compatibility shims:

1. Rename attribute types in-place to `Generate*`/`Define*` names
2. Update generators to use new attribute names
3. Update all usages in samples and Mississippi source
4. Delete old attribute names (no `[Obsolete]` shims needed)

## Project Naming Taxonomy

**Pattern:** `Mississippi.<Area>[.<Feature>].<Runtime>`

### Runtime Suffixes

| Suffix | Allowed Dependencies | Forbidden |
|--------|---------------------|-----------|
| `.Contracts` | Boundary-safe only | Orleans, AspNetCore, Blazor |
| `.Orleans.Contracts` | Orleans SDK (serialization attrs only) | AspNetCore, Blazor |
| `.Orleans` | Orleans SDK/Server | AspNetCore, Blazor |
| `.AspNet` | Microsoft.AspNetCore.App | Orleans Server, Blazor |
| `.Blazor` | Shared UI components | Orleans, AspNetCore Server |
| `.Blazor.Wasm` | Blazor WASM client | Orleans, AspNetCore Server |
| `.Blazor.Server` | Blazor Server | Orleans Silo |
| `.Infrastructure.<Provider>` | Provider SDK (Cosmos, etc.) | Orleans, AspNetCore, Blazor |
| `.Generators` | Roslyn analyzers only | All runtime packages |

### Generated Project Names (for Cascade sample)

| Project | Purpose | Orleans-Free? |
|---------|---------|---------------|
| `Cascade.Contracts.Generated` | Client-visible DTOs | ✅ Yes |
| `Cascade.Client.Generated` | Reservoir actions/effects | ✅ Yes |
| `Cascade.Domain` | Orleans projections + aggregates | ❌ No (Orleans) |

### Composition Roots (Defaults Packages)

For production use, Mississippi will provide pre-wired defaults packages:

| Package | Contents |
|---------|----------|
| `Mississippi.<Area>.Defaults.OrleansSilo` | Orleans registration extensions |
| `Mississippi.<Area>.Defaults.AspNet` | ASP.NET endpoints/hubs |
| `Mississippi.<Area>.Defaults.BlazorServer` | Blazor Server notifiers |
| `Mississippi.<Area>.Defaults.Wasm` | WASM client effects/components |

## Boundary Rules

Strict separation between runtimes prevents dependency leakage.

### Layer 1: WASM Client (Blazor WebAssembly)

**Allowed:**

- `System.Text.Json`
- `Microsoft.AspNetCore.SignalR.Client`
- `Reservoir`
- `*.Contracts` projects

**Forbidden:**

- `Orleans.Core`
- `Orleans.Serialization`
- Any grain interfaces
- `[Id(n)]`, `[GenerateSerializer]` attributes

### Layer 2: ASP.NET Server

**Allowed:**

- `Microsoft.AspNetCore.*`
- `Orleans.Client` (for grain factory)
- `*.Contracts` and `*.Orleans.Contracts`

**Forbidden:**

- `Orleans.Server`
- Blazor WASM packages

### Layer 3: Orleans Silo

**Allowed:**

- `Orleans.Server`
- `Orleans.Serialization.*`
- Full grain implementations

**Forbidden:**

- Blazor packages
- ASP.NET UI packages

## Current Boundary Violations (to be fixed)

Known violations in the current codebase:

1. **Orleans in Abstractions** — `*.Abstractions` projects pull Orleans SDK
2. **WASM → Orleans** — `Inlet.Blazor.WebAssembly` transitively depends on Orleans
3. **Mixed runtime packages** — `Inlet.Orleans.SignalR` has both ASP.NET and Orleans

## How Code Generation Addresses Violations

The unified code generation plan solves boundary violations by:

1. **Generated Orleans-free DTOs** — `[GenerateClientDto]` produces `*.Contracts.Generated`
   with no Orleans attributes, eliminating WASM → Orleans dependency

2. **PrivateAssets="all" pattern** — Generator reads Orleans-attributed types from
   Domain but Orleans packages don't flow transitively to clients

3. **Separate output projects** — Generated code goes to boundary-appropriate projects,
   not mixed into Domain

4. **Compile-time validation** — Generators can emit diagnostics when boundary rules
   are violated

## Integration with Code Generation Phases

| Phase | Naming/Taxonomy Impact |
|-------|------------------------|
| Phase 0 | Add `Generate*`/`Define*` attributes with legacy shims |
| Phase 1 | Use `[GenerateAggregateService]` on aggregates |
| Phase 2 | Verify `[GenerateProjectionApi]` produces correct DTOs |
| Phase 3 | DI generator uses conventions from `*.Contracts` |
| Phase 4 | `[GenerateClientDto]` emits to `*.Contracts.Generated` |
| Phase 5 | `[GenerateClientAction]` emits to `*.Client.Generated` |

## References

- Unified codegen spec: `spec/unified-codegen-dx/`
- Implementation plan: [implementation-plan.md](implementation-plan.md)
- RFC: [rfc.md](rfc.md)
