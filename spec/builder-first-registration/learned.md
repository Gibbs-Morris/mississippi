# Learned

## Initial Orientation

- VERIFIED: Registration surfaces for Reservoir, Aqueduct, Inlet, and EventSourcing are IServiceCollection extension methods.
- VERIFIED: Aqueduct.Grains uses ISiloBuilder extensions for Orleans configuration.
- VERIFIED: Inlet.Blazor uses a builder type (InletBlazorSignalRBuilder) to configure SignalR features.
- VERIFIED: EventSourcingRegistrations offers ISiloBuilder and HostApplicationBuilder overloads.
- VERIFIED: Reservoir docs and DevTools docs show IServiceCollection registration patterns.

## Verification Targets

- src/Reservoir/ReservoirRegistrations.cs
- src/Reservoir.Blazor/BuiltIn/ReservoirBlazorBuiltInRegistrations.cs
- src/Reservoir.Blazor/ReservoirDevToolsRegistrations.cs
- src/Aqueduct/AqueductRegistrations.cs
- src/Aqueduct.Grains/AqueductGrainsRegistrations.cs
- src/Inlet.Client/InletClientRegistrations.cs
- src/Inlet.Client/InletBlazorRegistrations.cs
- src/Inlet.Client/InletBlazorSignalRBuilder.cs
- src/Inlet.Client/SignalRConnection/SignalRConnectionRegistrations.cs
- src/Inlet.Silo/InletSiloRegistrations.cs
- src/Inlet.Server/InletServerRegistrations.cs
- src/Inlet.Server.Abstractions/InletInProcessRegistrations.cs
- src/EventSourcing.Brooks/EventSourcingRegistrations.cs
- src/EventSourcing.Aggregates/AggregateRegistrations.cs
- src/EventSourcing.Reducers/ReducerRegistrations.cs
- src/EventSourcing.Sagas/SagaRegistrations.cs
- src/EventSourcing.Snapshots/SnapshotRegistrations.cs
- src/EventSourcing.Snapshots.Cosmos/SnapshotStorageProviderRegistrations.cs
- src/EventSourcing.Brooks.Cosmos/BrookStorageProviderRegistrations.cs
- src/EventSourcing.UxProjections/UxProjectionRegistrations.cs
- src/EventSourcing.Sagas.Abstractions/SagaStepInfoRegistrations.cs
- src/EventSourcing.Brooks.Abstractions/Storage/BrookStorageProviderExtensions.cs
- src/EventSourcing.Snapshots.Abstractions/SnapshotStorageProviderExtensions.cs
- src/EventSourcing.Serialization.Abstractions/SerializationStorageProviderExtensions.cs
- src/Common.Abstractions/Mapping/MappingRegistrations.cs
- docs/Docusaurus/docs/client-state-management/reservoir.md
- docs/Docusaurus/docs/client-state-management/devtools.md
- docs/Docusaurus/docs/client-state-management/built-in-navigation.md
- docs/Docusaurus/docs/client-state-management/built-in-lifecycle.md
- docs/Docusaurus/docs/client-state-management/store.md
- docs/Docusaurus/docs/client-state-management/effects.md
- docs/Docusaurus/docs/event-sourcing-sagas.md
- tests/Reservoir.L0Tests/ReservoirRegistrationsTests.cs
- tests/Reservoir.Blazor.L0Tests/ReservoirDevToolsRegistrationsTests.cs
- tests/Reservoir.Blazor.L0Tests/BuiltIn/ReservoirBlazorBuiltInRegistrationsTests.cs
- tests/Inlet.Client.L0Tests/InletClientRegistrationsTests.cs
- tests/Inlet.Client.L0Tests/Registrations/InletBlazorRegistrationsTests.cs
- tests/Inlet.Silo.L0Tests/InletSiloRegistrationsTests.cs
- tests/Inlet.Server.L0Tests/InletServerRegistrationsTests.cs
- tests/Aqueduct.L0Tests/AqueductRegistrationsTests.cs
- samples/Spring/Spring.Client/Program.cs
- samples/Spring/Spring.Server/Program.cs
- samples/Spring/Spring.Silo/Program.cs
- samples/Crescent/Crescent.L2Tests/CrescentFixture.cs
- samples/LightSpeed/LightSpeed.Client/Program.cs

## Verified Facts

