# Recommendations

This document contains an ordered list of actionable improvements identified during the craftsmanship review.

## Priority Legend

| Priority | Description |
|----------|-------------|
| **P0** | Critical - Security, correctness, or major architectural issue |
| **P1** | High - Significant improvement to quality, maintainability, or DX |
| **P2** | Medium - Important refinement with clear value |
| **P3** | Low - Nice-to-have improvement |

## Effort Legend

| Effort | Description |
|--------|-------------|
| **XS** | < 1 hour |
| **S** | 1-4 hours |
| **M** | 1-2 days |
| **L** | 3-5 days |
| **XL** | 1-2 weeks |

## Risk Legend

| Risk | Description |
|------|-------------|
| **Low** | Isolated change, well-tested |
| **Medium** | Moderate scope, some dependencies |
| **High** | Wide-reaching, careful coordination needed |

---

## Action Items

### Build & Configuration

| ID | Priority | Effort | Risk | Description | Dependencies |
|----|----------|--------|------|-------------|--------------|
| R001 | P2 | S | Low | **Review NoWarn suppressions in Directory.Build.props** - 19 rules are globally suppressed. Some (like CA2007 for ConfigureAwait) may hide legitimate issues. Evaluate each and document justification or remove. | None |
| R002 | P3 | XS | Low | **Align Microsoft.Extensions package versions** - Minor inconsistency between 9.0.11 and 9.0.1 versions in Directory.Packages.props. | None |
| R003 | P3 | S | Low | **Document NoWarn justifications** - Add XML comments explaining why each suppressed rule is appropriate for this codebase. | R001 |

### Code Quality

| ID | Priority | Effort | Risk | Description | Dependencies |
|----|----------|--------|------|-------------|--------------|
| R010 | P1 | M | Medium | **Eliminate reflection in Store.ReduceFeatureStates (Store.cs:295-302)** - Uses reflection to call Reduce on generic root reducers. Replace with compiled expression trees or source-generated dispatch. | None |
| R011 | P2 | S | Low | **Improve effect error handling (Store.cs:333-340)** - Effects that throw exceptions are silently swallowed. Add logging or emit error actions to make failures observable. | None |
| R012 | P2 | S | Low | **Add ConfigureAwait(false) where appropriate** - Review async methods in library code for ConfigureAwait usage, especially in Orleans grains where it may not be needed. | None |

### Testing

| ID | Priority | Effort | Risk | Description | Dependencies |
|----|----------|--------|------|-------------|--------------|
| R020 | P2 | L | Low | **Add L1 integration tests for core grain interactions** - Current tests are mostly L0 unit tests. Add L1 tests with in-memory Orleans clusters for grain-to-grain communication. | None |
| R021 | P2 | M | Low | **Add contract tests for public APIs** - Create tests that verify public API signatures don't break between versions. | None |
| R022 | P3 | M | Low | **Add property-based tests for reducers** - Use FsCheck or similar to verify reducer invariants (immutability, idempotency where applicable). | None |
| R023 | P3 | S | Low | **Add benchmark tests for critical paths** - Baseline performance for command dispatch, event append, snapshot retrieval. | None |

### Documentation

| ID | Priority | Effort | Risk | Description | Dependencies |
|----|----------|--------|------|-------------|--------------|
| R030 | P1 | L | Low | **Expand Docusaurus documentation** - Current docs only cover Reservoir. Add sections for Brooks, Aggregates, Snapshots, Projections, Inlet, Aqueduct. | None |
| R031 | P2 | M | Low | **Create getting started guide** - Step-by-step guide for new consumers to set up event sourcing with Mississippi. | None |
| R032 | P2 | M | Low | **Document Orleans grain lifecycle** - Explain activation, deactivation, and state management for Mississippi grains. | None |
| R033 | P3 | S | Low | **Add XML doc coverage report** - Track and enforce XML documentation coverage for public APIs. | None |

### Architecture

