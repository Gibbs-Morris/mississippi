# Learned

## Verified
- `SagaPhaseDto` exists in Spring server and client samples:
	- samples/Spring/Spring.Server/Controllers/Projections/SagaPhaseDto.cs
	- samples/Spring/Spring.Client/Features/MoneyTransferStatus/Dtos/SagaPhaseDto.cs
- `SagaPhaseDtoMapper` exists in Spring server sample:
	- samples/Spring/Spring.Server/Controllers/Projections/Mappers/SagaPhaseDtoMapper.cs
- Saga status reducers in Spring projection are hand-authored and map saga lifecycle events to projection state:
	- samples/Spring/Spring.Domain/Projections/MoneyTransferStatus/Reducers/Saga*StatusReducer.cs
- Aggregate/projection generator patterns for DTOs/mappers/registrations exist in Inlet generators:
	- Server: src/Inlet.Server.Generators/ProjectionEndpointsGenerator.cs (DTO, mapper, registrations, controller)
	- Client: src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs (DTO generation)
	- Silo: src/Inlet.Silo.Generators/ProjectionSiloRegistrationGenerator.cs (reducers + snapshot converter)
- Generator test conventions exist in L0 test projects:
	- tests/Inlet.Server.Generators.L0Tests/ProjectionEndpointsGeneratorTests.cs
	- tests/Inlet.Client.Generators.L0Tests/ProjectionClientDtoGeneratorTests.cs
	- tests/Inlet.Silo.Generators.L0Tests/ProjectionSiloRegistrationGeneratorTests.cs
- `GenerateSagaEndpointsAttribute` is currently non-generic and uses properties for input type, feature key, and route prefix:
	- src/Inlet.Generators.Abstractions/GenerateSagaEndpointsAttribute.cs
- Generic attributes are supported in C# 11+ and the repo compiles with LangVersion 14.0:
	- https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-version-history#c-version-11
	- Directory.Build.props
- Language/runtime settings: LangVersion 14.0, TargetFramework net10.0 globally; generator projects target netstandard2.0:
	- Directory.Build.props
	- src/Inlet.*.Generators/*.csproj

## UNVERIFIED
- Full list of saga reducers in samples beyond MoneyTransferStatus.
- Whether saga reducer generation should mirror projection silo registration patterns or a new pattern.
