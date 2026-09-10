---
title: Keep a Workspace Projection Live
sidebar_position: 2
description: Keep a fixed account projection live across Blazor page navigation with an application-shell owner and shared Reservoir state.
---

# Keep a Workspace Projection Live

## Overview

Keep a workspace's account projection subscribed for the lifetime of the Blazor client. A provider in the application shell establishes the interest once, and pages read the resulting state from Reservoir.

This keeps related account screens connected to the same server read model while leaving business rules in the domain. A fixed entity ID, typed DTO, and explicit application lifetime give an AI assistant concrete boundaries for generating and testing those screens.

## When to use this

Use this pattern for a workspace with a small, fixed set of entities that should stay live as users navigate between pages. The application shell owns the subscriptions; individual pages own only their store listeners and presentation.

## Before you begin

- Start from [Spring](../../samples/spring-sample/index.md), with its generated client features and [Inlet client composition](./how-to.md).
- Choose an existing account ID for the workspace. Replace `doc-account-001` in the provider below with that ID before running the client.
- Give each DTO type and entity ID one application-level owner. This example keeps the selected account live throughout the client session, including while its display page is closed.

The following files add a provider and a display page to Spring. Keep the provider outside the router so it remains mounted during page navigation.

## Steps

### 1. Add The Application Owner

Create `samples/Spring/Spring.Client/Components/AccountProjectionProvider.razor`:

```razor
@namespace MississippiSamples.Spring.Client.Components
@inherits InletComponent
@using Mississippi.Inlet.Client
@using MississippiSamples.Spring.Client.Features.BankAccountBalance.Dtos

@code {
    /// <summary>
    /// Identifies the account kept live throughout this client session.
    /// </summary>
    public const string AccountId = "doc-account-001";

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (firstRender)
        {
            SubscribeToProjection<BankAccountBalanceProjectionDto>(AccountId);
        }
    }
}
```

The first browser render starts the subscription. Later renders retain the same interest. Inlet performs the asynchronous connection, hub subscription, and initial HTTP read, publishing the result into Reservoir.

### 2. Mount It Outside The Router

Update `samples/Spring/Spring.Client/App.razor` to include the provider alongside Spring's existing root components:

```razor
@using Mississippi.Reservoir.Client.BuiltIn.Components
@using MississippiSamples.Spring.Client.Components
@namespace MississippiSamples.Spring.Client

<ReservoirNavigationProvider/>
<ReservoirDevToolsInitializerComponent/>
<AccountProjectionProvider/>

<Router AppAssembly="@typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)"/>
    </Found>
    <NotFound>
        <LayoutView Layout="@typeof(MainLayout)">
            <p>Sorry, there's nothing at this address.</p>
        </LayoutView>
    </NotFound>
</Router>
```

Page navigation can now occur while the initial subscription is pending: the provider and its interest remain part of the application shell. Keep the provider mounted for the client session. The scoped hub-connection provider disposes its connection when its service scope ends.

### 3. Display Shared State In A Page

Create `samples/Spring/Spring.Client/Pages/ProjectionWatch.razor`:

```razor
@page "/projection-watch"
@namespace MississippiSamples.Spring.Client.Pages
@inherits InletComponent
@using Mississippi.Inlet.Client
@using Mississippi.Inlet.Client.SignalRConnection
@using MississippiSamples.Spring.Client.Components
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
    private const string AccountId = AccountProjectionProvider.AccountId;

    private void RefreshCurrent() =>
        RefreshProjection<BankAccountBalanceProjectionDto>(AccountId);
}
```

`InletComponent` inherits Reservoir's store subscription and render lifecycle. Disposing this display page releases its store listener. The application provider continues owning the live account interest, ready for other pages or a return visit.

### 4. Present Fetch And Transport State

Use `IsProjectionLoading<T>()` and `GetProjectionError<T>()` for the entity's fetch state. `GetProjection<T>()` returns its DTO when available, and `GetProjectionState<T>()` exposes its version.

The initial fetch maps HTTP 404 to `NotFound`: no projection data is available yet. Inlet publishes a loaded result with null DTO data and retains the active subscription so later events can provide data. Present this separately from a fetch error. Give this state a useful presentation, such as an invitation to open the account.

Read `SignalRConnectionState.Status` for the shared transport indicator. Projection entry `IsConnected` is separately controlled by projection connection actions; use the transport feature for the connection display above.

`RefreshProjection<T>(entityId)` requests the latest projection and publishes the result into Reservoir. Inlet also re-establishes active interests and refreshes them after a successful SignalR reconnection. Keep the loading, empty, error, and data presentation usable throughout that process.

## Verify the result

From the repository root, build the sample:

```powershell
pwsh ./build.ps1 -SkipMississippi -Configuration Release
```

Run Spring using the [sample startup instructions](https://github.com/Gibbs-Morris/mississippi/blob/main/README.md#quick-start--see-it-running), then open `/projection-watch`.

1. Confirm the configured account's holder, balance, and version appear after the initial fetch.
2. Leave the page open and deposit into the same account from another browser tab. Confirm the displayed projection updates.
3. Navigate to another page and back. Confirm the account remains selected and its data is available from the shared store.
4. Repeat navigation while the initial projection request is delayed in browser Network tools. The application owner remains mounted while the request finishes.
5. Click Refresh and confirm the latest data returns.

For Spring's existing automated browser validation, run `pwsh ./test-spring.ps1 -Doctor` and then `pwsh ./test-spring.ps1`. A `PASS` summary means tests executed successfully; the manual checks above exercise the additional workspace page specifically.

## Source Code

- [InletComponent.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletComponent.cs) defines the page helpers; [StoreComponent.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Client/StoreComponent.cs) manages component store listeners.
- [App.razor](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client/App.razor) supplies the existing Spring application shell.
- [InletSignalRActionEffect.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs) implements subscription, fetch, and reconnect behavior.
- [HubConnectionProvider.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/HubConnectionProvider.cs) owns the scoped connection.
- [ProjectionsReducer.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/Reducers/ProjectionsReducer.cs) and [SignalRConnectionState.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/SignalRConnection/SignalRConnectionState.cs) define the state consumed by the page.

## Summary

Mount a fixed workspace's subscription owner in the application shell, share its projection state between pages, and give each page its own presentation and store-listener lifecycle.

## Next Steps

- [Generated application contracts](../reference/generated-contracts.md) explains how the DTO, path, notification, and HTTP read fit together.
- [Reservoir state flow](../../reservoir/concepts/state-flow.md) explains dispatch, effects, and component updates.
- [Enable DevTools](../../reservoir/how-to/enable-devtools.md) to inspect projection actions and state during development.
