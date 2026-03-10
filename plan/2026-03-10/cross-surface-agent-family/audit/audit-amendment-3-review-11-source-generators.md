# Amendment 3 Review — Source Generator & Tooling Specialist

## Persona

Source Generator & Tooling Specialist — Roslyn incremental source generator correctness, caching, diagnostic emission, generated code readability, compilation performance impact, analyzer interaction, and IDE experience.

## Findings

### 1. OBSERVATION — This plan does not create source generators

- **Issue**: The VFE family is agent Markdown files, not C# source generators. Most of my remit doesn't apply.
- **Proposed change**: None.
- **Confidence**: High.

### 2. MINOR — No guidance for how the VFE agents should handle source-generator concerns in the code they review/build

- **Issue**: The Mississippi repo uses source generators (e.g., `Inlet.Client.Generators`, `Inlet.Runtime.Generators`). When VFE Build implements code that involves source generators, or VFE Review reviews generator-related changes, the agents need to know that generated code must not be hand-edited and that generator changes require specific testing patterns.
- **Why it matters**: Agents unfamiliar with source generators might edit generated files or fail to run generator-specific validation.
- **Proposed change**: This is adequately covered by the "read repo instructions first" requirement, since the repo's instruction files cover source generator patterns. No plan change needed. The `vfe-repo-policy-compliance` specialist would surface generator-related policy during review.
- **Evidence**: Repo has `*.Generators` projects. The repo instructions cover source generators.
- **Confidence**: High.
