# Task 7.8: Migration from Cascade Sample

**Status**: ⬜ Not Started  
**Depends On**: 7.4 Server Implementation, 7.5 Client Implementation

## Goal

Migrate the Cascade sample from its current custom `IProjectionSubscriber<T>` to use the framework's Ripples library.

## Current State

The Cascade sample has:
- `IProjectionSubscriber<T>` in `Cascade.Server/Components/Services/`
- `ProjectionSubscriber<T>` implementation
- `IProjectionSubscriberFactory` for DI
- Manual SignalR connection management

## Migration Steps

### 1. Replace Interface

```diff
- using Cascade.Server.Components.Services;
+ using Mississippi.Ripples.Abstractions;

- [Inject] private IProjectionSubscriber<ChannelProjection> Channel { get; set; }
+ [Inject] private IRipple<ChannelProjection> Channel { get; set; }
```

### 2. Replace Base Class

```diff
- public partial class ChannelView : ComponentBase, IAsyncDisposable
+ public partial class ChannelView : RippleComponent
```

### 3. Replace Manual Lifecycle

```diff
- protected override async Task OnInitializedAsync()
- {
-     await Channel.SubscribeAsync(ChannelId);
-     Channel.OnChanged += HandleChanged;
- }
- 
- private void HandleChanged(object? sender, EventArgs e) => InvokeAsync(StateHasChanged);
- 
- public async ValueTask DisposeAsync()
- {
-     Channel.OnChanged -= HandleChanged;
-     await Channel.DisposeAsync();
- }
+ protected override async Task OnInitializedAsync()
+ {
+     UseRipple(Channel);
+     await Channel.SubscribeAsync(ChannelId);
+ }
+ // Disposal handled automatically by RippleComponent
```

### 4. Update Service Registration

```diff
- services.AddScoped(typeof(IProjectionSubscriber<>), typeof(ProjectionSubscriber<>));
- services.AddScoped<IProjectionSubscriberFactory, ProjectionSubscriberFactory>();
+ services.AddRipplesServer();
```

### 5. Delete Old Files

- `Cascade.Server/Components/Services/IProjectionSubscriber.cs`
- `Cascade.Server/Components/Services/ProjectionSubscriber.cs`
- `Cascade.Server/Components/Services/IProjectionSubscriberFactory.cs`
- `Cascade.Server/Components/Services/ProjectionSubscriberFactory.cs`
- `Cascade.Server/Components/Services/ProjectionSubscriberLoggerExtensions.cs`
- `Cascade.Server/Components/Services/ProjectionErrorEventArgs.cs`

### 6. Add Generated Controllers (for future WASM support)

Add `[ExposeAsProjectionApi]` attributes to projection grains in `Cascade.Domain`:

```csharp
[BrookName("CASCADE", "MESSAGING", "CHANNEL")]
[ExposeAsProjectionApi(Route = "api/projections/channels")]
public class ChannelProjectionGrain : UxProjectionGrainBase<ChannelProjection>
{ }
```

## Acceptance Criteria

- [ ] All Cascade components use `IRipple<T>` instead of `IProjectionSubscriber<T>`
- [ ] All components extend `RippleComponent`
- [ ] Old subscription service files deleted
- [ ] Service registration uses `AddRipplesServer()`
- [ ] Grains have `[ExposeAsProjectionApi]` attributes
- [ ] Controllers generated via source generator
- [ ] All existing tests still pass
- [ ] Blazor app functions identically

## Breaking Changes in Cascade

| Old API | New API |
|---------|---------|
| `IProjectionSubscriber<T>` | `IRipple<T>` |
| `ProjectionSubscriberFactory` | Direct DI of `IRipple<T>` |
| `EventHandler OnChanged` | `Action OnChanged` |
| Manual `IAsyncDisposable` | `RippleComponent` base class |
