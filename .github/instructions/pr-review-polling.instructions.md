---
applyTo: '**'
---

# Post-Push PR Review Polling

Governing thought: After pushing code to a branch with an open PR, agents sleep for human review, then systematically address every new comment one-at-a-time until none remain.

> Drift check: If GitHub MCP tools are unavailable, fall back to GitHub CLI (`gh`); confirm `gh` is installed with `Get-Command gh` before use.

## Rules (RFC 2119)

- After pushing code to a branch that already has an open PR, agents **MUST** sleep for 300 seconds (e.g. `Start-Sleep -Seconds 300` in PowerShell) before polling for new review comments. Why: Gives reviewers time to inspect the pushed changes before the agent acts.
- Local commits that have not been pushed **MUST NOT** start the polling loop. Why: Reviewers cannot comment on changes that are not yet visible on the PR.
- Agents **MUST** poll for unresolved PR review comments/threads using GitHub MCP tools by default; if MCP tools are unavailable and `gh` is installed, agents **MUST** fall back to GitHub CLI using `gh api` and `gh api graphql` for both polling and thread actions; if neither MCP nor `gh` is available, agents **MUST** stop and report the blocker. Why: MCP is the preferred integration; CLI is a reliable fallback when it can perform the exact workflow.
- When new unaddressed comments are found, agents **MUST** address them one-at-a-time in this exact order per comment: (1) read and understand the comment, (2) apply the minimal focused fix, (3) commit with a message scoped to that single comment, (4) push the branch, (5) reply to the comment thread with what was changed and the commit SHA, (6) resolve the thread. Why: Isolated commits make review history auditable and prevent batched regressions.
- Agents **MUST NOT** batch unrelated fixes into a single commit; each comment gets its own commit. Why: Keeps the fix traceable to the review feedback that prompted it.
- Agents **MUST NOT** resolve a thread before pushing the fix and replying with evidence. Why: Premature resolution hides unfinished work.
- If a comment is declined (disagree or out-of-scope), agents **MUST** reply with rationale and leave the thread open for the reviewer. Why: Only the reviewer or author should close a declined thread.
- Review threads where `isOutdated` is `true` **SHOULD** be skipped during the polling loop; agents **SHOULD** record them in the remediation ledger as `SKIPPED (outdated)` and leave them open for human review. Why: GitHub does not permit resolving outdated threads via the normal flow; attempting to do so causes API errors or confusing state.
- If the exact thread reply or resolution action cannot be completed with MCP or `gh` on the current machine, agents **MUST** stop and report the blocker rather than substituting a top-level PR comment. Why: A top-level comment does not satisfy the required per-thread audit trail.
- After addressing all found comments, agents **MUST** sleep for another 300 seconds and poll again; this loop **MUST** repeat until either (a) a poll returns zero new unaddressed comments or (b) a configured maximum iteration cap is reached. Why: Reviewers may add follow-up comments after fixes land while still bounding the loop in adversarial scenarios.
- Agents **SHOULD** log each addressed thread (thread ID, status, commit SHA) in a running remediation ledger in their output. Why: Provides an auditable summary of all review actions taken.
- Agents **SHOULD** configure that maximum-iteration cap to a reasonable value (e.g., 20 iterations); if the cap condition in the previous rule is reached, agents **MUST** log the remaining unresolved threads in the ledger and stop with a summary for human review. Why: Prevents runaway loops in adversarial or high-volume review scenarios while keeping the stopping condition unambiguous.

## Scope and Audience

All agents that push code to branches associated with open pull requests.

## At-a-Glance Quick-Start

1. Trigger: code pushed to a branch with an open PR.
2. Sleep 300 seconds.
3. Poll for new unresolved review comments (GitHub MCP or `gh` CLI).
4. For each comment: fix → commit → push → reply → resolve.
5. Sleep 300 seconds, poll again.
6. Repeat until zero new comments or the iteration cap is reached.

## Procedure

### Poll and Remediate Loop

```text
[TRIGGER: code pushed to a branch with an open PR]
IF no open PR THEN EXIT
LOOP (max 20 iterations)
  SLEEP 300 seconds  (e.g. Start-Sleep -Seconds 300)
  POLL for unresolved review comments/threads
  IF no new unaddressed comments THEN EXIT LOOP
  FOR EACH unaddressed comment (one at a time)
    READ the comment and understand the request
    APPLY the minimal focused fix
    COMMIT with a message referencing the comment
    PUSH the branch
    REPLY to the thread with: what changed, commit SHA, rationale
    IF fix applied THEN RESOLVE the thread
    ELSE reply with decline rationale and LEAVE thread open
  END FOR
  IF iteration cap reached THEN LOG remaining threads and EXIT LOOP
END LOOP
```

### GitHub MCP (preferred)

Use MCP tools for:

- Fetching PR review comments and threads
- Replying to comment threads
- Resolving threads

### GitHub CLI Fallback

If MCP tools are unavailable:

- `gh pr view <number> --json reviews,comments` — fetch review states (approved/changes-requested) and general PR discussion; does NOT return inline review thread comments
- `gh api repos/{owner}/{repo}/pulls/{pull_number}/comments` — list review comments on the PR
- `gh api graphql -f query='query($owner:String!, $repo:String!, $number:Int!) { repository(owner:$owner, name:$repo) { pullRequest(number:$number) { reviewThreads(first:100) { nodes { id isResolved isOutdated comments(first:100) { nodes { databaseId url body path line } } } } } } }' -F owner=<owner> -F repo=<repo> -F number=<pull_number>` — fetch thread IDs and top-level comment IDs
- `gh api -X POST repos/{owner}/{repo}/pulls/{pull_number}/comments/{comment_id}/replies -f body='<reply>'` — reply to a top-level review comment in the thread
- `gh api graphql -f query='mutation($threadId:ID!) { resolveReviewThread(input:{threadId:$threadId}) { thread { id isResolved } } }' -F threadId='<thread-node-id>'` — resolve a review thread
- If any required thread action cannot be completed with `gh`, stop and report the blocker instead of posting a top-level PR comment

## Core Principles

- Sleep before polling; do not race reviewers.
- One comment = one commit = one reply = one resolution.
- Prefer MCP; fall back to CLI. Never skip the feedback loop.
- Keep a ledger of actions for traceability.

## References

- PR review guide: `.github/instructions/pull-request-reviews.instructions.md`
- PR description authoring: `.github/instructions/pr-description.instructions.md`
- Shared guardrails: `.github/instructions/shared-policies.instructions.md`
- Build issue remediation: `.github/instructions/build-issue-remediation.instructions.md`
