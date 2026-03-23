# Discovery Round 01

## Questions and Answers

1. Business value
   - Selected: A. Larger snapshots/events beyond Cosmos limits.
   - Interpretation: the first-order outcome is safe support for payloads that exceed the practical Cosmos size ceiling.

2. Scope
   - Selected: A. Strict Cosmos parity plus Blob backing.
   - Interpretation: the Blob provider should follow the existing Tributary Cosmos provider shape as closely as possible for first release.

3. Users
   - Selected: A. Framework adopters configuring Mississippi directly.
   - Interpretation: configuration ergonomics and public API cleanliness matter because external consumers will wire this provider themselves.

4. Done / quality
   - Selected: B. Unit tests plus Crescent L2 happy path.
   - Interpretation: comprehensive L0 coverage is required, and an end-to-end integration path in Crescent should be added if feasible within the provider slice.

5. Constraints
   - Selected: A. Match existing Tributary/Cosmos contracts and options patterns.
   - Interpretation: the new provider should preserve the existing provider contract surface and familiar configuration approach unless a strong technical reason requires deviation.

## Product Owner Notes

- The request is clearly not exploratory; it is a production-oriented provider addition driven by a concrete size limitation in Cosmos.
- Remaining unknowns are mostly technical and operational: the exact Blob mapping model, serializer/compression extension boundaries, and how much end-to-end coverage Crescent should carry in the first release.