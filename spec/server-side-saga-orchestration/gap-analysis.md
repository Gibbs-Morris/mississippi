# Gap Analysis: Server-Side Saga Orchestration First Draft

## Executive Summary

After reviewing `task.md`, the first draft spec, and the existing aggregate infrastructure, this document identifies **critical gaps, inconsistencies, and developer experience concerns** that must be addressed before implementation.

## Critical Finding: Contradiction Between Task.md and Implementation Plan

### The Core Contradiction

**task.md explicitly forbids namespace-based discovery:**

> | ❌ DO NOT | ✅ DO |
> |-----------|-------|
> | Use namespace/folder conventions for discovery | Use **types** or **attributes** (e.g., `[SagaStep]`, `[SagaCompensation]`) |
> 
> **NEVER use namespace conventions** - Discovery MUST use types or attributes, not folder structure like `Steps/` or `Compensations/`

**But the Implementation Plan in Phase 5 says:**

> Implement `SagaSiloRegistrationGenerator`
> - Scan for steps in `Steps/` namespace ⚠️ VIOLATES task.md
> - Scan for compensations in `Compensations/` namespace ⚠️ VIOLATES task.md
> - Scan for reducers in `Reducers/` namespace ⚠️ VIOLATES task.md

**Existing aggregate generators use namespace conventions:**

The `AggregateSiloRegistrationGenerator` currently discovers:

- Handlers in `{AggregateNamespace}.Handlers` sub-namespace
- Reducers in `{AggregateNamespace}.Reducers` sub-namespace
- Effects in `{AggregateNamespace}.Effects` sub-namespace

This creates a **design tension**: task.md requires attribute/type-based discovery for sagas, but aggregates use namespace conventions.

### Recommended Resolution

1. **Sagas MUST use attribute-based discovery** as task.md requires
2. **Consider migrating aggregates to attribute-based discovery in a future phase** for consistency
3. The saga pattern can serve as the template for eventually modernizing aggregate discovery

---

## Gap 1: ISagaState Interface Design

### Issues

1. **Mutable methods on immutable interface**

   ```csharp
   public interface ISagaState
   {
       ISagaState ApplyPhase(SagaPhase phase);
       ISagaState ApplySagaStarted(...);
       ISagaState ApplyStepProgress(...);
   }
   ```

   These "apply" methods on the interface break the fundamental principle that **all state changes go through events → reducers**.

2. **Reducers should handle state transitions, not the state itself**

   In the aggregate pattern, `BankAccountAggregate` doesn't have methods like `ApplyDeposit()` - that's what reducers are for.

### Recommendation

Remove apply methods from `ISagaState`. The interface should only define the tracking properties:

```csharp
public interface ISagaState
{
    Guid SagaId { get; }
    string? CorrelationId { get; }
    SagaPhase Phase { get; }
    int LastCompletedStepIndex { get; }
    int CurrentStepAttempt { get; }
    DateTimeOffset? StartedAt { get; }
    string? StepHash { get; }
    string? LastErrorCode { get; }
    string? LastErrorMessage { get; }
}
```

State transitions happen through infrastructure reducers:

- `SagaStartedReducer<TSaga>` → sets `Phase = Running`, `StartedAt`, `SagaId`, etc.
- `SagaStepCompletedReducer<TSaga>` → increments `LastCompletedStepIndex`
- `SagaCompensatingReducer<TSaga>` → sets `Phase = Compensating`

---

## Gap 2: Saga Input Handling is Underspecified

### The Problem

The spec mentions saga input in several places but never clearly defines how input flows through the system:

1. Where is input stored?
2. How do steps access input?
3. Is input part of saga state or separate?

### Questions Not Answered

1. Is `TransferFundsSagaInput` copied into `TransferFundsSagaState` by a reducer?
2. Does `ISagaContext.GetInput<T>()` retrieve from state or from command?
3. What happens if saga is replayed - where does input come from?

### Recommendation

**Input should be stored in saga state via the initial reducer.**

