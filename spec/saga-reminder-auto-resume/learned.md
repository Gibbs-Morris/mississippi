# Learned Repository Facts

## Verified Facts

- Aggregate runtime command path exists in `src/EventSourcing.Aggregates/GenericAggregateGrain.cs` and is the natural integration point for scheduled command dispatch.
- There is currently no reminder-based scheduling framework in `src/EventSourcing.Aggregates`.
- There is no reusable attribute contract today for binding aggregate types to scheduled commands/reminder policy.
- Saga orchestration runs via `IEventEffect<TSaga>` in `src/EventSourcing.Sagas/SagaOrchestrationEffect.cs`. The effect's `HandleAsync` method is asynchronous (returns `IAsyncEnumerable<object>`), but it executes within the grain's single-threaded context and blocks the grain until complete. This is not "synchronous" in the traditional sense — it is grain-blocking async. The distinction matters for understanding how scheduled command dispatch interacts with saga orchestration: each tick callback would also block the grain during command execution.
- There is no existing saga resume command; only start command exists (`StartSagaCommand<TInput>`).
- Saga remains a valid phase 2 consumer once aggregate scheduling primitives are available.
- `CommandHandlerBase<TCommand, TSnapshot>` uses `TSnapshot` as the second generic type parameter name (not `TAggregate`). This reflects the framework design intent: the second type parameter is the aggregate's projected state/snapshot. Concrete types like `WorldAggregate` are valid as `TSnapshot`, but documentation/code samples should acknowledge the `TSnapshot` convention.
- `ISagaState` (in `src/EventSourcing.Sagas.Abstractions/ISagaState.cs`) requires six properties: `CorrelationId`, `LastCompletedStepIndex`, `Phase`, `SagaId`, `StartedAt`, `StepHash`. Any code sample implementing `ISagaState` must include all six.
- `BrookNameAttribute` lives in `src/EventSourcing.Brooks.Abstractions/` and is used for aggregate state type identification. Code samples declaring aggregate state types should include `[BrookName("...")]` if the framework requires it for that type.
- `System.Type` is not natively Orleans-serializable. Any grain interface contracts or records that pass `Type` as a parameter will fail at runtime. Use `string` (assembly-qualified name or alias) instead.
- `ScheduleBackoff` enum will live in `Mississippi.EventSourcing.Aggregates.Abstractions` namespace alongside the scheduling attributes. It does not exist yet — it is a new type to be created.
- No files in `src/` reference `IRemindable`, `RegisterOrUpdateReminder`, or `UnregisterReminder`. Orleans reminder APIs are not used anywhere in the current codebase. Introducing them will require adding the `Microsoft.Orleans.Runtime` package dependency (or equivalent) to `Directory.Packages.props`.
- `SagaOrchestrationEffect` injects `IServiceProvider` (for saga step resolution). This is an existing factory-pattern exception per shared-policies. The new scheduler grain should not inject `IServiceProvider`; it should dispatch commands through an explicit `IScheduledCommandDispatcher` interface.

## Candidate Integration Points

- `src/EventSourcing.Aggregates.Abstractions`
  - new scheduling attributes and policy contracts
  - scheduled command registration and runtime contracts
  - optional idempotency marker interface(s) for scheduled handlers
- `src/EventSourcing.Aggregates`
  - reminder/scheduler runtime grain(s)
  - integration with aggregate grain command execution path
  - logging and metrics for scheduling lifecycle
- `src/EventSourcing.Sagas.Abstractions` (phase 2)
  - saga-specific policy defaults and resume command contracts
- `src/EventSourcing.Sagas` (phase 2)
  - use generic aggregate scheduling to implement saga auto-resume

## UNVERIFIED

- Whether existing source generators should emit scheduling registrations from attributes automatically.
- Whether reminder runtime should be one generic scheduler grain or per-aggregate-type scheduler grain.
