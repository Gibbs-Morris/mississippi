# Developer Experience Review

## Summary

- DX impact: Neutral trending positive. The feature story is strong, but the current plan still leaves first-time adopters with avoidable ambiguity around registration, serializer selection, and failure handling.
- Findings: 4 must-fix, 4 should-fix, 3 could-fix, 2 won't-fix.
- Verdict: CHANGES REQUESTED

## Usage Walkthrough

Primary adopter scenario: an existing Mississippi team already using Tributary snapshots with Cosmos wants larger payload support without changing domain code.

Expected consumer flow:

1. Find one obvious registration method for the Blob provider.
2. Configure only the minimum required options.
3. Keep JSON as the default unless they have a deliberate reason to choose another serializer.
4. Start the app and immediately understand any configuration mistake from the startup error.
5. Write, read, prune, and delete snapshots through the existing Tributary contract with no Blob-specific domain changes.

The plan is close to that pit-of-success path, but four friction points remain:

1. The registration surface is described as Cosmos-like, but it does not yet define the canonical happy-path overload or how the overloads should be explained to consumers.
2. `PayloadSerializerFormat` is exposed as a user-facing option, but the plan does not yet make the selection rules obvious when multiple serializers are registered.
3. The plan says startup should fail fast, but it does not yet require actionable error messages that tell the user which option, service registration, or configuration key is wrong.
4. Duplicate-version and decode-failure behavior is architecturally discussed, but the consumer-facing exception and diagnostic experience is still underspecified.

## DX Concerns

### Must Address

| # | Concern | Impact on Consumer | Recommendation |
|---|---------|-------------------|----------------|
| 1 | The plan does not define the primary registration path even though it proposes multiple `AddBlobSnapshotStorageProvider` overloads. | First-time adopters will not know which overload is the intended happy path versus advanced usage, which weakens discoverability and increases setup mistakes. | Add a plan-level requirement that names one canonical registration path for docs and samples, then explicitly describes the purpose of each additional overload. If all Cosmos-style overloads are kept, the plan should say so deliberately rather than implicitly. |
| 2 | Startup validation is present in principle but not as a user-facing contract. | Consumers will hit startup failures without clear guidance on whether the problem is `ContainerName`, the keyed `BlobServiceClient`, `PayloadSerializerFormat`, or container initialization mode. | Require explicit, actionable startup error messages and tests for each invalid configuration case. Each message should identify what is wrong, where it was read from, and what the user should change next. |
| 3 | Serializer selection is still ambiguous from the consumer's point of view. | A team with multiple registered serializers can accidentally depend on hidden DI order or unclear matching rules, which makes the API easy to misuse and hard to trust after restart. | Make the plan state the exact user-facing resolution rule for `PayloadSerializerFormat`: one and only one registered serializer must match; zero matches or multiple matches must fail startup; the failure message must name the configured format and the discovered candidates. |
| 4 | The error experience for duplicate-version conflicts and corrupt or unreadable blobs is not concrete enough yet. | Consumers need to know whether to retry, treat the failure as a concurrency issue, fix configuration, or investigate data corruption. Without that guidance, the API is technically correct but operationally unfriendly. | Add a DX acceptance item for failure semantics: duplicate-version writes must surface as a clearly non-transient conflict, and decode failures must report the stored serializer id, compression mode, frame version, and blob identity in both the exception message and structured logs. |

### Should Improve

