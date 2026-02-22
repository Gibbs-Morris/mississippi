# Project Names (src)

| Project | Description | Suggested Name | Updated Description | Razor | AspNet | Orleans |
|---|---|---|---|:---:|:---:|:---:|
| Aqueduct.Abstractions | Public contracts and abstractions for Aqueduct. | Aqueduct.Abstractions | Interfaces and contracts for event flow processing, referenced by both gateway and runtime Aqueduct packages. | | | ✓ |
| Aqueduct | Core Aqueduct runtime for event flow and stream processing. | Aqueduct.Gateway | ASP.NET gateway for routing event flow and stream processing requests to the Aqueduct runtime. | | ✓ | ✓ |
| Aqueduct.Grains | Orleans grain implementations and integrations for Aqueduct streams and processing. | Aqueduct.Runtime | Orleans grain implementations for stateful event flow and stream processing. | | | ✓ |
| Common.Abstractions | Core shared abstractions and primitives used across Mississippi packages. | Common.Abstractions | Shared primitives, interfaces, and base types used across all Mississippi packages. | | | ✓ |
| Common.Cosmos.Abstractions | Public contracts and abstractions for Common.Cosmos. | Common.Runtime.Storage.Abstractions | Interfaces for shared runtime storage providers (e.g., Cosmos DB). | | | |
| Common.Cosmos | Azure Cosmos DB support and storage components shared across Mississippi packages. | Common.Runtime.Storage.Cosmos | Azure Cosmos DB implementation of the common runtime storage abstractions. | | | |
| EventSourcing.Aggregates.Abstractions | Public contracts and abstractions for EventSourcing.Aggregates. | DomainModeling.Abstractions | Contracts for aggregate command handling, saga orchestration, and UX projections. Merges Aggregates.Abs + Sagas.Abs + UxProjections.Abs. | | | ✓ |
| EventSourcing.Aggregates | Aggregate runtime for command handling and event-sourced state transitions. | DomainModeling.Runtime | Orleans runtime hosting aggregate grains, saga orchestration, and UX projection engines. Merges Aggregates + Sagas + UxProjections. | | | ✓ |
| EventSourcing.Aggregates.Api | HTTP API endpoints and integration for EventSourcing.Aggregates. | DomainModeling.Gateway | ASP.NET gateway exposing HTTP endpoints for aggregate commands and UX projection queries. Merges Aggregates.Api + UxProjections.Api. | | ✓ | |
| EventSourcing.Brooks.Abstractions | Public contracts and abstractions for EventSourcing.Brooks. | Brooks.Abstractions | Interfaces and contracts for event stream (brook) operations: append, read, subscribe, and storage naming attributes. | | | ✓ |
| EventSourcing.Brooks | Brook runtime for appending, reading, and managing event streams. | Brooks.Runtime | Orleans runtime for appending, reading, and managing persistent event streams. | | | ✓ |
| EventSourcing.Brooks.Cosmos | Azure Cosmos DB provider for EventSourcing.Brooks storage. | Brooks.Runtime.Storage.Cosmos | Azure Cosmos DB storage provider for persisting event streams. | | | |
| EventSourcing.Reducers.Abstractions | Public contracts and abstractions for EventSourcing.Reducers. | Tributary.Abstractions | Contracts for reducers and snapshot persistence. Merges Reducers.Abs + Snapshots.Abs. Snapshot storage interfaces extract to Tributary.Runtime.Storage.Abstractions. | | | |
| EventSourcing.Reducers | Reducer runtime for deterministic state projection from events. | Tributary.Runtime | Engine for applying reducers and managing snapshot persistence to optimize event replay. Merges Reducers + Snapshots. | | | |
| EventSourcing.Sagas.Abstractions | Public contracts and abstractions for EventSourcing.Sagas. | DomainModeling.Abstractions | ↳ Contributes saga orchestration contracts for long-running, event-driven workflows. | | | ✓ |
| EventSourcing.Sagas | Saga orchestration runtime for long-running, event-driven workflows. | DomainModeling.Runtime | ↳ Contributes saga orchestration engine coordinating multi-step workflows. | | | |
| EventSourcing.Serialization.Abstractions | Public contracts and abstractions for EventSourcing.Serialization. | Brooks.Serialization.Abstractions | Interfaces for event serialization and deserialization strategies. | | | |
| EventSourcing.Serialization.Json | JSON serialization implementation for EventSourcing.Serialization. | Brooks.Serialization.Json | JSON serialization provider for event persistence and transport. | | | |
| EventSourcing.Snapshots.Abstractions | Public contracts and abstractions for EventSourcing.Snapshots. | Tributary.Abstractions | ↳ Contributes snapshot persistence and retrieval contracts. Storage interfaces extract to Tributary.Runtime.Storage.Abstractions. | | | ✓ |
| EventSourcing.Snapshots | Snapshot runtime for persisted aggregate state checkpoints. | Tributary.Runtime | ↳ Contributes snapshot persistence and restoration engine. Dead reference to Aggregates.Abs removed. | | | ✓ |
| EventSourcing.Snapshots.Cosmos | Azure Cosmos DB provider for EventSourcing.Snapshots storage. | Tributary.Runtime.Storage.Cosmos | Azure Cosmos DB storage provider for persisting state snapshots. | | | |
| EventSourcing.UxProjections.Abstractions | Public contracts and abstractions for EventSourcing.UxProjections. | DomainModeling.Abstractions | ↳ Contributes UX projection contracts for client-optimized read models delivered via Inlet. | | | ✓ |
| EventSourcing.UxProjections | UX projection runtime for client-optimized read models. | DomainModeling.Runtime | ↳ Contributes UX projection engine for client-optimized read models. | | | ✓ |
| EventSourcing.UxProjections.Api | HTTP API endpoints and integration for EventSourcing.UxProjections. | DomainModeling.Gateway | ↳ Contributes UX projection query endpoints. | | ✓ | |
| Inlet.Abstractions | Public contracts and abstractions for Inlet. | Inlet.Abstractions | Shared interfaces for Inlet real-time data transport between client, gateway, and runtime. | | | |
| Inlet.Client.Abstractions | Public contracts and abstractions for Inlet.Client. | Inlet.Client.Abstractions | Interfaces for consuming Inlet projection streams and state in client applications. | | | |
| Inlet.Client | Client runtime for consuming Inlet projection streams and APIs. | Inlet.Client | Client library for subscribing to Inlet projection streams and synchronizing state with Reservoir. | | | |
| Inlet.Client.Generators | Source generators for Inlet.Client to reduce boilerplate. | Inlet.Client.Generators | Source generators that produce typed Inlet client proxies from hub definitions. | | | |
| Inlet.Generators.Abstractions | Public contracts and abstractions for Inlet.Generators. | Inlet.Generators.Abstractions | Shared types and attributes for Inlet source generator pipelines. | | | |
| Inlet.Generators.Core | Core source generation engine and pipeline for Inlet. | Inlet.Generators.Core | Core source generation engine shared by client, gateway, and runtime Inlet generators. | | | |
| Inlet.Server.Abstractions | Public contracts and abstractions for Inlet.Server. | Inlet.Gateway.Abstractions | Interfaces for the Inlet gateway transport layer (HTTP/SignalR endpoints). | | | |
| Inlet.Server | Server runtime for exposing Inlet projections and API transport. | Inlet.Gateway | ASP.NET gateway that routes Inlet messages between clients and the runtime over HTTP/SignalR. | | ✓ | |
| Inlet.Server.Generators | Source generators for Inlet.Server to reduce boilerplate. | Inlet.Gateway.Generators | Source generators that produce typed Inlet gateway endpoints from hub definitions. | | | |
| Inlet.Silo.Abstractions | Public contracts and abstractions for Inlet.Silo. | Inlet.Runtime.Abstractions | Interfaces for Inlet runtime grain contracts and stream processing. | | | ✓ |
| Inlet.Silo | Orleans silo runtime integration for Inlet streams and projections. | Inlet.Runtime | Orleans grain runtime for Inlet stream processing, saga integration, and projection delivery. | | | ✓ |
| Inlet.Silo.Generators | Source generators for Inlet.Silo to reduce boilerplate. | Inlet.Runtime.Generators | Source generators that produce typed Inlet runtime grain implementations from hub definitions. | | | |
| Mississippi.EventSourcing.Testing | Testing utilities and fixtures for Mississippi.EventSourcing. | DomainModeling.TestHarness | Test fixtures, builders, and fakes for unit-testing aggregates and reducers. | | | ✓ |
| Mississippi.Reservoir.Testing | Testing utilities and fixtures for Mississippi.Reservoir. | Reservoir.TestHarness | Test fixtures and fakes for unit-testing Reservoir state, actions, and reducers. | | | |
| Refraction.Abstractions | Public contracts and abstractions for Refraction. | Refraction.Abstractions | Interfaces and contracts for the Refraction UI component model. | | | |
| Refraction | Refraction runtime for projection-driven reactive application flows. | Refraction.Client | Standalone Blazor component library for building reactive UIs without framework coupling. | ✓ | | |
| Refraction.Pages | State management integration for Refraction via Reservoir. | Refraction.Client.StateManagement | Connects Refraction components to Reservoir for integrated state management in Mississippi applications. | ✓ | | |
| Reservoir.Abstractions | Public contracts and abstractions for Reservoir. | Reservoir.Abstractions | Interfaces and contracts for Redux-style state management: actions, reducers, selectors, and effects. | | | |
| Reservoir | Core Reservoir Redux-style state management runtime for Mississippi clients. | Reservoir.Core | Core state management engine: action dispatch, reducer execution, selector memoization, and effect coordination. | | | |
| Reservoir.Blazor | Blazor integration for Reservoir state, actions, reducers, and effects. | Reservoir.Client | Blazor integration for Reservoir: component bindings, state subscriptions, and render-optimized selectors. | ✓ | | |
| Sdk.Client | Convenience SDK for integrating Mississippi in client applications. | Sdk.Client | Meta-package that bundles all client-side Mississippi packages for rapid application setup. | ✓ | | |
| Sdk.Server | Convenience SDK for integrating Mississippi in server applications. | Sdk.Gateway | Meta-package that bundles all gateway-side Mississippi packages for rapid ASP.NET host setup. | | ✓ | |
| Sdk.Silo | Convenience SDK for integrating Mississippi in silo applications. | Sdk.Runtime | Meta-package that bundles all runtime-side Mississippi packages for rapid Orleans silo setup. | | | |

