# Verification

## Claim List

| ID | Claim | Status |
|----|-------|--------|
| C1 | GenericAggregateGrain is the only place commands are executed | VERIFIED |
| C2 | Events are persisted before returning to caller | VERIFIED |
| C3 | There is no existing effect mechanism on the server | VERIFIED |
| C4 | Client-side IEffect uses CanHandle + HandleAsync pattern | VERIFIED |
| C5 | AggregateSiloRegistrationGenerator scans sub-namespaces (Handlers/, Reducers/) | VERIFIED |
| C6 | Effects sub-namespace does not currently exist in domain aggregates | VERIFIED |
| C7 | Reducers use IRootReducer for composition | VERIFIED |
| C8 | Handlers use IRootCommandHandler for composition | VERIFIED |
| C9 | Server reducers are named EventReducer, client reducers are ActionReducer | VERIFIED |
| C10 | Client effects use IEffect interface, not "ActionEffect" | VERIFIED |
| C11 | Orleans grains are single-threaded and non-reentrant by default | VERIFIED |
| C12 | Codebase has existing [OneWay] + [StatelessWorker] fire-and-forget pattern | VERIFIED |
| C13 | Client Store uses fire-and-forget for effects: `_ = TriggerEffectsAsync()` | VERIFIED |
| C14 | Effects running inline in aggregate would block subsequent commands | VERIFIED |
| C15 | Saga patterns can be implemented via effects calling other aggregates | VERIFIED |

## Verification Questions

### Architecture Questions

