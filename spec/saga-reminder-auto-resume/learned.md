# Learned Repository Facts

Source baseline: main `c8da151e607bcc8f3b253519317a8e7418d26261`, inspected on 4 October 2026. These facts describe source, rather than a deployed cluster or a passing runtime experiment.

## Verified Current Source

- Aggregate command dispatch and saga reminder recovery live in [GenericAggregateGrain.cs](../../src/DomainModeling.Runtime/GenericAggregateGrain.cs). The grain implements `IRemindable` and uses the saga reminder name `mississippi.saga.resume`.
- [OrleansSagaReminderRegistry.cs](../../src/DomainModeling.Runtime/OrleansSagaReminderRegistry.cs) already calls `RegisterOrUpdateReminder` and `UnregisterReminder`. The earlier claim that no reminder APIs exist is obsolete.
- [DomainModeling.Runtime.csproj](../../src/DomainModeling.Runtime/DomainModeling.Runtime.csproj) already references `Microsoft.Orleans.Reminders`. [Directory.Packages.props](../../Directory.Packages.props) centrally pins the Orleans packages. This proposal requires no automatic package addition or downgrade.
- No generic `AggregateScheduleDefaults`, `ScheduledCommand`, `IAggregateScheduleManager`, or `ScheduleAuditAggregate` implementation exists in the inspected source. They remain proposed types.
- Saga orchestration uses [SagaOrchestrationEffect.cs](../../src/DomainModeling.Runtime/SagaOrchestrationEffect.cs). The aggregate awaits synchronous effect dispatch; separately registered fire-and-forget effects have a different runtime path. Async methods alone do not establish recovery safety or exclude stale decisions.
- [CommandHandlerBase.cs](../../src/DomainModeling.Abstractions/CommandHandlerBase.cs) uses `TCommand` and `TSnapshot`; handlers receive the current aggregate snapshot.
- [ISagaState.cs](../../src/DomainModeling.Abstractions/ISagaState.cs) contains `CorrelationId`, `LastCompletedStepIndex`, `Phase`, `SagaId`, `StartedAt`, and `StepHash`. It has no durable remaining compensation cursor. [SagaCompensatingReducer.cs](../../src/DomainModeling.Runtime/SagaCompensatingReducer.cs) and [SagaFailedReducer.cs](../../src/DomainModeling.Runtime/SagaFailedReducer.cs) only change phase.
- The inspected main has `StartSagaCommand<TInput>` but no `ContinueSagaCommand`. [PR #361](https://github.com/Gibbs-Morris/mississippi/pull/361) is separate unmerged work; its contracts are not supplied by this specification PR.
- [BrookNameAttribute.cs](../../src/Brooks.Abstractions/Attributes/BrookNameAttribute.cs) takes three uppercase alphanumeric segments. A CLR type name fallback is not a stable stored identity.
- `GenericAggregateGrain.ExecuteAsync` has an expected-version overload. A scheduler still needs durable generation/checkpoint validation at execution time; serialization of one grain's calls does not make an earlier remote plan current.
- Saga reminder recovery reads confirmed history and recognizes selected lifecycle boundaries through [SagaLifecycleEventClassifier.cs](../../src/DomainModeling.Runtime/SagaLifecycleEventClassifier.cs). Unsupported tail events fail recovery explicitly. This does not prove exactly-once external effects.

## Proposed Integration Points

- `src/DomainModeling.Abstractions`: generic scheduling contracts and stable command binding identities, if approved.
- `src/DomainModeling.Runtime`: scheduler/dispatcher implementations, registration, logging and metrics, if approved.
- `tests/DomainModeling.Runtime.L0Tests`: deterministic policy, checkpoint and failure-boundary tests.
- Infrastructure test placement must be selected from actual dependencies. Persistent reminder provider and crash/reactivation proof may require L2; it is not automatically a pure unit test.
- Saga adoption remains gated on the explicit direction, checkpoint, replay, authorization and audit contracts in #404/#581. It must account for the existing saga reminder owner.

## Unverified Design Work

- Approval of a generic scheduler rather than the existing saga-specific reminder mechanism.
- Durable control state ownership, independent of optional audit verbosity, and recovery after restart with audit disabled.
- Registration, dispatch and cancellation outcome reconciliation when storage results are unknown.
- Stable schedule generation and tick identity, deduplication retention, missed-tick policy and stale request rejection.
- Reminder provider configuration and behavior for the intended deployment. Package references do not establish an active durable provider.
- Serialization/member/storage identities and complete executable examples for the proposed types.
- Any claim that a marker interface proves handler idempotency or that a single last-token field handles older delayed ticks.
