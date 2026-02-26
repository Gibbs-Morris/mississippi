# Review 11 — Source Generator & Tooling Specialist

**Reviewer persona:** Source Generator & Tooling Specialist — evaluating Roslyn incremental generator correctness, caching, diagnostic emission, generated code readability, compilation performance, analyzer interaction.

*This review is based solely on `04-draft-plan.md` and the repository structure.*

---

## Feedback

### 1. No source generator changes required — minimal risk

- **Issue:** None — strong positive feedback.
- **Why it matters:** The plan achieves auth for subscriptions WITHOUT modifying any source generators. The existing `ProjectionEndpointsGenerator` already emits `[Authorize]` on generated controllers. The subscription auth path is entirely runtime-based (reflection scan + hub auth check). No generator caching, incremental pipeline, or diagnostic emission changes.
- **Evidence:** The "Files Changed" section lists no files in `src/Inlet.Gateway.Generators/`, `src/Inlet.Runtime.Generators/`, or `src/Inlet.Client.Generators/`.
- **Confidence:** High.

### 2. `[GenerateAuthorization]` attribute doc should mention runtime scanning

- **Issue:** The attribute is currently documented as a source-generator input. After this change, it's also consumed at runtime by `ScanProjectionAssemblies`. The attribute's documentation should reflect both consumption paths.
- **Why it matters:** A developer or future generator author modifying the attribute needs to know it's read at both compile-time (generators) and runtime (assembly scan).
- **Proposed change:** Add an XML doc remark to `GenerateAuthorizationAttribute` mentioning it's read at build time by generators and at runtime by the projection assembly scanner.
- **Evidence:** `ProjectionPathAttribute` is already consumed both ways (generators read it via Roslyn symbols; `ScanProjectionAssemblies` reads it via reflection). But `ProjectionPathAttribute` doesn't document this dual consumption either — an existing gap.
- **Confidence:** Medium — documentation improvement, not blocking.

### 3. No `[PendingSourceGenerator]` backlog impact

- **Issue:** None — verification.
- **Why it matters:** The plan doesn't add or require any new source generators. No impact on the pending generator backlog.
- **Evidence:** Plan uses runtime reflection (established `ScanProjectionAssemblies` pattern) instead of generator-emitted registry code.
- **Confidence:** High.

### 4. Future consideration: generator-emitted auth registry population

- **Issue:** The plan uses runtime reflection to populate the auth registry. An alternative is to have the source generator emit `Register(path, entry)` calls alongside the controller code it already generates. This would move auth metadata resolution from runtime to compile-time.
- **Why it matters:** Generator-time resolution enables earlier validation (diagnostics at build time if policy names are invalid), eliminates reflection cost, and enables trimming-safe publication.
- **Proposed change:** Not blocking for initial implementation. The runtime approach is consistent with `IProjectionBrookRegistry`. A future optimization could add generator-emitted registration. Note this as a follow-up.
- **Evidence:** `ScanProjectionAssemblies` is the established pattern. Changing to generator-emitted code would be a departure that should be considered holistically for both brook and auth registries.
- **Confidence:** Low — future enhancement, not current concern.

### 5. IDE experience unaffected

- **Issue:** None — verification.
- **Why it matters:** No source generator changes means no impact on IntelliSense, go-to-definition, or generated code readability. The existing generated controllers, DTOs, and mappers are unchanged.
- **Confidence:** High.
