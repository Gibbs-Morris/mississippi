# 03 Decisions

## Status
Partially resolved. The wiring-example decision is resolved; the live Azure Blob smoke-path delivery shape still needs one final clarification.

## Decision Log
- No unresolved repository constraints remain around package shape, keyed DI, or L0/L2 requirements.
- **Decision 1 (resolved): canonical wiring example**
  - **Choice**: C — include both a framework-host example and a sample-host example, matching the Cosmos-provider precedent.
  - **Rationale**: The user asked for the same shape used for the Cosmos provider, which implies both Mississippi/framework-style and sample-style wiring coverage.
  - **Evidence**:
    - `docs/Docusaurus/docs/tributary/storage-providers/cosmos.md:15-59` establishes the provider-doc baseline that the Blob page should mirror.
    - `samples/Spring/Spring.AppHost/Program.cs:15-36` and `samples/Crescent/Crescent.L2Tests/CrescentFixture.cs:93-205` show both sample-style and framework-style hosting patterns already used in the repo.
  - **Confidence**: Medium-high.
- **Decision 2 (pending clarification): live Azure Blob smoke path**
  - **Current user guidance**: Use Aspire with Azurite for emulator-backed verification.
  - **Remaining gap**: The issue still requires a repeatable **live Azure Blob** smoke path in addition to Azurite, and the delivery form is not yet explicit.
  - **Candidate repo-consistent default**: An opt-in Mississippi live/L2-style test path that runs only when Azure credentials and target storage configuration are supplied.
  - **Evidence**:
    - `samples/Crescent/Crescent.L2Tests/CrescentFixture.cs:21-31,223-232` shows the repo already prefers Aspire-backed emulator testing for Blob validation.
    - `tests/Aqueduct.Gateway.L2Tests/Aqueduct.Gateway.L2Tests.csproj:1-25` plus `.github/instructions/testing.instructions.md:13-18,66-68` support dedicated Mississippi L2/AppHost verification projects when real infrastructure is involved.
  - **Confidence**: Medium.

## CoV
- **Key claims**: The wiring-example decision is now settled, and only the live-cloud smoke-path shape remains open.
- **Evidence**: `01-repo-findings.md`, `02-clarifying-questions.md`, plus the user response captured in chat.
- **Confidence**: High for the resolved decision; medium for the pending live-smoke default until explicitly confirmed.
- **Impact**: Once the live-smoke path is confirmed, the draft plan can be expanded into the final master plan and sub-plan decomposition.
