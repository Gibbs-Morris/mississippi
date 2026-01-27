---
id: industry-concepts
title: Industry Concepts Glossary
sidebar_label: Industry Concepts
description: Industry-standard technologies, patterns, and frameworks used in Mississippi
keywords:
  - actor model
  - orleans
  - grain
  - silo
  - blazor
  - signalr
  - aspnet
  - docker
  - kubernetes
  - redux
  - flux
sidebar_position: 1
---

# Industry Concepts Glossary

Industry-standard technologies, architectural patterns, and frameworks that Mississippi builds upon. These are not Mississippi-specific—they are widely used concepts in the .NET and distributed systems ecosystem.

## Architectural Patterns

### Actor Model

A mathematical model of concurrent computation where "actors" are independent units that encapsulate state and behavior, communicating exclusively through asynchronous message passing. Each actor processes messages sequentially (one at a time), eliminating shared mutable state and race conditions. Actors can create new actors, send messages to other actors, and decide how to respond to the next message.

**Key characteristics**: Location transparency, fault isolation, natural distribution model, no locks or shared memory.

### Virtual Actor Model

An evolution of the classic actor model where actors (grains) are **always available** through an identity-based reference, automatically activating when called and deactivating when idle. Unlike traditional actors that must be explicitly created and destroyed, virtual actors exist conceptually at all times—the runtime handles lifecycle management transparently. Mississippi uses Orleans' virtual actor implementation where grains activate on-demand, persist state, and scale horizontally across a cluster.

**Key characteristics**: Automatic activation/deactivation, transparent lifecycle, identity-based addressing, single-threaded execution per grain.

