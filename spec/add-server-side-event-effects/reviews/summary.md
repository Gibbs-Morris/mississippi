# Design Review Summary

**RFC:** Server-Side Event Effects  
**Date:** 2026-01-24  
**Reviewers:** Developer, Architect, Dev Manager, DevOps Engineer

---

## Consolidated Verdict

| Reviewer | Verdict | Key Condition |
|----------|---------|---------------|
| Developer | ✅ Approve | Testing examples + debugging story |
| Architect | ⚠️ Conditional | Replace IEffectRegistry with keyed services |
| Dev Manager | ✅ Approve | Documentation must ship with feature |
| DevOps | ⚠️ Conditional | Observability + idempotency guidance required |

**Overall:** **Proceed with implementation** after addressing high-priority items.

---

## Critical Issues (P0 - Must Fix Before Merge)

| Issue | Raised By | Recommendation |
|-------|-----------|----------------|
| No observability | DevOps | Add metrics, structured logging, trace propagation |
| At-most-once not documented | DevOps | Document that effects are not guaranteed |
| No testing examples | Developer | Add unit test patterns to docs |
| IEffectRegistry uses reflection | Architect | Replace with .NET 8 keyed services |

---

## High Priority (P1 - Should Address)

| Issue | Raised By | Recommendation |
|-------|-----------|----------------|
| Idempotency not addressed | DevOps | Add idempotency guidance + optional base class |
| Graceful shutdown handling | DevOps | Add `OnDeactivateAsync` cancellation |
| IAggregateCommandGateway scope creep | Architect | Consider deferring to saga PR |
| aggregateKey naming confusing | Developer | Add `EffectContext` or clarify docs |
| Anti-pattern documentation | Dev Manager | Document what NOT to do |

---

## Nice to Have (P2 - Consider for V1 or V2)

| Issue | Raised By | Recommendation |
|-------|-----------|----------------|
| Development mode (await effects) | Developer | `AwaitInDevelopment` config option |
| Analyzer for missing effects | Developer | Warning when event has no effects |
| Effect timeout | Developer, DevOps | Add timeout configuration |
| Effect dead letter queue | DevOps | Visibility into dropped effects |
| ADR for design decisions | Architect | Document why StatelessWorker over alternatives |
| Item template (`dotnet new effect`) | Dev Manager | Reduce boilerplate |

---

## Consolidated Recommendations by Category

### Code Changes

1. **Replace `IEffectRegistry` with keyed DI services** (Architect)
   - Remove runtime reflection
   - Use `AddKeyedTransient<IEventEffect<TAggregate>, TEffect>(aggregateTypeName)`
   - Simpler, AOT-compatible, platform-native

2. **Add observability infrastructure** (DevOps)
   - `effect.dispatched.total`, `effect.execution.duration`, `effect.execution.errors` metrics
   - Structured logging with correlation IDs
   - Activity/trace propagation

3. **Add graceful shutdown** (DevOps)
   - Cancel in-flight effects on `OnDeactivateAsync`
   - Allow configurable grace period

4. **Consider `EffectContext`** (Developer)
   ```csharp
   public sealed record EffectContext(
       string AggregateKey,
       string BrookName,
       long EventPosition,
       DateTimeOffset Timestamp,
       string CorrelationId
   );
   ```

### Documentation Requirements

| Document | Priority | Owner |
|----------|----------|-------|
| Effects Guide (how to create, folder structure) | P0 | Framework team |
| Testing Effects (unit test patterns) | P0 | Framework team |
| Anti-Patterns (what NOT to do) | P0 | Framework team |
| Idempotency Patterns | P1 | Framework team |
| Debugging Guide (logs, tracing) | P1 | Framework team |
| Saga Patterns (if gateway included) | P1 | Framework team |

### Scope Decision: IAggregateCommandGateway

**Options:**
1. **Include in V1** - If saga patterns are near-term need
2. **Defer to saga PR** - YAGNI, reduces scope

**Recommendation:** Get product input. If no concrete saga use case in next 2-3 sprints, defer.

---

## Risk Matrix

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Effects lost during pod termination | Medium | Medium | Document at-most-once, graceful shutdown |
| Duplicate effect execution | Medium | High | Idempotency guidance/base class |
| Debugging fire-and-forget | High | Low | Observability investment |
| IEffectRegistry fails in AOT | Low | High | Replace with keyed services |
| Gateway misused for non-saga | Medium | Medium | Defer gateway or add warnings |

---

## Suggested Implementation Order

### Phase 1: Core (Week 1)
1. `IEventEffect<TEvent, TAggregate>` interface
2. `EventEffectBase<TEvent, TAggregate>` base class
3. `EffectDispatcherGrain` with `[StatelessWorker]` + `[OneWay]`
4. Keyed service registration (not IEffectRegistry)
5. GenericAggregateGrain integration
6. Observability (metrics, logging)

### Phase 2: Generator + Sample (Week 1-2)
7. AggregateSiloRegistrationGenerator updates
8. Sample effect in Spring.Domain
9. Unit tests

### Phase 3: Documentation (Week 2)
10. Effects Guide
11. Testing Guide
12. Anti-Patterns
13. Debugging Guide

### Phase 4: Polish (Week 2-3)
14. Graceful shutdown
15. EffectContext (if adopted)
16. Idempotent base class (optional)
17. Integration tests

### Future (Saga PR)
- IAggregateCommandGateway
- Saga patterns documentation
- Saga sample

---

## Approval Checklist

Before merging to main:

- [ ] All P0 items addressed
- [ ] Observability implemented and tested
- [ ] Documentation written and reviewed
- [ ] Unit tests with >80% coverage
- [ ] Integration test for effect dispatch
- [ ] Load test for high-volume effects
- [ ] Pod termination tested (graceful shutdown)
- [ ] Code review by architect + devops

---

## Next Steps

1. **Discuss:** Scope decision on `IAggregateCommandGateway`
2. **Update:** Implementation plan based on review feedback
3. **Assign:** Documentation ownership
4. **Begin:** Phase 1 implementation
