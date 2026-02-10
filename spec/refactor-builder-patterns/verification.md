# Verification

## Claim list
1. Builder APIs with terminal methods like `Done()` are inconsistent with Microsoft .NET builder conventions.
2. A configure-lambda pattern can replace terminal methods without losing expressiveness.
3. All existing builder usage sites can be migrated with minimal behavioral changes.
4. Tests and samples can be updated to the new pattern without weakening coverage.
5. Generator outputs can be updated to emit the new pattern.
6. The new pattern will remain consistent across client/server/silo/feature builder types.

## Verification questions
1. Which builder interfaces currently expose terminal methods like `Done()` (or equivalents), and where are they defined?
2. Which builder implementations rely on terminal methods to return to parent builders?
3. What extension methods use nested fluent patterns that depend on `Done()`?
4. Which tests and samples call `Done()` or equivalent return-to-parent methods?
5. Which code generators emit builder chaining that includes terminal methods?
6. Are there existing configure-lambda patterns in the codebase for builder sub-configuration?
7. Do any builders currently expose `Build()` methods that materialize a final object?
8. Which public API docs or XML comments describe the terminal methods explicitly?
9. What package consumers (samples) will be broken by removing terminal methods?
10. What is the minimal change set to switch to a configure-lambda pattern for feature builders?
11. Are there builder patterns in Microsoft .NET libraries that clearly show preferred structure (evidence source)?
12. Does any internal test harness or helper builder depend on a `Done()`-style method?
