# Implementation Plan

## Outline

1. Inventory existing registration surfaces, builder types, and legacy entry points across src/ and tests/.
2. Identify standalone-capable packages and current native builder registrations.
3. Define a shared builder base interface with ConfigureServices and ConfigureOptions.
4. Refactor Reservoir and Aqueduct registration to builder-first patterns; remove legacy registration paths.
5. Standardize options wiring on builder surfaces.
6. Update tests to use builder-first registrations and add coverage.
7. Update docs and instruction files; add migration guidance.
8. Run build/cleanup/tests and mutation testing as required.
