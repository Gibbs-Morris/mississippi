# Review 01: Marketing and Contracts

## Findings

- **High — Canonical-path migration is incomplete.** The plan must make
  #540/#562/#564 update every `.github/skills` consumer and define any alias or
  deprecation period. Evidence: current skill-builder, Rules Manager, and
  key-principles references; confidence high.
- **High — Support tiers are not yet a public contract.** Publish a Docusaurus
  surface matrix with guarantee, evidence, limitation, and fallback for each
  host. Evidence: issue #540 and official GitHub surface list; confidence high.
- **High — Migration/release communication is missing.** Add before/after paths,
  old-agent/new-skill mappings, compatibility window, validation, rollback, and
  separate skill versioning. Evidence: PR template and documentation migration
  rules; confidence high.
- **Medium — Value targets are absent.** Define go/revise/stop thresholds for
  activation, output quality, context cost, duplicate procedures, rework, and
  workflow findability. Evidence: plan metrics section and PR value rules;
  confidence high.

## Strengths

Outcome-oriented names, the staged strangler approach, invariant separation,
and preservation of both historical/current baselines are sound.

## CoV

Findings were checked against issue #532, current repository consumers, official
surface guidance, and the repository PR/documentation instructions.
