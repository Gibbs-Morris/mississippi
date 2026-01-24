# Add Server-Side Event Effects

**Status:** Draft  
**Task Size:** Large  
**Approval Checkpoint:** Yes (new public API/contract, cross-component change)

## Summary

Add support for server-side effects in aggregate grains, mirroring the existing client-side Redux effect pattern. Effects allow executing asynchronous side operations (API calls, messaging, notifications) after events are persisted.

## Key Links

- [learned.md](./learned.md) - Verified repository facts
- [rfc.md](./rfc.md) - Design document
- [verification.md](./verification.md) - Claim verification
- [implementation-plan.md](./implementation-plan.md) - Detailed implementation steps
- [progress.md](./progress.md) - Work log

## Open Questions

1. **Command Effects vs Event Effects:** Should effects trigger on commands or events?
2. **Naming Conventions:** Should client-side effects be renamed to "ActionEffect" for consistency?
