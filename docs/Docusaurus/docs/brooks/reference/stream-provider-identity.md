---
id: stream-provider-identity
title: Brooks Stream Provider Identity
description: Reference the Orleans provider name, cursor stream namespace, and publisher/subscriber identity boundary.
sidebar_position: 8
sidebar_label: Stream Provider Identity
---

# Brooks Stream Provider Identity

## Overview

Cursor notifications use an Orleans stream provider and a stream identity within that provider. The provider name, stream namespace, and brook key are separate values.

## Applies To

- `Mississippi.Brooks.Abstractions.Streaming.BrookProviderOptions`
- `Mississippi.Brooks.Abstractions.Streaming.IStreamIdFactory`
- The built-in Brooks publisher, Brooks and UX cursor subscribers, and Inlet subscriber

## Default Identity

| Value | Built-in default |
|-------|------------------|
| Orleans provider name | `mississippi-streaming` |
| Cursor stream namespace | `BrookCursorUpdates` |
| Cursor stream key | The complete brook key, in `brookName\|entityId` form |

The [provider options](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Abstractions/Streaming/BrookProviderOptions.cs) initialize `OrleansStreamProviderName` from [BrookStreamingDefaults](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Abstractions/Streaming/BrookStreamingDefaults.cs). The property has a public setter and no option-level validation guard.

The [default stream-ID factory](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime/StreamIdFactory.cs) calls `StreamId.Create` with the [cursor namespace constant](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime/BrooksRuntimeOrleansStreamNames.cs) and the complete brook key. It produces the same identity for repeated calls with the same key, as verified by the [factory tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Brooks.Runtime.L0Tests/StreamIdFactoryTests.cs).

The entity ID alone is not the default stream key. Brooks with different names remain distinct even when their entity IDs match.

## Configuration And Registration

`IRuntimeBuilder.AddEventSourcing` accepts an optional `Action<BrookProviderOptions>` callback. Set `OrleansStreamProviderName` there to select the host's provider. The host supplies Orleans streams and storage; this registration does not create the selected provider.

Registration does not validate that the named provider exists. An incorrect name fails when Orleans `GetStreamProvider` is called: Brooks and UX cursors resolve it during activation, while Inlet resolves it on the first subscription to each brook. Writer lookup occurs on its publication path. Register the matching provider in the host before using this path.

The [registration](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime/BrooksRuntimeRegistrations.cs) adds the default factory with `TryAddSingleton<IStreamIdFactory, StreamIdFactory>`, preserving an existing unkeyed registration. Its [tests](https://github.com/Gibbs-Morris/mississippi/blob/main/tests/Brooks.Runtime.L0Tests/BrooksRuntimeRegistrationsTests.cs) verify preservation and singleton lifetime. Use [Runtime Composition](../../reference/runtime-composition.md) for the complete host registration flow.

## Custom Factory Boundary

The [Brooks cursor grain](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime/Cursor/BrookCursorGrain.cs), [UX projection cursor grain](https://github.com/Gibbs-Morris/mississippi/blob/main/src/DomainModeling.Runtime/UxProjectionCursorGrain.cs), and [Inlet subscription grain](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Runtime/Grains/InletSubscriptionGrain.cs) obtain their subscription stream IDs from `IStreamIdFactory`. All three select the provider named by `BrookProviderOptions`.

The [built-in writer](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Brooks.Runtime/Writer/BrookWriterGrain.cs) selects that provider but constructs its stream ID directly from `BrookCursorUpdates` and its full brook key. It does not call `IStreamIdFactory`.

Replacing the factory therefore changes subscriber identity without redirecting the built-in publisher. A replacement used with that publisher needs to return a compatible identity; otherwise subscribers listen on a different stream.

## Summary

The built-in cursor stream combines a configured provider with a fixed namespace and full brook key. Custom subscription identity does not change the built-in writer's publication identity.

## Next Steps

- Read [Runtime Composition](../../reference/runtime-composition.md) for host-owned providers and `AddEventSourcing` setup.
- Read [Brook Append Outcomes](../../reference/brook-append-outcomes.md) for cursor publication and retry boundaries.
