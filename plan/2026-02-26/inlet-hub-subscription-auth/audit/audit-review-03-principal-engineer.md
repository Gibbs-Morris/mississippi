# Review 03 — Principal Engineer

**Reviewer persona:** Principal Engineer — evaluating repo consistency, maintainability, technical risk, SOLID adherence, test strategy adequacy, backwards compatibility.

*This review is based solely on `04-draft-plan.md` and the repository structure.*

---

## Feedback

### 1. `Inlet.Runtime` → `Inlet.Generators.Abstractions` reference crosses a conceptual boundary

- **Issue:** `Inlet.Generators.Abstractions` is a source-generator attribute package (targets `netstandard2.0` for analyzer compat). `Inlet.Runtime` is a runtime package. Introducing a runtime dependency on a generator-time package blurs the boundary between "generator inputs" and "runtime metadata."
- **Why it matters:** Future contributors may add generator-specific types to `Inlet.Generators.Abstractions` not realizing it's now a runtime dependency.
- **Proposed change:** Accept the reference but add a doc comment to `Inlet.Generators.Abstractions.csproj` noting it's referenced at both generator time and runtime. Alternatively, extract the attributes into `Inlet.Abstractions` (where `ProjectionPathAttribute` already lives) — but this would be a larger refactor.
- **Evidence:** `ProjectionPathAttribute` is in `Inlet.Abstractions` (a runtime package), not in `Inlet.Generators.Abstractions`. This inconsistency exists already — `[GenerateProjectionEndpoints]` is in Generators.Abstractions but `[ProjectionPath]` is in Abstractions.
- **Confidence:** Medium — the cross-reference is pragmatic and safe, but the boundary blur deserves documentation.

### 2. `SubscribeAsync` is accumulating too many responsibilities

- **Issue:** After the change, `SubscribeAsync` will validate inputs, resolve auth metadata, evaluate authorization policy, log auth decisions, AND delegate to the grain. This is 5+ responsibilities in one method.
- **Why it matters:** Testability and readability. Long methods with branching logic are harder to maintain.
- **Proposed change:** Extract the auth check into a private method (e.g., `AuthorizeSubscriptionAsync(path)`) or a dedicated service (e.g., `ISubscriptionAuthorizationService`). Keep `SubscribeAsync` as an orchestrator.
- **Evidence:** `InletHub.SubscribeAsync` is currently 10 lines. The auth check flow in the plan has 6+ branches. Extracting keeps methods focused.
- **Confidence:** High — this is straightforward SRP compliance.

### 3. Registry population is eagerly coupled to assembly scanning

- **Issue:** `ScanProjectionAssemblies` populates both brook and auth registries in a single pass. If either registry needs to be populated independently (e.g., auth registry from a different source), the coupling is inconvenient.
- **Why it matters:** The registries serve different consumers (brook registry for grains, auth registry for gateway). Coupling them to one scan method may create issues if the gateway and silo are separate hosts.
- **Proposed change:** Keep the single-pass optimization but ensure both registries can also be populated independently via their `Register` methods. The plan already shows this — the interface has `Register(path, entry)`. Just verify the implementation allows independent use.
- **Evidence:** `IProjectionBrookRegistry` can be populated independently via `Register(path, brookName)` as well as via `ScanProjectionAssemblies`. Same pattern should hold.
- **Confidence:** High — the plan implicitly supports this but should state it explicitly.

### 4. Test strategy needs edge case coverage

- **Issue:** The plan lists 5 test scenarios but misses several edge cases:
  - Path registered in brook registry but NOT in auth registry (projection has `[ProjectionPath]` but no auth attributes)
  - Path registered in auth registry but not in brook registry (shouldn't happen but defensive)
  - Multiple projections sharing the same path (collision)
  - Empty/null policy string in `[GenerateAuthorization]`
  - `AllowAnonymousOptOut` toggled after registration
- **Why it matters:** Mutation testing will find these gaps. Better to design them upfront.
- **Proposed change:** Add these edge cases to the test scenarios section.
- **Evidence:** The mutation testing instructions require maintained/raised score. Edge cases are prime mutant targets.
- **Confidence:** High.

### 5. Pre-1.0 breaking change is fine but needs clear PR title

- **Issue:** The plan correctly notes this is pre-1.0 and breaking changes are permitted. But the PR title should use `+semver: feature` (not `breaking`) since the repo is pre-1.0.
- **Why it matters:** `pr-description.instructions.md` requires semver suffix. Pre-1.0, `+semver: feature` is appropriate since breaking changes are expected and don't need a major bump.
- **Proposed change:** Note the PR title should use `+semver: feature`.
- **Evidence:** `backwards-compatibility.instructions.md`: "While GitVersion next-version is below 1.0.0, backwards compatibility MUST NOT be a constraint."
- **Confidence:** High.
