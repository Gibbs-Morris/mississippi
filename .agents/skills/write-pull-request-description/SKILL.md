---
name: write-pull-request-description
description: Draft or revise pull request titles and descriptions from the actual change and validation evidence. Use when writing a PR body, refreshing a stale description, or explaining one layer of a stack. Not for code review, review-thread remediation, or managing branches and merges.
---

# Write a pull request description

Produce a reviewer-ready title and body for the selected change. Follow the
user's requested format and the consuming project's conventions. This skill
does not grant permission to publish, edit a remote PR, or run additional work;
use existing authorization when those actions are part of the request.

## Establish the change and its evidence

1. Identify the requested PR, branch comparison, or supplied diff. For a live PR
   or branch comparison, establish the head and actual target base. For a stack,
   compare the layer with its immediate parent, not the trunk. Do not assume a
   branch name or include ancestor work as new. A supplied diff can define the
   draft's scope without repository access or commit IDs; do not demand missing
   metadata unless it is needed for the requested output or local policy. If the
   change's scope cannot be established, request the narrow missing input or
   report the limitation before asserting scope.
2. Read the applicable local instructions and PR template. Determine title and
   versioning conventions, required sections, size accounting, verification
   gates, links, and any caller-specific audit content from that project. Do not
   impose a particular template, project layout, shell, or versioning system.
3. Inspect the actual changes and enough surrounding code, tests, or documents
   to explain the outcome. Use the issue or user request to establish motivation;
   a filename or commit subject alone is insufficient evidence of behavior. Use
   sufficient supplied change evidence without claiming independent inspection
   of inaccessible files.
   Follow local exemptions for generated material without hiding its scope.
   If the user includes uncommitted work, distinguish that proposed change from
   the current remote PR rather than silently mixing the two.
4. Gather the available verification results and the revisions they cover.
   Separate passing, failing, pending, skipped, unavailable, and unrun checks.
   A result for an older head or a different base does not prove this revision
   is ready. Use supplied evidence or authorized read-only checks; do not start
   a build or expand the task merely to make a description look complete.

## Draft the title and body

- Write a human-readable title naming the concrete outcome and applying the
  project's required title convention.
- Lead with the problem and resulting behavior. Explain why the change matters
  using verified facts; include a trigger and before/after example when useful.
  Avoid invented benefits, performance numbers, guarantees, or adoption claims.
- Preserve all required template sections and caller-required content. Remove
  optional sections that add nothing. Scale design explanation, use cases,
  examples, and diagrams to the change and local policy.
- Describe one logical outcome. For a stack, identify the parent, position,
  dependencies, and intended landing order when required. Give a useful review
  path through the important decisions or file groups instead of repeating the
  entire file manifest. Calculate requested size against the actual base and
  explain exceptions using the project's accounting rules.
- For breaking changes, identify the affected contract and provide verified
  before/after examples and consumer actions. Explain relevant limitations,
  rollout, or rollback requirements without inventing a supported recovery path.
- Report actual validation evidence and omissions. Keep historical results
  clearly labeled; unchecked boxes or a quiet review poll are not proof of
  readiness. Link to the specific evidence when it is available to reviewers.
- If a caller requires an audit summary, retain its required content and verify
  the supplied freshness/provenance conditions. Do not invent missing audit
  facts, expose inaccessible working notes as the only evidence, or take over
  a workflow role's authority. Report missing required inputs to that caller.

## Revise and verify

When updating an existing PR, reconcile both title and body with the final diff.
Remove abandoned approaches, stale examples, superseded claims, and outdated
check results. Preserve still-valid required content and unrelated authored
material; do not replace the body wholesale without checking what it contains.

Before returning or publishing the draft, compare its claims, identifiers,
examples, links, scope, and validation statements with the evidence. If the
head, base, or required audit inputs changed during preparation, refresh the
affected claims and readiness evidence. If refresh is unavailable, identify the
revision actually described and what remains unverified.

Return the requested title/body and any material unresolved limitation. Publish
only when the user's existing request authorizes that action and the applicable
workflow gates allow it; otherwise return the draft without changing remote
state. Report publication only after confirming the tool's result.
