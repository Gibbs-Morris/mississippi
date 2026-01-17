# Architect Review: Unified Source Generation Spec

**Reviewer:** Principal Software Architect (skeptical perspective)
**Date:** 2025-01-18
**Status:** CONDITIONAL APPROVAL — Critical gap must be resolved before implementation

---

## Executive Summary

The spec is **well-researched and technically sound in most areas**, with one
**critical architectural flaw** that must be resolved before implementation.
The proposed cross-project generation pattern (`Cascade.Contracts.Generated`
as a separate project receiving generated output from a Domain analyzer) is
**not technically feasible** with Roslyn incremental source generators.

### Verdict Table

| Area | Verdict | Notes |
| ---- | ------- | ----- |
| Problem Statement | ✅ **VALID** | Duplication is real; 9 DTOs + 332-line DI file |
| Goals | ✅ **VALID** | Orleans isolation is critical and well-articulated |
| Existing Generator Claims | ✅ **VERIFIED** | All 10 claims in verification.md confirmed |
| Cross-Project Generation | ❌ **CRITICAL GAP** | Not technically feasible as described |
| Attribute Naming | ⚠️ **REVISE** | Does not follow Orleans explicit-verb style |
| DI Generator Approach | ✅ **VALID** | Standard same-project generation; no issues |
| Phase 1 (Enable Existing) | ✅ **SAFE** | Low risk; incremental improvement |
| Phases 4-5 (Client DTOs/Actions) | 🔴 **BLOCKED** | Depends on infeasible cross-project pattern |

---

## Critical Gap: Cross-Project Source Generation

### What the Spec Claims

