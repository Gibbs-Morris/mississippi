# Fresh-context issue delivery benchmark

This pack measures the issue-to-goal workflow from #733, #734, and the
reusable evaluation foundation in #543. It separates deterministic contract
checks from paid/live host trials; it does not claim that a static scenario
file proves model behavior.

The normal pack has five categories and requires three trials of each category
for each primary host: C# behavior, PowerShell harness, documentation,
browser-visible behavior, and multi-project/generator work. Each category also
declares independent checks and failure cases for interruption, stale evidence,
unavailable tooling, or changed scope.

`issue-delivery-results.json` is an initial baseline. The live Codex and
Copilot rows are explicitly unsupported because no paid/live model invocation
is authorized in ordinary CI; those three trials per category remain in the
denominator and are not passes. Deterministic contract trials are reported
separately and cover the result/authority invariants.

## Required live-run evidence

An authorized manual evaluator must record, for every trial:

- configured, client-accepted, and demonstrably active model and effort separately;
- a new context ID and isolated worktree ID for every trial, a clean repository
  state, fresh repository/issue input, scenario ID, host/version, and the same
  recorded source revision;
- acceptance result, every declared independent check ID with its individual
  result, review rework, interventions,
  tokens/time only when directly measured, and failure/unsupported reason;
- false-completion and authorization-violation counts on every trial record and
  in the aggregate summary, including zero values;
- browser route/state/viewport and Playwright screenshot/evidence where relevant.

A service outage, missing credential, unsupported host, or unavailable browser
is a recorded unsupported/blocked outcome, not an omitted trial. One authorized
goal invocation may iterate internally; approvals, missing access, and service
outages remain separate interventions. No universal success rate or token-saving
claim is valid until the raw denominators and host/model evidence are supplied.

Validate the pack and report with:

```powershell
pwsh ./eng/src/agent-scripts/validate-agent-evaluation.ps1 -Json
```
