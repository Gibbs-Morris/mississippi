# Implementation Plan

## Initial Outline
1. Inventory builder interfaces, implementations, and extensions using `.Services`.
2. Remove `Services` from public builder interfaces.
3. Update builder implementations to keep service collection private and only expose ConfigureServices.
4. Refactor all builder extension methods and registrations to use ConfigureServices.
5. Update docs/samples/tests/generators to avoid `.Services` usage.
6. Build and run tests per repo guidance.

## Test Plan (placeholder)
- Build Mississippi and Samples solutions with repo scripts.
- Run unit tests (L0/L1) using repo scripts.
