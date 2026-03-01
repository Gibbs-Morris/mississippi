# 03 Decisions

## D1 — Host builder API style
- Decision statement: Support both fluent and lambda-config entrypoints for host builders.
- Chosen option: C (explicitly aligned with user direction).
- Rationale:
  - User preference: “both” and align with .NET + Orleans style.
  - Repo precedent already uses lambda-config over a concrete builder (`AddInletBlazorSignalR(Action<InletBlazorSignalRBuilder>)`).
- Evidence:
  - `src/Inlet.Client/InletBlazorRegistrations.cs`
  - `src/Inlet.Client/InletBlazorSignalRBuilder.cs`
- Risks + mitigations:
  - Risk: API duplication/confusion.
  - Mitigation: choose one canonical docs-first path and position the other as convenience overload.
- Confidence: High.

## D2 — Builder contracts location
- Decision statement: Place builder interfaces in area-specific abstractions projects.
- Chosen option: B.
- Rationale:
  - Matches existing contract layering and package boundaries.
  - Keeps dependencies minimal and prevents a monolithic abstraction package.
- Evidence:
  - `abstractions-projects.instructions.md`
  - Existing project layout under `src/*.{Abstractions,Runtime,Gateway,Client}`
- Risks + mitigations:
  - Risk: discoverability across multiple packages.
  - Mitigation: provide unified docs page and host-level extension entrypoints.
- Confidence: High.

## D3 — Guardrail strictness
- Decision statement: Use fluent guidance with minimal but essential runtime validation.
- Chosen option: A.
- Rationale:
  - Reduces cognitive load and avoids heavy type-state complexity.
  - Preserves good DX while still failing fast for missing core dependencies.
- Evidence:
  - User choice A.
  - `framework-patterns.instructions.md` (developer-experience-first)
  - `service-registration.instructions.md` (sync + clear registration behavior)
- Risks + mitigations:
  - Risk: misconfiguration detected later than compile time.
  - Mitigation: `Build()` performs explicit prereq checks with actionable errors.
- Confidence: High.

## D4 — v1 design scope
- Decision statement: Design v1 as a complete model including host and feature builders.
- Chosen option: B.
- Rationale:
  - Provides a cohesive developer story and avoids fragmented adoption.
  - Directly matches user objective to simplify overall setup complexity.
- Evidence:
  - User choice B.
  - Existing generated composition across client/server/runtime supports full-surface orchestration.
- Risks + mitigations:
  - Risk: larger rollout scope.
  - Mitigation: stage implementation phases while keeping single architectural contract.
- Confidence: High.

## D5 — Alignment target with .NET/Orleans conventions
- Decision statement: Favor extension-method entrypoints returning builder interfaces, preserving `IServiceCollection` chainability.
- Chosen option: Derived from user free-text on API style.
- Rationale:
  - Closest to existing .NET configuration patterns and current repository conventions.
  - Enables incremental migration from `AddXyz(...)` APIs.
- Evidence:
  - User free-text response.
  - Existing extension-centric registration APIs throughout `src/*Registrations.cs`.
- Risks + mitigations:
  - Risk: potential ambiguity between old and new paths.
  - Mitigation: define “preferred” and “legacy-compatible” paths in docs/migration notes.
- Confidence: Medium (final naming and overloads still pending).

## D6 — Prototype lifecycle
- Decision statement: Use throwaway prototype code/tests and delete all prototype artifacts after proof.
- Chosen option: A.
- Rationale:
  - Matches explicit user preference.
  - Keeps repository surface focused on intentional final API/docs deliverables.
- Evidence:
  - User answer: `A) Delete all prototype code and tests`.
  - Build/quality policy context supports clean final tree and explicit validation.
- Risks + mitigations:
  - Risk: loss of executable proof artifact.
  - Mitigation: capture proof results and architecture rationale in docs and PR narrative before deletion.
- Confidence: High.

## D7 — Interface naming normalization
- Decision statement: Use corrected interface names with standard casing/spelling.
- Chosen option: A.
- Rationale:
  - Aligns with repository naming rules and public API clarity expectations.
- Evidence:
  - User answer: `A) Use normalized names`.
  - `naming.instructions.md` requires clear PascalCase naming conventions.
- Risks + mitigations:
  - Risk: mismatch with early drafts referencing typo variants.
  - Mitigation: keep normalized names as canonical in all plan/docs artifacts.
- Confidence: High.

## D8 — Domain binding model
- Decision statement: Use typed marker assembly domain binding (`AddDomain<TMarker>()`) as primary model.
- Chosen option: A.
- Rationale:
  - Compile-time safety and stronger discoverability than string keys.
  - Matches generator- and assembly-scanning-centric architecture.
- Evidence:
  - User answer: `A) Typed marker assembly`.
  - Existing registration/scanning patterns in aggregate/projection/event registries rely on assemblies and marker types.
- Risks + mitigations:
  - Risk: onboarding friction for users unfamiliar with marker-type pattern.
  - Mitigation: docs include concise marker-type examples and migration snippets.
- Confidence: High.

## D9 — Proposed docs location
- Decision statement: Add a new top-level `proposed-patterns/` docs section for non-live builder design guidance.
- Chosen option: A.
- Rationale:
  - Clear separation from current/live guidance.
  - Fully compatible with autogenerated sidebar model.
- Evidence:
  - User answer: `A) New top-level proposed-patterns section`.
  - `docs/Docusaurus/sidebars.ts` auto-generates from folder structure.
- Risks + mitigations:
  - Risk: users may treat proposed docs as production guidance.
  - Mitigation: explicit “proposed / not live yet” framing in frontmatter and overview.
- Confidence: High.

