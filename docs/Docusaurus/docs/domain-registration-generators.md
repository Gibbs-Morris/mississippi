---
id: domain-registration-generators
title: Domain Registration Generators
sidebar_label: Domain Registrations
sidebar_position: 6
description: Use generated domain-level registration methods to compose client, server, and silo feature registrations per domain.
---

# Domain Registration Generators

## Overview

Focus: Public API / Developer Experience.

Mississippi now supports generated domain-composition registration methods that group generated feature registrations by domain and host type. This reduces host startup wiring from many per-feature calls to one domain-level call per host.

## What Gets Generated

Three generators emit host-specific extension classes and methods:

| Host | Generated Class | Method Suffix | Typical Call |
|------|------------------|---------------|--------------|
| Client | `DomainFeatureRegistrations` | `Client` | `services.AddSpringDomainClient();` |
| Server | `DomainServerRegistrations` | `Server` | `services.AddSpringDomainServer();` |
| Silo | `DomainSiloRegistrations` | `Silo` | `services.AddSpringDomainSilo();` |

Source:

- [DomainClientRegistrationGenerator](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Client.Generators/DomainClientRegistrationGenerator.cs)
- [DomainServerRegistrationGenerator](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Server.Generators/DomainServerRegistrationGenerator.cs)
- [DomainSiloRegistrationGenerator](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Silo.Generators/DomainSiloRegistrationGenerator.cs)

### Composition Flow

```mermaid
flowchart LR
    A[Generated Feature Methods] --> B[Domain Registration Generator]
    B --> C[Add{Domain}Client]
    B --> D[Add{Domain}Server]
    B --> E[Add{Domain}Silo]
    C --> F[Host Program.cs]
    D --> F
    E --> F
```

## Naming Rules

The domain method prefix is derived from the domain root namespace, then converted to PascalCase and prefixed with `Add`.

- `TestApp.Domain` becomes `AddTestAppDomain...`
- `CoreLogic` becomes `AddCoreLogic...`

Domain root extraction rules:

1. If namespace contains `.Aggregates.`, use everything before that segment.
2. Otherwise, if namespace contains `.Projections.`, use everything before that segment.
3. Otherwise, use the full source namespace.

Source:

- [GetDomainRegistrationMethodName](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Core/Naming/NamingConventions.cs)
- [GetDomainRootNamespace](https://github.com/Gibbs-Morris/mississippi/blob/main/src/Inlet.Generators.Core/Naming/NamingConventions.cs)

## Generator Inputs by Host

Each host generator composes different generated feature types:

| Host | Included generated registrations | Key discovery attributes |
|------|----------------------------------|--------------------------|
| Client | Aggregate features, saga features, and projection features | `GenerateCommand`, `GenerateSagaEndpoints`, `GenerateProjectionEndpoints` |
| Server | Aggregate mappers and projection mappers | `GenerateCommand`, `GenerateProjectionEndpoints`, `ProjectionPath` |
| Silo | Aggregate, saga, and projection registrations | `GenerateAggregateEndpoints`, `GenerateSagaEndpoints`, `GenerateProjectionEndpoints` |

## Spring Sample Usage

The Spring sample now uses one domain-level call per host:

```csharp
// Spring.Client
builder.Services.AddSpringDomainClient();

// Spring.Server
builder.Services.AddSpringDomainServer();

// Spring.Silo
builder.Services.AddSpringDomainSilo();
```

Source:

- [Spring.Client Program.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Client/Program.cs)
- [Spring.Server Program.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Server/Program.cs)
- [Spring.Silo Program.cs](https://github.com/Gibbs-Morris/mississippi/blob/main/samples/Spring/Spring.Silo/Program.cs)

## Summary

- Domain registration generators reduce per-feature startup wiring.
- Generated method names are host-suffixed (`Client`, `Server`, `Silo`).
- Method prefixes come from the detected domain root namespace.
- Spring demonstrates the intended usage pattern in all three hosts.

## Next Steps

- [Event Sourcing Sagas](./event-sourcing-sagas.md)
- [Saga Public APIs](./event-sourcing-sagas-public-apis.md)
- [Documentation Guide](./contributing/documentation-guide.md)
