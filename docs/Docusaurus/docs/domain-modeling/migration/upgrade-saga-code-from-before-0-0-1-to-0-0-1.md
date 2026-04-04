---
id: domain-modeling-upgrade-saga-code-from-before-0-0-1-to-0-0-1
title: Upgrade saga code from < 0.0.1 to 0.0.1
sidebar_label: Upgrade < 0.0.1 to 0.0.1
sidebar_position: 1
description: Migrate older Mississippi saga implementations to the 0.0.1 recovery-aware step contracts, runtime-status surface, and manual-resume API.
---

# Upgrade saga code from < 0.0.1 to 0.0.1

## Scope

Use this guide when your saga implementation was authored against versions earlier than `0.0.1` and you are moving that code to `0.0.1`.

It focuses on the contract changes that affect saga steps, compensation, runtime status, and manual resume surfaces.

## Compatibility Summary

| Area | 0.0.1 impact |
|------|--------------|
| Step source code | **Breaking** - `[SagaStep<TSaga>(index)]` becomes `[SagaStep<TSaga>(index, forwardRecoveryPolicy)]`, and step methods gain `SagaStepExecutionContext` |
| Compensation source code | **Breaking** - compensating steps must declare `CompensationRecoveryPolicy` when needed and accept `SagaStepExecutionContext` |
| Generated HTTP surfaces | **Additive** - existing `GET status` remains, while `GET runtime-status` and `POST resume` are added |
| In-flight saga recovery | **Validate carefully** - workflow hash mismatches now surface as `WorkflowMismatch` instead of silently replaying changed step definitions |
| Rollback | **Not guaranteed after 0.0.1 recovery metadata is written** - take backups before deployment if rollback matters |

## Breaking Changes

### Step attributes now declare recovery intent

Before `0.0.1`, a step only declared its order:

```csharp
[SagaStep<MoneyTransferSagaState>(0)]
internal sealed class WithdrawFromSourceStep : ISagaStep<MoneyTransferSagaState>
{
}
```

In `0.0.1`, the same step must declare its forward recovery policy, and compensatable steps can also declare a compensation recovery policy:

```csharp
[SagaStep<MoneyTransferSagaState>(
    0,
    SagaStepRecoveryPolicy.ManualOnly,
    CompensationRecoveryPolicy = SagaStepRecoveryPolicy.ManualOnly)]
internal sealed class WithdrawFromSourceStep
    : ISagaStep<MoneyTransferSagaState>,
      ICompensatable<MoneyTransferSagaState>
{
}
```

### Step execution and compensation now receive `SagaStepExecutionContext`

Before `0.0.1`, step methods received only saga state and cancellation:

```csharp
public Task<StepResult> ExecuteAsync(
    MoneyTransferSagaState state,
    CancellationToken cancellationToken)
{
}
```

In `0.0.1`, both forward execution and compensation receive attempt metadata:

```csharp
public Task<StepResult> ExecuteAsync(
    MoneyTransferSagaState state,
    SagaStepExecutionContext context,
    CancellationToken cancellationToken)
{
}

public Task<CompensationResult> CompensateAsync(
    MoneyTransferSagaState state,
    SagaStepExecutionContext context,
    CancellationToken cancellationToken)
{
}
```

### Generated saga surfaces now distinguish raw state from runtime status

Generated saga endpoints keep the raw saga state surface and add dedicated recovery operations:

- `GET status` returns the raw `ISagaState` snapshot.
- `GET runtime-status` returns metadata-only `SagaRuntimeStatus`.
- `POST resume` returns typed `SagaResumeResponse`.

If your saga state also uses `[GenerateMcpSagaTools]`, the generated MCP tool set mirrors the same runtime-status and manual-resume capabilities.

## Prepare for the Upgrade

Before changing code, work through this checklist:

1. Inventory each `ISagaStep<TSaga>` and `ICompensatable<TSaga>` implementation.
2. Identify which forward and compensation paths are safe to replay automatically.
3. Mark non-idempotent work as `SagaStepRecoveryPolicy.ManualOnly`.
4. Decide whether the saga needs a saga-level override with `[SagaRecovery(...)]`.
5. Add or update tests for forward failure, compensation, blocked/manual resume, and workflow mismatch.

Spring is the concrete example: both `WithdrawFunds` and `DepositFunds` declare `Idempotent = false`, so the sample marks both money-movement steps as `ManualOnly` instead of replaying them automatically.

## Upgrade Sequence

### 1. Keep saga state focused on business data

