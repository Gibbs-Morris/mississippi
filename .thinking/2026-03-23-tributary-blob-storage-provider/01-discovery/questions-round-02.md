# Discovery Round 02

## Questions and Answers

1. Blob mapping model
   - Selected: A. One logical persisted record per blob.
   - Interpretation: the provider should keep storage units simple and independently addressable rather than aggregating multiple records into a shared blob.

2. Write semantics
   - Selected: B. Match the public contract outcomes, but allow Blob-specific internals.
   - Interpretation: callers should observe the same contract-level behavior as the Cosmos provider even if concurrency and write mechanics are implemented differently underneath.

3. Compression semantics
   - Selected: A. Provider-wide option: off or gzip for all payloads.
   - Interpretation: first release should keep compression explicit and predictable instead of introducing threshold heuristics or per-artifact policies.

4. Serializer plug-in boundary
   - Selected: A. Only the payload/body format.
   - Interpretation: provider metadata stays in a fixed provider-owned shape, while the payload serialization strategy remains replaceable.

5. Crescent L2 scope
   - Selected: C. Realistic slice with compression and one non-default configuration.
   - Interpretation: the L2 should prove more than a trivial round trip by exercising compression and at least one intentional configuration variation.

## Product Owner Notes

- The user is implicitly favoring a low-risk v1 architecture: keep fixed provider metadata, keep compression simple, and preserve the external contract shape.
- Remaining uncertainties are likely around configuration surface, coexistence with Cosmos, performance/size expectations, and operational requirements such as emulator support and observability.