---
id: aqueduct-getting-started
title: Aqueduct Runtime Getting Started
sidebar_label: Getting Started
sidebar_position: 1
description: Build and run a local Orleans host with Aqueduct through the Mississippi runtime composition API.
---

# Aqueduct Runtime Getting Started

## Overview

Build and run a local Orleans silo with Aqueduct's in-memory SignalR backplane through one verified source-checkout
path.

## What you will achieve

You will create a small host that starts Orleans with localhost clustering, registers Aqueduct through
`UseMississippi(...)`, and stops cleanly when you press Ctrl+C.

## Prerequisites

- A Mississippi checkout with the `Mississippi.Sdk.Runtime` project used by this documentation.
- The .NET SDK selected by that checkout's `global.json`. The verification for this page used SDK `10.0.400`.
- PowerShell 7 or later.

The temporary project below references the checkout's `Mississippi.Sdk.Runtime` project directly so its APIs match this
documentation.

## Install

Run these commands from the root of the checkout. The repository's `global.json` selects the SDK used by
the commands; inspect `dotnet --version` before continuing.

```powershell
Set-Location (git rev-parse --show-toplevel)
dotnet --version
New-Item -ItemType Directory -Force .scratchpad/aqueduct-getting-started | Out-Null
dotnet new console --framework net10.0 --output .scratchpad/aqueduct-getting-started --name AqueductGettingStarted
dotnet add .scratchpad/aqueduct-getting-started/AqueductGettingStarted.csproj reference src/Sdk.Runtime/Sdk.Runtime.csproj
```

The `dotnet add reference` command adds the checkout's `Mississippi.Sdk.Runtime` project to the temporary app, keeping
the APIs used by the program aligned with this documentation.

## Create the project

Replace `Program.cs` in the temporary project with this complete host:

```csharp
using System;

using Microsoft.Extensions.Hosting;

using Mississippi.Aqueduct.Runtime;
using Mississippi.Hosting.Runtime;

using Orleans.Hosting;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.UseMississippi(runtime =>
    {
        runtime.AddAqueduct(aqueduct => aqueduct.UseMemoryStreams());
        runtime.ApplyToSilo(siloBuilder);
    });
});

using IHost host = builder.Build();
await host.RunAsync();
```

## Verify it works

Restore and compile the temporary project, then run it from the checkout root:

```powershell
Set-Location (git rev-parse --show-toplevel)
dotnet --version
dotnet restore .scratchpad/aqueduct-getting-started/AqueductGettingStarted.csproj --use-lock-file
dotnet build .scratchpad/aqueduct-getting-started/AqueductGettingStarted.csproj -c Release --no-incremental --no-restore -warnaserror
dotnet run --project .scratchpad/aqueduct-getting-started/AqueductGettingStarted.csproj -c Release --no-build --no-restore
```

The build must report `Build succeeded`, `0 Warning(s)`, and `0 Error(s)`. After startup, press Ctrl+C once. The
normal Orleans and Generic Host logs should include these lines:

```text
Orleans Silo started.
Application started. Press Ctrl+C to shut down.
Application is shutting down...
Orleans Silo stopped.
```

The verification run for this page reached all four lines and returned after the silo stopped through the normal host
shutdown path. Ctrl+C performs the normal Orleans shutdown, including stopping the silo and its stream agents.

## What happened

`UseLocalhostClustering()` configured a local Orleans silo. The `UseMississippi(...)` callback staged the runtime
composition, and `runtime.AddAqueduct(...)` enabled memory streams with the default provider name
`mississippi-streaming`.
`runtime.ApplyToSilo(siloBuilder)` is the recommended explicit native-configuration hook. The nested builder also
registered the Orleans `PubSubStore` convention required by the memory stream setup.

## Summary

The source-checkout path above provides one executable local Aqueduct runtime setup. It starts and stops a real Orleans
silo using the canonical nested composition path and the SDK project reference from the checkout.

## Next Steps

- Use [How To Configure Aqueduct Runtime Composition](../how-to/how-to.md) for host-owned providers and configuration
  overloads.
- Read [Aqueduct Reference](../reference/reference.md) for the complete runtime contract and diagnostics.
- Read [Aqueduct Concepts](../concepts/concepts.md) for the composition lifecycle.
