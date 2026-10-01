# Reserved characters in an entity ID change the command URL

## Source location

- Project: `Inlet.Client.Abstractions`.
- Source file: [src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs lines 208-211](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs#L208-L211).
- Type: `CommandActionEffectBase<TAction,TRequestDto,TState,TExecutingAction,TSucceededAction,TFailedAction>`.
- Member: `GetEndpoint`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

The command effect inserts EntityId directly into a URL path. Characters such as # and ? are then interpreted as a fragment or query instead of part of the ID. The corresponding projection fetcher escapes the ID as a path segment.

## Trigger

A command targets a string entity ID containing '#' or '?', for example customer#42 or customer?42. The interface imposes no URL-safe-ID constraint, and corresponding projection fetching treats the entity as an escaped path segment.

## Potential impact

The request can lose its command suffix, fail routing, or address an unintended entity. Text after a fragment marker is never sent to the server.

## Evidence

- [src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs lines 149-151](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs#L149-L151), [src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs lines 208-211](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs#L208-L211): The unescaped endpoint is passed directly to HttpClient.
- [src/Inlet.Client.Abstractions/Actions/ICommandAction.cs lines 19-28](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client.Abstractions/Actions/ICommandAction.cs#L19-L28): EntityId is a string identifying the aggregate; no URL-safe character restriction is declared.
- [src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs lines 79](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs#L79), [src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs lines 103](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Inlet.Client/ActionEffects/AutoProjectionFetcher.cs#L103): The corresponding projection endpoints explicitly use Uri.EscapeDataString(entityId), demonstrating the path-segment treatment.
- [docs/Docusaurus/docs/inlet/reference/generated-contracts.md lines 103](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/docs/Docusaurus/docs/inlet/reference/generated-contracts.md#L103), [docs/Docusaurus/docs/inlet/reference/generated-contracts.md lines 112](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/docs/Docusaurus/docs/inlet/reference/generated-contracts.md#L112): Entity ID identifies the aggregate and projection fetcher escapes it as a path segment.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Capture the request URI for an ID containing '#': verify the encoded value reaches the server path rather than becoming a fragment. Also check '?' separately.
- Do not claim every '/' ID can be routed by ASP.NET; '#' and '?' already demonstrate the missing client escaping independently of encoded-slash server limitations.

## Confidence

**High**. URI parsing of reserved characters changes the generated endpoint before the request is sent.