**Q1:** Where exactly in GenericAggregateGrain could effects be triggered?
- **Answer:** After `BrookWriterGrain.AppendEventsAsync()` completes successfully (line ~268) and before returning `OperationResult.Ok()`. This ensures events are persisted before effects run.
- **Evidence:** [GenericAggregateGrain.cs#L241-L283](../../src/EventSourcing.Aggregates/GenericAggregateGrain.cs)

**Q2:** How does the client Store trigger effects after reducers?
- **Answer:** In `CoreDispatch()`: (1) `ReduceFeatureStates(action)`, (2) `NotifyListeners()`, (3) `_ = TriggerEffectsAsync(action)` (fire-and-forget with discard)
- **Evidence:** [Store.cs#L232-L247](../../src/Reservoir/Store.cs)

**Q3:** What types does AggregateSiloRegistrationGenerator discover?
- **Answer:** It scans for:
  - Types with `[GenerateAggregateEndpoints]` attribute
  - Handler types in `{Aggregate}/Handlers/` extending `CommandHandlerBase<,>`
  - Reducer types in `{Aggregate}/Reducers/` extending `EventReducerBase<,>`
- **Evidence:** [AggregateSiloRegistrationGenerator.cs#L74-L178](../../src/Inlet.Silo.Generators/AggregateSiloRegistrationGenerator.cs)

**Q4:** Does any aggregate in the samples have an Effects folder?
- **Answer:** No, Spring.Domain.Aggregates.BankAccount only has: Commands, Events, Handlers, Reducers
- **Evidence:** Listed directory contents show no Effects folder

### Interface Questions

**Q5:** What is the signature of IEventReducer.TryReduce?
- **Answer:** `bool TryReduce(TProjection state, object eventData, out TProjection projection)`
- **Evidence:** [IEventReducer.cs](../../src/EventSourcing.Reducers.Abstractions/IEventReducer.cs)

**Q6:** What is the signature of client IEffect.HandleAsync?
- **Answer:** `IAsyncEnumerable<IAction> HandleAsync(IAction action, CancellationToken cancellationToken)`
- **Evidence:** [IEffect.cs](../../src/Reservoir.Abstractions/IEffect.cs)

**Q7:** What is the signature of ICommandHandler.TryHandle?
- **Answer:** `bool TryHandle(object command, TSnapshot? state, out OperationResult<IReadOnlyList<object>> result)`
- **Evidence:** [ICommandHandler.cs](../../src/EventSourcing.Aggregates.Abstractions/ICommandHandler.cs)

### Registration Questions

**Q8:** How are reducers registered in DI?
- **Answer:** `services.AddReducer<TEvent, TProjection, TReducer>()` which calls:
  - `services.AddTransient<IEventReducer<TProjection>, TReducer>()`
  - `services.AddTransient<IEventReducer<TEvent, TProjection>, TReducer>()`
  - `services.AddRootReducer<TProjection>()`
- **Evidence:** [ReducerRegistrations.cs#L45-L57](../../src/EventSourcing.Reducers/ReducerRegistrations.cs)

**Q9:** How are command handlers registered in DI?
- **Answer:** `services.AddCommandHandler<TCommand, TSnapshot, THandler>()` which calls:
  - `services.AddTransient<ICommandHandler<TSnapshot>, THandler>()`
  - `services.AddTransient<ICommandHandler<TCommand, TSnapshot>, THandler>()`
  - `services.AddRootCommandHandler<TSnapshot>()`
- **Evidence:** [AggregateRegistrations.cs#L69-L84](../../src/EventSourcing.Aggregates/AggregateRegistrations.cs)

**Q10:** How are client effects registered?
- **Answer:** `services.AddEffect<TEffect>()` which calls:
  - `services.AddScoped<IEffect, TEffect>()`
- **Evidence:** [ReservoirRegistrations.cs#L25-L36](../../src/Reservoir/ReservoirRegistrations.cs)

### Source Generator Questions

**Q11:** What code does AggregateSiloRegistrationGenerator produce?
- **Answer:** Generates `Add{AggregateName}Aggregate(this IServiceCollection services)` extension method that:
  - Calls `AddAggregateSupport()`
  - Registers event types: `services.AddEventType<{EventType}>()`
  - Registers handlers: `services.AddCommandHandler<{Cmd}, {Agg}, {Handler}>()`
  - Registers reducers: `services.AddReducer<{Event}, {Agg}, {Reducer}>()`
  - Registers snapshot converter: `services.AddSnapshotStateConverter<{Agg}>()`
- **Evidence:** [AggregateSiloRegistrationGenerator.cs#L200-L285](../../src/Inlet.Silo.Generators/AggregateSiloRegistrationGenerator.cs)

**Q12:** How does the generator discover handlers/reducers in sub-namespaces?
- **Answer:** `FindHandlersForAggregate()` and `FindReducersForAggregate()` look for:
  - `{AggregateNamespace}.Handlers` and `{AggregateNamespace}.Reducers` namespaces
  - Types extending `CommandHandlerBase<,>` or `EventReducerBase<,>` with matching aggregate type
- **Evidence:** [AggregateSiloRegistrationGenerator.cs#L74-L178](../../src/Inlet.Silo.Generators/AggregateSiloRegistrationGenerator.cs)

### Concurrency Questions

**Q13:** Is GenericAggregateGrain reentrant?
- **Answer:** No explicit `[Reentrant]` attribute visible. Default is non-reentrant (single-threaded).
- **Evidence:** [GenericAggregateGrain.cs](../../src/EventSourcing.Aggregates/GenericAggregateGrain.cs) - no reentrant attribute

**Q14:** Could running effects cause grain activation delays?
- **Answer:** Yes, if effects are awaited. The grain is single-threaded, so long-running effects would block subsequent commands.
- **Mitigation:** Use fire-and-forget via `[OneWay]` stateless worker grain (same pattern as snapshot persistence).

**Q15:** Does the codebase have existing fire-and-forget grain patterns?
- **Answer:** YES! `ISnapshotPersisterGrain` uses `[StatelessWorker]` + `[OneWay]` for fire-and-forget.
- **Evidence:** 
  - [ISnapshotPersisterGrain.cs#L39](../../src/EventSourcing.Snapshots.Abstractions/ISnapshotPersisterGrain.cs) - `[OneWay]` attribute
  - [SnapshotCacheGrain.cs#L270](../../src/EventSourcing.Snapshots/SnapshotCacheGrain.cs) - `_ = persisterGrain.PersistAsync(envelope)` pattern

**Q16:** What is the purpose of `[OneWay]` attribute in Orleans?
- **Answer:** Marks a grain method as fire-and-forget. The caller doesn't wait for completion; the method may not have even started when the call returns.
- **Evidence:** Microsoft Orleans documentation + existing usage in `ISnapshotPersisterGrain`

**Q17:** What is the purpose of `[StatelessWorker]` attribute in Orleans?
- **Answer:** Creates an auto-scaling pool of grain instances. No state affinity; Orleans creates new activations as needed based on load. Ideal for CPU/IO-bound work.
- **Evidence:** Microsoft Orleans documentation

### Throughput Questions

**Q18:** How does the client Store handle effect errors?
- **Answer:** Swallows exceptions. Effects should emit error actions; failures don't break dispatch.
- **Evidence:** [Store.cs#L296-L330](../../src/Reservoir/Store.cs) - try/catch around `effect.HandleAsync()` with empty catch blocks

**Q19:** Does the client Store await effects?
- **Answer:** NO - it uses fire-and-forget: `_ = TriggerEffectsAsync(action)`
- **Evidence:** [Store.cs#L245](../../src/Reservoir/Store.cs)

### Saga Pattern Questions

**Q20:** Can an aggregate grain call other aggregate grains via IGrainFactory?
- **Answer:** Yes, grains can inject `IGrainFactory` and call other grains. However, this creates coupling.
- **Evidence:** Standard Orleans pattern; no code-level restriction

**Q21:** Could effects call other aggregates via a gateway?
- **Answer:** Yes, effects could inject `IAggregateCommandGateway` which wraps `IGrainFactory` to send commands to aggregates.
- **Evidence:** Design decision for this RFC

### Naming Questions

**Q22:** Is there already a type named IEventEffect or EventEffectBase in the codebase?
- **Answer:** No
- **Evidence:** grep search for "IEventEffect|EventEffectBase" returns no matches

**Q23:** Are there any existing attributes that could conflict with effect discovery?
- **Answer:** No; `[GenerateCommand]` and `[GenerateAggregateEndpoints]` are the relevant attributes
- **Evidence:** Reviewed Inlet.Generators.Abstractions

## What Changed After Verification

1. **Fire-and-forget pattern confirmed** - Both client Store (`_ = TriggerEffectsAsync`) and snapshot persistence (`[OneWay]` + stateless worker) use fire-and-forget. Effects MUST use the same pattern for throughput.

2. **EffectDispatcherGrain required** - To avoid blocking the aggregate grain, effects run in a separate `[StatelessWorker]` grain with `[OneWay]` calls.

3. **IAggregateCommandGateway enables sagas** - Effects can inject a gateway to send commands to other aggregates, enabling saga orchestration patterns.

4. **Generator pattern is clear** - Must add `FindEffectsForAggregate()` method similar to handlers/reducers, looking in `{Aggregate}/Effects/` namespace for types extending `EventEffectBase<,>`.

5. **Registration pattern is clear** - Need `AddEventEffect<TEvent, TAggregate, TEffect>()` and infrastructure registration.

6. **Unified mental model** - Client (Action→Reducer→Effect) and Server (Event→Reducer→Effect) follow the same pattern. Saga is just an aggregate whose effects call other aggregates.
