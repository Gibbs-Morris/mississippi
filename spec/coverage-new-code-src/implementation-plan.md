# Implementation plan

## Plan (revised)

### 1. Scope & mapping
- Confirm changed src files via `git diff main...HEAD -- src`.
- Map changed modules to test projects:
	- EventSourcing.Sagas + EventSourcing.Sagas.Abstractions -> tests/EventSourcing.Sagas.L0Tests
	- Inlet.Client.Generators + Inlet.Generators.Abstractions -> tests/Inlet.Client.Generators.L0Tests
	- Inlet.Server.Generators -> tests/Inlet.Server.Generators.L0Tests
	- Inlet.Silo.Generators -> tests/Inlet.Silo.Generators.L0Tests

### 2. Tests to add/update (L0)
- EventSourcing.Sagas.L0Tests:
	- New tests for abstractions: `CompensationResult`, `StepResult`, `SagaStepAttribute`, `SagaStepCompensated`,
		`SagaStepFailed`, `SagaStepInfoRegistrations`, `ISagaStepInfoProvider` default `AppliesTo`.
	- New tests for `SagaStateMutator` (copy/update path + missing ctor exception).
	- New tests for `SagaOrchestrationEffect<TSaga>` covering:
		- Start -> step success -> step completed event
		- Step failure -> emits `SagaStepFailed` + `SagaCompensating`
		- Compensation success/skip -> emits `SagaStepCompensated`
		- Compensation failure -> emits `SagaFailed`
		- Compensation from negative step -> emits `SagaCompensated`
		- Missing step metadata -> emits `SagaFailed` for compensation / no events for execution
		- Non-compensatable step treated as compensated
	- Optional: test logger extensions by invoking extension methods with a fake logger.

- Inlet.Client.Generators.L0Tests:
	- Add tests for generators: ActionEffects, Mappers, Reducers, Registration, State.
	- Add tests to exercise `SagaClientGeneratorHelper` branches:
		- Saga with explicit `FeatureKey` + `RoutePrefix`.
		- Saga without optional props (defaults for route/feature).
		- Saga with missing `InputType` (should not generate output).
		- Non-saga type (no attribute) to exercise skip paths.
		- Input type as positional record vs property-based record.
	- Add a direct test for `GenerateSagaEndpointsAttribute` property set/get if coverage remains low.

- Inlet.Server.Generators.L0Tests:
	- Add tests for `SagaControllerGenerator`:
		- Generated controller + DTO files
		- Positional vs property input mapping (check generated code)
		- Default vs explicit route/feature key
		- Missing `InputType` yields no generation

- Inlet.Silo.Generators.L0Tests:
	- Add tests for `SagaSiloRegistrationGenerator`:
		- Steps discovered via `[SagaStep]` attribute and `ISagaStep<T>`
		- Explicit `Saga` attribute value vs inferred from interface
		- Compensation detection with `ICompensatable<T>`
		- Reducer discovery from `EventReducerBase<TEvent, TSaga>`
		- Case with zero steps (no AddSagaStepInfo block)

### 3. Coverage verification
- Re-run coverage for impacted test projects:
	- `pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject EventSourcing.Sagas.L0Tests -SkipMutation`
	- `pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Inlet.Client.Generators.L0Tests -SkipMutation`
	- `pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Inlet.Server.Generators.L0Tests -SkipMutation`
	- `pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Inlet.Silo.Generators.L0Tests -SkipMutation`
- Inspect coverage.cobertura.xml for changed files; confirm >=95% for changed src code.

### 4. Build/test validation
- Run unit tests:
	- `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1`
- Build sample solution if any shared dependencies changed:
	- `pwsh ./eng/src/agent-scripts/build-sample-solution.ps1`

### 5. Exceptions
- If any line is infeasible to cover, document the file/line and rationale in `verification.md`.
