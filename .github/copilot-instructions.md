---
applyTo: "**/*"
---

# Mississippi Framework Core Standards

## RFC 2119 Keywords
MUST/SHALL = absolute requirement. MUST NOT/SHALL NOT = absolute prohibition. SHOULD/RECOMMENDED = strong guidance with rare justified exceptions. MAY/OPTIONAL = truly optional.

## Zero-Warnings Policy
Warnings MUST be fixed, never suppressed. Builds use `--warnaserror`. Analyzers, StyleCop, and ReSharper violations block PRs. `#pragma warning disable` requires explicit approval with exhaustive justification for single-line scope only; restore immediately after.

## Script Precedence
Before trusting documentation, open referenced scripts in `eng/src/agent-scripts/` to verify current behavior. Scripts are authoritative; prose is best-effort context.

## Merge Semantics
Global rules apply to all files. Domain-specific instruction files define deltas or exceptions. When both match a file, domain overrides global for that specific rule.

## Command Index
Build: `pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1`  
Clean: `pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1`  
Test: `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1`  
Mutation: `pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1`  
Final: `pwsh ./go.ps1`

## Quick-Start Pattern
Read domain file → verify script → apply rules → validate → commit. Domain files specify exact `applyTo` globs and constraints for their file types.
