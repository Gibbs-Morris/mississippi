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

The nested Inlet SignalR builder also closes when `AddInletBlazorSignalR(...)` builds its registrations, preventing later changes to configuration captured by deferred service factories.

## Behavior

The client starts with a copy of the host's service descriptors, preserving host-provided defaults when subsystem registrations use `TryAdd`. Its staged changes become visible to the host only after the callback and validation succeed.

If the callback throws or validation fails, its staged service changes are discarded, its captured builders and collection are closed, and the attachment reservation is released. A corrected `UseMississippi(...)` call creates a fresh scope and can then succeed. Duplicate and recursive attachment are rejected before the duplicate callback runs. Different hosts can each attach their own client composition.

Staging covers service descriptors configured through the supplied builder. It does not roll back changes to shared object instances, direct mutations of the host captured by application code, or external side effects. Composition validation does not build a service provider, verify network connectivity, or replace service option validation.

## Failure behavior

`BuilderValidationException.Diagnostics` is an immutable snapshot. Each `BuilderDiagnostic` contains `Code`, `Message`, and `Remediation`; exception messages also include these details.

Use the named `BuilderDiagnosticCodes` constants when comparing `Code` programmatically.

| Code | Failure | Remediation |
| --- | --- | --- |
| `MSB001` | A client attachment is already in progress or complete for this host | Combine client configuration inside one `UseMississippi(...)` call |
| `MSB002` | The client builder has already completed attachment | Move all client configuration inside the terminal callback |
| `MSB003` | A captured client scope closed without attaching (`ConfigurationScopeClosed`) | Retry with a new `UseMississippi(...)` callback |

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
