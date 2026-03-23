# Serialization Expert Review

## Overall Verdict

The persisted-format direction is mostly sound for v1, but it is underspecified in the places that will decide whether stored blobs remain readable, diagnosable, and evolvable over time.

The strongest design choices already in place are:

1. Keeping a provider-owned envelope separate from the upstream snapshot payload.
2. Keeping the header uncompressed while limiting compression to the payload segment.
3. Recording serializer identity in persisted metadata rather than relying on ambient configuration.
4. Versioning the provider frame independently from the upstream snapshot payload bytes.

The main weaknesses are:

1. The proposed frame shape does not yet define a strict binary prelude tightly enough for durable decoding.
2. `payloadFormat` is too weak to serve as a long-lived serializer identity.
3. Forward-compatibility rules are implied, but not specified.
4. Integrity boundaries are unclear, which will make corruption and serializer mismatch harder to distinguish.

## Serialization Concerns

| # | Severity | Type | Concern | Recommendation |
|---|----------|------|---------|----------------|
| 1 | Critical | Envelope framing | The frame is described conceptually as `magic marker`, `storage format version`, `header byte length`, `header JSON`, `payload`, but the binary contract is not specified precisely enough for long-term decoding. Without fixed field widths, byte order, and maximum header size, independent readers can drift. | Define a strict prelude for v1: fixed magic bytes, fixed-width frame version, fixed-width flags or reserved field, and fixed-width header length with an explicit byte order. Cap header length and fail closed when it exceeds the limit. |
| 2 | Critical | Serializer identity | `payloadFormat` with values such as `json` is not a sufficient persisted serializer identity. It identifies a family, not a concrete wire contract. Two JSON serializers or two JSON configurations can both be `json` while producing incompatible payloads. | Persist a stable `payloadSerializerId` that uniquely identifies the concrete serializer contract used to produce `SnapshotEnvelope.Data`. Keep `json` as the default configured choice, but persist the resolved serializer identity, not only the requested format family. |
| 3 | High | Versioning | The design correctly separates provider frame version from payload bytes, but it does not define when each version changes. That risks using `storageFormatVersion` for payload-schema changes or bumping it unnecessarily for additive header fields. | Define explicit rules: bump frame version only when the outer blob framing changes incompatibly; do not bump it for additive header properties that old readers can ignore. Treat payload evolution separately through serializer identity and stable snapshot storage naming. |
| 4 | High | Compression boundary | Compressing only the payload segment is the right choice, but the integrity boundary is incomplete. A corrupt payload, wrong codec, or serializer mismatch can collapse into similar read failures unless the stored format can distinguish them. | Keep payload-only compression. Add a payload integrity field, preferably over the uncompressed payload bytes, so decode can distinguish corruption from codec mismatch or serializer mismatch. Do not advertise whole-blob compression semantics for a partially compressed custom frame. |
| 5 | High | Forward compatibility | The header uses JSON, which is good for additive evolution, but the compatibility behavior is not stated. Without explicit rules, implementations tend to become either too strict or too permissive. | Specify v1 reader behavior: unknown header properties are ignored, missing required properties fail closed, unknown frame versions fail closed, and unknown compression or serializer identifiers fail with explicit errors. |
| 6 | Medium | Canonical identity | The design uses `streamKey.ToString()` for hashing and also stores `streamKey` in the header. If the string form ever changes, the naming and validation contract changes even if logical identity did not. | Define one canonical persisted representation for stream identity and use it both for hashing and for stored header values. Do not let `ToString()` remain the implicit storage contract. |
| 7 | Medium | Payload metadata scope | `snapshotStorageName` and `reducerHash` are useful context, but they do not replace a concrete serializer contract identifier. In particular, `reducerHash` is not a payload schema version. | Keep `snapshotStorageName` and `reducerHash` as contextual metadata. Do not use them as primary decoding keys. Persist serializer identity and keep schema evolution responsibility with the payload serializer or snapshot contract. |

## Strong Recommendations

### 1. Freeze the outer frame as a real binary contract

For v1, the document should stop at a small and durable outer contract rather than leaving framing implicit.

Recommended shape:

1. Magic bytes.
2. Frame version.
3. Flags or reserved bits.
4. Header length.
5. Header bytes encoded as UTF-8 JSON.
6. Payload bytes, optionally compressed.

The critical point is not the exact field list. The critical point is that field widths, byte order, and validation rules must be fixed in the format spec.

Without that, the design is inspectable but not truly specified.

### 2. Replace `payloadFormat` with a real serializer identity

The current design says payload serialization is pluggable and JSON is the default. That is the right requirement, but the stored metadata should represent what was actually used, not only what family was requested.

`json` is an acceptable option name.

`json` is not an acceptable long-lived wire identity by itself.

For example, these could all truthfully claim to be `json` while not being safely interchangeable:

1. Different serializer implementations.
2. Different polymorphism rules.
3. Different enum encoding rules.
4. Different type registries.
5. Different contract resolvers or naming policies.

Recommendation:

1. Keep `PayloadSerializerFormat` as configuration input.
2. Persist a resolved `payloadSerializerId` in the blob header.
3. Require that each pluggable serializer expose a stable persisted identity.
4. Treat serializer identity mismatch as a hard read failure unless a deliberate compatibility path exists.

That keeps JSON as the default while making persisted blobs self-describing in a way that survives future serializer additions.

### 3. Separate the three version axes clearly

The design needs three distinct version axes, even if only one is numeric in v1:

1. Frame version: the outer blob envelope contract.
2. Serializer identity or serializer-specific schema version: how the payload bytes were encoded.
3. Snapshot type identity: the stable persisted identity of the snapshot payload contract.

The document already carries pieces of this with `storageFormatVersion`, `payloadFormat`, and `snapshotStorageName`, but the responsibilities are blurred.

Recommended rule set:

1. `storageFormatVersion` changes only when the provider-owned outer frame changes incompatibly.
2. `snapshotStorageName` remains the stable identity of the snapshot contract and must not be treated as optional decoration once data is persisted.
3. Serializer evolution is expressed through serializer identity, not by overloading the frame version.
4. Additive header fields should not force a frame-version bump when an older reader can safely ignore them.

This is the cleanest way to keep the provider envelope stable while still allowing the payload serialization layer to evolve independently.

### 4. Keep compression scoped to payload bytes only

This is one of the strongest calls in the design.

Compressing only the payload segment is the correct persisted-format boundary because it preserves:

1. Header inspectability.
2. Deterministic access to serializer and format metadata.
3. Better operational debugging.
4. A cleaner path for future codecs.

The main improvement needed is to make the boundary fully explicit:

1. `compressionAlgorithm` applies only to payload bytes.
2. `storedPayloadBytes` refers only to the payload segment as stored.
3. `uncompressedPayloadBytes` refers to the payload after decompression and before handing bytes back to the snapshot layer.
4. Any transport or blob properties must not describe the entire blob as compressed when only the payload segment is compressed.

If the design wants a single extra safeguard, make it an integrity checksum on the uncompressed payload. That gives better failure diagnosis than relying on codec or JSON exceptions alone.

### 5. Write down forward-compatibility behavior explicitly

Using JSON for the header is a defensible v1 choice because it is readable, extensible, and easy to inspect. The missing part is the compatibility policy.

Recommended v1 policy:

1. Readers must require the magic bytes and a supported frame version.
2. Readers must reject missing required header properties.
3. Readers must ignore unknown header properties.
4. Readers must reject unknown compression algorithms.
5. Readers must reject unknown serializer identities.
6. Readers should log the stored frame version, serializer identity, and compression algorithm on every decode failure.

That gives additive evolution room without introducing silent misreads.

## Schema Evolution Assessment

- Forward compatible: Partially. The header JSON is forward-friendly for additive metadata, but only if the spec explicitly allows unknown properties and keeps required fields stable.
- Backward compatible: Partially. The outer frame can be backward-readable if v1 defines a strict prelude and reserves future fields cleanly. The current document implies this but does not fully specify it.
- Storage name stability: Directionally correct. Persisting `snapshotStorageName` is the right move, but the document should treat it as a stable persisted identity, not a convenience field.

## Detailed Assessment By Topic

### Stored Envelope Framing

