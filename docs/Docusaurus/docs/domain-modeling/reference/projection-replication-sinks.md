---
title: Projection replication sinks reference
description: Public packages, APIs, defaults, operator contracts, and current slice limits for Domain Modeling replica sinks.
sidebar_position: 2
---

# Projection replication sinks reference

Use this page to look up the current replica-sink package surface, onboarding APIs, defaults, and explicit non-guarantees for the shipped Domain Modeling slice.

## Packages

| Package | Purpose |
| --- | --- |
| `Mississippi.DomainModeling.ReplicaSinks.Abstractions` | Projection binding metadata and stable external contract identity. |
| `Mississippi.DomainModeling.ReplicaSinks.Runtime` | Discovery, startup validation, latest-state coordination, dead-letter paging, and controlled re-drive. |
| `Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Abstractions` | Provider-facing registration, provisioning, inspection, and delivery-state contracts. |
| `Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Bootstrap` | Lightweight provider path used for runnable onboarding proof and runtime tests. |
| `Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos` | Concrete Cosmos-backed provider path for the current documentation slice. |

## Binding contracts

| Symbol | Purpose | Key facts |
| --- | --- | --- |
| `ProjectionReplicationAttribute` | Declares a projection-to-sink binding. | `sink` is the named registration key, `targetName` is provider-neutral, mapped contracts are the default path, and `WriteMode` defaults to `LatestState`. |
| `ReplicaContractNameAttribute` | Declares stable external identity. | The runtime composes identity as `AppName.ModuleName.Name.V{Version}`. |
| `ReplicaWriteMode` | Chooses the requested persistence strategy. | `LatestState` is the runnable slice. `History` is exposed publicly but rejected during startup validation. |
| `ReplicaProvisioningMode` | Controls provider target provisioning. | `ValidateOnly` is the default. `CreateIfMissing` opts into startup-time creation. |

## Registration APIs

| API | Purpose | Notes |
| --- | --- | --- |
| `AddReplicaSinks()` | Adds discovery, startup validation, runtime execution, and operator services. | Also wires the hosted services that validate bindings and run the execution pump. |
| `ScanReplicaSinkAssemblies(params Assembly[] assemblies)` | Discovers `ProjectionReplicationAttribute` bindings. | A generic `ScanReplicaSinkAssemblies<TMarker>()` overload scans the assembly that contains `TMarker`. |
| `AddCosmosReplicaSink(string sinkKey, string clientKey, Action<CosmosReplicaSinkOptions> configure)` | Registers the Cosmos provider using named options. | Requires a keyed `CosmosClient` registration for `clientKey`. |
| `AddCosmosReplicaSink(string sinkKey, string clientKey, IConfiguration configurationSection)` | Registers the Cosmos provider from configuration binding. | `clientKey` still comes from the method parameter, not the configuration section. |
| `IReplicaSinkProvider` | Provider abstraction that combines provisioning, writing, and inspection. | `Format` is informational; runtime resolution is by named sink key. |

## Cosmos provider defaults

| Setting | Default | Notes |
| --- | --- | --- |
| `ProvisioningMode` | `ValidateOnly` | Startup validates an existing target unless you opt into `CreateIfMissing`. |
| `DatabaseId` | `"mississippi"` | Set explicitly when you want a different database. |
| `ContainerId` | `"replica-sinks"` | Set explicitly when you want a different container. |
| `QueryBatchSize` | `100` | Validation requires a positive value. |

## Operator surface

| Symbol | Purpose | Notes |
| --- | --- | --- |
| `ReplicaSinkOperatorAccessLevel` | Describes caller permissions. | `Summary` can read redacted dead-letter metadata, `Detail` can request failure summaries, and `Admin` can re-drive dead letters. |
| `ReplicaSinkOperatorContext` | Carries the stable operator identity and access level. | The runtime uses `ActorId` for audit trails. |
| `ReplicaSinkDeadLetterQuery` | Requests a bounded dead-letter page. | Includes requested `PageSize`, optional continuation token, and optional failure-summary request. |
| `IReplicaSinkRuntimeOperator.ReadDeadLettersAsync(...)` | Reads a bounded page of dead-letter records. | The runtime caps `PageSize` at `ReplicaSinkRuntimeOptions.MaxDeadLetterPageSize`, which defaults to `50`. |
| `IReplicaSinkRuntimeOperator.ReDriveAsync(...)` | Issues a controlled dead-letter re-drive. | Requires admin access and re-reads current eligible source state instead of replaying the stored dead-letter payload. |

## Startup diagnostics

| ID | Meaning |
| --- | --- |
| `RS0001` | Missing sink registration |
| `RS0002` | Missing replica contract identity |
| `RS0003` | Missing mapper |
| `RS0004` | Duplicate logical binding |
| `RS0005` | Direct replication requires explicit opt-in |
| `RS0006` | `History` write mode is unsupported in the runnable slice |
| `RS0007` | Overlapping physical target registration |
| `RS0008` | Ambiguous sink registration multiplicity or missing runtime provider handle |

## Current slice limitations

- Replica-sink runtime execution is accepted only for single-instance deployment.
- The runnable slice is latest-state only.
- The operator surface is bounded runtime/admin functionality, not a broader gateway or UI surface.
- The accepted Cosmos evidence is bounded to the emulator-backed calibration harness and targeted L0/L2 tests.
- Targeted mutation is blocked/not proven for this slice because the per-project selector issue exhausted the bounded tooling-attempt cap.

## Quality evidence

The accepted Cosmos calibration evidence covers three emulator-backed scenarios:

- single-sink replay backlog
- two-sink fan-out replay backlog
- replay backlog followed by live writes

In the accepted baseline and confirmation run, those scenarios finished with zero retries and zero dead letters. Treat that as provider-baseline evidence for this slice, not as a blanket persistence conformance statement.

The accepted quality posture is clean build, cleanup, unit-test success, and strengthened deterministic coverage. Do not describe this slice as having a proven targeted mutation score.

## Failure behavior and residual risks

- Re-drive is fail-closed rather than atomic. If `NotifyLiveAsync(...)` succeeds and dead-letter clearing cannot be persisted afterward, the runtime quarantines the sink and preserves the dead-letter state.
- Failed admin re-drive attempts are not currently audited when `NotifyLiveAsync(...)` throws.
- The bootstrap provider still carries a same-target multi-lane fencing concern.
- Dead-letter paging uses mutable offset continuation, so inserts or clears between page reads can cause skips or duplicates.
- Park, throttle, and quarantine state is in-memory and restart-sensitive.
- Startup validation and Cosmos initialization are fail-fast, so one bad sink can block the host.

## See also

- [Projection Replication Sinks](../concepts/projection-replication-sinks.md)
- [How to configure projection replication sinks](../how-to/configure-projection-replication-sinks.md)
- [Domain Modeling overview](../index.md)
- [ADR-0002 background](../../adr/0002-named-versioned-replica-sinks-use-durable-out-of-band-delivery.md)
