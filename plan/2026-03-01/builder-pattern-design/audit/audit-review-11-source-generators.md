# CoV Review: Source Generator & Tooling Specialist

- **Claims / hypotheses**: Source generators need to safely shift to emitting builder patterns.
- **Verification questions**: Do analyzers block on obsolete calls when generating code?
- **Evidence**: Generators will conditionally emit the new builder signatures if Common.Builders.Abstractions is present.
- **Triangulation**: The #if or symbol-detection is standard Roslyn methodology for incremental generators.
- **Conclusion + confidence**: High.
- **Impact**: Seamless backwards compatibility while transitioning generators.

## Issues Identified
- **Issue**: We need to ensure that the Source Generators run after dependencies are calculated, so they see the new abstraction assemblies correctly.
- **Why it matters**: False compilation failures in generated code.
- **Proposed change**: Add a note for compilation validation covering incremental generator output inside L2 tests.
- **Evidence**: Covered by "L2 startup parity" in testing requirements.
- **Confidence**: High.
