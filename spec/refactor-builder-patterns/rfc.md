# RFC: Refactor Builder Patterns

## Problem
Builder chaining uses terminal methods (for example, `Done()`) that do not feel idiomatic in .NET. The user wants the pattern refactored to align with Microsoft .NET builder conventions.

## Goals
- Align builder APIs with common .NET builder patterns used in Microsoft libraries.
- Remove or replace non-idiomatic terminal methods.
- Update consumers (tests, samples) accordingly.

## Non-goals
- Add new features unrelated to builder flow.
- Introduce new external dependencies.

## Current State
- UNVERIFIED: Feature builders return to parent via `Done()`.
- UNVERIFIED: Some registrations use builder chaining instead of configure lambdas.

## Proposed Design
- Replace terminal `Done()` pattern with a configure-lambda pattern, or a more idiomatic alternative validated by verification.
- Ensure builder return types remain fluent and consistent across modules (client/server/silo/features).

## Alternatives
- Rename `Done()` to `Build()` or `Finish()` while keeping return-to-parent pattern.
- Keep current pattern and document it more clearly.

## Security
No new security surface expected; public API changes must be validated.

## Observability
No new logging required.

## Compatibility / Migrations
- Public API changes will require downstream updates.
- Update tests and samples to new patterns.

## Risks
- Widespread API changes could introduce breaking compilation changes.
- Inconsistent patterns across builder types if refactor is partial.
