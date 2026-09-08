---
applyTo: '**'
---

# Pull Request Description Authoring

Governing thought: PR descriptions explain one change, why it matters, and how it was verified, with detail proportional to review complexity.

> Drift check: Open `.github/PULL_REQUEST_TEMPLATE.md` before writing descriptions; the template defines the expected structure.

## Rules (RFC 2119)

### PR Titles

- PR titles **MUST** be human-readable descriptions of what the change accomplishes (not implementation details). Why: Titles appear in changelogs, release notes, and git history.
- PR titles **MUST** be updated on each commit/push when the scope or nature of the change evolves. Why: Stale titles mislead reviewers and pollute history.
- PR titles **MUST** end with a semver bump suffix for GitVersion (`+semver: <type>`). Why: Repository uses `commit-message-incrementing: Enabled` to derive versions from squash-merge commit messages.

**Semver suffix patterns (choose one):**

| Change Type | Suffix | When to Use |
|-------------|--------|-------------|
| Breaking change | `+semver: breaking` | Removes/renames public APIs, changes behavior in incompatible ways |
| New feature | `+semver: feature` | Adds new capabilities without breaking existing behavior |
| Bug fix | `+semver: fix` | Corrects defects without adding features |
| No version bump | `+semver: skip` | Docs-only, CI config, refactors with no public API changes |

**Title examples:**

- `Add fire-and-forget event effects for async side effects +semver: feature`
- `Fix null reference in aggregate grain activation +semver: fix`
- `Remove deprecated ILegacyEventStore interface +semver: breaking`
- `Update PR template and authoring instructions +semver: skip`

### PR Descriptions

- PR descriptions **MUST** be updated on each commit/push when a PR exists for the branch. Why: Keeps the description synchronized with the actual changes.
- Authors **MUST** compare the branch against the PR's actual base (`main` for a standalone PR, the immediate parent for a stack layer). Why: Descriptions explain this layer rather than repeating ancestor changes.
- Authors **MUST** include the single outcome, stack position and dependencies when applicable, and any size-exception rationale from [PR size and stacked delivery](pr-size-and-stacking.instructions.md). Why: Reviewers need clear boundaries and an efficient review path.
- Authors **MUST** include a Business Value section explaining why the change matters. Why: Reviewers and future maintainers need context beyond "what" to understand "why".
- Authors **SHOULD** include How It Works and concrete use cases when they help explain a non-trivial change. Why: Small fixes and documentation edits do not need an architecture essay.
- Authors **SHOULD** include architecture diagrams (ASCII or Mermaid) for non-trivial changes. Why: Visual representations accelerate understanding.
- Authors **SHOULD** identify key files or groups in review order rather than repeat GitHub's file manifest. Why: A review map explains where to focus without duplicating the diff.
- Authors **MUST** document breaking changes with before/after code examples. Why: Enables users to migrate without guessing.
- Authors **SHOULD** include code examples that demonstrate typical usage. Why: Copy-paste examples reduce adoption friction.
- Descriptions **MUST NOT** contain stale information from previous iterations. Why: Outdated content misleads reviewers.
- Descriptions **MUST NOT** contain invented benefits, unmeasured performance claims, boilerplate filler, or claims that unrun checks passed. Why: Authors remain accountable for generated descriptions and validation evidence.
- Authors **MUST** report actual validation results, pending checks, and applicable omissions. Why: Checkboxes alone do not establish the advancement gate.

## Scope and Audience

All contributors creating or updating pull requests.

## At-a-Glance Quick-Start

- Title format: `<Human-readable summary> +semver: <feature|fix|breaking|skip>`
- Before writing: `git diff <actual-pr-base>...HEAD --stat` to inspect this PR's changes
- Use the PR template in `.github/PULL_REQUEST_TEMPLATE.md`
- Update title and description on every push if PR exists
- Explain the outcome and motivation, then give the evidence needed to review it; remove unused template sections

## Procedure

### Initial PR Description

1. Identify the PR base and run `git diff <actual-pr-base>...HEAD --stat`
2. Read through each changed file to understand what was done
3. Identify the business problem being solved
4. Write the Business Value section first
5. Add stack context and size rationale when applicable
6. Explain the design and review order only to the depth needed
7. Record actual validation results and unresolved work
8. Add verified examples or migration guidance where needed

### Updating on Subsequent Commits

1. After each commit/push, review changes since last description update
2. Update scope, review map, stack dependencies, and size rationale if they changed
3. Update code examples if APIs changed
4. Verify Business Value still accurately reflects the PR scope
5. Remove any stale information that no longer applies

## Template Structure

Use the template proportionally; retain the outcome, review context, and validation evidence, and remove optional sections that add no information:

1. **Business Value** - Why this matters (required)
2. **Scope and Review Guide** - Single outcome, review order, size exception if needed
3. **Stack Context** - Parent, position, and landing intent (stacked PRs only)
4. **How It Works** - Design explanation (non-trivial changes only)
5. **Quality Gates** - Actual results and readiness checklist
6. **Migration Notes** - Breaking change guidance (if applicable)
7. **Related Issues** - Links to issues/discussions (if applicable)

## Good vs Bad Examples

### Bad: Implementation-focused

> "Added FireAndForgetEffectWorkerGrain.cs that implements IFireAndForgetEffectWorkerGrain interface"

### Good: Value-focused

> "Commands now return without waiting for external notification calls. Those calls run in background worker grains. The linked tests verify that a slow notification does not block command completion."

### Bad: Missing context

> "Changed HandleAsync signature to include brookKey parameter"

### Good: Complete context

> "**Breaking change**: The `HandleAsync` method on `EventEffectBase` now requires `brookKey` and `eventPosition` parameters. This enables effects to correlate with specific aggregate instances and event positions for debugging and idempotency. Existing effects must add these parameters (they can be ignored if not needed)."

## Core Principles

- Lead with business value, not implementation details
- Write for the reviewer who has no context
- Include enough detail that someone could implement similar functionality
- Keep descriptions synchronized with actual code changes
- Use tables and diagrams to convey complex relationships

## References

- PR template: `.github/PULL_REQUEST_TEMPLATE.md`
- Review guidelines: `.github/instructions/pull-request-reviews.instructions.md`
- Markdown conventions: `.github/instructions/markdown.instructions.md`
