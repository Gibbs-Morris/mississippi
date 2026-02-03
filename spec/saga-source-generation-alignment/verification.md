# Verification

## Claim list
1. Saga DTOs and mappers are generated to match aggregate/projection patterns.
2. Saga reducer generation covers the currently hand-authored reducers without behavior changes.
3. Generic attributes are supported by the current target framework (or a documented decision is made not to use them).
4. Generated output matches existing manual code behavior and signatures.

## Questions
1. Where are `SagaPhaseDto` and `SagaPhaseDtoMapper` defined and used?
2. What generators/patterns are used for aggregate and projection DTOs/mappers/reducers?
3. What saga reducers exist today and what repeated patterns can be generated?
4. What target frameworks and language versions are used for generators and consuming projects?
5. Does the current toolchain support generic attributes in C# for this repo?
6. What would be the generated API surface for saga DTOs/mappers/reducers, and does it match current usage?
7. Do sample apps compile with generated saga DTOs/mappers/reducers?
8. Are any public API changes introduced by replacing manual code with generated equivalents?
9. Where are aggregate/projection generator outputs validated in tests, and what conventions should saga generators match?
10. Do any existing saga reducers rely on handwritten logic that would not be safe to generate?
11. Are there existing generator helper utilities (naming, emitters, analysis) that should be reused?
12. What analyzer or StyleCop constraints apply to generated code (doc comments, file-scoped namespaces, etc.)?

## Answers
1. `SagaPhaseDto` appears in Spring server and client samples, and `SagaPhaseDtoMapper` in Spring server:
	- samples/Spring/Spring.Server/Controllers/Projections/SagaPhaseDto.cs
	- samples/Spring/Spring.Server/Controllers/Projections/Mappers/SagaPhaseDtoMapper.cs
	- samples/Spring/Spring.Client/Features/MoneyTransferStatus/Dtos/SagaPhaseDto.cs
2. Aggregate/projection generator patterns live in the Inlet generators:
	- Server DTO/mapper/controller/registrations: src/Inlet.Server.Generators/ProjectionEndpointsGenerator.cs
	- Client DTO generation: src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs
	- Silo registration for reducers/snapshot converter: src/Inlet.Silo.Generators/ProjectionSiloRegistrationGenerator.cs
3. Saga status reducers in Spring projection map saga lifecycle events to projection state:
	- samples/Spring/Spring.Domain/Projections/MoneyTransferStatus/Reducers/Saga*StatusReducer.cs
4. Target frameworks and language versions:
	- Directory.Build.props: TargetFramework net10.0, LangVersion 14.0
	- Inlet.*.Generators csproj: TargetFramework netstandard2.0, LangVersion 14.0
5. Generic attributes are supported in C# 11+ (C# version history lists generic attributes in C# 11), and
	the repo uses LangVersion 14.0 (so compiler support is present). Evidence:
	- https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-version-history#c-version-11
	- Directory.Build.props LangVersion 14.0
6. Generated API surface should mirror existing manual saga DTOs/mappers/reducers in samples. Evidence will
	require comparing generated output to existing Spring DTOs/mappers/reducers once design is finalized.
7. UNVERIFIED: compilation with generated saga DTOs/mappers/reducers will be verified after implementation.
8. Potential public API changes depend on introducing a generic attribute (new public type) or removing existing
	manual DTOs/mappers in samples. Impact to be assessed after design.
9. Generator test conventions exist in L0 tests for projection generators:
	- tests/Inlet.Client.Generators.L0Tests/ProjectionClientDtoGeneratorTests.cs
	- tests/Inlet.Server.Generators.L0Tests/ProjectionEndpointsGeneratorTests.cs
	- tests/Inlet.Silo.Generators.L0Tests/ProjectionSiloRegistrationGeneratorTests.cs
10. UNVERIFIED: No evidence yet that saga reducers contain custom logic beyond repeated phase/status updates.
11. Existing generator helpers include SourceBuilder and naming utilities:
	- src/Inlet.Generators.Core/Emit/SourceBuilder.cs
	- src/Inlet.Generators.Core/Naming/TargetNamespaceResolver.cs
12. Generated code follows auto-generated headers, file-scoped namespaces, and XML docs in current projection
	generators (e.g., ProjectionEndpointsGenerator). Evidence in generator code and tests above.
