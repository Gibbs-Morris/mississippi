# Amendment 3 Review — Event Sourcing & CQRS Specialist

## Persona

Event Sourcing & CQRS Specialist — event schema evolution, storage-name immutability, reducer purity, aggregate invariant enforcement, projection rebuild-ability, snapshot versioning, command/event separation discipline, idempotency, and saga compensation correctness.

## Findings

### 1. GAP — vfe-repo-policy-compliance doesn't explicitly mention storage-name immutability

- **Issue**: The repo has a critical rule: `[EventStorageName]` and `[SnapshotStorageName]` attribute values must never change once data has been persisted. The `vfe-repo-policy-compliance` specialist's remit says "repo instructions, local conventions, policy enforcement" which should cover this, but the remit description doesn't call out storage names specifically. Given how critical this rule is (it's a data-integrity concern, not just a convention), it warrants explicit mention.
- **Why it matters**: If the repo-policy specialist doesn't flag a storage-name change, it could cause data loss in production.
- **Proposed change**: Add to the `vfe-repo-policy-compliance` remit description: "including persisted-contract exceptions such as storage-name immutability when policy requires them." Or add a note in the specialist's remit map entry.
- **Evidence**: `.github/instructions/backwards-compatibility.instructions.md` says storage names are immutable once persisted. `.github/instructions/storage-type-naming.instructions.md` exists. Remit map for `vfe-repo-policy-compliance` says "repo instructions, local conventions, policy enforcement" without specific mention.
- **Confidence**: Medium — the general remit covers it, but explicitness reduces risk.

### 2. OBSERVATION — The plan correctly scopes event-sourcing concerns to the specialists, not the plan itself

- **Issue**: None. The plan is about agent files, not event-sourcing code. The specialist remit map correctly assigns event-sourcing concerns to the relevant specialists.
- **Proposed change**: None.
- **Confidence**: High.

### 3. MINOR — vfe-data-architect remit overlaps with event-sourcing concerns

- **Issue**: `vfe-data-architect` covers "schema evolution, data lifecycle, retention, contract and store implications" which overlaps significantly with event-sourcing concerns like event schema evolution and snapshot versioning.
- **Why it matters**: When both specialists review event-sourcing code, they'll produce overlapping findings.
- **Proposed change**: This is acceptable — overlapping coverage with independent adjudication is better than gaps. The adjudication step handles deduplication. No change needed but worth noting.
- **Evidence**: Remit map for `vfe-data-architect` and the event-sourcing concerns it covers.
- **Confidence**: High.
