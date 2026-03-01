# 02 — Clarifying Questions

## (A) Answered from Repository

1. **Is C# 14 `field` keyword available?** Yes — `LangVersion` is `14.0` in `Directory.Build.props` L8, SDK is 10.0.102 in `global.json`.
2. **Is the project pre-1.0?** Yes — `GitVersion.yml` `next-version: 0.0.1`. Breaking changes are freely permitted.
3. **How many key structs are affected?** 13 across 4 abstractions projects (Brooks, DomainModeling, Tributary, Aqueduct).
4. **Does StoreEventSubject have the same listener isolation issue?** Yes — `OnNext()` in `StoreEventSubject.cs` L68-73 also iterates without try-catch.

## (B) Asked User

1. **Key nulls scope**: Fix all 13 vs only the 3 dangerous ones? → **All 13 key structs** (user confirmed).
2. **Registry duplicate behavior**: Throw, log-and-skip, or conflict-only? → User wants **TryAdd-like DX matching IServiceCollection** — interpreted as: same name+type = idempotent skip; same name with different type = throw (fail-fast on conflicts).
3. **OperationResult default**: Change to success or document? → User wants **standard .NET design**. Research shows mixed conventions (Nullable=empty, ValueTask=success). Since OperationResult models "operation outcome" similar to ValueTask, and user's bug report explicitly describes the IsDefault sentinel fix, we'll make `default(OperationResult)` represent success.
4. **BrookPosition**: XML docs only vs skip? → **XML docs only** (user confirmed).