The `SagaStartedEvent` should include input data, and the `SagaStartedReducer` should copy it into state:

```csharp
// SagaStartedEvent includes the input
[GenerateSerializer]
public sealed record SagaStartedEvent<TInput>
{
    [Id(0)] public required Guid SagaId { get; init; }
    [Id(1)] public required TInput Input { get; init; }
    [Id(2)] public required string StepHash { get; init; }
    [Id(3)] public required DateTimeOffset StartedAt { get; init; }
    [Id(4)] public string? CorrelationId { get; init; }
}

// Reducer extracts input into state
internal sealed class TransferInitiatedReducer 
    : EventReducerBase<SagaStartedEvent<TransferFundsSagaInput>, TransferFundsSagaState>
{
    protected override TransferFundsSagaState ApplyEvent(
        SagaStartedEvent<TransferFundsSagaInput> evt,
        TransferFundsSagaState state) =>
        state with
        {
            SagaId = evt.SagaId,
            SourceAccountId = evt.Input.SourceAccountId,
            DestinationAccountId = evt.Input.DestinationAccountId,
            Amount = evt.Input.Amount,
            Phase = SagaPhase.Running,
            StartedAt = evt.StartedAt,
            StepHash = evt.StepHash,
            CorrelationId = evt.CorrelationId,
        };
}
```

This approach:

- Keeps input in the event stream (auditable)
- Makes replays deterministic (input is in state)
- Avoids magic context methods

---

## Gap 3: Step Discovery Association Problem

### The Problem

Steps and compensations are discovered by attribute, but **how does the generator know which saga they belong to?**

Consider a project with multiple sagas:

```
Domain/
├── Sagas/
│   ├── TransferFunds/
│   │   ├── TransferFundsSagaState.cs       // [GenerateSagaEndpoints]
│   │   ├── DebitSourceStep.cs              // [SagaStep(1)]
│   │   └── CreditDestinationStep.cs        // [SagaStep(2)]
│   └── RefundOrder/
│       ├── RefundOrderSagaState.cs         // [GenerateSagaEndpoints]
│       ├── CreateRefundStep.cs             // [SagaStep(1)]
│       └── NotifyCustomerStep.cs           // [SagaStep(2)]
```

**How does `DebitSourceStep` get associated with `TransferFundsSagaState`?**

The spec says discovery is by attribute (`[SagaStep(1)]`), but the attribute doesn't specify which saga!

### Current Aggregate Pattern

Aggregates solve this via namespace convention:

- `Handlers/DepositFundsHandler` → found in `BankAccountAggregate.Handlers` namespace
- The handler's `TAggregate` type argument (`CommandHandlerBase<TCommand, BankAccountAggregate>`) confirms the association

### Saga Step Pattern Needs

Steps extend `SagaStepBase<TSagaState>`:

```csharp
[SagaStep(1)]
internal sealed class DebitSourceStep : SagaStepBase<TransferFundsSagaState>
```

The **type argument `TransferFundsSagaState`** provides the association!

### Recommendation

**Discovery uses TWO signals:**

1. `[SagaStep(order)]` attribute identifies a step
2. `SagaStepBase<TSaga>` base class identifies which saga it belongs to

Generator pseudo-code:

```csharp
foreach (var type in allTypes)
{
    if (HasAttribute(type, "SagaStepAttribute"))
    {
        var baseType = GetBaseType(type); // SagaStepBase<TSaga>
        var sagaStateType = baseType.TypeArguments[0]; // TransferFundsSagaState
        
        if (!sagaRegistry.ContainsKey(sagaStateType))
            sagaRegistry[sagaStateType] = new SagaInfo(sagaStateType);
        
        sagaRegistry[sagaStateType].AddStep(type, GetStepOrder(type));
    }
}
```

**This is consistent with how aggregate handlers work** (CommandHandlerBase<TCommand, **TAggregate**>).

---

## Gap 4: Business Reducer Discovery is Unclear

### The Problem

For aggregates, reducers are found in `{Aggregate}.Reducers` namespace:

