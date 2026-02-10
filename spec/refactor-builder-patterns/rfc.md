# RFC: Refactor Builder Patterns

## Problem
Builder chaining uses terminal methods (for example, `Done()`) that do not feel idiomatic in .NET. The user wants the pattern refactored to align with Microsoft .NET builder conventions.

## Goals
- Align builder APIs with common .NET builder patterns used in Microsoft libraries.
- Remove or replace non-idiomatic terminal methods.
- Update consumers (tests, samples) accordingly.
- Keep fluent usage ergonomic and consistent across client/server/silo/feature builders.

## Non-goals
- Add new features unrelated to builder flow.
- Introduce new external dependencies.
- Change behavior of registrations beyond the builder API surface.

## Current State
- UNVERIFIED: Feature builders return to parent via `Done()`.
- UNVERIFIED: Some registrations use builder chaining instead of configure lambdas.
- UNVERIFIED: Existing tests and generators rely on specific chaining patterns.

## Proposed Design
- Preferred hypothesis: move to configure-lambda patterns (for example `AddFeature<T>(Action<IReservoirFeatureBuilder<T>> configure)`), and remove the explicit return-to-parent method where practical.
- Alternative if configure-lambda is not feasible everywhere: replace terminal methods with a name aligned to Microsoft patterns (for example, remove `Done()` in favor of returning the same builder and keep nesting shallow).

## Alternatives
- Rename `Done()` to `Build()` or `Finish()` while keeping return-to-parent pattern.
- Keep current pattern and document it more clearly.
- Introduce a separate builder type with `Build()` that materializes a registry (requires larger changes).

## Security
No new security surface expected; public API changes must be validated.

## Observability
No new logging required.

## Compatibility / Migrations
- Public API changes will require downstream updates.
- Update tests and samples to new patterns.
- Update generator outputs to emit the new pattern.

## Risks
- Widespread API changes could introduce breaking compilation changes.
- Inconsistent patterns across builder types if refactor is partial.
