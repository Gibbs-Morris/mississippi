# Decisions

## Decision 1

### Decision statement

Use one shared `.agent.md` file per agent across VS Code and GitHub.com.

### Chosen option

X

### Rationale

Official docs support a shared frontmatter subset across both surfaces. Using one file per agent avoids drift, reduces maintenance cost, and makes the family easier to reason about.

### Evidence

- GitHub Docs: `Custom agents configuration` says the YAML frontmatter properties apply across GitHub.com, Copilot CLI, and supported IDEs unless otherwise noted.
- GitHub Docs also state that unsupported VS Code-specific fields like `argument-hint` and `handoffs` are ignored on GitHub.com for compatibility.
- VS Code Docs: `Custom agents in VS Code` define `.agent.md` files under `.github/agents/` as the standard workspace-level custom-agent format.

### Risks and mitigations

- Risk: a VS Code-only affordance could accidentally become required for correctness.
- Mitigation: keep correctness in the shared prompt body and use `handoffs` or `argument-hint` only as optional UX improvements.

### Confidence rating

High

## Decision 2

### Decision statement

Leave `target` unset for every agent in the family.

### Chosen option

X

### Rationale

Unset `target` is the documented cross-surface default. Explicitly setting `target` would reduce portability without solving a current problem.

### Evidence

- GitHub Docs: if `target` is unset, the agent defaults to both environments.
- VS Code Docs: `target` is optional and used only to scope an agent to `vscode` or `github-copilot` when needed.

### Risks and mitigations

- Risk: a future surface-specific capability could tempt a split.
- Mitigation: treat surface-specific splits as an exception requiring explicit justification in a later change.

### Confidence rating

High

## Decision 3

### Decision statement

Use the family prefix `vfe` for filenames, agent names, and manifest references.

### Chosen option

X

### Rationale

`vfe` is short, unique in the current repo, and maps cleanly to `verification-first enterprise`, which matches the requested operating philosophy.

### Evidence

- Repo agent inventory already uses prefixes such as `flow`, `epic`, and `CoV`, so a new prefix is needed to avoid collisions.
- No existing `.github/agents/*manifest*.md` file exists, so adding a clearly prefixed family manifest improves discoverability.

### Risks and mitigations

- Risk: the prefix could be slightly opaque to first-time users.
- Mitigation: the family manifest will explain the prefix once and mark the three entry points as Start here.

### Confidence rating

High

## Decision 4

### Decision statement

Make only the three entry agents human-visible and protect them from subagent-style invocation.

### Chosen option

X

### Rationale

The user wants exactly three clear human entry points. The documented visibility controls support that cleanly.

### Evidence

- VS Code Docs: `user-invocable: false` hides an agent from the picker while still allowing subagent use; `disable-model-invocation: true` prevents subagent invocation.
- GitHub Docs describe the same semantics for `user-invocable` and `disable-model-invocation`.

### Risks and mitigations

- Risk: entry agents might still be overused if their descriptions are vague.
- Mitigation: give each entry agent a crisp description and explicit start-here wording in the manifest.

### Confidence rating

High

## Decision 5

### Decision statement

Keep internal specialists hidden but callable where the surface supports subagents.

### Chosen option

X

### Rationale

This minimizes picker clutter while preserving orchestration in VS Code and any compatible future surface behavior.

### Evidence

- VS Code subagent docs: hidden agents should use `user-invocable: false`; by default, agents without `disable-model-invocation: true` remain available as subagents.
- GitHub Docs: `user-invocable: false` prevents manual selection and allows programmatic access.

### Risks and mitigations

- Risk: GitHub.com might not expose the same orchestration richness as VS Code.
- Mitigation: make specialist prompts self-contained and callable by name, but do not depend on cross-agent orchestration for GitHub.com correctness.

### Confidence rating

High

## Decision 6

### Decision statement

Use `model: GPT-5.4 (copilot)` on all family agents and do not add a fallback list initially.

### Chosen option

X

### Rationale

`GPT-5.4` is officially supported, and VS Code requires qualified naming for explicit model selection. A fallback list is unnecessary unless runtime evidence or docs justify it.

### Evidence

- GitHub Docs: `Supported AI models in GitHub Copilot` lists `GPT-5.4` as GA and included across clients.
- VS Code Docs: `Custom agents in VS Code` require qualified model names such as `GPT-5 (copilot)` in agent and handoff metadata.

### Risks and mitigations

- Risk: a user or organization policy might disable that model.
- Mitigation: the prompts must still work when the surface ignores or overrides the model setting; no correctness depends on the configured model.

### Confidence rating

High

## Decision 7

### Decision statement