```
BankAccount/
├── Reducers/
│   ├── FundsDepositedReducer.cs    // EventReducerBase<FundsDeposited, BankAccountAggregate>
│   └── AccountOpenedReducer.cs
```

For sagas, the spec says reducers are discovered "by base type" but doesn't explain how to associate reducers with a specific saga.

### Example

```csharp
// Which saga does this reducer belong to?
internal sealed class SourceDebitedReducer 
    : EventReducerBase<SourceDebited, TransferFundsSagaState>
```

The `TransferFundsSagaState` type argument provides the association.

### Recommendation

**Reducer discovery for sagas works the same as for aggregates:**

The generator finds all types extending `EventReducerBase<TEvent, TState>` where `TState` implements `ISagaState`.

```csharp
foreach (var type in allTypes)
{
    var baseType = GetBaseType(type); // EventReducerBase<TEvent, TState>
    if (baseType?.Name == "EventReducerBase`2")
    {
        var eventType = baseType.TypeArguments[0];
        var stateType = baseType.TypeArguments[1];
        
        if (ImplementsInterface(stateType, "ISagaState"))
        {
            sagaRegistry[stateType].AddReducer(type, eventType);
        }
    }
}
```

---

## Gap 5: Missing Infrastructure Reducer Details

### The Problem

The spec mentions infrastructure reducers:

- `SagaStartedReducer<TSaga>`
- `SagaStepCompletedReducer<TSaga>`
- `SagaCompensatingReducer<TSaga>`
- etc.

**But where do these come from and how are they registered?**

### Questions

1. Are they generic reducers in `EventSourcing.Sagas` that work for any saga?
2. Do they need to be generated per saga?
3. How do they interact with business reducers?

### Recommendation

**Infrastructure reducers should be generic and auto-registered.**

```csharp
// In EventSourcing.Sagas package
public sealed class SagaStepCompletedReducer<TSaga> 
    : EventReducerBase<SagaStepCompletedEvent, TSaga>
    where TSaga : class, ISagaState
{
    protected override TSaga ApplyEvent(SagaStepCompletedEvent evt, TSaga state)
    {
        // Use record with expression via interface constraint
        return (TSaga)(object)(state with { LastCompletedStepIndex = evt.StepIndex });
    }
}
```

Wait - **this doesn't work!** The `with` expression requires the specific record type, not an interface.

### Alternative Recommendation

**Each saga needs generated infrastructure reducers**, or **use `ISagaState` Apply methods** (which contradicts the events → reducers principle).

**Better option: Expression-based state mutation via Activator/compiled delegate:**

```csharp
public sealed class SagaStepCompletedReducer<TSaga> 
    : EventReducerBase<SagaStepCompletedEvent, TSaga>
    where TSaga : class, ISagaState
{
    private static readonly Func<TSaga, int, TSaga> Mutator = BuildMutator();
    
    private static Func<TSaga, int, TSaga> BuildMutator()
    {
        // Compile expression: state => state with { LastCompletedStepIndex = value }
        // Using reflection to find the record's with method
    }
    
    protected override TSaga ApplyEvent(SagaStepCompletedEvent evt, TSaga state) =>
        Mutator(state, evt.StepIndex);
}
```

**This is complex.** The simplest solution is:

1. `ISagaState` includes Apply methods (as in current spec)
2. Infrastructure reducers call those methods
3. Document that Apply methods are ONLY for infrastructure use

---

## Gap 6: Effect Orchestration is Underspecified

### The Event Flow Question

task.md says:

> 1. **StartSagaCommand** → `SagaStartedEvent` + `SagaStepStartedEvent`
> 2. **SagaStepStartedEvent** triggers `SagaStepStartedEffect` which executes the step

**But who emits `SagaStepStartedEvent`?**

### Scenario Analysis

```
Command Handler emits:
  - SagaStartedEvent

Reducer applies SagaStartedEvent:
  - state.Phase = Running

Effect reacts to SagaStartedEvent:
  - Emits SagaStepStartedEvent(stepIndex: 0) ← WHO DOES THIS?

