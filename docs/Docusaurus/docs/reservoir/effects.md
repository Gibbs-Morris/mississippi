---
sidebar_position: 4
title: Effects
description: Handle asynchronous operations like API calls, timers, and navigation
---

# Effects

Effects handle asynchronous operations triggered by actions. While reducers must be pure and synchronous, effects can perform API calls, interact with browser APIs, set timers, and emit new actions over time. Effects are where side effects belong in a Reservoir application.

## When to Use Effects

Use effects when you need to:

- Make HTTP requests to APIs
- Access browser storage (localStorage, IndexedDB)
- Interact with real-time services (SignalR, WebSockets)
- Perform delayed or periodic operations
- Navigate or interact with browser APIs
- Coordinate complex multi-step workflows

## The IEffect Interface

```csharp
/// <summary>
/// Handles asynchronous side effects triggered by actions.
/// </summary>
public interface IEffect
{
    /// <summary>
    /// Determines whether this effect can handle the given action.
    /// </summary>
    /// <param name="action">The action to check.</param>
    /// <returns>True if this effect handles the action; otherwise, false.</returns>
    bool CanHandle(IAction action);

    /// <summary>
    /// Handles the action asynchronously and yields resulting actions.
    /// </summary>
    /// <param name="action">The action to handle.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async enumerable of actions to dispatch.</returns>
    IAsyncEnumerable<IAction> HandleAsync(
        IAction action,
        CancellationToken cancellationToken);
}
```

Effects use `IAsyncEnumerable<IAction>` to support streaming multiple actions over time—perfect for progress updates, polling, or multi-step workflows.

## Implementing Effects

### Basic Effect Pattern

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;

public sealed class LoadProductsEffect : IEffect
{
    public LoadProductsEffect(HttpClient httpClient)
        => Http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    private HttpClient Http { get; }

    public bool CanHandle(IAction action) => action is LoadProductsAction;

    public async IAsyncEnumerable<IAction> HandleAsync(
        IAction action,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Signal that loading has started
        yield return new ProductsLoadingAction();

        string[]? products = null;
        string? errorMessage = null;

        try
        {
            products = await Http.GetFromJsonAsync<string[]>(
                "api/products", 
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            errorMessage = ex.Message;
        }

        if (errorMessage is not null)
        {
            yield return new ProductsLoadFailedAction(errorMessage);
            yield break;
        }

        if (products is null)
        {
            yield return new ProductsLoadFailedAction("No products returned from API");
            yield break;
        }

        yield return new ProductsLoadedAction([.. products]);
    }
}
```

### Effect Lifecycle

1. **Action dispatched** — User or system dispatches an action
2. **CanHandle check** — Store calls `CanHandle()` on each registered effect
3. **HandleAsync execution** — For matching effects, `HandleAsync()` runs asynchronously
4. **Actions yielded** — Each yielded action is dispatched back to the store
5. **Concurrent execution** — Effects run concurrently; they don't block the UI

```text
Dispatch(LoadProductsAction)
         │
         ▼
    ┌────────────┐
    │  Reducers  │ ──▶ State updated (if any reducer handles it)
    └────────────┘
         │
         ▼
    ┌────────────┐
    │  Effects   │ ──▶ LoadProductsEffect.CanHandle() returns true
    └────────────┘
         │
         ▼
    HandleAsync() starts
         │
         ├──▶ yield ProductsLoadingAction ──▶ Dispatch ──▶ Reducers
         │
         ├──▶ await HTTP call
         │
         └──▶ yield ProductsLoadedAction ──▶ Dispatch ──▶ Reducers
```

## Dependency Injection

Effects support constructor injection for services:

```csharp
public sealed class AuthenticationEffect : IEffect
{
    public AuthenticationEffect(
        IAuthService authService,
        ILogger<AuthenticationEffect> logger)
    {
        AuthService = authService;
        Logger = logger;
    }

    private IAuthService AuthService { get; }
    private ILogger<AuthenticationEffect> Logger { get; }

    public bool CanHandle(IAction action) 
        => action is LoginAction or LogoutAction;