Do not constrain `tools` for internal specialists, and only add deliberate VS Code affordances on entry agents when they are safe to ignore on GitHub.com.

### Chosen option

X

### Rationale

Official docs say omitting `tools` enables all available tools. That best matches the requirement not to over-constrain environments with useful MCP and extension tools.

### Evidence

- GitHub Docs: omitting `tools` enables all available tools; specific tool names merely filter the available set.
- VS Code tool docs emphasize that agents draw from built-in, MCP, and extension tools that may vary by environment.

### Risks and mitigations

- Risk: entry-agent subagent invocation may be less tightly controlled than a fully enumerated `agents` list.
- Mitigation: entry-agent prompts will name the intended internal specialists explicitly, and handoffs remain limited to the three entry agents.

### Confidence rating

Medium

## Decision 8

### Decision statement

Add `handoffs` only on the three entry agents, always with `send: false`.

### Chosen option

X

### Rationale

This improves VS Code usability while preserving human control and remaining harmless on GitHub.com, where the field is ignored.

### Evidence

- VS Code Docs: `handoffs` create guided sequential workflows and support `send: false` to preserve user control.
- GitHub Docs: `handoffs` are ignored on GitHub.com for compatibility.
- Repo precedent: existing `epic-planner` and `epic-builder` agents already use handoffs.

### Risks and mitigations

- Risk: users may expect handoff buttons on GitHub.com.
- Mitigation: the manifest will explicitly describe handoffs as a VS Code enhancement, not a GitHub.com dependency.

### Confidence rating

High

## Decision 9

### Decision statement

Avoid `metadata` in the new family.

### Chosen option

X

### Rationale

`metadata` does not materially improve correctness for this workflow and is not necessary to satisfy the user’s requirements. Avoiding it keeps the new family closer to the smallest documented compatible subset.

### Evidence

- GitHub Docs list `metadata` as an annotation feature that is not used in VS Code and other IDE custom agents.
- Existing repo agents are inconsistent about using `metadata`, so there is no repo-wide need to continue it here.

### Risks and mitigations

- Risk: some existing repo tooling may have been informally reading metadata.
- Mitigation: the manifest will carry the family-level documentation instead of relying on hidden machine-readable metadata.

### Confidence rating

Medium

## Decision 10

### Decision statement

Require every non-trivial `vfe` workflow to use a file-backed working directory under `./plan/<work-id>/` for handoff and state persistence.

### Chosen option

X

### Rationale

Large problem spaces lose important context when plans, reviews, PR comment handling, docs notes, and automation ideas live only in chat. A persistent working directory gives every phase a stable artifact contract.

### Evidence

- Updated user requirement explicitly asks for a working directory under `./plan/xxxxx` with set Markdown formats for plans, reviews, comments, and docs suggestions.
- This repository already uses `./plan/YYYY-MM-DD/<name>/` as a durable planning-artifact location for planner workflows.

### Risks and mitigations

- Risk: agents may overwrite useful history if the file set is too loose.
- Mitigation: require a stable numbered Markdown set and prefer additive numbered files for repeated review rounds or non-trivial threads.

### Confidence rating

High

## Decision 11

### Decision statement

Restrict subagent delegation within this workflow to `vfe` family agents by default.

### Chosen option

X

### Rationale

The family is intended to be a self-contained coordinator-worker system. Allowing it to reach outside the family by default would weaken remit control, increase naming ambiguity, and undermine the manifest's role as the authoritative specialist map.

### Evidence

- Updated user requirement explicitly asks for guard rails so the workflow uses agents in the family and does not use agents without the prefix name when using subagents.
- The plan already treats `vfe` specialists as the internal worker set for the three entry agents.

### Risks and mitigations

- Risk: a needed specialty may not yet exist inside `vfe`.
- Mitigation: the current agent should continue linearly or the user should explicitly approve an external-agent exception.

### Confidence rating

High

## Decision 12

### Decision statement

Require material plan changes during execution to rerun the plan review process before Build continues.

### Chosen option

X

### Rationale

Verification-first delivery breaks down if the approved plan can change materially without the same scrutiny that produced the original approved plan.

### Evidence

- Updated user requirement explicitly asks to ensure the whole review process is done for each plan change made for the build.
- The plan already positions approval, review, and adjudication as gates before execution.

### Risks and mitigations

- Risk: review overhead could slow trivial edits.
- Mitigation: require the full relevant review set for narrow changes and the full roster for cross-cutting changes, but always require written synthesis before continuing.

### Confidence rating

High

## Decision 13

### Decision statement

Treat the user-provided workflow diagrams as the canonical modeled process for the `vfe` family and embed the key ones directly in the final plan.

### Chosen option

X