Reducer applies SagaStepStartedEvent:
  - (updates current step info)

Effect reacts to SagaStepStartedEvent:
  - Resolves step from registry
  - Calls step.ExecuteAsync()
  - Step returns StepResult with events
  - Effect emits: businessEvents + SagaStepCompletedEvent OR SagaStepFailedEvent
```

### The Problem

Effects can **emit events** (via `IAsyncEnumerable<object>`), but:

1. **Events emitted by effects are persisted** in the same operation
2. **Effects run after reducers** in `GenericAggregateGrain`
3. **If SagaStartedEffect emits SagaStepStartedEvent**, when does `SagaStepStartedEffect` run?

Looking at `GenericAggregateGrain.ExecuteAsync`:

```csharp
// 1. Command handler produces events
var result = await RootCommandHandler.HandleAsync(command, state, ...);
// 2. Events are persisted to brook
// 3. Reducers apply events to state
// 4. Effects run and can emit more events
// 5. Those events are also persisted and processed
```

This is a **recursive** or **chained** effect pattern.

### Recommendation

Clarify the effect chain model in the spec:

1. **SagaStartedCommandHandler** emits `SagaStartedEvent` (only)
2. **SagaStartedEffect** reacts and emits `SagaStepStartedEvent(stepIndex: 0)`
3. **SagaStepStartedEffect** reacts and:
   - Executes step
   - Emits business events + `SagaStepCompletedEvent` OR `SagaStepFailedEvent`
4. **SagaStepCompletedEffect** reacts and:
   - If more steps: emits `SagaStepStartedEvent(stepIndex: N+1)`
   - If final step: emits `SagaCompletedEvent`
5. Loop continues until saga completes or fails

**This requires that effects can trigger more effects within the same grain activation.**

---

## Gap 7: SignalR Projection Key is Unclear

### The Problem

The spec mentions saga status projections via SignalR but doesn't specify:

1. What is the projection key format? `{sagaId}` or `{sagaType}/{sagaId}`?
2. Is there a single `SagaStatusProjection` type or per-saga types?
3. How does the client subscribe to a specific saga instance?

### Current Projection Pattern

Looking at existing projections in the codebase, they use:

- `IProjectionRegistry` for projection type mapping
- SignalR hub for subscription management
- Entity-keyed projections

### Recommendation

**Saga status should use a generic projection with saga type discrimination:**

```csharp
// Single type for all saga statuses
public sealed record SagaStatusProjection
{
    public Guid SagaId { get; init; }
    public string SagaType { get; init; }  // "TransferFunds", "RefundOrder"
    public SagaPhase Phase { get; init; }
    public int CurrentStepIndex { get; init; }
    public int TotalSteps { get; init; }
    public string? CurrentStepName { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
}
```

**Projection key:** `saga:{sagaType}:{sagaId}` (e.g., `saga:transfer-funds:550e8400-e29b-41d4-a716-446655440000`)

**Client subscription:**

```csharp
SubscribeToProjection<SagaStatusProjection>($"saga:transfer-funds:{sagaId}");
```

---

## Gap 8: Step Registry Runtime vs Generation

### The Problem

The spec mentions `ISagaStepRegistry<TSaga>` but doesn't clarify if it's:

1. **Generated** per saga at compile time
2. **Built** at runtime via DI scanning
3. **Configured** manually in registration

### Current Approach Implied

The implementation plan suggests the generator produces registration code:

```csharp
services.AddSaga<TransferFundsSagaState, TransferFundsSagaInput>();
```

But where does step ordering and step hash come from?

### Recommendation

**Step registry should be generated at compile time:**

```csharp
// Generated: TransferFundsSagaStepRegistry.g.cs
public sealed class TransferFundsSagaStepRegistry : ISagaStepRegistry<TransferFundsSagaState>
{
    public string StepHash => "a1b2c3d4..."; // Hash of step types/order
    
