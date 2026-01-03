# Task 7.5: Client Implementation

**Status**: ⬜ Not Started  
**Depends On**: 7.3 Blazor Integration, 7.6 Source Generators

## Goal

Create the `Ripples.Client` project for Blazor WebAssembly with HTTP and SignalR access.

## Acceptance Criteria

- [ ] `ClientRipple<T>` implementation with HTTP + ETag
- [ ] `ClientRipplePool<T>` with batch fetch support
- [ ] `SignalRRippleConnection` for connection lifecycle
- [ ] `AddRipplesClient()` service registration extension
- [ ] Uses `ProjectionRouteRegistry` for URL construction
- [ ] Auto-reconnect with exponential backoff
- [ ] Project targets `net9.0`
- [ ] L0 tests with HTTP/SignalR mocking

## New Project

`src/Ripples.Client/Ripples.Client.csproj`

## Key Components

```csharp
public sealed class ClientRipple<T> : IRipple<T>
{
    private HttpClient HttpClient { get; }
    private ISignalRRippleConnection SignalRConnection { get; }
    
    public async Task SubscribeAsync(string entityId, CancellationToken ct)
    {
        // 1. Get route from generated registry
        string route = ProjectionRouteRegistry.GetRoute<T>();
        
        // 2. HTTP GET with ETag
        var response = await HttpClient.GetAsync($"{route}/{entityId}", ct);
        
        // 3. Subscribe via SignalR
        await SignalRConnection.SubscribeAsync(typeof(T).Name, entityId, ct);
    }
}
```

## Service Registration

```csharp
services.AddRipplesClient(options =>
{
    options.BaseApiUrl = "https://api.example.com";
    options.SignalRHubPath = "/hubs/projections";
    options.EnableAutoReconnect = true;
});
```
