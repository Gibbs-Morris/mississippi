# Domain registration skips sagas declared with the generic marker

## Source location

- Project: `Inlet.Generators.Core`.
- Source file: [src/Inlet.Generators.Core/Analysis/GeneratorSymbolAnalysis.cs lines 20-34](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/GeneratorSymbolAnalysis.cs#L20-L34).
- Type: `GeneratorSymbolAnalysis`.
- Member: `ContainsAttribute`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The shared attribute check compares a constructed marker such as `GenerateSagaEndpoints<Input>` with the open generic definition. Those symbols are different. Dedicated saga generators recognise the marker, but the combined client and silo registration generators do not.

## Trigger

Declare a saga with [`GenerateSagaEndpoints<Input>`] and use the generated AddDomainClient/AddDomainSilo composition method to register it.

## Potential impact

Generated domain registration omits the saga. A domain containing only generic-marker sagas has no combined registration method; a mixed domain has an incomplete method. The probe checked emitted registration code rather than a complete running host.

## Evidence

- [src/Inlet.Client.Generators/DomainClientRegistrationGenerator.cs lines 77-98](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/DomainClientRegistrationGenerator.cs#L77-L98): Uses ContainsAttribute with both non-generic and open generic saga symbols.
- [src/Inlet.Runtime.Generators/DomainSiloRegistrationGenerator.cs lines 95-116](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Runtime.Generators/DomainSiloRegistrationGenerator.cs#L95-L116): Uses the same generic identity check.
- [src/Inlet.Client.Generators/SagaClientGeneratorHelper.cs lines 128-134](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/SagaClientGeneratorHelper.cs#L128-L134): Dedicated client saga matching compares OriginalDefinition correctly.
- [src/Inlet.Runtime.Generators/SagaSiloRegistrationGenerator.cs lines 339-356](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Runtime.Generators/SagaSiloRegistrationGenerator.cs#L339-L356): Dedicated runtime saga matching also uses OriginalDefinition.
- [src/Inlet.Generators.Abstractions/GenerateSagaEndpointsAttribute{TInput}.cs lines 6-51](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Abstractions/GenerateSagaEndpointsAttribute%7BTInput%7D.cs#L6-L51): Generic public marker is documented as the strongly typed saga form.
- Isolated probe (`generic-saga-client-composition`): Zero input errors, zero generator diagnostics and zero generated composition sources; non-generic control emits DomainFeatureRegistrations.
- Isolated probe (`generic-saga-runtime-composition`): Zero input errors, runtime dependency symbols provided, and no source; non-generic control emits DomainSiloRegistrations.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- Verify desired composition includes both public marker forms, as existing dedicated generators and generated-contracts.md:27 indicate.
- No full host startup probe was executed.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
