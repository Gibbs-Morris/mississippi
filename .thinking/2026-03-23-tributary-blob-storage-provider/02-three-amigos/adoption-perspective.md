# Developer Evangelist Review

## Elevator Pitch

This feature gives Mississippi adopters a familiar Tributary snapshot-storage option for payloads that outgrow Cosmos's practical size ceiling, without forcing them to relearn the framework or redesign their application code.

## The Conference Talk

- **Title**: When Your Snapshot Outgrows Cosmos: Swapping Tributary to Azure Blob Storage Without Rewriting Your App
- **Abstract** (2 sentences): This talk shows how a Mississippi app can keep the same Tributary programming model while switching snapshot persistence to Azure Blob Storage for materially larger payloads. The audience sees optional gzip compression, pluggable payload serialization, and restart-safe reads, all without changing domain logic.
- **The "Aha!" Moment**: The storage provider changes, the domain code does not, and a payload that was previously an operational risk becomes a routine persisted artifact.
- **Demo Feasibility**: Yes. A 5 to 7 minute live demo is realistic if the happy path is reduced to provider registration, one oversized snapshot write, one restart, and one successful reload.

## Adoption Assessment

| Dimension | Score (1-5) | Evidence |
|-----------|-------------|----------|
| Demo-ability | 4 | The story is easy to stage: configure Blob, enable gzip, write a large snapshot, restart, reload. The feature is less flashy than a new CQRS API, but it has a clean before/after narrative grounded in a real failure mode. |
| Story Arc | 5 | The problem is obvious in one sentence: "Cosmos is the wrong fit when snapshot payloads get too large." The fix is equally clear: keep the same Tributary contract, swap the backing store. |
| Competitive Positioning | 3 | The feature is differentiated inside the Mississippi ecosystem, but it does not outmuscle dedicated event-native platforms such as Kurrent or broad persistence ecosystems such as Axon and Pekko. Its strength is low-friction optionality, not storage sophistication. |
| Real-World Relevance | 5 | The request is anchored in a production pain point, not tutorial novelty. Fintech audit snapshots, healthcare longitudinal state, and logistics planning aggregates all plausibly exceed a document-store comfort zone. |
| Progressive Disclosure | 4 | The confirmed v1 shape is beginner-friendly: app-level provider choice, JSON by default, provider-wide compression toggle, fixed envelope. Complexity rises only when adopters opt into non-default serialization or inspect stored metadata. |
| Shareability | 4 | "Swap one registration, keep your domain model, unlock bigger payloads" is a strong blog-post and conference hook. It is less viral than a new architectural pattern, but highly relatable for teams who have hit store limits. |
| Migration Path | 5 | The feature is explicitly scoped as a Cosmos alternative with mirrored registration and options patterns. That makes the path incremental and believable for existing Mississippi adopters. |

## Competitive Landscape

| Competitor | Their Approach | Our Differentiation | Honest Gaps |
|------------|---------------|---------------------|-------------|
| Axon Framework | Axon defaults to `AxonServerEventStore` and also supports embedded JPA, JDBC, and Mongo event storage engines. Its docs show configurable serializers and substantial monitoring/management surface area around the broader platform. | Mississippi can position this feature as a smaller, more targeted answer to one concrete pain point: oversized persisted artifacts in an otherwise familiar Tributary setup. The value is less platform buy-in and a simpler migration story for current users. | Axon offers a more mature end-to-end persistence ecosystem, richer operational tooling, and deeper event-store specialization than a single new Blob-backed snapshot provider. |
| Marten/Wolverine | Marten's quick start is extremely direct: configure a store, start a stream, append events, project state. Wolverine layers high-productivity aggregate handlers, optimistic concurrency, and HTTP/message workflow shortcuts on top. | Mississippi's Blob provider can compete on storage substitution simplicity for teams already bought into Mississippi, especially where Azure Blob is already approved infrastructure and the goal is larger snapshot persistence rather than replacing the whole stack. | Marten/Wolverine currently wins on demo friendliness in .NET. Their quick starts and aggregate-handler examples are shorter, more dramatic, and closer to a conference slide than a storage-provider feature announcement. |
| EventStoreDB / Kurrent | Kurrent positions itself as an event-native database with append-only streams, subscriptions, projections, optimistic concurrency checks, multiple hosting options, and dedicated client SDKs. The .NET getting started path is package, client, append, read. | Mississippi should not compete as a database platform here. The honest message is different: this feature extends Tributary's storage choices for larger snapshot artifacts while preserving the existing framework mental model. | Kurrent is far stronger as an event-store product story, with a clearer standalone pitch, stronger operational identity, and more obvious developer mindshare for event-native persistence. |
| Akka/Pekko | Pekko Persistence requires selecting journal and snapshot-store plugins, and the docs emphasize typed event sourcing, recovery, plugin configuration, and single-writer discipline. It is powerful, but the learning curve is tied to the actor model and plugin ecosystem. | Mississippi can be simpler to recommend to mainstream .NET teams who want an application-framework experience rather than actor-system adoption. A Blob provider that behaves like the Cosmos provider reinforces that "boring to adopt" advantage. | Pekko offers a wider persistence model with established plugin patterns, clustering, and recovery semantics. The Mississippi story here is narrower and less proven outside its own ecosystem. |

