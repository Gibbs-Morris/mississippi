---
title: How to configure projection replication sinks
description: Configure the shipped latest-state replica-sink flow for a Domain Modeling projection and the Cosmos provider path.
sidebar_position: 2
---

# How to configure projection replication sinks

This guide configures the supported replica-sink onboarding flow for the current slice: declare contract identity, bind a projection, register runtime discovery, and add the Cosmos provider path. It documents the shipped single-instance, latest-state setup only.

## Prerequisites

- Your problem is projection-derived external read-model replication, not aggregate or reducer behavior.
- The host will run as a single instance for this slice.
- A keyed `CosmosClient` registration already exists for the client key you plan to pass to `AddCosmosReplicaSink(...)`.
- You are using `ReplicaWriteMode.LatestState`, or you are omitting `WriteMode` and accepting the default.

## Steps

### 1. Declare a stable replica contract identity

Use `ReplicaContractNameAttribute` on the contract you want to publish externally.

```csharp
[ReplicaContractName("TestApp", "Orders", "MappedReplica")]
internal sealed class MappedReplicaContract
{
    public string Id { get; set; } = string.Empty;
}
```

### 2. Bind the projection to a named sink and target

Use `ProjectionReplicationAttribute` on the projection type. The `sink` value is the runtime registration key. The `targetName` is the provider-neutral destination name.

```csharp
[ProjectionReplication("bootstrap-mapped", "orders-read", typeof(MappedReplicaContract))]
internal sealed class MappedReplicaProjection
{
    public string Id { get; set; } = string.Empty;
}
```

### 3. Register the mapper for the mapped contract

Mapped contracts are the default path, so the runtime expects an `IMapper<TProjection, TContract>` registration.

```csharp
internal sealed class MappedReplicaProjectionToContractMapper : IMapper<MappedReplicaProjection, MappedReplicaContract>
{
    public MappedReplicaContract Map(MappedReplicaProjection input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new()
        {
            Id = input.Id,
        };
    }
}

services.AddMapper<MappedReplicaProjection, MappedReplicaContract, MappedReplicaProjectionToContractMapper>();
```

If you intentionally want direct projection replication instead, omit `ContractType`, put `ReplicaContractNameAttribute` on the projection itself, and set `IsDirectProjectionReplicationEnabled = true`.

```csharp
[ReplicaContractName("TestApp", "Orders", "DirectReplica")]
[ProjectionReplication("search", "orders-direct", IsDirectProjectionReplicationEnabled = true)]
internal sealed class SampleDirectReplicaProjection
{
    public string Value { get; set; } = string.Empty;
}
```

### 4. Register the runtime shell and scan your projection assembly

Add the runtime services, then scan the assembly that contains your annotated projection types.

```csharp
services.AddReplicaSinks();
services.ScanReplicaSinkAssemblies(typeof(MappedReplicaProjection).Assembly);
```

### 5. Add the Cosmos provider registration

Register the provider with `AddCosmosReplicaSink(...)`. The sink key you register must match the `sink` value used by your projection binding.

```csharp
services.AddCosmosReplicaSink(
    "bootstrap-mapped",
    "orders-client",
    options =>
    {
        options.DatabaseId = "orders-db";
        options.ContainerId = "orders-container";
        options.ProvisioningMode = ReplicaProvisioningMode.CreateIfMissing;
    });
```

If you do not override them, Cosmos defaults to `DatabaseId = "mississippi"`, `ContainerId = "replica-sinks"`, `QueryBatchSize = 100`, and `ProvisioningMode = ValidateOnly`.

## Verification

- The host starts without replica-sink validation failures.
- Invalid bindings fail fast with startup diagnostics instead of silently deferring the error.
- If you use `ProvisioningMode = CreateIfMissing`, the provider can provision the target during startup. If you keep the default `ValidateOnly`, the target must already exist.
- If you choose direct projection replication, the projection declares both `ReplicaContractNameAttribute` and `IsDirectProjectionReplicationEnabled = true`; otherwise startup validation rejects the binding.

## Next Steps

- Read [Projection Replication Sinks](../concepts/projection-replication-sinks.md) for the model, guarantees, and limits.
- Use [Projection replication sinks reference](../reference/projection-replication-sinks.md) for the public packages, defaults, operator APIs, and carry-forward limitations.
