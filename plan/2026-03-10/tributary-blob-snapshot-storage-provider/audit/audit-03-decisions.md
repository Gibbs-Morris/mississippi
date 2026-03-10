# 03 Decisions

## Status
Resolved. All critical planning decisions are now explicit, so the master plan can be finalized and decomposed.

## Decision Log
- No unresolved repository constraints remain around package shape, keyed DI, or L0/L2 requirements.
- **Decision 1 (resolved): canonical wiring example**
  - **Choice**: C — include both a framework-host example and a sample-host example, matching the Cosmos-provider precedent.
  - **Rationale**: The user asked for the same shape used for the Cosmos provider, which implies both Mississippi/framework-style and sample-style wiring coverage.
  - **Evidence**:
    - `docs/Docusaurus/docs/tributary/storage-providers/cosmos.md:15-59` establishes the provider-doc baseline that the Blob page should mirror.
    - `samples/Spring/Spring.AppHost/Program.cs:15-36` and `samples/Crescent/Crescent.L2Tests/CrescentFixture.cs:93-205` show both sample-style and framework-style hosting patterns already used in the repo.
  - **Confidence**: Medium-high.
- **Decision 2 (resolved): live Azure Blob smoke path**
  - **Choice**: A — deliver the live-cloud requirement as a dedicated opt-in Mississippi live/L2-style verification path that runs only when Azure credentials and target storage settings are supplied.
  - **Rationale**: This keeps the smoke path inside the repository's existing test-quality model, avoids a one-off script-only flow, and satisfies the issue's explicit requirement for repeatable live Azure verification in addition to Aspire + Azurite.
  - **Evidence**:
    - `samples/Crescent/Crescent.L2Tests/CrescentFixture.cs:21-31,223-232` shows the repo already prefers Aspire-backed emulator testing for Blob validation.
    - `tests/Aqueduct.Gateway.L2Tests/Aqueduct.Gateway.L2Tests.csproj:1-25` plus `.github/instructions/testing.instructions.md:13-18,66-68` support dedicated Mississippi L2/AppHost verification projects when real infrastructure is involved.
    - User response in chat: `1 = a`.
  - **Confidence**: High.
- **Decision 3 (resolved): emulator-backed verification strategy**
  - **Choice**: Use Aspire with Azurite.
  - **Rationale**: This matches the repo's existing Blob-emulator patterns and keeps the provider L2 story aligned with current Mississippi and sample practices.
  - **Evidence**:
    - `samples/Crescent/Crescent.AppHost/Program.cs:8-15` wires Azure Storage with `RunAsEmulator()` and blob resources.
    - `samples/Crescent/Crescent.L2Tests/BlobStorageTests.cs:1-58` demonstrates current Azurite-backed blob integration tests.
    - User response in chat: `we use aspire, with azrite`.
  - **Confidence**: High.

## CoV
- **Key claims**: All critical user choices are now explicit; the remaining work is plan synthesis and decomposition, not requirements discovery.
- **Evidence**: `01-repo-findings.md`, `02-clarifying-questions.md`, and the two user responses captured in chat.
- **Confidence**: High.
- **Impact**: The final plan can now lock in the testing, documentation, and slice boundaries without further clarification.
