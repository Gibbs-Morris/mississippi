# 04 Draft Plan

## Status
Draft planning is waiting on the outstanding user decisions captured in `02-clarifying-questions.md`.

## Current shape
The repository evidence already supports a likely decomposition into:
1. Blob provider package scaffolding and options/registration contract
2. Core blob persistence behavior with parity logging, metrics, pathing, conflict handling, pruning, and optional compression
3. Mississippi-owned L0 and Azurite-backed L2 verification
4. Live Azure smoke coverage plus Docusaurus and wiring documentation

## CoV
- **Key claims**: The draft shape follows repository conventions and the issue acceptance criteria.
- **Evidence**: `00-intake.md`, `01-repo-findings.md`, `02-clarifying-questions.md`.
- **Confidence**: Medium until the remaining user decisions are resolved.
- **Impact**: Once the answers land, this file can expand into the complete master plan and sub-plan decomposition.
