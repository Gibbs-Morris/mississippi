---
id: aqueduct-getting-started
title: Aqueduct Runtime Getting Started
sidebar_label: Getting Started
sidebar_position: 1
description: Configure Aqueduct in an Orleans runtime host through the canonical Mississippi composition callback.
---

# Aqueduct Runtime Getting Started

## Overview

Use this page to add the Aqueduct SignalR backplane to an Orleans runtime host through the canonical
`UseMississippi(...)` composition path.

## What You Will Achieve

By the end of this page, you will know where `runtime.AddAqueduct(...)` belongs, how to choose the local memory
stream path or an existing Orleans provider, and where to look when composition validation fails.

## Prerequisites

- Reference `Mississippi.Sdk.Runtime`, or reference both `Mississippi.Aqueduct.Runtime` and
  `Mississippi.Hosting.Runtime` from the Orleans host.
- Decide whether the host will use the development memory stream setup or a stream provider configured by the host.
- If the real task is end-to-end projection delivery, start with [Inlet](../../inlet/index.md) instead.

## Install

Install `Mississippi.Sdk.Runtime` for the complete runtime composition surface. If the application deliberately uses
focused packages, `Mississippi.Aqueduct.Runtime` supplies `AddAqueduct(...)` and `Mississippi.Hosting.Runtime` supplies
the `UseMississippi(...)` terminal extension.

## Configure The Host

## First Verified Success

For local development or tests, compose Aqueduct with its memory stream registrations inside the runtime terminal
callback:

```csharp
using Mississippi.Aqueduct.Runtime;
using Mississippi.Hosting.Runtime;


builder.UseOrleans(siloBuilder =>
{
    siloBuilder.UseMississippi(runtime =>
    {
        runtime.AddAqueduct(aqueduct => aqueduct.UseMemoryStreams());
        runtime.ApplyToSilo(siloBuilder);
    });
});
```

`UseMemoryStreams()` selects the final `StreamProviderName` and registers Orleans memory streams together with the
`PubSubStore` grain storage convention. It is the development and test path; a production host should configure its
stream provider separately and select its name in the nested builder.

The [Spring runtime host](../../samples/spring-sample/concepts/host-applications.md) provides a verified existing
provider example. Its AppHost supplies `StreamProvider`, and the runtime selects that name with
`aqueduct.StreamProviderName = "StreamProvider"`.

## Choose The Runtime Provider

- Use `aqueduct.UseMemoryStreams()` when the host needs the built-in development stream registrations.
- Use `aqueduct.UseMemoryStreams("ProviderName")` when development or test code needs a non-default provider name.
- Set `aqueduct.StreamProviderName` when the host already owns an Orleans provider, then keep that name aligned with
  the provider registration.

The builder also has convenience overloads for an `IConfiguration` section and for explicit settings. See the
[Aqueduct Reference](../reference/reference.md) for their exact parameter and key contracts.

## Verify It Works

Keep the complete Aqueduct callback inside one `UseMississippi(...)` call. At composition time, nonempty stream names
and positive timing settings are required, and a second `AddAqueduct(...)` call for the same runtime is rejected.

For the staged callback lifecycle, advanced `ConfigureSilo(...)` plumbing, and stable diagnostic codes, continue to
[How To Configure Aqueduct Runtime Composition](../how-to/how-to.md) and the [Aqueduct Reference](../reference/reference.md).

## What Happened

`UseMississippi(...)` attached one runtime composition, and the nested `AqueductBuilder` selected the stream provider,
stream namespaces, and timing values. With `UseMemoryStreams(...)`, the same composition also added the memory stream
provider and `PubSubStore` registrations.

## Summary

Aqueduct runtime setup is a nested builder operation inside the Orleans host's canonical `UseMississippi(...)`
callback. Choose memory streams for local development and tests, or select a host-owned provider by name.

## Next Steps

- Follow [How To Configure Aqueduct Runtime Composition](../how-to/how-to.md) for the complete task sequence.
- Read [Aqueduct Concepts](../concepts/concepts.md) for the nested scope and snapshot model.
- Use [Aqueduct Reference](../reference/reference.md) for exact options and diagnostic codes.
