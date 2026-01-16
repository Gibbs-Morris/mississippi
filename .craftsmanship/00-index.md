# Mississippi Repository Craftsmanship Review

## Overview

This document serves as the index and navigation hub for the comprehensive craftsmanship review of the Mississippi repository - an event sourcing framework for .NET built on Orleans.

## Definition of Done

The review is complete when:

- [x] Pass 0: Gitignore rules analyzed and complete inventory created
- [ ] Pass 1: Every non-ignored file reviewed for local context (file-by-file analysis)
- [ ] Pass 2: Holistic review completed with architectural context
- [ ] Pass 3: Action plan produced with prioritized recommendations

## Documents

| Document | Description | Status |
|----------|-------------|--------|
| [01-inventory.md](./01-inventory.md) | Complete list of non-ignored files with Pass 1/2 checkboxes | In Progress |
| [02-projects.md](./02-projects.md) | Project descriptions, dependencies, and Mermaid diagrams | Pending |
| [03-file-reviews.md](./03-file-reviews.md) | Per-file review notes (Pass 1 and Pass 2) | Pending |
| [04-architecture.md](./04-architecture.md) | Current (as-is) and proposed (to-be) architecture | Pending |
| [05-recommendations.md](./05-recommendations.md) | Ordered action list with priority, effort, risk | Pending |
| [06-missing-functionality.md](./06-missing-functionality.md) | Suspected missing capabilities with evidence | Pending |

## Resume Point

**Current Phase:** Pass 0 - Inventory Creation

**Next File to Process:** Starting inventory creation

**Last Updated:** 2026-01-16T00:44:28.918Z

## Repository Summary

- **Repository:** Gibbs-Morris/mississippi
- **Primary Language:** C# (.NET 9.0)
- **Framework Type:** Event Sourcing Framework built on Orleans
- **Total Non-Ignored Files:** ~1105 files
- **Solution Files:** mississippi.slnx (main), samples.slnx (samples)

## Key Technologies

- .NET 9.0 with C# 13.0
- Microsoft Orleans 9.2.1
- Azure Cosmos DB (event storage)
- Azure Blob Storage (distributed locking)
- .NET Aspire (hosting/testing)
- SignalR (real-time projections)
- Blazor WebAssembly (client-side)

## Project Structure Overview

```text
mississippi/
├── src/                    # Core framework libraries
│   ├── Aqueduct*/          # SignalR Orleans integration
│   ├── Common*/            # Shared utilities
│   ├── EventSourcing*/     # Event sourcing core
│   ├── Inlet*/             # Projection subscriptions
│   └── Reservoir*/         # Blazor state management
├── tests/                  # Test projects (L0-L2)
├── samples/                # Sample applications
│   ├── Cascade/            # Chat application sample
│   └── Crescent/           # Counter sample
├── docs/                   # Docusaurus documentation
├── eng/                    # Engineering scripts
└── .github/                # GitHub workflows & instructions
```
