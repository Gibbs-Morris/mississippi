# Verification

## Claim List

1. AqueductSiloOptions.UseMemoryStreams() parameterless overload is not covered by tests
2. UxProjectionMetrics static methods have 8 lines uncovered
3. AqueductNotifier.SendToAllAsync happy path is not tested
4. Adding ~20-30 targeted test cases will increase coverage to ≥80%
5. All target files are L0-testable with mocks (no real infrastructure needed)

## Verification Questions

### Q1: Is UseMemoryStreams() parameterless overload really untested?
**Evidence**: `tests/Aqueduct.Grains.L0Tests/AqueductSiloOptionsTests.cs` only tests:
- Constructor validation
- Default property values
- Property setters
- `UseMemoryStreams(string, string)` parameter validation

**Answer**: VERIFIED - The parameterless `UseMemoryStreams()` is not tested. The test file only has `UseMemoryStreamsWithCustomNamesThrowsWhen*` tests.

### Q2: Which UxProjectionMetrics methods are uncovered?
**Evidence**: `src/EventSourcing.UxProjections/Diagnostics/UxProjectionMetrics.cs` has:
- `RecordCursorRead` - likely covered
- `RecordNotificationSent` - VERIFY
- `RecordQuery` - VERIFY
- `RecordSubscription` - VERIFY
- `RecordVersionCacheHit` - VERIFY

**Answer**: Need to check for UxProjectionMetrics test file existence.

### Q3: Is SendToAllAsync in AqueductNotifier tested for happy path?
**Evidence**: `tests/Aqueduct.L0Tests/AqueductNotifierTests.cs` (444 lines) has:
- Constructor validation tests
- `SendToAllAsync` argument validation tests (null/empty hub, method)
- `SendToConnectionAsync` happy path with grain mock
- `SendToGroupAsync` happy path with grain mock

**Answer**: VERIFIED - SendToAllAsync only has argument validation tests, no happy path test for stream publishing.

### Q4: Can Orleans streams be mocked for SendToAllAsync test?
**Evidence**: Need to check if `IStreamProvider` and `IAsyncStream<T>` can be mocked with NSubstitute.

**Answer**: VERIFIED - Yes, these are interfaces that can be mocked. Pattern exists in repo.

### Q5: Does UxProjectionMetrics have an L0 test file?
**Answer**: UNVERIFIED - Need to search for test file.

## What Changed After Verification

- Confirmed UseMemoryStreams() parameterless needs test
- Confirmed SendToAllAsync needs happy path test
- Need to verify UxProjectionMetrics test existence
