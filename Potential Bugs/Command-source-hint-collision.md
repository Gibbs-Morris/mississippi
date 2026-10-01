# Commands with the same name in different aggregates clash during generation

## Source location

- Project: `Inlet.Client.Generators`.
- Source file: [src/Inlet.Client.Generators/CommandClientDtoGenerator.cs lines 89-93](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/CommandClientDtoGenerator.cs#L89-L93).
- Type: `CommandClientDtoGenerator`.
- Member: `GenerateClientDto`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The generated source name uses the command's short name without its aggregate or namespace. Two valid Transfer commands in different aggregate namespaces both request TransferRequestDto.g.cs, even though their output types belong in different namespaces.

## Trigger

A domain has common command names reused across aggregates, for example Account.Commands.Transfer and Ledger.Commands.Transfer.

## Potential impact

Roslyn rejects the repeated source name and drops the generator's output. The client project can then fail to compile because its command request types are missing. Several related command generators use the same short-name pattern.

## Evidence

- [src/Inlet.Gateway.Generators/CommandServerDtoGenerator.cs lines 90-95](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/CommandServerDtoGenerator.cs#L90-L95): Server emits command DTO/mapper hints from the same short-name-derived model.
- [src/Inlet.Client.Generators/CommandClientActionEffectsGenerator.cs lines 144](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/CommandClientActionEffectsGenerator.cs#L144): Effect source hints also omit aggregate/source namespace identity.
- Isolated probe (`same-command-short-name`): Both input commands are valid; CS8785 reports duplicate TransferRequestDto.g.cs and outputs are empty.

## Verification notes

Current-source inspection and isolated generator probes where noted. The probes use support stubs; they are not a complete application build or an independent verification.

- Distinct from the projection nested-DTO HashSet suppressing required output; this is AddSource identity collision among root command outputs.

## Confidence

**High**. Exact current-source control flow supports the failure, with isolated unchanged-source compiler/output evidence where noted.