## D10 — Runtime builder host context model
- Decision statement: `IRuntimeBuilder` will wrap both DI registration and Orleans silo configuration concerns.
- Chosen option: B.
- Rationale:
  - User-selected model aligns with existing mixed `IServiceCollection` + `ISiloBuilder` patterns in runtime-facing packages.
- Evidence:
  - User answer: `B) Runtime builder wraps both`.
  - Existing runtime APIs in `BrooksRuntimeRegistrations` and `AqueductGrainsRegistrations` already expose `ISiloBuilder`-centric configuration.
- Risks + mitigations:
  - Risk: runtime builder becomes too broad.
  - Mitigation: define clear sub-areas within runtime builder (services vs silo hooks) and keep methods narrowly scoped.
- Confidence: High.

## D11 — Finalization model
- Decision statement: Use a hybrid finalization model; runtime includes explicit silo application bridge while client/gateway remain chainable with terminal semantics.
- Chosen option: C.
- Rationale:
  - Preserves ergonomic `Build()` where appropriate while acknowledging Orleans host realities.
- Evidence:
  - User answer: `C) Hybrid`.
  - Runtime patterns in samples and source rely on `builder.UseOrleans(silo => ...)` integration.
- Risks + mitigations:
  - Risk: conceptual inconsistency across host builders.
  - Mitigation: document a unified mental model (“compose -> apply”) with host-specific terminal methods.
- Confidence: High.

## D12 — Convenience overload scope
- Decision statement: Include host-level convenience overloads (`HostApplicationBuilder`/`WebApplicationBuilder`) in v1 design.
- Chosen option: A.
- Rationale:
  - Improves onboarding and aligns with existing convenience patterns.
- Evidence:
  - User answer: `A) Yes, include convenience overloads in v1`.
  - Existing `AddEventSourcing(HostApplicationBuilder ...)` precedent in Brooks runtime.
- Risks + mitigations:
  - Risk: larger API footprint.
  - Mitigation: keep convenience overloads as thin adapters over canonical builder contracts.
- Confidence: High.

## D13 — Domain composition semantics on host builders
- Decision statement: Host-level domain composition should add the full domain surface needed for that host; internally this should flow through builder orchestration and generated registration migration.
- Chosen option: User free-text direction (no strict A/B/C selection).
- Rationale:
  - Matches desired DX (“add domain to host and get what it needs”).
  - Preserves internal builder composition and future generator alignment.
- Evidence:
  - User free text in `DomainAPI` response.
  - Existing generated domain registration classes already aggregate per-host registrations.
- Risks + mitigations:
  - Risk: hidden magic if too implicit.
  - Mitigation: expose explicit feature-builder path in parallel and make generated mapping transparent in docs.
- Confidence: Medium-High.

## D14 — Feature section explicitness
- Decision statement: Require explicit feature sections for feature-level configuration.
- Chosen option: A.
- Rationale:
  - Ensures intent clarity and avoids accidental over-registration in advanced scenarios.
- Evidence:
  - User answer: `A) Require explicit feature sections`.
  - Framework/registration guidance favors explicit, discoverable composition.
- Risks + mitigations:
  - Risk: increased ceremony for simple cases.
  - Mitigation: provide domain-level convenience method for simple onboarding path.
- Confidence: High.

## D15 — Validation behavior
- Decision statement: Builder validation failures throw actionable exceptions at terminal/apply step.
- Chosen option: A.
- Rationale:
  - Strong fail-fast behavior with clear diagnostics improves operational confidence.
- Evidence:
  - User answer: `A) Throw exceptions at terminal step`.
  - Existing guidance emphasizes deterministic startup and explicit failures.
- Risks + mitigations:
  - Risk: stricter startup behavior for existing apps.
  - Mitigation: actionable messages and migration docs with required-step matrix.
- Confidence: High.

## D16 — Documentation focus split
- Decision statement: Proposed docs should be split into separate DX-focused and internal-implementation-focused pages.
- Chosen option: C.
- Rationale:
  - Aligns with docs page-focus rules and prevents mixed-audience confusion.
- Evidence:
  - User answer: `C) Split into both focus types`.
  - `documentation-page-focus.instructions.md` mandates separation when both concerns are needed.
- Risks + mitigations:
  - Risk: more pages to maintain.
  - Mitigation: enforce minimal page set and strong cross-linking.
- Confidence: High.

## D17 — AddDomain default behavior
- Decision statement: `AddDomain<TDomainMarker>()` auto-wires full required host surface by default; explicit feature sections are optional overrides.
- Chosen option: A.
- Rationale:
  - Delivers strongest onboarding DX while preserving advanced explicit control.
- Evidence:
  - User answer: `A) Auto full host wiring by default`.
  - Existing generated domain composition already aggregates required registrations per host surface.
- Risks + mitigations:
  - Risk: hidden behavior surprises.
  - Mitigation: docs include deterministic “what gets wired” matrix and override examples.
- Confidence: High.

## Open/blocked decisions
- Exact host entrypoint names and builder method naming scheme.
- How generated domain registration methods are surfaced through builders.
- How strict docs should be about deprecating direct `AddXyz(...)` usage.
- Whether client/gateway should expose both terminal `Build()` and immediate-apply modes or standardize on one.
- Whether convenience overloads should be in primary packages or separate `*.Hosting` style modules.

## CoV
- Claim: Decisions D1–D4 are user-confirmed.
  - Evidence: structured question responses in this planning session.
  - Triangulation: explicit options selected + free-text clarification.
  - Confidence: High.
  - Impact: Enables drafting architecture skeleton in `04-draft-plan.md`.

- Claim: D5 is inferred and should be validated with naming-level questions.
  - Evidence: user free text + current extension-method patterns.
  - Triangulation: one user statement + multiple registration files.
  - Confidence: Medium.
  - Impact: requires another clarifying batch before plan finalization.