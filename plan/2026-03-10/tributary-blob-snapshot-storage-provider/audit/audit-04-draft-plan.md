# 04 Draft Plan

## Status
Decision-complete. The draft can now be promoted into the final master plan and decomposed into implementation-ready sub-plans.

## Current shape
The repository evidence supports a four-slice implementation sequence:
1. Blob provider package, repository, provider facade, pathing, options, hosted initialization, and L0 coverage
2. Mississippi-owned Aspire + Azurite L2/AppHost verification
3. Dedicated opt-in live Azure Blob smoke coverage inside the Mississippi L2 verification surface
4. Docusaurus documentation plus both framework-host and sample-host wiring examples

## Confirmed direction
- Emulator-backed verification will use Aspire with Azurite, following the current repo storage-emulator patterns.
- Documentation and validation will cover both framework-host and sample-host wiring examples, consistent with the Cosmos-provider baseline.
- The repeatable live Azure Blob smoke requirement will be satisfied by an opt-in live/L2-style verification path that activates only when Azure configuration is supplied.
- No reusable instruction gaps were identified; the existing keyed-services, testing, Aspire, and documentation instructions already cover the required patterns.

## CoV
- **Key claims**: The final shape follows repository conventions and now reflects fully-resolved user intent.
- **Evidence**: `00-intake.md`, `01-repo-findings.md`, `02-clarifying-questions.md`, `03-decisions.md`, `samples/Crescent/Crescent.AppHost/Program.cs`, and the user responses captured in chat.
- **Confidence**: High.
- **Impact**: The master plan can move directly into finalization, review, and decomposition.
