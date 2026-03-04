---
applyTo: '**/*.{cs,razor,css}'
---

# Namespace and Folder Placement

Governing thought: File location and namespace layout must be deterministic, vertically sliced, and repeatable so contributors can find behavior in the same place across framework, tests, and samples.

> Drift check: Review `Directory.Build.props`, `.github/instructions/naming.instructions.md`, and project role guidance in `.github/instructions/projects.instructions.md` before changing placement rules.

## Rules (RFC 2119)

- File placement **MUST** be deterministic from project identity, namespace segments, and approved archetype patterns; ad-hoc placement **MUST NOT** be used. Why: Keeps navigation predictable.
- Namespace segments **MUST** mirror folder segments for applicable source files, except explicitly approved root exceptions (for example `GlobalUsings.cs` and assembly-info style files). Why: Preserves one-to-one discoverability.
- Folder density limits **MUST** be enforced at **20 files per file type/extension** (for example `.cs`, `.css`, `.razor`); mixed types **MAY** exceed 20 in total if each type remains <=20. Why: Prevents over-dense folders while allowing mixed UI/component colocation.
- Vertical splitting **MAY** occur before the 20-per-type limit when it materially improves discoverability and repeated pattern alignment; if a folder/type exceeds 20, vertical split **MUST** be applied. Why: Enables proactive organization while keeping a hard ceiling.
- Horizontal technical-bucket splits (`Services`, `Models`, `Helpers`, `Utils`, `Common`) **MUST NOT** be introduced unless the segment is an established domain feature name and explicitly justified. Why: Avoids non-domain junk-drawer structures.
- Aggregate, Projection, and Saga code **MUST** follow consistent archetype patterns across `src/**`, `tests/**`, and `samples/**`; tests **SHOULD** mirror production vertical structure under test namespace roots. Why: Repetition improves user onboarding and maintenance.
- Archetype conformance deviations **MUST** include reason, approval metadata, and evidence; deviations by convenience/preference **MUST NOT** be accepted. Why: Allows edge cases without policy erosion.
- Exception usage **SHOULD** remain <=1% of mapped files; if it exceeds 1%, execution **MUST** stop for rules reassessment and recorded decision. Why: Keeps exceptions rare and intentional.
- Conflict resolution precedence **MUST** be: valid bounded-context naming, then archetype consistency, then per-type cap splitting inside the archetype branch, then deterministic qualifiers. Why: Prevents inconsistent tie-break behavior.
- Deterministic placement enforcement **MUST** include machine-readable inventories/reconciliation outputs and gate failures for unresolved collisions, cap breaches, missing approvals, or missing mappings. Why: Ensures no file is silently skipped.

## Scope and Audience

Contributors moving or adding source files under `src/`, `tests/`, and `samples/` where namespace/folder consistency is required.

## At-a-Glance Quick-Start

- Start from project identity and namespace, then map to matching folder segments.
- Keep each folder at <=20 files per extension.
- Split vertically by behavior context before creating horizontal utility buckets.
- Use Aggregate/Projection/Saga archetypes consistently across framework and samples.
- Record and approve any true edge-case deviation.

## Placement Decision Tree (Deterministic)

1. Exclude generated/intermediate files (`obj/**`, `bin/**`, generated outputs).
2. Determine base namespace from project identity and role.
3. Apply archetype branch (`Aggregates`, `Projections`, `Sagas`) when artifact kind matches.
4. Enforce per-type cap; split vertically within branch if needed (or proactively when clearer).
5. Validate conformance, collisions, and approvals; fail gates on unresolved issues.

## Archetype Segment Naming (Canonical)

| Artifact | Canonical Segments |
|----------|--------------------|
| Aggregate | `Aggregates/<Name>/Commands`, `Events`, `Reducers`, `Effects`, `State`, `Handlers`, `Registrations` |
| Projection | `Projections/<Name>/Reducers`, `Effects`, `State`, `Handlers`, `Contracts`, `Registrations` |
| Saga | `Sagas/<Name>/Commands`, `Events`, `Reducers`, `Effects`, `State`, `Compensation`, `Registrations` |

## Core Principles

- Determinism beats preference.
- Vertical slices beat horizontal technical buckets.
- Edge cases are allowed, but governance must stay strict.

## References

- Naming policy: `.github/instructions/naming.instructions.md`
- Project roles and boundaries: `.github/instructions/projects.instructions.md`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
