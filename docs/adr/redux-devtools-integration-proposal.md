# ADR: Redux DevTools Integration for Ripples

## Status

Proposed

## Context

The user requested Redux DevTools integration for the Ripples client-side state management library. Redux DevTools is a browser extension that provides:

1. **Action inspection**: See every dispatched action with payload
2. **State diff**: See exactly what changed in state after each action
3. **Time travel**: Jump to any previous state
4. **Action replay**: Replay actions from a previous session
5. **State export/import**: Save and load state for debugging

Fluxor already has Redux DevTools integration that we can learn from.

## Decision

### 1. New Package: `Mississippi.Ripples.Blazor.ReduxDevTools`

Create a dedicated NuGet package for Redux DevTools integration:

```
src/Ripples.Blazor.ReduxDevTools/
├── ReduxDevToolsMiddleware.cs
├── ReduxDevToolsOptions.cs
├── ReduxDevToolsRegistrations.cs
├── JsInterop/
│   ├── IReduxDevToolsInterop.cs
│   └── ReduxDevToolsInterop.cs
└── wwwroot/
    └── ripples-devtools.js
```

### 2. Usage API

```csharp
// Program.cs - Client setup
services.AddMississippiClient(o => o
    .ScanAssemblies(typeof(SidebarReducer).Assembly)
    .WithReduxDevTools(devTools => devTools
        .WithName("Cascade Chat")
        .WithMaxHistory(50)
        .EnableInProduction(false)));  // Default: disabled in Release

// Or with existing explicit registration
services.AddRipples();
services.AddRipplesReduxDevTools();
```

### 3. Middleware Implementation

The middleware intercepts all dispatched actions and sends state to the browser extension:

```csharp
/// <summary>
/// Middleware that integrates with the Redux DevTools browser extension.
/// </summary>
public sealed class ReduxDevToolsMiddleware : IMiddleware, IAsyncDisposable
{
    private IReduxDevToolsInterop Interop { get; }
    private IRippleStore Store { get; }
    private ReduxDevToolsOptions Options { get; }
    private bool IsInitialized { get; set; }

    public ReduxDevToolsMiddleware(
        IReduxDevToolsInterop interop,
        IRippleStore store,
        ReduxDevToolsOptions options)
    {
        Interop = interop;
        Store = store;
        Options = options;
    }

    public void Invoke(IAction action, Action<IAction> dispatch)
    {
        // Let action flow through
        dispatch(action);

        // After reducers run, send new state to DevTools
        if (IsInitialized && Interop.IsConnected)
        {
            var state = CollectAllState();
            _ = Interop.SendAsync(action, state);
        }
    }

    public async Task InitializeAsync()
    {
        var initialState = CollectAllState();
        await Interop.InitAsync(initialState);
        IsInitialized = true;

        // Subscribe to time-travel commands from DevTools
        Interop.OnJumpToState += HandleJumpToState;
    }

    private Dictionary<string, object> CollectAllState()
    {
        // Collect all feature states and projection states
        var state = new Dictionary<string, object>();
        
        // Feature states (IFeatureState)
        foreach (var (key, value) in Store.GetAllFeatureStates())
        {
            state[$"features/{key}"] = value;
        }
        
        // Projection states (IProjectionState<T>)
        foreach (var (key, value) in Store.GetAllProjectionStates())
        {
            state[$"projections/{key}"] = value;
        }
        
        return state;
    }

    private async Task HandleJumpToState(JumpToStatePayload payload)
    {
        // Restore state from DevTools (time travel)
        if (Options.EnableTimeTravel)
        {
            await Store.RestoreStateAsync(payload.State);
        }
    }

    public async ValueTask DisposeAsync()
    {
        Interop.OnJumpToState -= HandleJumpToState;
        await Interop.DisconnectAsync();
    }
}
```

### 4. JavaScript Interop

The JavaScript side connects to `window.__REDUX_DEVTOOLS_EXTENSION__`:

```javascript
// wwwroot/ripples-devtools.js
window.MississippiRipplesDevTools = {
    connection: null,
    dotNetRef: null,

    init: function(dotNetRef, state, options) {
        this.dotNetRef = dotNetRef;
        
        const reduxDevTools = window.__REDUX_DEVTOOLS_EXTENSION__;
        if (!reduxDevTools) {
            console.warn('Redux DevTools extension not detected');
            return false;
        }

        this.connection = reduxDevTools.connect({
            name: options.name || 'Mississippi Ripples',
            maxAge: options.maxAge || 50,
            features: {
                jump: options.enableTimeTravel ?? false,
                skip: false,
                reorder: false,
                dispatch: false,
                persist: false
            }
        });

        this.connection.subscribe((message) => {
            if (message.type === 'DISPATCH') {
                const payload = message.payload;
                if (payload.type === 'JUMP_TO_STATE' || payload.type === 'JUMP_TO_ACTION') {
                    this.dotNetRef.invokeMethodAsync('OnJumpToState', message.state);
                }
            }
        });

        this.connection.init(state);
        return true;
    },

    send: function(action, state) {
        if (this.connection) {
            this.connection.send(action, state);
        }
    },

    disconnect: function() {
        if (this.connection) {
            this.connection.unsubscribe();
            this.connection = null;
        }
    }
};
```

