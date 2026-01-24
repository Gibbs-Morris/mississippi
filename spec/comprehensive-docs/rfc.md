# RFC: Comprehensive Documentation Update

## Problem Statement

The Mississippi framework documentation is incomplete. While Reservoir (Redux-style state management) has good coverage, the event sourcing components (aggregates, brooks, reducers, snapshots, projections) have only brief placeholder documentation.

Developers need comprehensive documentation to understand:
1. How each concept works
2. How to implement custom components
3. How concepts relate to each other

## Goals

1. Document all core Reservoir concepts with code examples
2. Expand platform documentation for event sourcing concepts
3. Add guides for building custom storage providers
4. Show usage patterns from sample applications

## Non-Goals

- Changing any source code
- Adding new features
- API reference generation (separate concern)

## Current State

### Well Documented
- Reservoir: Actions, Reducers, Effects, Store

### Needs Expansion
- State & Registration patterns
- Aggregates (domain model)
- Brooks (event streams)
- UX Projections

### Not Yet Documented
- Commands & Command Handlers
- Domain Events & Event Reducers
- Custom Event Storage Providers
- Snapshots
- Custom Snapshot Storage Providers

## Proposed Approach

Document topics incrementally:
1. User specifies a topic
2. Scan source code for interfaces and implementations
3. Find usage patterns in samples
4. Write comprehensive documentation with examples
5. Commit and proceed to next topic

## Diagrams

To be added per-topic during implementation.

## Risks

- Documentation may become stale as code evolves
- Need to verify examples compile (manual check)

## Mitigations

- Reference source code files in docs
- Use patterns from existing samples
