# Verification

## Claim list
1. Saga DTOs and mappers are generated to match aggregate/projection patterns.
2. Saga reducer generation covers the currently hand-authored reducers without behavior changes.
3. Generic attributes are supported by the current target framework (or a documented decision is made not to use them).
4. Generated output matches existing manual code behavior and signatures.

## Questions
1. Where are `SagaPhaseDto` and `SagaPhaseDtoMapper` defined and used?
2. What generators/patterns are used for aggregate and projection DTOs/mappers/reducers?
3. What saga reducers exist today and what repeated patterns can be generated?
4. What target frameworks and language versions are used for generators and consuming projects?
5. Does the current toolchain support generic attributes in C# for this repo?
6. What would be the generated API surface for saga DTOs/mappers/reducers, and does it match current usage?
7. Do sample apps compile with generated saga DTOs/mappers/reducers?
8. Are any public API changes introduced by replacing manual code with generated equivalents?

## Answers
- TBD.
