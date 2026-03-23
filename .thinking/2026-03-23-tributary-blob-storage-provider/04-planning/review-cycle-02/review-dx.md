# Developer Experience Review

## Summary

- DX impact: Positive
- Findings: 8 total
- Verdict: CHANGES REQUESTED

Draft plan v2 resolved most of the cycle 1 adoption and setup concerns. The remaining issues are narrower: they are the places where a consumer could still hit ambiguity around the happy path, configuration rules, or failure behavior.

## Usage Walkthrough

The primary consumer story is now mostly coherent:

1. A developer decides Cosmos is no longer a good fit for large Tributary snapshots.
2. They look for the Blob provider registration that mirrors the Cosmos setup shape.
3. They expect one obvious registration path, sensible defaults, and a short migration example.
4. They write a large snapshot, restart, and read it back without changing domain code.
5. If they choose a non-default serializer or misconfigure registration, they expect startup or read failures to tell them exactly what to fix.

The remaining friction is that the plan still leaves a few of those steps too interpretive. The biggest gaps are exact observable outcomes, precise serializer-selection rules, and clearer acceptance criteria for the one canonical setup path.

## DX Concerns

### Must Address

| # | Concern | Impact on Consumer | Recommendation |
|---|---------|-------------------|----------------|
| 1 | The contract-parity matrix still uses comparison wording instead of exact consumer-visible outcomes for some behaviors. | A developer cannot tell from the plan what actually happens for missing reads, duplicate-version writes, delete-missing, corrupt blobs, or initialization failures. That weakens both IntelliSense-quality docs and troubleshooting guidance. | Replace comparison phrasing with exact outcomes such as `returns null`, `no-op success`, or a named actionable failure. The plan should define the consumer-visible result, not just say it matches Cosmos in spirit. |
| 2 | The serializer-selection and persisted-identity rule is still not explicit enough inside the plan itself. | This is the easiest advanced path to misuse. If multiple serializers are registered, consumers need to know exactly how the provider chooses one, what stable identifier is persisted, and what happens on restart when the identifier cannot be resolved. | State the exact pit-of-success rule in the plan: zero matches fail startup, multiple matches fail startup, the persisted serializer identity is a concrete stable identifier, and unknown persisted identities fail read with an actionable error. |

### Should Improve

| # | Concern | Impact on Consumer | Recommendation |
|---|---------|-------------------|----------------|
| 1 | The plan names a canonical registration path, but it still does not make that path an explicit acceptance point. | Consumers will have a worse onboarding experience if implementation lands with several equally promoted overloads or configuration entry points. | Add an acceptance item that locks one blessed registration path and classifies any alternate overloads as advanced-only. |
| 2 | The evidence package still allows a "non-default configuration or serializer path," which weakens the extensibility story. | Consumers evaluating custom serialization need proof that the real extension point survives restart, not just proof that some unrelated advanced option works. | Make non-default serializer restart-survival mandatory somewhere in the evidence package. If L2 cannot carry it, require it explicitly at L0 and say why. |
| 3 | The Crescent L2 trust slice still bundles too many concerns into the mandatory walkthrough. | A consumer-facing trust story is strongest when the happy path is obvious. If one scenario has to prove registration, large payloads, gzip, metadata inspection, restart, and advanced configuration all at once, failures become harder to explain and the story gets muddy. | Keep one mandatory L2 story focused on registration, write, restart, and read-back. Treat metadata inspection, large-payload amplification proof, and secondary advanced assertions as additive evidence. |
| 4 | Error experience is directionally improved, but the plan still does not define which diagnostics are part of the consumer contract versus implementation detail. | Supportability becomes inconsistent if reviewers cannot tell which exceptions, messages, and triage fields must be present when startup or read paths fail. | Add a small error-and-diagnostics evidence matrix for startup misconfiguration, duplicate conflicts, unreadable blobs, checksum failures, and unsupported serializer or compression cases. |
| 5 | Required versus optional configuration is still not surfaced as clearly as it should be for first-time adopters. | New users benefit from knowing which settings are mandatory, which are safe defaults, and which are advanced tuning knobs. Without that split, the setup story can still feel heavier than it needs to. | In the plan's documentation and acceptance scope, explicitly classify required inputs, defaults, and advanced knobs such as initialization mode and `ListPageSizeHint`. |

### Could Fix

| # | Concern | Impact on Consumer | Recommendation |
|---|---------|-------------------|----------------|
| 1 | Inspectability is well-covered through the blob frame, but an optional shortcut for quick field inspection remains undecided. | Some operators will want a simpler way to confirm serializer and compression choices without decoding the stored body. | Add optional Azure metadata duplication only if it is cheap and clearly non-authoritative; otherwise leave inspectability on the frame walkthrough alone. |
| 2 | Concurrency expectations beyond duplicate-version conflict are still not stated. | Consumers may infer guarantees about adjacent or near-simultaneous writes that the provider does not actually promise. | Add one explicit note saying whether concurrency semantics beyond duplicate-version conflict are part of v1 or intentionally unspecified. |

### Won't Fix

| # | Deferred item | Rationale |
|---|---|---|
| 1 | New shared storage abstractions or mixed-provider orchestration | That would make setup less discoverable and expand the feature far beyond the consumer problem this plan is trying to solve. |
| 2 | Manifest, pointer, or index-based lookup features to avoid stream-local scans | The v1 DX story is better served by a simple, explainable provider than by a more sophisticated storage model with more moving parts. |
| 3 | Compression or serializer expansion beyond the narrow v1 shape | The plan is more usable when defaults stay obvious and advanced behavior stays explicit rather than adaptive or magic. |
| 4 | Real-Azure scale certification as part of the launch bar | That is valuable operationally, but it is not required to make the first consumer-facing usage story understandable and supportable. |

## Positive DX Choices

- The plan now clearly centers the user problem: larger persisted payloads without changing domain code.
- The documentation scope includes the right onboarding assets: canonical registration, setup translation, decision guidance, and metadata inspection.
- The plan keeps the public contract stable and limits parity pressure to the public registration and options surface rather than forcing deeper cross-provider abstractions.
- Self-describing stored data remains the right DX choice for restart trust and troubleshooting.

## CoV: DX Verification

1. Usage walkthrough completed without confusion: partially verified. The happy path is now clear, but exact outcomes and configuration rules still need to be nailed down.
2. Error scenarios produce actionable messages: partially verified. The plan intends this, but the required diagnostics are not yet precise enough to review consistently.
3. API consistency with existing repo patterns: verified in direction. The plan preserves the Cosmos-style mental model, but the single canonical registration path still needs to be locked as an explicit acceptance criterion.
