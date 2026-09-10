---
title: Own a Live Projection Subscription
sidebar_position: 2
description: Display a server projection in Blazor, refresh it, and release its subscription when the page changes entity or closes.
---

# Own a Live Projection Subscription

## Overview

Give a page ownership of the projection data it needs. Subscribe when the page selects an entity, read the data from Reservoir, and release the old subscription when selection changes or the page closes.

This keeps account screens connected to the same server read model while leaving business rules in the domain. The typed DTO, entity ID, and explicit lifecycle also give an AI assistant concrete boundaries for generating and testing the page.

## When To Use This

Use this pattern when a Blazor screen selects an entity and owns the live projection state shared by its child components. Configure the generated projection features and Inlet connection before adding the screen.

## Before You Begin

- Start from the [Spring sample](../../samples/spring-sample/index.md), with its generated client features and [Inlet client composition](./how-to.md).
- Use the sample's generated `BankAccountBalanceProjectionDto` and its `bank-account-balance` projection path.
- Choose one owner for each DTO type and entity ID in a store. Let child components read the shared state. Inlet tracks subscriptions by that pair; one owner's unsubscribe releases the pair's active subscription.

The following component is a complete additional Spring page. Its route parameter supplies the selected account ID. For your application, substitute the generated DTO and the entity-selection mechanism that your page uses.

## Steps

### 1. Add The Page

Create `samples/Spring/Spring.Client/Pages/ProjectionWatch.razor`:

```razor
@page "/projection-watch/{AccountId}"
@namespace MississippiSamples.Spring.Client.Pages
@inherits InletComponent
@using Microsoft.AspNetCore.Components
@using Mississippi.Inlet.Client
@using Mississippi.Inlet.Client.SignalRConnection
@using MississippiSamples.Spring.Client.Features.BankAccountBalance.Dtos

<h1>Account balance</h1>
<p>Connection: @(GetState<SignalRConnectionState>().Status)</p>

@if (IsProjectionLoading<BankAccountBalanceProjectionDto>(AccountId))
{
    <p role="status">Loading account…</p>
}
else if (GetProjectionError<BankAccountBalanceProjectionDto>(AccountId) is not null)
{
    <p role="alert">The account could not be loaded. Try refreshing.</p>
}
else if (GetProjection<BankAccountBalanceProjectionDto>(AccountId) is { } account)
{
    <p>@account.HolderName: @account.Balance</p>
    <p>Version: @(GetProjectionState<BankAccountBalanceProjectionDto>(AccountId)?.Version)</p>
}
else
{
    <p>No account data has been loaded.</p>
}

<button type="button" @onclick="RefreshCurrent">Refresh</button>

@code {
    private string? subscribedAccountId;

    /// <summary>
    /// Gets or sets the account selected by the route.
    /// </summary>
    [Parameter]
    public string AccountId { get; set; } = string.Empty;

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (string.Equals(AccountId, subscribedAccountId, StringComparison.Ordinal))
        {
            return;
        }

        ReleaseSubscription();
        if (!string.IsNullOrWhiteSpace(AccountId))
        {
            subscribedAccountId = AccountId;
            SubscribeToProjection<BankAccountBalanceProjectionDto>(AccountId);
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleaseSubscription();
        }

        base.Dispose(disposing);
    }

    private void RefreshCurrent()
    {
        if (!string.IsNullOrWhiteSpace(AccountId))
        {
            RefreshProjection<BankAccountBalanceProjectionDto>(AccountId);
        }
    }

    private void ReleaseSubscription()
    {
        if (subscribedAccountId is { } previousId)
        {
            subscribedAccountId = null;
            UnsubscribeFromProjection<BankAccountBalanceProjectionDto>(previousId);
        }
    }
}
```

`InletComponent` inherits Reservoir's store subscription and render lifecycle. Calling the base lifecycle methods preserves that behavior. The component records its own projection interest and explicitly releases it during disposal.

The ID comparison makes repeated renders inexpensive. Starting the subscription after rendering follows the sample's browser lifecycle; a change to the route parameter releases the previous entity and selects the next one.

### 2. Present Loading, Data, And Connection Separately

Use `IsProjectionLoading<T>()` and `GetProjectionError<T>()` for that entity's fetch state. `GetProjection<T>()` returns its DTO when available, and `GetProjectionState<T>()` exposes its version.

An initial HTTP 404 means the projection has no data to load yet. Inlet records an empty result and retains the active subscription so later events can provide data. Give this state a useful empty presentation, such as an invitation to open the account.

Read `SignalRConnectionState.Status` for the transport indicator. This is the connection shared by projection subscriptions. Projection entry `IsConnected` is separately controlled by projection connection actions; the transport feature is the source for this page's connection display.

### 3. Refresh On User Request

`RefreshProjection<T>(entityId)` requests the latest projection through the configured fetcher and publishes the result into Reservoir. The Refresh button above provides a retry after a fetch error or a deliberate reload of the displayed account.

Inlet also re-subscribes and refreshes active interests after a successful SignalR reconnection. Keep your normal loading, empty, error, and data presentation usable during that process.

### 4. Keep Subscription Ownership With The Screen

When several panels display the same account, subscribe once in their shared page or another deliberate owner. Pass the entity ID to the panels and let them select the projection state.

A useful pattern is “the account workspace owns balance and ledger subscriptions; its summary and transaction panels read them.” Giving each panel independent ownership of the same pair couples one panel's disposal to the others' updates.

Unsubscribing releases the live interest. Cached projection entries remain in Reservoir, so use the current entity ID when selecting data and explicitly refresh when your workflow requires a new read.

## Verify The Result

From the repository root, build the sample:

```powershell
pwsh ./build.ps1 -SkipMississippi -Configuration Release
```

Run Spring using the [sample startup instructions](https://github.com/Gibbs-Morris/mississippi/blob/main/README.md#quick-start--see-it-running). Create an account on the Accounts page, then open `/projection-watch/{accountId}` using that account's ID.

1. Confirm the holder, balance, and version appear after the initial fetch.
2. Leave this page open and deposit into the same account from another browser tab. Confirm the page receives the projection update.
3. Open the watch route for another account. Confirm it shows that account's state.
4. Click Refresh and confirm the latest data returns.
5. Navigate away. In the browser's Network tools, inspect the SignalR connection messages for the unsubscribe request; the component's disposal releases its owned interest.

For the sample's existing automated browser validation, run `pwsh ./test-spring.ps1 -Doctor` and then `pwsh ./test-spring.ps1`. A `PASS` summary means tests executed successfully; use the manual checks above to exercise the new watch page specifically.

## Source Code

- [InletComponent.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletComponent.cs) defines the page helpers.
- [OperationsPage.razor.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client/Pages/OperationsPage.razor.cs) demonstrates ownership for balance, ledger, and saga projections.
- [InletSignalRActionEffect.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs) implements subscription, fetch, unsubscribe, and reconnect behavior.
- [ProjectionsReducer.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/Reducers/ProjectionsReducer.cs) and [SignalRConnectionState.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/SignalRConnection/SignalRConnectionState.cs) define the state consumed by the page.

## Summary

Own the subscription at the screen boundary, select data by the active entity ID, present fetch and transport state separately, and release the interest as part of the page lifecycle.

## Next Steps

- [Generated application contracts](../reference/generated-contracts.md) explains how the DTO, path, notification, and HTTP read fit together.
- [Reservoir state flow](../../reservoir/concepts/state-flow.md) explains dispatch, effects, and component updates.
- [Enable DevTools](../../reservoir/how-to/enable-devtools.md) to inspect projection actions and state during development.
