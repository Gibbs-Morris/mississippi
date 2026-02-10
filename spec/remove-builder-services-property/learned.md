# Learned

## Repository facts
- UNVERIFIED: Builder contracts expose `IServiceCollection Services { get; }` in IMississippiBuilder, IReservoirBuilder, and IAqueductServerBuilder.
- UNVERIFIED: Builder implementations expose public Services properties used in registrations.

## Files to verify
- src/Common.Abstractions/Builders/IMississippiBuilder.cs
- src/Reservoir.Abstractions/Builders/IReservoirBuilder.cs
- src/Aqueduct.Abstractions/Builders/IAqueductServerBuilder.cs
- src/Sdk.Client/Builders/MississippiClientBuilder.cs
- src/Sdk.Server/Builders/MississippiServerBuilder.cs
- src/Reservoir/Builders/ReservoirBuilder.cs
- src/Aqueduct/Builders/AqueductServerBuilder.cs
