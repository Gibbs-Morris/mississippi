# Generated clients send POST for commands configured with another HTTP method

## Source location

- Project: `Inlet.Client.Abstractions`, `Inlet.Client.Generators`.
- Source file: [src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs lines 149-151](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs#L149-L151).
- Type: `CommandActionEffectBase<TAction,TRequestDto,TState,TExecutingAction,TSucceededAction,TFailedAction>`.
- Member: `HandleAsync`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The common command effect always sends POST. The client generator reads the configured HttpMethod but does not emit a way to use it. The server generator does use that setting, so the two ends disagree for a supported non-POST command.

## Trigger

An aggregate command uses the supported GenerateCommandAttribute.HttpMethod setting PUT, PATCH, or DELETE and the generated action/effect is used by the client.

## Potential impact

A command configured for PUT, PATCH, or DELETE can receive HTTP 405 instead of executing, even when its route and request data match.

## Evidence

- [src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs lines 149-151](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs#L149-L151): The only request API is PostAsJsonAsync.
- [src/Inlet.Generators.Abstractions/GenerateCommandAttribute.cs lines 40-47](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Generators.Abstractions/GenerateCommandAttribute.cs#L40-L47): Public HttpMethod metadata is configurable with a POST default.
- [src/Inlet.Client.Generators/CommandClientActionEffectsGenerator.cs lines 210-218](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/CommandClientActionEffectsGenerator.cs#L210-L218), [src/Inlet.Client.Generators/CommandClientActionEffectsGenerator.cs lines 134-143](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Generators/CommandClientActionEffectsGenerator.cs#L134-L143): Parses the HTTP method but emits only aggregate prefix and route overrides.
- [src/Inlet.Gateway.Generators/AggregateControllerGenerator.cs lines 155-174](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Gateway.Generators/AggregateControllerGenerator.cs#L155-L174): Maps PUT/DELETE/PATCH/GET metadata to server method attributes and emits the selected attribute.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Generate both ends for a PUT/PATCH/DELETE command and capture the actual HttpRequestMessage.Method from the generated effect; compare with the generated controller attribute.
- Generator reviewer independently confirmed the metadata/emission mismatch. GET body semantics are outside this report; use a body-supporting non-POST verb for the minimal verification.

## Confidence

**High**. Supported metadata reaches the server generator but cannot affect the client's unconditional POST request.
