---
applyTo: '**/*.cs'
---

# Orleans Best Practices for Mississippi Framework

This document defines the Orleans development standards and best practices for the Mississippi Framework. All Orleans grains and related code must follow these guidelines to ensure consistency, maintainability, and alignment with modern Orleans patterns.

## Core Principles

### 1. NEVER Inherit from Grain ❌

**CRITICAL RULE**: Never inherit from the `Grain` base class. Always use the POCO (Plain Old CLR Object) grain pattern with `IGrainBase`.

#### Why This Rule Exists

- **Composition over Inheritance**: POCO grains allow you to inherit from your own domain base classes or compose multiple concerns via interfaces/mix-ins
- **Testability**: POCO grains are easier to unit test and mock
- **Flexibility**: Complete freedom from the Orleans hierarchy while keeping every runtime capability intact
- **Modern Orleans**: This is the recommended pattern for Orleans 7.0+ and aligns with modern .NET practices

#### Correct POCO Grain Pattern ✅

```csharp
using Orleans;
using Orleans.Runtime;          // For extension methods
using Orleans.Streams;          // For stream operations
using Microsoft.Extensions.Logging;

public interface ITodoGrain : IGrainWithStringKey
{
    Task Add(string item);
    ValueTask<IReadOnlyList<string>> Get();
}

public sealed class TodoGrain : IGrainBase, ITodoGrain, IRemindable
{
    public IGrainContext GrainContext { get; }                                // IGrainBase requirement

    private ILogger<TodoGrain> Logger { get; }
    private IPersistentState<TodoState> State { get; }
    private IDisposable? _flushTimer;
    private IGrainReminder? _dailyReminder;

    public TodoGrain(
        IGrainContext ctx,
        ILogger<TodoGrain> log,
        [PersistentState("todo")] IPersistentState<TodoState> state)
    {
        GrainContext = ctx;
        Logger = log;
        State = state;
    }

    /* ---------------- ITodoGrain Implementation ---------------- */

    public async Task Add(string item)
    {
        State.State.Items.Add(item);
        await State.WriteStateAsync();
    }

    public ValueTask<IReadOnlyList<string>> Get()
        => ValueTask.FromResult((IReadOnlyList<string>)State.State.Items);

    /* ------------- Lifecycle Methods (same signatures as Grain) ------------- */

    public async Task OnActivateAsync(CancellationToken ct)
    {
        // Use LoggerExtensions + LoggerMessage pattern per logging rules
        GrainLoggerExtensions.Activated(Logger, this.GetPrimaryKey().ToString());

        // 1-minute in-memory timer
        _flushTimer = this.RegisterGrainTimer(
            async (_, token) => await FlushAsync(token),
            state: null,
            options: new GrainTimerCreationOptions
            {
                DueTime  = TimeSpan.FromMinutes(1),
                Period   = TimeSpan.FromMinutes(1),
                KeepAlive = true
            });

        // day-boundary persistent reminder
        _dailyReminder = await this.RegisterOrUpdateReminder(
            "daily-flush",
            TimeSpan.Zero,
            TimeSpan.FromDays(1));
    }

    public async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
    {
        _flushTimer?.Dispose();
        GrainLoggerExtensions.Deactivating(Logger, this.GetPrimaryKey().ToString(), reason.ToString());
        await State.WriteStateAsync();
    }

    /* ---------------- Reminders ---------------- */

    async Task IRemindable.ReceiveReminder(string name, TickStatus status)
    {
        if (name == "daily-flush") await FlushAsync(CancellationToken.None);
    }

    /* ---------------- Helpers ---------------- */

    private Task FlushAsync(CancellationToken _)
    {
        GrainLoggerExtensions.Flushed(Logger, State.State.Items.Count);
        return Task.CompletedTask;
    }

    // Optional: call this.DeactivateOnIdle() whenever you like
}

/* ------------- State Type ------------- */
public record TodoState
{
    public List<string> Items { get; init; } = new();
}
```

### 2. Required Using Statements

Always include these using statements for POCO grains:

