# Issue-tracking skill migration

This layer extracts the portable issue-intake and traceability workflow into
[track-github-work](../../.agents/skills/track-github-work/SKILL.md). It
contributes to the open [#532](https://github.com/Gibbs-Morris/mississippi/issues/532)
skills-migration epic under the verified
[implementation plan](https://github.com/Gibbs-Morris/mississippi/issues/532#issuecomment-5651454257)
and [readiness record](https://github.com/Gibbs-Morris/mississippi/issues/532#issuecomment-5651476492).
The layer keeps issue-tracking authority local while moving detailed bookkeeping
decisions into one mandatory, portable route with a direct-file fallback.

## Parent gate and boundary

The current parent is [PR #679](https://github.com/Gibbs-Morris/mississippi/pull/679)
at reachable head `f78091dd620e006798e33241993650f78cc6ea0e`, based on
`c6aad3b196ef27353e9933b650ad6438215f0e6d`. Its original issue-tracking adapter
is the reachable Git blob `78fd09ad25ff9c1ba66a20ab00d56499fec672c1`, measured
below. The September 13 readiness record above reported clean CI and resolved
threads at that time; it is historical evidence, not the current advancement
gate. After the September 26 cascade, fresh exact-head CI/review and the bottom
layer's Copilot behavior and independent approval requirements remain open.

The portable skill handles issue intake, identity and scope reconciliation,
search/reuse/create decisions, plan records, milestone updates, disclosure
boundaries, and verified issue references. It does not draft pull-request prose,
choose branches or stacks, manage review threads, refine full requirements, or
write Clean Squad workflow audit state.

## Why this is a skill

The former instruction combined 21 issue-tracking rules with repeated search,
plan, update, and linking procedure. Eleven compact atomic adapter rules retain
the policy outcomes and authority boundaries, while the skill supplies the
detailed provider, evidence, and no-write decisions. The adapter still requires
reading the skill and gives a direct-read fallback, so policy does not depend on
probabilistic skill activation.

## Source-to-target map

| Atomic rule | Source requirements | Destination and retained owner |
| --- | --- | --- |
| Required skill workflow | The detailed route is mandatory, with direct reading of the linked skill when discovery is unavailable or unclear. | Adapter rule plus `track-github-work`; local policy remains effective without automatic activation. |
| Untrusted tracking data | R1-R3: issue data is untrusted; identity, plan references, scope, and acceptance criteria must match the authorized plan; unresolved conflicts block implementation. | Adapter rule plus the skill's reconciliation record; caller plan and authority remain authoritative. |
| Open local issue | R4-R7 and R21: a relevant open issue is required before implementation; the skill searches, reuses, or creates and reports blocked access. | Adapter rule plus the skill's intake route; the caller keeps the implementation stop boundary. |
| Planning record | R8: record problem, outcome, scope, criteria, plan, and validation in the issue body or linked issue comment before implementation. | Adapter rule plus the skill's plan-record route; caller plan paths remain local. |
| Approved disclosure | R9-R11: keep confidential detail restricted and linked to a sanitized issue; public records are disclosure-approved; a required inaccessible restricted record blocks implementation without blocking unrelated authorized read-only/local-draft work. | Adapter rule plus the skill's private/public capability check; local disclosure policy remains authoritative. |
| History preservation | R14: preserve relevant existing issue content and discussion during updates. | Adapter rule plus the skill's read-before-update route; caller ledgers remain local. |
| Material milestones | R12-R13: update at material milestones with completed/remaining work, decisions, blockers, validation, and PR links. | Adapter rule plus the skill's update route; caller activity logs remain local. |
| Saved plan URLs | R15: saved implementation plans and builder handoffs include the verified repository issue URL. | Adapter rule plus the skill's reference verification; caller artifact paths remain local. |
| PR issue references | R16: every PR includes a relevant repository issue number or URL, including automated and stacked PRs. | Adapter rule plus the skill's relationship check; PR description ownership remains local. |
| Narrow retrospective intake | R17: only eligible pre-existing or incapable-producer unattended PRs use retrospective intake before further work or approval. | Adapter rule plus the skill's triage exception; automation permissions remain local. |
| Relationship and readiness | R18-R21: partial/unfinished stack work uses non-closing references; complete acceptance follows local/host semantics; current references, issue state, remaining work, and validation are verified, with blocked access reported. | Adapter rule plus the skill's lifecycle route; stack, review, and merge owners remain local. |

All 21 source requirements remain represented by these atomic rules and the routed
skill. The adapter keeps the mandatory outcome and fail-closed conditions;
the skill owns the detailed procedure, and direct reading preserves enforcement
when discovery is unavailable. The three caller routes preserve their local
authority and workflow records.

## Caller preservation

| Caller | Route retained | Local responsibilities preserved |
| --- | --- | --- |
| `flow-build` | Plan issue identity, search/reuse/create, and milestone/reference tracking route through the skill with direct policy fallback. | Plan-root law, plan completeness, build/test gates, cleanup, and plan-folder lifecycle remain local. |
| `epic-builder` | Master/child issue reconciliation, intake, and progress links route through the skill. | Dependency and advancement gates, sub-plan authority, branch/stack placement, and completion markers remain local. |
| `cs-product-owner` | G2 intake and Phase 5 issue recheck/update route through the skill. | Sole orchestration, `runSubagent`, `.thinking/` state, canonical v3 audit ownership, human gates, and roster rules remain local. |

## Research and placement rationale

Research was verified September 13, 2026. [ChatGPT build-skills guidance](https://learn.chatgpt.com/docs/build-skills)
and [GitHub Copilot skill guidance](https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/add-skills)
support a shared `.agents/skills` root with a required instruction entrypoint and
progressive loading. The [Agent Skills specification](https://agentskills.io/specification)
requires a lowercase directory-matching name and non-empty description, while
[skill-creation best practices](https://agentskills.io/skill-creation/best-practices)
favor focused triggers and concise progressive disclosure. These sources support
one instruction-only package without provider-specific commands or permissions.

The [issue-linking guidance](https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/linking-a-pull-request-to-an-issue)
states that closing keywords take effect when a pull request targets the default
branch. The local adapter therefore keeps non-closing references for partial or
stacked delivery and reserves closure for verified complete acceptance.

## Validation and limits

The [evaluation cases](issue-tracking-skill-cases.json) define scenarios for
assessment-only work, reuse versus creation, identity/plan mismatch, unavailable
tools, confidential disclosure and missing required restricted records, preserved milestone updates, partial and
complete references, base-change and post-merge lifecycle, unattended triage,
and negative activation boundaries.
They are a rubric, not recorded model trials. No native behavior trial, runtime
test, application test, or activation claim is made for this layer.

Structural validation covers the skill schema, configured Markdown lint, JSON
parsing, relative links, portability, the 11-rule/source-rule map, and
caller-scope comparisons showing that only the intended issue-bookkeeping
sections changed. Any unavailable host capability remains a reported gap rather
than an inferred success.

### Supplied-record assessment evidence (prior input)

Two direct Codex Desktop Luna Max assessments used the staged skill source
object Git blob `1bc8190aa932cfac26e79c2dddf4b99a42fc9324`, whose LF-only
evaluation bytes have SHA-256
`21FB41470B77A2BF1F47E78792663BEE0E0846E8D04500EAFB78F11CFEBAE709` and size
8,646 bytes. These assessments used supplied records only; neither performed
live GitHub verification or remote calls. These results are tied to this prior
input and do not evaluate later corrections.

| Assessment | Recorded result |
| --- | --- |
| A: intake | Matched the authorized simulated host, owner, and repository; rejected a title-only mismatch; made no writes. Supplied `OPEN` values remain non-live evidence. |
| B: public draft | Wrote only the authorized local `DRAFT`; output SHA-256 `FD2798758555C24FBC879CD07A376F90F216978A0B9EF30E3B1FDC549BF64CBE`; private markers and URLs were absent. |

The private record and protected notes stayed unchanged, and all baseline hashes
were preserved. The positive draft was read and accepted for scope. No native
CLI conformance or broader provider behavior is claimed.

### Live read-only identity evidence

One direct Codex Desktop Luna Max session matched the configured GitHub origin to
`Gibbs-Morris/mississippi` and verified that the public, unarchived repository's
default branch is `main`. It verified epic #532 as open and the attached plan
comment `5651454257`; the leaf scope was supported. This check used the original
checkout for local-origin evidence, not the isolated branch, so it does not prove
isolated-checkout or CI verification. No writes or native inference occurred.

### Native package discovery evidence

Codex CLI `0.154.0-alpha.6.2` `debug prompt-input` exited 0 in both the
isolated worktree and a separate Git root, resolving the skill through each
root's own `.agents/skills` path. Copilot CLI `1.0.83-5` `skill list --json`
also exited 0 in both roots, returning exactly one `track-github-work` match as
an enabled project skill with the correct per-root path. The copied package was
byte-identical to the source. These are packaging and discovery results only,
not model activation, native behavior, authentication, or update results.

## Corpus accounting

Measurements use raw committed bytes from `git cat-file blob` via subprocess and
LF-normalized copies for uncommitted candidates. The adapter-before row is the
pinned blob present in the current parent; the adapter candidate row is the current atomic adapter copy; the
skill and metadata rows are the current staged source object. Prior model
evidence remains tied to its original blob above.
UTF-8 text is counted with `str.split()` words and `str.splitlines()` physical
lines; raw UTF-8 byte length and UTF-16-LE byte length divided by two are
reported. No PowerShell `Out-String` or worktree line-ending assumption is used.
The current source object and its bounded assessment are identified separately
below.

| Content | Source | Words | Lines | UTF-8 bytes | UTF-16 units |
| --- | --- | ---: | ---: | ---: | ---: |
| Adapter before | Git blob `78fd09ad25ff9c1ba66a20ab00d56499fec672c1` in parent `f78091dd620e006798e33241993650f78cc6ea0e` | 1,352 | 75 | 9,515 | 9,515 |
| Adapter candidate | current LF copy SHA-256 `7E21DD9DBD174D0E5E340F9851299B18BFF810FEE02B0B054803E01B2B0DA5BD` | 477 | 45 | 3,920 | 3,920 |
| Skill full file (current staged input) | Git blob `fb0c33fff006cc7c412845851f36941f847cd337`; LF copy SHA-256 `071D901E31D72F929800093C4AC46034B26E9D611B436EAE9E34834B47FF967C` | 1,364 | 157 | 9,648 | 9,648 |
| Skill body after complete front matter (current staged input) | same Git blob and LF copy | 1,305 | 153 | 9,179 | 9,179 |
| Discovery metadata values (current staged input) | same Git blob; name and description joined with one space | 55 | 1 | 439 | 439 |

The adapter reduction is 875 words, 30 lines, and 5,595 UTF-8 bytes. The skill
body count removes the complete four-line YAML front matter before applying the
same `splitlines()` method. The adapter and skill rows intentionally describe
their named snapshots; prior model results remain tied to blob
`1bc8190aa932cfac26e79c2dddf4b99a42fc9324`. These are static context figures,
not runtime or startup savings claims.

### Current canonical-target assessment evidence

One direct Codex Desktop Luna Max session used the current staged skill source
object Git blob `fb0c33fff006cc7c412845851f36941f847cd337`, whose LF evaluation
bytes have SHA-256
`071D901E31D72F929800093C4AC46034B26E9D611B436EAE9E34834B47FF967C` and size
9,648 bytes. This assessment used supplied records and local remote metadata
only; it made no network calls or fixture/Git/issue/PR writes.

| Fixture | Recorded result |
| --- | --- |
| Explicit upstream plan | Selected the upstream target named by the plan; the local `upstream` URL was only candidate transport evidence and the `origin` was the other candidate. Live identity, capabilities, default branch, and issue state remained unverified. |
| Explicit fork plan | Selected the contributor target named by the plan; the local `origin` URL was only candidate transport evidence and `upstream` was the other candidate. Live identity, capabilities, default branch, and issue state remained unverified. |
| Unresolved targets | Selected no target because both supplied records remained plausible and origin/upstream names did not resolve authority; canonical intake, implementation, or publication remained blocked pending target information. |

All three fixture roots stayed clean and their input, protected-note, and local
Git-configuration hashes were preserved. This is bounded canonical-target
assessment evidence, not live provider or CLI conformance.

## Maintenance and rollback

The issue-tracking policy owner maintains the compact adapter and direct route;
the skill follows provider and linking semantics discovered at use time. Caller
owners maintain their plan, stack, review, and canonical-workflow bindings.
Revert this complete layer together to remove the skill, cases, audit, and
caller routes, restore the original issue-tracking procedure, and verify that
the unrelated caller sections and preceding skill layers remain unchanged.