### Rationale

The prior plan described the workflow well in prose, but it did not yet preserve the user's exact workflow shape as the authoritative model. Making these diagrams normative removes ambiguity and gives the builder a clearer target.

### Evidence

- Updated user requirement provides the desired end-to-end workflow, family structure, planning flow, build loop, commit-shaping logic, branch verification loop, PR comment loop, commit-by-commit review view, cross-surface model, and CoV application.
- The user explicitly states that the three most useful diagrams to embed directly in prompts are the end-to-end workflow, per-slice build loop, and agent family structure.

### Risks and mitigations

- Risk: the plan could become verbose or redundant.
- Mitigation: mark the workflow section as authoritative, note prompt-embedding priority, and allow compact prompt versions while preserving the canonical plan record.

### Confidence rating

High

## Decision 14

### Decision statement

Add a default entry-to-specialist routing matrix that matches the canonical family structure, while still allowing conditional narrowing for smaller tasks.

### Chosen option

X

### Rationale

The original plan had remit guidance and conditional invocation examples, but it did not yet encode the user's explicit entry-to-specialist structure as the default routing model.

### Evidence

- Updated user requirement provides a direct Plan / Build / Review to specialist topology.
- The family already uses a coordinator-worker model with entry agents and internal specialists, so the routing matrix is a natural strengthening rather than a redesign.

### Risks and mitigations

- Risk: a rigid routing matrix could force unnecessary specialists on very small tasks.
- Mitigation: make the routing matrix the default and allow conditional narrowing where appropriate, without reaching outside the family.

### Confidence rating

High

## Decision 15

### Decision statement

Add a failure and recovery protocol covering build failure caps, specialist remit violations, missing artifacts, non-converging loops, and circular handoff circuit-breakers.

### Chosen option

Accept (synthesis M1, M3)

### Rationale

The plan described only the happy path. Real-world agent workflows encounter repeated failures, non-converging reviews, and circular handoffs. Without defined recovery behavior, agents would either hallucinate recovery steps or get stuck indefinitely.

### Evidence

