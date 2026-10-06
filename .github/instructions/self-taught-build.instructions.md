---
applyTo: '**'
---

# Self-Taught Lessons: Build

Governing thought: Record validated build recovery steps without changing the canonical quality gates.

> Drift check: Read overlapping instructions and the current build/cleanup scripts before adding or applying a lesson.

## Rules (RFC 2119)

- For CleanupCode `CS0009` on an `obj/Debug` reference, agents **SHOULD** rebuild the affected solution in Debug before retrying the canonical gate. Why: Cleanup evaluated Debug after successful Release builds; rebuilding the samples repaired invalid Spring.TestHarness metadata and both cleanup stages passed.

- For generated-source inspection, agents **SHOULD** check the evaluated output path and keep emitted files outside compilation globs. Why: EmitCompilerGeneratedFilesOptIn emitted project-root Generated files when IntermediateOutputPath was empty; the next build compiled them again and reported duplicate definitions.

## Scope and Audience

Agents diagnosing repository build and cleanup failures.

## References

- Learning governance: [Self-improvement](self-improvement.instructions.md)
- Recovery limits: [Build issue remediation](build-issue-remediation.instructions.md)
- Canonical gates: [Build rules](build-rules.instructions.md)
