---
title: Reservoir DevTools Reference
description: Reference DevTools enablement, payload options, sanitizers, state restoration, and initialization diagnostics.
sidebar_position: 4
sidebar_label: DevTools
---

# Reservoir DevTools Reference

## Overview

`ReservoirDevToolsOptions` configures the local Redux DevTools integration supplied by `Mississippi.Reservoir.Client`.

## Options

| Option | Default | Behavior |
| --- | --- | --- |
| `Enablement` | `Off` | Selects whether the service connects |
| `Name` | `null` | Nonblank value is forwarded as the extension instance name |
| `MaxAge` | `null` | Maximum retained action count in DevTools history; forwarded as `maxAge` |
| `Latency` | `null` | Batching latency in milliseconds; forwarded as `latency` |
| `AutoPause` | `null` | Set values are forwarded as `autoPause` |
| `AdditionalOptions` | Empty dictionary | Extra extension options, applied after typed options |
| `ActionSanitizer` | `null` | Optional action-payload replacement |
| `StateSanitizer` | `null` | Optional state-payload replacement |
| `SerializerOptions` | Web JSON defaults, case-insensitive property names | Serialization and restoration configuration |
| `IsStrictStateRehydrationEnabled` | `false` | Requires all registered features to be present and deserializable before applying a restored snapshot |
| `ThrowOnMissingInitializer` | `null` | Overrides the initialization checker's throw/warn choice when that hosted checker runs |

An `AdditionalOptions` entry with the same key as a typed option replaces that key's outgoing value. Unset nullable forwarding options remain absent from the options payload; their extension-side defaults are owned by Redux DevTools.

## Enablement

| Mode | Condition |
| --- | --- |
| `Off` | Integration is disabled |
| `Always` | Integration is enabled |
| `DevelopmentOnly` | Injected `IHostEnvironment.IsDevelopment()` is true |

Use the [WebAssembly setup recipe](../how-to/enable-devtools.md) to select `Always` or `Off` from `WebAssemblyHostBuilder.HostEnvironment` explicitly. Keep service registration available for the initializer even when the selected mode is `Off`.

Prefer `Off` outside controlled development or diagnostic environments. Selecting `Always` permits ordinary action payloads and feature-state snapshots to reach the browser extension, which can also request local restoration. Enable it deliberately, choose what data may be exposed, and supply appropriate sanitizers for that environment.

## Payloads

The normal action payload includes the action type name and JSON serialized from the concrete action type. The normal state payload maps feature keys to JSON serialized from each concrete state type.

A non-null sanitizer result replaces that payload. A null result falls back to the normal payload. Sanitizers affect what the extension receives, so preserve the information needed for the debugging or restoration task you intend to perform.

## Local State Restoration

| DevTools operation | Reservoir behavior |
| --- | --- |
| `JUMP_TO_STATE`, `JUMP_TO_ACTION` | Deserializes the supplied state into registered feature types |
| `RESET` | Dispatches the system action that restores initial feature states |
| `COMMIT` | Records the current local snapshot as the rollback point |
| `ROLLBACK` | Restores the committed local snapshot |
| `IMPORT_STATE` | Restores the final `computedStates` entry's state from the imported payload |

The store's system restoration path updates local feature state directly and can notify listeners. It bypasses ordinary user reducers, effects, and middleware. Use application commands for server-side business changes.

Strict restoration rejects the whole proposed restore when a registered feature is missing or fails deserialization. In the default mode, valid registered features can be restored while missing or invalid entries are left as they are. Extra input keys are outside the registered-feature iteration.

Strict validation applies to JSON restoration through `JUMP_TO_STATE`, `JUMP_TO_ACTION`, and the final imported snapshot from `IMPORT_STATE`. `RESET` restores registered initial state and `ROLLBACK` restores the in-memory committed snapshot through system actions; those operations do not use the JSON strict-validation path.

## Initialization Diagnostics

The root initializer starts observation after rendering and captures the initial rollback point. A later ordinary `ActionDispatchedEvent` triggers connection and reporting. The registration also supplies a hosted initialization checker; in hosts that execute it, the default check delay is five seconds. An explicit `ThrowOnMissingInitializer` value controls its response; otherwise it throws in an injected Development host environment and logs a warning in other cases.

## Source

[ReservoirDevToolsOptions](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Client/ReservoirDevToolsOptions.cs), [ReduxDevToolsService](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Client/ReduxDevToolsService.cs), [initialization checker](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Client/DevToolsInitializationCheckerService.cs), and [Store](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Core/Store.cs) define these behaviors.

## Summary

DevTools exposes local actions and snapshots for inspection. Configure environment selection and payloads deliberately, and treat restoration as a local development operation.

## Next Steps

[Enable DevTools](../how-to/enable-devtools.md) in a WebAssembly client.
