---
id: testing
title: Testing Reservoir
sidebar_label: Testing
sidebar_position: 11
description: Test Reservoir reducers and effects in isolation using the StoreTestHarness with Given/When/Then fluent API.
---

# Testing Reservoir

## Overview

The `Mississippi.Reservoir.Testing` package provides a fluent test harness for testing Reservoir actions, reducers, and effects in isolation. It supports Given/When/Then style scenarios without requiring the full Store infrastructure.

## Installation

Add the testing package to your test project:

```xml
<PackageReference Include="Mississippi.Reservoir.Testing" />
```

## Quick Start

```csharp
using Mississippi.Reservoir.Testing;

[Fact]
public void IncrementAction_IncreasesCounter()
{
    // Arrange - Create harness with reducers
    var harness = StoreTestHarnessFactory.ForFeature<CounterState>()
        .WithReducer<IncrementAction>(CounterReducers.Increment);
    
    // Act & Assert - Given/When/Then
    harness.CreateScenario()
        .Given(new CounterState { Count = 5 })
        .When(new IncrementAction())
        .ThenState(state => state.Count.Should().Be(6));
}
```

## Core Components

### StoreTestHarnessFactory

Entry point for creating test harnesses. Use `ForFeature<TState>()` to create a harness for your feature state type:

```csharp
var harness = StoreTestHarnessFactory.ForFeature<NavigationState>();
```

### StoreTestHarness&lt;TState&gt;

Configures reducers, effects, and services for testing. All methods return the harness for fluent chaining.

| Method | Purpose |
|--------|---------|
| `WithReducer<TAction>(reducer)` | Registers a reducer delegate |
| `WithEffect<TEffect>()` | Registers an effect by type (DI-resolved) |
| `WithEffect(instance)` | Registers an effect instance directly |
| `WithService<TService>(instance)` | Registers a service for effect DI |
| `WithInitialState(state)` | Sets the default initial state for scenarios |
| `CreateScenario()` | Creates a new scenario for Given/When/Then testing |

### StoreScenario&lt;TState&gt;

Executes the Given/When/Then pattern. Scenarios are disposable and should be wrapped in `using`.

| Method | Purpose |
|--------|---------|
| `Given(actions...)` | Applies actions through reducers to establish initial state |
| `GivenState(state)` | Sets state directly without running reducers |
| `When(action)` | Dispatches an action through reducers and effects |
| `WhenAsync(action)` | Async version for testing async effects |
| `ThenState(assertion)` | Asserts on the resulting state |
| `ThenEmits<TAction>(assertion?)` | Asserts an action was emitted by effects |
| `ThenEmitsNothing()` | Asserts no actions were emitted |

## Testing Reducers

Reducers are pure functions—given the same state and action, they always return the same result. This makes them easy to test:

```csharp
[Fact]
public void SetValueAction_UpdatesValue()
{
    // Arrange
    var harness = StoreTestHarnessFactory.ForFeature<MyState>()
        .WithReducer<SetValueAction>((state, action) => state with { Value = action.Value });
    
    // Act & Assert
    using var scenario = harness.CreateScenario();
    scenario
        .Given(new MyState { Value = "old" })
        .When(new SetValueAction("new"))
        .ThenState(state => state.Value.Should().Be("new"));
}
```

### Testing Multiple Reducers

Register multiple reducers and chain actions:

```csharp
[Fact]
public void MultipleActions_ApplySequentially()
{
    var harness = StoreTestHarnessFactory.ForFeature<CounterState>()
        .WithReducer<IncrementAction>((s, _) => s with { Count = s.Count + 1 })
        .WithReducer<DecrementAction>((s, _) => s with { Count = s.Count - 1 })
        .WithReducer<ResetAction>((s, _) => s with { Count = 0 });
    
    using var scenario = harness.CreateScenario();
    scenario
        .GivenState(new CounterState { Count = 10 })
        .When(new IncrementAction())    // 11
        .When(new IncrementAction())    // 12
        .When(new DecrementAction())    // 11
        .ThenState(s => s.Count.Should().Be(11));
}
```

### Using Given to Establish State

Use `Given` to apply actions through reducers to reach a specific state:

```csharp
[Fact]
public void ResetAction_ClearsCounter()
{
    var harness = StoreTestHarnessFactory.ForFeature<CounterState>()
        .WithReducer<IncrementAction>((s, _) => s with { Count = s.Count + 1 })
        .WithReducer<ResetAction>((s, _) => s with { Count = 0 });
    
    using var scenario = harness.CreateScenario();
    scenario
        .Given(new IncrementAction(), new IncrementAction(), new IncrementAction())  // Count = 3
        .When(new ResetAction())
        .ThenState(s => s.Count.Should().Be(0));
}
```

## Testing Effects

Effects handle async side effects and may emit new actions. The harness captures emitted actions for assertions.

### Testing Effect Action Emissions

```csharp
[Fact]
public void SubmitFormAction_EmitsSuccessOrFailure()
{
    // Arrange - register effect instance
    var mockService = Substitute.For<IFormService>();
    mockService.SubmitAsync(Arg.Any<FormData>()).Returns(Task.FromResult(true));
    
    var harness = StoreTestHarnessFactory.ForFeature<FormState>()
        .WithEffect(new SubmitFormEffect(mockService));
    
    // Act
    using var scenario = harness.CreateScenario();
    scenario.When(new SubmitFormAction(new FormData { Name = "Test" }));
    
    // Assert - effect emitted success action
    scenario.ThenEmits<FormSubmittedAction>(a => a.Success.Should().BeTrue());
}
```

### Testing Effects with Dependencies

Register services for constructor injection:

```csharp
[Fact]
public async Task NavigationEffect_NavigatesToUri()
{
    // Arrange
    var mockNavManager = new TestableNavigationManager();
    
    var harness = StoreTestHarnessFactory.ForFeature<NavigationState>()
        .WithService(mockNavManager as NavigationManager)
        .WithEffect<NavigationEffect>();
    
    // Act
    using var scenario = harness.CreateScenario();
    await scenario.WhenAsync(new NavigateAction("/dashboard"));
    
    // Assert
    mockNavManager.NavigatedUri.Should().Be("/dashboard");
}
```

### Asserting No Emissions

Use `ThenEmitsNothing()` to verify effects don't emit actions:

```csharp
[Fact]
public void UnhandledAction_EmitsNothing()
{
    var harness = StoreTestHarnessFactory.ForFeature<MyState>()
        .WithEffect(new MyEffect());  // Effect only handles SpecificAction
    
    using var scenario = harness.CreateScenario();
    scenario
        .When(new UnrelatedAction())
        .ThenEmitsNothing();
}
```

## Complete Example

Here's a complete example testing a login feature:

```csharp
// Feature State
public sealed record LoginState : IFeatureState
{
    public static string FeatureKey => "login";
    public bool IsLoading { get; init; }
    public string? Username { get; init; }
    public string? Error { get; init; }
}

// Actions
public sealed record LoginAction(string Username, string Password) : IAction;
public sealed record LoginSucceededAction(string Username) : IAction;
public sealed record LoginFailedAction(string Error) : IAction;

// Reducers
public static class LoginReducers
{
    public static LoginState OnLogin(LoginState state, LoginAction _) 
        => state with { IsLoading = true, Error = null };
    
    public static LoginState OnSuccess(LoginState state, LoginSucceededAction action) 
        => state with { IsLoading = false, Username = action.Username };
    
    public static LoginState OnFailed(LoginState state, LoginFailedAction action) 
        => state with { IsLoading = false, Error = action.Error };
}

// Tests
public sealed class LoginTests
{
    private StoreTestHarness<LoginState> CreateHarness() =>
        StoreTestHarnessFactory.ForFeature<LoginState>()
            .WithReducer<LoginAction>(LoginReducers.OnLogin)
            .WithReducer<LoginSucceededAction>(LoginReducers.OnSuccess)
            .WithReducer<LoginFailedAction>(LoginReducers.OnFailed);
    
    [Fact]
    public void LoginAction_SetsLoadingState()
    {
        using var scenario = CreateHarness().CreateScenario();
        scenario
            .GivenState(new LoginState())
            .When(new LoginAction("user", "pass"))
            .ThenState(s =>
            {
                s.IsLoading.Should().BeTrue();
                s.Error.Should().BeNull();
            });
    }
    
    [Fact]
    public void LoginSucceeded_ClearsLoadingAndSetsUsername()
    {
        using var scenario = CreateHarness().CreateScenario();
        scenario
            .Given(new LoginAction("testuser", "password"))  // Sets IsLoading = true
            .When(new LoginSucceededAction("testuser"))
            .ThenState(s =>
            {
                s.IsLoading.Should().BeFalse();
                s.Username.Should().Be("testuser");
            });
    }
    
    [Fact]
    public void LoginFailed_ClearsLoadingAndSetsError()
    {
        using var scenario = CreateHarness().CreateScenario();
        scenario
            .Given(new LoginAction("user", "wrong"))
            .When(new LoginFailedAction("Invalid credentials"))
            .ThenState(s =>
            {
                s.IsLoading.Should().BeFalse();
                s.Error.Should().Be("Invalid credentials");
            });
    }
}
```

## Best Practices

### 1. One Scenario Per Test

Create a fresh scenario for each test to ensure isolation:

```csharp
[Fact]
public void Test1()
{
    using var scenario = harness.CreateScenario();
    // ...
}

[Fact]
public void Test2()
{
    using var scenario = harness.CreateScenario();  // Fresh scenario
    // ...
}
```

### 2. Use `GivenState` for Direct State Setup

When you need specific state values without caring how they got there:

```csharp
scenario.GivenState(new MyState 
{ 
    Items = [item1, item2, item3],
    SelectedIndex = 1
});
```

### 3. Use `Given` to Test State Transitions

When you want to verify the path to a state matters:

```csharp
scenario
    .Given(new AddItemAction(item1))
    .Given(new SelectItemAction(0))
    .When(new DeleteSelectedAction())
    .ThenState(s => s.Items.Should().BeEmpty());
```

### 4. Dispose Scenarios

Always use `using` to dispose scenarios and release DI containers:

```csharp
using var scenario = harness.CreateScenario();
```

### 5. Use Deterministic Time in Effects

Inject `FakeTimeProvider` for time-dependent logic:

```csharp
var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero));
var harness = StoreTestHarnessFactory.ForFeature<MyState>()
    .WithService<TimeProvider>(fakeTime)
    .WithEffect<MyTimeBasedEffect>();
```

## Related Documentation

- [Reservoir Overview](reservoir.md) — Core concepts
- [Reducers](reducers.md) — Writing pure reducer functions
- [Effects](effects.md) — Handling async side effects
- [Built-in Navigation](built-in-navigation.md) — Testing navigation reducers
- [Built-in Lifecycle](built-in-lifecycle.md) — Testing lifecycle reducers

## Source Code

- [`StoreTestHarness<TState>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Testing/StoreTestHarness.cs)
- [`StoreScenario<TState>`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Testing/StoreScenario.cs)
- [`StoreTestHarnessFactory`](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Reservoir.Testing/StoreTestHarnessFactory.cs)