```csharp
using Orleans;                    // Core Orleans types
using Orleans.Runtime;           // Extension methods (GetPrimaryKey, DeactivateOnIdle, etc.)
using Orleans.Streams;           // Stream operations
using Microsoft.Extensions.Logging; // Logging
```

### 3. Extension Method Usage

All the familiar Orleans helpers are available as extension methods on `this`:

| Old Grain Inheritance | POCO Grain Extension Method |
|----------------------|------------------------------|
| `GetPrimaryKey()` | `this.GetPrimaryKey()` |
| `DeactivateOnIdle()` | `this.DeactivateOnIdle()` |
| `RegisterOrUpdateReminder()` | `this.RegisterOrUpdateReminder()` |
| `RegisterGrainTimer()` | `this.RegisterGrainTimer()` |
| `GetStreamProvider()` | `this.GetStreamProvider()` |

### 4. Migration Checklist

When converting from `Grain` inheritance to POCO pattern:

- [ ] Add `IGrainBase` interface implementation
- [ ] Add `public IGrainContext GrainContext { get; }` property
- [ ] Inject `IGrainContext` in constructor
- [ ] Add `using Orleans.Runtime;` for extension methods
- [ ] Replace direct method calls with `this.` qualified extension methods
- [ ] If using `Grain<TState>`, inject `IPersistentState<TState>` instead
- [ ] Update any inheritance hierarchy to use your own base classes
- [ ] For each grain still inheriting from `Grain`, create a `.scratchpad/tasks/pending` item to track migration to POCO (`IGrainBase`) if not addressed immediately (see Agent Scratchpad)

### 5. Dependency Injection and Property Pattern

**CRITICAL RULE**: All dependency-injected services must follow the get-only property pattern consistent with logging best practices.

#### Property Declaration Pattern ✅

Use `private Type Name { get; }` properties for all injected dependencies:

```csharp
public class TodoGrain : IGrainBase, ITodoGrain
{
    public IGrainContext GrainContext { get; }              // Orleans infrastructure
    private ILogger<TodoGrain> Logger { get; }              // Logging
    private IPersistentState<TodoState> State { get; }      // State management
    private IMyCustomService CustomService { get; }         // Custom services
    private IOptions<MyOptions> Options { get; }            // Configuration
    
    public TodoGrain(
        IGrainContext ctx,
        ILogger<TodoGrain> logger,
        [PersistentState("todo")] IPersistentState<TodoState> state,
        IMyCustomService customService,
        IOptions<MyOptions> options)
    {
        GrainContext = ctx;
        Logger = logger;
        State = state;
        CustomService = customService;
        Options = options;
    }
}
```

#### Anti-Pattern: Private Readonly Fields ❌

```csharp
// DO NOT DO THIS - VIOLATES PROPERTY PATTERN
public class BadGrain : IGrainBase, IBadGrain
{
    private readonly ILogger<BadGrain> _logger;      // Should be property
    private readonly IMyService _service;            // Should be property
    
    // This pattern should be avoided
}
```

#### Why Use Properties Over Fields

- **Consistency**: Aligns with logging best practices across the framework
- **Accessibility**: Properties can be accessed by derived classes if needed
- **Debugging**: Properties are more accessible in debuggers and reflection
- **Testing**: Easier to mock and verify in unit tests
- **Standards**: Follows enterprise coding standards for dependency injection

### 6. Dependency Injection

POCO grains support full dependency injection in constructors:

```csharp
public TodoGrain(
    IGrainContext ctx,                                    // Orleans infrastructure
    ILogger<TodoGrain> log,                              // Logging
    [PersistentState("todo")] IPersistentState<TodoState> state,  // State management
    IMyCustomService customService,                      // Your custom services
    IOptions<MyOptions> options)                         // Configuration
{
    GrainContext = ctx;
    Logger = log;
    State = state;
    CustomService = customService;
    Options = options;
}
```

### 7. State Management

For persistent state, use `IPersistentState<T>` instead of inheriting from `Grain<TState>`:

