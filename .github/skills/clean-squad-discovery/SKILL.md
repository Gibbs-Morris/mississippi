---
name: clean-squad-discovery
description: 'Discovery and intake guidance for Clean Squad. Use when shaping a Story Pack, qualifying governed intake, running manual five-question refinement, or generating autonomous discovery defaults with first principles and CoV.'
user-invocable: false
---

# Clean Squad Discovery

Use this skill for pre-governed shaping, governed qualification, manual discovery
loops, and autonomous discovery defaults.

## Qualification round

1. Governed intake defaults to `full-run` plus `autonomous-defaults` when the
	request is clearly asking for end-to-end governed delivery.
2. If the intake appears partial or ambiguous, River asks exactly one
	qualification round that captures execution scope and discovery mode.
3. `manual-refinement` means River continues the human interview directly.
4. `autonomous-defaults` means River delegates discovery-batch generation to
	`cs Requirements Analyst`.

## Manual five-question loop

1. Ask questions in batches of exactly five when governed discovery is active.
2. Prefer the highest-leverage questions first.
3. After each answer set, update the discovery artifact before asking the next batch.

## Autonomous discovery defaults

1. Reuse `01-discovery/questions-round-NN.md` rather than inventing a new artifact family.
2. Each inferred answer records trust tier, source category, evidence reference(s), confidence, and `requiresHumanConfirmation`.
3. Use this precedence order: confirmed human intent → approved governed artifacts → authoritative repo contract surfaces → existing repo patterns → framework defaults → explicit assumptions.
4. Stay bounded to three autonomous rounds or fifteen inferred questions.
5. Promote unresolved high-impact ambiguity to explicit open questions or assumptions instead of inferring past the evidence.
6. Keep inferred defaults separate from confirmed requirements until qualification or G1 later accepts them.

## First-principles framing

1. Why is the request being made?
2. What user or business outcome matters?
3. Which assumptions are conventions rather than fundamentals?
4. What is the minimum clear scope that still solves the real problem?

## CoV reminder

For every non-trivial claim:

1. Draft the claim.
2. Ask verification questions.
3. Answer independently from evidence.
4. Revise the claim.

## Boundary reminder

- `cs Entrepreneur` stays pre-governed and never creates `.thinking/<task>/`.
- `cs River Orchestrator` starts governed discovery only after direct governed input or explicit G0 approval.
- `cs River Orchestrator` remains the orchestrator and canonical writer; it does not author autonomous discovery defaults itself.
