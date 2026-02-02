# Implementation Plan (Revised)

## Requirements (from task.md)

- Sagas are aggregates; reuse existing aggregate infrastructure and patterns.
- Discovery MUST use types/attributes, never namespace conventions.
- State MUST be a record; all state changes via events and reducers.
- Generator reuse: saga generators should mirror aggregate equivalents.
- Provide server, client, and silo generation with a consistent DX.

## Constraints

- Follow repository guardrails (logging, DI, zero warnings, CPM, abstractions).
- No new public API breaking changes without approval.

## Assumptions (UNVERIFIED)

- Existing aggregate generators can be cloned/adapted without architectural conflicts.
- SignalR projection mechanisms are available for saga status updates.

## Unknowns

- Exact aggregate generator entry points and extension patterns.
- Current projection subscription patterns and required DTO shape.
- How aggregates handle step/effect registration and runtime orchestration.

## Changes From Initial Plan

- Explicitly call out attribute/type-based discovery to avoid namespace conventions.
- Add concrete file/module touch list per generator and runtime layer.
- Add plan to reuse projection infrastructure for saga status updates.
- Add rollout, observability, and testing command checklist.

## Detailed Plan

### 1. Abstractions (new projects)

- Create `src/EventSourcing.Sagas.Abstractions` containing:
	- `SagaPhase` enum.
	- `ISagaState` (record-oriented contract), `ISagaDefinition`.
	- `ISagaContext` (access to saga input and correlation).
	- `SagaStepBase<TSaga>` and `SagaCompensationBase<TSaga>`.
	- `StepResult` and `CompensationResult` records.
	- Saga infrastructure events (Started/StepStarted/StepCompleted/StepFailed/Compensating/Completed/Failed, etc.).
	- `SagaStepAttribute`, `SagaCompensationAttribute`, `SagaOptionsAttribute`, `DelayAfterStepAttribute`.
	- `ISagaStepRegistry<TSaga>` and `ISagaStepInfo` contracts.
	- `SagaStatusProjection` record (if reusing UX projection patterns).
- Create `src/Inlet.Generators.Abstractions` additions:
	- `GenerateSagaEndpointsAttribute` mirroring `GenerateAggregateEndpointsAttribute` but for sagas.
- Create `src/Inlet.Client.Abstractions` additions:
	- `ISagaAction`, `ISagaExecutingAction`, `ISagaSucceededAction`, `ISagaFailedAction`.
	- `SagaActionEffectBase<TAction, TState>` mirroring `CommandActionEffectBase`.

### 2. Runtime Implementation

- Create `src/EventSourcing.Sagas` with:
	- `SagaStepRegistry<TSaga>` (attribute-based step/compensation discovery, step hash computation).
	- `SagaContext` implementation.
	- `StartSagaCommandHandler<TInput, TSaga>` using existing command handler patterns.
	- Saga reducers implementing `EventReducerBase<TEvent, TSaga>` for infrastructure events.
	- Saga effects: `SagaStepStartedEffect<TSaga>`, `SagaStepCompletedEffect<TSaga>`, `SagaStepFailedEffect<TSaga>`.
	- `SagaOrchestrator` using `IAggregateGrainFactory` to invoke saga grains.
	- `ServiceCollectionExtensions` for `AddSaga<TSaga, TInput>` and orchestration registration.
	- LoggerExtensions per class that logs entry/success/failure with correlation IDs.

### 3. Server Generators

- Add `SagaControllerGenerator` in [src/Inlet.Server.Generators](../../src/Inlet.Server.Generators):
	- Discover saga types via `[GenerateSagaEndpoints]` and `ISagaDefinition`.
	- Generate `POST /api/sagas/{saga-route}/{sagaId}` start endpoint.
	- Generate `GET /api/sagas/{saga-route}/{sagaId}/status` status endpoint.
- Add `SagaServerDtoGenerator`:
	- Generate `Start{SagaName}SagaDto` and mapper `IMapper<Dto, Input>`.

### 4. Client Generators

- Add saga equivalents in [src/Inlet.Client.Generators](../../src/Inlet.Client.Generators):
	- `SagaClientActionsGenerator` (Start/Executing/Succeeded/Failed actions).
	- `SagaClientActionEffectsGenerator` (HTTP POST to saga endpoint).
	- `SagaClientStateGenerator` and `SagaClientReducersGenerator`.
	- `SagaClientRegistrationGenerator` (registers mappers/reducers/effects).

### 5. Silo Generators

- Add `SagaSiloRegistrationGenerator` in [src/Inlet.Silo.Generators](../../src/Inlet.Silo.Generators):
	- Discover saga state types via `[GenerateSagaEndpoints]` + `ISagaState`.
	- Discover steps and compensations via `[SagaStep]` and `[SagaCompensation]` attributes.
	- Discover reducers via `EventReducerBase<TEvent, TSaga>` where `TSaga : ISagaState`.
	- Register event types, reducers, step registry, and snapshot converters.
	- Avoid namespace-based discovery entirely.

### 6. Sample Saga (Spring)

- Add TransferFunds saga in `samples/Spring/Spring.Domain`:
	- Input record, state record, events, reducers, steps, compensations.
	- `[GenerateSagaEndpoints]` and storage attributes on state.
- Wire generated registrations in `samples/Spring/Spring.Silo`.
- Add client page(s) in `samples/Spring/Spring.Client` to trigger saga and display status.

### 7. Tests

- Add generator L0 tests mirroring existing patterns:
	- Client: under [tests/Inlet.Client.Generators.L0Tests](../../tests/Inlet.Client.Generators.L0Tests).
	- Server: under [tests/Inlet.Server.Generators.L0Tests](../../tests/Inlet.Server.Generators.L0Tests).
	- Silo: under [tests/Inlet.Silo.Generators.L0Tests](../../tests/Inlet.Silo.Generators.L0Tests).
- Add runtime L0 tests for `SagaStepRegistry`, reducers, and effects.
- Add L2 integration test in `samples/Spring/Spring.L2Tests` for end-to-end saga flow.

## Data Model / API Changes

- New saga endpoints under `/api/sagas/{saga-name}`.
- New saga status projection DTOs and status endpoints.
- New saga abstractions in `.Abstractions` projects.

## Observability

- Add LoggerExtensions classes for saga orchestrator, step registry, step execution, and reducers.
- Emit logs for start, step start/completion/failure, compensation, completion/failure.

## Test Plan (commands)

- Build: `pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1`
- Cleanup: `pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1`
- Unit tests: `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1`
- Mutation tests: `pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1`

## Rollout / Backout

- Additive feature only; no data migrations expected.
- Backout by removing saga registrations and generated endpoints; keep existing aggregate APIs intact.