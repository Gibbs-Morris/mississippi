# Implementation Plan

## Overview

Add targeted L0 tests to increase SonarCloud new code coverage from 74.3% to ≥80%.

## Steps

### 1. Add UseMemoryStreams() parameterless overload test
**File**: `tests/Aqueduct.Grains.L0Tests/AqueductSiloOptionsTests.cs`
**Lines to cover**: ~3-5
```csharp
// Test that UseMemoryStreams() calls UseMemoryStreams(defaultProvider, "PubSubStore")
```

### 2. Add SendToAllAsync happy path test
**File**: `tests/Aqueduct.L0Tests/AqueductNotifierTests.cs`
**Lines to cover**: ~8-10
```csharp
// Mock IStreamProvider, IAsyncStream<AllMessage>
// Verify OnNextAsync called with correct AllMessage
```

### 3. Add UxProjectionMetrics tests (if not exists)
**File**: `tests/EventSourcing.UxProjections.L0Tests/Diagnostics/UxProjectionMetricsTests.cs`
**Lines to cover**: ~8
```csharp
// Test RecordNotificationSent
// Test RecordQuery with hasResult true/false
// Test RecordSubscription
// Test RecordVersionCacheHit
```

### 4. Verify build and tests pass
```powershell
pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1
pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1
```

### 5. Commit and push
```powershell
git add tests/
git commit -m "test: add coverage for AqueductSiloOptions, AqueductNotifier, UxProjectionMetrics"
git push
```

## Expected Coverage Impact

| File | Before | After | Delta |
|------|--------|-------|-------|
| AqueductSiloOptions.cs | 64.3% | ~95% | +30% |
| AqueductNotifier.cs | 73.5% | ~85% | +11% |
| UxProjectionMetrics.cs | 76.5% | ~95% | +18% |

Estimated new lines covered: ~25-35 lines

## Rollback Plan

If tests fail or cause issues:
1. `git reset --hard HEAD~1`
2. `git push --force-with-lease`
