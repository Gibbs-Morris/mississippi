# Example harness issue

Contract version: 1.0

## Problem

Agent tasks cannot tell whether a validation result belongs to the source tree they intended to test.

## Observable outcome

A read-only report identifies the reviewed revision, selected inputs, and evidence artifacts.

## Scope

Add the report shape and deterministic tests. Preserve existing exit codes and console summaries.

## Relevant source and contracts

- `eng/src/agent-scripts/RepositoryAutomation.psm1` — shared automation entry points.
- `eng/src/agent-scripts/README.md` — command catalogue and artifact guidance.

## Decisions and non-goals

Use relative paths in published evidence. Do not collect secrets or make the report a signed attestation.

## Dependencies and readiness

The existing PowerShell test harness is the available regression surface.

## Acceptance criteria

- [AC1] A successful run writes one machine-readable report with a stable run identifier.
- [AC2] A source edit makes earlier evidence stale.

## Implementation outline

Define the result contract, write it atomically, and add fixture-based freshness checks.

## Validation plan

Run the PowerShell test orchestrator and inspect the JSON fixture results for success and stale-input cases.

## Risks and delivery boundary

Existing text output remains unchanged. The report proves evidence identity, not test correctness beyond what ran.

## Validation evidence map

- [AC1] Test: success-report fixture; expected: schema and run identifier are present.
- [AC2] Test: source-change fixture; expected: the earlier report is rejected as stale.
