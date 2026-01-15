# Learned Facts

## File Locations

| File | Purpose |
|------|---------|
| `src/Inlet.Blazor.WebAssembly/Effects/InletSignalREffect.cs` | Main class (486 lines) |
| `src/Inlet.Blazor.WebAssembly/Effects/InletSignalREffectOptions.cs` | Options class |
| `src/Inlet.Blazor.WebAssembly/Effects/IProjectionFetcher.cs` | Fetcher interface |
| `src/Inlet.Blazor.WebAssembly/Effects/IProjectionDtoRegistry.cs` | Registry interface |
| `src/Inlet.Blazor.WebAssembly/InletBlazorSignalRBuilder.cs` | DI registration (line 151: `AddEffect<InletSignalREffect>()`) |
| `tests/Inlet.Blazor.WebAssembly.L0Tests/Effects/InletSignalREffectTests.cs` | Existing tests |

## Current Dependencies

- `IServiceProvider` - for lazy `IInletStore` resolution
- `NavigationManager` - for hub URL resolution
- `IProjectionFetcher` - for fetching projection data
- `IProjectionDtoRegistry` - for path/type mapping
- `InletSignalREffectOptions` - configuration

## Current Test Coverage

- 105 tests in the L0Tests project (16 for InletSignalREffect specifically)
- All tests pass as of verification run
- Core handler logic (`HandleSubscribeAsync`, `HandleRefreshAsync`, `OnProjectionUpdatedAsync`) is **NOT** L0-testable due to internal `HubConnection` creation

## DI Registration

**VERIFIED**: `InletSignalREffect` is registered via `Services.AddEffect<InletSignalREffect>()` in `InletBlazorSignalRBuilder.Build()` (line 151)

## Code Duplication Identified

1. **Action factory pattern** (4× identical reflection):
   - `CreateErrorAction` (line 96)
   - `CreateLoadedAction` (line 104)
   - `CreateLoadingAction` (line 114)
   - `CreateUpdatedAction` (line 122)

2. **Fetch-with-error-handling pattern** (3×):
   - `HandleRefreshAsync` (lines 207-251)
   - `HandleSubscribeAsync` (lines 257-354)
   - `OnProjectionUpdatedAsync` (lines 390-420)