### 5. Configuration Options

```csharp
/// <summary>
/// Options for Redux DevTools integration.
/// </summary>
public sealed class ReduxDevToolsOptions
{
    /// <summary>
    /// Name shown in Redux DevTools panel.
    /// </summary>
    public string Name { get; set; } = "Mississippi Ripples";

    /// <summary>
    /// Maximum number of actions to keep in history.
    /// </summary>
    public int MaxHistory { get; set; } = 50;

    /// <summary>
    /// Enable time travel (jumping to previous states).
    /// </summary>
    public bool EnableTimeTravel { get; set; } = true;

    /// <summary>
    /// Whether to enable DevTools in Release builds.
    /// </summary>
    public bool EnableInProduction { get; set; } = false;

    /// <summary>
    /// Filter actions by type name (null = log all).
    /// </summary>
    public Func<IAction, bool>? ActionFilter { get; set; }
}
```

### 6. Required Changes to RippleStore

To support DevTools, we need to add some capabilities to `IRippleStore`:

```csharp
public interface IRippleStore : IDisposable
{
    // Existing members...

    /// <summary>
    /// Gets all registered feature states (for DevTools).
    /// </summary>
    IEnumerable<KeyValuePair<string, object>> GetAllFeatureStates();

    /// <summary>
    /// Gets all projection states (for DevTools).
    /// </summary>
    IEnumerable<KeyValuePair<string, object>> GetAllProjectionStates();

    /// <summary>
    /// Restores state from a snapshot (for time travel).
    /// </summary>
    Task RestoreStateAsync(IDictionary<string, object> state);
}
```

### 7. Integration Points

**App.razor / _Host.cshtml**:

```html
<!-- Add script reference -->
<script src="_content/Mississippi.Ripples.Blazor.ReduxDevTools/ripples-devtools.js"></script>
```

**Component initialization** (via RippleComponent or explicit):

```csharp
// Automatic initialization when first RippleComponent renders
@inherits RippleComponent<SidebarState>

// Or manual initialization
@inject ReduxDevToolsMiddleware DevTools
@code {
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await DevTools.InitializeAsync();
        }
    }
}
```

## Implementation Plan

### Phase 1: Core Infrastructure

1. Create `src/Ripples.Blazor.ReduxDevTools` project
2. Add `ReduxDevToolsOptions` and builder API
3. Implement `IReduxDevToolsInterop` with JSInterop
4. Create `ripples-devtools.js` for browser extension communication

### Phase 2: Middleware Integration

1. Implement `ReduxDevToolsMiddleware`
2. Add `GetAllFeatureStates()` and `GetAllProjectionStates()` to `IRippleStore`
3. Wire up auto-initialization in `RippleComponent`
4. Add DI registration extensions

### Phase 3: Time Travel (Optional)

1. Add `RestoreStateAsync()` to `IRippleStore`
2. Implement state restoration in middleware
3. Handle projection state restoration

### Phase 4: Testing & Samples

1. Add L0 tests for middleware behavior
2. Update Cascade.Client sample to use DevTools
3. Add documentation

## Consequences

### Positive

- **Developer productivity**: Visual debugging of state changes
- **Easier onboarding**: New developers can see state flow
- **Bug reproduction**: Export/import state for bug reports
- **Familiar tooling**: Reuses widely-adopted Redux DevTools

### Negative

- **New dependency**: Adds ~50KB to client bundle
- **Security consideration**: State visible in browser extension (disabled by default in prod)
- **Blazor-only**: Won't work for non-Blazor clients

### Neutral

- **Optional**: DevTools is opt-in, no impact on existing code
- **Zero runtime cost when disabled**: Middleware short-circuits if not connected

## Alternatives Considered

### 1. Custom DevTools UI

Build a custom Blazor component for debugging.

**Rejected**: More work, less familiar, Redux DevTools already excellent.

### 2. Console Logging Only

Just log actions to browser console.

**Rejected**: Loses time travel, state inspection, and visual UI.

### 3. Integrate with Existing Fluxor DevTools

Reuse Fluxor's implementation directly.

**Rejected**: Would create dependency on Fluxor, patterns don't align perfectly.

## References

- [Redux DevTools Extension API](https://github.com/reduxjs/redux-devtools/blob/main/extension/docs/API/Methods.md)
- [Fluxor Redux DevTools Implementation](https://github.com/mrpmorris/Fluxor/tree/master/Source/Lib/Fluxor.Blazor.Web.ReduxDevTools)
- [Redux DevTools without Redux](https://medium.com/@zalmoxis/redux-devtools-without-redux-or-how-to-have-a-predictable-state-with-any-architecture-61c5f5a7716f)