## Rules

- Repository prefix note: `Directory.Build.props` adds `Mississippi.` to package/assembly identity, so suggested names stay unprefixed.
- Use PascalCase dot-separated segments.
- Base pattern for project names: `<Feature>.<Role>`.
- Agreed role pattern: `<Feature>.Abstractions`, `<Feature>.Core`, `<Feature>.Client`, `<Feature>.Gateway`, `<Feature>.Runtime`.
- Storage/provider pattern: `<Feature>.Runtime.Storage.Abstractions`, `<Feature>.Runtime.Storage.Cosmos`, `<Feature>.Runtime.Storage.SqlServer`, `<Feature>.Runtime.Storage.Postgres`, `<Feature>.Runtime.Storage.Table`.
- Serialization provider pattern: `<Feature>.Serialization.Abstractions`, `<Feature>.Serialization.Json`, `<Feature>.Serialization.Protobuf`, etc. Named by format, not by role.
- Prefer role names over technology names (`Gateway` over `Api`, `Client` over `Blazor`, `Runtime` over `Grains`).
- Keep one package = one concern; do not mix client/gateway/runtime logic.
- Test support packages should use `TestHarness` naming to avoid confusion with test projects (for example `<Feature>.TestHarness`).
- Keep naming consistent within a feature family (Abstractions + runtime roles should share the same feature stem).

