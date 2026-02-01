# Task 09: Saga Step Delay Effect

## Objective

Add a configurable delay between saga steps for demonstration purposes, specifically to create a visible 10-second gap between the debit and credit steps in the money transfer saga.

## Rationale

The user explicitly requested:
> "add a delay somehow...about 10s between taking the money and adding the money to the accounts so it feels more real time over the wire"

Without a delay, both steps execute in milliseconds—too fast to observe. The delay makes the saga progression tangible and creates a compelling demo.

## Approach Options

### Option A: Step-Level Delay Attribute (Recommended)

Add a `[DelayAfterStep]` attribute that the saga orchestrator respects. Clean, declarative, no production code changes.

### Option B: In-Step Delay

Add `Task.Delay()` inside the step's `ExecuteAsync`. Simple but couples demo behavior to step logic.

### Option C: Effect-Based Delay

Create a dedicated `DelayEffect` that fires between steps. More complex but follows existing effect patterns.

**Recommendation:** Option A provides the cleanest separation of concerns and is most "framework-like".

## Deliverables

### 1. `DelayAfterStepAttribute.cs`

**Location:** `src/EventSourcing.Sagas.Abstractions/DelayAfterStepAttribute.cs`

```csharp
namespace Mississippi.EventSourcing.Sagas;

/// <summary>
/// Specifies a delay to wait after this step completes before proceeding to the next step.
/// Useful for demonstrations or rate-limiting saga progression.
/// </summary>
/// <remarks>
/// The delay is applied after successful step execution but before the next step begins.
/// During compensation, delays are NOT applied to maintain quick rollback.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DelayAfterStepAttribute : Attribute
{
    /// <summary>
    /// Gets the delay duration in milliseconds.
    /// </summary>
    public int DelayMilliseconds { get; }

    /// <summary>
    /// Gets the delay as a TimeSpan.
    /// </summary>
    public TimeSpan Delay => TimeSpan.FromMilliseconds(DelayMilliseconds);

    /// <summary>
    /// Initializes a new instance with the specified delay in milliseconds.
    /// </summary>
    /// <param name="delayMilliseconds">The delay in milliseconds.</param>
    public DelayAfterStepAttribute(int delayMilliseconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(delayMilliseconds);
        DelayMilliseconds = delayMilliseconds;
    }
}
```

### 2. Update `SagaOrchestrator.cs`

**Location:** `src/EventSourcing.Sagas/SagaOrchestrator.cs`

Add delay handling after step execution:

```csharp
// In the step execution flow, after step.ExecuteAsync succeeds:

private async Task ApplyPostStepDelayAsync(
    Type stepType, 
    CancellationToken cancellationToken)
{
    DelayAfterStepAttribute? delayAttr = stepType
        .GetCustomAttribute<DelayAfterStepAttribute>();
    
    if (delayAttr is not null && delayAttr.DelayMilliseconds > 0)
    {
        Logger.ApplyingPostStepDelay(stepType.Name, delayAttr.DelayMilliseconds);
        
        await Task.Delay(delayAttr.Delay, cancellationToken);
        
        Logger.PostStepDelayCompleted(stepType.Name);
    }
}
```

### 3. Logger Extensions

**Location:** `src/EventSourcing.Sagas/LoggerExtensions.cs`

```csharp
[LoggerMessage(
    EventId = 2010,
    Level = LogLevel.Debug,
    Message = "Applying post-step delay of {DelayMs}ms after step {StepName}")]
public static partial void ApplyingPostStepDelay(
    this ILogger logger, 
    string stepName, 
    int delayMs);

[LoggerMessage(
    EventId = 2011,
    Level = LogLevel.Debug,
    Message = "Post-step delay completed for step {StepName}")]
public static partial void PostStepDelayCompleted(
    this ILogger logger, 
    string stepName);
```

### 4. Apply to Transfer Saga Steps

**Location:** `samples/Spring/Spring.Domain/Sagas/TransferFunds/Steps/DebitSourceAccountStep.cs`

```csharp
using Mississippi.EventSourcing.Sagas;

namespace Spring.Domain.Sagas.TransferFunds.Steps;

[DelayAfterStep(10_000)] // 10 second delay after debit
public sealed class DebitSourceAccountStep : SagaStepBase<TransferFundsSaga>
{
    // ... existing implementation
}
```

