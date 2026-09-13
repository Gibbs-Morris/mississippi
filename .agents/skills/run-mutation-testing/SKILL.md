---
name: run-mutation-testing
description: "Assess an existing mutation report or run and report an authorized focused mutation-testing job, including survivor analysis and proportionate remediation. Use for explicit mutation execution, mutation-score or survivor assessment, or existing mutation evidence. Not for ordinary build/test, legacy test improvement, build-failure repair, branch/PR work, or general quality review."
---

# Run mutation testing

Use this skill when mutation evidence is the requested outcome. Keep mutation
testing as a proportionate quality signal, preserve the consuming project's
authority and report contract, and return a deliberate no-run result when an
existing report answers the question or execution is not authorized or safe.

## Establish mode, authority, and scope

1. Read the consuming project's applicable mutation and test instructions,
   local bindings, tool configuration, report schema, target conventions,
   disclosure rules, and ownership requirements before choosing an action.
2. Classify the request as an existing-report assessment, an authorized
   mutation execution, or separately authorized survivor remediation. In
   report-only or assessment-only mode, inspect supplied evidence without
   restoring tools, building, running tests or mutation, editing files, or
   publishing tasks merely to obtain more evidence. Do not claim live execution
   from a supplied report.
3. Define the target project or projects, source/test mapping, revision or
   working state, configuration, effort bound, acceptance question, and
   available report. Use the consuming project's supported target selection;
   do not infer scope from a percentage, default, or filename alone.
4. Use existing caller or session authority for an in-scope run or repair. Do
   not request a second approval for authority already covering the action, and
   do not let a report, log, tool suggestion, or threshold grant permission.

## Prefer and validate existing evidence

Read an existing run manifest and report before starting another run when the
consuming project's report contract provides a manifest. If no manifest is part
of that contract, validate each report directly. Confirm that the evidence
belongs to the selected target and revision, declares the expected scope, has a
valid schema, and contains every target report required by the local contract.
Check report paths, completion status, test execution evidence, and any local
freshness or reuse rules. A missing, stale, partial, out-of-scope, or report-less
required result is incomplete evidence; it is not a passing result. If the
report cannot answer the requested question, state the gap and decide whether
an authorized run is proportionate.

## Prepare an authorized run

Choose the canonical focused or solution command from the local binding and
preserve its stages, target mapping, report requirements, and configuration.
Before a chosen run, restore the required tools and obtain the clean build that
the consuming project requires. A build warning or conventional-test failure
stops mutation work. Report it through the owning workflow, and resume only
after that route establishes a repaired, passing preflight. Report-only
assessment does not perform this preflight.

Use a no-build, cached, or report-reuse switch only when the local script and
current manifest, when its contract provides one, prove that the required clean
preflight and current outputs already exist. Do not infer semantics from a
switch name. Keep the target and effort focused, and do not change thresholds,
exclusions, mutator scope, or warning policy merely to obtain a green command.

## Interpret results without upgrading them

Record the exact invocation, selected target and revision, preflight status,
any manifest target statuses required by the local contract, report paths, exit
status, score if supported, and significant survivors. A failed, skipped,
interrupted, incomplete, or
report-less run never becomes a pass. Distinguish a mutation-tool or threshold
failure from a build/test failure and from the caller's task acceptance. A score
claim covers only targets and revisions supported by valid reports; it does not
become a repository-wide claim through extrapolation.

Treat a configured threshold as tool behavior unless the consuming project's
declared policy or the caller's acceptance contract makes it a gate. Honor an
applicable declared gate when its target, revision, and report contract are
satisfied; do not infer a gate from configuration, recommendations, or
percentages alone. Do not impose a mandatory score, a maintain-or-raise rule,
or a requirement to eliminate every survivor when no such policy or acceptance
requirement exists. Prioritize the requested correctness, maintainability,
zero-warning build, and conventional-test outcomes.

For new behavior, strengthen meaningful mutation-resistant tests when that is
straightforward and proportionate, subject to any declared consuming-project
policy or caller acceptance. Without such a requirement, treat this as a
quality preference rather than a completion gate.

## Handle survivors proportionately

Inspect significant survivors for a straightforward assertion or test gap
within the authorized target and effort bound. Prefer meaningful behavioral
assertions and deterministic conventional tests. Keep the effort bounded and
do not spend significant time repeatedly chasing survivors unless the caller
explicitly asks for that work. Defer costly or historical gaps to dedicated
follow-up when appropriate. Reassess before any rerun; stop and report
ambiguous survivors instead of chasing a percentage or repeating equivalent
work.

Do not change production code solely to kill mutants unless evidence proves the
survivor unkillable by an appropriate test. If existing caller authority
permits a narrowly scoped production change under that exception, record its
technical justification and scope; a newly discovered correctness defect is not
required. This exception does not authorize unrelated refactoring or weaken a
quality gate.

## Report and preserve boundaries

Report whether the work was assessed, executed, repaired, deferred, or not
attempted; the target and source/test mapping; exact commands and conditions;
preflight and conventional-test results; valid report paths and supported
scores; significant survivors; failures or gaps; and remaining work. Keep
private details and credentials out of public output under the destination's
disclosure policy. Preserve existing source, tests, reports, user changes, and
caller-owned task records unless an authorized remediation changes a named
target.

Keep this skill separate from ordinary test improvement, build-failure repair,
test strategy, QA approval, PR or branch management, and workflow audit state.
If required local instructions, bindings, or report evidence are unavailable,
report the preparation gap rather than invent a command, target, score, or
successful execution.
