# Change Log (ADR-style)

- 2026-02-03: Added this log file to record one-line ADR entries per commit.
- 2026-02-03: Added EventSourcing.Sagas.Abstractions project file to introduce saga abstractions layer.
- 2026-02-03: Added DI abstractions package to enable saga registration helpers.
- 2026-02-03: Added `ISagaState` and `SagaPhase` to define saga lifecycle state shape.
- 2026-02-03: Added `ISagaStep<TSaga>` and `ICompensatable<TSaga>` contracts for step execution and compensation.
- 2026-02-03: Added `SagaStepAttribute` to mark and order saga steps.
- 2026-02-03: Added `StepResult` to represent step success/failure with emitted events.
- 2026-02-03: Added `CompensationResult` to represent compensation outcomes.
- 2026-02-03: Added saga infrastructure event records for lifecycle and compensation flow.
- 2026-02-03: Added `StartSagaCommand<TInput>` to standardize saga start inputs.
