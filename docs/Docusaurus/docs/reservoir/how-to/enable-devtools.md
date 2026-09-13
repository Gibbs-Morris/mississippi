---
title: Enable Redux DevTools in a WebAssembly Client
description: Register Reservoir DevTools, initialize it after rendering, and inspect local actions and state during development.
sidebar_position: 4
sidebar_label: Enable DevTools
---

# Enable Redux DevTools in a WebAssembly Client

## Overview

Connect Reservoir to the Redux DevTools browser extension to inspect dispatched actions and local feature state. Use the action sequence and state snapshots to explain a UI result or give an AI assistant concrete debugging evidence.

## When to use this

Use this setup during client development when you need to inspect state transitions or explore a recorded local state. Use application commands for business changes on the server; DevTools restoration changes the local store snapshot.

## Before you begin

- Have a .NET 10 Blazor WebAssembly client using `Mississippi.Reservoir.Client` and a configured Reservoir builder.
- Install the browser extension through the [official Redux DevTools project](https://github.com/reduxjs/redux-devtools#documentation).
- Keep development data appropriate for browser inspection, and choose sanitizers for fields your application should omit or redact.

## Steps

### 1. Register the Integration

In `Program.cs`, use these namespaces for the WebAssembly environment and Reservoir extensions:

```csharp
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Mississippi.Reservoir.Client;
```

Insert this call in your existing Reservoir configuration. Here `builder` is your `WebAssemblyHostBuilder` and `reservoir` is its `IReservoirBuilder`:

```csharp
reservoir.AddReservoirDevTools(options =>
{
    options.Enablement = builder.HostEnvironment.IsDevelopment()
        ? ReservoirDevToolsEnablement.Always
        : ReservoirDevToolsEnablement.Off;
    options.Name = "Spring Sample";
});
```

This selects enablement from the actual WebAssembly environment while registering the integration in both environments. In a full Mississippi client, place the call inside `client.Reservoir(...)`.

`DevelopmentOnly` uses an injected `IHostEnvironment`. WebAssembly exposes `IWebAssemblyHostEnvironment`, so the explicit choice above avoids assuming that the two services are interchangeable. See the [option reference](../reference/devtools.md) for the exact modes.

### 2. Add the Root Initializer

In `App.razor` or the root layout, import the client namespace and render one initializer:

```razor
@using Mississippi.Reservoir.Client

<ReservoirDevToolsInitializerComponent/>
```

The component calls `ReduxDevToolsService.Initialize()` after its first render, when the rendering context is available. It stops the service when disposed. Keep the initializer at the application root so navigation between pages retains the integration. Initialization subscribes to store events; a later ordinary dispatched action triggers the connection attempt.

[Spring's App.razor](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client/App.razor) demonstrates the root component placement.

### 3. Select the Payload You Want to Inspect

Use `ActionSanitizer` and `StateSanitizer` to return the explicit payload the extension should receive. Return a sanitized object when redacting fields: a `null` sanitizer result selects the normal serialization fallback.

Keep enough action identity and non-sensitive context to connect an action with its state transition. If you intend to restore snapshots, retain every field value required to reproduce the intended feature state. Successful deserialization alone is insufficient: omitted optional properties can receive defaults even in strict mode. Verify a semantic round trip before restoring sanitized payloads; use redacted, incomplete payloads for inspection only.

## Verify the result

Build the client and run it in Development with the extension installed:

1. After the root has rendered, dispatch a local action, such as an account-selection action from [Add a feature](./create-feature.md).
2. Open browser developer tools, select the Redux DevTools instance named `Spring Sample`, and confirm the registered feature state appears.
3. Inspect the action and resulting state, including any sanitizer output.
4. When exploring jump/reset/rollback operations, observe the local state change separately from server projections and external effects.

For repository validation, run:

```powershell
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Reservoir.Client.L0Tests -SkipMutation
pwsh ./build.ps1 -SkipMississippi -Configuration Release
```

Require passing executed tests and a zero-warning build. The client tests cover registration, initialization, interop, sanitizers, restoration, and built-in client behavior. The browser-extension inspection above verifies your application and browser setup.

## Source

- [DevTools registration](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Client/ReservoirDevToolsRegistrations.cs).
- [Initializer component](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Client/ReservoirDevToolsInitializerComponent.razor).
- [DevTools service](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Client/ReduxDevToolsService.cs).
- [Matching WebAssembly host implementation](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/WebAssembly/WebAssembly/src/Hosting/WebAssemblyHostBuilder.cs).

## Summary

Register DevTools with an explicit environment choice, initialize it after rendering, and inspect deliberate action/state payloads. Use those observations to verify local transitions rather than infer behavior from the UI alone.

## Next Steps

- [DevTools reference](../reference/devtools.md) for options and restoration behavior.
- [Test a feature and effect](./test-feature.md) for executable assertions.
