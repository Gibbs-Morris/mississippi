---
name: address-pull-request-feedback
description: Assess or address existing GitHub pull request review feedback. Use when evaluating reviewer comments, applying their fixes, or completing an established post-push review loop. Not for reviewing a new diff, writing PR descriptions, or merging.
---

# Address pull request feedback

Address the requested feedback with evidence and within the user's authorized
scope. Existing authorization remains valid; do not ask again for
routine actions it already covers. An assessment-only request returns findings
and proposed actions without editing files or publishing replies. This workflow
does not itself grant branch, publication, thread-resolution, or merge authority.

## Establish scope and current evidence

1. Identify the repository, PR, requested comments, and permitted actions from
   the request and current workflow context. Sufficient supplied material can
   support an assessment; remote mutations require verified live PR identity.
2. Read applicable review policy and caller constraints. Discover required wait
   periods, iteration caps, commit isolation, validation commands, stack tooling,
   declined-thread handling, and output contracts. Keep these local bindings;
   do not import another project's timing or authority model.
3. Before changes or thread mutations, establish the current head and immediate
   PR base. For stacked changes,
   identify the layer that owns each finding and use the project's available
   stack workflow to edit and propagate that layer.
4. When reading a live PR, collect the relevant inline threads, replies, review
   submissions, and general discussion. Complete pagination before claiming
   complete coverage. Keep a compact identity/status index and read the discussions needed for the task
   rather than repeatedly loading all resolved history.

Prefer the integrations required by local policy. If GitHub CLI fallback or
pagination details are needed, read [GitHub thread actions](references/github-thread-actions.md).
Missing permissions, unavailable tools, partial responses, and failed queries
are evidence gaps, not proof that no comments remain.

## Decide each disposition

Read the comment in its current code and discussion context before accepting its
premise. Distinguish an actionable defect, an already-satisfied request, a
declined or out-of-scope suggestion, an informational comment, and a blocker.
Use the caller's required status vocabulary when recording the result.

- Inspect unresolved outdated threads; a moved line does not establish a fix.
- Consider new replies to previously handled discussions. A resolved flag alone
  does not establish that every later concern has a disposition.
- For a disputed claim, use source, tests, configuration, or rendering evidence
  appropriate to that claim. Do not change correct content merely to quiet a
  finding or invent an empty fix commit.
- Follow local policy for already-satisfied and declined comments. Give the
  rationale and remaining evidence or decision needed; leave the thread open
  when reviewer or author agreement is required.

## Apply an authorized fix

For each actionable comment, follow the local isolation policy and this order:

1. Apply the smallest complete correction in its owning layer.
2. Run validation appropriate to the change and all required local gates. A
   failed check remains failed; report an unrelated baseline problem separately
   rather than silently broadening the PR or claiming it passed.
3. Commit the focused fix with a traceable message. Do not combine unrelated
   findings into the commit.
4. Push and, for a stack, propagate as required. Confirm that the published
   revision contains the fix; use its final SHA if a rebase changed the ID.
5. Reply in the actual review thread with what changed, the published commit
   SHA, relevant validation, and any rationale the reviewer needs.
6. Resolve only after the fix is published, the evidence reply is present, and
   the concern is addressed under local policy. Never substitute a general PR
   comment for a required thread reply.

If push, reply, or resolution fails, keep that stage incomplete. Re-read remote
state after an ambiguous response before repeating a mutation, so a timeout
does not create duplicate replies or hide a completed action. Stop and report
the exact blocked action when the required operation cannot be completed.

## Finish the feedback loop

When local policy requires post-push polling, start it only after an actual push
to an open PR. Local-only commits do not start it. Honor the configured wait
period, cap, and any caller-specific freshness preemption or wait reporting.
If no cap is supplied, choose and state a finite cap before polling rather than
starting an unbounded retry loop.
After handling the current comments, wait as required and poll again for new
unaddressed feedback. Keep the iteration and disposition ledger across retries.

A quiet poll ends that polling cycle; it does not prove merge readiness. At a
cap or blocker, report outstanding threads and the next required decision.
Before advancing a dependent layer, separately verify the current head/base
checks, required approvals, all feedback dispositions, and current PR artifacts
under the project's advancement policy.

Return the caller's requested report, including the reviewed revision/scope,
thread identities and dispositions, published fix SHAs, validation results,
reply/resolution links when available, and unresolved blockers. Report limited
coverage or unexecuted checks explicitly. Do not merge as a side effect of
finishing review feedback.
