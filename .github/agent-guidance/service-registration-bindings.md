# Service-registration source bindings

Use these bindings with [register-dotnet-services](../../.agents/skills/register-dotnet-services/SKILL.md).
The [registration rules](../instructions/service-registration.instructions.md) and
[keyed-service rules](../instructions/keyed-services.instructions.md) retain local
requirements. Read the relevant current sources before choosing an API or key.

## Registration and configuration

- Public `{Feature}Registrations` and `Add{Feature}` entrypoints compose child
  wiring at the visibility required by policy. [InletSiloRegistrations](../../src/Inlet.Runtime/InletSiloRegistrations.cs)
  shows a service-collection boundary; [RuntimeHostingRegistrations](../../src/Hosting.Runtime/RuntimeHostingRegistrations.cs)
  and [RuntimeBuilder](../../src/Hosting.Runtime/RuntimeBuilder.cs) own runtime
  attachment and validation. Do not mutate a captured host collection inside
  `UseMississippi` or configure a closed builder scope.
- [BrookStorageProviderRegistrations](../../src/Brooks.Runtime.Storage.Cosmos/BrookStorageProviderRegistrations.cs)
  and [SnapshotStorageProviderRegistrations](../../src/Tributary.Runtime.Storage.Cosmos/SnapshotStorageProviderRegistrations.cs)
  expose `IRuntimeBuilder` extensions. Their action callbacks take
  `CosmosBrookStorageBuilder` or `CosmosSnapshotStorageBuilder`; configuration and
  connection-string overloads converge on staged registration. They configure
  options, add startup validation, register lazy client factories when owning
  connections, and defer resource creation to hosted initializers.
- Read [BrookStorageOptions](../../src/Brooks.Runtime.Storage.Cosmos/BrookStorageOptions.cs),
  [SnapshotStorageOptions](../../src/Tributary.Runtime.Storage.Cosmos/SnapshotStorageOptions.cs),
  their validators, and nested builders for defaults, binding, and validation.
  Source behavior is evidence, not permission to copy an existing exception to
  constructor-injection policy into new ordinary services.
- For event-sourced domains, preserve the [domain registration rules](../instructions/domain-modeling.instructions.md#registration):
  event types, command handlers, reducers, then snapshot converters. The
  [aggregate registration generator](../../src/Inlet.Runtime.Generators/AggregateSiloRegistrationGenerator.cs)
  emits that sequence; use generated registration where supported.

## Key ownership and forwarding

Read [BrookCosmosDefaults](../../src/Brooks.Runtime.Storage.Cosmos/BrookCosmosDefaults.cs)
and [SnapshotCosmosDefaults](../../src/Tributary.Runtime.Storage.Cosmos/SnapshotCosmosDefaults.cs)
for exact client/container keys and storage defaults. `CosmosClientServiceKey` is
configurable in each provider; container keys remain module-owned.
[CosmosRepository](../../src/Brooks.Runtime.Storage.Cosmos/Storage/CosmosRepository.cs),
[SnapshotContainerOperations](../../src/Tributary.Runtime.Storage.Cosmos/Storage/SnapshotContainerOperations.cs),
and [BlobDistributedLockManager](../../src/Brooks.Runtime.Storage.Cosmos/Locking/BlobDistributedLockManager.cs)
show keyed constructor contracts. Document these requirements in registration
remarks. [Spring runtime](../../samples/Spring/Spring.Runtime/Program.cs) forwards
its host Blob client to `BlobLockingServiceKey` and aliases its host Cosmos client
to `spring-cosmos`, then configures both providers to that shared client key.
Sharing there is intentional; independent host clients need separate forwarding.

The [Brooks registration tests](../../tests/Brooks.Runtime.Storage.Cosmos.L0Tests/BrookStorageProviderRegistrationsTests.cs)
and [snapshot registration tests](../../tests/Tributary.Runtime.Storage.Cosmos.L0Tests/SnapshotStorageProviderRegistrationsTests.cs)
provide graph and validation examples. Use identity assertions when testing
aliases or independent keys; report unexecuted startup/cloud behavior separately.
