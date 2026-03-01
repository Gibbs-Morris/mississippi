# 05 — Plan Review: Defensive Bugfixes

**Reviewed**: 2026-03-01
**Scope**: Sub-plans SP-01 through SP-04 (7 bugs, 4 parallel work streams)
**Method**: 12-persona combined review with deduplication and priority synthesis

---

## Persona 01: Marketing & Contracts

**Focus**: naming, discoverability, migration communication

| # | Issue | Why It Matters | Proposed Change | Confidence |
|---|-------|---------------|-----------------|------------|
| 01-1 | PR title `+semver: fix` is correct but the changes include a **semantic break** in `OperationResult` (`default` was failure, becomes success) and a **behavioral break** in Registry (throws where it didn't before). Calling all of this `fix` undersells migration impact. | Consumers who relied on `default(OperationResult).Success == false` or on silent duplicate registration will break. Even pre-1.0, users read changelogs. | SP-02 PR title should be `+semver: breaking` or at minimum the PR description should include a **Migration Notes** section with before/after code for OperationResult and Registry. The non-generic `OperationResult` semantic flip is the most disruptive. | High |
| 01-2 | The `field ?? string.Empty` change across 13 structs alters the public contract of every property from "could return null on default" to "never returns null". Callers doing `if (key.EntityId is null)` will have dead branches. | Nullable reference type annotations do not currently mark these as non-null (they're `string`, not `string?`, so NRT already assumes non-null). The behavioral change aligns with the existing annotation. | No change needed—the fix aligns behavior with the already-declared NRT contract. Document in PR description that `default` struct properties now return `string.Empty` instead of `null`. | High |
| 01-3 | Exception message strings in the Registry fix (SP-02) use `existingType.FullName` which can return `null` for dynamically emitted types. | Produces a confusing exception message like `...registered to type ''`. | Use `existingType.FullName ?? existingType.Name` for robustness in the exception message. | Low |

---

## Persona 02: Solution Engineering

**Focus**: adoption readiness, ecosystem compliance

| # | Issue | Why It Matters | Proposed Change | Confidence |
|---|-------|---------------|-----------------|------------|
| 02-1 | C# 14 `field` keyword is confirmed via `LangVersion=14.0` in `Directory.Build.props`, but downstream consumers who reference these packages with an older `LangVersion` may see warnings or errors if they try to decompile or use source-link. | `field` is a keyword in the IL only at the property level; downstream consumers consume the compiled assembly—no `field` keyword leaks. However, source-link debugging into these properties will show unfamiliar syntax for users on older C# versions. | No code change needed. Note in PR description that `field` keyword is compile-time only and does not affect binary compatibility. | Medium |
| 02-2 | SP-03 says "create test project if needed" for `Tributary.Abstractions.L0Tests`. The plan should confirm whether this project already exists to avoid underestimating scope. | Creating a new test project requires `slnx` changes, `Directory.Build.props` convention adherence, and CPM compliance. | Verify existence of `tests/Tributary.Abstractions.L0Tests/` before execution. If it doesn't exist, add explicit project-creation steps to SP-03 including `slnx` registration. | High |
| 02-3 | `CosmosRetryPolicy` is a `public sealed class`—adding `ArgumentOutOfRangeException.ThrowIfNegative` is a behavioral change for any consumer currently passing negative values (however unlikely). The plan correctly notes this is pre-1.0. | Acceptable pre-1.0, but if any sample application or test uses a negative value, CI will break. | Grep for `CosmosRetryPolicy` constructor calls in samples/tests to confirm no existing negative usage before implementation. | Medium |

---

## Persona 03: Principal Engineer

**Focus**: consistency, maintainability, SOLID, test strategy

| # | Issue | Why It Matters | Proposed Change | Confidence |
|---|-------|---------------|-----------------|------------|
| 03-1 | **`OperationResult<T>` default-as-success violates `[MemberNotNullWhen(true, nameof(Value))]` contract.** `default(OperationResult<T>)` where T is a reference type produces `Success = true` (ErrorCode is null) but `Value = null`. The `MemberNotNullWhen` attribute promises callers that `if (result.Success) { result.Value.DoSomething(); }` is safe—this would NRE. | This is a **correctness regression**. Callers relying on the NRT flow analysis will write code that crashes at runtime on `default` instances of `OperationResult<T>`. The non-generic `OperationResult` is fine (no Value property), but `OperationResult<T>` is dangerous. | Either: (a) Do NOT apply the default-as-success pattern to `OperationResult<T>`, only to `OperationResult`; or (b) Make `OperationResult<T>.Success` return `ErrorCode is null && Value is not null` (but this changes semantics for value-type T where `default(T)` is valid); or (c) Accept that `default(OperationResult<T>)` is a code smell that cannot be fully fixed and document it. Option (a) is safest—two types with different default semantics is confusing but less dangerous than an NRT contract violation. | **High** |
| 03-2 | SP-04 silently swallows exceptions in `Store.NotifyListeners()` with an empty catch block. The repo's LoggerExtensions policy says logging "MUST go through LoggerExtensions." Silent swallowing hides bugs in listener implementations. | A Blazor component with a rendering bug in its `StateHasChanged` path would silently fail with no diagnostics. Developers would spend hours debugging why their UI doesn't update. | At minimum, store the exceptions and aggregate them (like `AggregateException`). Better: `Store` does not have an `ILogger` today—either add one (constructor change) or emit errors via the existing `StoreEventSubject` (e.g., a new `ListenerErrorEvent`). The `StoreEventSubject.OnNext(new ListenerErrorEvent(...))` approach avoids adding a logger dependency to a client-side class. | High |
| 03-3 | The plan doesn't mention whether `OperationResult.Ok()` factory method should still exist after the change. If `default` equals success, callers might start using `default` instead of `Ok()`, leading to inconsistent code. | API surface ambiguity. Two ways to create success (`Ok()` vs `default`) means inconsistent usage patterns across the codebase. | Keep `Ok()` and document it as the preferred way. Add an analyzer or code review note discouraging raw `default(OperationResult)`. The change protects against *accidental* defaults, not as an alternative API. | Medium |
| 03-4 | The plan estimates "~300 lines" for SP-02 which bundles three logically distinct fixes: key null-safety (mechanical), OperationResult semantics (nuanced), and Registry duplicate detection (concurrency-sensitive). This violates the single-responsibility PR principle from the review guide ("~600 changed lines" cap, "single-responsibility, mixed concerns MUST be split"). | SP-02 is the riskiest sub-plan because it combines a serialization layout change, a behavioral change, and a validation change. Any regression is harder to bisect. | Consider splitting SP-02 into three sub-sub-plans: SP-02a (key null-safety), SP-02b (OperationResult), SP-02c (Registry). Each can be its own commit within the PR, or ideally separate PRs. | Medium |
| 03-5 | Test strategy section says "must pass mutation testing" for all sub-plans but doesn't specify which test projects cover which source projects. Some test projects may not exist yet (Tributary.Abstractions.L0Tests, Aqueduct.Abstractions.L0Tests). | Missing test project mapping increases risk of incomplete coverage or wasted time during execution. | Add explicit test→source project mapping to each sub-plan and confirm project existence. | Medium |

---

## Persona 04: Technical Architect

**Focus**: architecture, boundaries, dependency direction

| # | Issue | Why It Matters | Proposed Change | Confidence |
|---|-------|---------------|-----------------|------------|
| 04-1 | The `field` keyword approach keeps the fix inside each struct without adding new fields or types—architecturally clean. No dependency direction changes. | Validates the approach. | No change needed. | High |
| 04-2 | SP-04's emit-via-`StoreEventSubject` fallback for listener errors (if adopted) would make `StoreEventSubject` itself a potential failure point during error handling. If `OnNext` throws during error emission, we have infinite recursion potential. | The error-handling path must not use the same path that could fail. | If exposing listener errors via `StoreEventSubject`, ensure the error event emission is itself wrapped in try-catch with final swallow. Alternatively, use a separate error channel. | Medium |
| 04-3 | Registry changes are in `DomainModeling.Runtime` (implementation), not in `IEventTypeRegistry`/`ISnapshotTypeRegistry` (abstractions). The interface contract (`void Register(string, Type)`) doesn't document whether duplicates throw. | A second implementation of `IEventTypeRegistry` wouldn't know it should throw on conflicts. The behavior is an undocumented contract. | Add XML doc to `IEventTypeRegistry.Register` and `ISnapshotTypeRegistry.Register` in the abstractions specifying: "Throws `InvalidOperationException` if `eventName` is already registered to a different type. Silently skips if the same name and type are re-registered." This makes the contract explicit for any implementor. | High |
| 04-4 | `OperationResult` lives in `DomainModeling.Abstractions`—an abstractions package. The serialization `[Id]` layout is an implementation detail of Orleans. Removing `[Id(0)]` from `Success` changes the wire format. While pre-1.0, any currently running Orleans silos using this type in grain method signatures will fail during rolling upgrades. | The backwards-compatibility instruction says storage names are sacred but pre-1.0 API changes are free. `OperationResult` is an RPC return type (not persisted to storage), so this is an RPC-level break during rolling upgrades only. | Document in PR that this is a rolling-upgrade breaking change: all silos must be restarted together, not rolled. Pre-1.0 this is acceptable but worth noting. | Medium |

---

## Persona 05: Platform Engineer

**Focus**: telemetry, failure modes, deployment safety

| # | Issue | Why It Matters | Proposed Change | Confidence |
|---|-------|---------------|-----------------|------------|
| 05-1 | SP-04 silently swallowing listener exceptions means zero observability into listener failures. No metrics, no log entries, no health check degradation. | In production (even client-side), unobservable failures are operationally dangerous. A broken listener could silently degrade the entire UI with no way to diagnose it remotely. | Add an error counter or emit a structured event through `StoreEventSubject` for each failed listener. This preserves isolation while enabling monitoring via the existing observability surface. | High |
| 05-2 | SP-02 Registry throwing `InvalidOperationException` at startup is a fail-fast pattern—correct for configuration errors. But if the registration happens during `IServiceProvider` resolution (it does: `TryAddSingleton<IEventTypeRegistry>(provider => { ... })`), the exception will surface as a DI resolution failure, potentially with a confusing stack trace. | Developers will see `InvalidOperationException` wrapped in an `InvalidOperationException` from the DI container, making diagnosis harder. | Wrap the exception in a more descriptive custom exception or ensure the message clearly states "during event type registry initialization" with the conflicting type names. The current plan's message is adequate: `"Event name '{eventName}' is already registered to type '{existingType.FullName}'..."`. | Medium |
| 05-3 | `CosmosRetryPolicy` with `maxRetries = 0` means "try once, no retries." The plan correctly allows this. But `maxRetries = 0` with the existing log message `"All retries exhausted after {MaxRetries + 1} attempts"` would log `"1 attempts"` (grammatically incorrect). | Minor but indicates attention to detail. | Fix log message grammar: use conditional pluralization or say "after {MaxRetries + 1} attempt(s)". | Low |

---

## Persona 06: Distributed Systems (Mississippi Specialist)

**Focus**: Orleans correctness, grain lifecycle, concurrency

| # | Issue | Why It Matters | Proposed Change | Confidence |
|---|-------|---------------|-----------------|------------|
| 06-1 | **Registry `TryGetValue` + indexer assignment is NOT atomic on `ConcurrentDictionary`.** The proposed fix replaces atomic `TryAdd` with a non-atomic read-then-write sequence. Between `TryGetValue` returning false and `nameToType[eventName] = eventType`, another thread could register a different type for the same name, and the second write overwrites silently—exactly the bug the fix is supposed to prevent. | This is a **concurrency regression** in a type used at startup during parallel assembly scanning and DI resolution. Orleans silo startup can be concurrent. | Use `ConcurrentDictionary.GetOrAdd(eventName, eventType)` which is atomic: `Type registered = nameToType.GetOrAdd(eventName, eventType); if (registered != eventType) throw new InvalidOperationException(...);`. The `GetOrAdd` atomically returns the existing value or inserts the new one. Then separately `typeToName.TryAdd(eventType, eventName)` for the reverse mapping (benign race on same-type). | **High** |
| 06-2 | Orleans `[GenerateSerializer]` generates code that calls the parameterless constructor for `readonly record struct` types. When `field ?? string.Empty` is added, the Orleans deserializer sets properties via generated code that assigns to the backing field directly. Does `field ?? string.Empty` in the getter interfere with Orleans assignment? | Orleans serializer uses `field`-level access (via generated code). The `field ?? string.Empty` only affects the *getter*—the setter/init still writes the raw value. So Orleans writes `null` to the backing field, and the getter returns `string.Empty`. This is correct behavior. | No code change needed—verify with a round-trip serialization test (deserialize a payload with null string fields, confirm properties return `string.Empty`). Add this test to each sub-plan. | High |
| 06-3 | Removing `[Id(0)]` from `OperationResult.Success` changes the Orleans serialization wire format. Any in-flight RPC calls or queued messages containing `OperationResult` will fail to deserialize during a rolling upgrade. | Pre-1.0 the framework has no deployed users (development-only), so this is acceptable. But the plan should explicitly acknowledge the rolling-upgrade incompatibility. | Add a note to SP-02 acceptance criteria: "Requires full cluster restart; not compatible with rolling upgrades from previous builds." | Medium |
| 06-4 | The `typeToName` reverse dictionary in the Registry uses `TryAdd` for the reverse mapping. If two different event names map to the same CLR type (via explicit `Register` calls with different names), only the first name wins for reverse lookup. The plan doesn't address this existing asymmetry. | Not a regression from the plan—pre-existing issue. But worth documenting as out-of-scope. | No change—note as a pre-existing limitation that is not in scope for this fix. | Low |

---

## Persona 07: Event Sourcing & CQRS (Mississippi Specialist)

**Focus**: schema evolution, storage-name immutability, reducer purity

| # | Issue | Why It Matters | Proposed Change | Confidence |
|---|-------|---------------|-----------------|------------|
| 07-1 | `OperationResult` is used as an RPC return type (`Task<OperationResult> ExecuteAsync(...)` on aggregate grains), never persisted to event/snapshot storage. Serialization layout changes are RPC-only—no event schema evolution concern. | Confirms the change is safe from an event-sourcing perspective. `OperationResult` does not participate in event streams or snapshot storage. | No change needed. | High |
| 07-2 | The Registry duplicate detection fix protects against a critical event-sourcing failure mode: two different CLR types with the same `[EventStorageName]`. Without this fix, event deserialization silently returns the wrong type, corrupting aggregate state reconstruction. | This is arguably the highest-value fix in the entire plan. It prevents data corruption in the event store. | Elevate this in the PR description—this is a data-integrity fix, not just a "nice to have" validation. | High |
| 07-3 | `ScanAssembly` calls `Register` for each attributed type. After the fix, if a user has two types with the same `[EventStorageName]` in the same assembly, `ScanAssembly` will throw mid-scan. The caller (`AddAggregateSupport` in `AggregateRegistrations.cs`) will get an `InvalidOperationException` during DI resolution. The partially-registered state is left in the dictionaries. | Fail-fast at startup is correct, but partial registration state could confuse retry attempts (if someone tries to call `ScanAssembly` again on the same registry after catching the exception). | This is acceptable for startup-only code—if the configuration is wrong, the app should not start. No code change needed, but document that Registry state is undefined after a conflict exception. | Medium |
| 07-4 | `BrookPosition` documentation fix (Finding 7) correctly identifies that `default(BrookPosition).Value == 0` is indistinguishable from a valid first event position. The plan chooses documentation over code change. | Correct decision. Changing `BrookPosition` would break event position math throughout the framework. Position 0 is semantically "first event." | No change to the decision. Ensure the XML docs are detailed enough: explain that `default` bypasses the parameterless constructor, and recommend `new BrookPosition()` over `default`. | High |

---

## Persona 08: Performance & Scalability (Mississippi Specialist)

**Focus**: allocations, hot-path cost

| # | Issue | Why It Matters | Proposed Change | Confidence |
|---|-------|---------------|-----------------|------------|
| 08-1 | `field ?? string.Empty` adds a null-coalescing check on every property access. For keys used as grain identity (resolved on every grain call), this is a hot path. | The check is a single null comparison + branch—nanosecond cost. `string.Empty` is a static field, no allocation. JIT will likely inline this. Negligible. | No change needed. | High |
| 08-2 | SP-04's try-catch per listener in `NotifyListeners` adds a try-catch overhead per iteration. In Blazor scenarios with many components subscribing to state changes, this loop runs frequently. | Try-catch in .NET has zero cost when no exception is thrown (the JIT emits metadata tables, not guard instructions). The only cost is preventing certain JIT optimizations like inlining. For a loop that calls `Action` delegates, inlining is already impossible. | No performance concern. | High |
| 08-3 | The Registry `GetOrAdd` approach (if adopted per 06-1) uses a delegate-free overload (`GetOrAdd(key, value)`)—no closure allocation. | Validates that the suggested concurrency fix has no allocation overhead. | Confirm the reviewers' suggestion uses the `GetOrAdd(TKey, TValue)` overload, not `GetOrAdd(TKey, Func<TKey, TValue>)`. | Medium |
| 08-4 | `OperationResult.Success` changing from stored bool to computed `ErrorCode is null` adds one null check per access instead of a direct field read. `Success` is checked on every command response. | Single null comparison—nanosecond cost. Not measurable. The JIT may even optimize this to a single test instruction. | No performance concern. | High |

---

## Persona 09: Developer Experience (Mississippi Specialist)

**Focus**: API ergonomics, error messages

| # | Issue | Why It Matters | Proposed Change | Confidence |
|---|-------|---------------|-----------------|------------|
| 09-1 | `default(OperationResult).Success == true` is counterintuitive for developers coming from Result/Either patterns where "uninitialized" typically means "error" or "pending." The `ValueTask` parallel (default = success) is defensible but not universally expected. | Developers may be confused by `default(OperationResult)` being success. Opinion will vary by background. The plan's rationale (pit-of-success) is sound—accidental defaults should not trigger error handling. | No code change—the decision is defensible. Add a code example in the XML docs showing the intended usage patterns: `Ok()`, `Fail()`, and a warning about `default`. | Medium |
| 09-2 | `BrookAsyncReaderKey.Parse(null!)` currently throws `NullReferenceException`, which has no `paramName`. The fix adds `ArgumentNullException.ThrowIfNull(key)` which includes `paramName: "key"`. Good DX improvement. | Developers catching or logging the exception will see the parameter name, speeding up diagnosis. | No change—plan is correct. | High |
| 09-3 | Registry `InvalidOperationException` message in SP-02 includes both type `FullName`s and the conflicting name. This is good. But the exception type `InvalidOperationException` is generic—consider whether a custom exception type would be better for programmatic catching. | `InvalidOperationException` is fine for startup configuration errors that should crash the app. A custom type would add API surface for no practical benefit at this stage. | No change—`InvalidOperationException` is appropriate. | High |
| 09-4 | `CosmosRetryPolicy` with `maxRetries = 0` semantics ("try once, no retries") is not immediately obvious. Some developers might expect 0 to mean "don't try at all." | The retry loop `for (int attempt = 0; attempt <= MaxRetries; ...)` means `maxRetries = 0` does execute the operation once. This is standard retry semantics but could confuse. | Add XML doc to the `maxRetries` parameter: "The maximum number of retry attempts after the initial try. A value of 0 means the operation is attempted once with no retries." | Medium |

---

## Persona 10: Security (Mississippi Specialist)

**Focus**: input validation, trust boundaries

| # | Issue | Why It Matters | Proposed Change | Confidence |
|---|-------|---------------|-----------------|------------|
| 10-1 | Key struct null-safety is defensive hardening—good. The `field ?? string.Empty` pattern prevents null strings from flowing into Orleans grain identity resolution, where a null key could cause unpredictable grain activation behavior. | Orleans uses key strings for grain identity. A null key could map to an unexpected silo/grain, causing cross-tenant data leakage in multi-tenant deployments. | No change—the fix is correct and security-relevant. | High |
| 10-2 | `BrookAsyncReaderKey.Parse(null!)` null guard prevents a potential denial-of-service where untrusted input causes `NullReferenceException` instead of the expected `ArgumentNullException`. Both crash, but `ArgumentNullException` is typically caught earlier in validation middleware. | Minor security improvement—shifts the exception to a more expected type. | No change—plan is correct. | Medium |
| 10-3 | The Store listener isolation (SP-04) prevents a malicious or buggy listener from blocking other listeners. In a Blazor context, listeners run in the browser—this is a client-side concern, not a trust boundary issue. | Low security impact—client-side code. Resilience, not security. | No change. | Low |
| 10-4 | Registry duplicate detection prevents a misconfiguration attack: if a malicious assembly is scanned and it has a type with the same `[EventStorageName]` as a legitimate type, the first-registered-wins behavior means the malicious type could win. The fix makes this throw, which is better—but only if the legitimate type is registered first. | If the malicious assembly is scanned first, it still wins. The fix doesn't change the trust model—assembly scanning is trusted by default. | No code change—document that `ScanAssembly` trusts all types in the assembly. Assembly trust is managed via deployment, not runtime validation. | Low |

---

## Persona 11: Source Generator & Tooling (Mississippi Specialist)

**Focus**: Roslyn generator interaction, IDE experience

| # | Issue | Why It Matters | Proposed Change | Confidence |
|---|-------|---------------|-----------------|------------|
| 11-1 | C# 14 `field` keyword interacts with Orleans `[GenerateSerializer]` source generator. The Orleans generator emits code that accesses serializable members. `string BrookName { get => field ?? string.Empty; }` with `[Id(0)]` — the generator must recognize this as a serializable property and access the backing field for writes. | Orleans source generator reads the model at compile time. `field` keyword introduces a compiler-synthesized backing field. The Orleans generator should handle this identically to a standard auto-property because it operates on the property-level `[Id]` attribute, not on fields directly. The generated serializer calls the property setter (or init accessor), not the backing field. | Verify by compiling SP-01 and inspecting the generated serializer code (in `obj/` output). If the generated code calls the property getter during serialization (returning `string.Empty` for null) and the setter during deserialization (writing the raw value), the round-trip is correct. Add a test that serializes and deserializes a default-constructed key struct. | High |
| 11-2 | `Inlet.Client.Generators`, `Inlet.Gateway.Generators`, and `Inlet.Runtime.Generators` are Roslyn source generators that emit code referencing `OperationResult`. Changing `OperationResult` serialization layout doesn't affect generated source (generators emit `OperationResult result = await grain.ExecuteAsync(...)` and check `result.Success`—no direct serialization code). | Confirming no generator code needs updating. Generators work at the API level, not the serialization level. | No change needed—generators are unaffected. | High |
| 11-3 | The `[MemberNotNullWhen]` attributes on `OperationResult.Success` will be on a computed property after SP-02. IDE flow analysis will still work correctly because `MemberNotNullWhen` is evaluated based on the property's return value at call sites, regardless of whether it's stored or computed. | Confirms the NRT attributes work on computed properties. | No change needed. | High |

---

## Persona 12: Data Integrity & Storage (Mississippi Specialist)

**Focus**: Cosmos partition keys, event stream consistency

| # | Issue | Why It Matters | Proposed Change | Confidence |
|---|-------|---------------|-----------------|------------|
| 12-1 | Key structs (`BrookKey`, `SnapshotStreamKey`, etc.) are used as Cosmos DB partition keys and document IDs. `default` keys with `string.Empty` properties would produce partition keys like `"|"` (BrookKey) or `"|||"` (SnapshotStreamKey). These are valid but semantically dangerous—a query with this partition key would return unrelated documents. | Changing null to `string.Empty` slightly improves the failure mode (consistent empty-string key instead of NRE) but doesn't prevent misuse. A `default` key used as a Cosmos partition key is always a bug. | No code change—the fix is still an improvement (no NRE). Consider adding a `IsDefault` property to key structs for runtime validation at Cosmos operation boundaries. This is out-of-scope for this plan but worth tracking as a follow-up. | Medium |
| 12-2 | `CosmosRetryPolicy` negative validation is at the constructor level. The `MaxRetries` property is `private` — no external mutation after construction. This means the validation is complete. | Confirms the fix is sufficient—no other path to set a negative value. | No change needed. | High |
| 12-3 | Plan correctly identifies that `BrookPosition` is a data-integrity type where `default` semantics cannot change without breaking persisted event positions. The documentation-only approach is correct. | Any code change to `BrookPosition.Value` default would orphan all persisted events at position 0. | No change to the decision. The XML doc should explicitly state: "This is a data-integrity constraint. Do not change the default behavior of `Value`." | High |
| 12-4 | `OperationResult` serialization layout change: the plan removes `[Id(0)]` from `Success`. The remaining IDs are `[Id(1)]` for `ErrorCode` and `[Id(2)]` for `ErrorMessage`. For `OperationResult<T>`, the IDs are `[Id(0)]` on `Success`, `[Id(1)]` on `Value`, `[Id(2)]` on `ErrorCode`, `[Id(3)]` on `ErrorMessage`. Removing `[Id(0)]` leaves a gap. Orleans handles ID gaps gracefully (missing IDs are left at default during deserialization), so this doesn't cause immediate deserialization failures—but it does mean old serialized payloads will have an extra `bool` field in slot 0 that is silently discarded. | Since `OperationResult` is RPC-only (not persisted), there are no old payloads to worry about. This is a non-issue for this repo. | No change needed—confirm in PR description that `OperationResult` is never persisted to storage. | Medium |

---

## SYNTHESIS

### Deduplication

The following issues were raised by multiple personas and are consolidated:

- **OperationResult<T> MemberNotNullWhen violation** (03-1, 11-3, 12-4): Converging assessment—the non-generic `OperationResult` change is safe, but **`OperationResult<T>` is not safe** because `default` produces `Success=true` with `Value=null`, violating `[MemberNotNullWhen(true, nameof(Value))]`.
- **Registry concurrency bug** (06-1, 08-3): The proposed `TryGetValue` + indexer assignment is not atomic. Must use `GetOrAdd` instead.
- **Store listener swallowing without observability** (03-2, 05-1): Empty catch blocks violate logging policy and hide bugs. Need at least an event emission for diagnostics.
- **Registry contract documentation** (04-3, 07-2): The behavior should be documented on the interface, not just the implementation.
- **OperationResult RPC-only confirmation** (07-1, 12-4): Multiple personas confirm this is safe because the type is not persisted.

### Categorized Findings

#### MUST (Block the plan if not addressed)

| ID | Finding | Origin | Action |
|----|---------|--------|--------|
| M-1 | `OperationResult<T>` default-as-success violates `[MemberNotNullWhen(true, nameof(Value))]`. `default(OperationResult<int?>)` would have `Success=true`, `Value=null`. | 03-1 | Either (a) don't apply default-as-success to `OperationResult<T>`, only `OperationResult`; or (b) make `OperationResult<T>.Success` check `ErrorCode is null && Value is not null`; or (c) remove the `MemberNotNullWhen(true, nameof(Value))` annotation (degrading NRT experience). |
| M-2 | Registry `TryGetValue` + indexer write is not atomic. Concurrent `ScanAssembly` / `Register` calls can silently overwrite conflicting registrations—defeating the purpose of the fix. | 06-1 | Replace with `ConcurrentDictionary.GetOrAdd(eventName, eventType)` + type comparison. |

#### SHOULD (Strongly recommended, address before merge)

| ID | Finding | Origin | Action |
|----|---------|--------|--------|
| S-1 | `Store.NotifyListeners` empty catch block provides zero observability. Emit a diagnostic event via `StoreEventSubject` (e.g., `ListenerErrorEvent`) for each caught exception. | 03-2, 05-1 | Add event emission in catch block. New  `ListenerErrorEvent : StoreEventBase` type in `Reservoir.Abstractions.Events`. |
| S-2 | `IEventTypeRegistry.Register` and `ISnapshotTypeRegistry.Register` interface XML docs should specify duplicate handling semantics (throw on conflict, skip on same-type). | 04-3 | Update XML docs on interface methods in DomainModeling.Abstractions. |
| S-3 | SP-02 bundles three distinct logical changes (keys, OperationResult, Registry). Consider splitting into separate commits or sub-plans for easier bisection. | 03-4 | At minimum, make each fix a separate atomic commit within the PR. |
| S-4 | Add Orleans round-trip serialization tests for default-constructed key structs to verify `[GenerateSerializer]` + `field` keyword interaction. | 06-2, 11-1 | Add tests in each sub-plan that serialize → deserialize → assert properties return `string.Empty`. |
| S-5 | PR title for SP-02 should use `+semver: breaking` given the OperationResult semantic change and Registry behavioral change. | 01-1 | Update planned PR title. |
| S-6 | Confirm existence of `tests/Tributary.Abstractions.L0Tests/` and `tests/Aqueduct.Abstractions.L0Tests/` before execution. If absent, SP-03 scope is larger than estimated. | 02-2, 03-5 | Verify and update SP-03 scope if needed. |

#### COULD (Nice to have, low risk of skipping)

| ID | Finding | Origin | Action |
|----|---------|--------|--------|
| C-1 | Add `maxRetries` XML doc clarifying 0 = "try once, no retries". | 09-4 | One-line doc addition. |
| C-2 | Use `existingType.FullName ?? existingType.Name` in Registry exception message for robustness against dynamic types. | 01-3 | Defensive string building. |
| C-3 | Fix `"All retries exhausted after {N} attempts"` grammar for N=1 case. | 05-3 | Minor log message polish. |
| C-4 | Document in PR that `OperationResult` is RPC-only and never persisted, confirming serialization layout change is safe. | 06-3, 12-4 | PR description note. |
| C-5 | Add `IsDefault` property to key structs for runtime validation at storage boundaries. | 12-1 | Follow-up task, not this plan. |

#### WON'T (Rejected or out-of-scope)

| ID | Finding | Origin | Reason |
|----|---------|--------|--------|
| W-1 | Change `BrookPosition` default behavior. | 07-4, 12-3 | Data integrity constraint—changing position 0 semantics would orphan persisted events. |
| W-2 | Use custom exception type for Registry duplicates. | 09-3 | `InvalidOperationException` is appropriate for startup configuration errors. Over-engineering for pre-1.0. |
| W-3 | Guard against malicious assemblies in `ScanAssembly`. | 10-4 | Assembly trust is a deployment concern, not a runtime validation concern. |
| W-4 | Address `typeToName` reverse dictionary asymmetry (two names → same type). | 06-4 | Pre-existing limitation, not introduced by this plan. |
