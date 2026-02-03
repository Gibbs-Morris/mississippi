# Verification

## Claim List

1. Spring sample includes a saga-driven money transfer flow between accounts.
2. Saga endpoints and registrations are generated and wired into Spring.Server and Spring.Silo.
3. Spring.Client exposes UI actions that start a transfer saga and display progress/status.
4. New saga-related code in src and samples has L0 unit tests with 100% coverage.
5. All new/modified src code in this branch has 100% unit test coverage.
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

1. The Spring.Client Index page provides open/deposit/withdraw flows and projections, with no transfer saga UI present. (EVIDENCE: samples/Spring/Spring.Client/Pages/Index.razor, samples/Spring/Spring.Client/Pages/Index.razor.cs)
2. No saga state types are defined in Spring sample; no GenerateSagaEndpoints usage appears under samples/Spring. (EVIDENCE: grep GenerateSagaEndpoints in samples/)
3. Spring.Silo registers aggregates/projections via generated Add{Aggregate}/Add{Projection} extension methods in Program.cs; Spring.Server registers aggregate/projection mappers similarly. (EVIDENCE: samples/Spring/Spring.Silo/Program.cs, samples/Spring/Spring.Server/Program.cs)
4. Existing projections are BankAccountBalance, BankAccountLedger, and FlaggedTransactions; these are current UI data sources. (EVIDENCE: samples/Spring/Spring.Domain/Projections/)
5. Spring.Domain.L0Tests contains handler/reducer tests for bank account operations; additional saga tests will need to follow these patterns. (EVIDENCE: samples/Spring/Spring.Domain.L0Tests/Aggregates/BankAccount/Handlers/WithdrawFundsHandlerTests.cs)
6. Saga runtime code has L0 tests under tests/EventSourcing.Sagas.L0Tests, including StartSagaCommandHandlerTests and SagaInfrastructureReducerTests. (EVIDENCE: tests/EventSourcing.Sagas.L0Tests/)
7. Saga state and step requirements are defined by ISagaState, ISagaStep<TSaga>, SagaStepAttribute, and GenerateSagaEndpointsAttribute. (EVIDENCE: src/EventSourcing.Sagas.Abstractions/ISagaState.cs, src/EventSourcing.Sagas.Abstractions/ISagaStep.cs, src/EventSourcing.Sagas.Abstractions/SagaStepAttribute.cs, src/Inlet.Generators.Abstractions/GenerateSagaEndpointsAttribute.cs)
8. Saga client features are generated with Add{Saga}SagaFeature extension methods that register mappers, reducers, and action effects via Reservoir. (EVIDENCE: src/Inlet.Client.Generators/SagaClientRegistrationGenerator.cs)
9. Unit test coverage tooling uses test-project-quality.ps1 with XPlat Code Coverage output and Cobertura parsing. (EVIDENCE: eng/src/agent-scripts/test-project-quality.ps1)
10. Coverage gaps across all src projects are not yet measured; requires running coverage scripts per project. (UNVERIFIED)
11. Zero-warning and cleanup gates are enforced by shared policies and copilot instructions; formatting warnings require running clean-up.ps1. (EVIDENCE: .github/instructions/shared-policies.instructions.md, .github/copilot‑instructions.md)
12. Updating the Spring sample requires updating mississippi-framework.instructions.md when new patterns are added. (EVIDENCE: .github/instructions/mississippi-framework.instructions.md)
