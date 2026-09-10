---
title: Add a Reservoir Feature
description: Add typed account-selection state, actions, reducers, and selectors to an existing Reservoir client.
sidebar_position: 2
---

# Add a Reservoir Feature

## Overview

Add a local feature by defining its state, the actions that change it, and pure reducers for those actions. Register the feature once, then let pages dispatch intent and select the values they display.

This guide uses Spring's account-selection feature. Keeping the selected account IDs in one state slice lets several screens use the same selection. The named actions and transitions also give an AI assistant concrete inputs and expected results to implement or explain.

## When to Use This

Use this recipe when an existing client needs shared local state, such as a selected account, an active workspace, or a filter. For server-derived account data, combine the local selection with an [Inlet projection subscription](../../inlet/how-to/how-to.md).

## Before You Begin

- Have a client using the current `Mississippi.Reservoir.Abstractions`, `Mississippi.Reservoir.Core`, and, for Blazor, `Mississippi.Reservoir.Client` APIs.
- Create an `IReservoirBuilder` through the [Reservoir startup path](../getting-started/getting-started.md), or use the Reservoir callback of an existing Mississippi client builder.
- Use a feature key unique within your store.

Create the following six files under your client project's `Features/DualEntitySelection` folder. They reproduce the complete [Spring feature](https://github.com/Gibbs-Morris/mississippi/tree/main/samples/Spring/Spring.Client/Features/DualEntitySelection). The code uses Spring's namespace; keep it consistently or replace it across all six files and the imports in your application. Spring already contains these files, so use its checkout to inspect and verify the reference implementation.

## Steps

### 1. Define the Feature State

Create `DualEntitySelectionState.cs`:

```csharp
using Mississippi.Reservoir.Abstractions.State;


namespace MississippiSamples.Spring.Client.Features.DualEntitySelection;

/// <summary>
///     Feature state for tracking two active entity identifiers in the UI.
/// </summary>
/// <remarks>
///     <para>
///         This state is used by the Operations page to support simultaneous
///         A/B account panels and transfers between them.
///     </para>
///     <para>
///         It is a UI selection concern, not aggregate command state.
///     </para>
/// </remarks>
internal sealed record DualEntitySelectionState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "dualEntitySelection";

    /// <summary>
    ///     Gets the account A entity identifier.
    /// </summary>
    public string? AccountAId { get; init; }

    /// <summary>
    ///     Gets the account B entity identifier.
    /// </summary>
    public string? AccountBId { get; init; }
}
```

`IFeatureState` requires a static `FeatureKey`. The record starts with both IDs unset. Its immutable properties make each later state value an explicit result of an action.

### 2. Define the Actions

Create `SetEntityAIdAction.cs`:

```csharp
using Mississippi.Reservoir.Abstractions.Actions;


namespace MississippiSamples.Spring.Client.Features.DualEntitySelection;

/// <summary>
///     Action dispatched to set the account A entity ID.
/// </summary>
/// <param name="EntityId">The entity ID to set, or empty string to clear selection.</param>
internal sealed record SetEntityAIdAction(string EntityId) : IAction;
```

Create `SetEntityBIdAction.cs`:

```csharp
using Mississippi.Reservoir.Abstractions.Actions;


namespace MississippiSamples.Spring.Client.Features.DualEntitySelection;

/// <summary>
///     Action dispatched to set the account B entity ID.
/// </summary>
/// <param name="EntityId">The entity ID to set, or empty string to clear selection.</param>
internal sealed record SetEntityBIdAction(string EntityId) : IAction;
```

Each action carries the ID needed for its transition. This keeps the input available to tests and development tools.

### 3. Implement the Reducers

Create `DualEntitySelectionReducers.cs`:

```csharp
namespace MississippiSamples.Spring.Client.Features.DualEntitySelection;

/// <summary>
///     Pure reducer functions for the DualEntitySelection feature state.
/// </summary>
internal static class DualEntitySelectionReducers
{
    /// <summary>
    ///     Sets the account A entity ID.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action containing the entity ID.</param>
    /// <returns>A new state with account A updated.</returns>
    public static DualEntitySelectionState SetEntityAId(
        DualEntitySelectionState state,
        SetEntityAIdAction action
    ) =>
        state with
        {
            AccountAId = string.IsNullOrEmpty(action.EntityId) ? null : action.EntityId,
        };

    /// <summary>
    ///     Sets the account B entity ID.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The action containing the entity ID.</param>
    /// <returns>A new state with account B updated.</returns>
    public static DualEntitySelectionState SetEntityBId(
        DualEntitySelectionState state,
        SetEntityBIdAction action
    ) =>
        state with
        {
            AccountBId = string.IsNullOrEmpty(action.EntityId) ? null : action.EntityId,
        };
}
```

Each reducer creates a new state record while preserving the other selection. An empty ID clears its slot to `null`. These reducers preserve other strings as supplied; normalize input at the appropriate application boundary when your feature requires it.

For your own reducers, return the existing state when an action intentionally makes no change. Reservoir permits that local-state pattern. See [state flow](../concepts/state-flow.md) for dispatch and notification behavior.

### 4. Define the Values Screens Need

Create `Selectors/DualEntitySelectionSelectors.cs`:

