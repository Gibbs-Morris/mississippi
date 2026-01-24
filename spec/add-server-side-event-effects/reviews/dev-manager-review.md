# Design Review: Dev Manager Perspective

**Reviewer Role:** Development Manager (focused on maintainability, team productivity, long-term costs)  
**Date:** 2026-01-24  
**RFC:** Server-Side Event Effects

---

## Overall Assessment: ✅ Approve with Documentation Requirements

The design is maintainable and follows established patterns. My main concerns are around documentation, onboarding, and long-term support costs.

---

## Maintainability Analysis

### Code Organization: ✅ Excellent

The folder convention is intuitive and consistent:
```
Aggregates/
  BankAccount/
    Commands/     ← Existing
    Events/       ← Existing
    Handlers/     ← Existing
    Reducers/     ← Existing
    Effects/      ← New (same pattern)
```

**Benefit:** New team members can predict where code lives. The pattern is self-documenting.

### Pattern Consistency: ✅ Excellent

| Concept | Handler | Reducer | Effect |
|---------|---------|---------|--------|
| Base class | `CommandHandlerBase<T,A>` | `EventReducerBase<T,A>` | `EventEffectBase<T,A>` |
| Discovery | `{Agg}/Handlers/` namespace | `{Agg}/Reducers/` namespace | `{Agg}/Effects/` namespace |
| Registration | Generated | Generated | Generated |
| DI | Injected via constructor | Injected via constructor | Injected via constructor |

**Benefit:** Once a developer learns one pattern, they know all three. Training cost is minimal.

### Cognitive Load: ✅ Good

Developers only need to:
1. Create a class in `Effects/` folder
2. Extend `EventEffectBase<TEvent, TAggregate>`
3. Implement `HandleAsync`

The fire-and-forget complexity is hidden. They don't need to understand Orleans grains or `[OneWay]` attributes.

---

## Documentation Requirements

### Must-Have Documentation 📋

| Document | Purpose | Priority |
|----------|---------|----------|
| **Effects Guide** | How to create effects, folder structure, naming | P0 |
| **Testing Effects** | Unit test patterns, mock gateway | P0 |
| **Effect vs Handler vs Reducer** | When to use each | P0 |
| **Debugging Effects** | Finding logs, correlation IDs | P1 |
| **Saga Patterns** | If gateway is included, how to use it safely | P1 |
| **Migration Guide** | For teams with existing workarounds | P2 |

### Anti-Pattern Documentation

Effects create new failure modes. Document explicitly:

```markdown
## Anti-Patterns

### ❌ Effect That Modifies State
Effects cannot change aggregate state. If you need state changes, 
emit an event from your handler instead.

### ❌ Effect That Must Succeed
Effects are fire-and-forget. If you need guaranteed delivery,
use the Outbox Pattern instead.

### ❌ Effect That Calls Back to Same Aggregate
This creates circular dependencies and potential infinite loops.
Use saga patterns with careful state tracking.

### ❌ Awaiting External Services Without Timeout
Always add timeout to external calls:
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await emailService.SendAsync(..., cts.Token);
```
```

---

## Team Onboarding Considerations

### Learning Curve: Low ✅

| Audience | Time to Productive | Notes |
|----------|-------------------|-------|
| Developer familiar with codebase | 30 min | Just needs to see one example |
| New hire | 2 hours | Part of aggregate training |
| Contractor | 1 hour | Self-service with good docs |

### Common Questions to Pre-Answer

1. **"Why doesn't my effect run?"** → Check namespace convention, check event type match
2. **"How do I test effects?"** → Mock the services, call HandleAsync directly
3. **"How do I see if my effect ran?"** → Check logs for effect correlation ID
4. **"My effect failed, what now?"** → Effects are fire-and-forget; add retry logic in effect
5. **"Can I await the effect result?"** → No, by design; use saga pattern if needed

---

## Long-Term Support Cost Analysis

### Code Ownership

| Component | Owner | Change Frequency |
|-----------|-------|------------------|
| `IEventEffect` interface | Framework team | Rare (breaking change) |
| `EventEffectBase` | Framework team | Rare |
| `EffectDispatcherGrain` | Framework team | Occasional (observability) |
| User-defined effects | Domain teams | Frequent |

**Key Insight:** The framework surface is small. Most effects are user code that doesn't require framework changes.

### Breaking Change Risk: Low

The public API surface is minimal:
- `IEventEffect<TEvent, TAggregate>` - Interface
- `EventEffectBase<TEvent, TAggregate>` - Base class
- `HandleAsync(TEvent, string, CancellationToken)` - Single method

Changes to internal implementation (dispatcher, registry) don't break user code.

### Technical Debt Risk: Medium

**Risk Areas:**
1. `EffectRegistry` reflection could become problematic in AOT scenarios
2. `IAggregateCommandGateway` could be misused without saga patterns
3. No built-in retry/resilience could lead to copy-paste patterns

**Mitigation:**
- Plan migration to keyed services before .NET 9/10
- If including gateway, add prominent warnings
- Provide official resilience pattern in docs

---

## Team Productivity Impact

### Positive Impacts

| Impact | Estimate |
|--------|----------|
| Eliminate manual event bus wiring | -2 hours/aggregate |
| Standardized pattern (less decision fatigue) | -30 min/feature |
| Generator automation | -15 min/effect |
| Reduced integration testing complexity | -1 hour/feature |

### Neutral Impacts

| Impact | Notes |
|--------|-------|
| Learning curve | One-time cost, minimal |
| Debugging | Same as existing fire-and-forget patterns |

### Negative Impacts

| Impact | Mitigation |
|--------|------------|
| Effects hide complexity | Training, documentation |
| Fire-and-forget debugging | Observability, logging |
| Potential misuse (sagas without state) | Documentation, code review |

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Team misuses effects for guaranteed delivery | Medium | Medium | Documentation, code review checklist |
| Effects become dumping ground for side effects | Medium | Low | Architecture review, effect categories |
| Debugging production effect failures | High | Low | Observability investment (logs, traces) |
| Breaking change in future | Low | High | Stable interface design, deprecation policy |

---

## Code Review Checklist for Effects

Add to team code review guidelines:

```markdown
## Effect Code Review Checklist

- [ ] Effect is in correct namespace (`{Aggregate}/Effects/`)
- [ ] Effect handles only one event type
- [ ] Effect has appropriate timeout for external calls
- [ ] Effect logs at appropriate level (Info for success, Warning for transient failure, Error for permanent)
- [ ] Effect does not modify aggregate state
- [ ] Effect does not call back to same aggregate (unless saga pattern)
- [ ] Effect has unit tests
- [ ] Effect exception handling follows pattern (catch, log, don't throw)
```

---

## Recommendations

### Before Implementation

1. **Write the documentation first** - Effects Guide, Testing Guide, Anti-Patterns
2. **Create item template** - `dotnet new effect --name AccountOpenedEffect --aggregate BankAccount`
3. **Add observability from day 1** - Metrics, correlation IDs, structured logging

### During Implementation

1. **Involve 2-3 domain teams** - Get early feedback on the API
2. **Create 3+ sample effects** - Cover notification, integration, audit scenarios
3. **Write troubleshooting guide** - Based on anticipated issues

### After Implementation

1. **Office hours / training session** - 30-minute walkthrough for all devs
2. **Monitor adoption** - Track effects created per sprint
3. **Gather feedback at 30/60/90 days** - Iterate on pain points

---

## Verdict

**Approve** with the following requirements:

1. **P0:** Documentation (Effects Guide, Testing, Anti-Patterns) must ship with feature
2. **P0:** Observability (structured logging, metrics) must be in V1
3. **P1:** Item template for effect creation
4. **P1:** Code review checklist for team

The design is maintainable and follows existing patterns. Main investment is in documentation and training, not ongoing code maintenance.