- The repo's `build-issue-remediation.instructions.md` already caps fix attempts at 5, establishing precedent.
- Principal Engineer review (#1, #2) identified both the missing failure protocol and the circular handoff risk.

### Risks and mitigations

- Risk: rigid caps could stop agents that are making progress slowly.
- Mitigation: caps are per-issue (build) and per-amendment (review), not per-workflow. The final escalation is to the user, not to abandonment.

### Confidence rating

High

## Decision 16

### Decision statement

Require `01-plan.md` to include a `## Status` field (`draft | in-review | approved | superseded`) and require VFE Build to verify status is `approved` before proceeding.

### Chosen option

Accept (synthesis M2)

### Rationale

VFE Build says "read approved plan artifacts" but nothing in the working directory distinguished an approved plan from a draft. A simple status field prevents premature implementation.

### Evidence

- Solution Engineering review (#2) identified the gap.
- The working directory already requires a rigid structure with Status as section 2 in the format rules.

### Risks and mitigations

- Risk: agents might forget to update the status field.
- Mitigation: the format rules already require Status; this merely pins the allowed values for `01-plan.md` specifically.

### Confidence rating

High

## Decision 17

### Decision statement

Add concurrent-write guard, sensitive-information guardrail, resumption protocol, enhanced `09-handoff.md` as status dashboard, triviality threshold, materiality definition, specialist conflict resolution priority, prompt budget guidance, Compliance/Governance to VFE Plan routing, argument-hint values, and corroboration definition.

### Chosen option

Accept (synthesis M4-M8, S1-S7, C3-C4, C6)

### Rationale

Third review round identified 8 must-fix and 7 should-fix genuine gaps ranging from race conditions on parallel specialist writes to missing definitions for key terms ("material", "trivial", "corroborated"). Each fix is small, targeted, and strengthens real-world viability.

### Evidence

- 12 independent persona reviews with deduplicated synthesis.
- Concurrent-write gap mirrors distributed-systems best practices (no shared mutable state without coordination).
- Sensitive-info guardrail mirrors existing `.scratchpad` policy.
- Triviality threshold addresses adoption friction identified by Solution Engineering and DX reviewers independently.

### Risks and mitigations

- Risk: additional rules increase prompt length.
- Mitigation: prompt budget guidance (S6) was added in the same batch, and specialist template flexibility (C6) reduces specialist prompt bloat.

### Confidence rating

High

## Decision 18

### Decision statement

Adopt a three-pass review discipline as the standard review process for all specialist review cycles across VFE Plan, VFE Build, and VFE Review. Every review cycle runs three consecutive passes: review → adjudicate → fix → review → adjudicate → fix → review → adjudicate → fix. Early exit is permitted only if a pass produces zero findings.

### Chosen option

Accept (user directive — Amendment 5)

### Rationale

Each review pass discovers issues missed by the previous pass. In practice, three successive review rounds consistently surface diminishing but real findings: the first pass catches structural issues, the second catches interaction effects from fixes, the third catches edge cases and consistency gaps. One pass is insufficient; more than three shows rapidly diminishing returns.

### Evidence

- Empirical observation across four rounds of plan review in this session: each round found genuine issues the previous round missed (Amendment 1: 15 findings, Amendment 2: 12 findings, Amendment 3: 15 findings, Amendment 4: 15 findings).
- The existing Failure Protocol already capped non-converging loops at 3 cycles, indicating 3 was already the natural boundary.
- Whole-branch verification already used "up to 3 passes" — this decision standardizes the pattern across all review points.

### Risks and mitigations

- Risk: 3 passes per review cycle increases total review time.
- Mitigation: early-exit rule (skip remaining passes if a pass produces zero findings) avoids unnecessary work. In practice, pass 3 is often empty or near-empty.
- Risk: prompt budget pressure from triple invocations.
- Mitigation: each pass is recorded as a separate artifact, keeping individual prompt context focused.

### Confidence rating

High

## Decision 19

### Decision statement

Add 4 new specialists (Event Sourcing / CQRS, Source Generator / Tooling, Developer Experience, Naming / Packaging) and expand 3 existing specialist remits (Solution Architect, Performance / Distributed Systems, Data Architect) to close coverage gaps identified by comparing the VFE specialist roster against the flow Planner's 12-persona review model.

### Chosen option

Accept — add new specialists and expand existing remits rather than relying on adjacent specialists to partially cover these domains.

### Rationale

The flow Planner uses 12 purpose-built review personas including Mississippi-framework-specific ones (Event Sourcing & CQRS, Source Generator & Tooling, Data Integrity & Storage) and enterprise generalists (Marketing & Contracts, Developer Experience, Solution Engineering). The VFE plan had 18 specialists but lacked dedicated coverage for:

1. **Event Sourcing / CQRS** — reducer purity, aggregate invariants, projection rebuild, snapshot versioning, saga compensation, command/event separation. The data-architect covered schema evolution generically but not the Mississippi event-sourcing domain model.
2. **Source Generator / Tooling** — Roslyn incremental source generators, diagnostic emission, generated code readability, compilation performance, `[PendingSourceGenerator]` alignment. No existing specialist covered this.
3. **Developer Experience** — API ergonomics, pit-of-success design, IntelliSense, registration ceremony, concepts to learn, migration friction. The principal-software-engineer mentioned "developer ergonomics" but without the consuming-developer perspective.
4. **Naming / Packaging** — public naming clarity, package naming consistency, NuGet identity, changelog/migration communication. No existing specialist focused on naming and packaging as a first-class concern.

Three existing specialists were expanded rather than replaced:
- **Solution Architect**: added adoption readiness, onboarding friction, ecosystem/standards compliance (from flow Planner's Solution Engineering persona).
- **Performance / Distributed Systems**: added Orleans actor-model specifics — grain lifecycle, reentrancy, single-activation, placement, turn-based concurrency, dead-letter handling (from flow Planner's Distributed Systems Engineer persona).
- **Data Architect**: added Cosmos-specific concerns — partition key design, cross-partition cost, idempotent writes, conflict resolution, TTL/retention (from flow Planner's Data Integrity & Storage persona).

### Evidence

- Flow Planner persona definitions in the agent mode instructions (12 personas with detailed remit descriptions).
- VFE specialist remit map (18 specialists prior to this change).
- Mississippi repo structure: `src/Inlet.*.Generators/`, `src/Brooks.*`, `src/Tributary.*`, `src/DomainModeling.*` confirm that event sourcing, source generators, and Cosmos storage are core concerns requiring dedicated specialist coverage.
- Repo instructions under `.github/instructions/orleans-serialization.instructions.md`, `.github/instructions/storage-type-naming.instructions.md`, `.github/instructions/naming.instructions.md` confirm these are active policy areas.

### Risks and mitigations

- Risk: 22 specialists increases prompt-budget pressure and round-trip time for review cycles.
- Mitigation: conditional invocation guidance already says entry agents invoke only relevant specialists per task. Most tasks will engage 8-12 specialists, not all 22.
- Risk: event-sourcing-cqrs remit overlaps with data-architect.
- Mitigation: remit map makes boundaries explicit: data-architect owns storage-level concerns (partition keys, retention, migration); event-sourcing-cqrs owns domain-model-level concerns (reducer purity, aggregate invariants, projection rebuild, saga compensation).

### Confidence rating

High

## Decision 20

### Decision statement

Encode the Chain-of-Verification (CoV) methodology from the Meta AI paper (Dhuliawala et al., 2023, arXiv:2309.11495) as a mandatory operating discipline for every agent in the VFE family, with three intensity levels and explicit prompt-template integration.

### Chosen option

Accept — embed the full 4-step CoV loop as a core family operating principle rather than treating verification as an optional best practice.

### Rationale

The Meta CoV paper demonstrates that structured self-verification — (1) generate baseline response, (2) plan verification questions, (3) execute verifications independently, (4) generate final verified response — significantly reduces hallucination in LLM outputs. The VFE family's mission is "verification-first enterprise," making this methodology a natural fit.

Key design decisions within this amendment:

1. **4-step loop is mandatory for substantive outputs**: Plans, design decisions, code changes, review findings, and specialist recommendations all require the full loop. This aligns with the paper's finding that the "Factored" variant (independent verification) produces the strongest results.

2. **Three intensity levels** (Full / Light / Skip): Not every output warrants full CoV. Routine housekeeping uses Light CoV; pure pass-through operations skip it entirely. This prevents the discipline from becoming a bottleneck.

3. **Prompt template expanded from 8 to 9 sections**: A dedicated "Chain-of-Verification requirements" section (section 5) was added to every agent's prompt template to make CoV expectations explicit rather than buried in general guardrails.

4. **Specialist output contract updated**: Non-trivial findings now require a CoV trace (what was verified and how), and specialists must self-verify every finding against repo evidence before returning it.

5. **Connection to specialist independence**: The paper's "Factored" variant — where each verification question is answered in complete isolation — directly justifies the VFE family's existing specialist independence rule (specialists don't read each other's findings during a round).

### Evidence

- Meta AI paper: Dhuliawala et al., 2023, "Chain-of-Verification Reduces Hallucination in Large Language Models", arXiv:2309.11495. Confirmed from arxiv.org abstract and HuggingFace papers page.
- The paper's 4-step methodology: (i) generate baseline, (ii) plan verification questions targeting factuality, (iii) execute verifications independently (Factored variant is strongest), (iv) generate final verified response.
- Existing plan already referenced CoV conceptually in diagram #10 and the CoV section at the bottom, but did not encode the actual 4-step methodology into agent behavior.
- The VFE family's specialist independence rule (specialists don't read each other's findings) is a direct instantiation of the Factored variant.

### Risks and mitigations

- Risk: Full CoV on every output could increase latency and token consumption.
- Mitigation: Three intensity levels ensure only substantive outputs get the full loop; routine operations use Light CoV.
- Risk: Agents might produce verbose CoV traces that clutter working-directory artifacts.
- Mitigation: Light CoV traces are one-line notes; Full CoV traces are structured subsections, not free-form dumps.

### Confidence rating

High

## Decision 21

### Decision statement

Adopt the CoV Mississippi Branch Review agent's diff-vs-main file-by-file review methodology as the standard whole-branch review procedure for VFE Build and VFE Review, adding a formal "Branch-vs-Main Review Methodology" section to the plan.

### Chosen option

Accept — encode the systematic file-by-file methodology rather than leaving "review branch wide" as an unstructured instruction.

### Rationale

The CoV Mississippi Branch Review agent uses a proven five-step methodology for whole-branch review: (1) compute the complete diff vs main, (2) build an explicit review checklist in deterministic order, (3) review one file at a time (full file at HEAD + diff + intent summary + issues with path/line), (4) apply a structured severity × category taxonomy, and (5) produce a coverage proof showing every file reached REVIEWED status.

The VFE plan previously said "review branch wide" and "review commit by commit" but did not prescribe *how* to systematically cover every changed file. This left the whole-branch review quality dependent on the agent's judgment about what to look at, rather than guaranteeing complete coverage.

Key additions:
1. **Branch-vs-Main Review Methodology section**: standalone methodology reference that both Build and Review link to.
2. **VFE Build step 14 updated**: now explicitly requires the diff-vs-main methodology with file checklist and coverage proof before specialist passes.
3. **VFE Review steps 3–5 restructured**: diff computation, file-by-file loop, and coverage proof are now numbered workflow steps rather than implicit.
4. **VFE Review step 9 added**: final output structure requirement (changed-file count, checklist, severity-sorted issues, totals, top risks).
5. **Diagram 6 rewritten**: shows the full flow from diff computation through file-by-file loop to specialist review passes.
6. **Issue taxonomy**: severity (BLOCKER/MAJOR/MINOR/NIT) × category (Correctness/Security/Concurrency/Performance/Reliability/API-Compatibility/Testing/Observability/Maintainability/Style) replaces the less granular grouping during review work. The existing blocking/important/optional/out-of-scope grouping is retained for final report output.

### Evidence

- CoV Mississippi Branch Review agent: `.github/agents/CoV-mississippi-branch-review-chat.agent.md` — defines the complete methodology including required git commands, one-file-at-a-time loop, status tracking, and coverage proof.
- The agent has been used successfully in this repository for branch reviews, validating the methodology's effectiveness.
- The VFE plan's existing specialist review discipline (3-pass, CoV) complements the file-by-file methodology — specialists add domain-specific depth after the baseline file review establishes complete coverage.

### Risks and mitigations

- Risk: file-by-file review on large branches (100+ files) could be very time-consuming.
- Mitigation: the methodology guarantees completeness, which is the VFE family's core value proposition. For very large branches, the file-by-file pass can tag files as "trivial/generated" and spend proportionally less time on them, while still marking them REVIEWED.
- Risk: redundancy between file-by-file findings and specialist findings.
- Mitigation: the plan specifies that the file-by-file loop runs first and specialist passes merge into the same taxonomy, so deduplication happens naturally during adjudication.

### Confidence rating

High

## Decision 22

### Decision statement

Conduct a 5-pass final review of the complete PLAN.md using all 12 specialist personas, fixing issues after each pass until convergence.

### Chosen option

Accept — run all 5 passes as requested, applying the three-pass review discipline's early-exit rule for passes that produce zero findings.

### Rationale

The plan had accumulated 9 amendments (Decisions 1–21) across multiple sessions. A systematic multi-pass review with all 12 personas ensured no cross-reference inconsistencies, logical gaps, or stale content remained.

### Findings summary

| Pass | Findings | Fixes applied |
|------|----------|---------------|
| Pass 1 | 2 MAJOR, 5 MINOR, 2 NIT (8 total) | 7 fixes: Diagram 1 Build/Review sections updated for diff-vs-main methodology, EP→CG edge added to Diagram 2, manifest format note, prompt budget Mermaid clarification, security guardrail refinement, VFE Build step 2 no-plan behavior |
| Pass 2 | 0 MAJOR, 2 MINOR, 1 NIT (3 total) | 2 fixes: Diagram 1 R5/R6 ordering corrected (group before output), prompt template mandatory sections clarified (Mission, CoV, Output contract, Guardrails always required) |
| Pass 3 | 0 findings — all 12 personas pass | None needed |
| Pass 4 | 0 findings — all 12 personas pass | None needed |
| Pass 5 | 0 findings — all 12 personas pass | None needed |

The plan converged after Pass 2. Passes 3–5 confirmed stability across all 12 personas with no new findings.

### Evidence

- Pass 1 MAJOR findings: Diagram 1 did not reflect Branch-vs-Main methodology in Build/Review sections (corrected); EP→CG edge missing from Diagram 2 (added).
- Pass 2 MINOR findings: R5/R6 logical ordering in Diagram 1 Review section (swapped); prompt template didn't specify mandatory sections for specialists (clarified).
- Passes 3–5: all 12 personas returned PASS with evidence-based verification against plan sections, routing defaults, Phase Matrix, verification checklist, step counts, and cross-reference consistency.

### Confidence rating

High — three consecutive clean passes from all 12 personas confirms convergence.

## CoV

- Claim: a shared-file, minimal-frontmatter strategy is the safest cross-surface baseline. Evidence: GitHub and VS Code docs both support the common frontmatter subset; GitHub explicitly ignores certain VS Code-only fields. Confidence: High.
- Claim: `vfe` is the right prefix. Evidence: current prefixes in repo do not include it and it maps directly to the requested philosophy. Confidence: High.
- Claim: a working directory under `./plan/...` is the right persistence mechanism for this family. Evidence: explicit user requirement and existing repo plan-folder pattern. Confidence: High.
- Claim: family-only subagent delegation is required for this plan. Evidence: explicit user requirement and the plan's self-contained family design. Confidence: High.
- Claim: plan amendments must rerun review before execution continues. Evidence: explicit user requirement and the workflow's existing verification gate structure. Confidence: High.
- Claim: the user-provided diagrams should be treated as the canonical modeled process. Evidence: explicit user instruction and the completeness of the supplied workflow set. Confidence: High.
- Claim: the entry-to-specialist family structure should be explicit in the plan, not only inferred from remit text. Evidence: explicit user-provided family-structure diagram. Confidence: High.
- Claim: `GPT-5.4 (copilot)` is justified without fallback. Evidence: official supported-model docs plus VS Code qualified-name requirements. Confidence: High.
- Impact: the draft plan can now specify the exact family structure, frontmatter template, and implementation sequence.

## Decision 23

### Decision statement

Add a formal Builder-to-Planner refinement loop, plan versioning, branch freshness/drift detection, Ralph Wiggum loop awareness, blocking/non-blocking issue classification, artifact lifecycle classification, artifact metadata requirements, completion summary artifact, and refinement detection in Review to the VFE agent family plan.

### Chosen option

Accept — integrate all components as a unified amendment addressing the formal replan protocol and the 10 gaps identified during the requirements checklist review.

### Rationale

The user provided detailed requirements for a formal refinement loop between Build and Plan, covering: builder trigger conditions (11 conditions), builder-side behavior (7 steps with 7 request classifications), planner-side behavior (9 steps), resume rules (5 steps), circuit-breaker interaction, plan versioning, branch freshness detection, loop awareness, blocking/non-blocking classification, artifact metadata, artifact lifecycle, and review-side detection of refinement omission. Many of these requirements also addressed gaps identified during the requirements checklist review (Gaps 1-10).

### Changes applied

1. **New artifact types**: `15-replan-request-<n>.md`, `16-replan-response-<n>.md`, `17-completion-summary.md` added to Optional Phase Files.
2. **Artifact Metadata section**: Required metadata fields (work item, date/time UTC ISO-8601, branch, base branch/commit, agent role, artifact status, plan version) for all artifacts.
3. **Resumption Protocol**: Expanded from 1 paragraph to 5 numbered steps (determine run type, record determination, check plan authority, decide continue/supersede/restart, verify post-refinement alignment).
4. **Artifact Lifecycle Classification**: 4 categories — Retained, Active, Ephemeral, Thread-scoped — with per-artifact classification and protection of critical decision records.
5. **Plan Versioning**: v1/v2/v3 incrementing scheme, `01-plan.md` always authoritative, prior versions preserved as `01-plan-v<n>.md`, refinement classification types.
6. **Branch Freshness and Drift Detection**: Required checks before build, 4 drift triggers, 3-level response (minor→continue, material→refinement, severe→ask user).
7. **Ralph Wiggum Loop Awareness**: 8-step required behavior for loop detection, review run metadata requirements.
8. **Blocking and Non-Blocking Issue Classification**: 9 blocking issue types, 4 non-blocking types, blocker artifact requirements.
9. **Builder-to-Planner Refinement Loop**: Core rule with 11 trigger conditions, builder-side behavior (7 steps, 7 request classifications, metadata requirements), planner-side behavior (9 steps with versioned response and plan review), resume rules (5 steps), circuit-breaker interaction (3 refinements for same area triggers escalation).
10. **VFE Plan workflow**: Mission expanded (refinement acceptance, sub-planning). Step 1: repo instruction enumeration. Step 7: version `v1`. New step 16: formal refinement request handling. Handoffs: "back from VFE Build via formal refinement request".
11. **VFE Build workflow**: Mission expanded (refinement detection). Step 1: repo instruction enumeration. New step 3: branch freshness check. New step 5: plan sufficiency check. Steps renumbered to 24. New step 24: completion summary. Handoffs: "to VFE Plan via formal refinement request".
12. **VFE Review workflow**: Mission expanded (plan version verification). Steps 1-2: repo instruction enumeration, replan artifact reading. New step 5: refinement detection (check builder followed plan version and used formal refinement). Step 9: refinement loop compliance added to explicit coverage. Handoffs updated. Steps renumbered to 14.
13. **Diagram 1**: Added branch freshness check (B2a), plan sufficiency decision node (B3a), and replan path back to P1.
14. **Diagram 3**: Added refinement request entry path with versioned response, plan preservation, and material change review gate.
15. **Diagram 4**: Added plan sufficiency check before slice definition with replan path.
16. **New Diagram 11**: Builder-to-Planner Refinement Loop flow with request classification, planner response, resume rules, and circuit-breaker.
17. **Manifest Requirements**: Expanded from 15 to 21 sections (added plan versioning, refinement loop, drift detection, blocking classification, artifact lifecycle, loop awareness).
18. **Refinement-aware specialists**: New subsection identifying 8 specialists that must understand refinement requests and their review responsibilities.
19. **Verification Checklist**: 12 new items (27-38) covering refinement loop, plan versioning, drift detection, completion summary, artifact metadata/lifecycle, loop awareness, blocking classification, manifest sections, diagrams.
20. **Prompt Template**: Added repo instruction file enumeration rule and refinement awareness rules to "all prompts must say" sections.
21. **Prompt-Embedding Priority**: Added Diagram 11 recommendation for Build and Plan prompts.

### Gaps addressed from requirements review

- Gap 1 (branch freshness): Branch Freshness and Drift Detection section
- Gap 2 (completion summary): `17-completion-summary.md` artifact + VFE Build step 24
- Gap 3 (plan versioning): Plan Versioning section
- Gap 4 (artifact metadata): Artifact Metadata section
- Gap 6 (blocking/non-blocking): Blocking and Non-Blocking Issue Classification section
- Gap 7 (resumption recording): Expanded Resumption Protocol
- Gap 8 (artifact lifecycle): Artifact Lifecycle Classification section
- Gap 9 (repo instruction enumeration): All three entry-agent step 1s + prompt template rule

### Gaps deferred

- Gap 5 (artifact-specific templates): The current format rules (7-point structure) and metadata requirements provide sufficient structure. Artifact-specific templates are a manifest-level implementation detail that the flow Builder can determine during agent file creation.
- Gap 10 (human vs machine distinction): Addressed implicitly by the artifact lifecycle classification (Retained artifacts are human-facing audit trail, Ephemeral/Thread-scoped are machine-operational). Explicit labeling is not required for correctness.

### Evidence

- User's detailed requirements specification with 10 explicit acceptance criteria
- Requirements checklist review identifying 10 gaps
- Existing plan sections (Plan Amendment Review Rule, Three-Pass Review Discipline, Failure and Recovery Protocol) that the new sections interact with
- Existing Diagrams 1, 3, 4 that were extended

### Risks and mitigations

- Risk: Prompt length may increase beyond the 3000-word entry-agent budget with all new rules. Mitigation: the prompt template allows compression and reference to manifest for details.
- Risk: Refinement loop could be abused to avoid coding. Mitigation: circuit-breaker (3 refinements for same area), clear trigger conditions, and Review detection of unnecessary refinement requests.

### Confidence rating

High — all changes are directly grounded in the user's detailed specification and the gap analysis from the requirements review.

## Decision 24

### Decision statement

Address the two remaining gaps from the requirements checklist review: Gap 5 (artifact-specific templates) and Gap 10 (human-facing vs machine-usable artifact distinction).

### Chosen option

Accept both — add artifact-specific required sections and artifact audience classification to the plan.

### Rationale

Gap 5: The generic 7-item format structure (Purpose/Status/Inputs/Findings/Decisions/Open questions/Next action) is too loose to ensure deterministic, parseable artifacts. Different artifact types serve different purposes and need different required sections. Defining per-artifact templates makes content predictable for both agents and humans, reduces ambiguity during creation, and enables machine-resumption from structured artifacts.

Gap 10: The lifecycle classification (Retained/Active/Ephemeral/Thread-scoped) defines *when* artifacts live and die, but not *who they serve*. Adding an audience classification (human-facing, machine-facing, dual-audience) makes writing style expectations explicit. Human-facing artifacts should be polished and self-contained; machine-facing artifacts should be structured and parseable; dual-audience artifacts must balance both.

### Changes applied

1. **Artifact-Specific Required Sections**: New subsection after Format Rules defining per-artifact required sections for: `00-intake.md`, `01-plan.md`, `02-slice-backlog.md`, `03-build-log.md`, `04-review-summary.md`, `06-docs-advice.md`, `07-automation-advice.md`, `08-decisions.md`, `09-handoff.md`, `15-replan-request-<n>.md`, `16-replan-response-<n>.md`, `17-completion-summary.md`.

2. **Artifact Audience Classification**: New subsection after Artifact Lifecycle Classification with three audience categories:
   - Human-facing: `00-intake.md`, `01-plan.md`, `04-review-summary.md`, `06-docs-advice.md`, `07-automation-advice.md`, `08-decisions.md`, `17-completion-summary.md`, replan request/response pairs
   - Machine-facing: `02-slice-backlog.md`, `03-build-log.md`, `09-handoff.md`, `14-operability.md`
   - Dual-audience: `05-comments-log.md`, specialist files, synthesis, branch review rounds, PR thread files

### Evidence

- User's gap analysis listing 10 gaps with specific descriptions
- Existing plan sections (Format Rules, Artifact Lifecycle Classification) that the new sections extend
- Artifact types already defined in the Optional Phase Files section

### Risks and mitigations

- Risk: Per-artifact templates add plan length. Mitigation: templates are reference material for flow Builder, not prompt content — they inform agent file creation but need not be embedded verbatim in prompts.

### Confidence rating

High — both gaps are clearly valid. The artifact-specific templates codify content that was already implied by the plan's artifact descriptions. The audience classification adds a dimension orthogonal to lifecycle that was genuinely missing.