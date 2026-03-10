# Review 12: Data Integrity And Storage Engineer

- Issue: The docs sidecar and review prompts should explicitly capture data-contract and migration notes whenever persistence, schema, or API contracts change. Why it matters: those impacts are easy to forget when docs are deferred until the end. Proposed change: strengthen the docs-sidecar definition to mention data contracts, schema changes, migration notes, and caveats explicitly. Evidence: user requirements already call for migration notes and caveats; this repository is contract-heavy. Confidence: High.
- Issue: The data-architect specialist should explicitly own lifecycle and retention implications, not just schema shape. Why it matters: enterprise delivery frequently changes deletion, retention, or archival semantics without large schema changes. Proposed change: mention retention, lifecycle, and ownership boundaries in the data-architect prompt body plan. Evidence: requested remit includes lifecycle, but the draft plan could make it more explicit in the specialist template guidance. Confidence: Medium.

## CoV

- Claim: data-contract documentation belongs in the rolling sidecar, not just the final review. Evidence: user explicitly asked for ongoing docs tracking. Confidence: High.