| ID | Priority | Effort | Risk | Description | Dependencies |
|----|----------|--------|------|-------------|--------------|
| R040 | P2 | XL | High | **Implement event versioning/upcasting** - Support evolving event schemas over time without breaking existing streams. | None |
| R041 | P2 | L | Medium | **Add outbox pattern for integration events** - Ensure reliable event publication to external systems. | None |
| R042 | P3 | XL | High | **Add process manager/saga support** - Coordinate multi-aggregate workflows with compensation. | R040 |
| R043 | P3 | L | Medium | **Add dead letter queue for failed events** - Handle events that consistently fail processing. | None |

### Observability

| ID | Priority | Effort | Risk | Description | Dependencies |
|----|----------|--------|------|-------------|--------------|
| R050 | P2 | M | Low | **Add health check endpoints** - Implement IHealthCheck for Cosmos, Blob, and Orleans cluster connectivity. | None |
| R051 | P2 | S | Low | **Add distributed tracing correlation** - Ensure correlation IDs flow through command → event → projection chain. | None |
| R052 | P3 | M | Low | **Create Grafana/Prometheus dashboard templates** - Provide ready-to-use dashboards for framework metrics. | None |

### Resilience

| ID | Priority | Effort | Risk | Description | Dependencies |
|----|----------|--------|------|-------------|--------------|
| R060 | P2 | M | Medium | **Add circuit breaker for Cosmos calls** - Prevent cascade failures when Cosmos is unavailable. | None |
| R061 | P2 | S | Low | **Add jitter to retry policies** - Prevent thundering herd when retrying Cosmos operations. | None |
| R062 | P3 | M | Medium | **Implement graceful degradation** - Allow read-only mode when write path is unavailable. | R060 |

### Developer Experience

| ID | Priority | Effort | Risk | Description | Dependencies |
|----|----------|--------|------|-------------|--------------|
| R070 | P2 | M | Low | **Add dotnet new templates** - Provide project templates for aggregates, projections, and complete apps. | None |
| R071 | P3 | S | Low | **Add Roslyn analyzers for common mistakes** - Warn about missing attributes, incorrect reducer patterns. | None |
| R072 | P3 | M | Low | **Create VS Code extension** - Snippets and code actions for common patterns. | None |

### Storage Providers

| ID | Priority | Effort | Risk | Description | Dependencies |
|----|----------|--------|------|-------------|--------------|
| R080 | P3 | XL | High | **Add PostgreSQL event store** - Alternative to Cosmos for on-premises scenarios. | None |
| R081 | P3 | XL | High | **Add SQL Server event store** - Alternative for Microsoft-stack environments. | None |
| R082 | P3 | L | Medium | **Add in-memory event store** - For testing and development scenarios. | None |

### Samples

| ID | Priority | Effort | Risk | Description | Dependencies |
|----|----------|--------|------|-------------|--------------|
| R090 | P2 | L | Low | **Expand Cascade sample** - Add more features: user profiles, message search, notifications. | None |
| R091 | P3 | M | Low | **Add e-commerce sample** - Demonstrate order management, inventory, payment integration. | None |
| R092 | P3 | S | Low | **Add minimal API sample** - Show framework usage without Blazor, pure API scenario. | None |

---

## Implementation Roadmap

### Phase 1: Foundations (Weeks 1-2)

1. R001 - Review NoWarn suppressions
2. R011 - Improve effect error handling
3. R030 - Expand documentation
4. R050 - Add health checks

### Phase 2: Quality (Weeks 3-4)

1. R010 - Eliminate Store reflection
2. R020 - Add L1 tests
3. R021 - Add contract tests
4. R051 - Distributed tracing

### Phase 3: Resilience (Weeks 5-6)

1. R060 - Circuit breaker
2. R061 - Retry jitter
3. R040 - Event versioning design

### Phase 4: Features (Weeks 7-8)

1. R070 - dotnet new templates
2. R090 - Expand Cascade sample
3. R041 - Outbox pattern

---

## Quick Wins (< 1 day effort)

1. **R002** - Align package versions
2. **R003** - Document NoWarn justifications
3. **R012** - Review ConfigureAwait usage
4. **R023** - Add basic benchmark tests
5. **R033** - Add XML doc coverage

---

## High-Impact Items

1. **R010** - Store reflection elimination (performance)
2. **R030** - Documentation expansion (adoption)
3. **R040** - Event versioning (production-readiness)
4. **R050** - Health checks (operability)
5. **R070** - Project templates (developer experience)
