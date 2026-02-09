# Step by Step Changes

## File-by-File Plan

| File | Change Type | Purpose | Notes |
| --- | --- | --- | --- |
| src/Common.Abstractions/Builders/IMississippiClientBuilder.cs | Add | Define client builder contract | UNVERIFIED path/name |
| src/Common.Abstractions/Builders/IMississippiServerBuilder.cs | Add | Define server builder contract | UNVERIFIED path/name |
| src/Common.Abstractions/Builders/IMississippiSiloBuilder.cs | Add | Define silo builder contract | UNVERIFIED path/name |
| src/Sdk.Client/Builders/MississippiClientBuilder.cs | Add | Implement client builder | Use existing meta-package |
| src/Sdk.Client/MississippiClientBuilderExtensions.cs | Add | HostApplicationBuilder entry point | UNVERIFIED path/name |
| src/Sdk.Server/Builders/MississippiServerBuilder.cs | Add | Implement server builder | Use existing meta-package |
| src/Sdk.Server/MississippiServerBuilderExtensions.cs | Add | HostApplicationBuilder entry point | UNVERIFIED path/name |
| src/Sdk.Silo/Builders/MississippiSiloBuilder.cs | Add | Implement silo builder | Use existing meta-package |
| src/Sdk.Silo/MississippiSiloBuilderExtensions.cs | Add | ISiloBuilder entry point | UNVERIFIED path/name |
| src/Reservoir/Builders/ReservoirBuilder.cs | Add | Builder-first Reservoir registration | NEW |
| src/Reservoir/Builders/ReservoirFeatureBuilder.cs | Add | Feature-level registration builder | NEW |
| src/Reservoir/ReservoirRegistrations.cs | Update/Delete | Replace with builder-first API | Remove legacy IServiceCollection surface |
| src/Reservoir.Blazor/ReservoirDevToolsRegistrations.cs | Update/Delete | Add builder-first entry point | Remove legacy IServiceCollection surface |
| src/Reservoir.Blazor/BuiltIn/ReservoirBlazorBuiltInRegistrations.cs | Update/Delete | Add builder-first entry point | Remove legacy IServiceCollection surface |
| src/Aqueduct/Builders/AqueductServerBuilder.cs | Add | Builder-first Aqueduct server registration | NEW |
| src/Aqueduct/AqueductRegistrations.cs | Update/Delete | Replace with builder-first API | Remove legacy IServiceCollection surface |
| src/Aqueduct.Grains/AqueductGrainsRegistrations.cs | Update | Align ISiloBuilder usage with builder model | Keep existing Orleans support |
| src/Inlet.Client/InletClientRegistrations.cs | Update/Delete | Add builder-first entry point | Remove legacy IServiceCollection surface |
| src/Inlet.Client/InletBlazorRegistrations.cs | Update/Delete | Add builder-first entry point | Use InletBlazorSignalRBuilder |
| src/Inlet.Silo/InletSiloRegistrations.cs | Update/Delete | Add builder-first entry point | Preserve ScanProjectionAssemblies behavior |
| src/Inlet.Server/InletServerRegistrations.cs | Update/Delete | Add builder-first entry point | Preserve Aqueduct composition |
| src/Inlet.Server.Abstractions/InletInProcessRegistrations.cs | Update/Move | Remove concrete registrations from Abstractions | Target location UNVERIFIED |
| src/EventSourcing.Brooks/EventSourcingRegistrations.cs | Update/Delete | Add Mississippi builder entry points | Preserve ISiloBuilder/HostApplicationBuilder overloads |
| src/EventSourcing.Snapshots/SnapshotRegistrations.cs | Update/Delete | Add builder-first entry points | Preserve existing behavior |
| src/EventSourcing.Sagas/SagaRegistrations.cs | Update/Delete | Add builder-first entry points | Preserve existing behavior |
| src/EventSourcing.Sagas.Abstractions/SagaStepInfoRegistrations.cs | Update/Move | Remove concrete provider registration from Abstractions | Target location UNVERIFIED |
| src/EventSourcing.Brooks.Abstractions/Storage/BrookStorageProviderExtensions.cs | Update | Add builder-friendly wrappers | Mirror builder-first extension points |
| src/EventSourcing.Snapshots.Abstractions/SnapshotStorageProviderExtensions.cs | Update | Add builder-friendly wrappers | Mirror builder-first extension points |
| src/EventSourcing.Serialization.Abstractions/SerializationStorageProviderExtensions.cs | Update | Add builder-friendly wrappers | Mirror builder-first extension points |
| docs/Docusaurus/docs/client-state-management/reservoir.md | Update | Builder-first usage examples | Replace ServiceCollection snippets |
| docs/Docusaurus/docs/client-state-management/devtools.md | Update | Builder-first usage examples | Replace ServiceCollection snippets |
| docs/Docusaurus/docs/client-state-management/built-in-navigation.md | Update | Builder-first usage examples | Replace ServiceCollection snippets |
| docs/Docusaurus/docs/client-state-management/built-in-lifecycle.md | Update | Builder-first usage examples | Replace ServiceCollection snippets |
| docs/Docusaurus/docs/client-state-management/store.md | Update | Builder-first usage examples | Replace ServiceCollection snippets |
| docs/Docusaurus/docs/client-state-management/effects.md | Update | Builder-first usage examples | Replace ServiceCollection snippets |
| docs/Docusaurus/docs/event-sourcing-sagas.md | Update | Builder-first usage examples | Replace AddInletSilo reference if needed |
| docs/Docusaurus/docs/migrations/builder-first-registration.md | Add | Migration guidance and before/after samples | UNVERIFIED path/name |
| tests/Reservoir.L0Tests/ReservoirRegistrationsTests.cs | Update | Use builder-first API in tests | Validate fallback behavior if retained |
| tests/Reservoir.Blazor.L0Tests/ReservoirDevToolsRegistrationsTests.cs | Update | Use builder-first API in tests | Validate builder wiring |
| tests/Reservoir.Blazor.L0Tests/BuiltIn/ReservoirBlazorBuiltInRegistrationsTests.cs | Update | Use builder-first API in tests | Validate builder wiring |
| tests/Inlet.Client.L0Tests/InletClientRegistrationsTests.cs | Update | Use builder-first API in tests | Validate builder wiring |
| tests/Inlet.Client.L0Tests/Registrations/InletBlazorRegistrationsTests.cs | Update | Use builder-first API in tests | Validate builder wiring |
| tests/Inlet.Silo.L0Tests/InletSiloRegistrationsTests.cs | Update | Use builder-first API in tests | Validate builder wiring |
| tests/Inlet.Server.L0Tests/InletServerRegistrationsTests.cs | Update | Use builder-first API in tests | Validate builder wiring |
| tests/Aqueduct.L0Tests/AqueductRegistrationsTests.cs | Update | Use builder-first API in tests | Validate builder wiring |
| samples/Spring/Spring.Client/Program.cs | Update | Use builder-first API in sample | Keep behavior same |
| samples/Spring/Spring.Server/Program.cs | Update | Use builder-first API in sample | Keep behavior same |
| samples/Spring/Spring.Silo/Program.cs | Update | Use builder-first API in sample | Keep behavior same |
| samples/Crescent/Crescent.L2Tests/CrescentFixture.cs | Update | Use builder-first API in tests | Keep behavior same |
| samples/LightSpeed/LightSpeed.Client/Program.cs | Update | Use builder-first API in sample | Keep behavior same |
