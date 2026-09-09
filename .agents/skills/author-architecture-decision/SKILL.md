---
name: author-architecture-decision
description: Assess whether a choice warrants an architecture decision record (ADR), or draft, revise, or supersede one from evidence and local lifecycle rules. Use when deciding whether to record an architectural choice or writing an ADR. Not for designing a system from scratch or summarizing an ADR in a pull request.
---

# Author an architecture decision

Assess whether a record is warranted, or produce the requested record or focused
update. For assessment-only requests, return a recommendation and rationale
without creating or editing a record. Respect the user's
format and output scope, the project's lifecycle, and the caller's authority.
Writing a record does not itself approve the decision, implement it, or authorize
publication, branch changes, or workflow-state updates.
For a focused update, apply only the relevant steps and return only the requested
change; do not redraft the whole record or add unrequested sections.

## Establish the record's purpose and constraints

1. Identify the decision, requested operation, and available evidence. Distinguish
   a new record, a correction to a mutable proposal, a changed accepted decision,
   and a metadata-only update. Use sufficient supplied material for a draft;
   repository access is needed only when the requested work or local policy
   requires facts that have not been supplied.
2. Check whether a new record adds value: significant structural impact, difficult
   reversal, cross-component effects, real trade-offs, or a precedent. For a
   minor choice or an already-recorded decision, explain why another record may
   be unnecessary; honor an explicit request or policy that still requires one.
3. For repository changes, inspect the existing records and applicable policy.
   Discover the format/version, output location, required metadata, filename and
   numbering rules, status values, diagram criteria, link conventions, and
   validation commands. Keep project paths and publication-specific metadata in
   the consuming project's policy rather than assuming this skill's layout.
4. Establish the actual context, constraints, considered alternatives, selected
   or proposed option, rationale, consequences, and available confirmation
   evidence. Verify technical claims against supplied evidence or inspected
   sources. Separate confirmed facts, assumptions, and future intent; do not
   invent options, measurements, participants, approval, or implementation.
   If a required decision or fact is missing, request it or label the draft as
   incomplete rather than presenting a completed or accepted record.

## Draft or update the requested content

- Prefer the project's required template and preserve its format. For a new MADR
  4.0.0 body, use the bundled [MADR body template](assets/madr-body.md)
  and add the metadata required by the consuming project. The asset is a body
  template, not a substitute for local frontmatter or lifecycle rules. Do not
  silently migrate an existing record to another format.
- State one decision and why it matters. Give enough context for a reader who
  did not attend the discussion. Describe the alternatives actually considered
  and the trade-offs honestly; do not manufacture strawman options or pad the
  record with generic architecture advice.
- In complete MADR records, retain Context and Problem Statement, Considered Options, and Decision
  Outcome with the chosen-option rationale. Include Decision Drivers,
  Consequences, Confirmation, option analysis, or More Information when useful
  or locally required. Remove unused optional sections and replace placeholders
  before describing the record as complete.
- Use diagrams when the relevant relationships are materially clearer visually
  and local policy calls for them. Keep prose authoritative, put the diagram
  beneath the discussion it explains, and verify alignment. Use an interaction,
  process, or structural view suited to the question. Record a required omission
  rationale when a qualifying diagram is intentionally absent; metadata-only
  or link-only edits should not trigger unrelated diagram work.
- Preserve valid content outside the requested change. Protect an accepted
  record's historical context and outcome. When the decision changes, follow the
  local superseding procedure: create the successor, link both records, and
  update only the predecessor's permitted lifecycle/link metadata. Do not
  rewrite the original decision's history to match the new choice.
- Set status and participant metadata from actual authority and evidence. A
  draft, generated document, or code merge is not proof of decision acceptance.
  Reuse authorization already supplied; do not add a second approval ceremony.

## Save and verify within scope

For file output, use the discovered destination and identifier convention.
When sequential numbering is required, inspect the relevant numbering scope,
choose an unused identifier, and follow the project's collision and merge-time
reconciliation rules. Treat provisional branch numbers accordingly; update all
affected filenames, titles, ordering metadata, and relative references together.
Do not assume every project numbers records or uses the same trunk branch.

Check required metadata, section requirements, status transitions, evidence,
links, and the final diff. Verify that an accepted record's protected content
remains intact and that a narrow update did not become an unrelated rewrite.
Run the applicable document validation when authorized and available, including
render/build checks required by the publication system. Report checks actually
run, failures or omissions, and any incomplete record or unresolved collision.

Return only the requested record, update, or relevant completion information.
Create supporting notes, publish documents, or change workflow state only when
the user's request or the active caller's authority covers those actions.