## Validation Columns

The table includes three checkbox columns to validate suggested role assignments against actual .csproj file content:

| Column | Detection Rule | Expected Role |
|---|---|---|
| **Razor** | SDK is `Microsoft.NET.Sdk.Razor` | Should be **Client** |
| **AspNet** | References `Microsoft.AspNetCore.App` via `<FrameworkReference>` | Should be **Gateway** |
| **Orleans** | References `Microsoft.Orleans.Sdk` via `<PackageReference>` | Should be **Runtime** (unless also Gateway or Abstractions) |

### Validation Rules

- **Razor ✓** → Suggested name MUST contain `.Client` (e.g., `Refraction.Client`, `Reservoir.Client`, `Sdk.Client`).
- **AspNet ✓** → Suggested name MUST contain `.Gateway` (e.g., `EventSourcing.Aggregates.Gateway`, `Inlet.Gateway`, `Sdk.Gateway`). Takes precedence over Orleans.
- **Orleans ✓** (without AspNet) → Suggested name MUST contain `.Runtime`, or be an `.Abstractions`/`.TestHarness` package that uses Orleans for code generation.
- **No checkmarks** → Package is `.Abstractions` (no runtime deps), `.Core` (shared logic), storage provider, serialization provider, or generator.

### Edge Cases

