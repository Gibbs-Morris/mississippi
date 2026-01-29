# Verification

## Claim List

1. **Generated code namespace**: Source generators emit to `Spring.Silo.Registrations` namespace, new classes must not conflict
2. **Service key constants**: `MississippiDefaults.ServiceKeys.BlobLocking` is the correct key for blob locking
3. **Cosmos shared key pattern**: Both Brooks and Snapshots can share a single keyed Cosmos client
4. **Orleans stream provider**: StreamProvider name must match between AppHost and silo configuration
5. **Inlet projection scanning**: `ScanProjectionAssemblies` must be called after `AddInletSilo`
6. **No behavior change**: All registrations must remain semantically identical after refactoring

## Verification Questions

### Q1: What namespace do the source generators emit to for Spring.Silo?

**Answer**: From searching `Spring.Silo.Registrations` usage in Program.cs line 27:
```csharp
using Spring.Silo.Registrations;
```
This is the namespace used by generated code. Our new classes should use a different namespace to avoid conflicts.

**Decision**: Use `Spring.Silo.Infrastructure` for new registration classes.

### Q2: What is the exact service key for blob locking?

**Answer**: From `MississippiDefaults.ServiceKeys.BlobLocking` found in Program.cs line 87-92:
```csharp
builder.Services.AddKeyedSingleton(
    MississippiDefaults.ServiceKeys.BlobLocking,
    (sp, _) => sp.GetRequiredKeyedService<BlobServiceClient>("blobs"));
```

**Verified**: Correct usage pattern.

### Q3: Can Brooks and Snapshots share a Cosmos client?

**Answer**: From Program.cs lines 94-99 and 110-122:
```csharp
const string sharedCosmosKey = "spring-cosmos";
builder.Services.AddKeyedSingleton(sharedCosmosKey, ...);
builder.Services.AddCosmosBrookStorageProvider(options => { options.CosmosClientServiceKey = sharedCosmosKey; ... });
builder.Services.AddCosmosSnapshotStorageProvider(options => { options.CosmosClientServiceKey = sharedCosmosKey; ... });
```

**Verified**: Yes, both use the same keyed service key.

### Q4: What StreamProvider name is used?

**Answer**: From AppHost line 34:
```csharp
.WithMemoryStreaming("StreamProvider");
```
And Spring.Silo Program.cs lines 128, 131:
```csharp
siloBuilder.UseAqueduct(options => options.StreamProviderName = "StreamProvider");
siloBuilder.AddEventSourcing(options => options.OrleansStreamProviderName = "StreamProvider");
```

**Verified**: "StreamProvider" is consistent.

### Q5: Must ScanProjectionAssemblies come after AddInletSilo?

**Answer**: From `src/Inlet.Silo/InletSiloRegistrations.cs` lines 54-64:
```csharp
/// <para>
///     Call this after <see cref="AddInletSilo" /> to populate the registry.
/// </para>
```

**Verified**: Yes, order matters. Keep them together in the same extension method.

### Q6: What meters are configured for Mississippi?

**Answer**: From Program.cs lines 55-61:
```csharp
.AddMeter("Mississippi.EventSourcing.Brooks")
.AddMeter("Mississippi.EventSourcing.Aggregates")
.AddMeter("Mississippi.EventSourcing.Snapshots")
.AddMeter("Mississippi.Storage.Cosmos")
.AddMeter("Mississippi.Storage.Snapshots")
.AddMeter("Mississippi.Storage.Locking")
.AddMeter("Microsoft.Orleans")
```

**Verified**: These must all be preserved.

## Changes After Verification

1. Use namespace `Spring.Silo.Infrastructure` instead of `Spring.Silo.Registrations` to avoid generated code conflicts
2. Use namespace `Spring.Server.Infrastructure` for server registrations
3. Keep `AddInletSilo()` and `ScanProjectionAssemblies()` together in the same method