## Marketing Hooks

1. Keep Tributary. Lose the Cosmos size ceiling.
2. From 4 MB anxiety to Blob-backed breathing room.
3. Blog post: Azure Blob Storage for Tributary: The Easiest Way to Survive Oversized Snapshots

## Must Address (adoption blockers)

| # | Concern | Impact on Adoption | Recommendation |
|---|---------|-------------------|----------------|
| 1 | The feature can sound like internal plumbing rather than user value. | Developers ignore it because "new provider" does not immediately communicate why they should care. | Lead every explanation with the concrete user problem: larger persisted payloads without changing application code. |
| 2 | If the setup differs noticeably from the Cosmos provider, the migration story collapses. | Existing adopters will read the feature as a parallel subsystem instead of a drop-in alternative. | Mirror the Cosmos registration and options feel closely enough that a before/after slide fits on one screen. |
| 3 | If the demo does not include restart-safe reload and metadata proof, it feels like a thin blob-upload adapter. | Trust drops because the audience cannot tell whether this is durable framework infrastructure or just storage glue. | Make the minimal demo include write, inspect envelope metadata, restart, and read-back success. |
| 4 | If payload-size value is described vaguely, users may assume Blob is now the "better default" for everything. | Mis-positioning creates confusion and weakens the product narrative. | State plainly that Blob is the larger-payload option, not a blanket replacement for Cosmos. |

## Should Improve (would increase adoption velocity)

| # | Concern | Impact on Adoption | Recommendation |
|---|---------|-------------------|----------------|
| 1 | A provider feature alone is not memorable enough for conference or social traction. | Good engineering may ship with muted awareness. | Pair release messaging with one concrete sample scenario such as a multi-megabyte healthcare or logistics snapshot. |
| 2 | Non-default serializer support is valuable but abstract. | Developers may not notice extensibility beyond "JSON plus gzip." | Show one short alternate-serializer demo or sample test so the extension point feels real. |
| 3 | Blob metadata and envelope semantics are not naturally visible in a framework demo. | Troubleshooting and compatibility confidence stay theoretical. | Publish one inspect-the-blob walkthrough showing version, serializer identity, and compression metadata. |
| 4 | Crescent L2 is framed as "if feasible," which weakens confidence. | Early adopters may wonder whether the feature is fully production-ready. | Treat the L2 as part of the launch story if at all possible, because realistic lifecycle proof materially improves trust. |

## Content Opportunities

This feature supports a strong cluster of practical content:

- A short video: "Switch Tributary from Cosmos to Blob in 10 minutes."
- A migration article showing before-and-after registration for Cosmos versus Blob.
- A production-minded walkthrough on gzip compression and self-describing snapshot envelopes.
- A Crescent sample scenario that writes a large snapshot, restarts, and validates durable reload.
- A comparison article: "When to choose Cosmos vs Blob for Mississippi snapshot storage."

## Real-World Scenarios

1. **Fintech ledger servicing**: a team stores rich statement or audit snapshot material that grows beyond comfortable document-store limits, but still wants deterministic reloads and inspectable metadata.
2. **Healthcare case management**: a patient-centric aggregate accumulates large state representations, attachments-derived summaries, or timeline projections where restart-safe reads matter more than queryable document storage.
3. **Logistics and planning systems**: route or shipment planning snapshots can become large, especially when they aggregate historical recalculations and optimization outputs that are expensive to rebuild on every cold start.

## Minimal Compelling Demo

The smallest demo I would recommend publicly is this:

1. Start with a working Tributary app that uses the current Cosmos-style registration mental model.
2. Swap the storage registration to Blob with default JSON serialization and gzip enabled.
3. Persist one intentionally large snapshot that represents the motivating scenario.
4. Show that the blob contains provider-owned metadata for format, serializer, and compression.
5. Restart the app or rebuild the service provider.
6. Reload the snapshot successfully and highlight that no domain logic changed.

This is strong because it demonstrates the real promise in one sitting: same framework model, larger payload tolerance, and credible durability.

## CoV: Adoption Verification

1. Competitive claims verified against actual documentation: verified. Axon docs confirm multiple event storage engines and serializer configuration. Marten docs confirm a very short event-store quick start and JSONB-backed event storage. Wolverine docs confirm aggregate-handler productivity features and multi-stream workflow support. Kurrent docs confirm event-native positioning, SDK onboarding, append/read basics, projections, and optimistic concurrency. Pekko docs confirm plugin-based persistence and snapshot/journal selection.
2. Demo feasibility tested (code fits on a slide): verified in principle. The most compelling public demo is not a full implementation walkthrough but a concise storage swap plus write/restart/read story.
3. Real-world relevance confirmed (not just a tutorial problem): verified. The request originates from a concrete Cosmos payload ceiling concern, and comparable large-state pressures are common in regulated, trace-heavy, and planning-heavy systems.
4. Progressive disclosure path exists from simple to advanced: verified. The confirmed scope offers a beginner path through default JSON plus optional gzip, with advanced value arriving later through custom serializers, metadata inspection, and broader operational guidance.
