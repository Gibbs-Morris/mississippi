# ADR-0001: Dual Hosting Model

**Status**: Accepted
**Date**: 2026-01-03

## Context

Mississippi.Ripples must support both Blazor Server and Blazor WebAssembly applications. These hosting models have fundamentally different runtime characteristics:

| Aspect | Blazor Server | Blazor WebAssembly |
|--------|---------------|-------------------|
| Execution | Server process | Browser sandbox |
| Backend Access | Direct (in-process) | HTTP only |
| Latency | Minimal | Network round-trip |
| SignalR | Already there (UI transport) | Separate connection |

Developers want to write code once and have it work in both environments without conditional compilation or runtime checks scattered throughout their components.

## Decision

We will provide a **single interface** (`IRipple<T>`) with **two implementations**:

1. **`ServerRipple<T>`** (in `Ripples.Server`)
   - Uses `IGrainFactory` for direct grain calls
   - Zero serialization overhead
   - In-process SignalR hub context access

2. **`ClientRipple<T>`** (in `Ripples.Client`)
   - Uses `HttpClient` for projection fetches
   - Uses `HubConnection` for real-time subscriptions
   - ETag-based caching for efficiency

Service registration determines which implementation is injected:

```csharp
// Blazor Server
services.AddRipplesServer();

// Blazor WASM
services.AddRipplesClient(options => options.BaseApiUrl = "...");
```

## Consequences

### Positive

- Developers write `IRipple<T>` code once
- Same components work in both hosting models
- Clear separation of transport concerns
- Easy to test (mock `IRipple<T>`)

### Negative

- Two implementations to maintain
- WASM requires generated HTTP controllers
- Must keep Server and Client behaviors semantically identical

### Neutral

- Source generators handle controller and route registry creation
- Migration path for existing Cascade sample is straightforward
