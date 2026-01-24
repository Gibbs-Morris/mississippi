# Design Review: Developer Perspective

**Reviewer Role:** Developer (focused on developer experience)  
**Date:** 2026-01-24  
**RFC:** Server-Side Event Effects

---

## Overall Assessment: 👍 Positive with Suggestions

The design follows familiar patterns and should be easy for developers to adopt. However, there are opportunities to improve discoverability, debugging, and testing ergonomics.

---

## What I Like ✅

### 1. Consistent Pattern with Handlers/Reducers
The folder structure (`Effects/` alongside `Handlers/` and `Reducers/`) is intuitive:
```
BankAccount/
  Commands/
  Events/
  Handlers/
  Reducers/
  Effects/        ← Same pattern, easy to discover
```

### 2. Simple Base Class
`EventEffectBase<TEvent, TAggregate>` mirrors `CommandHandlerBase` and `EventReducerBase`. Developers already know this pattern:
```csharp
internal sealed class AccountOpenedEffect : EventEffectBase<AccountOpened, BankAccountAggregate>
{
    protected override async Task HandleAsync(AccountOpened eventData, string aggregateKey, CancellationToken ct)
    {
        // Just implement this
    }
}
```

### 3. Dependency Injection Support
Effects can inject any service, which feels natural:
```csharp
public AccountOpenedEffect(IEmailService emailService, ILogger<AccountOpenedEffect> logger)
```

### 4. Source Generator Auto-Registration
Developers don't need to manually wire up effects in DI—the generator finds them automatically by convention.

---

## Concerns & Suggestions 🤔

### 1. **Debugging Fire-and-Forget is Hard**

**Problem:** With `[OneWay]` fire-and-forget, when an effect fails:
- No exception reaches the caller
- No indication the effect even ran
- Developers debugging "why didn't my email send?" will have a hard time

**Suggestion: Add a "development mode" option**
```csharp
// appsettings.Development.json
{
  "Mississippi": {
    "Effects": {
      "AwaitInDevelopment": true  // Await effects in dev, fire-and-forget in prod
    }
  }
}
```

Or at minimum, ensure **structured logging with correlation IDs** is prominent in the docs so developers can trace effect execution.

### 2. **No Way to Know if Effects Exist for an Event**

**Problem:** When writing a command handler, I don't know if my event will trigger effects. This could lead to surprises:
- "I added an effect but nothing happens" (forgot the namespace convention)
- "I removed an event but effects still reference it" (compile error, but confusing)

**Suggestion: Add an analyzer warning**
```
// Warning: Event 'AccountOpened' has no registered effects. 
// If this is intentional, suppress with [NoEffectsExpected].
```

Or alternatively, add XML doc hints:
```csharp
/// <summary>Account was opened.</summary>
/// <effects>
///   <effect type="AccountOpenedEffect" />
/// </effects>
public sealed record AccountOpened(...);
```

### 3. **Testing Effects is Unclear**

**Problem:** How do I unit test an effect? The RFC doesn't show testing patterns.

**Suggestion: Add testing guidance and helper**
```csharp
// Test helper
public sealed class TestEffectContext
{
    public List<object> DispatchedCommands { get; } = [];
    
    public IAggregateCommandGateway CreateMockGateway() => ...;
}

// Usage in test
[Fact]
public async Task AccountOpenedEffect_SendsWelcomeEmail()
{
    var emailService = Substitute.For<IEmailService>();
    var effect = new AccountOpenedEffect(emailService, NullLogger.Instance);
    
    await effect.HandleAsync(new AccountOpened("John"), "acc-123", CancellationToken.None);
    
    await emailService.Received(1).SendWelcomeEmailAsync("John", Arg.Any<CancellationToken>());
}
```

### 4. **"aggregateKey" Parameter Naming is Confusing**

**Problem:** I see `aggregateKey` but I'm not sure what it is. Is it the same as `brookKey`? The entity ID?

**Suggestion:** Use consistent naming or add XML doc clarity:
```csharp
/// <param name="entityId">The unique identifier for the aggregate instance (e.g., "acc-123").</param>
Task HandleAsync(TEvent eventData, string entityId, CancellationToken cancellationToken);
```

Or better, create a context object:
```csharp
public sealed record EffectContext(
    string AggregateKey,
    string BrookName,
    long EventPosition,
    DateTimeOffset Timestamp
);

Task HandleAsync(TEvent eventData, EffectContext context, CancellationToken cancellationToken);
```

### 5. **No Retry/Resilience Story**

**Problem:** If my email API fails, the effect just... fails. No retry. The RFC says "users use Polly via DI" but doesn't show how.

**Suggestion: Provide a code example**
```csharp
// Show in docs how to use Polly
public sealed class ResilientAccountOpenedEffect : EventEffectBase<AccountOpened, BankAccountAggregate>
{
    private ResiliencePipeline Pipeline { get; }
    
    protected override Task HandleAsync(AccountOpened eventData, string key, CancellationToken ct)
    {
        return Pipeline.ExecuteAsync(async token => 
        {
            await EmailService.SendWelcomeEmailAsync(eventData.HolderName, token);
        }, ct);
    }
}
```

### 6. **EffectRegistry Uses Reflection at Runtime**

**Problem:** `AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())` is slow and could fail in trimmed/AOT scenarios.

**Suggestion:** Have the source generator also register a type mapping:
```csharp
// Generated
services.AddAggregateEffectMapping<BankAccountAggregate>("Spring.Domain.Aggregates.BankAccount.BankAccountAggregate");
```

This pre-registers the type name → Type mapping at compile time.

---

## Missing from the Design 📋

| Gap | Impact | Recommendation |
|-----|--------|----------------|
| Effect execution order | Medium | Document that order is undefined, or add `[EffectOrder(1)]` |
| Effect timeout | Low | Effects can hang forever; add optional timeout |
| Effect metadata/context | Medium | Provide `EffectContext` with timestamp, position, etc. |
| IntelliSense / templates | Low | Add item template for `dotnet new effect` |
| Error action pattern | Medium | Show pattern for effects to report failures back to aggregate |

---

## Testing Checklist for Implementation

- [ ] Can I create an effect with just `EventEffectBase` inheritance?
- [ ] Does the generator find my effect without manual registration?
- [ ] Can I see logs when my effect runs (locally)?
- [ ] Can I unit test my effect in isolation?
- [ ] Can I integration test effect + aggregate together?
- [ ] Is IntelliSense helpful when implementing `HandleAsync`?

---

## Verdict

**Ready to proceed** with the implementation, but I'd like to see:
1. Testing examples in documentation
2. Logging/debugging story clarified
3. Consider `EffectContext` for richer metadata

The fire-and-forget model is the right choice for production, but developer experience during debugging needs attention.
