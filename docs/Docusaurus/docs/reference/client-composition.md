---
title: Client Composition
description: Reference ClientBuilder, terminal UseMississippi attachment, and structured client composition failures.
sidebar_position: 40
---

# Client Composition

`UseMississippi(...)` configures, validates, and attaches a `ClientBuilder` to a Blazor WebAssembly host exactly once.

## Applies to

- `Mississippi.Hosting.Client`, included by `Mississippi.Sdk.Client`
- `Mississippi.Hosting.Abstractions` for `IMississippiBuilder`, `BuilderDiagnostic`, `BuilderDiagnosticCodes`, and `BuilderValidationException`

## Contract

| API | Behavior |
| --- | --- |
| `WebAssemblyHostBuilder.UseMississippi(Action<ClientBuilder>)` | Invokes the callback, validates the composition, commits its service descriptors, and returns the original host builder |
| `ClientBuilder.Reservoir(Action<IReservoirBuilder>)` | Configures the shared Reservoir sub-builder and returns the same client builder for chaining |
| `IMississippiBuilder.Validate()` | Returns attachment-readiness diagnostics without modifying registrations; an empty list means the builder is ready |
| `IMississippiBuilder.Services` | Exposes staged registrations for advanced extensions; the collection becomes read-only when the composition scope exits |

Generated `Add{Domain}Client()` extensions receive `ClientBuilder`. Feature-level extensions receive `IReservoirBuilder`. Configure both inside the same terminal callback.

## Defaults and constraints

An empty client composition is allowed. Features are added explicitly through the client or its sub-builders. Repeated `Reservoir(...)` calls share one sub-builder.

The callback runs synchronously during startup. Configure the supplied builder inside the callback; do not capture it for later mutations. The host is built and run separately with the normal WebAssembly host APIs.

Feature-scoped Reservoir builders are writable only inside their `AddFeatureState(...)` callback. That scope closes when the callback exits, including on failure. After client attachment, a captured Reservoir builder rejects further registration before invoking another feature callback.

The nested Inlet SignalR builder also closes when `AddInletBlazorSignalR(...)` builds its registrations, preventing later changes to configuration captured by deferred service factories. It closes when a configuration callback fails as well, including in Reservoir-only applications whose parent collection remains writable; retry through a fresh `AddInletBlazorSignalR(...)` callback.

## Behavior

The client starts with a copy of the host's service descriptors, preserving host-provided defaults when subsystem registrations use `TryAdd`. Its staged changes become visible to the host only after the callback and validation succeed.

The host's attachment identity is independent of its mutable service descriptors. Clearing those descriptors cannot bypass an in-progress or completed attachment. Failed composition releases that identity for a fresh attempt on a writable host; successful attachment remains terminal for that host collection.

Inside the callback, add services through `client.Services`. Configure the host's own `builder.Services` before calling `UseMississippi(...)`. If a callback changes the captured host collection directly, attachment fails with `MSB004` instead of overwriting those changes. The direct host changes remain, the staged client graph is discarded, and a fresh callback can retry.

The host service collection must remain writable until composition finishes. A read-only host is rejected with `MSB005` before configuration or publication. If a callback freezes the host collection, its staged scope still closes and cleanup does not mask an exception from the callback. A frozen host requires a fresh host instance; subsequent attempts report `MSB005` rather than duplicate attachment.

If the callback throws or validation fails, its staged service changes are discarded and its captured builders and collection are closed. When the host remains writable, cleanup removes all client attachment reservations, including replaced or duplicated descriptors, so a corrected `UseMississippi(...)` call can create a fresh scope and succeed. Duplicate and recursive attachment are rejected before the duplicate callback runs. Different hosts can each attach their own client composition.

If the host collection throws while staged services are being published, composition restores the original host descriptors and rethrows the publication error. If restoration also fails, an `AggregateException` reports both errors. Create a fresh host in that case because its registrations may be incomplete.

Staging covers service descriptors configured through the supplied builder. It does not roll back changes to shared object instances, direct mutations of the host captured by application code, or external side effects. Composition validation does not build a service provider, verify network connectivity, or replace service option validation.

## Failure behavior

`BuilderValidationException.Diagnostics` is an immutable snapshot. Each `BuilderDiagnostic` contains `Code`, `Message`, and `Remediation`; exception messages also include these details.

Use the named `BuilderDiagnosticCodes` constants when comparing `Code` programmatically.

| Code | Failure | Remediation |
| --- | --- | --- |
| `MSB001` | A client attachment is already in progress or complete for this host | Combine client configuration inside one `UseMississippi(...)` call |
| `MSB002` | The client builder has already completed attachment | Move all client configuration inside the terminal callback |
| `MSB003` | A captured client scope closed without attaching (`ConfigurationScopeClosed`) | Retry with a new `UseMississippi(...)` callback |
| `MSB004` | The captured host service collection changed during composition (`HostServicesChanged`) | Use `client.Services` inside the callback, or configure host services before it |
| `MSB005` | Host services are read-only (`HostServicesReadOnly`) | Compose before freezing the host services; create a fresh host if they are already frozen |

Null host or callback arguments produce `ArgumentNullException`. Exceptions thrown by application callbacks propagate unchanged. Directly mutating the read-only `Services` collection after attachment produces `InvalidOperationException`.

## Compatibility

`ClientBuilder` replaces `MississippiClientBuilder`. `UseMississippi(...)` replaces both `AddMississippiClient` overloads. The former overload that returned a client builder is removed; put its subsequent feature configuration inside the terminal callback. Custom domain extensions now receive and return `ClientBuilder`.

Reservoir-only applications can also use the dedicated Reservoir entrypoints. This page describes the Mississippi client role.

## Example

This excerpt uses the generated domain and feature names in Spring's client startup. Application host creation, components, and HTTP services are configured before this block.

```csharp
builder.UseMississippi(client =>
{
    client.AddMississippiSamplesSpringDomainClient();
    client.Reservoir(reservoir =>
    {
        reservoir.AddDualEntitySelectionFeature();
        reservoir.AddDemoAccountsFeature();
        reservoir.AddAuthSimulationFeature();
        reservoir.AddReservoirBlazorBuiltIns();
        reservoir.AddInletClient();
        reservoir.AddInletBlazorSignalR(signalR => signalR
            .WithHubPath("/hubs/inlet")
            .ScanProjectionDtos(typeof(BankAccountBalanceProjectionDto).Assembly));
    });
});
```

## Next Steps

- [Compose Inlet in a Mississippi client](../inlet/how-to/how-to.md)
- [Spring host applications](../samples/spring-sample/concepts/host-applications.md)
- [Inlet generated registration reference](../inlet/reference/reference.md)
