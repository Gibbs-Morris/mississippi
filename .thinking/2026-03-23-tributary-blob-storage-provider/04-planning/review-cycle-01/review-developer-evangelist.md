# Developer Evangelist Review

## Verdict

- Adoption/story verdict: Changes requested.
- Core issue: the draft plan is technically credible, but it still reads like a storage-provider implementation plan rather than a plan to help a developer quickly understand, trust, and copy the feature.
- Strongest asset already present: the feature has a sharp real-world problem statement from earlier phases: keep Tributary, remove the Cosmos payload ceiling, and avoid domain-code churn.
- Main planning gap: that value proposition is not yet turned into explicit delivery checkpoints, copy-paste assets, or proof points in the plan itself.

## Must-Fix

| # | Concern | Impact on Adoption | Recommendation |
|---|---------|-------------------|----------------|
| 1 | The plan leads with implementation shape, not the user pain and outcome. | A developer scanning the plan cannot immediately see why this feature matters beyond "new Blob provider." That weakens urgency and makes the work feel infrastructural instead of user-relevant. | Rewrite the executive framing in the plan so the first claim is: larger snapshots beyond Cosmos's practical ceiling, with the same Tributary contract and nearly the same setup shape. Keep the technical details after that outcome statement. |
| 2 | The plan does not commit to a minimal compelling demo or copy-paste path. | Without one bounded demo story, the release is harder to explain, harder to teach, and harder to trust. It becomes a capability that sounds useful but is not obviously repeatable by adopters. | Add an explicit delivery item for the smallest public proof: switch registration, write one intentionally large snapshot, inspect stored metadata, restart, and read it back with no domain-code changes. Treat that as a required planning artifact, not a nice-to-have. |
| 3 | The plan promises Cosmos-like adoption but does not require a before-and-after setup story. | Existing Mississippi users are the most credible early adopters. If they cannot see the translation from Cosmos to Blob in one screen, the migration story stays abstract. | Add a required copy-paste asset to the plan: one Cosmos-to-Blob registration comparison and one minimal Blob-only setup example. The point is not broad docs coverage; it is a fast migration mental model. |
| 4 | Acceptance criteria prove correctness, but not convincingly enough that the feature matters to developers. | The current acceptance list would let the team ship something technically correct while still leaving adopters unsure when to choose Blob, what changed for them, and why they should care. | Add adoption-facing acceptance criteria: one clear "when to choose Blob over Cosmos" statement, one proven large-payload scenario that maps to a real production case, and one proof that the public setup remains recognizably familiar. |
| 5 | Crescent L2 is present, but the plan still treats the decisive story proof too loosely. | If restart-safe reload, metadata inspection, and non-default configuration are not presented as the public proof package, the feature risks looking like a thin SDK adapter rather than a production-ready framework capability. | Recast the L2 increment as the trust-building story asset for launch: same contract, larger payload, self-describing blob, restart-safe read. Keep that exact proof set explicit in the plan. |

## Should-Fix

| # | Concern | Impact on Adoption | Recommendation |
|---|---------|-------------------|----------------|
| 1 | The plan does not yet name one concrete production scenario to anchor the story. | "Larger payloads" is true, but generic. Developers remember a believable scenario more than an abstract limit discussion. | Add one primary scenario to the plan narrative, such as healthcare longitudinal state or logistics planning snapshots, and use it consistently in docs and review language. |
| 2 | Copy-paste value is implied, not planned. | Developers adopt infrastructure features faster when they can start from a known-good snippet instead of reconstructing configuration from prose. | Add explicit documentation scope for three assets: minimal setup snippet, Cosmos-to-Blob migration snippet, and blob-inspection walkthrough showing serializer and compression metadata. |
| 3 | The plan does not explicitly protect the "no domain changes" message. | The strongest adoption hook is that storage changes while domain code does not. If that message is not protected, later docs or samples may accidentally bury it. | Add a review checkpoint that all public examples demonstrate provider substitution without domain-model changes. |
| 4 | The release story does not yet separate "better for larger payloads" from "better in general." | If the positioning stays vague, teams may misread Blob as the new default for every case and later feel misled when the tradeoffs surface. | Add a small decision guide to planned docs or release notes: choose Blob for larger persisted artifacts and Cosmos where its existing operational profile still fits. |
| 5 | The plan lists public setup work, but not a memorable headline or message spine. | Features that lack a crisp headline tend to ship quietly, even when they solve a real problem. | Add one canonical message line to the plan for downstream docs and PR description use: keep Tributary, swap storage, survive oversized snapshots. |

## Could-Fix

| # | Concern | Impact on Adoption | Recommendation |
|---|---------|-------------------|----------------|
| 1 | The plan does not yet reserve a visual artifact for the story. | A simple before/after diagram or one-slide flow can materially improve docs, review clarity, and conference viability. | Add one lightweight visual to follow-on documentation scope: Cosmos pain point to Blob-backed restart-safe snapshot flow. |
| 2 | The inspectability story is present architecturally but not yet packaged for teaching. | Developers and support engineers benefit from seeing that the stored blob is understandable, not opaque. | Add a "how to inspect a stored blob frame" content item if the documentation slice allows it. |
| 3 | There is no explicit social or workshop hook in the plan. | This will not block implementation, but it limits how reusable the launch material becomes. | Capture one reusable content angle for later: "10 minutes to move Tributary snapshots off Cosmos when size becomes the problem." |

## Won't-Fix For V1

| # | Concern | Reason to Defer | Recommendation |
|---|---------|-----------------|----------------|
| 1 | Broad migration tooling between Cosmos and Blob. | The confirmed feature is a new provider, not a data-migration product. Pulling migration tooling into this plan would dilute the copy-paste adoption story instead of sharpening it. | Keep the v1 ask focused on provider adoption guidance, not data-porting automation. |
| 2 | Claims that Blob is generally superior to Cosmos. | That positioning would be misleading and easy to disprove. The honest story is about payload-size fit and low-friction substitution. | Keep messaging comparative but narrow: Blob solves the larger-payload case without forcing a new programming model. |
| 3 | A broad competitive bake-off against Marten, Axon, EventStoreDB, or Pekko in release materials. | The feature does not win on platform breadth. Over-positioning it would weaken credibility. | Keep competitor framing internal and use external messaging that stays grounded in the actual Mississippi value proposition. |

## Recommended Plan Delta

The plan should explicitly deliver four adoption artifacts alongside the implementation work:

1. A problem-first opening that states the user pain in one sentence.
2. One minimal copy-paste setup path plus one Cosmos-to-Blob translation snippet.
3. One decisive end-to-end proof story: large snapshot, metadata visible, restart-safe reload, no domain changes.
4. One short decision guide explaining when to choose Blob versus Cosmos.

If those four items are added, the plan will do more than describe how to build the provider. It will also describe how to make a skeptical Mississippi adopter believe the feature is real, useful, and easy to try.

## CoV: Adoption Verification

1. Feedback stayed inside the confirmed feature scope: verified.
2. Feedback focused on demo clarity, adoption, copy-paste value, and feature significance rather than code design changes: verified.
3. Recommendations align with the earlier adoption perspective and do not contradict the architecture or other review outputs: verified.
