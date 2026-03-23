# Business Perspective — Three Amigos

## First Principles

- Why does this feature exist?
  Tributary needs a Blob-backed storage provider because the existing Cosmos-backed provider has a practical payload ceiling of roughly 4 MB, which blocks legitimate application scenarios that need to persist materially larger artifacts.
- What user outcome does it enable?
  Framework adopters can keep using the Tributary programming model they already understand while choosing a storage provider that supports larger persisted payloads without forcing a redesign of their application code or configuration habits.
- Is the stated requirement the real requirement?
  Yes, with one important refinement: the real requirement is not "support Azure Blob Storage" in isolation. The real requirement is "remove Cosmos size constraints while preserving a familiar, low-friction adoption experience and trustworthy persistence behavior."

## User Stories

### US-1: Choose a larger-payload provider without relearning Tributary setup

As a framework adopter, I want to configure a Blob-based Tributary provider using familiar registration and options patterns so that I can switch from Cosmos when payload size becomes a problem without incurring avoidable adoption cost.

**Acceptance Criteria:**

1. Given an application that currently understands the Cosmos provider setup model, when the adopter configures the Blob provider, then the registration and options experience is recognizably aligned with the Cosmos provider rather than introducing a materially different mental model.
2. Given an adopter selecting Blob storage for an application, when the provider is configured for first use, then Blob is treated as an app-level alternative provider for that Tributary setup rather than requiring side-by-side mixed-provider behavior in v1.
3. Given an adopter evaluating whether Blob solves their problem, when they review the provider behavior, then the business-facing value proposition is clear: Blob is the option for larger persisted payloads while preserving the existing Tributary contract experience as closely as practical.

### US-2: Persist larger Tributary artifacts safely and predictably

As a framework adopter, I want the Blob provider to persist larger records with optional gzip compression and selectable payload serialization so that I can handle larger storage scenarios without losing predictability or compatibility.

**Acceptance Criteria:**

1. Given a persisted Tributary record that would be a poor fit for Cosmos because of payload size constraints, when the application uses the Blob provider, then the record can be stored and retrieved through the same contract-level behavior expected from the existing provider model.
2. Given provider-wide compression is configured as `off`, when records are written and read, then the provider behaves correctly without compression-specific requirements.
3. Given provider-wide compression is configured as `gzip`, when records are written and read, then the provider preserves correct round-trip behavior using gzip consistently for persisted payloads.
4. Given no custom payload serializer is selected, when records are persisted, then JSON is the default payload serializer.
5. Given a non-default payload serializer is configured, when records are persisted and retrieved, then the provider uses that serializer for payload content while keeping provider-owned metadata in a fixed stored envelope.
6. Given a stored record exists in Blob storage, when it is inspected through approved product validation paths, then the record carries provider-owned metadata sufficient to identify storage-format version, serializer identity, and compression algorithm.

### US-3: Trust the provider before adopting it in production workloads

As a product team or adopter, I want strong automated validation of the Blob provider so that I can trust it for real applications that depend on durable storage behavior.

**Acceptance Criteria:**

1. Given the provider implementation is considered feature-complete, when the test suite is reviewed, then comprehensive unit coverage exists for the confirmed behaviors in scope for v1.
2. Given the Crescent L2 path is feasible, when the end-to-end scenario runs, then it proves more than a trivial round trip by covering compression, one non-default configuration, persisted metadata expectations, and restart or reload compatibility.
3. Given the provider is presented as production-oriented, when stakeholders evaluate release readiness, then evidence exists that the Blob provider preserves expected contract-level behavior rather than behaving as an experimental or best-effort adapter.

## Business Rules

1. The Blob provider MUST be positioned as a solution to the Cosmos payload-size limitation, not as a general redesign of Tributary storage.
2. The Blob provider MUST follow the same public contracts as the existing Cosmos provider, with any Blob-specific differences kept behind the implementation boundary.
3. The first release MUST support Blob as an application-level alternative provider, not require mixed Cosmos-and-Blob operation inside the same application workflow.
4. The first release MUST use one logical persisted record per blob.
5. The first release MUST use prefix listing plus naming conventions for latest-record lookup and prune discovery.
6. Compression in v1 MUST be a provider-wide choice of either `off` or `gzip`; automatic thresholds and per-artifact policies are out of scope.
7. Payload serialization MUST be pluggable, and JSON MUST be the default payload serializer.
8. Provider metadata MUST remain in a fixed provider-owned stored envelope and MUST include enough information to identify format version, serializer identity, and compression algorithm.
9. Quality for v1 MUST include comprehensive unit tests and SHOULD include a Crescent L2 scenario when feasible.
10. The Crescent L2 scope, when implemented, MUST validate behavior, metadata expectations, and restart or reload compatibility in addition to a basic happy path.

## Rollout Implications

