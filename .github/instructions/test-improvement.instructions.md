---
applyTo: "tests/**/*"
---

# Test Improvement Workflow

## Scope
Raising coverage/mutation on legacy code. See testing/mutation-testing files for standards.

## Quick-Start
`test-project-quality.ps1 -TestProject Name -SkipMutation` for coverage. Add `-NoBuild` for fast iteration. Then run without `-SkipMutation` for mutation.

## Core Principles
Don't edit production without approval. Target ≥95% coverage, ≥80% mutation. Use `test-project-quality.ps1` for quick feedback. Defer after 5 attempts per issue.

## Anti-Patterns
❌ Editing production first. ❌ Exceeding attempt limits. ❌ Ignoring scratchpad deferrals.

## Enforcement
Build/test/mutation MUST pass. Scratchpad tasks for remaining gaps.
