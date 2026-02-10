# Verification

## Claim List
1. Builder interfaces no longer expose `IServiceCollection Services`.
2. Builder implementations no longer expose public Services properties.
3. All internal registrations use `ConfigureServices` instead of `.Services` access.
4. Public extension methods remain functional without `.Services` access.
5. Docs and samples do not reference `.Services` for builder registration.

## Questions
- Q1: Which public builder interfaces expose `IServiceCollection Services` today?
- Q2: Which builder implementations expose public `Services` properties and where are they used internally?
- Q3: Which extension methods create child builders by accessing `.Services`?
- Q4: Where do registration helpers call `.Services` on IMississippi* builders or IAqueductServerBuilder?
- Q5: Do any tests depend on `.Services` for assertions or service provider construction?
- Q6: Do any generator outputs or samples reference `.Services` on Mississippi builders?
- Q7: Can all direct `.Services` usages be replaced by `ConfigureServices` without losing functionality?
- Q8: What documentation references `.Services` on builder types?
- Q9: Which namespaces/projects will need updates due to the public contract change?
- Q10: Are there any Orleans-specific builder uses that require special handling (ISiloBuilder.Services)?

## Answers
- A1: IMississippiBuilder, IReservoirBuilder, and IAqueductServerBuilder expose `IServiceCollection Services`. Evidence: src/Common.Abstractions/Builders/IMississippiBuilder.cs, src/Reservoir.Abstractions/Builders/IReservoirBuilder.cs, src/Aqueduct.Abstractions/Builders/IAqueductServerBuilder.cs.
- A2: MississippiClientBuilder, MississippiServerBuilder, MississippiSiloBuilder, and AqueductServerBuilder expose public Services properties. Evidence: src/Sdk.Client/Builders/MississippiClientBuilder.cs, src/Sdk.Server/Builders/MississippiServerBuilder.cs, src/Sdk.Silo/Builders/MississippiSiloBuilder.cs, src/Aqueduct/Builders/AqueductServerBuilder.cs.
- A3: ReservoirBuilderExtensions and AqueductBuilderExtensions create child builders via `builder.Services`. Evidence: src/Reservoir/ReservoirBuilderExtensions.cs, src/Aqueduct/AqueductBuilderExtensions.cs. Mississippi client/server builder extensions use host builder Services (HostApplicationBuilder/WebApplicationBuilder), not Mississippi builders. Evidence: src/Sdk.Client/MississippiClientBuilderExtensions.cs, src/Sdk.Server/MississippiServerBuilderExtensions.cs.
- A4: AqueductGrainsRegistrations uses `siloBuilder.Services` (ISiloBuilder). Evidence: src/Aqueduct.Grains/AqueductGrainsRegistrations.cs.
- A5: Tests use `builder.Services` for assertions and service provider creation on TestMississippiSiloBuilder. Evidence: tests/EventSourcing.Brooks.L0Tests/EventSourcingRegistrationsTests.cs, tests/EventSourcing.Snapshots.L0Tests/SnapshotRegistrationsTests.cs, tests/EventSourcing.UxProjections.L0Tests/UxProjectionRegistrationsTests.cs.
- A6: No generator outputs reference `.Services` on Mississippi builders; generators use `ConfigureServices`. Evidence: src/Inlet.Silo.Generators/SagaSiloRegistrationGenerator.cs, src/Inlet.Client.Generators/CommandClientRegistrationGenerator.cs.
- A7: Direct `.Services` usage in builder extensions can be replaced by passing the builder into the child builder and using `ConfigureServices` internally. No evidence of functionality that requires direct IServiceCollection exposure beyond registration.
- A8: Docs currently show `builder.Services` on host builders in samples/docs; no references to `.Services` on Mississippi builders identified in docs. Evidence: docs/Docusaurus/docs/client-state-management/* updates; samples/LightSpeed/LightSpeed.Client/Program.cs still uses builder.Services.AddReservoir (host builder).
- A9: Projects needing updates include Common.Abstractions, Reservoir.Abstractions, Aqueduct.Abstractions (interfaces), Sdk.Client/Sdk.Server/Sdk.Silo/Aqueduct/Reservoir (implementations/extensions), plus tests that use TestMississippiSiloBuilder. Evidence: files listed in A1/A2/A3/A5.
- A10: ISiloBuilder.Services is used in AqueductGrainsRegistrations; MississippiSiloBuilder currently proxies Services via ISiloBuilder.Services. Evidence: src/Aqueduct.Grains/AqueductGrainsRegistrations.cs, src/Sdk.Silo/Builders/MississippiSiloBuilder.cs.
