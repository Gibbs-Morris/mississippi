# Verification

## Claim List
1. Generators can emit AddSpringDomain wrappers for Spring client/server/silo without new public APIs.
2. Generated wrappers can include all required Spring registrations (aggregates, projections, sagas, mappers).
3. Manual SpringDomain*Registrations files can be deleted without changing Program.cs usage.
4. Generator scoping can limit output to Spring sample projects.
5. PendingSourceGenerator usage can be removed from Spring registrations once generated.

## Verification Questions
1. Where are the current SpringDomain*Registrations classes and which generated methods do they call?
2. Which generator projects are responsible for emitting the underlying Add* methods used by Spring today?
3. What options exist in the generator pipeline to scope output to specific projects (e.g., analyzer config, target root namespace)?
4. Are there existing patterns in generators for emitting cross-aggregate wrapper methods?
5. What namespaces do generated registrations land in for Spring client/server/silo?
6. Is PendingSourceGenerator required for generator discovery, or can it be removed safely once generated?
7. Do any other samples rely on AddSpringDomain-style wrappers that could be impacted by generator changes?
8. Are Spring program files already using AddSpringDomain in client/server/silo?
