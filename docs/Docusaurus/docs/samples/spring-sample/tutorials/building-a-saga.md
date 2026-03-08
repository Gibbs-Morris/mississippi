---
id: spring-building-a-saga
title: "Building a Saga: Money Transfer"
sidebar_label: Building a Saga
sidebar_position: 4
description: Step-by-step walkthrough of the MoneyTransferSaga that orchestrates a withdrawal and deposit across two BankAccount aggregates with compensation.
---

# Building a Saga: Money Transfer

## Overview

A saga coordinates a workflow that spans multiple aggregates or services. The `MoneyTransferSaga` in Spring transfers money between two bank accounts in two steps:

1. **Withdraw** from the source account
2. **Deposit** to the destination account

If step 0 fails, no forward work was completed, so the saga immediately ends as `Compensated` (nothing to roll back). If step 1 fails (destination account is closed, for example), the saga compensates by re-depositing the withdrawn amount back to the source account. This is the saga pattern in action — forward steps with automatic rollback.

```mermaid
flowchart TB
    Start["StartMoneyTransfer"] --> Step0["Step 0:\nWithdraw from Source"]
    Step0 -->|fail| NoRollback["Compensated\n(nothing to roll back)"]
    Step0 -->|success| Step1["Step 1:\nDeposit to Destination"]
    Step1 -->|success| Done["Saga Complete"]
    Step1 -->|fail| Comp0["Compensate Step 0:\nDeposit back to Source"]
    Comp0 --> Compensated["Compensated\n(funds returned)"]
```

## Before You Begin

Before following this tutorial, read these pages:

- [Spring Sample App](../index.md)
- [Building an Aggregate](./building-an-aggregate.md)
- [Event Sourcing Sagas](../../../archived/concepts/event-sourcing-sagas.md)

## Step 1: Define the Saga State

The saga state record tracks the lifecycle of the workflow. It implements `ISagaState` from Mississippi and stores the input data, phase, and step progress.

```csharp
[BrookName("SPRING", "BANKING", "TRANSFER")]
[SnapshotStorageName("SPRING", "BANKING", "TRANSFERSTATE")]
[GenerateSagaEndpoints(
    InputType = typeof(StartMoneyTransferCommand),
    RoutePrefix = "money-transfer",
    FeatureKey = "moneyTransfer")]
[GenerateSerializer]
[Alias("Spring.Domain.Aggregates.MoneyTransferSaga.MoneyTransferSagaState")]
public sealed record MoneyTransferSagaState : ISagaState
{
    [Id(0)] public Guid SagaId { get; init; }
    [Id(1)] public SagaPhase Phase { get; init; }
    [Id(2)] public int LastCompletedStepIndex { get; init; } = -1;
    [Id(3)] public string? CorrelationId { get; init; }
    [Id(4)] public DateTimeOffset? StartedAt { get; init; }
    [Id(5)] public string? StepHash { get; init; }
    [Id(6)] public StartMoneyTransferCommand? Input { get; init; }
}
```

Key attributes:

| Attribute | Purpose |
|-----------|---------|
| `[GenerateSagaEndpoints]` | Source-generates API endpoints, saga grain, and client dispatchers |
| `InputType` | The command type that starts the saga |
| `RoutePrefix` | The REST path prefix for saga endpoints |
| `FeatureKey` | The key used to identify this saga in source-generated API and client surfaces |

The `ISagaState` interface requires `SagaId`, `Phase`, `LastCompletedStepIndex`, `CorrelationId`, `StartedAt`, and `StepHash`. Mississippi uses these fields to track saga lifecycle and step ordering.

([MoneyTransferSagaState.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Aggregates/MoneyTransferSaga/MoneyTransferSagaState.cs))

## Step 2: Define the Input Command

The input command carries the data needed to execute the saga.

```csharp
[GenerateCommand(Route = "transfer")]
[GenerateSerializer]
[Alias("Spring.Domain.Aggregates.MoneyTransferSaga.Commands.StartMoneyTransferCommand")]
public sealed record StartMoneyTransferCommand
{
    [Id(0)] public string SourceAccountId { get; init; } = string.Empty;
    [Id(1)] public string DestinationAccountId { get; init; } = string.Empty;
    [Id(2)] public decimal Amount { get; init; }
}
```

The saga state captures this input in its `Input` property so that all steps can access it without being passed the command directly.

