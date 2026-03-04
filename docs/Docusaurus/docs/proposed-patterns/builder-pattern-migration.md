---
id: builder-pattern-migration
title: Builder Pattern Migration Matrix
sidebar_label: Builder Pattern Migration
description: Migration guidance from legacy registration APIs to a proposed builder-first host composition pattern.
---

# Builder Pattern Migration Matrix

## Overview

Focus: **Public API / Developer Experience**.

This page documents a **proposed builder pattern based on a validated prototype baseline**.

It exists to capture the migration target and guardrails while the framework incrementally moves from legacy registration APIs to builder-first host composition.

## Proposed pattern summary

The framework is moving from direct `Add*`/`Use*` registration helpers toward host-surface builders:

- `ClientBuilder.Create()`
- `GatewayBuilder.Create()`
- `RuntimeBuilder.Create()`

The terminal attach point is `UseMississippi(...)`, with overloads for:

- `HostApplicationBuilder`
- `WebApplicationBuilder`
- `WebAssemblyHostBuilder` (client only)

Builder instances delegate registrations immediately into their own `IServiceCollection` and then merge into host services at `UseMississippi(...)`.

## Root contracts and terminal behavior

All root builders implement `IMississippiBuilder`:

- expose `IServiceCollection Services`
- expose `Validate()` diagnostics

Terminal attach behavior in `UseMississippi(...)`:

- validate builder state
- throw `BuilderValidationException` if diagnostics are present
- merge `builder.Services` into host services
- mark attachment and reject duplicate attach on the same host (`InvalidOperationException`)
- provide the only terminal step for builder attachment (there is no builder `Build()` method)

## Guiding rules

- `UseMississippi(...)` **MUST** be treated as the terminal attach step for each builder instance.
- A host **MUST NOT** call `UseMississippi(...)` more than once; duplicate attach throws `InvalidOperationException`.
- All builders **MUST** pass `Validate()` before attach; attach throws `BuilderValidationException` when diagnostics are present.
- Gateway composition **MUST** explicitly choose one security mode before attach: `ConfigureAuthorization()` or `AllowAnonymousExplicitly()`.
- Runtime composition **MUST** configure required runtime features before attach: `runtime.AddBrooks(...)` and `runtime.AddDomainModeling()`.
- Runtime Orleans wiring **SHOULD** use `runtime.ApplyToSilo(siloBuilder)` inside `UseOrleans(...)`.
- Subsystem packages **SHOULD** expose builder-first extension methods of the form `AddXxx(this I*Builder ...)` that delegate to `builder.Services`.

Ordering note: `UseOrleans(...)` and `UseMississippi(runtime)` both contribute configuration before final host build. Dependencies resolve when the host container is built, so registration order between those two calls does not by itself cause runtime DI breakage.

## Runtime typed sub-builders

`IRuntimeBuilder` introduces typed sub-builder entry points:

- `AddAggregate<TSnapshot>() -> IAggregateBuilder<TSnapshot>`
- `AddSaga<TSagaState>() -> ISagaBuilder<TSagaState>`
- `AddUxProjection<TProjectionState>() -> IUxProjectionBuilder<TProjectionState>`

Current behavior by sub-builder:

- `IAggregateBuilder<TSnapshot>` is behavior-bearing and currently supports `AddSnapshotStateConverter<TConverter>()`.
- `ISagaBuilder<TSagaState>` is currently a typed marker surface.
- `IUxProjectionBuilder<TProjectionState>` is currently a typed marker surface.

Runtime also supports silo-level composition through:

- `AddSiloConfiguration(Action<ISiloBuilder>)`
- `ApplyToSilo(ISiloBuilder)` (replays registered silo actions)

## Reservoir nested builders (inside client composition)

This pattern extends builder semantics into Reservoir via nested builders:

- `IReservoirBuilder : IMississippiBuilder`
- `IFeatureStateBuilder<TState> : IMississippiBuilder`

Composition flow:

1. `client.AddReservoir(reservoir => { ... })`
2. `reservoir.AddFeature<TState>(feature => { ... })`
3. `feature.AddReducer(...)` / `feature.AddActionEffect(...)`

Validation semantics:

