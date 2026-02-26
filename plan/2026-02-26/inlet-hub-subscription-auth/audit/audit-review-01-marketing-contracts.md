# Review 01 — Marketing & Contracts

**Reviewer persona:** Marketing & Contracts — evaluating public naming clarity, contract discoverability, package naming consistency, changelog/migration communication quality.

*This review is based solely on `04-draft-plan.md` and the repository structure.*

---

## Feedback

### 1. `ProjectionAuthorizationEntry` naming may confuse consumers with ASP.NET Core's `AuthorizationPolicy`

- **Issue:** The name `ProjectionAuthorizationEntry` is functional but could be confused with ASP.NET Core's existing auth types. Consumers seeing "authorization entry" may expect a full `AuthorizationPolicy` object.
- **Why it matters:** Developer discoverability and IntelliSense clarity. Wrong mental model leads to incorrect usage.
- **Proposed change:** Consider `ProjectionAuthorizationMetadata` to signal it's declarative metadata rather than an executable policy.
- **Evidence:** `GeneratedApiAuthorizationModel` in the generators uses "Model" suffix. "Metadata" aligns with the attribute-resolving semantic.
- **Confidence:** Medium — naming is subjective but "metadata" better describes what it is.

### 2. Interface name `IProjectionAuthorizationRegistry` is consistent with `IProjectionBrookRegistry`

- **Issue:** None — this is positive feedback.
- **Why it matters:** Package consumers will see both registries in IntelliSense and immediately understand the parallel pattern.
- **Evidence:** `IProjectionBrookRegistry` is the established naming convention.
- **Confidence:** High.

### 3. No changelog or migration guide mentioned

- **Issue:** The plan lists "Migration Steps for Consumers" but doesn't mention where this information will be published. No changelog entry, no doc update, no release notes draft.
- **Why it matters:** If force mode users upgrade without knowing the hub is now protected, their clients will break at connection time.
- **Proposed change:** Add a task to write a migration note in the changelog/release notes. Even pre-1.0, consumers need to know.
- **Evidence:** `pr-description.instructions.md` requires breaking changes to have migration guidance. The plan itself notes this is an intentional breaking change.
- **Confidence:** High.

### 4. `HubException` message content should be documented

- **Issue:** The plan says "use a generic message like 'Access denied for projection {path}'" but doesn't specify the exact message format.
- **Why it matters:** Programmatic error handling on the client may depend on message content. Even if the client currently treats all errors the same, future consumers might want to distinguish auth errors from other hub errors.
- **Proposed change:** Define a constant for the auth denial message in `InletHubConstants` (e.g., `SubscriptionAccessDeniedMessage`). Client code can optionally match against it.
- **Evidence:** `InletHubConstants` already defines `ProjectionUpdatedMethod` — it's the right place for hub protocol constants.
- **Confidence:** Medium — may be overengineering for pre-1.0, but establishes good practice.

### 5. No mention of NuGet package impact

- **Issue:** The plan adds a reference from `Inlet.Runtime` → `Inlet.Generators.Abstractions`. This means the `Mississippi.Inlet.Runtime` NuGet package will pull in `Mississippi.Inlet.Generators.Abstractions` as a dependency.
- **Why it matters:** Package consumers who don't use generators would get an unwanted transitive dependency (though it's tiny and dependency-free).
- **Proposed change:** Acknowledge this in the plan. It's acceptable given `Inlet.Generators.Abstractions` is zero-dependency, but should be a conscious decision.
- **Evidence:** `Inlet.Generators.Abstractions.csproj` has no dependencies. The package is effectively just attribute definitions.
- **Confidence:** Medium — worth noting, not blocking.