The framing strategy is right in principle:

1. Small binary prelude.
2. Human-inspectable JSON header.
3. Raw payload bytes after that.

That is a good v1 compromise between supportability and efficiency.

The risk is that the design currently names the parts without specifying the wire contract precisely enough. Persisted formats become difficult to evolve when the first version is only described structurally and not byte-for-byte.

Recommendation:

Define the prelude as a strict contract in the architecture document, including validation behavior and size limits.

### Serializer Identity Strategy

This is the weakest current part of the design.

The provider is intentionally not serializing typed snapshot state itself. It stores upstream `SnapshotEnvelope.Data` bytes. That boundary is correct.

Because the provider stores opaque payload bytes, the persisted serializer identity becomes more important, not less. The provider cannot infer payload semantics later.

Recommendation:

Persist the exact serializer identity that produced the payload bytes. If JSON is the default, that default should resolve to a concrete persisted identifier, not merely the token `json`.

### Versioning Approach

The design is right to keep provider framing versioned separately from the upstream payload format. The remaining task is to prevent accidental conflation.

Bad outcome to avoid:

1. Bumping frame version because a new header field was added.
2. Bumping frame version because the default JSON serializer changed behavior.
3. Leaving frame version unchanged while changing the binary prelude incompatibly.

Recommendation:

Add explicit version-bump rules to the architecture so implementers do not improvise later.

### Compression Boundary

This is sound for v1.

The key benefit is that future readers can always discover the compression algorithm before touching the payload bytes. That is exactly the right property for restart and upgrade scenarios.

The remaining weakness is diagnostic clarity. A stored integrity checksum would materially improve supportability and migration safety without changing the main design.

### Forward Compatibility Choices

The design is close to sound here, but one more decision is needed: whether the header JSON is a tolerant schema or a strict schema.

For v1, it should be tolerant in one direction only:

1. Unknown properties are ignored.
2. Missing required properties are fatal.
3. Unknown enum-like values such as compression or serializer identifiers are fatal.

That is the correct balance for a provider-owned persisted envelope.

## Recommended Header Shape Adjustments

The current proposed header fields are mostly reasonable. I would tighten them as follows:

1. Keep `storageFormatVersion`.
2. Replace `payloadFormat` with `payloadSerializerId`.
3. Keep `compressionAlgorithm`.
4. Keep `snapshotStorageName`.
5. Keep `streamKey`, but only if it is a canonical persisted representation rather than a casual string form.
6. Keep `version`.
7. Keep `writtenUtc`.
8. Keep `uncompressedPayloadBytes`.
9. Keep `storedPayloadBytes`.
10. Add a payload integrity field.

I would treat `reducerHash` as diagnostic or validation metadata, not as part of the core format identity story.

## Strongest Risks

1. The team ships a format whose binary prelude is not precise enough to be durable.
2. The team persists `json` as the serializer identifier and later discovers multiple incompatible payload writers all claim the same value.
3. A future additive header field causes unnecessary frame-version churn because versioning rules were never written down.
4. Corruption, codec mismatch, and serializer mismatch become operationally hard to separate because no integrity field exists.
5. `streamKey.ToString()` accidentally becomes part of the storage contract even though it was not designed as one.

## Soundness for v1

Yes, conditionally.

The persisted-format design is sound for v1 if the team makes the following changes before treating the format as stable:

1. Specify the outer frame prelude as a real binary contract.
2. Persist a concrete serializer identity instead of only `payloadFormat`.
3. Define explicit version-bump rules for frame versus payload evolution.
4. Specify tolerant header evolution rules.
5. Add an integrity field for payload corruption diagnosis.

If those points are addressed, the design has a good v1 balance of inspectability, pluggability, and future evolution. If they are not addressed, the format will probably work initially but accumulate ambiguity exactly where persisted systems usually become expensive to support.

## CoV: Serialization Verification

1. The review stays grounded in the confirmed requirement that payload serialization is pluggable and JSON is the default: verified.
2. The recommendations focus on persisted-format and serialization boundaries, not broader cloud or repository design: verified.
3. The critique distinguishes outer-frame evolution from payload-serialization evolution and snapshot contract identity: verified.
