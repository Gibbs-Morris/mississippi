---
id: client-projections
title: Client Projections
sidebar_label: Client Projections
sidebar_position: 5
description: Subscribing to real-time projection updates from Blazor clients.
---

# Client Projections

## Overview

Inlet provides real-time projection synchronization between UX projection grains and Blazor clients via SignalR. Clients subscribe to projections by dispatching actions; updates flow automatically when the server-side projection changes.

## How It Works

```mermaid
%%{init: {'theme':'base','themeVariables': {'primaryColor':'#4a9eff','primaryTextColor':'#ffffff','primaryBorderColor':'#4a9eff','secondaryColor':'#50c878','tertiaryColor':'#6c5ce7','lineColor':'#9aa4b2','fontFamily':'Fira Sans'}}}%%
sequenceDiagram
    participant UI as Blazor component
    participant Store as Inlet store
    participant Effect as InletSignalRActionEffect
    participant Hub as InletHub
    participant SubGrain as IInletSubscriptionGrain
    participant Proj as UX projection grain

    UI->>Store: Dispatch(SubscribeToProjectionAction<T>)
    Store->>Effect: HandleAsync
    Effect->>Hub: SubscribeAsync(path, entityId)
    Hub->>SubGrain: SubscribeAsync(path, entityId)
    SubGrain-->>Hub: subscriptionId
    Hub-->>Effect: subscriptionId
    Effect->>Effect: Fetch initial projection
    Effect->>Store: Dispatch(ProjectionLoadedAction<T>)
    Store-->>UI: Render with data

    Note over Proj,SubGrain: When projection changes...

    Proj-->>SubGrain: version update
    SubGrain-->>Hub: ProjectionUpdatedAsync(path, entityId, version)
    Hub-->>Effect: ProjectionUpdatedAsync callback
    Effect->>Effect: FetchAtVersionAsync
    Effect->>Store: Dispatch(ProjectionUpdatedAction<T>)
    Store-->>UI: Render updated data

    classDef client fill:#4a9eff,color:#ffffff,stroke:#4a9eff;
    classDef action fill:#50c878,color:#ffffff,stroke:#50c878;
    classDef server fill:#f4a261,color:#ffffff,stroke:#f4a261;
    classDef silo fill:#9b59b6,color:#ffffff,stroke:#9b59b6;
    classDef hub fill:#6c5ce7,color:#ffffff,stroke:#6c5ce7;

    class UI,Store client;
    class Effect action;
    class Hub hub;
    class SubGrain,Proj silo;
```

## Subscribing to Projections

### From InletComponent

`InletComponent` is the recommended base class for components that work with projections:

```csharp
using Mississippi.Inlet.Client;
using Mississippi.Inlet.Client.Abstractions.Actions;
using Spring.Client.Features.BankAccountBalance.Dtos;

public class AccountDetails : InletComponent
{
    [Parameter]
    public string AccountId { get; set; } = string.Empty;
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        // Subscribe to projection updates
        Dispatch(new SubscribeToProjectionAction<BankAccountBalanceProjectionDto>(AccountId));
    }
    
    // Access projection data
    private BankAccountBalanceProjectionDto? Balance => 
        GetProjection<BankAccountBalanceProjectionDto>(AccountId);
    
    // Check loading state
    private bool IsLoading => 
        IsProjectionLoading<BankAccountBalanceProjectionDto>(AccountId);
    
    // Check connection state
    private bool IsConnected => 
        IsProjectionConnected<BankAccountBalanceProjectionDto>(AccountId);
    
    // Access any errors
    private Exception? Error => 
        GetProjectionError<BankAccountBalanceProjectionDto>(AccountId);
}
```

([InletComponent.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletComponent.cs))

### InletComponent API

| Method | Description |
|--------|-------------|
| `GetProjection<T>(entityId)` | Returns projection data, or null if not loaded |
| `GetProjectionState<T>(entityId)` | Returns full projection state including metadata |
| `IsProjectionLoading<T>(entityId)` | Returns true if projection is currently loading |
| `IsProjectionConnected<T>(entityId)` | Returns true if subscribed and connected |
| `GetProjectionError<T>(entityId)` | Returns any error that occurred |

## Projection Actions

Inlet provides built-in actions for projection lifecycle management:

### SubscribeToProjectionAction

Subscribes to real-time updates for a projection:

```csharp
Dispatch(new SubscribeToProjectionAction<BankAccountBalanceProjectionDto>(entityId));
```

**Behavior:**
1. Ensures the SignalR connection is established
2. Subscribes to the projection path on the server
3. Fetches the initial projection value
4. Dispatches `ProjectionLoadedAction<T>` with the data

### UnsubscribeFromProjectionAction

Stops receiving updates for a projection:

```csharp
Dispatch(new UnsubscribeFromProjectionAction<BankAccountBalanceProjectionDto>(entityId));
```

**Behavior:**
1. Unsubscribes from the server

### RefreshProjectionAction

Forces a refresh of the projection data:

```csharp
Dispatch(new RefreshProjectionAction<BankAccountBalanceProjectionDto>(entityId));
```

**Behavior:**
1. Fetches latest projection from server
2. Dispatches `ProjectionUpdatedAction<T>` with fresh data

