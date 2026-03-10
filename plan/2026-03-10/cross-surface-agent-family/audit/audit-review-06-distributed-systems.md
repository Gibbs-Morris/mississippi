# Review 06: Distributed Systems Engineer

- Issue: The plan should ensure that Mississippi-specific distributed-systems specialists engage conditionally, not by rote on every trivial slice. Why it matters: unnecessary distributed-systems escalation will create noise and overlap. Proposed change: specify that entry agents should invoke distributed-systems review when the change touches concurrency, messaging, actor behavior, consistency, throughput, placement, or cross-service interactions. Evidence: requested strong bias toward distributed systems combined with the remit-boundary requirement. Confidence: High.
- Issue: The branch-wide review pass should explicitly synthesize cross-slice concurrency and consistency interactions. Why it matters: distributed bugs frequently appear at composition boundaries between otherwise-correct slices. Proposed change: add a whole-branch review checkpoint for cross-slice concurrency, consistency, and backpressure interactions when relevant. Evidence: inference from the requested distributed-systems bias. Confidence: Medium.

## CoV

- Claim: specialist invocation criteria should be relevance-based, not automatic. Evidence: user wants minimal remit overlap and world-class specialist viewpoints. Confidence: High.