```csharp
// Instead of: public class MyGrain : Grain<MyState>
public class MyGrain : IGrainBase, IMyGrain
{
    private IPersistentState<MyState> State { get; }
    
    public MyGrain(
        IGrainContext ctx,
        [PersistentState("mystate")] IPersistentState<MyState> state)
    {
        GrainContext = ctx;
        State = state;
    }
    
    // Access state via State.State
    // Persist via await State.WriteStateAsync()
}
```

### 8. Testing POCO Grains

POCO grains are much easier to test:

```csharp
[Fact]
public async Task Add_ShouldAddItemToState()
{
    // Arrange
    var mockContext = new Mock<IGrainContext>();
    var mockLogger = new Mock<ILogger<TodoGrain>>();
    var mockState = new Mock<IPersistentState<TodoState>>();
    mockState.Setup(s => s.State).Returns(new TodoState());
    
    var grain = new TodoGrain(mockContext.Object, mockLogger.Object, mockState.Object);
    
    // Act
    await grain.Add("test item");
    
    // Assert
    mockState.Verify(s => s.WriteStateAsync(), Times.Once);
    Assert.Contains("test item", mockState.Object.State.Items);
}
```

### 9. Common Gotchas

1. **Missing using Orleans.Runtime**: Build errors for extension methods
2. **Forgot this. qualification**: Extension methods must be called with `this.`
3. **Old Grain with TState patterns**: Use `IPersistentState<T>` instead
4. **Constructor injection**: All dependencies must be injected, including `IGrainContext`
5. **Using private readonly fields**: Use `private Type Name { get; }` properties instead

### 10. Performance Considerations

- POCO grains have the same performance characteristics as inherited grains
- Extension methods are resolved at compile time
- No runtime overhead from the inheritance pattern
- Better memory layout due to composition over inheritance

### 11. Code Review Checklist

When reviewing grain implementations:

- [ ] Does NOT inherit from `Grain` ❌
- [ ] Implements `IGrainBase` ✅
- [ ] Has `IGrainContext GrainContext { get; }` property ✅
- [ ] Uses extension methods with `this.` qualification ✅
- [ ] Includes `using Orleans.Runtime;` ✅
- [ ] Uses get-only properties for all injected dependencies ✅
- [ ] Proper dependency injection in constructor ✅
- [ ] Follows naming conventions ✅
- [ ] Has appropriate unit tests ✅

## Examples

### Good Example: POCO Grain Pattern ✅

```csharp
internal class ExampleGrain : IExampleGrain, IGrainBase
{
    public IGrainContext GrainContext { get; }
    
    private IExampleService ExampleService { get; }
    private IOptions<ExampleOptions> Options { get; }
    
    public ExampleGrain(
        IExampleService exampleService,
        IOptions<ExampleOptions> options,
        IGrainContext grainContext)
    {
        ExampleService = exampleService;
        Options = options;
        GrainContext = grainContext;
    }
    
    // Uses this.GetPrimaryKeyString() extension method
    public async Task<string> GetDataAsync()
    {
        var grainId = this.GetPrimaryKeyString();
        // ... rest of implementation
    }
}
```

### Anti-Pattern: Grain Inheritance ❌

```csharp
// DO NOT DO THIS - VIOLATES RULE
public abstract class BadGrain<TModel> : Grain, IExampleGrain<TModel>
{
    // This pattern should be avoided - use IGrainBase instead
}
```

## Enforcement

This rule should be enforced through:

1. **Code Reviews**: Always check for `Grain` inheritance
2. **Static Analysis**: Use .NET analyzers to detect violations (aligns with zero warnings policy)
3. **Documentation**: Keep this file updated with examples
4. **Refactoring**: Gradually migrate existing violations


## References

- [Orleans Migration Guide](https://learn.microsoft.com/en-us/dotnet/orleans/migration-guide)
- [Orleans Timers and Reminders](https://learn.microsoft.com/en-us/dotnet/orleans/grains/timers-and-reminders)
- [Orleans POCO Grains](https://learn.microsoft.com/en-us/dotnet/orleans/grains/poco-grains)
