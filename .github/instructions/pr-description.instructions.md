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

- Every PR description **MUST** link a relevant repository issue under [issue tracking and PR traceability](issue-tracking.instructions.md). Why: Reviewers need the planned outcome and current task record.
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

## Procedure

When drafting or updating a PR title or description, use the
[write-pull-request-description skill](../../.agents/skills/write-pull-request-description/SKILL.md)
with the rules above and the [repository PR template](../PULL_REQUEST_TEMPLATE.md).
The skill owns the drafting and evidence-reconciliation procedure; this file
retains the repository's mandatory title, content, and validation requirements.
The rules above apply even when the skill is not selected or available. If the
host cannot discover skills automatically, read the linked skill file directly.

## References

- PR template: `.github/PULL_REQUEST_TEMPLATE.md`
- Review guidelines: `.github/instructions/pull-request-reviews.instructions.md`
- Markdown conventions: `.github/instructions/markdown.instructions.md`
