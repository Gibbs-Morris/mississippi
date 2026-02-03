# Learned

## Verified
- `SagaPhaseDto` and `SagaPhaseDtoMapper` are now generated via projection generators; manual sample files were retired:
	- samples/Spring/Spring.Server/Controllers/Projections/SagaPhaseDto.cs
	- samples/Spring/Spring.Server/Controllers/Projections/Mappers/SagaPhaseDtoMapper.cs
	- samples/Spring/Spring.Client/Features/MoneyTransferStatus/Dtos/SagaPhaseDto.cs
- Saga status reducers are now generated for projections marked with `GenerateSagaStatusReducersAttribute`:
	- src/Inlet.Silo.Generators/SagaStatusReducersGenerator.cs
	- src/Inlet.Generators.Abstractions/GenerateSagaStatusReducersAttribute.cs
- Aggregate/projection generator patterns for DTOs/mappers/registrations exist in Inlet generators:
	- Server: src/Inlet.Server.Generators/ProjectionEndpointsGenerator.cs (DTO, mapper, registrations, controller)
	- Client: src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs (DTO generation)
	- Silo: src/Inlet.Silo.Generators/ProjectionSiloRegistrationGenerator.cs (reducers + snapshot converter)
- Generator test conventions exist in L0 test projects:
	- tests/Inlet.Server.Generators.L0Tests/ProjectionEndpointsGeneratorTests.cs
	- tests/Inlet.Client.Generators.L0Tests/ProjectionClientDtoGeneratorTests.cs
	- tests/Inlet.Silo.Generators.L0Tests/ProjectionSiloRegistrationGeneratorTests.cs
- Saga status reducer generator tests live in:
	- tests/Inlet.Silo.Generators.L0Tests/SagaStatusReducersGeneratorTests.cs
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