    public IReadOnlyList<SagaStepInfo> Steps { get; } = new[]
    {
        new SagaStepInfo(0, "DebitSourceAccountStep", typeof(DebitSourceAccountStep), 
            CompensationType: typeof(RefundSourceAccountCompensation)),
        new SagaStepInfo(1, "CreditDestinationAccountStep", typeof(CreditDestinationAccountStep),
            CompensationType: null),
    };
}
```

This enables:

1. Compile-time validation of step ordering
2. Step hash computation for drift detection
3. Fast runtime step resolution

---

## Developer Experience Concerns

### DX Issue 1: Boilerplate for Simple Sagas

A saga with 2 steps requires:

- 1 input record
- 1 state record (implementing ISagaState with many properties)
- 2 step classes
- 0-2 compensation classes
- N business event records
- N business reducers
- LoggerExtensions class

**This is significant boilerplate** compared to a simple aggregate command.

**Recommendation:** Consider a "simple saga" pattern for linear, compensationless workflows.

### DX Issue 2: ISagaState Property Boilerplate

Every saga state must include:

```csharp
[Id(10)] public Guid SagaId { get; init; }
[Id(11)] public string? CorrelationId { get; init; }
[Id(12)] public SagaPhase Phase { get; init; } = SagaPhase.NotStarted;
[Id(13)] public int LastCompletedStepIndex { get; init; } = -1;
[Id(14)] public int CurrentStepAttempt { get; init; } = 1;
[Id(15)] public DateTimeOffset? StartedAt { get; init; }
[Id(16)] public string? StepHash { get; init; }
// Plus Apply methods...
```

**Recommendation:** Consider a base record or source generator to reduce this.

### DX Issue 3: Step Order is Magic Numbers

```csharp
[SagaStep(1)]  // What if I need to insert a step between 1 and 2?
internal sealed class DebitSourceAccountStep : SagaStepBase<TransferFundsSagaState>
```

**Recommendation:** Allow step ordering alternatives:

```csharp
// Option A: Explicit numeric order (current)
[SagaStep(Order = 1)]

// Option B: Relative ordering
[SagaStep(After = typeof(DebitSourceAccountStep))]

// Option C: Named phases
[SagaStep(Phase = "Debit")]
[SagaStep(Phase = "Credit", After = "Debit")]
```

### DX Issue 4: Testing Sagas is Not Addressed

The spec doesn't discuss:

1. How to unit test individual steps
2. How to integration test saga flows
3. How to test compensation scenarios
4. Test helpers for saga state assertions

**Recommendation:** Add testing patterns to spec:

```csharp
// Unit testing a step
var step = new DebitSourceAccountStep(mockGrainFactory, mockLogger);
var context = SagaTestContext.Create();
var state = new TransferFundsSagaState { Amount = 100 };

var result = await step.ExecuteAsync(context, state, CancellationToken.None);

Assert.True(result.Success);
Assert.Single(result.Events);
Assert.IsType<SourceDebited>(result.Events[0]);
```

---

## Summary of Required Changes

### Must Fix (Blocking Issues)

1. **Remove Apply methods from ISagaState** or clearly document they are infrastructure-only
2. **Clarify saga input storage** - input should flow through SagaStartedEvent into state
3. **Document step/compensation association** - uses base type TAggregate argument
4. **Specify effect orchestration chain** - who emits what, when
5. **Define SignalR projection key format**

### Should Fix (DX Improvements)

6. **Add saga step registry generation** with compile-time step hash
7. **Address ISagaState boilerplate** with base record or generator
8. **Add testing patterns** for steps, compensations, and flows

### Consider (Future Enhancements)

9. **Simple saga pattern** for linear workflows
10. **Relative step ordering** as alternative to numeric
11. **Aggregate discovery migration** to attribute-based (consistency)

---

## Next Steps

1. Update `rfc.md` with design decisions on gaps #1-5
2. Update `implementation-plan.md` with generator changes for step discovery
3. Add `testing-strategy.md` with saga testing patterns
4. Update `verification.md` with new verification questions for gap resolutions
