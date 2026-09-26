# Verify-change local bindings

Read these bindings with the [portable skill](../../.agents/skills/verify-change/SKILL.md)
when validating Mississippi changes. Scripts remain authoritative; inspect their
current signatures and the [README](../../README.md#ci--local-pipeline-options)
before choosing commands. These bindings do not create exceptions to policy.

## Authority and check selection

The [build rules](../instructions/build-rules.instructions.md),
[test policy](../instructions/testing.instructions.md),
[shared guardrails](../instructions/shared-policies.instructions.md), and
[scoped instruction inventory](../../AGENTS.md#instruction-loading) define local
requirements. Preserve their zero-warning, package, coverage, deterministic-test,
cleanup, and meaningful-execution rules. Use scope/risk to add separate checks;
narrow iteration does not waive final gates. Assessment-only inspects supplied
reports and source without invoking commands that run checks or write artifacts.

| Check | Canonical entrypoint and evidenced scope |
| --- | --- |
| Both-solution final pipeline | `pwsh ./go.ps1 -Configuration Release`: build and cleanup each solution, L0/L1 tests against the cleaned tree, Mississippi coverage summary, then final warnings-as-errors build. |
| Focused conventional tests/coverage | `pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject <test-csproj-path> -SourceProject <source-csproj-path> -SkipMutation`; verify the actual source mapping. Bare test names resolve under `tests/`, not sample folders. |
| Solution unit tests | `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1` or `unit-test-sample-solution.ps1`; both default to L0/L1. |
| Separate core L2 | `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1 -TestLevels L2Tests`; requires the selected tests' infrastructure. |
| Separate sample L2 | `pwsh ./eng/src/agent-scripts/integration-test-sample-solution.ps1 -TestLevels L2Tests`. |
| Iteration cleanup | `pwsh ./clean-up-targeted.ps1 -Files <paths>` or `-FileListPath <file>`; default changed-vs-base mode uses `-BaseRef main`. Select the actual base for a stack. |
| Final cleanup | `pwsh ./clean-up.ps1` processes both solutions with `Directory.DotSettings`; do not replace it with a targeted or one-solution skip. |
| PowerShell | `pwsh ./eng/tests/orchestrate-powershell-tests.ps1`; separate from `go.ps1`. |
| Markdown | Use the configured markdownlint runner and active rules; for installed CLI2, `npx --no-install markdownlint-cli2 <explicit-files>`. |

`go.ps1` excludes separate PowerShell, L2/L3, documentation/browser, publishing,
deployment, and service-backed remote CI checks. `-SkipCleanup` produces
provisional iteration evidence, even when the pipeline exits zero or emits PASS;
full cleanup and checks invalidated by its edits still need final evidence.
`-IncludeMutation` selects explicit additional mutation work; default `go.ps1`
does not run mutation. Use the [mutation policy and binding](../instructions/mutation-testing.instructions.md#mandatory-route)
for that separate capability rather than imposing a score gate here.

For sample tests, inspect project references and pass the intended source project
explicitly: automatic resolution can select a framework test helper under `src/`.
The [feature binding](event-sourced-feature-bindings.md) includes the
complete Spring domain test/source command.

For custom test levels, invoke a wrapper from PowerShell with an array, for example
`& ./eng/src/agent-scripts/unit-test-sample-solution.ps1 -TestLevels @('L0Tests','L1Tests','L2Tests')`.
Levels filter project filenames, not a smoke level. Preserve the MTP runner and
per-project TRX/nonempty checks; VSTest-only logger/collector flags do not apply.
`-NoBuild` on focused quality is iteration evidence only after the required clean
build exists; it does not replace final warning-free builds. Standalone build and
cleanup wrappers are cataloged in the [script guide](../../eng/src/agent-scripts/README.md#script-catalogue).

## Spring and documentation

[Spring validation](../../README.md#validate-spring-after-a-change) separates
prerequisites from executed tests:

- `pwsh ./test-spring.ps1 -Doctor` checks SDK/Docker and returns READY only.
- `pwsh ./test-spring.ps1` runs L3 Smoke against a fresh Aspire test host.
- `pwsh ./test-spring.ps1 -TestLevel L2 -Suite Full` covers API/authorization.
- `pwsh ./test-spring.ps1 -TestLevel L3 -Suite Full` runs all browser journeys.

Read the emitted SUMMARY path under `artifacts/spring/` and its selected
level/suite, status, counts, TRX, and logs. PASS requires a nonempty completed run
with every selected test passing; Smoke is a suite within L3, not all L2/L3 tests.
The fixture owns its app/containers. Use separate worktrees for simultaneous
builds; do not use process-name cleanup or Docker prune. Keep rendered screenshots
and semantic assertions with the [UX evidence policy](../instructions/ux-validation.instructions.md).

For Docusaurus scope, inspect [package scripts](../../docs/Docusaurus/package.json)
and the [CI mapping](../../eng/src/agent-scripts/README.md#github-actions-mapping).
`npm run build` and `npm test` run from `docs/Docusaurus` after documented
prerequisites; `run-docs.ps1 -Mode Build` is not a browser-test pass. Follow the
[documentation policy](../instructions/documentation-authoring.instructions.md)
for examples, links, and page validation. Remote CI outcomes require the current
head/base and actual applicable jobs; local `go.ps1` cannot establish them.

## Evidence reuse

The [source-bound evidence contract](../../eng/src/agent-scripts/README.md#source-bound-validation-evidence)
records full-pipeline, focused-quality, and Spring runs under
`.scratchpad/validation-evidence/<run-id>/evidence.json`. Inspect status, arguments,
executed counts, source-before/after fingerprints, and artifact hash/length data.
The existing `Test-ValidationEvidence` function in
[ValidationEvidence.psm1](../../eng/src/agent-scripts/ValidationEvidence.psm1)
checks schema, selected-input freshness, required artifacts, and execution
consistency. A fresh selected fingerprint does not establish omitted input
freshness or a different scope. Verify relevant configuration/tool inputs too.

Inspect every selected module's TRX and current Cobertura files under
`.scratchpad/coverage-test-results`; compare invocation/source provenance before
reuse. The three documented empty SDK facades are explicit local exemptions,
not tested passes; every other selected module and the overall run need meaningful
execution. A missing record limits provenance claims; do not invent records for
standalone commands or assign an old report to current HEAD. Keep failed,
interrupted, skipped, stale, and prerequisite-only results separate from PASS.
