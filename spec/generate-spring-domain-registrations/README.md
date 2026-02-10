# Generate Spring Domain Registrations Spec

## Status
- Status: Draft
- Size: Medium
- Approval checkpoint: No (generator output for samples only)

## Requirements
- Generate Add{Product}Domain wrappers for client/server/silo via source generators.
- Remove manual SpringDomain*Registrations classes.
- Keep Program.cs usage of AddSpringDomain unchanged in Spring.
- Ensure generated wrappers include all required registrations for each SDK type.

## Constraints
- Follow sample rules: domain logic stays in Spring.Domain; hosts remain thin.
- Keep generator output scoped to SDK projects by target root namespace.
- Do not introduce new public framework APIs unless required for generator pipeline.

## Assumptions
- Spring projects already run Inlet generators as analyzers.
- Generated registration methods exist for Spring aggregates, projections, sagas, and mappers.

## Unknowns
- Best scoping mechanism for SDK-only generator output.
- Generator naming conventions for domain-level wrappers.
- Whether any additional generated registrations are required beyond current Spring wrappers.

## Index
- learned.md
- rfc.md
- verification.md
- implementation-plan.md
- progress.md