([StartMoneyTransferCommand.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Aggregates/MoneyTransferSaga/Commands/StartMoneyTransferCommand.cs))

## Step 3: Implement Saga Steps

Each step is a class that implements `ISagaStep<TSaga>`. Steps are ordered by the `[SagaStep<TSaga>(index)]` attribute. Mississippi executes steps in index order and compensates in reverse order on failure.

### Step 0: WithdrawFromSourceStep

This step withdraws funds from the source account by dispatching a `WithdrawFunds` command to the `BankAccountAggregate`. It also implements `ICompensatable<TSaga>` so it can reverse the withdrawal if a later step fails.

```csharp
[SagaStep<MoneyTransferSagaState>(0)]
internal sealed class WithdrawFromSourceStep
    : ISagaStep<MoneyTransferSagaState>,
      ICompensatable<MoneyTransferSagaState>
{
    public WithdrawFromSourceStep(IAggregateGrainFactory aggregateGrainFactory)
    {
        ArgumentNullException.ThrowIfNull(aggregateGrainFactory);
        AggregateGrainFactory = aggregateGrainFactory;
    }

    private IAggregateGrainFactory AggregateGrainFactory { get; }

    public async Task<StepResult> ExecuteAsync(
        MoneyTransferSagaState state,
        CancellationToken cancellationToken)
    {
        StartMoneyTransferCommand? input = state.Input;
        if (input is null)
            return StepResult.Failed(AggregateErrorCodes.InvalidState,
                "Transfer input not provided.");

        if (string.IsNullOrWhiteSpace(input.SourceAccountId)
            || string.IsNullOrWhiteSpace(input.DestinationAccountId))
            return StepResult.Failed(AggregateErrorCodes.InvalidCommand,
                "Account identifiers are required.");

        if (string.Equals(input.SourceAccountId, input.DestinationAccountId,
                StringComparison.Ordinal))
            return StepResult.Failed(AggregateErrorCodes.InvalidCommand,
                "Source and destination accounts must differ.");

        if (input.Amount <= 0)
            return StepResult.Failed(AggregateErrorCodes.InvalidCommand,
                "Transfer amount must be positive.");

        WithdrawFunds command = new() { Amount = input.Amount };
        IGenericAggregateGrain<BankAccountAggregate> grain =
            AggregateGrainFactory
                .GetGenericAggregate<BankAccountAggregate>(input.SourceAccountId);

        OperationResult result = await grain.ExecuteAsync(command, cancellationToken);
        return result.Success
            ? StepResult.Succeeded()
            : StepResult.Failed(
                result.ErrorCode ?? AggregateErrorCodes.InvalidCommand,
                result.ErrorMessage);
    }

    public async Task<CompensationResult> CompensateAsync(
        MoneyTransferSagaState state,
        CancellationToken cancellationToken)
    {
        StartMoneyTransferCommand? input = state.Input;
        if (input is null)
            return CompensationResult.SkippedResult("Transfer input not provided.");

        // Reverse the withdrawal by depositing back
        DepositFunds command = new() { Amount = input.Amount };
        IGenericAggregateGrain<BankAccountAggregate> grain =
            AggregateGrainFactory
                .GetGenericAggregate<BankAccountAggregate>(input.SourceAccountId);

        OperationResult result = await grain.ExecuteAsync(command, cancellationToken);
        return result.Success
            ? CompensationResult.Succeeded()
            : CompensationResult.Failed(
                result.ErrorCode ?? "COMPENSATION_FAILED",
                result.ErrorMessage);
    }
}
```

The step validates the input, dispatches a `WithdrawFunds` command, and returns a `StepResult`. The `CompensateAsync` method reverses the withdrawal by dispatching a `DepositFunds` command back to the same account.

([WithdrawFromSourceStep.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Aggregates/MoneyTransferSaga/Steps/WithdrawFromSourceStep.cs))

### Step 1: DepositToDestinationStep

This step deposits funds to the destination account. It is the final step and does not implement `ICompensatable` — there is no subsequent step that could fail.

