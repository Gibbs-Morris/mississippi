---
id: aqueduct-reference
title: Aqueduct Reference
sidebar_label: Reference
sidebar_position: 1
description: Reference the Aqueduct runtime builder, options, defaults, and structured composition diagnostics.
---

# Aqueduct Reference

## Overview

Aqueduct is the Mississippi subsystem for Orleans-backed SignalR backplane integration. This page documents the
runtime composition contract and the options required by an Orleans host.

## Applies to

- `Mississippi.Aqueduct.Abstractions`
- `Mississippi.Aqueduct.Gateway`
- `Mississippi.Aqueduct.Runtime`

The runtime API in this page is included by `Mississippi.Sdk.Runtime`. Gateway hub registration is a separate
host-specific surface and is not a second runtime attachment path.

## Contract

Compose Aqueduct from an Orleans silo's single runtime terminal callback:

```csharp
using Mississippi.Aqueduct.Runtime;
using Mississippi.Hosting.Runtime;


builder.UseOrleans(siloBuilder =>
{
    siloBuilder.UseMississippi(runtime =>
    {
        runtime.AddAqueduct(aqueduct =>
            aqueduct.StreamProviderName = "StreamProvider");
        runtime.ApplyToSilo(siloBuilder);
    });
});
```

`AddAqueduct(...)` returns the same `IRuntimeBuilder` for chaining. Its optional `Action<AqueductBuilder>` runs when
the runtime applies queued native configuration. `ApplyToSilo(...)` is the recommended explicit hook; omitting it
allows `UseMississippi(...)` to apply queued native configuration automatically at the end of the terminal callback.

The overloads are:

| API | Contract |
| --- | --- |
| `IRuntimeBuilder AddAqueduct(Action<AqueductBuilder>? configure = null)` | Queue one nested Aqueduct configuration for the runtime |
| `IRuntimeBuilder AddAqueduct(IConfiguration configuration)` | Read option property-name keys from the supplied configuration |
| `IRuntimeBuilder AddAqueduct(string streamProviderName, string serverStreamNamespace = ..., string allClientsStreamNamespace = ...)` | Queue explicit stream and namespace settings |

Only one `AddAqueduct(...)` call is valid for a given runtime builder. Combine settings in one call.

## Verified Ownership Boundary

- Distributed SignalR backplane integration
- Orleans-driven push delivery of events and notifications into SignalR-connected clients
- Gateway-side hub lifetime management and notifier registration
- Runtime-side backplane registration
- Aqueduct-specific options and abstractions for distributed message routing

## Options

`AqueductBuilder` exposes these settings inside the `AddAqueduct(...)` callback:

| Property | Meaning | Default |
| --- | --- | --- |
| `StreamProviderName` | Orleans stream provider used for SignalR delivery | `mississippi-streaming` |
| `ServerStreamNamespace` | Namespace for server-targeted messages | `mississippi-server` |
| `AllClientsStreamNamespace` | Namespace for broadcasts to all clients | `mississippi-all-clients` |

Names must be nonempty and must not consist only of whitespace. The values are preserved as supplied.

## Defaults

The defaults are provided by `AqueductStreamDefaults` and `AqueductOptions`:

- `StreamProviderName`: `mississippi-streaming`
- `ServerStreamNamespace`: `mississippi-server`
- `AllClientsStreamNamespace`: `mississippi-all-clients`

## Memory Streams

`aqueduct.UseMemoryStreams()` enables Orleans memory streams using the builder's final `StreamProviderName` and adds
the `PubSubStore` grain storage convention. `aqueduct.UseMemoryStreams("ProviderName")` selects the supplied provider
name and enables the same registrations. These methods are intended for development and tests.

For a host-owned provider, set `StreamProviderName` to the existing provider name. Aqueduct does not provision an
external provider through this API.

## Configuration

The `IConfiguration` overload reads these exact keys from the supplied configuration object:

| Key | Target property |
| --- | --- |
| `StreamProviderName` | `AqueductBuilder.StreamProviderName` |
| `ServerStreamNamespace` | `AqueductBuilder.ServerStreamNamespace` |
| `AllClientsStreamNamespace` | `AqueductBuilder.AllClientsStreamNamespace` |

Missing keys keep the defaults.

## Behavior

The nested configuration is applied to the runtime's staged silo, then copied into a snapshot used to configure
`IOptions<AqueductOptions>`. Capturing an `AqueductBuilder` beyond its callback is unsupported: the scope closes after
success or failure, and later property changes throw `BuilderValidationException` with `MSB206`.

Runtime service descriptors are staged with the surrounding `RuntimeBuilder`. Use `runtime.Services` or
`runtime.ConfigureSilo(...)` for advanced runtime work; Aqueduct does not expose a nested service collection.

## Related But Separate Areas

- [Inlet](../../inlet/index.md) composes with Aqueduct for higher-level projection delivery.
- [Domain Modeling](../../domain-modeling/index.md) owns domain behavior, not transport infrastructure.
- [Runtime Composition](../../reference/runtime-composition.md) documents the terminal runtime lifecycle and native
  Orleans integration.

## Failure behavior

`BuilderValidationException.Diagnostics` contains a stable `Code` plus `Message` and `Remediation` guidance. The
current Aqueduct diagnostic codes are:

| Code | Failure | Remediation |
| --- | --- | --- |
| `MSB201` | `StreamProviderName` is empty or whitespace | Set a nonempty provider name |
| `MSB202` | `ServerStreamNamespace` is empty or whitespace | Set a nonempty server namespace |
| `MSB203` | `AllClientsStreamNamespace` is empty or whitespace | Set a nonempty broadcast namespace |
| `MSB206` | A captured nested builder scope is closed | Configure a fresh `AddAqueduct(...)` callback |
| `MSB207` | Aqueduct was added more than once to one runtime | Combine settings in one `AddAqueduct(...)` call |

Null runtime or configuration arguments produce `ArgumentNullException`. The optional `AddAqueduct(...)` configuration
callback may be omitted. Required callbacks on the runtime terminal and native configuration APIs also report
`ArgumentNullException`; see [Runtime Composition](../../reference/runtime-composition.md).

## Compatibility

The runtime composition layer removes the silo-level `UseAqueduct(...)` entry point and the
`AqueductSiloOptions` host-capturing type. Migrate those settings to `runtime.AddAqueduct(...)` inside
`UseMississippi(...)`. The new `UseMemoryStreams("ProviderName")` overload uses the Orleans `PubSubStore` convention
and does not accept a separate storage-name argument.

## Summary

Use `runtime.AddAqueduct(...)` once inside `UseMississippi(...)`, choose a provider strategy, and rely on the stable
diagnostics when validation rejects the composition.

## Next Steps

- Read [Aqueduct Concepts](../concepts/concepts.md).
- Follow [How To Configure Aqueduct Runtime Composition](../how-to/how-to.md) for the runtime setup task sequence.
- Follow [Aqueduct Runtime Composition (Next)](../migration/migration.md) for the runtime API cutover.
- Read [Aqueduct Operations](../operations/operations.md) for provider and rollout guidance.
