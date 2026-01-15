# Refactor InletSignalREffect

**Status**: Draft  
**Size**: Medium  
**Approval Checkpoint**: No (internal refactoring, no API/contract changes)

## Links

- [learned.md](learned.md) - Verified repository facts
- [rfc.md](rfc.md) - Problem/design/alternatives
- [verification.md](verification.md) - Claims and evidence
- [implementation-plan.md](implementation-plan.md) - Step-by-step plan
- [progress.md](progress.md) - Execution log

## Summary

Refactor `InletSignalREffect` to improve testability and apply SOLID/KISS/DRY principles while maintaining backward compatibility.

## Goals

1. Make the class fully L0-testable by extracting `HubConnection` creation
2. Reduce code duplication (DRY)
3. Simplify the class by extracting responsibilities (SRP)
4. Maintain all existing behavior (no breaking changes)
5. Increase test coverage significantly
