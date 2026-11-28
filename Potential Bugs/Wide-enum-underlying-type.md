# Generated projection enums cannot hold valid wide values

## Source location

- Project: `Inlet.Client.Generators`.
- Source file: [src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs lines 169-183](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/ProjectionClientDtoGenerator.cs#L169-L183).
- Type: `ProjectionClientDtoGenerator`.
- Member: `GenerateNestedEnumDto`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The generated enum copies source values but omits its underlying integer type. C# therefore treats it as int, even when the source enum uses long, ulong, or uint.

## Trigger

A domain enum uses long/ulong/uint with a value outside the int range and appears in a projection.

## Potential impact

A valid source value outside the int range can make the generated enum fail compilation, preventing that projection's generated client or endpoint code from building.

## Evidence

- [src/Inlet.Gateway.Generators/ProjectionEndpointsGenerator.cs lines 574-582](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/ProjectionEndpointsGenerator.cs#L574-L582): Gateway enum emission has the same default-int declaration and constant copying.
- Isolated probe (`wide-enum`): Status:long with Huge=2147483648L compiles as input; generated StatusDto raises CS0266 because it is implicitly int.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- Enums whose values happen to fit int can mask the lost base type; the reproduced wide value is valid C#.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
