---
title: Builder Pattern Migration Matrix
description: Proposed migration guidance from legacy registration APIs to builder-first APIs.
---

# Builder Pattern Migration Matrix (Proposed)

> Non-live design note: this page documents the proposed builder-first startup model and deprecation mapping. It is not yet the stable public guidance for released versions.

## Why this exists

The framework is moving from direct `Add*`/`Use*` registration helpers toward host-surface builders:

- `ClientBuilder.Create()`
- `GatewayBuilder.Create()`
- `RuntimeBuilder.Create()`

Legacy registration classes remain available for migration periods, but are marked with `[Obsolete("Use {BuilderType}.{Method}() instead. This API will be removed in a future major version.")]`.

## Startup migration (before/after)

### Runtime host

Before:

```csharp
services
    .AddEventSourcing(...)
    .AddSnapshots(...)
    .AddOrderAggregate()
    .AddPaymentSaga();
```

After:

```csharp
RuntimeBuilder runtime = RuntimeBuilder.Create()
    .AddAggregate<OrderState>()
    .AddSaga<PaymentSagaState>()
    .ConfigureSnapshotRetention(options => { /* ... */ });

hostBuilder.UseOrleans(silo => runtime.ApplyToSilo(silo));
hostBuilder.UseMississippi(runtime);
```

### Gateway host

Before:

```csharp
services
    .AddAqueduct(...)
    .AddInletServer(...);
```

After:

```csharp
GatewayBuilder gateway = GatewayBuilder.Create()
    .ConfigureAuthorization()
    .AllowAnonymousExplicitly(); // optional development-only path

hostBuilder.UseMississippi(gateway);
```

### Client host

Before:

```csharp
services
    .AddInletClient()
    .AddInletBlazor()
    .AddReservoir();
```

After:

```csharp
ClientBuilder client = ClientBuilder.Create();
hostBuilder.UseMississippi(client);
```

## Legacy-to-builder mapping

| Legacy registration surface | Builder replacement |
| --- | --- |
| `AqueductGrainsRegistrations` | `RuntimeBuilder.Create()` |
| `AqueductRegistrations` | `GatewayBuilder.Create()` |
| `BrooksRuntimeRegistrations` | `RuntimeBuilder.Create()` |
| `BrookStorageProviderRegistrations` | `RuntimeBuilder.Create()` |
| `MappingRegistrations` | `RuntimeBuilder.Create()` |
| `AggregateRegistrations` | `RuntimeBuilder.Create()` + typed `AddAggregate<TSnapshot>()` |
| `SagaRegistrations` | `RuntimeBuilder.Create()` + typed `AddSaga<TSagaState>()` |
| `UxProjectionRegistrations` | `RuntimeBuilder.Create()` + typed `AddUxProjection<TProjectionState>()` |
| `ReducerRegistrations` | `RuntimeBuilder.Create()` + feature composition |
| `SnapshotRegistrations` | `RuntimeBuilder.Create()` + feature composition |
| `SnapshotStorageProviderRegistrations` | `RuntimeBuilder.Create()` |
| `InletSiloRegistrations` | `RuntimeBuilder.Create()` |
| `InletServerRegistrations` | `GatewayBuilder.Create()` |
| `InletInProcessRegistrations` | `GatewayBuilder.Create()` |
| `InletClientRegistrations` | `ClientBuilder.Create()` |
| `InletBlazorRegistrations` | `ClientBuilder.Create()` |
| `SignalRConnectionRegistrations` | `ClientBuilder.Create()` |
| `ReservoirRegistrations` | `ClientBuilder.Create()` |
| `ReservoirDevToolsRegistrations` | `ClientBuilder.Create()` |
| `ReservoirBlazorBuiltInRegistrations` | `ClientBuilder.Create()` |

## Generated registrations

Generated registration classes are also deprecated when corresponding builder contracts are available in compilation:

- Domain-level generated classes:
  - `DomainSiloRegistrations`
  - `DomainServerRegistrations`
  - `DomainFeatureRegistrations`
- Per-feature generated classes:
  - `{Aggregate}AggregateRegistrations`
  - `{Saga}SagaRegistrations`
  - `{Projection}ProjectionRegistrations`
  - `ProjectionsFeatureRegistration`

This keeps generated and hand-authored migration guidance aligned.