**Note:** Only the `DebitSourceAccountStep` gets the delay. The `CreditDestinationAccountStep` does NOT need a delay—it completes immediately after the 10-second wait following the debit.

### 5. TimeProvider Integration (Testability)

For testability, inject `TimeProvider` into the orchestrator rather than using `Task.Delay` directly:

```csharp
public sealed partial class SagaOrchestrator : ISagaOrchestrator
{
    private TimeProvider TimeProvider { get; }
    
    public SagaOrchestrator(
        // existing parameters...
        TimeProvider? timeProvider = null)
    {
        TimeProvider = timeProvider ?? TimeProvider.System;
    }
    
    private async Task ApplyPostStepDelayAsync(
        Type stepType, 
        CancellationToken cancellationToken)
    {
        DelayAfterStepAttribute? delayAttr = stepType
            .GetCustomAttribute<DelayAfterStepAttribute>();
        
        if (delayAttr is not null && delayAttr.DelayMilliseconds > 0)
        {
            Logger.ApplyingPostStepDelay(stepType.Name, delayAttr.DelayMilliseconds);
            
            await TimeProvider.Delay(delayAttr.Delay, cancellationToken);
            
            Logger.PostStepDelayCompleted(stepType.Name);
        }
    }
}
```

## Alternative: Development-Only Delay

If delays should ONLY apply in development, use configuration:

```csharp
[DelayAfterStep(10_000, DevelopmentOnly = true)]
```

With orchestrator checking:

```csharp
if (delayAttr.DevelopmentOnly && !HostEnvironment.IsDevelopment())
    return;
```

**For this demo, we'll keep it simple and always apply the delay.**

## Acceptance Criteria

- [ ] `DelayAfterStepAttribute` exists in `EventSourcing.Sagas.Abstractions`
- [ ] Attribute accepts delay in milliseconds
- [ ] `SagaOrchestrator` reads attribute and applies delay after step execution
- [ ] Delay uses `TimeProvider` for testability
- [ ] Delays NOT applied during compensation (rollback should be fast)
- [ ] Logger messages for delay start/end
- [ ] `DebitSourceAccountStep` decorated with `[DelayAfterStep(10_000)]`
- [ ] End-to-end demo shows 10-second gap between balance changes

## Demo Verification

1. Start transfer from Alice to Bob
2. Observe in Account Watch:
   - T+0: Alice balance decreases (debit step completes)
   - T+10s: Bob balance increases (credit step completes)
3. Saga status updates through: Initiated → SourceDebited → (10s wait) → DestinationCredited → Completed

## Test Cases

### Unit Tests (`SagaOrchestrator.L0Tests`)

```csharp
[Fact]
public async Task ExecuteStep_WithDelayAttribute_WaitsBeforeNextStep()
{
    // Arrange
    var fakeTime = new FakeTimeProvider();
    var orchestrator = CreateOrchestrator(timeProvider: fakeTime);
    
    // Act
    var executeTask = orchestrator.ExecuteAsync(saga, cancellationToken);
    
    // Assert - step 1 completed
    // Advance time by 10 seconds
    fakeTime.Advance(TimeSpan.FromSeconds(10));
    
    // Assert - step 2 now executes
    await executeTask;
}

[Fact]
public async Task ExecuteStep_WithDelayAttribute_DoesNotDelayDuringCompensation()
{
    // Arrange - saga that will fail and compensate
    
    // Assert - compensation runs without delays
}
```

## File Changes Summary

| File | Change |
|------|--------|
| `src/EventSourcing.Sagas.Abstractions/DelayAfterStepAttribute.cs` | New |
| `src/EventSourcing.Sagas/SagaOrchestrator.cs` | Add delay handling |
| `src/EventSourcing.Sagas/LoggerExtensions.cs` | Add delay log messages |
| `samples/Spring/Spring.Domain/.../DebitSourceAccountStep.cs` | Add attribute |
| `tests/EventSourcing.Sagas.L0Tests/...` | Add delay tests |

## Dependencies

- Saga orchestrator infrastructure (existing)
- TimeProvider injection (existing pattern in repo)

## Blocked By

- [05-domain-saga](05-domain-saga.md) (need the steps to decorate)

## Blocks

- [10-integration-testing](10-integration-testing.md) (tests need to account for delay)
