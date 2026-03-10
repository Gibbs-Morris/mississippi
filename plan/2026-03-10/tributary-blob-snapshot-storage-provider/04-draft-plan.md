# 04 Draft Plan

## Status
Draft planning is waiting on the final live Azure Blob smoke-path decision. The wiring-example decision is resolved: the final plan will include both framework-host and sample-host wiring coverage, matching the Cosmos-provider precedent.

## Current shape
The repository evidence already supports a likely decomposition into:
1. Blob provider package scaffolding and options/registration contract
2. Core blob persistence behavior with parity logging, metrics, pathing, conflict handling, pruning, and optional compression
3. Mississippi-owned L0 and Azurite-backed L2 verification
4. Live Azure smoke coverage plus Docusaurus and wiring documentation

## Confirmed direction
- Emulator-backed verification will use Aspire with Azurite, following the current repo storage-emulator patterns.
- Documentation and validation will cover both framework-host and sample-host wiring examples, consistent with the Cosmos-provider baseline.

## CoV
- **Key claims**: The draft shape follows repository conventions and the issue acceptance criteria.
- **Evidence**: `00-intake.md`, `01-repo-findings.md`, `02-clarifying-questions.md`, `03-decisions.md`, and the user response captured in chat.
- **Confidence**: Medium-high; only the live-cloud smoke-path delivery form remains unresolved.
- **Impact**: Once that last decision lands, this file can expand into the complete master plan and sub-plan decomposition.
