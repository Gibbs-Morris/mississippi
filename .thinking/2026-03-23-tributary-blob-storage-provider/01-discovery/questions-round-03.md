# Discovery Round 03

## Questions and Answers

1. Provider selection granularity
   - Selected: A. App-level alternative.
   - Interpretation: the first release can assume an application typically chooses Blob instead of Cosmos for a given Tributary setup, rather than mixing both providers side by side in one workflow.

2. Lookup strategy
   - Selected: A. Prefix listing plus naming conventions.
   - Interpretation: latest-record discovery and prune enumeration should start with a simple naming-based strategy rather than adding an index or manifest layer in v1.

3. Registration and configuration shape
   - Selected: A. Mirror Cosmos registration patterns closely.
   - Interpretation: Blob setup should feel familiar to current Mississippi adopters and preserve the existing options and DI mental model where possible.

4. Persisted metadata contract
   - Selected: A. Fixed provider metadata in the stored document or envelope.
   - Interpretation: serializer identity, compression algorithm, and storage-format version should live in a provider-owned stable envelope rather than being scattered across Azure Blob metadata features.

5. Crescent L2 assertions
   - Selected: C. Behavior, metadata, and restart/reload compatibility.
   - Interpretation: the first L2 should go beyond a simple happy path and prove the provider writes durable, inspectable state that survives a fresh app lifecycle.

## Product Owner Notes

- The desired first release deliberately avoids premature complexity: no mandatory mixed-provider model, no manifest/index design, and no advanced compression heuristics.
- The project now has enough discovery precision to move into cross-functional analysis without likely reopening foundational scope decisions.