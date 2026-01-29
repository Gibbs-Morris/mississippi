# Learned Facts

## Repository Structure

### Spring Sample Projects

| Project | Purpose |
|---------|---------|
| `samples/Spring/Spring.AppHost` | Aspire orchestration host |
| `samples/Spring/Spring.Silo` | Orleans silo host (grains, event sourcing) |
| `samples/Spring/Spring.Server` | ASP.NET Core API + SignalR + Blazor WASM host |
| `samples/Spring/Spring.Client` | Blazor WASM client |
| `samples/Spring/Spring.Domain` | Domain aggregates and projections |

### Spring.Silo Program.cs Registration Groups (current state)

Located at: `samples/Spring/Spring.Silo/Program.cs`

Current inline registrations (154 lines):
1. **HttpClient** - `builder.Services.AddHttpClient()`
2. **Application Services** - `INotificationService`
3. **Domain Aggregates** - Generated `AddBankAccountAggregate()`, `AddTransactionInvestigationQueueAggregate()`
4. **Domain Projections** - Generated `AddBankAccountBalanceProjection()`, etc.
5. **OpenTelemetry** - Tracing, Metrics, Logging with many meters
6. **Aspire Azure Storage** - Keyed Table/Blob clients for Orleans clustering
7. **Aspire Cosmos** - Cosmos client with Gateway mode configuration
8. **Keyed Service Forwarding** - Re-keying Aspire clients to Mississippi service keys
9. **Inlet Silo** - `AddInletSilo()`, `ScanProjectionAssemblies()`
10. **Event Sourcing Infrastructure** - JSON serialization, snapshot caching, Brooks/Snapshots Cosmos config
11. **Orleans Silo** - `UseOrleans()` with Aqueduct and event sourcing configuration

### Spring.Server Program.cs Registration Groups (current state)

Located at: `samples/Spring/Spring.Server/Program.cs`

Current inline registrations (112 lines):
1. **OpenTelemetry** - Similar to Silo but fewer meters
2. **Aspire Azure Storage** - Keyed Table client for Orleans client clustering
3. **Orleans Client** - `UseOrleansClient()`
4. **Controllers** - `AddControllers()`
5. **OpenAPI** - Scalar documentation
6. **JSON Serialization** - `AddJsonSerialization()`
7. **Aggregate Support** - `AddAggregateSupport()`
8. **UX Projections** - `AddUxProjections()`
9. **SignalR + Aqueduct** - `AddSignalR()`, `AddAqueduct<InletHub>()`
10. **Inlet Server** - `AddInletServer()`, `ScanProjectionAssemblies()`
11. **Aggregate Mappers** - Generated `AddBankAccountAggregateMappers()`
12. **Projection Mappers** - Generated `Add*ProjectionMappers()`

### Existing Registration Patterns in Framework

Framework uses extension methods on `IServiceCollection` that return `IServiceCollection` for chaining:

- `src/Inlet.Server/InletServerRegistrations.cs` - `AddInletServer()`, `AddInletSignalRGrainObserver()`, `MapInletHub()`
- `src/Inlet.Silo/InletSiloRegistrations.cs` - `AddInletSilo()`, `ScanProjectionAssemblies()`
- `src/EventSourcing.Brooks.Cosmos/BrookStorageProviderRegistrations.cs` - `AddCosmosBrookStorageProvider()`
- `src/EventSourcing.Snapshots.Cosmos/SnapshotStorageProviderRegistrations.cs` - `AddCosmosSnapshotStorageProvider()`

### Source Generators

The aggregates and projections registrations are source-generated:
- `src/Inlet.Silo.Generators/AggregateSiloRegistrationGenerator.cs` - Generates `Add{Name}Aggregate()`
- `src/Inlet.Silo.Generators/ProjectionSiloRegistrationGenerator.cs` - Generates `Add{Name}Projection()`
- `src/Inlet.Server.Generators/CommandServerDtoGenerator.cs` - Generates `Add{Name}AggregateMappers()`
- `src/Inlet.Server.Generators/ProjectionEndpointsGenerator.cs` - Generates `Add{Name}ProjectionMappers()`

Generated code goes to namespace: `Spring.Silo.Registrations` (referenced but not a physical folder)

### Service Key Constants

`MississippiDefaults.ServiceKeys.BlobLocking` - Used for forwarding blob clients
Shared Cosmos key pattern: constant string for re-keying

## Naming Conventions

- Registration classes: `{Area}Registrations.cs` or `{Feature}FeatureRegistration.cs`
- Extension methods: `Add{Feature}()`, `Use{Feature}()`, `Map{Feature}()`
- All return `IServiceCollection` or appropriate builder for chaining
