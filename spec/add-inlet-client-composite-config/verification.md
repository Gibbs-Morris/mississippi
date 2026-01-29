# Verification

## Claim list
1. Default `Add{App}Inlet()` remains and preserves behavior.
2. New overload allows optional SignalR configuration without changing defaults.
3. Configuration is applied after defaults to allow overrides.
4. Generator tests cover the overload and invocation.
5. No docs updates are required; doc plan will be recorded.

## Questions
1. Does the generator emit an overload with `Action<InletBlazorSignalRBuilder>?`?
2. Does `Add{App}Inlet()` forward to the overload with `null`?
3. Is `configureSignalR` invoked after `WithHubPath` and DTO registration?
4. Are both projections-present and projections-absent paths invoking `configureSignalR`?
5. Is there a new L0 test for the generator output?
6. Do L0 generator tests pass?
7. Are there docs references that require updates?