| # | Concern | Impact on Consumer | Recommendation |
|---|---------|-------------------|----------------|
| 1 | The plan says the provider will mirror Cosmos, but it does not give adopters a direct Cosmos-to-Blob translation guide. | Existing users will understand the new feature faster if they can map familiar Cosmos concepts to the Blob equivalents without reading architecture prose. | Add a short parity checklist or mapping table in the plan or follow-on docs covering overloads, option names, initializer behavior, and expected `Format` behavior. |
| 2 | The public `Format` contract is not called out in the plan. | `ISnapshotStorageProvider.Format` is part of the public surface, so leaving the Blob value implicit creates avoidable uncertainty for diagnostics, tests, and any consumer logic that reports provider format. | Lock the Blob provider's `Format` value into the plan and treat it as part of the contract-parity review with Cosmos. |
| 3 | Diagnostics are described well for operators, but not yet as part of the user journey. | A developer adopting the provider should know how to confirm that the stored snapshot used the intended serializer and compression settings without reverse-engineering the blob frame. | Add a plan requirement for one user-facing verification path: either a documented inspect-the-blob walkthrough or an acceptance criterion that proves metadata visibility and the absence of misleading `Content-Encoding`. |
| 4 | `ContainerInitializationMode` is a good option, but its intended audience is not yet obvious. | Least-privilege teams and local developers will make different choices here, and the default can feel arbitrary unless the guidance is explicit. | Add consumer guidance that explains when to keep `CreateIfMissing`, when to switch to `ValidateExists`, and what startup failure should be expected in the validation-only mode. |

### Could Fix

| # | Concern | Impact on Consumer | Recommendation |
|---|---------|-------------------|----------------|
| 1 | The options list is clear, but there is no committed minimal configuration example in the plan. | Teams adopt provider features faster when they can see the smallest viable configuration shape before reading detailed option descriptions. | Add one no-frills configuration example to the delivery plan or documentation scope: container name, default serializer, default compression, and the keyed client path. |
| 2 | The plan assumes keyed client reuse will feel familiar, but Blob client sharing is less self-evident than Cosmos client sharing. | Consumers may not immediately understand why `BlobServiceClientServiceKey` exists or when they should override it. | Add one focused explanation for the service-key option and when to leave it alone versus override it to share infrastructure registrations. |
| 3 | Hashed blob naming is the right technical choice, but it reduces casual storage-browser discoverability. | Operators may initially think blobs are opaque or untraceable if the plan does not tell them where the readable identity lives. | Add one sentence to the user-facing guidance that explains the tradeoff and points operators to the canonical stream identity in the uncompressed header and optional diagnostic metadata. |

### Won't Fix For V1

| # | Concern | Reason to Defer | Recommendation |
|---|---------|-----------------|----------------|
| 1 | Human-readable blob names or stream names embedded directly in blob paths. | The confirmed architecture already chose bounded hashed prefixes for stability and stream-safe listing. Reopening that choice would redesign the feature instead of polishing the consumer experience. | Keep hashed naming in v1 and compensate with better inspection guidance. |
| 2 | Mixed-provider orchestration, per-snapshot compression heuristics, or automatic serializer fallback. | These would expand the feature beyond the confirmed requirement of a Blob-backed alternative provider with predictable provider-wide behavior. | Keep the v1 story narrow and explicit: one provider, one configured serializer selection rule, one provider-wide compression mode. |

## Positive DX Choices

- Mirroring the Cosmos registration shape is the right default because it lets existing adopters reuse their mental model instead of learning a new setup pattern.
- Defaulting to JSON keeps the happy path simple and avoids forcing serializer decisions onto teams that only need larger payload support.
- Persisting the concrete serializer identity is a strong pit-of-success choice because it prevents silent fallback after restart.
- `CreateIfMissing` versus `ValidateExists` is a good options split because it respects both local developer convenience and least-privilege production environments.
- Keeping the blob header uncompressed is a good DX decision because it preserves inspectability during troubleshooting.

## CoV: DX Verification

1. Usage walkthrough completed without confusion: not yet verified. Registration priority, serializer resolution, and failure messaging still need to be made explicit in the plan.
2. Error scenarios produce actionable messages: not yet verified. The architecture names the failure cases, but the plan does not yet require concrete user-facing messages.
3. API consistency with existing repo patterns: partially verified. The proposed overload shape, keyed-client usage, hosted initialization, and Cosmos-style setup model align with the existing provider pattern, but the Blob provider's contract details still need to be locked down explicitly.
