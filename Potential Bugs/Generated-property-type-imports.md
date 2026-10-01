# Generated DTO files cannot resolve valid property types

## Source location

- Project: `Inlet.Generators.Core`.
- Source file: [src/Inlet.Generators.Core/Analysis/PropertyModel.cs lines 31-34](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/PropertyModel.cs#L31-L34).
- Type: `PropertyModel`.
- Member: `PropertyModel constructor`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The model renders property types using short names such as Guid, `List<int>`, or Mode. Generated files do not consistently import those types' namespaces or use their full names. A using statement in the original source file does not apply to a generated file.

## Trigger

A valid command includes Guid, DateTime, a domain enum/custom value type, or a projection exposes `List<int>`; the consuming compilation has no compensating global using.

## Potential impact

A valid command, saga input, or projection can produce code that fails compilation because the property type cannot be found. Global using statements can hide the problem in some applications.

## Evidence

- [src/Inlet.Client.Generators/CommandClientDtoGenerator.cs lines 71-90](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/CommandClientDtoGenerator.cs#L71-L90): Emits the namespace and SourceTypeName properties with no usings.
- [src/Inlet.Gateway.Generators/CommandServerDtoGenerator.cs lines 105-134](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/CommandServerDtoGenerator.cs#L105-L134): Imports only Json.Serialization, then emits minimally qualified SourceTypeName.
- [src/Inlet.Client.Generators/SagaClientDtoGenerator.cs lines 26-45](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/SagaClientDtoGenerator.cs#L26-L45): Saga DTO also emits input SourceTypeName without imported namespaces.
- [src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs lines 70-84](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Core/Analysis/TypeAnalyzer.cs#L70-L84): Generic DTO names are short, and framework values use MinimallyQualifiedFormat.
- [src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs lines 84-87](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs#L84-L87): Projection DTO imports System and Immutable but not Collections.Generic.
- Isolated probe (`command-guid-usings`): Input has ordinary using System and zero input errors; generated Guid field raises CS0246.
- Isolated probe (`command-domain-type-usings`): A command's Mode enum is defined and source compiles; emitted request DTO cannot resolve Mode.
- Isolated probe (`projection-list-usings`): `List<int>` input compiles with file-local usings; generated DTO cannot resolve List<>.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- Global imports in Mississippi or some consumers can conceal this problem. Domain-type probe reproduces it even with System and generic global imports.
- This finding is about type resolution in generated files, not adding a missing feature.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