- **Orleans + AspNet both checked**: AspNet takes precedence → package is Gateway (hybrid scenario, may need refactoring).
- **Abstractions with Orleans ✓**: Orleans source generators require `Microsoft.Orleans.Sdk` even for abstractions; these are still `.Abstractions` packages.
- **TestHarness with Orleans ✓**: Test harness packages may reference `Microsoft.Orleans.Sdk` for serialization of test types; they are still `.TestHarness` packages.
- **Gateway with Orleans ✓**: Some gateway packages (e.g., Aqueduct.Gateway) also reference Orleans.Sdk; the AspNet dependency is the role signal.
- **Generators**: Source generators use standard SDK and have no runtime framework dependencies.
- **Serialization providers**: Named by format (e.g., `.Json`, `.Protobuf`) rather than by role, following the same pattern as storage providers (`.Cosmos`, `.SqlServer`).

## Validation Findings

All projects now pass validation. Previously resolved mismatches:

| Project | Detected | Original | Resolution |
|---|---|---|---|
| Aqueduct | AspNet ✓, Orleans ✓ | Aqueduct.Core | → **Aqueduct.Gateway** (AspNet takes precedence) |
| EventSourcing.Aggregates | Orleans ✓ | EventSourcing.Aggregates.Core | → **EventSourcing.Aggregates.Runtime** (Orleans = stateful execution) |

### Additional Naming Decisions

| Project | Original | Resolution | Rationale |
|---|---|---|---|
| Refraction.Pages | Refraction.Client.Pages | → **Refraction.Client.StateManagement** | Clarifies purpose: integrates with Reservoir for state management |

## EventSourcing Three-Layer Architecture

The 19 `EventSourcing.*` projects restructure into three named layers (14 final packages). Water names for infrastructure plumbing; clear naming for the developer-facing domain layer.

### Layer 1 — Brooks (Event Storage)

Lowest layer. Owns event stream persistence, serialization, and storage naming.

| Package | Description | Dependencies |
|---|---|---|
| Brooks.Abstractions | Interfaces for event stream (brook) append, read, subscribe, and storage naming attributes. | Common.Abstractions |
| Brooks.Runtime | Orleans runtime for managing persistent event streams. | Brooks.Abstractions |
| Brooks.Runtime.Storage.Abstractions | Interfaces for brook storage providers (IBrookStorageProvider, Reader, Writer). Extracted from Brooks.Abstractions. | Brooks.Abstractions |
| Brooks.Runtime.Storage.Cosmos | Azure Cosmos DB storage provider for event streams. | Brooks.Runtime.Storage.Abstractions, Common.Runtime.Storage.Cosmos |
| Brooks.Serialization.Abstractions | Interfaces for event serialization and deserialization strategies. | _(none)_ |
| Brooks.Serialization.Json | JSON serialization provider for event persistence. | Brooks.Serialization.Abstractions |

### Layer 2 — Tributary (State Transformation)

Mid-layer. Owns reducers (deterministic state projection) and snapshots (state checkpoints).

