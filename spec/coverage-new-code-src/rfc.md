# RFC: 95% coverage for changed src code

## Problem
Coverage for new/changed code under src is below the 95% target required for this work.

## Goals
- Identify changed code under src for this task.
- Achieve >=95% coverage for changed src code (allow small exceptions if infeasible).
- Keep changes minimal and aligned with existing patterns.

## Non-goals
- Raising coverage for unrelated legacy code.
- Broad refactors or new features.

## Current state (UNVERIFIED)
- Recent saga-related changes under src likely have low coverage.

## Proposed design (UNVERIFIED)
- Identify changed src files via git diff.
- Add focused L0 tests to cover changed logic.
- Re-run coverage per affected test project(s) and confirm >=95% for changed src code.

## Alternatives
- Ignore coverage gaps (rejected).
- Broader refactoring to simplify testing (rejected).

## Security
- No security-sensitive changes expected.

## Observability
- No new telemetry expected.

## Compatibility
- No breaking changes expected.

## Risks
- Some generator/Orleans behaviors may be hard to test directly.

## Mermaid
```mermaid
flowchart TD
  A[Identify changed src files] --> B[Map to test projects]
  B --> C[Add/adjust L0 tests]
  C --> D[Run coverage]
  D --> E[>=95% coverage for changed src]
```

```mermaid
sequenceDiagram
  participant Dev as Developer
  participant Repo as Repository
  participant Tests as Test Runner
  Dev->>Repo: Diff changed src files
  Dev->>Repo: Add tests
  Dev->>Tests: Run test-project-quality
  Tests-->>Dev: Coverage results
```