## Projection State

Each projection subscription maintains state in the projection cache:

```csharp
public interface IProjectionState<T>
{
    T? Data { get; }
    long Version { get; }
    bool IsLoading { get; }
    bool IsConnected { get; }
    Exception? ErrorException { get; }
}
```

([IProjectionState.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Abstractions/State/IProjectionState.cs))

Access via `GetProjectionState<T>(entityId)`:

```csharp
var state = GetProjectionState<BankAccountBalanceProjectionDto>(AccountId);

if (state?.IsLoading == true)
{
    // Show loading spinner
}
else if (state?.ErrorException != null)
{
    // Show error message
}
else if (state?.Data != null)
{
    // Render projection data
}
```

## Configuration

### Registering Inlet Client

In your Blazor client's `Program.cs`:

```csharp
// Register Inlet client services
builder.Services.AddInletClient();

// Configure SignalR with projection DTO scanning
builder.Services.AddInletBlazorSignalR(signalR => signalR
    .WithHubPath("/hubs/inlet")
    .ScanProjectionDtos(typeof(BankAccountBalanceProjectionDto).Assembly));
```

([Spring.Client/Program.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client/Program.cs))

### InletBlazorSignalRBuilder Options

```csharp
builder.Services.AddInletBlazorSignalR(signalR => signalR
    // Set the hub endpoint path
    .WithHubPath("/hubs/inlet")
    // Scan assemblies for [ProjectionPath] decorated DTOs
    .ScanProjectionDtos(typeof(MyProjectionDto).Assembly)
    // Set route prefix for projection API calls
    .WithRoutePrefix("/api/projections"));
```

([InletBlazorSignalRBuilder.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/InletBlazorSignalRBuilder.cs))

## Connection Lifecycle

Inlet manages the SignalR connection automatically:

### Connection States

```csharp
public enum SignalRConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting
}
```

([SignalRConnectionStatus.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/SignalRConnection/SignalRConnectionStatus.cs))

### Connection Actions

| Action | Description |
|--------|-------------|
| `RequestSignalRConnectionAction` | Requests a connection attempt |
| `SignalRConnectingAction` | Connection attempt started |
| `SignalRConnectedAction` | Successfully connected |
| `SignalRDisconnectedAction` | Connection lost |
| `SignalRReconnectingAction` | Attempting reconnection |
| `SignalRReconnectedAction` | Successfully reconnected |

### Automatic Reconnection

When reconnected, the SignalR action effect re-subscribes to active projections and refreshes their data.

([InletSignalRActionEffect.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client/ActionEffects/InletSignalRActionEffect.cs))

## Rendering Patterns

### Basic Pattern

```razor
@inherits InletComponent

@if (IsProjectionLoading<BankAccountBalanceProjectionDto>(AccountId))
{
    <LoadingSpinner />
}
else if (GetProjectionError<BankAccountBalanceProjectionDto>(AccountId) is { } error)
{
    <ErrorMessage Message="@error.Message" />
}
else if (GetProjection<BankAccountBalanceProjectionDto>(AccountId) is { } balance)
{
    <div>
        <h2>@balance.HolderName</h2>
        <p>Balance: @balance.Balance.ToString("C")</p>
    </div>
}
else
{
    <p>No data available</p>
}
```

### Connection Status Indicator

```razor
@inherits InletComponent

<div class="connection-status @(IsProjectionConnected<BankAccountBalanceProjectionDto>(AccountId) ? "connected" : "disconnected")">
    @if (IsProjectionConnected<BankAccountBalanceProjectionDto>(AccountId))
    {
        <span>🟢 Live</span>
    }
    else
    {
        <span>🔴 Offline</span>
    }
</div>
```

## Multiple Subscriptions

A single component can subscribe to multiple projections:

```csharp
protected override void OnInitialized()
{
    base.OnInitialized();
    
    // Subscribe to multiple projections
    Dispatch(new SubscribeToProjectionAction<BankAccountBalanceProjectionDto>(AccountId));
    Dispatch(new SubscribeToProjectionAction<BankAccountLedgerProjectionDto>(AccountId));
}

private BankAccountBalanceProjectionDto? Balance => 
    GetProjection<BankAccountBalanceProjectionDto>(AccountId);
    
private BankAccountLedgerProjectionDto? Ledger => 
    GetProjection<BankAccountLedgerProjectionDto>(AccountId);
```

## Cleanup

Subscriptions are cleaned up when you explicitly unsubscribe, and the server clears subscriptions when a connection is closed.

([InletHub.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Server/InletHub.cs))

For manual cleanup:

```csharp
public override void Dispose()
{
    Dispatch(new UnsubscribeFromProjectionAction<BankAccountBalanceProjectionDto>(AccountId));
    base.Dispose();
}
```

## Summary

- Dispatch subscription actions to receive projection updates in real time.
- Use `InletComponent` helpers to read projection data, loading state, and errors.
- The SignalR action effect fetches initial data and reacts to server version updates.

## Next Steps

- [Client Aggregates](./client-aggregates.md) — Dispatching commands to modify state
- [Server](./server.md) — Server-side projection infrastructure