| Package | Description | Dependencies |
|---|---|---|
| Tributary.Abstractions | Contracts for reducers and snapshot persistence/retrieval. Merges Reducers.Abs + Snapshots.Abs. | Brooks.Abstractions |
| Tributary.Runtime | Engine for applying reducers and managing snapshot persistence. Merges Reducers + Snapshots. | Tributary.Abstractions, Brooks.Runtime, Brooks.Serialization.Abstractions |
| Tributary.Runtime.Storage.Abstractions | Interfaces for snapshot storage providers (ISnapshotStorageProvider, Reader, Writer). Extracted from Snapshots.Abstractions. | Tributary.Abstractions |
| Tributary.Runtime.Storage.Cosmos | Azure Cosmos DB storage provider for state snapshots. | Tributary.Runtime.Storage.Abstractions, Common.Runtime.Storage.Cosmos |

### Layer 3 — DomainModeling (Application / Domain)

Top layer. Owns aggregates, sagas, UX projections, and the test harness.

| Package | Description | Dependencies |
|---|---|---|
| DomainModeling.Abstractions | Contracts for aggregate command handling, saga orchestration, and UX projections. | Brooks.Abstractions, Inlet.Abstractions |
| DomainModeling.Runtime | Orleans runtime hosting aggregate grains, saga orchestration, and UX projection engines. | DomainModeling.Abstractions, Brooks.Runtime, Tributary.Runtime, Brooks.Serialization.Abstractions |
| DomainModeling.Gateway | ASP.NET gateway exposing HTTP endpoints for aggregate commands and UX projection queries. | DomainModeling.Abstractions, Common.Abstractions, Inlet.Abstractions |
| DomainModeling.TestHarness | Test fixtures, builders, and fakes for unit-testing aggregates and reducers. | DomainModeling.Abstractions, Tributary.Abstractions |

### Dependency Flow

```text
DomainModeling (Layer 3)
  ├─→ Tributary (Layer 2)
  │     └─→ Brooks (Layer 1)
  └─→ Brooks (Layer 1)
```

Layers depend strictly downward. No upward or lateral violations.

### Merge Mapping

| Source Project(s) | Target Package | Notes |
|---|---|---|
| Aggregates.Abs + Sagas.Abs + UxProjections.Abs | DomainModeling.Abstractions | Merge |
| Aggregates + Sagas + UxProjections | DomainModeling.Runtime | Merge |
| Aggregates.Api + UxProjections.Api | DomainModeling.Gateway | Merge |
| Reducers.Abs + Snapshots.Abs | Tributary.Abstractions | Merge |
| Reducers + Snapshots | Tributary.Runtime | Merge |
| _(extracted from Brooks.Abs)_ | Brooks.Runtime.Storage.Abstractions | New package |
| _(extracted from Snapshots.Abs)_ | Tributary.Runtime.Storage.Abstractions | New package |
| EventSourcing.Brooks.Abstractions | Brooks.Abstractions | Rename; storage interfaces extracted |
| EventSourcing.Brooks | Brooks.Runtime | Rename |
| EventSourcing.Brooks.Cosmos | Brooks.Runtime.Storage.Cosmos | Rename |
| EventSourcing.Serialization.Abstractions | Brooks.Serialization.Abstractions | Rename (moves under Brooks stem) |
| EventSourcing.Serialization.Json | Brooks.Serialization.Json | Rename (moves under Brooks stem) |
| EventSourcing.Snapshots.Cosmos | Tributary.Runtime.Storage.Cosmos | Rename |
| Mississippi.EventSourcing.Testing | DomainModeling.TestHarness | Rename |

### Cleanup Notes

- **Dead reference**: Snapshots → Aggregates.Abstractions has a `ProjectReference` but zero code usage. Remove during restructure.
- **Misplaced types**: `ISnapshotTypeRegistry` and `IEventTypeRegistry` currently live in Aggregates.Abstractions. These are infrastructure concerns that should move to Tributary.Abstractions and Brooks.Abstractions (or Brooks.Serialization.Abstractions) respectively.
- **Storage interface extraction**: `IBrookStorageProvider/Reader/Writer` extract from Brooks.Abstractions to Brooks.Runtime.Storage.Abstractions. `ISnapshotStorageProvider/Reader/Writer` extract from Snapshots.Abstractions to Tributary.Runtime.Storage.Abstractions.