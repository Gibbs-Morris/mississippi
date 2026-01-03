# Task 7.3: Blazor Integration

**Status**: ⬜ Not Started  
**Depends On**: 7.2 Core Store

## Goal

Create the `Ripples.Blazor` project with Blazor-specific integration components.

## Acceptance Criteria

- [ ] `RippleComponent` base class with auto-subscription lifecycle
- [ ] `RippleProvider.razor` cascading parameter setup
- [ ] `UseRipple()` helper for manual lifecycle management
- [ ] `StateHasChanged` integration on ripple updates
- [ ] Project targets `net9.0`
- [ ] Works with both Server and WASM Blazor
- [ ] L0 tests using bUnit

## New Project

`src/Ripples.Blazor/Ripples.Blazor.csproj`

## Key Components

```csharp
public abstract class RippleComponent : ComponentBase, IAsyncDisposable
{
    protected void UseRipple<T>(IRipple<T> ripple) where T : class;
    public ValueTask DisposeAsync();
}
```
