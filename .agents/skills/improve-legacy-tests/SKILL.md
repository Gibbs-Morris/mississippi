---
name: improve-legacy-tests
description: Improve weak or legacy tests through characterization, behavior gap analysis, stronger deterministic assertions, and regression verification. Use when strengthening tests for existing behavior. Not for feature implementation, build-failure repair, mutation-report assessment, or general test strategy.
---

# Improve legacy tests

Improve confidence in existing behavior within the caller's authorized scope.
Keep test-quality work distinct from a requested behavior change. In assessment
mode, inspect the supplied source and evidence without editing or running checks.

## Establish the boundary and accepted behavior

Identify the target, source/test mapping, requested outcome, editable paths,
existing user changes, and available baseline. Read the consuming project's
applicable instructions and available local test bindings before choosing test levels,
naming, assertion libraries, packages, warning policy, coverage requirements,
commands, or report locations. Do not import another project's conventions.
If the target or required guidance is missing, report the narrow preparation
gap; continue useful authorized analysis without guessing a target or command.

Use contracts, requirements, existing callers, and maintained examples to
establish intended inputs, outputs, state changes, failures, and side effects.
Inspect existing tests and implementation to characterize current behavior.
A characterization records what happens; it does not prove that behavior is
intended. Mark disputed or undocumented behavior explicitly. A coverage report,
passing old assertion, or survivor does not authorize a new behavior contract.
If evidence conflicts, reconcile the material expectation before encoding it
as an approved invariant or changing production.

## Find a meaningful test gap

Obtain the authorized focused baseline using the local commands and inspect
actual execution and warnings. Validate any existing coverage or mutation
report's target, revision, completeness, and supported scope before reusing it.
Missing, empty, stale, partial, skipped, or failed evidence is not a clean pass.
Separate a pre-existing failure from a newly introduced regression. Route a
build or environment failure through its owning repair workflow rather than
weakening assertions, warnings, or dependencies to make the baseline green.

Choose a bounded gap that could hide incorrect behavior: a boundary, rejected
input, alternate state, failure outcome, ordering requirement, or side effect.
Design assertions against observable behavior and independent expected values.
Avoid merely calling uncovered lines, duplicating the implementation as the
oracle, or asserting incidental internals without a contract reason. Existing
reports help prioritize work; coverage alone does not establish test quality.

## Improve tests through a controlled loop

Add or strengthen the smallest tests that expose the selected gap. Preserve
useful existing assertions and follow local fixture and naming conventions.
For already-correct behavior, new tests may pass immediately; report that
honestly instead of changing production to manufacture a failing stage.
Demonstrate assertion sensitivity using available regression evidence or an
authorized disposable faulty implementation when useful. Do not infer that a
full mutation run is needed for ordinary test improvement.

If a new test fails, establish whether the cause is a wrong expectation, fixture
error, environment failure, or production defect. A failure grants no authority
to weaken the contract or edit production. Correct a demonstrably wrong test
within scope; report a production defect and obtain only missing authority.
When production changes are already authorized, make the smallest justified
change, retain a regression test, and report that change separately from test
refactoring. Avoid unrelated refactors, new features, suppressions, or exclusions.

Keep tests isolated and deterministic. Use the project's supported injected
clock, seeded inputs, temporary resources, and controlled dependencies. Diagnose
flakiness with a bounded reproduction and evidence about time, ordering, shared
state, or external services. Fix the supported test seam or fixture within scope;
do not hide intermittent failures with sleeps, retries, skips, or wider exclusions.
If the only correction needs an unauthorized production seam or infrastructure
change, report that boundary and leave the unresolved failure visible.

After a passing focused run, refactor test setup for clarity without weakening
assertions. Rebuild when inputs changed; use cached or no-build loops only when
local scripts and current outputs support them. Preserve the required final
build, cleanup, warning, coverage, and regression gates. Do not optimize tests
unless a measured cost or the request justifies it.

## Verify and hand off

Run the authorized focused tests and applicable surrounding regression checks.
Inspect executed counts, result files, warnings, and coverage on touched paths;
explain remaining behavior gaps and any unverified wider scope. For explicit
mutation execution or report assessment, use the available mutation workflow
and its local binding. It retains ownership of mutation evidence and effort;
ordinary improvement does not acquire a score threshold or survivor quota.
Keep feature implementation, general strategy, issue/PR management, and final
release approval with their existing owners.

Report the accepted contract and any characterized uncertainty, target and
revision, changed tests and justified production edits if authorized, commands,
executed results, warnings, supported coverage, remaining gaps, and deferred
work. Distinguish passing, failing, partial, unavailable, and unrun checks.
A proposed test plan or a ready prerequisite is not executed verification.
