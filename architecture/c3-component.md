# C3: Component Diagram

This diagram shows the internal components of the Event Sourcing subsystem within the Mississippi Framework.

```mermaid
C4Component
    title Component diagram for Event Sourcing Subsystem

    Container_Boundary(aggregates, "Aggregates Container") {
        Component(aggGrain, "IAggregateGrain", "Orleans Grain Interface", "Aggregate root grain interface for command handling")
        Component(commandHandler, "Command Handlers", "C# Classes", "Process commands and produce events")
        Component(eventRegistry, "Event Type Registry", "C# Service", "Maps event types to names for serialization")
        Component(snapshotRegistry, "Snapshot Type Registry", "C# Service", "Maps snapshot types to names for serialization")
        Component(aggFactory, "Aggregate Factory", "C# Service", "Creates and configures aggregate instances")
        
        Rel(aggGrain, commandHandler, "Delegates to")
        Rel(commandHandler, eventRegistry, "Uses")
        Rel(aggGrain, aggFactory, "Created by")
        Rel(aggGrain, snapshotRegistry, "Uses")
    }

    Container_Boundary(brooks, "Brooks Container") {
        Component(brookDef, "Brook Definition", "C# Interface", "Defines event stream structure and keys")
        Component(brookEvent, "Brook Event", "C# Record", "Immutable event with position and metadata")
        Component(brookPosition, "Brook Position", "C# Value Object", "Position in event stream")
        Component(brookStorage, "Brook Storage Provider", "C# Interface", "Abstract storage operations")
        
        Rel(brookEvent, brookPosition, "Contains")
        Rel(brookStorage, brookDef, "Uses")
        Rel(brookStorage, brookEvent, "Stores/Retrieves")
    }

    Container_Boundary(reducers, "Reducers Container") {
        Component(reducer, "IReducer", "C# Interface", "Reduces events to state")
        Component(reducerEngine, "Reducer Engine", "C# Service", "Orchestrates state reduction")
        Component(foldFunc, "Fold Functions", "C# Functions", "Pure state transformation functions")
        
        Rel(reducerEngine, reducer, "Executes")
        Rel(reducer, foldFunc, "Implements with")
    }

    Container_Boundary(snapshots, "Snapshots Container") {
        Component(snapshotGrain, "Snapshot Grain", "Orleans Grain", "Manages aggregate snapshots")
        Component(snapshotStore, "Snapshot Storage", "C# Interface", "Abstract snapshot persistence")
        Component(snapshotStrategy, "Snapshot Strategy", "C# Service", "Determines when to snapshot")
        
        Rel(snapshotGrain, snapshotStore, "Persists via")
        Rel(snapshotGrain, snapshotStrategy, "Uses")
    }

    Container_Boundary(projections, "Projections Container") {
        Component(projectionGrain, "Projection Grain", "Orleans Grain", "Processes events into read models")
        Component(projectionHandler, "Event Handlers", "C# Classes", "Transform events to projections")
        Component(projectionStore, "Projection Storage", "C# Interface", "Stores read models")
        
        Rel(projectionGrain, projectionHandler, "Delegates to")
        Rel(projectionHandler, projectionStore, "Updates")
    }

    Container_Boundary(effects, "Effects Container") {
        Component(effect, "IEffect", "C# Interface", "Side effect definition")
        Component(effectRunner, "Effect Runner", "C# Service", "Executes side effects")
        Component(effectHandler, "Effect Handlers", "C# Classes", "Implement specific effects")
        
        Rel(effectRunner, effect, "Executes")
        Rel(effect, effectHandler, "Implemented by")
    }

    Container_Boundary(serialization, "Serialization Container") {
        Component(serializer, "Event Serializer", "C# Service", "Serializes/deserializes events")
        Component(jsonProvider, "JSON Provider", "C# Service", "System.Text.Json implementation")
        Component(typeMapping, "Type Mapping", "C# Service", "Maps CLR types to storage names")
        
        Rel(serializer, jsonProvider, "Uses")
        Rel(serializer, typeMapping, "Uses")
    }

    Container_Boundary(uxprojections, "UX Projections Container") {
        Component(uxGrain, "UX Projection Grain", "Orleans Grain", "UI-optimized projections")
        Component(uxHandler, "UX Event Handlers", "C# Classes", "Transform events for UI")
        Component(uxStore, "UX Projection Store", "C# Interface", "Stores UI projections")
        
        Rel(uxGrain, uxHandler, "Delegates to")
        Rel(uxHandler, uxStore, "Updates")
    }

    ' Cross-container relationships
    Rel(commandHandler, brookStorage, "Appends events to")
    Rel(aggGrain, reducerEngine, "Reduces state with")
    Rel(aggGrain, snapshotGrain, "Loads/saves via")
    Rel(reducerEngine, serializer, "Deserializes with")
    
    Rel(projectionGrain, brookStorage, "Reads events from")
    Rel(projectionHandler, effectRunner, "Triggers")
    
    Rel(uxGrain, brookStorage, "Reads events from")
    Rel(uxGrain, snapshotStore, "May use")

    UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="2")
```

## Key Components

### Aggregates Container
- **IAggregateGrain**: Orleans grain interface for aggregate roots
- **Command Handlers**: Execute business logic and produce domain events
- **Event Type Registry**: Maps event types to stable storage identifiers
- **Snapshot Type Registry**: Maps snapshot types to stable identifiers
- **Aggregate Factory**: Creates configured aggregate instances

### Brooks Container (Event Storage)
- **Brook Definition**: Defines stream structure and partitioning
- **Brook Event**: Immutable event record with metadata
- **Brook Position**: Position tracking in event stream
- **Brook Storage Provider**: Abstract interface for storage backends

### Reducers Container
- **IReducer**: Interface for state reduction logic
- **Reducer Engine**: Orchestrates event replay and state building
- **Fold Functions**: Pure functions for state transformation

### Snapshots Container
- **Snapshot Grain**: Manages periodic aggregate snapshots
- **Snapshot Storage**: Abstract persistence interface
- **Snapshot Strategy**: Determines snapshot frequency

### Projections Container
- **Projection Grain**: Processes events into read models
- **Event Handlers**: Transform events to projection updates
- **Projection Storage**: Persists read models

### Effects Container
- **IEffect**: Side effect contract
- **Effect Runner**: Executes effects asynchronously
- **Effect Handlers**: Implement specific side effects (email, notifications, etc.)

### Serialization Container
- **Event Serializer**: Core serialization logic
- **JSON Provider**: System.Text.Json implementation
- **Type Mapping**: CLR type to storage name translation

### UX Projections Container
- **UX Projection Grain**: UI-optimized projections
- **UX Event Handlers**: Transform events for UI consumption
- **UX Projection Store**: Stores UI-friendly data structures

## Component Interactions

1. **Command Processing**: Commands → Command Handlers → Events → Brook Storage
2. **State Reduction**: Events → Reducer Engine → Fold Functions → Current State
3. **Snapshot Management**: State → Snapshot Strategy → Snapshot Grain → Snapshot Storage
4. **Read Model Updates**: Events → Projection Grain → Event Handlers → Projection Storage
5. **Side Effects**: Events → Effect Runner → Effect Handlers → External Systems
6. **UI Projections**: Events → UX Grain → UX Handlers → UX Store
