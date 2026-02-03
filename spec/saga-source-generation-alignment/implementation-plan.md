# Implementation plan

## Initial outline
1. Inventory saga DTO/mapper/reducer patterns and aggregate/projection generator patterns.
2. Assess generic attribute support with repo target frameworks and language version.
3. Design saga source generation changes aligned to existing generator conventions.
4. Implement generator updates and migrate manual saga DTO/mapper/reducers.
5. Add/update tests and validation.

## Assumptions
- Generator additions can target the same test projects used for aggregate/projection generators.
- Samples can be updated to consume generated saga DTOs/mappers without functional changes.

## Unknowns
- Required changes in sample projects or build settings.
- Whether any existing saga reducer patterns are not suitable for generation.
