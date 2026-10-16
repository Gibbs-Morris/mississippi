# Client feature generation rejects namespaces accepted by domain registration

## Source location

- Project: `Inlet.Client.Generators`.
- Source file: [src/Inlet.Client.Generators/CommandClientActionEffectsGenerator.cs lines 76-81](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/CommandClientActionEffectsGenerator.cs#L76-L81).
- Type: `CommandClientActionEffectsGenerator`.
- Member: `GenerateActionEffect`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The newer namespace resolver accepts App.Aggregates.Account.Commands. Several client feature generators still require .Domain.Aggregates. and skip that command. Combined domain registration uses the newer resolver and can call a feature method that was never generated.

## Trigger

Consume a valid generated command in App.Aggregates.Account.Commands, with a target application namespace resolved from RootNamespace or AssemblyName.

## Potential impact

The client can fail to compile because AddAccountAggregateFeature is missing, or lack the state, reducers, and command effects needed to run that feature.

## Evidence

- [src/Inlet.Generators.Core/Naming/NamingConventions.cs lines 67-89](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Naming/NamingConventions.cs#L67-L89): Legacy helper returns null unless the namespace contains .Domain.Aggregates. and ends in .Commands.
- [src/Inlet.Generators.Core/Naming/TargetNamespaceResolver.cs lines 38-69](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Naming/TargetNamespaceResolver.cs#L38-L69): New resolver extracts names from .Aggregates. without requiring .Domain.
- [src/Inlet.Client.Generators/CommandClientStateGenerator.cs lines 106-109](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/CommandClientStateGenerator.cs#L106-L109): State discovery skips commands when legacy extraction returns null.
- [src/Inlet.Client.Generators/CommandClientReducersGenerator.cs lines 185-189](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/CommandClientReducersGenerator.cs#L185-L189): Reducer discovery uses the same legacy gate.
- [src/Inlet.Client.Generators/CommandClientRegistrationGenerator.cs lines 161-165](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/CommandClientRegistrationGenerator.cs#L161-L165): Feature registration discovery uses the legacy gate.
- [src/Inlet.Client.Generators/DomainClientRegistrationGenerator.cs lines 49-61](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/DomainClientRegistrationGenerator.cs#L49-L61): Domain composition uses the newer resolver.
- Isolated probe (`non-domain-client-composition`): Zero input errors; only DomainFeatureRegistrations.g.cs is emitted and it calls AddAccountAggregateFeature at line 27. Matching .Domain control emits all five expected source categories.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- Naming acceptance is explicit in TargetNamespaceResolver, not an invented arbitrary namespace layout.
- Probe output dependency diagnostics are intentionally stub-related; missing generated source categories are the relevant result.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
