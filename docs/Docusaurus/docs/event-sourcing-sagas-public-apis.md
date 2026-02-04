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

[`ISagaState`][isagastate] defines the required saga state properties.

| Member | Type | Purpose |
|--------|------|---------|
| `SagaId` | `Guid` | Unique saga identifier |
| `Phase` | `SagaPhase` | Current lifecycle phase |
| `LastCompletedStepIndex` | `int` | Index of last completed step |
| `CorrelationId` | `string?` | Optional correlation identifier |
| `StartedAt` | `DateTimeOffset?` | Timestamp when the saga started |
| `StepHash` | `string?` | Hash of ordered steps |

`SagaPhase` describes lifecycle phases: `NotStarted`, `Running`, `Compensating`, `Completed`, `Compensated`, and `Failed`. See [`ISagaState.cs`][isagastate].

## Start Command and Lifecycle Events

### Start Command

[`StartSagaCommand<TInput>`][startsagacommand] starts a saga instance with an input payload.

| Property | Type | Purpose |
|----------|------|---------|
| `SagaId` | `Guid` | Unique saga identifier |
| `Input` | `TInput` | Saga input payload |
| `CorrelationId` | `string?` | Optional correlation identifier |

### Lifecycle Events

These events represent saga lifecycle transitions and step outcomes. See the [Sagas Abstractions folder][sagas-abstractions].

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

[`ISagaStep<TSaga>`][isagastep] executes a step and returns `StepResult`.

[`ICompensatable<TSaga>`][isagastep] provides optional compensation and returns `CompensationResult`.

### StepResult

[`StepResult`][stepresult] communicates success/failure and optional emitted events.

| Member | Purpose |
|--------|---------|
| `Success` | Indicates step success |
| `ErrorCode` | Error code on failure |
| `ErrorMessage` | Error message on failure |
| `Events` | Emitted events on success |

### CompensationResult

[`CompensationResult`][compensationresult] communicates success/failure/skip for compensation.

| Member | Purpose |
|--------|---------|
| `Success` | Compensation succeeded |
| `Skipped` | Compensation skipped |
| `ErrorCode` | Error code on failure |
| `ErrorMessage` | Error message on failure or skip |

## Step Metadata

### SagaStepAttribute

[`SagaStepAttribute<TSaga>`][sagastepattribute] marks a class as a saga step, defines a zero-based order, and binds the step to a saga state type via the type parameter.

### SagaStepInfo and Providers

[`SagaStepInfo`][sagastepinfo] describes a step with index, name, type, and compensation capability.

[`ISagaStepInfoProvider<TSaga>`][isagastepinfoprovider] exposes ordered `SagaStepInfo` entries for orchestration.

[`AddSagaStepInfo<TSaga>`][sagastepinforegistrations] registers a default provider for a saga state.

## Registrations

[`AddSagaOrchestration<TSaga, TInput>`][sagaregistrations] wires saga infrastructure into DI.

It registers:

- Event types for saga lifecycle and step events.
- The `StartSagaCommand<TInput>` handler.
- Reducers for saga lifecycle events.
- The saga orchestration effect as an event effect.

## Generator Attributes

### GenerateSagaEndpointsAttribute

[`[GenerateSagaEndpoints]`][generatesagaendpoints] marks a saga state for infrastructure code generation. It includes optional `FeatureKey`, `InputType`, and `RoutePrefix` settings, with documented defaults and a route pattern of `api/sagas/{RoutePrefix}/{sagaId}`.

### GenerateSagaEndpointsAttribute (generic)

[`[GenerateSagaEndpoints<TInput>]`][generatesagaendpoints-generic] provides the same behavior with a strongly typed input payload.

### GenerateSagaStatusReducersAttribute

[`[GenerateSagaStatusReducers]`][generatesagastatusreducers] marks a projection for saga status reducer generation.

## Summary

- [`ISagaState`][isagastate] and `SagaPhase` define saga state shape and lifecycle phases.
- Steps implement [`ISagaStep<TSaga>`][isagastep] and optionally [`ICompensatable<TSaga>`][isagastep], using [`StepResult`][stepresult] and [`CompensationResult`][compensationresult] for outcomes.
- [`AddSagaOrchestration`][sagaregistrations] and [`AddSagaStepInfo`][sagastepinforegistrations] are the core registration helpers.

## Next Steps

- [Event Sourcing Sagas](./event-sourcing-sagas.md)
- [Documentation Guide](./contributing/documentation-guide.md)

<!-- Reference-style links -->
[isagastate]: https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/ISagaState.cs
[startsagacommand]: https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/StartSagaCommand.cs
[sagas-abstractions]: https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions
[isagastep]: https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/ISagaStep.cs
[stepresult]: https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/StepResult.cs
[compensationresult]: https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/CompensationResult.cs
[sagastepattribute]: https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/SagaStepAttribute.cs
[sagastepinfo]: https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/SagaStepInfo.cs
[isagastepinfoprovider]: https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/ISagaStepInfoProvider.cs
[sagastepinforegistrations]: https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas.Abstractions/SagaStepInfoRegistrations.cs
[sagaregistrations]: https://github.com/Gibbs-Morris/mississippi/blob/main/src/EventSourcing.Sagas/SagaRegistrations.cs
[generatesagaendpoints]: https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateSagaEndpointsAttribute.cs
[generatesagaendpoints-generic]: https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateSagaEndpointsAttribute%7BTInput%7D.cs
[generatesagastatusreducers]: https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Abstractions/GenerateSagaStatusReducersAttribute.cs
