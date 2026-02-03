# Verification

## Claim List

1. Spring sample includes a saga-driven money transfer flow between accounts.
2. Saga endpoints and registrations are generated and wired into Spring.Server and Spring.Silo.
3. Spring.Client exposes UI actions that start a transfer saga and display progress/status.
4. New saga-related code in src and samples has L0 unit tests with 100% coverage.
5. Overall unit test coverage for all src projects on this branch is 100%.
6. Mississippi build and cleanup scripts run cleanly with zero warnings after changes.

## Questions

1. Where in Spring.Client is money movement currently surfaced, and what is the existing UX flow? (UI entry point)
2. Does Spring.Domain already contain any saga state or saga step types? If so, where and how are they registered?
3. What registration patterns do Spring.Silo and Spring.Server use for generated aggregate endpoints and effects?
4. Are there existing sample projections relevant to bank accounts that should reflect transfer status?
5. What is the current L0 test coverage in Spring.Domain.L0Tests for bank account commands and reducers?
6. Are there existing tests for saga runtime code in src/EventSourcing.Sagas and their coverage status?
7. What are the required attributes and naming conventions for saga state and steps in src and samples?
8. How are generated saga client actions/state expected to be wired into Reservoir features and UI?
9. What is the current test/coverage tooling and target for unit tests across src projects?
10. Which src projects currently lack full coverage, and what gaps remain to reach 100%?
11. Are there any analyzer rules or build gates that would block adding new sample domain types or UI components?
12. Does updating the Spring sample require updates to Mississippi framework instruction docs (per policy)?

## Answers

- TBD
