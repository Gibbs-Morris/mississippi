# Projection registration cannot see DTOs generated in the same compiler run

## Source location

- Project: `Inlet.Client.Generators`.
- Source file: [src/Inlet.Client.Generators/ProjectionClientRegistrationGenerator.cs lines 156-174](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/ProjectionClientRegistrationGenerator.cs#L156-L174).
- Type: `ProjectionClientRegistrationGenerator`.
- Member: `GetProjectionDtosFromCompilation`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The registration generator scans the compiler's original type tree for marked DTOs. Another generator creates those DTOs later in the same run; ordinary source generators do not see each other's output. Combined domain registration can still call the missing AddProjectionsFeature method.

## Trigger

A client project references a separate domain assembly containing [GenerateProjectionEndpoints, ProjectionPath] projections and relies on the packaged client generators.

## Potential impact

A client referencing a separate domain assembly can get DTOs and domain registration without the feature registration they need. It can fail compilation or lack projection reducers unless the application supplies handwritten registration.

## Evidence

- [src/Inlet.Client.Generators/ProjectionClientRegistrationGenerator.cs lines 216-240](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/ProjectionClientRegistrationGenerator.cs#L216-L240): Pipeline receives the original CompilationProvider and returns when discovered DTOs are empty.
- [src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs lines 116-117](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs#L116-L117): DTO is added as generated source during source output.
- [src/Inlet.Client.Generators/DomainClientRegistrationGenerator.cs lines 64-74](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/DomainClientRegistrationGenerator.cs#L64-L74): Referenced projections mark the domain as containing projections.
- [src/Inlet.Client.Generators/DomainClientRegistrationGenerator.cs lines 200-203](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/DomainClientRegistrationGenerator.cs#L200-L203): Composition unconditionally calls AddProjectionsFeature for that domain.
- [samples/Spring/Spring.Client/Features/ProjectionsFeatureRegistration.cs lines 24-32](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/samples/Spring/Spring.Client/Features/ProjectionsFeatureRegistration.cs#L24-L32): Spring supplies a handwritten registration for four generated DTOs, masking this omission.
- Isolated probe (`referenced-projection-registration`): A domain is first compiled to a metadata reference. Client input has zero errors; DTO and composition sources appear, but ProjectionsFeatureRegistration.g.cs does not.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- The probe inspects generator contributions; Hosting/ClientBuilder types are intentionally not implemented in support stubs, so unrelated output diagnostics are not claimed as proof.
- A second artificial generator pass can see prior output, but ordinary compiler execution does not chain normal generators this way.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
