---
id: event-sourcing-sagas-public-apis
title: Saga Public APIs
sidebar_label: Saga APIs
sidebar_position: 5
description: Public contracts, events, and registrations used by Mississippi saga orchestration.
---

# Saga Public APIs

## Overview

This page lists the public saga contracts and registration helpers used by Mississippi saga orchestration.

## Saga State Contract

`ISagaState` defines the required saga state properties. (https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/ISagaState.cs)

| Member | Type | Purpose |
|--------|------|---------|
| `SagaId` | `Guid` | Unique saga identifier |
| `Phase` | `SagaPhase` | Current lifecycle phase |
| `LastCompletedStepIndex` | `int` | Index of last completed step |
| `CorrelationId` | `string?` | Optional correlation identifier |
| `StartedAt` | `DateTimeOffset?` | Timestamp when the saga started |
| `StepHash` | `string?` | Hash of ordered steps |

`SagaPhase` describes lifecycle phases: `NotStarted`, `Running`, `Compensating`, `Completed`, `Compensated`, and `Failed`. (https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/ISagaState.cs)

## Start Command and Lifecycle Events

### Start Command

`StartSagaCommand<TInput>` starts a saga instance with an input payload. (https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/StartSagaCommand.cs)

| Property | Type | Purpose |
|----------|------|---------|
| `SagaId` | `Guid` | Unique saga identifier |
| `Input` | `TInput` | Saga input payload |
| `CorrelationId` | `string?` | Optional correlation identifier |

### Lifecycle Events

These events represent saga lifecycle transitions and step outcomes. (https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions)

| Event | Purpose |
|-------|---------|
| `SagaStartedEvent` | Saga start event with `SagaId`, `StepHash`, `StartedAt`, and `CorrelationId` |
| `SagaInputProvided<TInput>` | Captures the saga input payload |
| `SagaStepCompleted` | Records a successful step completion |
| `SagaStepFailed` | Records a failed step with error details |
| `SagaCompensating` | Indicates compensation begins from a step index |
| `SagaStepCompensated` | Records a compensated step |
| `SagaCompleted` | Saga completed successfully |
| `SagaCompensated` | Compensation completed |
| `SagaFailed` | Saga failed without compensation or after compensation failure |

## Steps and Results

### ISagaStep and ICompensatable

`ISagaStep<TSaga>` executes a step and returns `StepResult`. (https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/ISagaStep.cs)

`ICompensatable<TSaga>` provides optional compensation and returns `CompensationResult`. (https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/ISagaStep.cs)

### StepResult

`StepResult` communicates success/failure and optional emitted events. (https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/StepResult.cs)

| Member | Purpose |
|--------|---------|
| `Success` | Indicates step success |
| `ErrorCode` | Error code on failure |
| `ErrorMessage` | Error message on failure |
| `Events` | Emitted events on success |

### CompensationResult

`CompensationResult` communicates success/failure/skip for compensation. (https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/CompensationResult.cs)

| Member | Purpose |
|--------|---------|
| `Success` | Compensation succeeded |
| `Skipped` | Compensation skipped |
| `ErrorCode` | Error code on failure |
| `ErrorMessage` | Error message on failure or skip |

## Step Metadata

### SagaStepAttribute

`SagaStepAttribute<TSaga>` marks a class as a saga step, defines a zero-based order, and binds the step to a saga state type via the type parameter. (https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/SagaStepAttribute.cs)

### SagaStepInfo and Providers

`SagaStepInfo` describes a step with index, name, type, and compensation capability. (https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/SagaStepInfo.cs)

`ISagaStepInfoProvider<TSaga>` exposes ordered `SagaStepInfo` entries for orchestration. (https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/ISagaStepInfoProvider.cs)

`AddSagaStepInfo<TSaga>` registers a default provider for a saga state. (https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/SagaStepInfoRegistrations.cs)

## Registrations

`AddSagaOrchestration<TSaga, TInput>` wires saga infrastructure into DI. (https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas/SagaRegistrations.cs)

It registers:

- Event types for saga lifecycle and step events.
- The `StartSagaCommand<TInput>` handler.
- Reducers for saga lifecycle events.
- The saga orchestration effect as an event effect.

(https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas/SagaRegistrations.cs)

## Generator Attributes

### GenerateSagaEndpointsAttribute

`[GenerateSagaEndpoints]` marks a saga state for infrastructure code generation. It includes optional `FeatureKey`, `InputType`, and `RoutePrefix` settings, with documented defaults and a route pattern of `api/sagas/{RoutePrefix}/{sagaId}`. (https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateSagaEndpointsAttribute.cs)

### GenerateSagaEndpointsAttribute (generic)

`[GenerateSagaEndpoints<TInput>]` provides the same behavior with a strongly typed input payload. (https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/Saga/GenerateSagaEndpointsAttribute.cs)

### GenerateSagaStatusReducersAttribute

`[GenerateSagaStatusReducers]` marks a projection for saga status reducer generation. (https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateSagaStatusReducersAttribute.cs)

## Summary

- `ISagaState` and `SagaPhase` define saga state shape and lifecycle phases. (https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/ISagaState.cs)
- Steps implement `ISagaStep<TSaga>` and optionally `ICompensatable<TSaga>`, using `StepResult` and `CompensationResult` for outcomes. (https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/ISagaStep.cs, https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/StepResult.cs, https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/CompensationResult.cs)
- `AddSagaOrchestration` and `AddSagaStepInfo` are the core registration helpers. (https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas/SagaRegistrations.cs, https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/SagaStepInfoRegistrations.cs)

## Next Steps

- [Event Sourcing Sagas](./event-sourcing-sagas.md)
- [Documentation Guide](./contributing/documentation-guide.md)
