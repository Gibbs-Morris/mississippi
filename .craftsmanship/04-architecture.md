# Architecture Analysis

## Current Architecture (As-Is)

### High-Level Overview

Mississippi is an event sourcing framework built on Microsoft Orleans, following a clean layered architecture with clear separation of concerns.

```mermaid
graph TB
    subgraph "Client Layer"
        BC[Blazor Client]
        API[REST API]
    end

    subgraph "Real-time Layer"
        SH[SignalR Hub]
        AQ[Aqueduct]
    end

    subgraph "Application Layer"
        IN[Inlet]
        RES[Reservoir Store]
    end

    subgraph "Orleans Cluster"
        AG[Aggregate Grains]
        PR[Projection Grains]
        BW[Brook Writer Grains]
        BR[Brook Reader Grains]
        SC[Snapshot Cache Grains]
    end

    subgraph "Persistence Layer"
        CDB[(Cosmos DB)]
        BLOB[(Azure Blob)]
    end

    BC --> SH
    BC --> API
    API --> AG
    SH --> AQ
    AQ --> AG
    IN --> RES
    RES --> BC
    AG --> BW
    BW --> CDB
    BR --> CDB
    SC --> CDB
    AG --> SC
    BW --> BLOB
```

### Core Domain Architecture

```mermaid
classDiagram
    class CommandHandlerBase~TCommand,TSnapshot~ {
        <<abstract>>
        +Handle(command, state) OperationResult
        +TryHandle(command, state, result) bool
        #HandleCore(command, state) OperationResult*
    }

    class EventReducerBase~TEvent,TProjection~ {
        <<abstract>>
        +Reduce(state, event) TProjection
        +TryReduce(state, event, projection) bool
        #ReduceCore(state, event) TProjection*
    }

    class GenericAggregateGrain~TAggregate~ {
        -brookKey: BrookKey
        -lastKnownPosition: BrookPosition?
        +ExecuteAsync(command) OperationResult
        +GetStateAsync() TAggregate?
    }

    class BrookWriterGrain {
        +AppendEventsAsync(events, expectedPosition) BrookPosition
    }

    class SnapshotCacheGrain~T~ {
        +GetStateAsync() T?
    }

    GenericAggregateGrain --> CommandHandlerBase : uses
    GenericAggregateGrain --> BrookWriterGrain : writes to
    GenericAggregateGrain --> SnapshotCacheGrain : reads from
    SnapshotCacheGrain --> EventReducerBase : uses
```

### Event Flow Architecture

```mermaid
sequenceDiagram
    participant Client
    participant AggregateGrain
    participant CommandHandler
    participant BrookWriter
    participant SnapshotCache
    participant Reducer
    participant CosmosDB

    Client->>AggregateGrain: ExecuteAsync(command)
    AggregateGrain->>SnapshotCache: GetStateAsync()
    SnapshotCache-->>AggregateGrain: currentState
    AggregateGrain->>CommandHandler: Handle(command, state)
    CommandHandler-->>AggregateGrain: events[]
    AggregateGrain->>BrookWriter: AppendEventsAsync(events)
    BrookWriter->>CosmosDB: Store events
    BrookWriter-->>AggregateGrain: newPosition
    Note over SnapshotCache: Async update via stream
    SnapshotCache->>Reducer: Reduce(state, event)
    Reducer-->>SnapshotCache: newState
    AggregateGrain-->>Client: OperationResult.Ok()
```

### State Management Architecture (Blazor)

```mermaid
graph LR
    subgraph "Reservoir (Redux-like)"
        A[Action] --> D[Dispatch]
        D --> M[Middleware]
        M --> R[Reducers]
        R --> S[State]
        S --> C[Components]
        C --> A
        D --> E[Effects]
        E --> A
    end

    subgraph "Inlet (Projections)"
        P[Projection Grain] --> N[Notifier]
        N --> SR[SignalR]
        SR --> H[Hub Connection]
        H --> IE[Inlet Effect]
        IE --> A
    end
```

---

## Architectural Patterns Identified

### 1. CQRS Pattern

- **Command Side**: `GenericAggregateGrain` + `CommandHandlerBase` + `BrookWriterGrain`
- **Query Side**: `UxProjectionGrain` + `SnapshotCacheGrain` + Reducers

### 2. Event Sourcing Pattern

- **Event Storage**: Brooks (Cosmos DB event streams)
- **State Derivation**: Pure function reducers
- **Snapshotting**: Cached materialized views at specific versions

### 3. POCO Grain Pattern (Orleans 7+)

- Grains implement `IGrainBase` (not inherit from `Grain`)
- Dependencies injected via constructor
- Properties use get-only pattern

### 4. Abstractions Layering

- Every module has `.Abstractions` project with interfaces
- Implementations reference abstractions
- Consumers can depend on abstractions only

### 5. LoggerExtensions Pattern

- All logging via `[LoggerMessage]` source generators
- Structured, high-performance logging
- Consistent naming: `{Component}LoggerExtensions`

### 6. Redux-like State Management

- Reservoir provides Flux/Redux pattern for Blazor
- Actions, Reducers, Effects, Middleware
- Inlet integrates projections into Reservoir

---

## Architectural Strengths

### ✅ Excellent Separation of Concerns

- Clean abstractions layer for every module
- Consumers can mock/substitute at any boundary
- Test utilities provide in-memory implementations

### ✅ Type-Safe Command Handling

- Generic dispatch with compile-time type checking
- Pattern matching for command routing
- OperationResult for explicit error handling (no exceptions for business logic)