- Reservoir registrations are IServiceCollection extensions: AddReservoir, AddReducer, AddFeatureState, AddMiddleware, AddActionEffect, AddRootReducer, AddRootActionEffect (src/Reservoir/ReservoirRegistrations.cs).
- Reservoir Blazor integrations are IServiceCollection extensions: AddReservoirBlazorBuiltIns and AddReservoirDevTools (src/Reservoir.Blazor/BuiltIn/ReservoirBlazorBuiltInRegistrations.cs, src/Reservoir.Blazor/ReservoirDevToolsRegistrations.cs).
- Aqueduct registrations are IServiceCollection extensions: AddAqueduct, AddAqueductNotifier, AddAqueductGrainFactory (src/Aqueduct/AqueductRegistrations.cs).
- Aqueduct.Grains registers via ISiloBuilder extensions UseAqueduct (src/Aqueduct.Grains/AqueductGrainsRegistrations.cs).
- Inlet Silo registration is IServiceCollection based (AddInletSilo, ScanProjectionAssemblies) (src/Inlet.Silo/InletSiloRegistrations.cs).
- Inlet client registration is IServiceCollection based (AddInletClient, AddProjectionPath) (src/Inlet.Client/InletClientRegistrations.cs).
- Inlet Blazor SignalR uses a builder type InletBlazorSignalRBuilder configured through AddInletBlazorSignalR (src/Inlet.Client/InletBlazorRegistrations.cs, src/Inlet.Client/InletBlazorSignalRBuilder.cs).
- Inlet Server registration is IServiceCollection based and composes Aqueduct and Inlet Silo (src/Inlet.Server/InletServerRegistrations.cs).
- Inlet in-process registrations live in the Abstractions project and register a concrete implementation (src/Inlet.Server.Abstractions/InletInProcessRegistrations.cs).
- EventSourcingRegistrations exposes ISiloBuilder and HostApplicationBuilder overloads plus IServiceCollection AddEventSourcingByService (src/EventSourcing.Brooks/EventSourcingRegistrations.cs).
- Event sourcing support registrations are IServiceCollection extensions for aggregates, reducers, sagas, snapshots, and UX projections (src/EventSourcing.Aggregates/AggregateRegistrations.cs, src/EventSourcing.Reducers/ReducerRegistrations.cs, src/EventSourcing.Sagas/SagaRegistrations.cs, src/EventSourcing.Snapshots/SnapshotRegistrations.cs, src/EventSourcing.UxProjections/UxProjectionRegistrations.cs).
- Saga step info registration lives in Abstractions and registers a concrete provider (src/EventSourcing.Sagas.Abstractions/SagaStepInfoRegistrations.cs).
- Storage provider extension points live in Abstractions and are IServiceCollection based (src/EventSourcing.Brooks.Abstractions/Storage/BrookStorageProviderExtensions.cs, src/EventSourcing.Snapshots.Abstractions/SnapshotStorageProviderExtensions.cs, src/EventSourcing.Serialization.Abstractions/SerializationStorageProviderExtensions.cs).
- Reservoir docs show IServiceCollection registration patterns (docs/Docusaurus/docs/client-state-management/reservoir.md, devtools.md, built-in-navigation.md, built-in-lifecycle.md, store.md, effects.md).
- Event-sourcing sagas docs reference AddInletSilo registration (docs/Docusaurus/docs/event-sourcing-sagas.md).
- Tests validate current registrations via ServiceCollection in Reservoir, DevTools, built-ins, Inlet client/blazor/silo/server, and Aqueduct suites (tests/Reservoir.L0Tests/ReservoirRegistrationsTests.cs, tests/Reservoir.Blazor.L0Tests/ReservoirDevToolsRegistrationsTests.cs, tests/Reservoir.Blazor.L0Tests/BuiltIn/ReservoirBlazorBuiltInRegistrationsTests.cs, tests/Inlet.Client.L0Tests/InletClientRegistrationsTests.cs, tests/Inlet.Client.L0Tests/Registrations/InletBlazorRegistrationsTests.cs, tests/Inlet.Silo.L0Tests/InletSiloRegistrationsTests.cs, tests/Inlet.Server.L0Tests/InletServerRegistrationsTests.cs, tests/Aqueduct.L0Tests/AqueductRegistrationsTests.cs).
- Samples use builder.Services and siloBuilder registrations for Reservoir/Aqueduct/Inlet/EventSourcing (samples/Spring/Spring.Client/Program.cs, samples/Spring/Spring.Server/Program.cs, samples/Spring/Spring.Silo/Program.cs, samples/Crescent/Crescent.L2Tests/CrescentFixture.cs, samples/LightSpeed/LightSpeed.Client/Program.cs).