The `ISagaState` contract does not need to absorb operator-facing recovery fields. Keep business data in the saga state, and let the framework expose recovery metadata separately.

If you need a saga-level override, add `[SagaRecovery(...)]` to the state type. When the attribute is absent, generated registration defaults to `SagaRecoveryMode.Automatic`.

### 2. Update every step attribute and method signature

Convert each step to the new attribute and signature pair:

```csharp
[SagaStep<MySagaState>(2, SagaStepRecoveryPolicy.Automatic)]
internal sealed class ExampleStep : ISagaStep<MySagaState>
{
    public Task<StepResult> ExecuteAsync(
        MySagaState state,
        SagaStepExecutionContext context,
        CancellationToken cancellationToken)
    {
    }
}
```

If the step compensates, add `ICompensatable<TSaga>` and choose `CompensationRecoveryPolicy` explicitly.

### 3. Use `SagaStepExecutionContext` to stabilize downstream work

`SagaStepExecutionContext` provides the attempt metadata the older contracts did not expose.

| Property | Use during migration |
|----------|----------------------|
| `AttemptId` and `AttemptStartedAt` | Correlate one concrete run of a forward or compensation step |
| `OperationKey` | Pass a stable idempotency or deduplication key to downstream systems |
| `Direction`, `StepIndex`, `StepName` | Log and reason about exactly which part of the saga is executing |
| `Source` and `IsReplay` | Distinguish a fresh start from reminder-driven or manual recovery |

If a step was previously relying on ambient correlation or ad hoc retry flags, replace that logic with values from `SagaStepExecutionContext`.

### 4. Move operator diagnostics to runtime-status surfaces

Use `SagaRuntimeStatus` or a projection such as `MoneyTransferStatusProjection` for operator-facing recovery details instead of expanding raw saga state.

The current runtime-status contract includes fields such as:

- `RecoveryMode`
- `ResumeDisposition`
- `PendingDirection`
- `PendingStepIndex`
- `PendingStepName`
- `BlockedReason`
- `LastResumeSource`
- `LastResumeAttemptedAt`
- `AutomaticAttemptCount`
- `ReminderArmed`
- `WorkflowHashMatches`

### 5. Update clients and tooling to the typed resume response

Manual resume callers should consume `SagaResumeResponse` instead of inferring behavior from raw strings or generic success/failure envelopes.

The typed response tells you whether the request was:

- `Accepted`
- `Blocked`
- `Terminal`
- `WorkflowMismatch`
- `Unauthorized`
- `NoAction`

## Data, State, and Serialization Implications

- Raw saga state remains a separate surface from recovery metadata.
- Recovery-aware hosts can now expose operator metadata without changing the shape of your `ISagaState` record.
- In-flight sagas created under older definitions can report `WorkflowMismatch` when the persisted `StepHash` no longer matches the current ordered step definition.
- Status projections can now carry nullable recovery enums such as `SagaExecutionDirection?` and `SagaResumeSource?`. Generated gateway and client DTOs handle those nullable enum shapes directly.

## Validation After the Upgrade

Validate the upgrade with the same discipline you would use for any breaking source-contract change:

1. Build with zero warnings.
2. Run the saga's L0 tests, including success, forward failure, compensation, blocked/manual resume, and workflow mismatch paths.
3. Start a saga and compare `GET status` with `GET runtime-status` to confirm raw state and recovery metadata stay separate.
4. Issue `POST resume` for a blocked or manual-only saga and verify the typed `SagaResumeResponse` matches the expected disposition.
5. If you expose a status projection, verify recovery fields such as `ResumeDisposition`, `PendingStepName`, and `LastResumeSource` update as expected.

## Rollback

Treat rollback as safe only before any upgraded host has written `0.0.1` recovery metadata for the target saga.

After `0.0.1` recovery events, checkpoints, or status metadata have been persisted, this guide does not guarantee an automated rollback path. If rollback matters, take backups of the relevant saga streams and snapshots before deployment and validate recovery behavior in a pre-production environment first.

## Related Release Notes and Reference

There is not yet a dedicated public `0.0.1` release-notes page in this docs set. Use these verified references for the final contract surface:

- [Sagas and Orchestration](../../concepts/sagas-and-orchestration.md)
- [Building a Saga](../../samples/spring-sample/tutorials/building-a-saga.md)
- [Building Projections](../../samples/spring-sample/tutorials/building-projections.md)
- [Domain Modeling Reference](../reference/reference.md)
