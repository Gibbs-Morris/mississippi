# Verification

## Claim List

- Builder-first registration will be the default across Mississippi, Reservoir, and Aqueduct.
- Native builder registration will exist only for standalone-capable packages.
- IServiceCollection registration will be retained only as a documented fallback.
- Reservoir registration will be refactored to a store/feature builder model.
- Options configuration will be standardized on builder surfaces.
- Legacy registration entry points will be removed after builder-first replacements exist.
- Public contracts will live in Abstractions projects; implementations in main projects.
- Docs and instruction files will reflect the new builder-first conventions and migration guidance.
- Tests will be updated to use builder-first registration patterns and pass.

## Questions

1. Which packages expose current builder surfaces, and what are their entry point types and namespaces?
2. Where are legacy registration entry points defined, and which ones are still referenced by tests or docs?
3. Which public registration APIs currently accept IServiceCollection directly?
4. Which packages are standalone-capable and already support native builder registration?
5. Are any event-sourcing-related packages exposing native builder registration that should be constrained to Mississippi builders?
6. Is there any shared builder base interface today, or are there multiple ad hoc builder patterns?
7. Where do options get configured today, and how many registrations bypass builder patterns?
8. Which Reservoir registrations exist (store, features, devtools, Blazor) and how are they surfaced?
9. Which Aqueduct registrations exist (server/client/backplane) and how are they surfaced?
10. Are any registration classes currently located in Abstractions projects or leaking implementations into Abstractions?
11. Are there explicit tests covering builder registration, and do they rely on legacy APIs?
12. Which tests will fail if legacy registrations are removed (or signatures change)?
13. Which docs pages mention IServiceCollection-first or legacy registration paths?
14. What migration guidance already exists, if any, for registration changes?
15. Are extension points for storage providers, snapshots, and sagas documented and registered via builders today?
16. What is the minimal set of builder APIs to cover Mississippi, Reservoir, and Aqueduct consistently?
17. Are there internal or public DI registration extension methods that will become unused after builder-first changes?
18. Are there any usage examples in README or docs that must change to builder-first?
19. Are there any breaking public API usages in samples or external-facing docs that require explicit migration notes?
20. Are there CI or script constraints that must be considered for build/test validation of these changes?

## Answers

UNVERIFIED
