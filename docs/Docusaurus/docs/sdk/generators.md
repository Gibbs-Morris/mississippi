# Source Generators

Mississippi SDK uses Roslyn source generators to automate boilerplate code creation for projections, aggregates, and commands.

## Design Rationale

### Why Source Generators?

Event-sourced systems require significant boilerplate:

- **DTOs** mirroring domain types for API responses
- **Mappers** converting projections to DTOs
- **Controllers** exposing projection endpoints
- **Registration code** wiring up DI

This code is repetitive and error-prone to maintain by hand. Source generators eliminate this burden by producing correct, consistent code at compile time.

### Marking Types for Generation

Decorate projections with `[GenerateProjectionEndpoints]`:

```csharp
[GenerateProjectionEndpoints]
public sealed class BankAccountBalanceProjection : IProjection
{
    public decimal Balance { get; set; }
    public DateTimeOffset LastTransactionDate { get; set; }
}
```

The generator produces:

- `BankAccountBalanceDto` - JSON-serializable response DTO
- `BankAccountBalanceProjectionMapper` - projection-to-DTO mapper
- `BankAccountBalanceProjectionMapperRegistrations` - DI wiring
- `BankAccountBalanceController` - API controller

### Shared Generator Core Library

Generator projects reference `Sdk.Generators.Core` as an analyzer dependency:

```xml
<ProjectReference Include="..\Sdk.Generators.Core\Sdk.Generators.Core.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="true"
                  PrivateAssets="all" />
```

The core library contains shared utilities for type analysis, source building, and naming conventions that all generators use. The `OutputItemType="Analyzer"` attribute is required because source generators run in the compiler host, which only loads assemblies explicitly marked as analyzers. Without this, the generator would throw `FileNotFoundException` at runtime when trying to load the Core DLL. The `PrivateAssets="all"` ensures the dependency doesn't flow transitively to consuming projects.

### ExcludeAssets=runtime for Client Projects

Blazor WASM clients cannot reference Domain projects that contain Orleans serialization attributes—the Orleans runtime would be bundled into the WASM output.

Solution: reference Domain with `ExcludeAssets="runtime"`:

```xml
<ProjectReference Include="..\Domain\Domain.csproj" ExcludeAssets="runtime" />
```

This allows the client generator to **see** Domain types at compile time while excluding them from the deployed WASM bundle. The generator produces standalone DTOs that work purely with System.Text.Json.

## Generator Attributes

| Attribute | Purpose |
|-----------|---------|
| `[GenerateProjectionEndpoints]` | Generate server endpoints + client DTOs for a projection |
| `[GenerateAggregateEndpoints]` | Generate command endpoints for an aggregate |
| `[GenerateCommand]` | Generate command DTO and validation |
| `[GeneratorIgnore]` | Exclude a property from generated DTOs |
| `[GeneratorPropertyName("name")]` | Override the property name in generated code |
| `[GeneratorRequired]` | Force a property to be required even if nullable |

## Future Enhancements

- Aggregate endpoint generation with command routing
- SignalR hub generation for real-time projections
- OpenAPI schema generation from attributed types
- Validation attribute propagation
