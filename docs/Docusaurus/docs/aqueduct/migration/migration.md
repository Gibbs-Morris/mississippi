---
id: aqueduct-runtime-composition-migration
title: "Migrate Aqueduct Runtime Composition (Next: 6a2987de → 10cb90b1)"
sidebar_label: Runtime Migration
sidebar_position: 1
description: Move an Orleans host to the Next Aqueduct runtime composition API while preserving stream identities.
---

# Migrate Aqueduct Runtime Composition (Next: 6a2987de → 10cb90b1)

This guide maps the source API shape verified at revision `6a2987de2d559f9b5d6c1f4a11e0e33801cee4e9` to the target
two-setting builder API verified at revision `10cb90b1b53d6839e016c88f96c793154b86504d`. The source uses the
silo-level `UseAqueduct(...)` and `AqueductSiloOptions`; the target uses nested `runtime.AddAqueduct(...)`. These
revisions identify API shapes in the repository and are not NuGet release numbers.

## Overview

The runtime API change leaves the default stream identity values and persisted contracts unchanged. A custom PubSub
storage name is handled separately below; this migration does not rename or translate it.

## Who should read this

- Runtime host maintainers whose startup code uses `UseAqueduct(...)` or `AqueductSiloOptions`.
- Gateway host maintainers whose gateway options were previously supplied by a colocated `UseAqueduct(...)` callback.
- Teams moving a source checkout or package set to the `Next` runtime composition contract.

The runtime builder now owns the provider and server namespace. Gateway-side `AqueductOptions` remains the configuration
surface for the broadcast namespace and gateway heartbeat; `DeadServerTimeoutMultiplier` currently has no production
consumer. When one host application contains both roles, move the shared values as shown below rather than assuming the
runtime callback configures the gateway.

## Compatibility summary

- The runtime change is breaking. The old silo-level entry point and host-capturing options type are removed; no
  compatibility wrapper is provided.
- Mixed versions of the runtime and gateway composition are not verified. Keep participating hosts on one compatible
  source/package set and use the coordinated procedure below.
- The API move does not change event, snapshot, or message serialization types. Provider and server-namespace identity
  still has to remain consistent across participating runtimes and gateways; gateway broadcast identity remains
  consistent among gateways.
- The target memory-stream helper uses the `PubSubStore` convention. It has no parameter for an arbitrary old storage
  name, so a custom old PubSub storage identity needs a separate data and deployment decision.

## Breaking changes

The target runtime builder exposes two runtime stream identity settings:

- `StreamProviderName`
- `ServerStreamNamespace`

Runtime heartbeat and dead-server timeout settings are not part of this builder. If the legacy callback set
`HeartbeatIntervalMinutes`, move that value to the gateway configuration that consumes it; `DeadServerTimeoutMultiplier`
currently has no production consumer. `AllClientsStreamNamespace` remains a gateway setting for broadcasts and is not set
or validated by the runtime builder.

The target `UseMemoryStreams()` and `UseMemoryStreams("ProviderName")` methods configure memory streams and the
`PubSubStore` convention. The old provider-and-storage-name overload is not mapped automatically.

## Required preparation

1. Record the current `StreamProviderName` and `ServerStreamNamespace` values for every participating runtime and
   gateway, and record `AllClientsStreamNamespace` for every participating gateway.
2. Record provider-owned stream or storage identities used by the host, including `PubSubStore` when memory streams are
   selected. Aqueduct connection and group membership state is volatile in memory; preserve provider-owned persisted
   stream or subscription metadata separately where applicable.
3. Find every runtime and gateway startup caller. Prepare one target `UseMississippi(...)` callback per runtime, and
   configure each gateway with matching provider and server namespace values plus its broadcast and heartbeat settings.
4. Back up deployment manifests, runtime configuration, and provider-owned persisted state or stream metadata according
   to the provider's normal backup procedure. This API cutover does not define a backup or data-conversion format.
5. Plan a coordinated maintenance window. Mixed runtime versions and mixed stream identities have not been established
   as a supported rolling deployment path.

## Upgrade sequence

1. Stop traffic that can publish or consume Aqueduct messages, and stop all participating runtime and gateway hosts.
2. Deploy the target source/package set to every participating host. Keep the recorded stream and storage identities
   unchanged.
3. Start the runtime hosts and confirm that each one completes the target `UseMississippi(...)` composition without a
   `BuilderValidationException`.
4. Start the gateway hosts with their explicitly configured gateway-side options and confirm that their provider and
   server namespace settings match the recorded runtime identities and that their broadcast namespace matches the other
   gateways.
5. Re-enable traffic only after all participating hosts report healthy through their normal Orleans and application
   checks.

## Code and configuration changes

Replace the old runtime registration:

```csharp
siloBuilder.UseAqueduct(options =>
{
    options.StreamProviderName = "StreamProvider";
});
```

With the target nested composition:

```csharp
siloBuilder.UseMississippi(runtime =>
{
    runtime.AddAqueduct(aqueduct =>
        aqueduct.StreamProviderName = "StreamProvider");
    runtime.ApplyToSilo(siloBuilder);
});
```

### Move colocated gateway options explicitly

The legacy silo callback also copied gateway values into the shared `AqueductOptions`. In a colocated host,
`builder.Services` and the silo share the same service collection, so keep the nondefault provider and server namespace
identical across both roles and move the broadcast namespace and heartbeat interval to the gateway's
`AddAqueduct<THub>(...)` callback. `NotificationsHub` below represents the application's existing hub.

