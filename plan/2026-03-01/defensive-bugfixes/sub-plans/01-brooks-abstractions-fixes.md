# Sub-Plan 01: Brooks Abstractions Fixes

## Context

- Master plan: `/plan/2026-03-01/defensive-bugfixes/PLAN.md`
- This is sub-plan 01 of 04

## Dependencies

- Depends on: none
- PR 1 (plan commit) must be merged before execution

## Objective

Fix null-safety issues in Brooks.Abstractions key structs, add null guard to `BrookAsyncReaderKey.Parse`, and add XML documentation warnings to `BrookPosition` about default ambiguity.

## Scope

### Files modified

- `src/Brooks.Abstractions/BrookKey.cs` — add `field ?? string.Empty` to `BrookName` and `EntityId` properties
- `src/Brooks.Abstractions/BrookRangeKey.cs` — add `field ?? string.Empty` to `BrookName` and `EntityId` properties
- `src/Brooks.Abstractions/BrookAsyncReaderKey.cs` — add `ArgumentNullException.ThrowIfNull(key)` to `Parse` method
- `src/Brooks.Abstractions/BrookPosition.cs` — add XML doc warnings about `default` vs `new BrookPosition()` behavior

### Test files modified/created

- `tests/Brooks.Abstractions.L0Tests/BrookKeyTests.cs` — add tests for `default(BrookKey)` property access
- `tests/Brooks.Abstractions.L0Tests/BrookRangeKeyTests.cs` — add tests for `default(BrookRangeKey)` property access
- `tests/Brooks.Abstractions.L0Tests/BrookAsyncReaderKeyTests.cs` — add test for `Parse(null)` throwing `ArgumentNullException`
- `tests/Brooks.Abstractions.L0Tests/BrookPositionTests.cs` — add test documenting `default(BrookPosition).Value == 0`

## Deployability

- Feature gate: None needed — these are defensive hardening changes with no user-visible behavior change
- Safe to deploy: All changes make existing APIs more robust; no behavior change for correctly-constructed instances

## Implementation Breakdown

1. **BrookKey null-safety** — Change `BrookName` and `EntityId` from `string { get; }` to `string { get => field ?? string.Empty; }`. Add unit tests asserting `default(BrookKey).BrookName == ""` and `default(BrookKey).EntityId == ""` and `default(BrookKey).ToString()` returns non-null.

2. **BrookRangeKey null-safety** — Change `BrookName` and `EntityId` from `string { get; }` to `string { get => field ?? string.Empty; }`. Add unit tests asserting `default(BrookRangeKey).BrookName == ""` and `default(BrookRangeKey).EntityId == ""`.

3. **BrookAsyncReaderKey.Parse null guard** — Add `ArgumentNullException.ThrowIfNull(key)` as the first line of `Parse(string key)`. Add unit test asserting `Parse(null)` throws `ArgumentNullException`. Note: `BrookAsyncReaderKey` has no direct string properties to fix (it composes `BrookKey` which is fixed in step 1).

4. **BrookPosition XML docs** — Add `<remarks>` documentation to the type, `Value` property, and parameterless constructor warning that `default(BrookPosition)` produces `Value = 0` (which is a valid position) whereas `new BrookPosition()` produces `Value = -1` (the "not set" sentinel). Add a test that documents this behavior: `Assert.Equal(0, default(BrookPosition).Value)` and `Assert.Equal(-1, new BrookPosition().Value)`.

## Testing Strategy

- All tests are L0 (pure unit tests, no I/O)
- Target: 100% coverage on changed code paths
- Pattern: test default-constructed struct, test constructed-via-constructor struct, test ToString/implicit operator on both
- Must pass mutation testing

## Acceptance Criteria

- [ ] Builds with zero warnings
- [ ] All tests pass
- [ ] Deployable on its own (no feature gate needed)
- [ ] `default(BrookKey).BrookName` returns `string.Empty` (not null)
- [ ] `default(BrookKey).EntityId` returns `string.Empty` (not null)
- [ ] `default(BrookRangeKey).BrookName` returns `string.Empty` (not null)
- [ ] `default(BrookRangeKey).EntityId` returns `string.Empty` (not null)
- [ ] `BrookAsyncReaderKey.Parse(null!)` throws `ArgumentNullException`
- [ ] `BrookPosition` type and `Value` property have XML doc warnings about default behavior
- [ ] Mutation tests pass

## PR Metadata

- Branch: `epic/defensive-bugfixes/01-brooks-abstractions-fixes`
- Title: `Fix BrookKey null defaults, Parse null guard, BrookPosition docs +semver: fix`
- Base: `main`

## Decomposition Guardrails Applied

- All changes are in Brooks.Abstractions — no cross-project dependencies
- No storage name changes (immutable storage names unaffected)
- No partial contracts — all changes are complete within this sub-plan
