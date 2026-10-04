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

The runtime builder controls the stream provider and server namespace. The gateway keeps ownership of the broadcast
namespace used among gateways.

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

The overload reads `StreamProviderName` and `ServerStreamNamespace`. Omitted values retain their runtime defaults;
`AllClientsStreamNamespace` remains a gateway setting for broadcasts.

#### Explicit settings

Use this form when the values are known at composition time:

```csharp
runtime.AddAqueduct(
    "StreamProvider",
    serverStreamNamespace: "mississippi-server");
```

### Keep advanced native configuration on the runtime root

Use `runtime.ConfigureSilo(...)` to queue synchronous native Orleans callbacks, and use `runtime.Services` only for
advanced staged service registration. Do not mutate the captured host service collection from inside the callback.
See [Runtime Composition](../../reference/runtime-composition.md) for the staging and publication rules.

## Verify the result

- The host has exactly one `UseMississippi(...)` callback for its runtime composition.
- The nested callback selects a nonempty stream provider and server namespace. Configure the gateway broadcast namespace
  separately on participating gateways.
- The selected stream provider exists on the host, unless `UseMemoryStreams(...)` created it for local development or
  tests.
- The host can build and start using its normal Orleans validation. Provider resolution alone does not check external
  connectivity or message delivery.

### Verify a host-owned named provider

When the host owns the stream provider, register it on `siloBuilder` and pass the same name to `AddAqueduct(...)`.
This source-checkout check registers an Orleans memory stream provider independently of Aqueduct; it does not call
`aqueduct.UseMemoryStreams()`.

From the checkout root, create a temporary project with the SDK project reference used by the [getting-started
path](../getting-started/getting-started.md):

```powershell
Set-Location (git rev-parse --show-toplevel)
dotnet --version
dotnet new console --framework net10.0 --output .scratchpad/aqueduct-provider-resolution --name AqueductProviderResolution
dotnet add .scratchpad/aqueduct-provider-resolution/AqueductProviderResolution.csproj reference src/Sdk.Runtime/Sdk.Runtime.csproj
```

Replace `Program.cs` with this complete host. It resolves the selected provider before entering the normal
`RunAsync()` lifecycle:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Aqueduct.Runtime;
using Mississippi.Hosting.Runtime;

using Orleans.Hosting;
using Orleans.Streams;


const string providerName = "host-owned-provider";

HostApplicationBuilder builder = Host.CreateApplicationBuilder();
builder.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryStreams(providerName);
    siloBuilder.AddMemoryGrainStorage("PubSubStore");
    siloBuilder.UseMississippi(runtime =>
    {
        runtime.AddAqueduct(aqueduct => aqueduct.StreamProviderName = providerName);
        runtime.ApplyToSilo(siloBuilder);
    });
});

using IHost host = builder.Build();

IOptions<AqueductOptions> options = host.Services.GetRequiredService<IOptions<AqueductOptions>>();
string selectedProviderName = options.Value.StreamProviderName;
_ = host.Services.GetRequiredKeyedService<IStreamProvider>(selectedProviderName);
Console.WriteLine($"Resolved host-owned provider '{selectedProviderName}'.");

await host.RunAsync();
```

Run the check from the checkout root. The restore and build use the SDK selected by `global.json`; the verification
used SDK `10.0.400`:

```powershell
Set-Location (git rev-parse --show-toplevel)
dotnet --version
dotnet restore .scratchpad/aqueduct-provider-resolution/AqueductProviderResolution.csproj --use-lock-file
dotnet build .scratchpad/aqueduct-provider-resolution/AqueductProviderResolution.csproj -c Release --no-incremental --no-restore -warnaserror
dotnet run --project .scratchpad/aqueduct-provider-resolution/AqueductProviderResolution.csproj -c Release --no-build --no-restore
```

The build must report `0 Warning(s)` and `0 Error(s)`. After the resolved-provider line appears, wait for the normal
Orleans startup message and press Ctrl+C once. The built-in logs should include:

```text
Resolved host-owned provider 'host-owned-provider'.
Orleans Silo started.
Application started. Press Ctrl+C to shut down.
Application is shutting down...
Orleans Silo stopped.
```

This check proves that the configured name and keyed service registration agree. To prove the failure path, leave the
host registration at `host-owned-provider` and change only the `AddAqueduct(...)` value to a different nonempty name,
such as `valid-but-missing-provider`. Running the same command must produce an unhandled `InvalidOperationException`
from `GetRequiredKeyedService(...)` before `RunAsync()` starts the host; no Orleans startup log should appear. Because
the run command uses `--no-build`, run the documented build command again before rerunning it, and rebuild again after
restoring matching names for the normal run. Provider resolution does not check external connectivity or message
delivery.

## Summary

Configure Aqueduct once as a nested runtime builder, validate it through `UseMississippi(...)`, and apply native
Orleans configuration through the outer runtime builder. Memory streams are a local/test convenience; production
provider registration remains host-owned.

## Next Steps

- Use [Aqueduct Reference](../reference/reference.md) for options, defaults, and diagnostics.
- Read [Aqueduct Operations](../operations/operations.md) for provider and rollout considerations.
- Use [Aqueduct Troubleshooting](../troubleshooting/troubleshooting.md) when composition or provider resolution fails.
- Follow [Aqueduct Runtime Composition (Next)](../migration/migration.md) for the API cutover.
