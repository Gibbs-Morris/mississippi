# Review Synthesis (Reviews 01-12)

## Must Fix (Blockers)

### M1: OperationResult<T> MemberNotNullWhen Violation
**Issue**: Making `default(OperationResult<T>).Success = true` violates `[MemberNotNullWhen(true, nameof(Value))]` — Value would be null but the NRT annotation says it's non-null when Success is true.
**Resolution**: Only fix non-generic `OperationResult`. Leave `OperationResult<T>` with its current stored `Success` bool. Document that `default(OperationResult<T>)` is not a valid result. The user's bug report only mentions `OperationResult`, not `OperationResult<T>`.
**Confidence**: High — NRT contract violation would cause production NREs.

### M2: Registry Concurrency Regression
**Issue**: The proposed `TryGetValue → indexer write` is not atomic on `ConcurrentDictionary`. Concurrent `ScanAssembly` calls can race.
**Resolution**: Use `GetOrAdd(eventName, eventType)` which is atomic. Compare returned type with desired type; throw if different. This is safe, lock-free, and correct.
```csharp
Type registeredType = nameToType.GetOrAdd(eventName, eventType);
if (registeredType != eventType)
{
    throw new InvalidOperationException($"...");
}
typeToName.TryAdd(eventType, eventName);
```
**Confidence**: High — ConcurrentDictionary.GetOrAdd is well-documented as atomic.

## Should Fix

### S1: Log Swallowed Exceptions in Store Listener Isolation
**Issue**: Silently swallowing listener exceptions in `Store.NotifyListeners()` hides bugs.
**Resolution**: Add a logger parameter or use `System.Diagnostics.Debug.WriteLine` as a minimal signal. However, `Store` is a consumer-facing class and doesn't currently have ILogger. Adding ILogger would be scope creep. Use `System.Diagnostics.Trace.TraceWarning` as a minimal non-breaking approach, or accept the swallow since tests will catch listener bugs.
**Decision**: Accept swallow — Store is not instrumented with ILogger and adding it is scope creep. The try-catch already matches the existing pattern in `HandleEffectsAsync` (Store.cs L430-437) which also swallows.

### S2: Add Serialization Round-Trip Tests for OperationResult
**Issue**: Changing OperationResult serialization layout needs round-trip tests to ensure Orleans can serialize/deserialize correctly.
**Resolution**: Add Orleans serialization round-trip tests in the test project.
**Decision**: Accept — add to SP-02 testing strategy.

### S3: Verify Test Project Existence for Tributary.Abstractions and Aqueduct.Abstractions
**Issue**: Sub-plan 03 assumes test projects exist. Need to verify.
**Resolution**: Builder should check and create if needed.
**Decision**: Accept — already noted in SP-03 as "create if needed."

### S4: Interface XML Doc Updates for Registry
**Issue**: `IEventTypeRegistry.Register` and `ISnapshotTypeRegistry.Register` don't document the duplicate-name behavior.
**Resolution**: Update XML docs on the interface methods.
**Decision**: Accept — add to SP-02.

### S5: PR Title Semver Suffix
**Issue**: Bug fixes should use `+semver: fix`, which is already specified. Confirmed correct.
**Decision**: No change needed.

### S6: BrookAsyncReaderKey.FromString Also Needs Null Guard
**Issue**: `BrookAsyncReaderKey.FromString(string key)` delegates to `Parse(key)` — so adding the guard to `Parse` covers both. But `FromString` should be checked too.
**Resolution**: Since `FromString` calls `Parse`, the guard in `Parse` covers it. No additional work needed.
**Decision**: No change needed.

## Could Do

### C1: Add `[MemberNotNull]` on key struct string properties to signal non-null guarantee
**Decision**: Won't — the `field ?? string.Empty` approach already makes properties non-null. Adding more annotations is unnecessary complexity.

### C2: Consider a Roslyn analyzer for `default(BrookPosition)` usage
**Decision**: Won't — deferred per user decision to do XML docs only.

### C3: Consider adding `ArgumentOutOfRangeException.ThrowIfNegative(maxRetries)` validation to IRetryPolicy interface contract
**Decision**: Won't — scope creep. The fix is in the concrete class.

### C4: Consider logging for registry skip events
**Decision**: Won't — registry is used at startup only and TryAdd-like silence is the chosen DX.

### C5: Consider thread-safety of StoreEventSubject.OnError in the catch block
**Decision**: No issue — observer removal happens via Unsubscribe which takes the lock. The OnError call is a notification, not a mutation.

## Won't Fix

- No changes to BrookPosition semantics (user-confirmed documentation only)
- No logging infrastructure added to Store (follows existing swallow pattern in HandleEffectsAsync)
- No Roslyn analyzer for default struct usage
- No changes to OperationResult<T> (only non-generic OperationResult is fixed)
