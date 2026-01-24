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
