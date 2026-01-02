# Mississippi Framework Architecture

This folder contains C4 model architecture diagrams for the Mississippi Framework, created using Mermaid diagram syntax.

## C4 Model Diagrams

The C4 model provides a hierarchical approach to visualizing software architecture at different levels of abstraction:

### [C1: System Context Diagram](c1-system-context.md)
Shows the big picture - how the Mississippi Framework fits into the overall system landscape and its relationships with external systems and users.

**Key elements:**
- Mississippi Framework as the central system
- External dependencies (Orleans, Cosmos DB, .NET Runtime)
- User types (Developers, End Users)

### [C2: Container Diagram](c2-container.md)
Zooms into the Mississippi Framework to show the major containers (deployable units) and how they interact.

**Key elements:**
- Core infrastructure (Core, Hosting, AspNetCore.Orleans)
- Event Sourcing subsystem (Brooks, Aggregates, Projections, Snapshots, etc.)
- Storage providers (Cosmos DB integrations)
- Sample applications

### [C3: Component Diagram](c3-component.md)
Zooms into the Event Sourcing subsystem to show the internal components and their relationships.

**Key elements:**
- Aggregate management components
- Event storage (Brooks) components
- State reduction (Reducers) components
- Snapshot management components
- Projection components
- Side effects handling
- Serialization infrastructure

## Viewing the Diagrams

These diagrams use Mermaid syntax and can be viewed:

1. **On GitHub**: GitHub automatically renders Mermaid diagrams in Markdown files
2. **In VS Code**: Install the "Markdown Preview Mermaid Support" extension
3. **In JetBrains Rider**: Mermaid diagrams are supported natively in Markdown preview
4. **Online**: Copy the diagram code to [Mermaid Live Editor](https://mermaid.live/)

## About the Mississippi Framework

Mississippi is a sophisticated .NET 9.0 framework designed to streamline distributed application development with:

- **Event Sourcing**: Full event sourcing capabilities with the Brooks pattern
- **CQRS**: Command Query Responsibility Segregation with projections
- **Orleans Integration**: Built on Microsoft Orleans for distributed computing
- **Azure Cosmos DB**: Native support for cloud-native storage
- **DDD Support**: Domain-Driven Design patterns with aggregates and bounded contexts

## References

- [C4 Model Documentation](https://c4model.com/)
- [Mermaid Documentation](https://mermaid.js.org/)
- [Mississippi Framework README](../README.md)