```csharp
using System;


namespace MississippiSamples.Spring.Client.Features.DualEntitySelection.Selectors;

/// <summary>
///     Selectors for deriving values from <see cref="DualEntitySelectionState" />.
/// </summary>
internal static class DualEntitySelectionSelectors
{
    /// <summary>
    ///     Selects the account A entity ID.
    /// </summary>
    /// <param name="state">The selection state.</param>
    /// <returns>The account A entity ID, or null if none.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static string? GetAccountAId(
        DualEntitySelectionState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.AccountAId;
    }

    /// <summary>
    ///     Selects the account B entity ID.
    /// </summary>
    /// <param name="state">The selection state.</param>
    /// <returns>The account B entity ID, or null if none.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static string? GetAccountBId(
        DualEntitySelectionState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.AccountBId;
    }

    /// <summary>
    ///     Selects whether both account IDs are present.
    /// </summary>
    /// <param name="state">The selection state.</param>
    /// <returns>True when both account IDs are set; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="state" /> is null.
    /// </exception>
    public static bool HasAccountPair(
        DualEntitySelectionState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        return !string.IsNullOrWhiteSpace(state.AccountAId) && !string.IsNullOrWhiteSpace(state.AccountBId);
    }
}
```

`HasAccountPair` answers one question: are both IDs nonblank? Use a separate predicate for a different question, such as whether the IDs differ, and keep business acceptance in the authoritative server handler.

### 5. Register the Feature

Create `DualEntitySelectionFeatureRegistration.cs`:

```csharp
using Mississippi.Reservoir.Abstractions;


namespace MississippiSamples.Spring.Client.Features.DualEntitySelection;

/// <summary>
///     Extension methods for registering the dual entity selection feature.
/// </summary>
internal static class DualEntitySelectionFeatureRegistration
{
    /// <summary>
    ///     Adds the dual entity selection feature to the service collection.
    /// </summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IReservoirBuilder AddDualEntitySelectionFeature(
        this IReservoirBuilder builder
    )
    {
        builder.AddFeatureState<DualEntitySelectionState>(feature => feature
            .AddReducer<SetEntityAIdAction>(DualEntitySelectionReducers.SetEntityAId)
            .AddReducer<SetEntityBIdAction>(DualEntitySelectionReducers.SetEntityBId));
        return builder;
    }
}
```

In your existing `Program.cs`, import the feature namespace and call its registration on the `IReservoirBuilder` before building the host. This is an insertion into your configured startup, where `reservoir` is that builder:

```csharp
using MississippiSamples.Spring.Client.Features.DualEntitySelection;

reservoir.AddDualEntitySelectionFeature();
```

In a full Mississippi client, place the same call inside `client.Reservoir(reservoir => { ... })`. [Spring's Program.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client/Program.cs) shows that composition with its other features.

### 6. Dispatch from a Page and Select for Display

Use an existing Blazor page derived from `StoreComponent`, or from `InletComponent` when the page also consumes projections. Add these imports to its code-behind:

```csharp
using MississippiSamples.Spring.Client.Features.DualEntitySelection;
using MississippiSamples.Spring.Client.Features.DualEntitySelection.Selectors;
```

These page-member excerpts come from Spring's [OperationsPage](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client/Pages/OperationsPage.razor.cs):

```csharp
private string? AccountAId => Select<DualEntitySelectionState, string?>(DualEntitySelectionSelectors.GetAccountAId);

private bool HasAccountPair => Select<DualEntitySelectionState, bool>(DualEntitySelectionSelectors.HasAccountPair);

private void ClearAccountA() => Dispatch(new SetEntityAIdAction(string.Empty));
```

Wire a page event handler to dispatch the relevant action, and pass selected values down to presentational components. For a specific selection, dispatch `new SetEntityAIdAction(accountId)` with the ID from that event. `StoreComponent` owns its store subscription and disposes it with the component; call the base lifecycle methods when overriding them.

## Verify the Result

Build your client project after adding all six files and its startup call. Exercise these transitions through your page or feature tests:

| Starting selection | Action | Expected result |
| --- | --- | --- |
| Both slots unset | Set A to `account-a` | A is set, B stays unset, `HasAccountPair` is false |
| A is `account-a` | Set B to `account-b` | Both IDs are present, `HasAccountPair` is true |
| Both slots set | Set A to an empty string | A becomes null, B is preserved, `HasAccountPair` is false |

Check the old state value as well as the new one: reducer calls leave the old record unchanged. Put these cases in an AI implementation brief so the proposed feature has observable acceptance criteria.

To verify the repository's reference implementation, run from its root:

```powershell
pwsh ./build.ps1 -SkipMississippi -Configuration Release
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject samples/Spring/Spring.Client.L0Tests/Spring.Client.L0Tests.csproj -SourceProject samples/Spring/Spring.Client/Spring.Client.csproj -SkipMutation
```

Require successful exit codes, a zero-warning sample build, and `RESULT: PASS` with executed tests. The sample build verifies the feature and page integrations. [DualEntitySelectionTests](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client.L0Tests/Features/DualEntitySelection/DualEntitySelectionTests.cs) verifies the documented selection sequence, slot preservation, immutable prior states, and the presence predicate. Confirm that all six cases in that class execute successfully. These scripts belong to the Mississippi repository; use your application's own build and test entry points for its feature.

## Summary

State describes the selection, actions carry changes, reducers produce immutable results, and selectors answer display questions. Feature registration connects those parts to one store scope so pages share the same local state model.

## Next Steps

- [Reservoir state flow](../concepts/state-flow.md) for the dispatch model.
- [Selector reference](../reference/selectors.md) for composition and memoization.
- [Reservoir registration reference](../reference/reference.md) for the builder APIs.
