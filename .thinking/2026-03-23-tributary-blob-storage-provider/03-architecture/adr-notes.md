# ADR Extraction Notes

## Context

This note maps the final Tributary Blob snapshot architecture to publishable Architecture Decision Records.
The goal is to capture only decisions that meet the repository ADR threshold: structure-affecting, hard to reverse, cross-cutting, precedent-setting, or meaningfully trade-off driven.

## Sources Reviewed

- `03-architecture/solution-design.md`
- `03-architecture/expert-cloud-review.md`
- `03-architecture/expert-serialization-review.md`
- `02-three-amigos/synthesis.md`
- `.github/instructions/adr.instructions.md`

## ADR Set

1. `ADR-0001` records the stream identity and hashed blob naming strategy.
   Why it qualified: it defines the persisted namespace, stream-safety model, and future listing behavior.

2. `ADR-0002` records the provider-owned blob frame.
   Why it qualified: it fixes the durable on-disk format, versioning boundary, and authoritative metadata location.

3. `ADR-0003` records serializer selection and persisted serializer identity.
   Why it qualified: it defines how opaque payload bytes remain restart-safe and evolvable when multiple serializers exist.

4. `ADR-0004` records payload-only compression plus payload integrity validation.
   Why it qualified: it sets the persistence format boundary, operational behavior, and decode-failure model.

5. `ADR-0005` records Azure conditional-write and stream-local maintenance semantics.
   Why it qualified: it defines correctness under concurrency and the scaling boundary for prune and delete-all.

6. `ADR-0006` records container initialization mode.
   Why it qualified: it sets the deployment contract between application startup, infrastructure provisioning, and least-privilege hosting.

## Decisions Intentionally Not Recorded As Separate ADRs

- The addition of a Blob provider itself was not recorded separately because the design work already converged on that feature request and the architectural trade-offs are captured by the six implementation-shaping decisions above.
- Observability details were not recorded separately because they support the chosen architecture rather than define a stable structural precedent.
- Test strategy was not recorded separately because it verifies the design rather than choose the design.
- Optional diagnostic duplication into Azure blob metadata was not recorded separately because the final design leaves it optional and non-authoritative.
- Azurite scope was not recorded separately because it is a validation boundary, not a runtime architecture decision.

## CoV Check

1. Context accuracy: verified against the final solution design and both expert reviews.
2. Alternatives: each ADR includes the rejected options that were explicitly discussed in the design or reviews.
3. Consequences: each ADR includes both operational upside and the constraints the team accepts.
4. Alignment: no existing numbered ADRs were present, so this set establishes the initial ADR baseline for the repository.
5. Evidence: all ADRs trace directly to the architecture task folder documents and repository ADR instructions.
