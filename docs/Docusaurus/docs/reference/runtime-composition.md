---
title: Runtime Composition
description: Reference RuntimeBuilder, terminal Orleans attachment, native configuration, and runtime diagnostics.
sidebar_position: 41
---

# Runtime Composition

`ISiloBuilder.UseMississippi(...)` configures, validates, and attaches a `RuntimeBuilder` to an Orleans host once.

## Applies to

- `Mississippi.Hosting.Runtime`, included by `Mississippi.Sdk.Runtime`
- `Mississippi.Hosting.Runtime.Abstractions` for runtime extension contracts and native integration diagnostic codes
- `Mississippi.Hosting.Abstractions` for the shared builder and diagnostic contracts
- `Mississippi.Brooks.Runtime` for the unified event-sourcing registration

## Contract

| API | Behavior |
| --- | --- |
| `ISiloBuilder.UseMississippi(Action<RuntimeBuilder>)` | Creates a staged runtime scope, invokes configuration, validates it, applies pending native configuration, and commits service descriptors |
| `IRuntimeBuilder.ConfigureSilo(Action<ISiloBuilder>)` | Queues synchronous native configuration in registration order |
| `IRuntimeBuilder.ApplyToSilo(ISiloBuilder)` | Applies queued callbacks against staged services for the owning host, without publishing them to the host yet |
| `IMississippiBuilder.Services` | Provides advanced access to the staged runtime registrations |
| `IMississippiBuilder.Validate()` | Returns structured attachment-readiness diagnostics without changing registrations |
| `IRuntimeBuilder.AddEventSourcing(Action<BrookProviderOptions>?)` | Registers Brooks factories, stream identity support, and options together |

`RuntimeBuilder` implements both `IRuntimeBuilder` and `IMississippiBuilder`. Runtime subsystem extensions can depend on the role contract without referencing the hosting implementation.

## Defaults and constraints

Empty runtime roots are valid. No placeholder aggregate, saga, or projection is required for infrastructure-first setup.

`ApplyToSilo(...)` is the recommended explicit integration hook at the end of configuration. It is optional: terminal attachment applies pending native callbacks automatically if the hook was omitted. It is not a second attachment API. Queue native configuration before explicit application; repeated application and configuration after application are rejected.

Brooks uses `BrookStreamingDefaults.OrleansStreamProviderName` unless configured otherwise. The host still supplies Orleans stream providers and storage. Repeated `AddEventSourcing(...)` calls keep one canonical grain factory and compose option callbacks in order. A single existing unkeyed concrete singleton registration is preserved, including its factory callback and position. Duplicate or non-singleton concrete registrations are replaced by one default singleton. Public and internal grain-factory mappings remain authoritative and resolve the same concrete instance across service scopes. Existing custom stream-ID factories are preserved.

## Behavior

The runtime starts with a copy of the host's service descriptors. Native callbacks receive an `ISiloBuilder` adapter with that staged collection and the original host `Configuration`. Their registrations become visible to the host only after terminal composition succeeds.

Inside composition, register services through `runtime.Services` or the staged silo passed to `ConfigureSilo(...)`. Configure the outer host's services before `UseMississippi(...)`. Direct changes to the captured host collection during either callback reject attachment with `MSB004` instead of being overwritten. Those direct changes remain on the host, the staged graph closes without attaching, and a fresh callback can retry. Application callback mutations are detected before automatic native application begins.

The host services must remain writable until composition finishes. A read-only host is rejected with `MSB005` before configuration or publication. Freezing the captured host inside either callback still closes the staged scope, and cleanup preserves an exception thrown by the callback. A frozen host requires a fresh host instance; subsequent attempts report `MSB005` rather than duplicate attachment.

The staged graph retains the runtime attachment reservation and its nonterminal scope identity. Calling `UseMississippi(...)` again through a native callback or a wrapper over its staged services is rejected as duplicate attachment, even after clearing staged registrations; successful publication keeps one runtime attachment marker.

The original host's attachment identity is also tracked independently of its descriptors. Clearing original host services cannot permit recursive attachment through a captured silo or another wrapper over those services, and cannot make a successfully attached host reusable. Failed composition releases the independent identity; successful attachment remains terminal for that host collection.

If application or native configuration throws, the staged scope closes, its changes are discarded, and the independent attachment reservation is released. Cleanup removes all runtime attachment descriptors from a writable host, including replaced, duplicate, and keyed copies, so a fresh terminal callback can retry. Catching a native callback exception inside application configuration does not make that partially configured scope valid: terminal validation still rejects it.

If the host collection throws while staged services are being published, composition restores the original host descriptors and rethrows the publication error. If restoration also fails, an `AggregateException` reports both errors. Create a fresh host in that case because its registrations may be incomplete.

Out-of-memory, access-violation, and stack-overflow faults propagate directly. Publication restoration and error aggregation are skipped for those fatal runtime faults.

Captured runtime builders and native adapters cannot modify the staged service collection after the terminal scope closes. Registration callbacks are synchronous; asynchronous initialization belongs in hosted services or Orleans lifecycle participants.

Staging covers service descriptors. The forwarded configuration and existing service instances are shared objects; their mutations and external callback side effects are not rolled back. Composition does not build a service provider, start a silo, or validate network connectivity.

## Failure behavior

`BuilderValidationException.Diagnostics` contains stable codes, messages, and remediation. Shared codes use `BuilderDiagnosticCodes`; native integration codes use `RuntimeBuilderDiagnosticCodes`.

| Code | Failure | Remediation |
| --- | --- | --- |
| `MSB001` | Duplicate or recursive runtime attachment | Use one runtime terminal callback for the host |
| `MSB002` | The runtime builder has already attached | Configure it inside the terminal callback |
| `MSB003` | The scope closed without attaching | Retry with a fresh scope |
| `MSB004` | Direct changes to captured host services during composition | Use `runtime.Services` or the staged native callback, or configure the host before composition |
| `MSB005` | Host services are read-only | Compose before freezing the host services; create a fresh host if they are already frozen |
| `MSB101` | The supplied silo has different services or configuration from the owning host | Pass the owning silo to `ApplyToSilo(...)` |
| `MSB102` | Native configuration was applied twice | Apply explicitly once or rely on terminal automatic application |
| `MSB103` | A native callback failed, leaving an incomplete scope | Correct the callback and retry with a fresh scope |
| `MSB104` | Native configuration was queued after application | Move all `ConfigureSilo(...)` calls before `ApplyToSilo(...)` |

Null host and callback arguments produce `ArgumentNullException`. Application callback exceptions propagate unchanged; validation prevents a caught native failure from being attached.

## Example

This excerpt follows Spring's runtime setup after its host-owned stream provider has been configured. The remaining domain and storage registrations are omitted here.

```csharp
builder.UseOrleans(silo => silo.UseMississippi(runtime =>
{
    runtime.AddEventSourcing(options =>
        options.OrleansStreamProviderName = "StreamProvider");
    runtime.ConfigureSilo(configuredSilo => configuredSilo.AddActivityPropagation());
    runtime.ApplyToSilo(silo);
}));
```

## Compatibility

The runtime builder replaces the separate `IServiceCollection.AddEventSourcingByService()`, `ISiloBuilder.AddEventSourcing(...)`, and `HostApplicationBuilder.AddEventSourcing(...)` entrypoints. Use the single runtime builder extension inside `UseMississippi(...)`; no compatibility wrappers remain for those methods.

## Next Steps

- [Spring host applications](../samples/spring-sample/concepts/host-applications.md)
- [Client composition](./client-composition.md)
- [Brooks](../brooks/index.md)