From [implementation-plan.md](implementation-plan.md#L77-L79):

> 1. **AnalyzerReference** — Contracts.Generated references Domain with
>    `ReferenceOutputAssembly="false"`, so Orleans types are visible to
>    the generator but NOT linked at runtime.

And from [handoff.md](handoff.md#L112-L118):

```xml
<ProjectReference Include="..\Cascade.Domain\Cascade.Domain.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

### Why This Is Incorrect

**Roslyn incremental source generators cannot emit code into external projects.**

The `SourceProductionContext.AddSource()` API adds generated files to the
**current compilation only**. When you reference a generator project via
`OutputItemType="Analyzer"`:

1. The generator DLL is loaded into the Roslyn compiler
2. The generator runs **in the context of the consuming project**
3. Generated code is added **to the consuming project's compilation**
4. Generated files appear under `<consuming>/obj/<config>/<tfm>/generated/`

**Evidence:**

```csharp
// From ProjectionApiGenerator.cs line 663
spc.AddSource($"{projection.TypeName}Dto.g.cs", dtoSource);
```

This adds `*Dto.g.cs` to the **current** `SourceProductionContext`—there is no
API to target a different project.

### What Actually Happens with the Proposed Pattern

If `Cascade.Contracts.Generated` references `Cascade.Domain` with
`OutputItemType="Analyzer"`:

1. The generator from Domain runs during `Contracts.Generated` build
2. But it sees `Contracts.Generated`'s source code—which is **empty**
3. No `[UxProjection]` types exist in `Contracts.Generated`
4. **No DTOs get generated**

The generator cannot "reach into" Domain from a different project's context.

### Impact on Spec

This breaks **Phase 4** (Client DTO Generator) and **Phase 5** (Client Action
Generator) as currently designed. The Orleans-free `*.Generated` projects will
not receive generated code.

---

## Required Resolution: Revised Architecture

### Option A: Same-Project Emit with Dual-Output (Recommended)

Keep DTOs in Domain but generate **two versions**:

1. **Orleans-annotated version** — for silo use (current behavior)
2. **Orleans-stripped version** — suffixed `*ClientDto` in same output

Both emit to `Cascade.Domain/obj/generated/`:

```csharp
// Generated into Cascade.Domain
public sealed record ChannelSummary { [Id(0)] ... }  // Orleans version
public sealed record ChannelSummaryClientDto { ... } // Client version (stripped)
```

**Tradeoff:** Domain project still contains client DTOs, but they're generated
and Orleans-free. Client references Domain with `ReferenceOutputAssembly="true"`
for the ClientDto types only.

**Risk:** This violates the "no Orleans references in Client" constraint because
Domain has Orleans packages. However, if ClientDto types have no Orleans
*attributes*, they won't require Orleans at runtime.

**Mitigation:** Use `[EditorBrowsable(Never)]` on Orleans-annotated types to
discourage direct client use.

### Option B: Shared Abstractions Assembly

Create `Cascade.Projections.Abstractions`:

- Contains **interface definitions** for projection shapes
- No Orleans references
- Domain projections implement these interfaces
- Client references only the abstractions project
- Generator validates interface conformance

**Tradeoff:** Requires additional interfaces; more ceremony but cleaner boundary.

### Option C: MSBuild Post-Compile Copy (Discouraged)

Use an MSBuild target to copy generated `*ClientDto.g.cs` files from Domain's
`obj/` folder to `Contracts.Generated/Generated/` after Domain builds.

**Tradeoff:** Fragile; depends on build order and file paths. Not recommended.

### Recommendation

**Option A** (dual-output) is the most pragmatic. It:

- Maintains the generator's standard behavior
- Requires minimal spec changes
- Keeps client DTOs Orleans-attribute-free
- Allows proper Orleans isolation at the attribute level (not project level)

---

## Attribute Naming Review

### Current Proposal

| Proposed Attribute | Style |
| ------------------ | ----- |
| `[GenerateClientDto]` | `Generate` + noun |
| `[GenerateClientAction]` | `Generate` + noun |

### Orleans Style Reference

Orleans uses explicit verb prefixes indicating **what happens**:

| Orleans Attribute | Pattern |
| ----------------- | ------- |
| `[GenerateSerializer]` | Generate + capability |
| `[Immutable]` | Adjective (describes type) |
| `[Alias("...")]` | Noun (provides identity) |
| `[Id(n)]` | Noun (provides identity) |

### Recommended Changes

For **opt-in generation markers**, use explicit action verbs:

| Current Name | Revised Name | Rationale |
| ------------ | ------------ | --------- |
| `[GenerateClientDto]` | `[ExposeToClient]` | States intent (visibility), not mechanism |
| `[GenerateClientAction]` | `[GenerateClientDispatcher]` | Matches Orleans `[GenerateSerializer]` style |

Alternative:

| Current Name | Revised Name | Rationale |
| ------------ | ------------ | --------- |
| `[GenerateClientDto]` | `[ClientVisible]` | Declarative; indicates exposure |
| `[GenerateClientAction]` | `[ClientDispatchable]` | Declarative; indicates dispatch capability |

### My Preference

Use **declarative adjectives** that describe the capability rather than the
implementation:

- `[ClientVisible]` — this projection is visible to clients
- `[ClientDispatchable]` — this command is dispatchable from clients

This follows the `[Immutable]` pattern where the attribute describes the
**property** of the type, not the **mechanism**.

---

## Claim Verification Status

### Verified (No Issues)

| ID | Claim | Evidence |
| -- | ----- | -------- |
| C1 | AggregateServiceGenerator exists | Line 19: `IIncrementalGenerator` |
| C2 | Only UserAggregate has `[AggregateService]` | grep confirms |
| C3 | Generated IUserService is unused | Program.cs uses grain factory directly |
| C4 | ProjectionApiGenerator strips Orleans attrs | Lines 237-245 filter [Id], [GenerateSerializer] |
| C5 | Client uses manual Contracts DTOs | .csproj references confirm |
| C6 | 9 manual DTOs exist in Contracts | ls confirms file count |
| C7 | CascadeRegistrations has 80+ calls | 332 lines, 80+ registrations |
| C8 | Client is Orleans-free | No Orleans package refs |
| C9 | ProjectionPath drives discovery | InletBlazorSignalRBuilder.ScanProjectionDtos() |
| C10 | Generators respect internal accessibility | Lines 98-105 check DeclaredAccessibility |

### Unverified (New Finding)

| ID | Claim | Status | Impact |
| -- | ----- | ------ | ------ |
| C11 | Cross-project generation works via AnalyzerReference | ❌ INVALID | Blocks Phases 4-5 |

---

## Phase Risk Assessment

| Phase | Risk | Recommendation |
| ----- | ---- | -------------- |
| **Phase 1: Enable Existing Generators** | 🟢 Low | Proceed |
| **Phase 2: Verify Projection DTOs** | 🟢 Low | Proceed |
| **Phase 3: DI Generator** | 🟢 Low | Proceed (same-project emit works) |
| **Phase 4: Client DTO Generator** | 🔴 High | **BLOCKED** — resolve architecture first |
| **Phase 5: Client Action Generator** | 🔴 High | **BLOCKED** — depends on Phase 4 pattern |

### Recommended Phasing

1. Execute **Phases 1-3** immediately — they are safe and provide value.
2. Pause at Phase 4 and **resolve the cross-project architecture** with
   stakeholder input (Option A vs B).
3. Resume Phases 4-5 after architecture decision.

---

## Open Questions Requiring Decision

### Q1: Client DTO Emit Strategy

**Options:**

| Option | Pros | Cons |
| ------ | ---- | ---- |
| A: Dual-output in Domain | Simple; uses existing generator model | Domain grows; boundary is attribute-based not project-based |
| B: Abstractions project | Clean boundary | More ceremony; requires interfaces |
| C: MSBuild copy | True project separation | Fragile; build-order dependent |

**Recommended:** Option A (dual-output) with `[EditorBrowsable(Never)]` on
Orleans types.

### Q2: Attribute Naming Convention

**Options:**

| Style | Example | Pro | Con |
| ----- | ------- | --- | --- |
| Action verb | `[GenerateClientDto]` | Clear mechanism | Implementation-focused |
| Declarative | `[ClientVisible]` | Intent-focused | Indirect mechanism |
| Hybrid | `[ExposeToClient]` | Explicit action | Slightly verbose |

**Recommended:** `[ClientVisible]` and `[ClientDispatchable]` for Orleans-style
declarative approach.

---

## Summary of Required Changes

1. **CRITICAL:** Revise Phases 4-5 to use same-project dual-output pattern.
2. **RECOMMENDED:** Rename attributes to `[ClientVisible]` / `[ClientDispatchable]`.
3. **UPDATE:** Correct handoff.md and implementation-plan.md to reflect feasible
   architecture.
4. **ADD:** Verification question for cross-project emit feasibility (now answered).

---

## Approval

| Condition | Status |
| --------- | ------ |
| Phases 1-3 | ✅ APPROVED — Proceed immediately |
| Phases 4-5 | 🟡 CONDITIONAL — Resolve cross-project architecture first |
| Attribute naming | 🟡 ADVISORY — Recommend revision but not blocking |

**Next Action:** Product/technical stakeholder decision on Option A vs B for
client DTO architecture.