    public async IAsyncEnumerable<IAction> HandleAsync(
        IAction action,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        switch (action)
        {
            case LoginAction login:
                await foreach (var resultAction in HandleLoginAsync(login, cancellationToken))
                {
                    yield return resultAction;
                }
                break;

            case LogoutAction:
                await AuthService.LogoutAsync(cancellationToken);
                yield return new LoggedOutAction();
                break;
        }
    }

    private async IAsyncEnumerable<IAction> HandleLoginAsync(
        LoginAction login,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new LoginStartedAction();

        try
        {
            var result = await AuthService.LoginAsync(
                login.Username, 
                login.Password, 
                cancellationToken);

            if (result.IsSuccess)
            {
                yield return new LoginSucceededAction(result.User);
            }
            else
            {
                yield return new LoginFailedAction(result.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Login failed unexpectedly");
            yield return new LoginFailedAction("An unexpected error occurred");
        }
    }
}
```

## Streaming Multiple Actions

Effects excel at streaming actions over time:

### Progress Updates

```csharp
public sealed class FileUploadEffect : IEffect
{
    public bool CanHandle(IAction action) => action is UploadFileAction;

    public async IAsyncEnumerable<IAction> HandleAsync(
        IAction action,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var upload = (UploadFileAction)action;
        
        yield return new UploadStartedAction(upload.FileName);

        var progress = new Progress<int>(percent => { });
        
        // Simulate chunked upload with progress
        for (int i = 0; i <= 100; i += 10)
        {
            await Task.Delay(100, cancellationToken);
            yield return new UploadProgressAction(upload.FileName, i);
        }

        yield return new UploadCompletedAction(upload.FileName);
    }
}
```

### Polling

```csharp
public sealed class NotificationPollingEffect : IEffect
{
    private readonly INotificationService notificationService;

    public NotificationPollingEffect(INotificationService notificationService)
        => this.notificationService = notificationService;

    public bool CanHandle(IAction action) => action is StartPollingAction;

    public async IAsyncEnumerable<IAction> HandleAsync(
        IAction action,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var notifications = await notificationService.GetNewAsync(cancellationToken);
            
            if (notifications.Any())
            {
                yield return new NotificationsReceivedAction(notifications);
            }

            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
        }
    }
}
```

## Error Handling

Effects are responsible for their own error handling. The store catches and swallows exceptions to prevent effect failures from breaking dispatch:

```csharp
public async IAsyncEnumerable<IAction> HandleAsync(
    IAction action,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    var loadAction = (LoadDataAction)action;
    
    yield return new DataLoadingAction();

    try
    {
        var data = await dataService.LoadAsync(
            loadAction.Id, 
            cancellationToken);
        yield return new DataLoadedAction(data);
    }
    catch (HttpRequestException ex)
    {
        yield return new DataLoadFailedAction($"Network error: {ex.Message}");
    }
    catch (UnauthorizedAccessException)
    {
        yield return new DataLoadFailedAction("Access denied");
        yield return new ForceLogoutAction();
    }
    catch (OperationCanceledException)
    {
        // Don't emit error action for cancellation
        yield break;
    }
    catch (Exception ex)
    {
        // Log unexpected errors and emit generic failure
        logger.LogError(ex, "Unexpected error loading data {Id}", loadAction.Id);
        yield return new DataLoadFailedAction("An unexpected error occurred");
    }
}
```

## Registration

Register effects with dependency injection:

```csharp
// Program.cs
builder.Services.AddEffect<LoadProductsEffect>();
builder.Services.AddEffect<AuthenticationEffect>();
builder.Services.AddEffect<FileUploadEffect>();

// Effects are resolved when the Store is created
builder.Services.AddReservoir(store => store.RegisterState<AppState>());
```

Effects are registered with **scoped lifetime**:

- In Blazor WebAssembly, scoped behaves as singleton
- In Blazor Server, each circuit gets its own effect instances

## Rules and Limitations

### Rules

1. **Effects must be stateless between actions.** Don't store state that persists across action handling. Use the store for persistent state.

2. **Effects must handle their own errors.** The store swallows exceptions; emit error actions for the UI to display.

3. **Effects should respect cancellation.** Check `cancellationToken.IsCancellationRequested` and handle `OperationCanceledException`.

4. **Effects must not access the store directly.** Use yielded actions to trigger state changes.

### Limitations

1. **No guaranteed execution order.** Effects run concurrently; don't depend on ordering between effects.

2. **No transaction support.** Multiple yielded actions are dispatched independently.

3. **Effects are fire-and-forget.** The `Dispatch()` call returns before effects complete.

## Best Practices

### Do

- ✅ Emit loading/started actions immediately before async work
- ✅ Emit success/failure actions after async work completes
- ✅ Handle all expected exception types explicitly
- ✅ Use `[EnumeratorCancellation]` attribute on the cancellation token parameter
- ✅ Implement `IDisposable` if the effect holds resources
- ✅ Keep effects focused on a single concern
- ✅ Use constructor injection for dependencies

### Don't

- ❌ Store mutable state in effect fields
- ❌ Directly modify component state or UI
- ❌ Catch and swallow exceptions silently (emit error actions instead)
- ❌ Perform synchronous blocking calls
- ❌ Access `HttpContext` or other request-scoped services (use scoped services appropriately)

## Testing Effects

Test effects by invoking `HandleAsync` and collecting yielded actions:

```csharp
public sealed class LoadProductsEffectTests
{
    [Fact]
    public async Task HandleAsync_EmitsLoadingThenLoadedActions()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("/api/products")
            .Respond("application/json", """["Widget", "Gadget"]""");
        
