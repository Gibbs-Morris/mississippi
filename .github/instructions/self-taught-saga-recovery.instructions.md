---
applyTo: 'src/DomainModeling.Runtime/**,tests/DomainModeling.Runtime.L0Tests/**'
---

# Self-Taught Lessons: Saga Recovery

Governing thought: Choose continuation from confirmed durable saga progress.

> Drift check: Read overlapping guidance and existing lessons before adding rules, following `self-improvement.instructions.md`.

## Rules (RFC 2119)

- Before enabling failed or compensating saga continuation, agents **MUST** establish recovery direction and compensation progress from durable confirmed history. Why: PR #361's regression tests reproduced forward execution after a rollback failure and repeated compensation from a forward-step index.

## Scope and Audience

Agents implementing or reviewing saga continuation in the DomainModeling runtime and its tests.

## References

- [Self-improvement governance](self-improvement.instructions.md)
- [Orleans conventions](orleans.instructions.md)
- [Observed recovery defects and fixes](https://github.com/Gibbs-Morris/mississippi/pull/361)