Before:

```csharp
builder.UseOrleans(siloBuilder =>
{
    siloBuilder.UseAqueduct(options =>
    {
        options.StreamProviderName = "orders-provider";
        options.ServerStreamNamespace = "orders-server";
        options.AllClientsStreamNamespace = "orders-broadcast";
        options.HeartbeatIntervalMinutes = 5;
    });
});

builder.Services.AddAqueduct<NotificationsHub>();
```

After:

```csharp
builder.UseOrleans(siloBuilder =>
{
    siloBuilder.UseMississippi(runtime =>
    {
        runtime.AddAqueduct(aqueduct =>
        {
            aqueduct.StreamProviderName = "orders-provider";
            aqueduct.ServerStreamNamespace = "orders-server";
        });
        runtime.ApplyToSilo(siloBuilder);
    });
});

builder.Services.AddAqueduct<NotificationsHub>(options =>
{
    options.StreamProviderName = "orders-provider";
    options.ServerStreamNamespace = "orders-server";
    options.AllClientsStreamNamespace = "orders-broadcast";
    options.HeartbeatIntervalMinutes = 5;
});
```

The runtime callback does not set or validate `AllClientsStreamNamespace` or `HeartbeatIntervalMinutes`; the gateway
callback configures them. `DeadServerTimeoutMultiplier` was copied by the legacy silo helper but has no production
consumer in the current implementation, so do not add it to the runtime migration as a required setting.

For development or tests, move the memory-stream call into the nested builder:

```csharp
runtime.AddAqueduct(aqueduct => aqueduct.UseMemoryStreams());
```

When a non-default development provider name is required, use:

```csharp
runtime.AddAqueduct(aqueduct => aqueduct.UseMemoryStreams("ProviderName"));
```

The target runtime configuration overload reads these option-property keys from the supplied `IConfiguration`:

| Key | Target setting |
| --- | --- |
| `StreamProviderName` | `AqueductBuilder.StreamProviderName` |
| `ServerStreamNamespace` | `AqueductBuilder.ServerStreamNamespace` |

Missing keys keep their runtime defaults. `AllClientsStreamNamespace` remains a gateway setting and is not read by this
configuration overload. Do not carry runtime heartbeat or dead-server timeout keys into this callback;
`HeartbeatIntervalMinutes` remains a gateway setting and `DeadServerTimeoutMultiplier` currently has no production
consumer.

For a colocated gateway, use `services.AddAqueduct<THub>(options => ...)` or
`services.Configure<AqueductOptions>(...)` on the gateway service collection for `AllClientsStreamNamespace`,
`HeartbeatIntervalMinutes`, and any other gateway-owned options. Keep the provider and server namespace values in both
role-specific configurations consistent.

`runtime.ApplyToSilo(siloBuilder)` is the recommended explicit native-configuration hook. If it is omitted, the
terminal `UseMississippi(...)` operation applies queued native configuration automatically.

## Data, state, and serialization implications

This is an API and composition migration. It does not rename events, snapshots, Orleans grain contracts, or serialized
message types. Preserve the exact provider and server namespace strings across runtimes and gateways, and preserve the
gateway broadcast namespace among gateways, so old and new deployments address the same stream identities after the
coordinated change.

The target memory path registers `PubSubStore`. It does not copy, rename, or translate data from a custom old PubSub
storage name. Aqueduct connection and group membership state is in memory rather than a persisted storage contract.
Keep any provider-owned stream or subscription metadata until the separate storage decision and its verification are
complete.

No mixed-version wire, storage, or stream-identity behavior is claimed by this guide.

## Validation

Run the repository's selected SDK from the checkout root and verify the target runtime tests and host startup:

```powershell
dotnet --version
dotnet restore mississippi.slnx --use-lock-file
dotnet build tests/Aqueduct.Runtime.L0Tests/Aqueduct.Runtime.L0Tests.csproj -c Release --no-incremental --no-restore -warnaserror
dotnet test --project tests/Aqueduct.Runtime.L0Tests/Aqueduct.Runtime.L0Tests.csproj --configuration Release --no-build --no-restore
```

The build must finish without warnings or errors, and the test run must have zero failed or skipped tests. Run the
affected runtime-host and gateway-host checks as well. Confirm that every runtime host uses one `AddAqueduct(...)` call,
that the provider and server namespace are nonempty and consistent across runtimes and gateways, that participating
gateways agree on the broadcast namespace, and that startup reports no `MSB201`, `MSB202`, `MSB206`, or `MSB207`
diagnostics.

## Rollback

Rollback is possible before changing stream or storage identities: stop all participating hosts, redeploy the previous
source/package set, restore the recorded configuration, and start the complete previous set together. Keep the provider
data and deployment backups until the previous hosts have passed their normal health checks.

Do not roll back only one host or leave old and new stream identities active together. If a deployment changed a stream
or storage identity, this guide provides no automatic data rollback; restore the original identity and use the provider's
separate recovery procedure before re-enabling traffic.

## Related release notes and reference

No release number is assigned to this repository cutover. Use these related pages for the supported contracts and task
details:

- [How To Configure Aqueduct Runtime Composition](../how-to/how-to.md)
- [Aqueduct Reference](../reference/reference.md)
- [Runtime Composition](../../reference/runtime-composition.md)
- [Aqueduct Operations](../operations/operations.md)
