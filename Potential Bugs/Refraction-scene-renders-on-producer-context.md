# Scene state update renders outside Blazor's required context

## Source location

- Project: `Refraction.Client.StateManagement`.
- Source file: [src/Refraction.Client.StateManagement/Scenes/SceneBase.cs lines 135-139](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Refraction.Client.StateManagement/Scenes/SceneBase.cs#L135-L139).
- Type: `SceneBase<TState>`.
- Member: `OnInitialized`.
- Investigated revision: `aa4a686677ab41b25ecdfd445b280b45d2f43fb1`.

## Potential problem

SceneBase passes StateHasChanged directly to the store as its listener. Store listeners run on the context that calls Dispatch. A background update therefore calls rendering outside Blazor's renderer context, unlike StoreComponent, which uses InvokeAsync.

## Trigger

Render a SceneBase-derived component in Blazor Server and make a serialized state update from a timer, background service, or external callback. No concurrent dispatches are required.

## Potential impact

In Blazor Server the render call can throw and the scene does not update. Because the callback runs inline, its error can also stop later listeners and effects for that dispatch after state has already changed.

## Evidence

- [src/Refraction.Client.StateManagement/Scenes/SceneBase.cs lines 18-30](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Refraction.Client.StateManagement/Scenes/SceneBase.cs#L18-L30), [src/Refraction.Client.StateManagement/Scenes/SceneBase.cs lines 135-139](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Refraction.Client.StateManagement/Scenes/SceneBase.cs#L135-L139): The base class promises automatic rerendering on state changes but passes the raw rendering method as the callback.
- [src/Reservoir.Core/Store.cs lines 283-300](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Reservoir.Core/Store.cs#L283-L300), [src/Reservoir.Core/Store.cs lines 338-349](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Reservoir.Core/Store.cs#L338-L349): Listener callbacks run inline after reduction and before effects, with no exception isolation.
- [src/Reservoir.Client/StoreComponent.cs lines 149-156](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/src/Reservoir.Client/StoreComponent.cs#L149-L156): The analogous base component already routes its callback through InvokeAsync(StateHasChanged).
- [docs/Docusaurus/docs/reservoir/concepts/state-flow.md lines 69](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/docs/Docusaurus/docs/reservoir/concepts/state-flow.md#L69), [docs/Docusaurus/docs/reservoir/concepts/state-flow.md lines 75](https://github.com/Gibbs-Morris/mississippi/blob/aa4a686677ab41b25ecdfd445b280b45d2f43fb1/docs/Docusaurus/docs/reservoir/concepts/state-flow.md#L75): Store callbacks must not throw, and background dispatches may be serialized by an application policy. A single worker context satisfies serialization but is still outside the renderer.

- [Supporting reference](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/synchronization-context?view=aspnetcore-10.0): External notifications must switch to the renderer synchronization context with ComponentBase.InvokeAsync before triggering rendering.

## Verification notes

Source inspection and related repository contracts. No fresh runtime reproduction was run for this finding.

- Mount a minimal SceneBase-derived component with the real Store and dispatch one action from a background context; compare with an equivalent StoreComponent-derived component.
- Use Blazor Server or another renderer enforcing dispatcher affinity. Do not claim this fails in every single-threaded WebAssembly deployment.

## Confidence

**High**. The raw callback bypasses the renderer dispatch used by the repository's equivalent base component and required by Blazor for external notifications.
