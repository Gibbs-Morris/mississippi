# Example bug-fix issue

Contract version: 1.0

## Problem

The input parser accepts an empty identifier and later produces an unhelpful storage error.

## Observable outcome

Empty identifiers are rejected with a stable validation message before storage is called.

## Scope

Update the parser and its focused tests. Do not change storage naming or unrelated validation.

## Relevant source and contracts

- `.github/instructions/issue-tracking.instructions.md` — issue and evidence traceability rules.
- `README.md` — public validation and test entry points.

## Decisions and non-goals

Keep the existing exception type. Do not add a compatibility wrapper or change persisted names.

## Dependencies and readiness

The parser project and its L0 test project are available in the current solution.

## Acceptance criteria

- [AC1] Empty identifiers fail before the storage dependency is invoked.
- [AC2] Nonempty identifiers retain the existing parsed value.
- [AC3] The focused test run executes and passes at least one test.

## Implementation outline

Add the guard at the parser boundary, preserve the valid path, and add tests for both branches.

## Validation plan

Run the focused L0 test project and a warning-as-error build. Inspect the test result and compiler output.

## Risks and delivery boundary

The change is limited to validation behavior and its tests. Merge-ready means review and CI are complete; it is not merged until authorized.

## Validation evidence map

- [AC1] Test: parser invalid-input test; expected: validation error and no storage call.
- [AC2] Test: parser valid-input test; expected: original identifier is returned.
- [AC3] Command: focused test command; expected: nonempty passing test result.