- feature builder validates that at least one reducer was added for each feature
- reservoir builder validates that at least one feature was added when configured
- failures throw `BuilderValidationException` with structured diagnostics

Important note: reducer registration is not idempotent and should be configured once per feature path.

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
    .AddBrooks(brooks => brooks.UseJsonSerialization())
    .AddDomainModeling()
    .AddTributary();

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
    .AddInlet();

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
ClientBuilder client = ClientBuilder.Create()
    .AddReservoir()
    .AddInlet();

hostBuilder.UseMississippi(client);
```

## Legacy-to-builder mapping

| Legacy registration surface | Builder replacement |
| --- | --- |
| `AqueductRegistrations` | `GatewayBuilder.Create().AddAqueduct<THub>(...)` |
| `AqueductGrainsRegistrations` | `RuntimeBuilder.Create().AddAqueduct(...)` |
| `BrooksRuntimeRegistrations` | `RuntimeBuilder.Create().AddBrooks(...)` |
| `BrookStorageProviderRegistrations` | `RuntimeBuilder.Create().AddBrooks(b => b.UseCosmosStorage(...))` |
| `MappingRegistrations` | `RuntimeBuilder.Create().AddDomainModeling()` |
| `AggregateRegistrations` | `RuntimeBuilder.Create().AddAggregate<TSnapshot>()` |
| `SagaRegistrations` | `RuntimeBuilder.Create().AddSaga<TSagaState>()` |
| `UxProjectionRegistrations` | `RuntimeBuilder.Create().AddUxProjection<TProjectionState>()` |
| `SnapshotStorageProviderRegistrations` | `RuntimeBuilder.Create().AddTributary(t => t.UseCosmosSnapshotStorage(...))` |
| `InletSiloRegistrations` | `RuntimeBuilder.Create().AddInlet(...)` |
| `InletServerRegistrations` | `GatewayBuilder.Create().AddInlet(...)` |
| `InletClientRegistrations` | `ClientBuilder.Create().AddInlet()` |
| `InletBlazorRegistrations` | `ClientBuilder.Create().AddInletSignalR(...)` |
| `SignalRConnectionRegistrations` | `ClientBuilder.Create().AddInletSignalR(...)` |
| `ReservoirRegistrations` | `ClientBuilder.Create().AddReservoir(...)` |
| `ReservoirDevToolsRegistrations` | `ClientBuilder.Create().AddDevTools(...)` |
| `ReservoirBlazorBuiltInRegistrations` | `ClientBuilder.Create().AddReservoirBuiltIns()` |

## Generated registrations

Generated registration classes now emit builder overloads alongside existing `IServiceCollection` overloads.

- Domain-level generated classes add builder overloads for `IRuntimeBuilder`, `IGatewayBuilder`, and `IClientBuilder`.
- Per-feature generated classes continue to generate service registrations and compose through domain-level helpers.

Generator output currently does **not** emit `[Obsolete]` attributes for legacy generated methods; migration is guided by availability of builder overloads.

## Subsystem extension pattern

Subsystem packages follow a consistent shape:

- runtime extensions (for example `AddBrooks`, `AddDomainModeling`, `AddTributary`, `AddInlet`, `AddAqueduct`) extend `IRuntimeBuilder`
- gateway extensions (for example `AddInlet`, `AddAqueduct`) extend `IGatewayBuilder`
- client extensions (for example `AddInlet`, `AddInletSignalR`, `AddReservoir`, `AddDevTools`, `AddReservoirBuiltIns`) extend `IClientBuilder`

Each extension delegates registrations into `builder.Services`, and some extensions additionally set validation markers used during terminal attach.

## Current constraints

- This document reflects the current prototype baseline, not finalized framework policy.
- `ISagaBuilder<TSagaState>` and `IUxProjectionBuilder<TProjectionState>` are currently marker-first surfaces.
- Generator migration currently relies on builder overload availability rather than generated `[Obsolete]` attributes.

## Key diagnostics used by validation

- `Gateway.AuthorizationNotConfigured`
- `Runtime.BrooksNotConfigured`
- `Runtime.DomainModelingNotConfigured`
- `Reservoir.NoFeaturesConfigured`
- `Reservoir.Feature.NoReducersConfigured`

## See also

- [Proposed Patterns](./index)
