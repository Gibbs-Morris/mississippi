# Learned Facts

## Docusaurus Structure

- Docs location: `docs/Docusaurus/docs/`
- Platform docs: `docs/Docusaurus/docs/platform/`
- Reservoir docs: `docs/Docusaurus/docs/reservoir/`
- Sidebars config: `docs/Docusaurus/sidebars.ts`

## Existing Documentation

### Reservoir (Redux-style state management)
- `index.md` - Overview with data flow diagram
- `actions.md` - IAction interface, defining actions
- `reducers.md` - IActionReducer, pure state transformations
- `effects.md` - IEffect for async operations
- `store.md` - IStore central state container
- `getting-started.md` - Quick start guide

### Platform (Event Sourcing)
- `aggregates.md` - Brief overview (needs expansion)
- `brooks.md` - Brief overview (needs expansion)
- `ux-projections.md` - Brief overview (needs expansion)

## Source Code Locations (to explore)

### Reservoir
- `src/Reservoir/` - Core implementation
- `src/Reservoir.Abstractions/` - Interfaces and contracts
- `src/Reservoir.Blazor/` - Blazor integration

### Aqueduct (Orleans Stream Processing)
- `src/Aqueduct/` - Implementation
- `src/Aqueduct.Abstractions/` - Contracts
- `src/Aqueduct.Grains/` - Orleans grain implementations

### Inlet (Client-Server Bridge)
- `src/Inlet/` - Core implementation
- `src/Inlet.Abstractions/` - Contracts
- `src/Inlet.Blazor.Server/` - Blazor Server integration
- `src/Inlet.Blazor.WebAssembly/` - WASM client integration
- `src/Inlet.Blazor.WebAssembly.Abstractions/` - WASM contracts
- `src/Inlet.Client.Generators/` - Source generators for client
- `src/Inlet.Server.Generators/` - Source generators for server
- `src/Inlet.Silo.Generators/` - Source generators for Orleans silo
- `src/Inlet.Generators.Abstractions/` - Shared generator contracts
- `src/Inlet.Generators.Core/` - Shared generator logic
- `src/Inlet.Orleans/` - Orleans integration
- `src/Inlet.Orleans.SignalR/` - SignalR for live updates
- `src/Inlet.Projection.Abstractions/` - Projection contracts

### SDK Reference Packages
- `src/Sdk.Client/` - Client-side SDK (references Reservoir, Inlet.Blazor.WebAssembly)
- `src/Sdk.Server/` - Server-side SDK (references API layers, Inlet.Server)
- `src/Sdk.Silo/` - Orleans silo SDK (references grains, storage, Inlet.Silo)

### Event Sourcing - Aggregates
- `src/EventSourcing.Aggregates/` - Implementation
- `src/EventSourcing.Aggregates.Abstractions/` - Contracts
- `src/EventSourcing.Aggregates.Api/` - API layer

### Event Sourcing - Brooks (Event Streams)
- `src/EventSourcing.Brooks/` - Implementation
- `src/EventSourcing.Brooks.Abstractions/` - Contracts
- `src/EventSourcing.Brooks.Cosmos/` - Cosmos DB storage

### Event Sourcing - Reducers
- `src/EventSourcing.Reducers/` - Implementation
- `src/EventSourcing.Reducers.Abstractions/` - Contracts

### Event Sourcing - Snapshots
- `src/EventSourcing.Snapshots/` - Implementation
- `src/EventSourcing.Snapshots.Abstractions/` - Contracts
- `src/EventSourcing.Snapshots.Cosmos/` - Cosmos DB storage

### Event Sourcing - UX Projections
- `src/EventSourcing.UxProjections/` - Implementation
- `src/EventSourcing.UxProjections.Abstractions/` - Contracts
- `src/EventSourcing.UxProjections.Api/` - API layer

## Key Interfaces (to document)

- UNVERIFIED: Need to explore each abstraction project
