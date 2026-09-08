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
