---
applyTo: "**"
---

# Build Issue Remediation

## Scope
Fix compiler/analyzer/StyleCop/cleanup violations. Precision edits only. See global for zero-warnings policy.

## Quick-Start
Build → identify → fix (5 attempts max per issue) → verify → defer if blocked. Use `build-mississippi-solution.ps1`, `clean-up-mississippi-solution.ps1`.

## Core Principles
One issue = one code+file+location. Max 5 attempts before defer. Minimal edits. No `NoWarn` additions. Document deferrals in scratchpad.

## Prioritization
Unused vars/usings, nullable refs, disposables, async mismatches. Target highest count reduction with safest edits.

## Anti-Patterns
❌ Relaxing analyzers. ❌ Global suppressions. ❌ Changing unrelated code. ❌ Exceeding attempt limit.

## Enforcement
Defer after 5 focused attempts. Record in scratchpad with reason + next steps.
