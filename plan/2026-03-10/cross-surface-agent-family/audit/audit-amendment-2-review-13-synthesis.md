# Amendment 2 Review 13: Synthesis

## Must

### 1. Treat the user-provided workflow diagrams as the canonical modeled process.

Decision:
- Accept

Rationale:
- The previous plan was consistent in substance, but not yet authoritative in the exact structural form the user asked for.

Evidence:
- Updated `Canonical Workflow Model` section now captures the supplied end-to-end workflow, family structure, planning flow, per-slice build loop, commit-shaping logic, branch verification loop, PR comment loop, commit-by-commit review view, cross-surface model, and CoV pattern.

Required follow-through:
- Builder should preserve these diagrams or faithful compact equivalents in final agent prompt bodies where the plan requires direct embedding.

## Must

### 2. Preserve direct prompt-embedding guidance for the three highest-value diagrams.

Decision:
- Accept

Rationale:
- This came directly from the user and materially improves consistency of the final family behavior.

Evidence:
- Updated `Prompt-Embedding Priority` section.

Required follow-through:
- Entry-agent implementations should embed the end-to-end workflow, per-slice build loop, and agent family structure directly when prompt size permits.

## Must

### 3. Keep the explicit default entry-to-specialist routing matrix.

Decision:
- Accept

Rationale:
- The family structure is now explicit rather than inferential, which reduces orchestration ambiguity.

Evidence:
- Updated `Default Entry-to-Specialist Routing` section.

Required follow-through:
- Final agent prompts should reflect this routing model while still allowing conditional narrowing for small tasks.

## Should

### 4. Keep manifest language simple even though the plan now contains rich diagrams.

Decision:
- Accept

Rationale:
- The canonical diagrams are for correctness; the manifest should remain the concise human entry surface.

Evidence:
- Marketing, DX, and solution-engineering amendment reviews converged on discoverability and clarity.

## Could

### 5. Mention compact-diagram fallback explicitly in the final agent prompts.

Decision:
- Defer

Rationale:
- The plan already allows compact prompt versions through the prompt-embedding priority rule. Extra prompt-level wording can be decided during implementation.

Evidence:
- Principal engineer, DX, and tooling amendment reviews.

## CoV

- Claim: this plan/process change has now completed a full review round. Evidence: twelve `audit-amendment-2-review-*` persona files plus this synthesis file exist in the audit folder. Confidence: High.
- Claim: the final plan now models the user's workflow directly rather than approximately. Evidence: authoritative workflow diagrams and routing model are present in `PLAN.md`. Confidence: High.
