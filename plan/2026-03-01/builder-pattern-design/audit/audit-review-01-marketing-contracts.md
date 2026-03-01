# CoV Review: Marketing & Contracts

- **Claims / hypotheses**: The package split into Common.Builders.*.Abstractions helps discoverability and keeps contracts isolated.
- **Verification questions**: Does this fragmentation cause confusing naming for users?
- **Evidence**: PLAN.md states "Abstractions Topology (New): Common.Builders.Abstractions, Common.Builders.Client.Abstractions, etc."
- **Triangulation**: Standard MS conventions like Microsoft.AspNetCore.Builder provide precedents for bounded builder scopes.
- **Conclusion + confidence**: High.
- **Impact**: We accept the current naming as solid.

## Issues Identified
- **Issue**: The changelog / migration docs need a dedicated home for the 21 [Obsolete] replacements to guide users easily.
- **Why it matters**: A massive wall of obsolete warnings in user code upon upgrading will be frustrating without a central reference.
- **Proposed change**: Add a requirement to update docs/Docusaurus with a 'Migration to Builders' guide covering the 21 deprecated patterns.
- **Evidence**: [Obsolete("Use {BuilderType}.{Method}() instead...")] mapped in the plan.
- **Confidence**: High.
