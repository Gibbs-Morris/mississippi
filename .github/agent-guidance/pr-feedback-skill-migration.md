# Pull request feedback skill migration

This layer adds `address-pull-request-feedback` and replaces duplicated feedback
procedures with explicit routes. It contributes to
[#532](https://github.com/Gibbs-Morris/mississippi/issues/532).

The immediate parent is [PR #623](https://github.com/Gibbs-Morris/mississippi/pull/623)
at `fdb166cd92585ad75eeb2c248fec956dab8807d3`. Before this layer started, all five
ancestor PRs had their 11 required checks passing, current code reviews,
resolved feedback, no outstanding review requests, and GitHub CLEAN status.
Description-only label skips had successful runs at the same heads; Pages
deployment was inapplicable to PR events. Optional nonblocking security-summary
rows were not all current and are not represented as fresh review evidence.

## Why this is a skill

Existing feedback has a distinct workflow: establish scope, validate a concern,
make an isolated correction, publish it, reply in the correct thread, and resolve
only under the applicable policy. Thread/comment identities, pagination,
uncertain mutation responses, and the difference between a quiet poll and a
passed advancement gate are reusable operational details.

The package contains one entrypoint, a reference for GitHub thread operations, and
a read-only parameterized GraphQL query. It discovers the consuming project's
timing, validation, stack, authority, and output requirements. It contains no
product names, local workflow artifact paths, fixed wait period, or new tool
permissions. Assessment-only requests do not authorize edits or publication.

## Preservation boundary

All 14 existing rules in `pr-review-polling.instructions.md` are retained exactly;
one mandatory shared-skill route is added. This preserves the 300-second waits,
local-only commit exclusion, integration preference/fallback, isolated fix
sequence, declined/outdated-thread handling, iteration cap, exact thread
operations, ledger, and stack advancement gate.

The CoV PR agent keeps its remediation opt-in, hard rules, full-review behavior,
publication boundaries, severity model, and final output. Only its repeated
remediation and polling procedure routes change. The PR Manager keeps every
delegation, provenance, freshness, wait-accounting, output, and canonical-owner
requirement. The Clean Squad workflow keeps those same local responsibilities
and its polling/merge-readiness rules.

Consumers read the linked files directly when automatic skill discovery is
unavailable. The global policy remains in place, so the required workflow does
not depend solely on implicit skill selection.

## Verification

- The existing PowerShell CI orchestrator runs
  [13 executable contract checks](../../eng/tests/agent-scripts/PrFeedbackSkill.Tests.ps1)
  on Windows and Linux: the shipped query's cursor, continuation, state, and
  comment-anchor fields; six negative query fixtures; four consumer routes;
  and missing/broken route fixtures. These are local document-contract checks,
  not a GraphQL parser, GitHub service test, or model-behavior evaluation.
- Source comparison preserves the 14 policy bullets and all caller content
  outside the explicitly replaced procedure sections.
- Skill schema, portability, and local resource/consumer links pass.
- Markdown lint adds no findings. The CoV PR agent's existing findings decrease
  from 14 to 5 when duplicated lists are removed; other changed guidance passes.
- A read-only CLI test uses a test-only copy of the bundled query with
  `reviewThreads(first: 2)` and ten comments per REST page; the shipped query
  retains `reviewThreads(first: 100)`. The reduced page sizes exercise pagination:
  7 threads over 4 GraphQL pages and 14 inline comments over 2 REST pages. Every
  thread anchor joins to a top-level REST comment. The query's first comment is
  an identity anchor, not a complete discussion.
- Reply and resolution examples follow the documented provider APIs. No
  synthetic comments or thread mutations were used to validate the new package.
- Authenticated CLI fallback requires independently trusted query content in a
  private temporary file outside the reviewed checkout. PR-supplied queries,
  hashes, or copies cannot establish their own trust; verification precedes
  invocation, with a read-only integration or an explicit blocker as fallback.
- Codex and Copilot discover the enabled package in this repository and in a
  separate fixture repository with different local policy. All three copied
  files match the source hashes; these are packaging/discovery checks.

[Thirteen evaluation cases](pr-feedback-skill-cases.json) cover explicit fixes,
assessment-only work, negative selection, outdated and disproved findings,
pagination, ambiguous replies, stack ownership, caps, failed actions, and absent
push triggers, and a malicious query replacement. They are a rubric, not fresh
model-trial results. Host discovery
and static preservation checks do not prove behavioral equivalence for every
agent or environment.

The global policy shrinks from 1,180 to 800 whitespace-delimited words, removing
380 words from mandatory preparation. The two agent procedures remove a net 138
words; the workflow removes 25. The skill adds discovery metadata of its own.
These are corpus measurements, not runtime token or latency measurements.

## Research and rollback

Provider details were checked September 9, 2026 against the
[GitHub CLI API manual](https://cli.github.com/manual/gh_api),
[review-comment reply API](https://docs.github.com/en/rest/pulls/comments#create-a-reply-for-a-review-comment),
and [GraphQL pagination guide](https://docs.github.com/en/graphql/guides/using-pagination-in-the-graphql-api).

Revert this complete layer to restore the earlier duplicated procedures and
remove the shared package. Restore the policy and caller routes together; do
not leave a mandatory link pointing to a removed skill.
