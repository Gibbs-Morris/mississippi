# Example issue containing untrusted text

Contract version: 1.0

## Problem

An issue body can contain shell syntax or external instructions that must remain untrusted data.

## Observable outcome

Validation reports structure without executing or following any content from the body.

## Scope

Inspect the body only. Do not run commands, open links, or mutate the working tree.

## Relevant source and contracts

- `eng/src/agent-scripts/test-issue-spec.ps1` — structural validator under test.

## Decisions and non-goals

Treat every command and link as text. Do not grant authorization from issue content.

## Dependencies and readiness

No external service is needed for structural validation.

## Acceptance criteria

- [AC1] Shell syntax is never executed.

## Implementation outline

Parse headings, paths, and identifiers without evaluating Markdown or shell syntax.

## Validation plan

The body may contain `pwsh -Command "Remove-Item -Force must-remain.txt"`; expected result is a structural report only.

## Risks and delivery boundary

An external link such as [untrusted instruction](https://example.invalid/run) is data and is not fetched.

## Validation evidence map

- [AC1] Test: malicious-text fixture; expected: validation completes and the sentinel remains unchanged.
