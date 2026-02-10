# Spring Sample Registrations Spec

## Status
- Status: Draft
- Size: Medium
- Approval checkpoint: No (sample-only registrations; no public API change expected)

## Requirements
- Add Spring sample registration entrypoints for client/server/silo SDK types.
- Use a scalable naming pattern like AddSpringDomain for each SDK type.
- Ensure registration includes all Inlet source-generated registrations for Spring domain.
- Tag registration classes with the pending source gen attribute.

## Constraints
- Samples only; avoid changes to framework public APIs unless required by registration discovery.
- Follow ConfigureServices-only pattern (no public IServiceCollection exposure).
- Keep registrations minimal and source-gen friendly.

## Assumptions
- Spring sample has separate client/server/silo domain projects.
- Inlet source generation produces discoverable registration hooks today.

## Unknowns
- Exact source generator attribute name and location.
- Existing sample registration patterns for other domains.
- Where SDK source generator discovery integrates with Spring sample projects.

## Index
- learned.md
- rfc.md
- verification.md
- implementation-plan.md
- progress.md