```csharp
[SagaStep<MoneyTransferSagaState>(1)]
internal sealed class DepositToDestinationStep : ISagaStep<MoneyTransferSagaState>
{
    public DepositToDestinationStep(IAggregateGrainFactory aggregateGrainFactory)
    {
        ArgumentNullException.ThrowIfNull(aggregateGrainFactory);
        AggregateGrainFactory = aggregateGrainFactory;
    }

    private IAggregateGrainFactory AggregateGrainFactory { get; }

    public async Task<StepResult> ExecuteAsync(
        MoneyTransferSagaState state,
        CancellationToken cancellationToken)
    {
        StartMoneyTransferCommand? input = state.Input;
        if (input is null)
            return StepResult.Failed(AggregateErrorCodes.InvalidState,
                "Transfer input not provided.");

        if (string.IsNullOrWhiteSpace(input.DestinationAccountId))
            return StepResult.Failed(AggregateErrorCodes.InvalidCommand,
                "Destination account is required.");

        if (input.Amount <= 0)
            return StepResult.Failed(AggregateErrorCodes.InvalidCommand,
                "Transfer amount must be positive.");

        DepositFunds command = new() { Amount = input.Amount };
        IGenericAggregateGrain<BankAccountAggregate> grain =
            AggregateGrainFactory
                .GetGenericAggregate<BankAccountAggregate>(input.DestinationAccountId);

        OperationResult result = await grain.ExecuteAsync(command, cancellationToken);
        return result.Success
            ? StepResult.Succeeded()
            : StepResult.Failed(
                result.ErrorCode ?? AggregateErrorCodes.InvalidCommand,
                result.ErrorMessage);
    }
}
```

([DepositToDestinationStep.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Domain/Aggregates/MoneyTransferSaga/Steps/DepositToDestinationStep.cs))

## Checkpoint 1

At this point, your saga should include:

- a state record implementing `ISagaState`
- an input command for starting the saga
- ordered step classes for withdrawal and deposit
- compensation on the withdrawal step only

## How Saga Compensation Works

When the deposit step fails, Mississippi automatically runs compensation in reverse order:

```mermaid
flowchart LR
    Fail["Step 1 Fails"] --> RevStep0["Compensate Step 0"]
    RevStep0 -->|deposits back| Source["Source Account\n(funds restored)"]
```

1. Step 1 (`DepositToDestinationStep`) returns `StepResult.Failed(...)`.
2. Mississippi detects the failure and enters the `Compensating` phase.
3. Step 0 (`WithdrawFromSourceStep`) implements `ICompensatable`, so Mississippi calls `CompensateAsync`.
4. The compensation deposits the amount back to the source account.
5. If compensation succeeds, the saga moves to the `Compensated` phase. The source account has its funds restored.

Steps that do not implement `ICompensatable` are skipped during compensation. This is intentional — not every step has a meaningful reversal.

## The Complete Saga File Structure

```text
Aggregates/MoneyTransferSaga/
├── MoneyTransferSagaState.cs           # Saga state record (ISagaState)
├── Commands/
│   └── StartMoneyTransferCommand.cs    # Input command
└── Steps/
    ├── WithdrawFromSourceStep.cs       # Step 0 (with compensation)
    └── DepositToDestinationStep.cs     # Step 1 (no compensation)
```

Sagas do not need their own events, `CommandHandler`s, or `EventReducer` types. The saga framework emits lifecycle events (`SagaStarted`, `SagaStepCompleted`, `SagaCompleted`, `SagaFailed`, etc.) automatically. Steps dispatch commands to existing aggregates, reusing the BankAccount's existing command/handler/event/EventReducer pipeline.

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Steps dispatch commands, not emit events directly | Reuses existing aggregate validation and event pipelines |
| Only Step 0 implements `ICompensatable` | Step 1 is the final step — if it fails, compensation runs backwards from step 0, not step 1 itself, so step 1 has nothing to reverse |
| Input validation happens in the first step | Catches invalid requests before any state changes |
| `IAggregateGrainFactory` is injected via DI | Steps use the same dependency injection as all other domain types |

## Checkpoint 2

Before continuing to projections, verify these outcomes in the Spring sample source:

- the step order matches the saga diagram on this page
- the withdrawal step implements compensation and the deposit step does not
- saga steps dispatch commands to existing aggregates instead of emitting events directly

## Summary

A Mississippi saga is a state record plus a set of ordered steps. Steps execute in order and compensate in reverse. Each step dispatches commands to existing aggregates, reusing their validation and event pipelines. Source generators create the API endpoints, saga grain, and client dispatchers from annotations.

## Next Steps

- [Building Projections](./building-projections.md) — Create read-optimized views that track saga and aggregate state
- [Host Architecture](../concepts/host-applications.md) — See how sagas are wired into the host with a single registration call
