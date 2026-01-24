# Progress Log

## 2026-01-24

### Initial Research and Planning

- **Explored codebase** to understand aggregate architecture
- **Identified key components:**
  - GenericAggregateGrain - command execution entry point
  - CommandHandlerBase / RootCommandHandler - command dispatch
  - EventReducerBase / RootReducer - event reduction
  - AggregateSiloRegistrationGenerator - DI registration codegen
  
- **Explored client-side Redux (Reservoir)**:
  - IEffect interface with CanHandle + HandleAsync
  - Store.TriggerEffectsAsync runs effects after reducers
  - CommandEffectBase for HTTP command posting

- **Created spec folder** with:
  - README.md - task overview
  - learned.md - verified facts
  - rfc.md - design document
  - verification.md - claims and answers
  - implementation-plan.md - detailed steps
  - progress.md - this log

### Key Design Decision: Event Effects (not Command Effects)

Rationale:
1. Events are persisted facts; effects should react to facts
2. Command effects would run before persistence (transactional risk)
3. Aligns with event sourcing philosophy
4. Mirrors client-side pattern (actions dispatch → reducers → effects)

### Open Questions Identified

1. **Should effects be fire-and-forget or awaited?**
   - Recommendation: Fire-and-forget by default (matches client Store pattern)
   
2. **Should client IEffect be renamed to IActionEffect?**
   - Recommendation: Defer to separate PR; document inconsistency for now

3. **Effect failure handling?**
   - Recommendation: Catch and log; don't fail the command

### Next Steps (when approved)

1. Implement IEventEffect and EventEffectBase in abstractions
2. Implement RootEventEffectDispatcher in aggregates
3. Add registration methods to AggregateRegistrations
4. Modify GenericAggregateGrain to call dispatcher
5. Update AggregateSiloRegistrationGenerator
6. Add sample effect to Spring.Domain
7. Write tests

### Status: Awaiting Approval

This is a large change with new public APIs. RFC and implementation plan are ready for review.
