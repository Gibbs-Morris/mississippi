---
applyTo: "tests/**/*,**/stryker*.json"
---

# Mutation Testing

## Scope
Stryker.NET workflow, survivor remediation. Mississippi solution only. See testing file for coverage.

## Quick-Start
Run: `pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1` (30min, wait for finish). Survivors: `summarize-mutation-survivors.ps1`. Thresholds: high 90, low 80, break 80.

## Core Principles
Maintain/raise score. Kill survivors via targeted tests, not production changes. Scratchpad tasks auto-generated per survivor.

## Workflow
Baseline → summarize → add tests → re-run → update tasks → repeat. Max 5 attempts per survivor before defer.

## Anti-Patterns
❌ Suppressing mutants. ❌ Changing production to kill survivors. ❌ Ignoring Stryker output.

## Enforcement
Mississippi only. Thresholds enforced. Survivors tracked in scratchpad.
