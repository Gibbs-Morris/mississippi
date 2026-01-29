---
id: client-aggregates
title: Client Aggregates
sidebar_label: Client Aggregates
sidebar_position: 6
description: Dispatching commands to aggregates from Blazor clients using generated actions.
---

# Client Aggregates

## Overview

Inlet generates client-side actions and effects that dispatch commands to server-side aggregates. This page covers how to use generated command actions and how command execution is tracked on the client.

## How It Works

```mermaid
%%{init: {'theme':'base','themeVariables': {'primaryColor':'#4a9eff','primaryTextColor':'#ffffff','primaryBorderColor':'#4a9eff','secondaryColor':'#50c878','tertiaryColor':'#6c5ce7','lineColor':'#9aa4b2','fontFamily':'Fira Sans'}}}%%
sequenceDiagram
    participant UI as Blazor component
    participant Store as Inlet store
    participant Effect as CommandActionEffectBase
    participant API as Aggregate controller
    participant Grain as Aggregate grain

    UI->>Store: Dispatch({Command}Action)
    Store->>Effect: HandleAsync
    Effect->>Store: Dispatch({Command}ExecutingAction)
    Effect->>API: POST /api/aggregates/{aggregate}/{entityId}/{route}
    API->>Grain: ExecuteAsync
    Grain-->>API: OperationResult
    API-->>Effect: OperationResult
    Effect->>Store: Dispatch({Command}SucceededAction | {Command}FailedAction)

    classDef client fill:#4a9eff,color:#ffffff,stroke:#4a9eff;
    classDef action fill:#50c878,color:#ffffff,stroke:#50c878;
    classDef server fill:#f4a261,color:#ffffff,stroke:#f4a261;
    classDef silo fill:#9b59b6,color:#ffffff,stroke:#9b59b6;

    class UI,Store client;
    class Effect action;
    class API server;
    class Grain silo;
```

## Dispatching Commands

### Basic Usage

Dispatch the generated action from any component. The Spring sample dispatches the generated command action directly:

```csharp
private void Deposit() => Dispatch(new DepositFundsAction(SelectedEntityId!, depositAmount));
```

([Index.razor.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client/Pages/Index.razor.cs))

### Generated Action Structure

For each command, Inlet generates four action types:

```csharp
// Primary action - dispatch this to execute the command
internal sealed record DepositFundsAction(string EntityId, decimal Amount) : ICommandAction;

// Dispatched when command starts executing
internal sealed record DepositFundsExecutingAction(string CommandId, string CommandType, DateTimeOffset Timestamp)
    : ICommandExecutingAction<DepositFundsExecutingAction>;

// Dispatched when command succeeds
internal sealed record DepositFundsSucceededAction(string CommandId, DateTimeOffset Timestamp)
    : ICommandSucceededAction<DepositFundsSucceededAction>;

// Dispatched when command fails
internal sealed record DepositFundsFailedAction(string CommandId, string? ErrorCode, string? ErrorMessage, DateTimeOffset Timestamp)
    : ICommandFailedAction<DepositFundsFailedAction>;
```

([DepositFundsAction.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client/Features/BankAccountAggregate/Actions/DepositFundsAction.cs))
([DepositFundsExecutingAction.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client/Features/BankAccountAggregate/Actions/DepositFundsExecutingAction.cs))

## Tracking Execution State

### Generated Feature State

Each aggregate gets a generated feature state for tracking command execution:

```csharp
internal sealed record BankAccountAggregateState : AggregateCommandStateBase, IAggregateCommandState
{
    public static string FeatureKey => "bankAccountAggregate";
}
```

([BankAccountAggregateState.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client/Features/BankAccountAggregate/State/BankAccountAggregateState.cs))

`AggregateCommandStateBase` provides the core execution tracking fields, including `IsExecuting`, error fields, and command history.

([AggregateCommandStateBase.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Abstractions/State/AggregateCommandStateBase.cs))

### Accessing State in Components

```csharp
public class DepositForm : InletComponent
{
    private BankAccountAggregateState State => GetState<BankAccountAggregateState>();

    private bool IsSubmitting => State.IsExecuting;

    private string? ErrorMessage => State.ErrorMessage;
}
```

## Registering the Feature

Register the generated feature in your client's `Program.cs`:

```csharp
using Spring.Client.Features.BankAccountAggregate;

// Register the generated aggregate feature
builder.Services.AddBankAccountAggregateFeature();
```

This registers the aggregate command feature, reducers, and effects for the aggregate.

([Spring.Client/Program.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client/Program.cs))

## Generated Reducers and Effects

The generated reducers delegate to `AggregateCommandStateReducers`, and the generated effects derive from `CommandActionEffectBase`.

([CommandClientReducersGenerator.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Generators/CommandClientReducersGenerator.cs))
([CommandActionEffectBase.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs))

## Multiple Commands

Each command gets its own action type, but they share the same feature state:

```csharp
// Multiple commands for the same aggregate
Dispatch(new OpenAccountAction(AccountId, "John Doe", 0m));
Dispatch(new DepositFundsAction(AccountId, 100m));
Dispatch(new WithdrawFundsAction(AccountId, 50m));
```

All commands update the same aggregate command state feature.

## Combining with Projections

A typical pattern combines command dispatch with projection subscription:

```csharp
public class AccountManager : InletComponent
{
    [Parameter]
    public string AccountId { get; set; } = string.Empty;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        Dispatch(new SubscribeToProjectionAction<BankAccountBalanceProjectionDto>(AccountId));
    }

    private BankAccountBalanceProjectionDto? Balance =>
        GetProjection<BankAccountBalanceProjectionDto>(AccountId);

    private BankAccountAggregateState CommandState => GetState<BankAccountAggregateState>();

    private void HandleDeposit(decimal amount)
    {
        Dispatch(new DepositFundsAction(AccountId, amount));
    }
}
```

When the command succeeds:

1. The aggregate processes the command and emits an event.
2. The projection grain updates its state.
3. Inlet notifies the client via SignalR.
4. The projection is refetched and the UI updates.

## Error Handling

`CommandActionEffectBase` maps HTTP and operation failures into `{Command}FailedAction` with an error code and message.

([CommandActionEffectBase.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Abstractions/ActionEffects/CommandActionEffectBase.cs))

## Summary

- Dispatch generated command actions to invoke aggregate endpoints.
- Track execution state via the generated aggregate feature state.
- Combine commands with projection subscriptions for write/read UX flows.

## Next Steps

- [Server](./server.md) — Server-side API and Orleans integration
- [Client Projections](./client-projections.md) — Real-time projection updates