- Product positioning must clearly explain when adopters should choose Blob over Cosmos: the driver is larger payload support with familiar Tributary usage, not a blanket replacement of Cosmos for every scenario.
- Release communication should set expectations for v1 simplicity: app-level provider choice, provider-wide compression, fixed metadata envelope, and no mixed-provider workflow or advanced indexing model.
- Adoption readiness depends on clear guidance for framework consumers because the primary audience configures Mississippi directly and will judge the feature by setup friction as much as by raw capability.
- Rollout confidence materially improves if Crescent L2 coverage is delivered, because this feature's value proposition depends on durable behavior across realistic lifecycle events, not only unit-tested components.
- Documentation and release notes should explicitly frame the feature as enabling larger persisted artifacts beyond the practical Cosmos ceiling, while calling out deferred capabilities so users do not assume richer Blob-native behavior than has been promised.

## Success Metrics

- At least one validated adoption path exists where a framework consumer can configure the Blob provider using a familiar options and DI experience without custom workaround steps.
- The provider demonstrably supports the target class of larger payload scenarios that motivated the request, removing the practical blocker imposed by Cosmos size limits.
- Automated test evidence shows stable round-trip behavior for default JSON serialization, gzip compression, and at least one non-default serializer or configuration path.
- Crescent L2, if delivered, passes consistently and demonstrates restart or reload compatibility plus persisted metadata validation.
- Early adopter feedback indicates the provider is understandable as a Cosmos alternative rather than requiring substantial retraining or product support.

## Risks to User Value

| Risk | Impact | Mitigation |
|------|--------|------------|
| The Blob provider feels materially different from Cosmos to configure or reason about. | Adoption drops because the feature solves one problem while creating a new integration burden. | Keep registration and options patterns closely aligned with Cosmos and document the choice model clearly. |
| The product message overpromises Blob capabilities beyond the confirmed v1 scope. | Users adopt with the wrong expectations and perceive the release as incomplete or misleading. | Document the v1 boundaries explicitly: app-level alternative, simple prefix-based lookup, provider-wide compression, and no mixed-provider workflow. |
| Compression and serializer choices are not made sufficiently visible in stored records. | Support and troubleshooting become difficult, reducing trust in persisted-data compatibility. | Require a fixed provider-owned envelope with explicit version, serializer, and compression metadata. |
| The provider passes unit tests but lacks realistic lifecycle validation. | Users may discover restart, reload, or metadata issues late in adoption. | Treat Crescent L2 as high-value release evidence and ensure it covers restart or reload compatibility plus metadata assertions when feasible. |
| The feature removes the Cosmos size blocker but does not make the selection criteria clear. | Teams may misuse Blob for scenarios where they expected broader behavioral differences or operational guarantees. | Publish simple guidance on when to choose Blob and what the first release intentionally does not optimize for. |

## Out of Scope

- Automatic compression thresholds.
- Per-artifact compression policies.
- Pluggable provider metadata formats.
- Manifest, index, or blob-tag based lookup models.
- Mixed Cosmos-and-Blob operation inside one application workflow.
- A broader redesign of Tributary storage abstractions.

## CoV: Acceptance Criteria Verification

1. US-1 AC1 is testable because reviewers can compare the public registration and options experience against the established Cosmos pattern; it is unambiguous because "recognizably aligned" is anchored to the confirmed requirement to mirror Cosmos patterns closely; it captures user intent because the main audience is framework adopters configuring Mississippi directly.
2. US-1 AC2 is testable because the release can verify that Blob is presented and validated as an app-level alternative; it is unambiguous because mixed-provider behavior was explicitly deferred; it captures user intent because v1 is meant to solve the size problem with low complexity.
3. US-1 AC3 is testable through product documentation and release-positioning review; it is unambiguous because the core value proposition is explicitly tied to larger payloads plus familiar setup; it captures user intent because the user requested Blob to overcome Cosmos size constraints.
4. US-2 AC1 through AC6 are testable through unit and integration evidence; they are unambiguous because each criterion maps directly to confirmed scope decisions on contract parity, provider-wide compression, JSON defaulting, serializer pluggability, and fixed metadata; they capture user intent because these were the main confirmed functional expectations across discovery rounds 1 through 3.
5. US-3 AC1 through AC3 are testable through the existence and scope of automated validation artifacts; they are unambiguous because the required proof points were explicitly confirmed in the requirements synthesis; they capture user intent because the request called for full unit testing and a realistic Crescent L2 when feasible.
6. Evidence cross-reference: these criteria are grounded in the confirmed synthesis and the three discovery rounds, especially the confirmed requirements for Cosmos-pattern familiarity, one-record-per-blob storage, `off` or `gzip` compression, payload-only serializer pluggability, fixed provider metadata, and Crescent L2 validation depth.
7. Revised criteria result: no criteria changes were required after verification because each criterion remains testable, anchored to confirmed scope, and avoids introducing unconfirmed technical design detail.
