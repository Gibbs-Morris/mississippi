# Verification

## Claim List
1. Generators can emit AddSpringDomain wrappers for Spring client/server/silo without new public APIs.
2. Generated wrappers can include all required Spring registrations (aggregates, projections, sagas, mappers).
3. Manual SpringDomain*Registrations files can be deleted without changing Program.cs usage.
4. Generator scoping can limit output to Spring sample projects.
5. PendingSourceGenerator usage can be removed from Spring registrations once generated.
