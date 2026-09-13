---
id: aqueduct-how-to
title: How To Configure Aqueduct Runtime Composition
sidebar_label: How To
sidebar_position: 1
description: Configure and validate the Aqueduct SignalR backplane inside an Orleans runtime composition callback.
---

# How To Configure Aqueduct Runtime Composition

## Overview

Configure the Aqueduct runtime backplane once inside the Orleans host's `UseMississippi(...)` callback. The nested
`AqueductBuilder` owns Aqueduct settings; the outer `RuntimeBuilder` remains the place for other runtime features and
advanced native Orleans plumbing.

## When to use this

Use this page when an Orleans silo needs Aqueduct's runtime grains, stream options, and SignalR backplane registration.
For gateway hub registration or client projection delivery, follow the adjacent host-specific documentation instead.

## Before you begin

- Reference `Mississippi.Sdk.Runtime`, or reference both `Mississippi.Aqueduct.Runtime` and
  `Mississippi.Hosting.Runtime`.
- Configure an Orleans stream provider on the host, or choose `UseMemoryStreams()` for local development and tests.
- If the host owns the provider, record its exact name. Aqueduct validates the name but does not create an external
  provider for you.

## Steps

### Attach the runtime terminal

The following block matches the runtime host composition pattern used by the Spring sample. The host's other
services and Orleans provider configuration are outside this focused excerpt.

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

`ApplyToSilo(...)` is the explicit integration hook. If it is omitted, `UseMississippi(...)` applies queued native
configuration automatically when the terminal callback completes.

### Choose one Aqueduct configuration form

The following snippets replace the single `runtime.AddAqueduct(...)` line in the callback above. Choose exactly one
form for each runtime; do not combine them.

#### Existing host-owned provider

Use this form when Orleans already provides the stream provider:

```csharp
runtime.AddAqueduct(aqueduct =>
    aqueduct.StreamProviderName = "StreamProvider");
```

#### Development memory streams

Use this form when the host should create Orleans memory streams for local development or tests:

```csharp
runtime.AddAqueduct(aqueduct => aqueduct.UseMemoryStreams());
```

Call `UseMemoryStreams("ProviderName")` when a non-default development provider name is needed. The method selects
that final provider name before registering memory streams and the `PubSubStore` grain storage convention.

#### Configuration section

Use this form when settings are stored under an `Aqueduct` configuration section:

```csharp
runtime.AddAqueduct(builder.Configuration.GetSection("Aqueduct"));
```

The overload reads `StreamProviderName`, `ServerStreamNamespace`, `AllClientsStreamNamespace`,
`HeartbeatIntervalMinutes`, and `DeadServerTimeoutMultiplier`. Omitted values retain defaults. A malformed integer
becomes invalid and is reported by the structured validation diagnostics.

#### Explicit settings

Use this form when the values are known at composition time:

```csharp
runtime.AddAqueduct(
    "StreamProvider",
    serverStreamNamespace: "mississippi-server",
    allClientsStreamNamespace: "mississippi-all-clients",
    heartbeatIntervalMinutes: 1,
    deadServerTimeoutMultiplier: 3);
```

### Keep advanced native configuration on the runtime root

Use `runtime.ConfigureSilo(...)` to queue synchronous native Orleans callbacks, and use `runtime.Services` only for
advanced staged service registration. Do not mutate the captured host service collection from inside the callback.
See [Runtime Composition](../../reference/runtime-composition.md) for the staging and publication rules.

## Verify the result

- The host has exactly one `UseMississippi(...)` callback for its runtime composition.
- The nested callback selects a nonempty stream provider and nonempty stream namespaces.
- `HeartbeatIntervalMinutes` and `DeadServerTimeoutMultiplier` are positive.
- The selected stream provider exists on the host, unless `UseMemoryStreams(...)` created it for local development or
  tests.
- The host can build and start using its normal Orleans validation and connectivity checks.

## Migrate From The Legacy Runtime Entry Point

The runtime cutover removes the silo-level `UseAqueduct(...)` entry point and the host-capturing
`AqueductSiloOptions` type. Keep the legacy form only while identifying the code to migrate:

```csharp
// Legacy runtime composition being migrated.
siloBuilder.UseAqueduct(options =>
{
    options.StreamProviderName = "StreamProvider";
});
```

Move the settings into the nested builder and keep the terminal attachment on `UseMississippi(...)`:

```csharp
siloBuilder.UseMississippi(runtime =>
{
    runtime.AddAqueduct(aqueduct =>
        aqueduct.StreamProviderName = "StreamProvider");
    runtime.ApplyToSilo(siloBuilder);
});
```

For the legacy custom memory setup, replace the old provider-and-storage-name pair with
`UseMemoryStreams("ProviderName")`. The new runtime builder uses the Orleans `PubSubStore` convention and does not
accept a separate storage-name argument. Update callers and tests in the same change; the removed runtime symbols are
not compatibility wrappers.

## Summary

Configure Aqueduct once as a nested runtime builder, validate it through `UseMississippi(...)`, and apply native
Orleans configuration through the outer runtime builder. Memory streams are a local/test convenience; production
provider registration remains host-owned.

## Next Steps

- Use [Aqueduct Reference](../reference/reference.md) for options, defaults, and diagnostics.
- Read [Aqueduct Operations](../operations/operations.md) for provider and rollout considerations.
- Use [Aqueduct Troubleshooting](../troubleshooting/troubleshooting.md) when composition or provider resolution fails.
