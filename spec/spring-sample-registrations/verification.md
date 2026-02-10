# Verification

## Claim List
1. Spring samples can be registered via a single AddSpringDomain method per SDK type.
2. Each registration method can include all Inlet-generated registrations without manual aggregate lists.
3. A pending source generation attribute exists and is appropriate for these registration classes.
4. The registration classes can live in Spring sample domain projects without violating layering rules.
5. The SDK source generator can emit or discover these registrations for downstream usage.

## Verification Questions
1. Where are the Spring sample projects for client/server/silo, and which assemblies represent the Spring domain?
2. Are there existing registration entrypoints for Spring (or other samples) that we should mirror?
3. What is the exact name and namespace of the pending source gen attribute used today?
4. Which projects currently use the pending source gen attribute, and how is it applied?
5. Where does Inlet source generation run, and what registrations or partials does it emit for Spring?
6. How are generated registrations currently discovered or invoked by SDKs (client/server/silo)?
7. Is there a standard naming convention for domain registration extensions (e.g., AddXDomain) in samples?
8. Do any existing sample registration classes aggregate multiple features without manual lists?
9. Are there constraints in the samples instructions that limit where registration classes should live?
10. Do the Spring samples require both client and server registrations in the same assembly or separate ones?
11. What is the minimal set of registrations required for Spring to run end-to-end today?
12. Are there tests or docs that reference manual aggregate listing that must be updated?
