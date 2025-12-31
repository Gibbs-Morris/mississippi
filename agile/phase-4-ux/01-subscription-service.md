# Task 4.1: Projection Subscription Service

**Status**: ⬜ Not Started  
**Depends On**: Phase 1 (SignalR hub), Phase 3 (Blazor Server)

## Goal

Create `IProjectionSubscriber<T>` service that provides auto-updating access to projections via SignalR notifications + HTTP fetch, enabling one-line subscription in Blazor components.

## Acceptance Criteria

- [ ] `IProjectionSubscriber<T>` interface with `Current`, `Version`, `Subscribe`, `Dispose`
- [ ] `ProjectionSubscriber<T>` implementation manages SignalR connection
- [ ] On version notification, fetches new data via HTTP with ETag
- [ ] Handles reconnection with `Resubscribe` call
- [ ] Notifies Blazor via event for `StateHasChanged()`
- [ ] Thread-safe property access
- [ ] L0 tests for subscription logic

## Interface Design

```csharp
/// <summary>
/// Provides auto-updating access to a UX projection.
/// </summary>
/// <typeparam name="T">The projection type.</typeparam>
public interface IProjectionSubscriber<T> : IAsyncDisposable
    where T : class
{
    /// <summary>Current projection value. May be null before first load.</summary>
    T? Current { get; }
    
    /// <summary>Current version of the projection.</summary>
    long? Version { get; }
    
    /// <summary>Whether the subscription is active and connected.</summary>
    bool IsConnected { get; }
    
    /// <summary>Whether initial data has been loaded.</summary>
    bool IsLoaded { get; }
    
    /// <summary>Event raised when Current changes.</summary>
    event Action? OnChanged;
    
    /// <summary>Event raised on error.</summary>
    event Action<Exception>? OnError;
    
    /// <summary>Subscribe to a specific entity.</summary>
    Task SubscribeAsync(string entityId, CancellationToken ct = default);
    
    /// <summary>Refresh data manually.</summary>
    Task RefreshAsync(CancellationToken ct = default);
}
```

## Implementation Design

```csharp
public class ProjectionSubscriber<T> : IProjectionSubscriber<T>
    where T : class
{
    private HubConnection HubConnection { get; }
    private HttpClient HttpClient { get; }
    private ILogger<ProjectionSubscriber<T>> Logger { get; }
    
    private string? currentEntityId;
    private string? currentSubscriptionId;
    private long? currentVersion;
    private string? currentETag;
    private T? current;
    
    public T? Current => current;
    public long? Version => currentVersion;
    public bool IsConnected => HubConnection.State == HubConnectionState.Connected;
    public bool IsLoaded => current is not null;
    
    public event Action? OnChanged;
    public event Action<Exception>? OnError;
    
    public async Task SubscribeAsync(string entityId, CancellationToken ct = default)
    {
        // 1. Store entity info
        currentEntityId = entityId;
        
        // 2. Connect SignalR if needed
        await EnsureConnectedAsync(ct);
        
        // 3. Subscribe via hub
        string projectionType = typeof(T).Name;
        string brookType = GetBrookType<T>(); // Extract from attribute
        currentSubscriptionId = await HubConnection.InvokeAsync<string>(
            "SubscribeAsync", projectionType, brookType, entityId, Guid.NewGuid().ToString(), ct);
        
        // 4. Initial fetch
        await RefreshAsync(ct);
    }
    
    private async Task OnProjectionChangedAsync(string projectionType, string entityId, long newVersion)
    {
        if (entityId != currentEntityId) return;
        if (currentVersion.HasValue && newVersion <= currentVersion.Value) return;
        
        await RefreshAsync();
    }
    
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        // Fetch with ETag
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/projections/{typeof(T).Name}/{currentEntityId}");
        if (currentETag is not null)
        {
            request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(currentETag));
        }
        
        var response = await HttpClient.SendAsync(request, ct);
        
        if (response.StatusCode == HttpStatusCode.NotModified)
        {
            return; // No changes
        }
        
        if (response.IsSuccessStatusCode)
        {
            current = await response.Content.ReadFromJsonAsync<T>(ct);
            currentETag = response.Headers.ETag?.Tag;
            if (long.TryParse(currentETag?.Trim('"'), out long version))
            {
                currentVersion = version;
            }
            OnChanged?.Invoke();
        }
    }
    
    // Reconnection handler
    private async Task HandleReconnectAsync()
    {
        if (currentEntityId is not null)
        {
            // Re-subscribe to all active subscriptions
            await SubscribeAsync(currentEntityId);
        }
    }
}
```

## Factory Pattern

```csharp
/// <summary>
/// Factory for creating projection subscribers.
/// </summary>
public interface IProjectionSubscriberFactory
{
    IProjectionSubscriber<T> Create<T>() where T : class;
}
```

## Blazor Usage

```razor
@inject IProjectionSubscriber<ChannelMessagesProjection> Messages
@implements IAsyncDisposable

@code {
    [Parameter] public string ChannelId { get; set; } = "";
    
    protected override async Task OnInitializedAsync()
    {
        Messages.OnChanged += StateHasChanged;
        await Messages.SubscribeAsync(ChannelId);
    }
    
    public async ValueTask DisposeAsync()
    {
        Messages.OnChanged -= StateHasChanged;
        await Messages.DisposeAsync();
    }
}
```

## TDD Steps

1. **Red**: Create `ProjectionSubscriberTests`
   - Test: `SubscribeAsync_ConnectsAndFetchesInitialData`
   - Test: `OnProjectionChanged_RefreshesWhenVersionNewer`
   - Test: `OnProjectionChanged_IgnoresWhenVersionOlder`
   - Test: `RefreshAsync_UsesETagForConditionalRequest`
   - Test: `RefreshAsync_Returns304_DoesNotUpdateCurrent`
   - Test: `HandleReconnect_ResubscribesAllActive`

2. **Green**: Implement with mocks for `HubConnection` and `HttpClient`

3. **Refactor**: Extract connection management; add retry logic

## Files to Create

- `samples/Cascade/Cascade.Server/Components/Services/IProjectionSubscriber.cs`
- `samples/Cascade/Cascade.Server/Components/Services/ProjectionSubscriber.cs`
- `samples/Cascade/Cascade.Server/Components/Services/IProjectionSubscriberFactory.cs`
- `samples/Cascade/Cascade.Server/Components/Services/ProjectionSubscriberFactory.cs`
- `samples/Cascade/Cascade.Domain.L0Tests/Services/ProjectionSubscriberTests.cs` (or separate test project)

## Registration

```csharp
services.AddScoped(typeof(IProjectionSubscriber<>), typeof(ProjectionSubscriber<>));
services.AddScoped<IProjectionSubscriberFactory, ProjectionSubscriberFactory>();
```

## Notes

- Service is scoped per Blazor circuit (one per browser tab)
- SignalR reconnection is automatic with `WithAutomaticReconnect()`
- ETag eliminates unnecessary data transfer on rapid version changes
- Consider debouncing for very high-frequency updates
