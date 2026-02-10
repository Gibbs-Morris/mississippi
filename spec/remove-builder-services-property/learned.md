# Learned

## Repository facts
- UNVERIFIED: Builder contracts expose `IServiceCollection Services { get; }` in IMississippiBuilder, IReservoirBuilder, and IAqueductServerBuilder.
- UNVERIFIED: Builder implementations expose public Services properties used in registrations.

## Requirements (from user)
- Remove the public `Services` property to enforce a ConfigureServices-only pattern.

## Verified facts
- IMississippiBuilder, IReservoirBuilder, and IAqueductServerBuilder expose `IServiceCollection Services`. Evidence: src/Common.Abstractions/Builders/IMississippiBuilder.cs, src/Reservoir.Abstractions/Builders/IReservoirBuilder.cs, src/Aqueduct.Abstractions/Builders/IAqueductServerBuilder.cs.
- MississippiClientBuilder, MississippiServerBuilder, MississippiSiloBuilder, and AqueductServerBuilder expose public Services properties. Evidence: src/Sdk.Client/Builders/MississippiClientBuilder.cs, src/Sdk.Server/Builders/MississippiServerBuilder.cs, src/Sdk.Silo/Builders/MississippiSiloBuilder.cs, src/Aqueduct/Builders/AqueductServerBuilder.cs.
- ReservoirBuilderExtensions and AqueductBuilderExtensions use `builder.Services` to create child builders. Evidence: src/Reservoir/ReservoirBuilderExtensions.cs, src/Aqueduct/AqueductBuilderExtensions.cs.
- Tests use TestMississippiSiloBuilder.Services to build providers and inspect registrations. Evidence: tests/EventSourcing.Brooks.L0Tests/EventSourcingRegistrationsTests.cs, tests/EventSourcing.Snapshots.L0Tests/SnapshotRegistrationsTests.cs, tests/EventSourcing.UxProjections.L0Tests/UxProjectionRegistrationsTests.cs.

## Files to verify
- src/Common.Abstractions/Builders/IMississippiBuilder.cs
- src/Reservoir.Abstractions/Builders/IReservoirBuilder.cs
- src/Aqueduct.Abstractions/Builders/IAqueductServerBuilder.cs
- src/Sdk.Client/Builders/MississippiClientBuilder.cs
- src/Sdk.Server/Builders/MississippiServerBuilder.cs
- src/Reservoir/Builders/ReservoirBuilder.cs
- src/Aqueduct/Builders/AqueductServerBuilder.cs
