# 03 — Decisions

## D1: Key Struct Null-Safety Mechanism

**Decision**: Use C# 14 `field` keyword with `?? string.Empty` on all string properties across all 13 key structs.

**Chosen option**: `public string BrookName { get => field ?? string.Empty; }`

**Rationale**: The `field` keyword gives direct backing field access without breaking `with` expressions or Orleans `[GenerateSerializer]`. String interpolation in implicit operators already handles null gracefully, but direct property access returns null. This approach makes properties never-null regardless of how the struct is constructed.

**Evidence**: `Directory.Build.props` L8 confirms `LangVersion=14.0`. User's bug report explicitly describes this approach.

**Risks**: Minimal — `field` keyword is stable in C# 14. Properties that previously returned null on default will now return `string.Empty`.

**Confidence**: High.

---

## D2: Registry Duplicate Behavior

**Decision**: Throw `InvalidOperationException` when registering a name that already maps to a **different** type. Allow idempotent re-registration of the same name→type pair (silent skip).

**Chosen option**: ContainsKey check: if name exists and maps to same type, skip; if maps to different type, throw.

**Rationale**: Mirrors `IServiceCollection.TryAdd` composability DX (safe to call multiple times) while detecting genuine configuration errors (two different types sharing a storage name). Pure TryAdd would silently hide conflicts.

**Evidence**: User wants "IServiceCollection TryAdd DX". Research confirms TryAdd = first registration wins, silent skip on same service type. Extended for type registries: same name+type = idempotent; same name+different type = error.

**Risks**: Breaking change if anyone currently registers duplicate names intentionally. Pre-1.0, so acceptable.

**Confidence**: High.

---

## D3: OperationResult Default Semantics

**Decision**: Make `default(OperationResult)` represent success using an IsDefault sentinel pattern.

**Chosen option**: Change `Success` property to check `!(Success == false && ErrorCode != null)` or equivalent sentinel.

**Rationale**: User wants standard .NET DX. `ValueTask<T>` default = success; `OperationStatus` default (0) = `Done` = success. For a "did the operation work?" type, having `default` = success is the pit-of-success design. Uninitialized results won't falsely trigger error-handling paths.

**Implementation approach**: Change the internal representation. Instead of storing `bool success`, infer success state:
- If ErrorCode is null and ErrorMessage is null → success (covers both `default` and `Ok()`)
- If ErrorCode/ErrorMessage are set → failure

This removes the need for a separate `success` bool and makes `default` = success naturally.

**Evidence**: `ValueTask<T>` default = completed successfully. User's bug report says "Fixed with IsDefault sentinel check."

**Risks**: Serialization ID layout change. Pre-1.0 so acceptable. `[Id(0)]` was on `Success` bool — can keep the same ID or remap.

**Confidence**: High.

---

## D4: BrookPosition Documentation

**Decision**: Add XML doc warnings only. No code changes.

**Chosen option**: XML docs on `Value` property and type-level docs warning that `default(BrookPosition)` produces `Value=0` which is a valid position.

**Rationale**: Fixing requires either changing the default value semantics (breaking serialization) or introducing a sentinel (breaking event sourcing position math). Documentation is the safe choice.

**Evidence**: `BrookPosition()` constructor sets `Value = -1`, but `default(BrookPosition)` zeroes memory to `Value = 0`. The `NotSet` property checks `Value == -1`. Changing this is not feasible without breaking changes to persisted data.

**Confidence**: High.

---

## D5: Scope of All 13 Key Structs

**Decision**: Fix all 13 key structs for consistency, not just the 3 most dangerous ones.

**Chosen option**: Apply `field ?? string.Empty` to every string property across:
- Brooks.Abstractions: BrookKey (2 props), BrookRangeKey (2 props), BrookAsyncReaderKey (0 new — nested BrookKey handles it)
- DomainModeling.Abstractions: AggregateKey (1), UxProjectionKey (1), UxProjectionNotificationKey (1), UxProjectionVersionedKey (0 — nested), UxProjectionVersionedCacheKey (2), UxProjectionCursorKey (2)
- Tributary.Abstractions: SnapshotStreamKey (4), SnapshotKey (0 — nested)
- Aqueduct.Abstractions: SignalRGroupKey (2), SignalRClientKey (2), SignalRServerDirectoryKey (1)

**Rationale**: User confirmed "All 13 key structs" for consistency. Even structs that produce misleading strings (`"|"`) rather than null should have non-null properties.

**Confidence**: High.
