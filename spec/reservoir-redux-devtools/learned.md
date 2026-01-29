# Learned

## Repository orientation (initial)

- Reservoir core store lives in src/Reservoir/Store.cs.
- Reservoir Blazor integration lives in src/Reservoir.Blazor/StoreComponent.cs.
- Reservoir abstractions live in src/Reservoir.Abstractions/.

## Observations (initial, from code inspection)

- Store dispatches actions through middleware, then reducers, then effects, and notifies subscribers. (src/Reservoir/Store.cs)
- Store has a virtual OnActionDispatched hook and subscribable listeners. (src/Reservoir/Store.cs)
- Store does not expose a public state replacement API, only GetState and Dispatch. (src/Reservoir.Abstractions/IStore.cs)

## Verified facts

- IStore is registered as scoped via ReservoirRegistrations.AddReservoir. (src/Reservoir/ReservoirRegistrations.cs)
- Reservoir.Blazor contains built-in feature registrations (navigation/lifecycle) but no JS interop references. (src/Reservoir.Blazor/**)
- Reservoir.Blazor currently has no wwwroot or static web assets folder. (src/Reservoir.Blazor)
- IAction is a marker interface with no intrinsic type/name field. (src/Reservoir.Abstractions/Actions/IAction.cs)
- Redux DevTools browser extension exposes window.__REDUX_DEVTOOLS_EXTENSION__.connect with subscribe/init/send methods. (https://raw.githubusercontent.com/reduxjs/redux-devtools/main/extension/docs/API/Methods.md)
- Redux DevTools options include name, maxAge, latency, action/state sanitizers, and features. (https://raw.githubusercontent.com/reduxjs/redux-devtools/main/extension/docs/API/Arguments.md)

## To verify

- Existing middleware/diagnostics hooks in Reservoir and Reservoir.Blazor beyond current IMiddleware usage.
- Any JS interop patterns or static web assets in Reservoir.Blazor or other Blazor projects.
- DI registration patterns for optional, dev-only features in Reservoir.Blazor.
