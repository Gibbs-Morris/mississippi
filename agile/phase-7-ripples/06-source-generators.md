# Task 7.6: Source Generators

**Status**: ⬜ Not Started  
**Depends On**: 7.1 Abstractions

## Goal

Create source generators that auto-generate controllers and route registries from grain attributes.

## Acceptance Criteria

- [ ] `[ExposeAsProjectionApi]` attribute defined
- [ ] `[ExposeAsCommandApi]` attribute defined
- [ ] `ProjectionControllerGenerator` creates controllers from grain attributes
- [ ] `AggregateControllerGenerator` creates command controllers
- [ ] `ProjectionRouteRegistryGenerator` creates route lookup for WASM
- [ ] Batch endpoint generation for `IRipplePool<T>` support
- [ ] Project targets `netstandard2.0` (source generator requirement)
- [ ] Integration tests verify generated code compiles

## New Project

`src/Ripples.Generators/Ripples.Generators.csproj`

## Projection Controller Generation

### Input

```csharp
[BrookName("CASCADE", "MESSAGING", "CHANNEL")]
[ExposeAsProjectionApi(Route = "api/projections/channels")]
public class ChannelProjectionGrain : UxProjectionGrainBase<ChannelProjection>
{ }
```

### Output

```csharp
// ChannelProjectionController.g.cs
[GeneratedCode("Ripples.Generators", "1.0.0")]
[Route("api/projections/channels/{entityId}")]
[ApiController]
public sealed partial class ChannelProjectionController 
    : UxProjectionControllerBase<ChannelProjection>
{
    public ChannelProjectionController(
        IUxProjectionGrainFactory factory,
        ILogger<UxProjectionControllerBase<ChannelProjection>> logger)
        : base(factory, logger) { }
    
    [HttpPost("batch")]
    public async Task<ActionResult<Dictionary<string, ChannelProjection>>> GetBatchAsync(
        [FromBody] BatchProjectionRequest request,
        CancellationToken ct = default)
    {
        // Batch fetch for RipplePool support
    }
}
```

## Route Registry Generation

```csharp
// ProjectionRouteRegistry.g.cs
[GeneratedCode("Ripples.Generators", "1.0.0")]
public static partial class ProjectionRouteRegistry
{
    private static readonly Dictionary<Type, string> routes = new()
    {
        [typeof(ChannelProjection)] = "api/projections/channels",
        [typeof(UserProjection)] = "api/projections/users",
    };
    
    public static string GetRoute<T>() => routes[typeof(T)];
    public static string GetRoute(Type projectionType) => routes[projectionType];
}
```

## Aggregate Controller Generation

### Input

```csharp
[ExposeAsCommandApi(Route = "api/channels")]
public interface IChannelAggregateGrain : IAggregateGrain
{
    [HttpPost("join")]
    Task<OperationResult> JoinAsync(JoinChannelRequest request);
    
    [HttpPost("send")]
    Task<OperationResult> SendMessageAsync(SendMessageRequest request);
}
```

### Output

```csharp
// ChannelAggregateController.g.cs
[GeneratedCode("Ripples.Generators", "1.0.0")]
[Route("api/channels/{entityId}")]
[ApiController]
public sealed partial class ChannelAggregateController : ControllerBase
{
    private IGrainFactory GrainFactory { get; }
    
    [HttpPost("join")]
    public async Task<ActionResult<OperationResult>> JoinAsync(
        [FromRoute] string entityId,
        [FromBody] JoinChannelRequest request,
        CancellationToken ct = default)
    {
        var grain = GrainFactory.GetGrain<IChannelAggregateGrain>(entityId);
        var result = await grain.JoinAsync(request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
    
    [HttpPost("send")]
    public async Task<ActionResult<OperationResult>> SendMessageAsync(
        [FromRoute] string entityId,
        [FromBody] SendMessageRequest request,
        CancellationToken ct = default)
    {
        var grain = GrainFactory.GetGrain<IChannelAggregateGrain>(entityId);
        var result = await grain.SendMessageAsync(request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
```

## Command Registry Generation

```csharp
// CommandRouteRegistry.g.cs
[GeneratedCode("Ripples.Generators", "1.0.0")]
public static partial class CommandRouteRegistry
{
    private static readonly Dictionary<Type, string> routes = new()
    {
        [typeof(IChannelAggregateGrain)] = "api/channels",
        [typeof(IUserAggregateGrain)] = "api/users",
    };
    
    public static string GetRoute<TGrain>() where TGrain : IAggregateGrain
        => routes[typeof(TGrain)];
}
```
