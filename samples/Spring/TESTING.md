# Where Spring tests live and when they run

Choose the test level by the behavior under test. Choose a smoke suite when a small set of critical journeys is enough for an initial check.
Using Aspire does not by itself make a test L2: the browser journey through the deployed client, API, Orleans, storage, and SignalR is L3.

| Location | Add tests for | Local command | CI execution |
| --- | --- | --- | --- |
| `Spring.Domain.L0Tests` | Domain handlers, reducers, and effects | Repository unit-test scripts | L0 Tests |
| `Spring.Client.L0Tests` | Client state and component behavior in isolation | Repository unit-test scripts | L0 Tests |
| `Spring.L2Tests` | Generated API contracts, authorization, storage-backed commands, and projection queries | `pwsh ./test-spring.ps1 -TestLevel L2 -Suite Full` | L2 Tests on PR; workflow dispatch |
| `Spring.L3Tests/Smoke` | A small set of critical browser journeys proving the app works | `pwsh ./test-spring.ps1` | L3 Tests: `L3 Spring E2E (Smoke)` on PR and merge queue |
| `Spring.L3Tests` outside `Smoke` | Additional browser scenarios, including API reference UI | `pwsh ./test-spring.ps1 -TestLevel L3 -Suite Full` | L3 Tests: select `Full` when dispatching manually |
| `Spring.TestHarness` | Shared Aspire application lifecycle and resource diagnostics; no tests or Playwright | Built transitively | Reused by L2 and L3 |

Run these commands from the repository root. The default is `-TestLevel L3 -Suite Smoke`.
`Full` means every test at the selected level. To verify both levels, run L2 Full followed by L3 Full.
L2 Smoke is not defined for Spring; the command rejects that combination instead of running no tests.

## Add a test

1. Start with an L0 test for behavior that does not require a deployed service.
2. Use L2 for functional API and infrastructure contracts. Keep browser references out of this project.
3. Use L3 when exercising the real application through its browser UI or a complete external user journey.
4. Put a critical L3 smoke journey under `Spring.L3Tests/Smoke` and apply `[Trait("Category", "Smoke")]`.
5. Put additional L3 cases outside `Smoke`; they are included in the full suite without enlarging the PR smoke gate.
6. Reuse the application fixture from `Spring.TestHarness`; do not make one test project reference another.

Both levels own fresh Aspire deployments with isolated endpoints and emulator state. L2 does not install or launch Chromium.
L3 shares its browser fixture within the test collection. Use separate worktrees for simultaneous builds.
The root command records the selected level, suite, project, result, and artifact directory in `summary.json`.

See [Spring validation prerequisites and diagnostics](../../README.md#validate-spring-after-a-change) and the [repository test-level definitions](../../.github/instructions/testing.instructions.md).

## Flagship journey coverage

The full L3 suite preserves the banking, transfer, API-reference, accessible panel and theme journeys.
FlagshipJourneysTests adds first-run completion at 390×844, updates in a second browser, rejected withdrawals with unchanged projections, a real committed deposit whose reply is lost with expanded keyboard-scrollable history at 320/390px, all six burst controls and ledger retention, high-value investigation flags, defined transfer compensation, the five-persona command/read/saga authorization matrix including reselecting the active persona, delayed allowed reads after a newer denial, and account anchors/shared links after task navigation.
The L0 component tests distinguish accepted requests from loading projections, prioritize in-flight requests, retain mixed response history, escape snapshot content and prevent an invalid transfer draft from submitting an earlier valid amount.

Use the [capability map](JOURNEYS.md) to connect a demo task to its sources and assertions.
See [browser review evidence](#browser-review-evidence) for the artifact workflow and its limits.
The C# Full suite also saves named flagship PNGs and per-image route, viewport, theme and browser metadata under its runner-owned artifact directory.
Three touch cases cover all task routes at 1440×900, 390×844 and 320×740, including active navigation, heading focus and containment.
Existing journey assertions remain in place; screenshots support them and do not replace passing L2/L3 results.

Before final validation, stop the interactive app and run the canonical cleanup and Release pipeline in addition to full L2 and L3.
go.ps1 excludes deployed L2/L3 tests. Doctor's READY result is prerequisite evidence only.
Samples do not require mutation testing under the current policy; report any chosen or skipped mutation run explicitly.

## Browser review evidence

Stop the interactive app, then run pwsh ./test-spring.ps1 -TestLevel L3 -Suite Full.
Read the emitted SUMMARY JSON and spring.trx; PASS requires executed, passing tests.
The runner creates a unique directory under artifacts/spring, containing its test results, resource logs, banking screenshot and trace.
Its flagship subdirectory contains named PNGs and per-image route, viewport, theme, browser and file-timestamp metadata from the C# Playwright tests.

FlagshipRouteEvidenceTests taps all five task links at 1440×900, 390×844 and 320×740, checking the active task, heading focus and page containment.
FlagshipJourneysTests retains the existing outcome assertions and captures first-run, shared-pair, account switching, bursts, stale-read denial, investigation, persona, compensation and lost-reply states.
The existing banking tests retain their additional setup, transfer and theme screenshots.
These images are observed browser states, not pixel-comparison baselines or replacements for assertions.
The C# capture helper does not run axe or certify accessibility; semantic, touch, focus and keyboard checks remain explicit test assertions.

Generated screenshots, manifests, traces and raw results belong in the ignored artifacts directory and review attachments, not in the application source tree.
The existing L3 Tests workflow uploads `artifacts/spring` as `spring-l3-<suite>-<runner OS>` and retains it for seven days.
For the complete gallery in CI, dispatch that workflow on the reviewed branch with suite=Full; the normal PR run selects Smoke.
Link the exact run and artifact in the PR, identify the captured routes and sizes, and attach selected phone and desktop previews to the PR description or a top-level comment.
Archive needed evidence before that retention period expires.

For interactive auth comparison, use run-spring.ps1 with -LocalAuth On, stop it, then relaunch with -LocalAuth Off.
The local header personas are a development demonstration; with auth off, all five receive 401 on protected Auth Proof endpoints.
Do not leave the interactive app running while test scripts use the same worktree.