### ✅ Immutability Enforcement

- `EventReducerBase` throws if reducer returns same reference
- Aggregates are immutable records
- Events are immutable records

### ✅ Comprehensive Architecture Testing

- ArchUnitNET tests enforce patterns
- Naming conventions, layering, grain patterns all tested
- Automated enforcement prevents drift

### ✅ Observable & Instrumented

- OpenTelemetry metrics throughout
- Structured logging with LoggerExtensions
- Diagnostics classes in each module

### ✅ Developer Experience

- Central Package Management
- Comprehensive instruction files
- Sample applications demonstrate patterns

---

## Architectural Concerns

### ⚠️ Potential Issues

1. **Reflection in Store.cs (Lines 295-302)**
   - Uses reflection to call `Reduce` on generic root reducers
   - Performance concern for high-frequency dispatches
   - Could use compiled expression trees or source generators

2. **Exception Swallowing in Effects (Lines 333-340)**
   - Effects that throw exceptions are silently ignored
   - Could lose critical error information
   - Should emit error actions or log

3. **NoWarn List Size**
   - 19 analyzer rules suppressed globally
   - Some may hide legitimate issues (e.g., CA2007 ConfigureAwait)

4. **Missing Async Flow in Reducers**
   - Reducers are synchronous
   - Cannot perform async operations during reduction
   - This is correct for Redux but may cause patterns where async work is awkwardly handled

---

## Proposed Architecture (To-Be)

### Short-Term Improvements

```mermaid
graph TB
    subgraph "Immediate Changes"
        A[Review NoWarn suppressions]
        B[Add compiled delegates for Store]
        C[Improve effect error handling]
    end

    subgraph "Testing Improvements"
        D[Add L1 tests for Orleans integration]
        E[Expand architecture tests]
        F[Add contract tests for public APIs]
    end

    subgraph "Documentation"
        G[Add API documentation site]
        H[Expand sample coverage]
        I[Add migration guides]
    end
```

### Medium-Term Improvements

```mermaid
graph TB
    subgraph "Performance"
        A[Compiled expression caching in Store]
        B[Batch event writes optimization]
        C[Snapshot preloading strategies]
    end

    subgraph "Resilience"
        D[Circuit breaker for Cosmos calls]
        E[Retry policies with jitter]
        F[Dead letter queue for failed events]
    end

    subgraph "Observability"
        G[Distributed tracing correlation]
        H[Health check endpoints]
        I[Metrics dashboards]
    end
```

### Long-Term Vision

```mermaid
graph TB
    subgraph "Multi-Storage Support"
        A[PostgreSQL event store]
        B[SQL Server event store]
        C[Event Store DB integration]
    end

    subgraph "Advanced Features"
        D[Event versioning/upcasting]
        E[Saga/Process Manager support]
        F[Outbox pattern for integration events]
    end

    subgraph "Tooling"
        G[Event replay tools]
        H[Projection rebuilder]
        I[Admin dashboard]
    end
```

---

## Layering Rules

### Allowed Dependencies

```mermaid
graph TD
    subgraph "Abstraction Layer"
        AA[*.Abstractions]
    end

    subgraph "Implementation Layer"
        IA[Implementations]
        GEN[*.Generators]
    end

    subgraph "Storage Layer"
        SA[*.Cosmos / *.Memory]
    end

    subgraph "Integration Layer"
        INT[*.Api / *.Orleans / *.SignalR]
    end

    subgraph "Consumer Layer"
        APP[Applications]
    end

    IA --> AA
    SA --> AA
    INT --> AA
    INT --> IA
    GEN -.-> AA
    APP --> AA
    APP --> IA
    APP --> SA
    APP --> INT
```

### Forbidden Dependencies

- Abstractions MUST NOT depend on implementations
- Storage providers MUST NOT depend on each other
- Source generators MUST NOT depend on runtime implementations
- Test utilities MUST NOT be referenced by production code

---

## Module Responsibility Matrix

| Module | Responsibility | Key Abstractions | Key Implementations |
|--------|---------------|------------------|---------------------|
| Common | Shared utilities, constants | IMapper, MississippiDefaults | Mappers |
| Brooks | Event stream storage | IBrookStorageProvider | CosmosRepository |
| Aggregates | Command handling | ICommandHandler, IGenericAggregateGrain | GenericAggregateGrain |
| Reducers | State derivation | IEventReducer, IRootReducer | DelegateEventReducer |
| Snapshots | Materialized views | ISnapshotCacheGrain | SnapshotCacheGrain |
| UxProjections | Read models | IUxProjectionGrain | UxProjectionGrain |
| Aqueduct | SignalR scaling | IAqueductNotifier | AqueductHubLifetimeManager |
| Inlet | Projection subscriptions | IInletStore | InletStore |
| Reservoir | Blazor state | IStore, IAction | Store |

---

## Technology Stack Assessment

| Technology | Version | Assessment | Recommendation |
|------------|---------|------------|----------------|
| .NET | 9.0 | ✅ Current LTS | Maintain |
| Orleans | 9.2.1 | ✅ Latest stable | Maintain |
| Azure Cosmos | 3.54.1 | ✅ Current | Maintain |
| .NET Aspire | 13.1.0 | ✅ Latest | Maintain |
| xUnit | 2.9.3 | ✅ Current | Maintain |
| Moq | 4.20.72 | ⚠️ Consider NSubstitute | Evaluate |
| FluentAssertions | 8.3.0 | ✅ Current | Maintain |