**Source**: [Orleans documentation](https://learn.microsoft.com/en-us/dotnet/orleans/)

### Redux

A predictable state container pattern for JavaScript/TypeScript applications, commonly used with React. Redux enforces unidirectional data flow: **Actions** describe what happened → **Reducers** compute new state → **Store** holds the single source of truth → **Views** render based on state. Key principles include single state tree, read-only state (immutability), and pure reducer functions.

**Key concepts**: Store (state container), Action (event descriptor), Reducer (pure state transformer), Middleware (side effect handling), Selector (state queries).

**In Mississippi**: Reservoir implements Redux-style state management for Blazor, adapting the pattern to .NET with `IStore`, `IAction`, `ActionReducerBase`, and `ActionEffectBase`.

**Source**: [Redux documentation](https://redux.js.org/)

### Flux

Facebook's application architecture pattern for building client-side web applications with unidirectional data flow. Flux introduced the dispatcher → store → view cycle that influenced Redux and other state management libraries. Unlike Redux (single store), Flux allows multiple stores, each managing a domain of application state.

**Key concepts**: Dispatcher (central hub routing actions), Store (domain-specific state and logic), Action (payload describing events), View (React components).

**Relationship to Redux**: Redux simplified Flux by combining dispatcher and store concepts, enforcing a single store, and making reducers pure functions.

### Atomic Design

A UI composition methodology by Brad Frost that organizes interface elements into a five-level hierarchy: **Atoms** (basic building blocks like buttons, inputs, labels), **Molecules** (simple component groups like search forms), **Organisms** (complex UI sections like headers, cards), **Templates** (page-level layouts defining structure), and **Pages** (specific template instances with real content). This hierarchy enables systematic component reuse, consistent design language, and scalable UI development by composing larger structures from smaller primitives.

**Principles**: Bottom-up composition, single responsibility per level, reusable primitives, predictable complexity progression, design system alignment.

**In Mississippi**: The Refraction UI library implements Atomic Design with clear folder structure for Atoms, Molecules, Organisms, and Templates.

**Source**: [Atomic Design by Brad Frost](https://atomicdesign.bradfrost.com/)

## Orleans (Virtual Actor Framework)

### Orleans Grain

The concrete implementation of a virtual actor in Microsoft Orleans. A grain is a .NET class that encapsulates state and behavior, identified by a unique key (string, GUID, or composite). Grains are single-threaded (one request processed at a time per instance), support persistent or transient state, and automatically distribute across an Orleans cluster.

**Key characteristics**: Identity-based addressing, automatic activation/deactivation, single-threaded execution, location transparency.

**Source**: [Orleans Grains documentation](https://learn.microsoft.com/en-us/dotnet/orleans/grains/)

### Orleans Silo

A host process running the Orleans runtime, responsible for activating/deactivating grains, routing messages, and participating in cluster membership. Multiple silos form an Orleans cluster, with each silo hosting a subset of active grains. Silos communicate via Orleans' internal messaging protocol and coordinate grain placement through the distributed grain directory.

**Key responsibilities**: Grain hosting, message routing, cluster membership, grain directory participation, persistence coordination.

**Source**: [Orleans Silos documentation](https://learn.microsoft.com/en-us/dotnet/orleans/host/configuration-guide/server-configuration)

### Orleans Cluster

A distributed system of multiple Orleans silos coordinating as one logical unit through membership protocols and grain directory services. A cluster is defined by a unique `ClusterId` (environment identifier) and `ServiceId` (logical application name). Silos join clusters via clustering providers (localhost, Azure Storage, SQL Server, etc.), maintain consistent grain placement via the distributed directory, and provide automatic failover when silos join/leave.

**Key characteristics**: Membership management, distributed grain directory, automatic failover, cluster-wide consistency, flexible clustering providers.

**Source**: [Orleans Clustering documentation](https://learn.microsoft.com/en-us/dotnet/orleans/implementation/cluster-management)

### Stateless Worker

An Orleans grain marked with `[StatelessWorker]` attribute that can have multiple concurrent activations across the cluster. Unlike regular grains (single activation per identity), stateless workers scale horizontally to handle high-throughput, stateless operations. Each silo can activate its own instance, and Orleans load-balances requests across available activations.

**Key characteristics**: Multiple activations allowed, no persistent identity state, local-silo preference for activation, horizontal scaling for throughput.

**Use cases**: Request routing, validation, transformation, fan-out coordination—any operation that doesn't require single-instance consistency.

**Source**: [Orleans Stateless Workers](https://learn.microsoft.com/en-us/dotnet/orleans/grains/stateless-worker-grains)

### Orleans Streams

Orleans' built-in pub/sub messaging system for asynchronous, reliable event delivery between grains and external clients. Streams provide implicit (grain-managed) or explicit subscriptions, with pluggable stream providers (memory, Azure Event Hubs, Azure Service Bus, etc.). Producers publish events to stream IDs; consumers subscribe and receive events asynchronously.

**Key concepts**: Stream namespace (logical grouping), Stream ID (unique identifier), Stream provider (transport implementation), Subscription (consumer registration).

**Stream providers**: `MemoryStreamProvider` (development/testing), `AzureQueueStreamProvider`, `EventHubStreamProvider`, `ServiceBusStreamProvider`.

**Source**: [Orleans Streams documentation](https://learn.microsoft.com/en-us/dotnet/orleans/streaming/)

### [OneWay] Attribute

An Orleans attribute marking grain methods as fire-and-forget—the caller doesn't wait for completion or receive a result. OneWay methods return immediately after the message is sent, improving throughput for notifications or logging where confirmation isn't needed. The method still executes on the grain but errors don't propagate to the caller.

**Usage**: Apply `[OneWay]` to grain interface methods returning `Task` (not `Task<T>`). Exceptions are logged but not thrown to the caller.

**Trade-offs**: Higher throughput vs. no delivery guarantee visibility, no error propagation, no result.

**Source**: [Orleans OneWay documentation](https://learn.microsoft.com/en-us/dotnet/orleans/grains/request-context)

## Blazor & Web Technologies

### Blazor WebAssembly (Blazor WASM)

A client-side hosting model for Blazor where the .NET runtime (Mono) executes directly in the browser via WebAssembly. The entire application (runtime, framework, app assemblies) downloads to the browser, enabling full offline capability and rich client-side interactivity without server round-trips for UI updates.

**Key features**: Client-side execution, offline capability, progressive web app (PWA) support, .NET debugging in browser DevTools, WebAssembly sandbox security.

**Source**: [Blazor WebAssembly documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-webassembly)

### WASM

WebAssembly: a W3C standard defining a binary instruction format for a stack-based virtual machine, enabling near-native performance in modern web browsers. WASM provides a portable compilation target for languages like C, C++, Rust, and .NET, allowing them to run in browsers alongside JavaScript.

**Key benefits**: Faster execution than JavaScript for compute-intensive tasks, deterministic performance, language-agnostic runtime, sandboxed security.

**Specification**: [WebAssembly Core Specification (W3C)](https://www.w3.org/TR/wasm-core-1/)

### Razor

The declarative template syntax used in Blazor components and ASP.NET views, blending HTML markup with C# code via `@` directives. Razor files (`.razor` or `.cshtml`) contain component markup, parameters, event handlers, and logic.

**Key syntax**: `@code` blocks (inline C#), `@using` (namespace imports), `@inject` (dependency injection), `@bind` (two-way data binding), control flow (`@if`, `@foreach`, `@switch`).

**Source**: [Razor syntax documentation](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/razor)

### HTML

HyperText Markup Language—the standard markup language for web pages rendered by browsers. In Blazor applications, HTML is produced by components via Razor syntax, with Blazor's renderer producing HTML DOM updates based on component state changes.

### CSS

Cascading Style Sheets—the styling language for HTML documents controlling layout, colors, typography, and animations. Modern CSS features include custom properties (design tokens), flexbox/grid layouts, and CSS animations.

### CSS Design Token

A design token is a named, reusable design decision (color, spacing, typography, timing) stored as a CSS custom property (`--token-name: value`). Tokens create a single source of truth for design values, enabling consistent theming and easy updates.

**Token structure**: Raw tokens (base palette values) → Semantic tokens (purpose-based aliases referencing raw tokens).

**Example**: `--rf-raw-neo-blue-n3: hsl(200, 90%, 60%)` (raw) → `--rf-color-action-primary: var(--rf-raw-neo-blue-n3)` (semantic).

## SignalR (Real-Time Communication)

### SignalR

A real-time web communication library from Microsoft (part of ASP.NET Core) enabling bidirectional communication between servers and clients. SignalR abstracts transport protocols (WebSockets, Server-Sent Events, Long Polling) and provides high-level APIs for pushing server-side updates to connected clients.

**Core concepts**: Hubs (server-side endpoints), Connections (client sessions), Groups (logical connection sets), Backplanes (for scaling across servers).

**Source**: [SignalR documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/)

### SignalR Hub

The server-side endpoint in SignalR that handles client connections and method invocations. Hubs provide strongly-typed methods clients can call and server methods that push data to clients. Each hub type manages its own set of connections and groups.

**Pattern**: Inherit from `Hub<TClient>` where `TClient` defines client-side methods. Use `Clients.All`, `Clients.Caller`, `Clients.Group(name)` to target messages.

### SignalR Connection

A persistent, bidirectional communication channel between a server and a single client, identified by a unique `ConnectionId` (GUID string). Connections are established when clients connect to a hub and terminated when clients disconnect or timeout.

**Lifecycle**: Client connects → server assigns ConnectionId → client subscribes to topics/groups → server pushes updates → client disconnects or times out.

### SignalR Group

A named collection of SignalR connections enabling targeted broadcasts. Groups are logical—clients explicitly join/leave groups during their session. Sending to a group delivers the message to all current members.

**Usage**: `Groups.AddToGroupAsync(connectionId, groupName)`, then `Clients.Group(groupName).Method(data)`.

### SignalR User

A SignalR identity representing an authenticated user across potentially multiple connections. When a user connects from multiple devices/tabs, all connections share the same user identifier (from authentication claims).

**Usage**: `Clients.User(userId).Method(data)` sends to all connections for that authenticated user.

### SignalR Backplane

Infrastructure enabling SignalR to scale across multiple server processes by synchronizing connection state and message delivery. Traditional backplanes use external stores (Redis, Azure Service Bus, SQL Server) as message buses.

**Trade-offs**: External backplanes add latency and infrastructure dependencies.

### HubLifetimeManager

The ASP.NET Core SignalR abstraction responsible for connection lifecycle, group management, and message routing. Every hub type has a `HubLifetimeManager<THub>` that handles adding/removing connections, group management, and message delivery coordination.

**Default implementations**: `DefaultHubLifetimeManager` (single-server), `RedisHubLifetimeManager` (Redis backplane).

## ASP.NET Core & Server Infrastructure

### ASP.NET Core

Microsoft's cross-platform, high-performance web framework for building modern web applications and APIs. Provides HTTP endpoints (controllers, minimal APIs), SignalR hubs, middleware pipeline, and integrates with authentication, authorization, and observability.

**Source**: [ASP.NET Core documentation](https://learn.microsoft.com/en-us/aspnet/core/)

### Controller

An ASP.NET Core component that handles HTTP requests and returns responses, typically for RESTful API endpoints. Controllers inherit from `ControllerBase` with `[ApiController]` attribute, defining action methods mapped to HTTP verbs and routes.

### DTO (Data Transfer Object)

A simple data container used to transfer data between layers (client/server, API/domain). DTOs are typically immutable records with only properties, no behavior, decoupling public API contracts from internal domain models.

### Scalar (OpenAPI)

An interactive API documentation tool that provides a modern UI for exploring OpenAPI specifications. Scalar is an alternative to Swagger UI with improved design and customizable themes.

**Source**: [Scalar documentation](https://github.com/scalar/scalar)

## .NET Development Stack

### C\#

Microsoft's modern, type-safe, object-oriented programming language running on .NET. C# provides first-class support for async/await, pattern matching, records (immutable data types), source generators, and nullable reference types.

**Source**: [C# documentation](https://learn.microsoft.com/en-us/dotnet/csharp/)

### PowerShell

Microsoft's cross-platform automation shell and scripting language. PowerShell scripts (`.ps1` files) automate builds, tests, and deployments using PowerShell 7+ features.

**Source**: [PowerShell documentation](https://learn.microsoft.com/en-us/powershell/)

### dotnet CLI

The .NET command-line interface for building, running, testing, and managing .NET applications. Commands include `dotnet build`, `dotnet test`, `dotnet add package`, and `dotnet tool restore`.

**Source**: [.NET CLI documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/)

## Infrastructure & Orchestration

### .NET Aspire

Microsoft's opinionated stack for building observable, production-ready distributed applications. Aspire provides orchestration (AppHost), service defaults, component integrations (Azure, databases, messaging), and a developer dashboard.

**Core concepts**: AppHost (orchestrator), Resources (services, databases, storage), References (dependency injection), Emulators (local development).

**Source**: [.NET Aspire documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)

### Docker

A containerization platform that packages applications with their dependencies into portable, isolated containers. Docker containers provide consistent environments across development, testing, and production.

**In .NET Aspire**: Used for running Azure service emulators (Azurite, Cosmos DB emulator) during local development.

### Kubernetes

An open-source container orchestration platform for automating deployment, scaling, and management of containerized applications. Kubernetes manages container lifecycle, service discovery, load balancing, and rolling updates.

**Source**: [Kubernetes documentation](https://kubernetes.io/docs/)

---

## See Also

- [Event Sourcing Glossary](event-sourcing.md) — Mississippi's event sourcing framework
- [Reservoir & Inlet Glossary](reservoir-inlet.md) — Client-side state and real-time subscriptions
- [Aqueduct & Server Glossary](aqueduct-server.md) — Server-side Mississippi components
