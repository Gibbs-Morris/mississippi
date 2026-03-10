# Amendment 3 Review — Synthesis

## Deduplicated Findings

All findings from the 12 persona reviews have been collected, deduplicated, and categorized below. Where multiple reviewers raised the same issue, the strongest formulation is kept and sources are noted.

---

### Must-Accept (blocking flaws or critical gaps)

#### M1 — No error recovery or failure protocol

- **Sources**: Principal Engineer (#1)
- **Issue**: The plan describes only the happy path. No guidance for repeated build failure, specialist remit violation, missing artifacts, or non-converging review loops.
- **Accept/Reject**: **Accept.** This is a genuine gap. An implementation-grade plan must define failure behavior.
- **Required edit**: Add a "Failure and Recovery Protocol" section with: (1) build failure cap at 5 attempts per issue (consistent with repo policy), (2) specialist remit violation → discard directives, log observation, (3) missing artifacts → attempt reconstruction or ask user, (4) non-converging review loops → cap at 3 cycles then escalate.

#### M2 — No explicit plan-approval status marker

- **Sources**: Solution Engineering (#2)
- **Issue**: VFE Build says "read approved plan artifacts" but nothing in the working directory distinguishes an approved plan from a draft. Build could start prematurely.
- **Accept/Reject**: **Accept.** Simple fix with high correctness value.
- **Required edit**: Require `01-plan.md` to have a `## Status` line at the top with values `draft | in-review | approved | superseded`. VFE Build must verify status = `approved` before proceeding.

#### M3 — Circular handoff potential with no termination

- **Sources**: Principal Engineer (#2)
- **Issue**: Plan → Build → Review → Plan can cycle indefinitely. The plan-amendment rule compounds the risk.
- **Accept/Reject**: **Accept.** Every loop needs a termination condition.
- **Required edit**: Add a circuit-breaker: "If a task returns to Plan from Build or Review more than twice, the agent must escalate to the user with a summary of what's blocking convergence."

#### M4 — Concurrent specialist writes to shared files

- **Sources**: Distributed Systems (#1)
- **Issue**: Parallel specialist rounds could write to shared files (`08-decisions.md`, `09-handoff.md`) simultaneously.
- **Accept/Reject**: **Accept.** Classic race condition.
- **Required edit**: Add to Working Directory Contract: "During parallel specialist rounds, specialists write only to their own `10-specialist-<name>.md` files. Only the coordinating entry agent writes to shared files after collecting specialist outputs."

#### M5 — Working directory sensitive-information guardrail missing

- **Sources**: Security (#2)
- **Issue**: Intake notes, decisions, and PR comments could contain security-sensitive details (credentials, threat models, auth details). No guardrail prevents recording them.
- **Accept/Reject**: **Accept.** Follows the pattern from `.scratchpad` instructions ("Secrets/PII MUST NOT live in .scratchpad/").
- **Required edit**: Add to Working Directory Contract: "Agents must not record credentials, secrets, tokens, connection strings, or detailed threat-model vulnerabilities in working-directory files. Record only the decision outcome and a sanitized reference."

#### M6 — Specialist conflict resolution priority undefined

- **Sources**: Principal Engineer (#3)
- **Issue**: When specialists disagree, the adjudicator has no decision framework.
- **Accept/Reject**: **Accept.** Needs a clear priority order.
- **Required edit**: Add to Internal Specialist Requirements: "When specialist findings conflict, prioritize: (1) correctness and data integrity, (2) security, (3) repo policy compliance, (4) operability, (5) performance, (6) developer experience. Record the conflict and resolution in `08-decisions.md`."

#### M7 — Plan-amendment materiality threshold undefined

- **Sources**: Principal Engineer (#5)
- **Issue**: "Any material change" triggers full review rerun. Material is undefined.
- **Accept/Reject**: **Accept.** Without a threshold, the rule is either over- or under-triggered.
- **Required edit**: Define: "A change is material if it affects architecture, public contracts, slice scope, acceptance criteria, or NFRs. Wording clarifications, typo fixes, and reordering within a slice are not material."

#### M8 — No triviality threshold for working directory creation

- **Sources**: Solution Engineering (#1), Developer Experience (#3)
- **Issue**: "Every non-trivial vfe workflow" with no definition of trivial. Small tasks don't need 10 files.
- **Accept/Reject**: **Accept.** Critical for adoption.
- **Required edit**: Define: "A task is trivial if it can complete in a single turn with no specialist invocation and no handoff. For trivial tasks, working-directory creation is optional — the entry agent should note this decision. For non-trivial tasks, the working directory is required."

---

### Should-Accept (important improvements)

#### S1 — No resumption protocol for interrupted workflows

- **Sources**: Solution Engineering (#3), Platform Engineer (#4), Developer Experience (#1)
- **Issue**: No guidance for reconstructing state when returning to a partially-completed workflow.
- **Accept/Reject**: **Accept.** The durability promise requires a resumption story.
- **Required edit**: Add to Working Directory Contract: "When an entry agent is invoked and a working directory already exists for the task, it must read `09-handoff.md` first to reconstruct current state, then inspect artifact statuses before proceeding."

#### S2 — `09-handoff.md` should serve as status dashboard

- **Sources**: Platform Engineer (#4), Developer Experience (#1)
- **Issue**: A user inspecting the working directory has no obvious entry point. `09-handoff.md` is numbered ninth but should be the first thing anyone reads.
- **Accept/Reject**: **Accept.** Enhances the handoff file's required format rather than adding a new file.
- **Required edit**: Require `09-handoff.md` to always include at the top: current phase, current blocker (if any), last completed action, which files have the most relevant state, and next expected action.

#### S3 — VFE Plan missing Compliance/Governance specialist

- **Sources**: Technical Architect (#2)
- **Issue**: Planning is when compliance constraints should be surfaced. VFE Plan's default routing omits Compliance/Governance.
- **Accept/Reject**: **Accept.** Compliance blockers found late are expensive.
- **Required edit**: Add `Compliance / Governance` to VFE Plan's default specialist routing.

#### S4 — "Untrusted until corroborated" needs actionable definition

- **Sources**: Security (#1)
- **Issue**: The guardrail is stated but not operationalized.
- **Accept/Reject**: **Accept.** Simple addition with high clarity value.
- **Required edit**: Add to guardrails: "Corroboration means: for code behavior → read the code; for repo policy → verify against instruction files; for external systems → verify against official docs or ask the user; for subjective opinions → treat as adjudication input, not fact."

#### S5 — Entry-agent argument-hint values not specified

- **Sources**: Developer Experience (#4)
- **Issue**: `argument-hint` is allowed but no recommended values are given.
- **Accept/Reject**: **Accept.** Good DX improvement with no cost.
- **Required edit**: Add recommended argument hints. Plan: "Describe the feature or task you want to plan." Build: "Path to plan directory or describe what to implement." Review: "Branch name or describe what to review."

#### S6 — Prompt budget guidance missing

- **Sources**: Performance/Scalability (#1)
- **Issue**: Entry-agent prompts with 3 embedded diagrams plus full template could be very long.
- **Accept/Reject**: **Accept.** Practical guidance prevents prompt bloat.
- **Required edit**: Add: "Entry-agent prompt bodies should target ≤3000 words. Specialist prompts should target ≤1500 words. If embedding all three diagrams exceeds the budget, embed the end-to-end workflow and reference the manifest for the others."

#### S7 — Working directory retention rule is a non-answer

- **Sources**: Platform Engineer (#2)
- **Issue**: "The implementation should make clear when it is expected to remain" doesn't actually define a retention policy.
- **Accept/Reject**: **Accept.** Needs a concrete lifecycle.
- **Required edit**: Replace with: "Working directories remain until the associated branch is merged or closed. After merge, archival or deletion is at the team's discretion."

---

### Could-Accept (minor or nice-to-have)

#### C1 — Manifest philosophy section should explain "verification-first" concretely

- **Sources**: Marketing & Contracts (#1)
- **Accept/Reject**: **Defer to implementation.** The manifest section 1 requirement says "Family name and purpose." The builder can elaborate without a plan change.

#### C2 — Family comparison/decision matrix in manifest

- **Sources**: Marketing & Contracts (#2)
- **Accept/Reject**: **Defer to implementation.** The "When To Use" section already exists in the plan. The builder can include it in the manifest.

#### C3 — Entry-agent descriptions should mention verification philosophy

- **Sources**: Marketing & Contracts (#3)
- **Accept/Reject**: **Accept as a minor edit.** Update the recommended descriptions.

#### C4 — Specialist-to-specialist communication model should be explicit

- **Sources**: Technical Architect (#1)
- **Accept/Reject**: **Accept.** Quick clarification prevents confusion.
- **Required edit**: Add: "Specialists operate independently during a round and do not read each other's findings. Deduplication and conflict resolution are the entry agent's responsibility."

#### C5 — Extensibility model for adding new specialists

- **Sources**: Technical Architect (#3)
- **Accept/Reject**: **Defer.** The pattern is obvious from the existing files. Not needed for v1.

#### C6 — 8-section prompt template flexibility for specialists

- **Sources**: Developer Experience (#5)
- **Accept/Reject**: **Accept as minor wording.** Add: "Specialists may merge sections when content would be redundant."

#### C7 — Specialist invocation logging in build log

- **Sources**: Platform Engineer (#1)
- **Accept/Reject**: **Defer.** Already partially covered by working-directory expectations.

#### C8 — README.md in working directory

- **Sources**: Developer Experience (#2)
- **Accept/Reject**: **Reject.** The enhanced `09-handoff.md` (S2) serves this purpose. Adding another file increases the artifact count, exactly what we're trying to keep proportional.

---

### Won't-Accept

#### W1 — Changelog/release-notes requirement (Marketing #4)
- **Reject**: The builder instructions already say "delete the plan folder." A changelog entry for adding agent files to a pre-1.0 repo is unnecessary ceremony.

#### W2 — Working directory collision handling (Solution Engineering #4)
- **Reject**: Slug collisions on the same date are extremely unlikely. Over-engineering.

#### W3 — Resource cost awareness note (Platform Engineer #3)
- **Reject**: Already covered by conditional invocation guidance and M8 triviality threshold.

#### W4 — Working directory file integrity/checksums (Data Integrity #3)
- **Reject**: Over-engineering for ephemeral collaboration artifacts.

#### W5 — Branch protection guidance (Security #3)
- **Reject**: This is standard git practice and covered by the "read repo instructions first" requirement.

---

## Summary of Required Plan Edits

| ID | Category | Section to Edit |
|----|----------|----------------|
| M1 | Must | New: Failure and Recovery Protocol |
| M2 | Must | Working Directory → Required files + VFE Build workflow |
| M3 | Must | New line in Handoff descriptions |
| M4 | Must | Working Directory Contract |
| M5 | Must | Working Directory Contract |
| M6 | Must | Internal Specialist Requirements |
| M7 | Must | Plan Amendment Review Rule |
| M8 | Must | Working Directory Contract |
| S1 | Should | Working Directory Contract |
| S2 | Should | Working Directory → Format Rules or Handoff Rule |
| S3 | Should | Default Entry-to-Specialist Routing |
| S4 | Should | Prompt Template guardrails |
| S5 | Should | Frontmatter Policy |
| S6 | Should | Prompt-Embedding Priority |
| S7 | Should | Working Directory → Retention Rule |
| C3 | Could | Frontmatter Policy → descriptions |
| C4 | Could | Internal Specialist Requirements |
| C6 | Could | Prompt Template |
