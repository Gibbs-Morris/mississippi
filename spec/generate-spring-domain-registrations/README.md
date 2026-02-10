# Generate Spring Domain Registrations Spec

## Status
- Status: Draft
- Size: Medium (initial)
- Approval checkpoint: TBD

## Requirements
- Generate AddSpringDomain wrappers for Spring client/server/silo via source generators.
- Remove manual SpringDomain*Registrations classes.
- Keep Program.cs usage of AddSpringDomain unchanged.
- Ensure generated wrappers include all required Spring registrations.

## Constraints
- Follow sample rules: domain logic stays in Spring.Domain; hosts remain thin.
- Keep generator output scoped to Spring sample projects.
- Do not introduce new public framework APIs unless required for generator pipeline.

## Assumptions
- Spring projects already run Inlet generators as analyzers.
- Generated registration methods exist for Spring aggregates, projections, sagas, and mappers.

## Unknowns
- Best scoping mechanism for Spring-only generator output.
- Generator naming conventions for domain-level wrappers.
- Whether any additional generated registrations are required beyond current manual wrappers.

## Index
- learned.md
- rfc.md
- verification.md
- implementation-plan.md
- progress.md
