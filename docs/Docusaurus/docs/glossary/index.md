---
id: glossary
title: Glossary
sidebar_label: Overview
sidebar_position: 0
description: Definitions of key terms and acronyms used in Mississippi Framework
keywords:
  - glossary
  - terminology
  - definitions
---

# Glossary

Defines recurring terms and acronyms to ensure all readers share the same vocabulary. The glossary separates **industry-standard concepts** from **Mississippi-specific terminology** for clarity.

## Industry Standards

### [Industry Concepts](industry-concepts.md)

Standard technologies and patterns Mississippi builds upon (not Mississippi-specific):

- **Architectural Patterns**: Actor Model, Virtual Actor Model, Redux, Flux, Atomic Design
- **Orleans**: Grain, Silo, Cluster, Stateless Worker, Streams, [OneWay] Attribute
- **Blazor & Web**: Blazor WASM, WASM, Razor, HTML, CSS, CSS Design Token
- **SignalR**: Hub, Connection, Group, User, Backplane, HubLifetimeManager
- **ASP.NET Core**: Controller, DTO, Scalar
- **.NET Stack**: C#, PowerShell, dotnet CLI
- **Infrastructure**: .NET Aspire, Docker, Kubernetes

## Mississippi Framework

### [Event Sourcing](event-sourcing.md)

Mississippi's event sourcing framework:

- **Commands & Handlers**: Command, Command Handler
- **Aggregates**: Aggregate, Event, EventReducer, EventEffect
- **Brooks**: Brook, Brook Storage Provider
- **Snapshots**: Snapshot, Snapshot Storage Provider
- **Projections**: Projection, UX Projection, Saga

### [Reservoir & Inlet](reservoir-inlet.md)

Mississippi's client-side state and real-time subscriptions:

- **Reservoir**: Store, Action, ActionReducer, ActionEffect, FeatureState, StoreComponent
- **Inlet**: InletComponent, IInletStore, IProjectionCache, InletHub, IInletSubscriptionGrain, [ProjectionPath]

### [Aqueduct & Server](aqueduct-server.md)

Mississippi's server-side components:

- **Aqueduct**: AqueductHubLifetimeManager, ISignalRClientGrain, ISignalRGroupGrain, IAqueductNotifier
- **Source Generation**: Client Generators, Server Generators, Silo Generators, UxProjectionControllerBase, IMapper
- **Refraction**: Component library, RefractionTokens
- **Storage**: Storage Provider, Cosmos DB providers

## Term Count Summary

| Page | Terms | Type |
|------|-------|------|
| [Industry Concepts](industry-concepts.md) | 32 | Industry |
| [Event Sourcing](event-sourcing.md) | 14 | Mississippi |
| [Reservoir & Inlet](reservoir-inlet.md) | 15 | Mississippi |
| [Aqueduct & Server](aqueduct-server.md) | 16 | Mississippi |
| **Total** | **77** | |