        var httpClient = new HttpClient(mockHttp) 
        { 
            BaseAddress = new Uri("https://api.example.com") 
        };
        
        var sut = new LoadProductsEffect(httpClient);
        var action = new LoadProductsAction();
        var actions = new List<IAction>();

        // Act
        await foreach (var resultAction in sut.HandleAsync(action, CancellationToken.None))
        {
            actions.Add(resultAction);
        }

        // Assert
        Assert.Equal(2, actions.Count);
        Assert.IsType<ProductsLoadingAction>(actions[0]);
        Assert.IsType<ProductsLoadedAction>(actions[1]);
        
        var loaded = (ProductsLoadedAction)actions[1];
        Assert.Equal(["Widget", "Gadget"], loaded.Products);
    }

    [Fact]
    public async Task HandleAsync_OnNetworkError_EmitsFailedAction()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("/api/products")
            .Throw(new HttpRequestException("Connection refused"));
        
        var httpClient = new HttpClient(mockHttp) 
        { 
            BaseAddress = new Uri("https://api.example.com") 
        };
        
        var sut = new LoadProductsEffect(httpClient);
        var action = new LoadProductsAction();
        var actions = new List<IAction>();

        // Act
        await foreach (var resultAction in sut.HandleAsync(action, CancellationToken.None))
        {
            actions.Add(resultAction);
        }

        // Assert
        Assert.Equal(2, actions.Count);
        Assert.IsType<ProductsLoadingAction>(actions[0]);
        Assert.IsType<ProductsLoadFailedAction>(actions[1]);
    }

    [Fact]
    public void CanHandle_WithLoadProductsAction_ReturnsTrue()
    {
        // Arrange
        var sut = new LoadProductsEffect(new HttpClient());

        // Act & Assert
        Assert.True(sut.CanHandle(new LoadProductsAction()));
        Assert.False(sut.CanHandle(new SomeOtherAction()));
    }
}
```

## Disposable Effects

Effects that hold resources should implement `IDisposable`:

```csharp
public sealed class WebSocketEffect : IEffect, IDisposable
{
    private readonly ClientWebSocket webSocket = new();
    private bool disposed;

    public bool CanHandle(IAction action) 
        => action is ConnectWebSocketAction or DisconnectWebSocketAction;

    public async IAsyncEnumerable<IAction> HandleAsync(
        IAction action,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Implementation...
        yield break;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        webSocket.Dispose();
    }
}
```

The store will dispose effects that implement `IDisposable` when the store is disposed.

## Next Steps

- Learn how the [Store](./store.md) coordinates effects with reducers
- Review [Actions](./actions.md) for defining effect triggers
- See [Reducers](./reducers.md) for handling actions yielded by effects
