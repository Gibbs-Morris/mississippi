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

Before implementation, PR679 at head
`877f562a2f82bdf78f849475642f26cf6157b3e6` against base
`cbe8fc71a8aa62a24b507295af82b6d12c491b29` passed its 05:46 UTC advancement
gate: both parent PRs were `CLEAN` and `MERGEABLE`, all applicable CI and all 11
required checks passed, all 9 of 9 review threads were resolved on each, and
the upper metadata refresh was clean. The open epic and plan above were verified
before editing. This evidence is the parent gate for this layer; its eventual
head still needs its own exact-head CI and review checks.

The portable skill handles issue intake, identity and scope reconciliation,
search/reuse/create decisions, plan records, milestone updates, disclosure
boundaries, and verified issue references. It does not draft pull-request prose,
choose branches or stacks, manage review threads, refine full requirements, or
write Clean Squad workflow audit state.

## Why this is a skill

The former instruction combined 21 issue-tracking rules with repeated search,
plan, update, and linking procedure. Eight compact adapter gates retain the
policy outcomes and authority boundaries, while the skill supplies the detailed
provider, evidence, and no-write decisions. The adapter still requires reading
the skill and gives a direct-read fallback, so policy does not depend on
probabilistic skill activation.

## Source-to-target map

| Gate | Source requirements | Destination and retained owner |
| --- | --- | --- |
| Identity and trust | R1-R3: issue data is untrusted; identity, plan references, scope, and acceptance criteria must match the authorized plan; conflicts block. | Compact adapter gate plus the skill's reconciliation record; caller plan and authority remain authoritative. |
| Intake timing and access | R4-R7 and R21: verify a relevant open local issue; search, reuse, or create after planning; verify a created issue's returned host/repository identity, open state, and usable URL; report blocked or ambiguous access rather than inventing tracking. | Adapter gate plus the skill's search/reuse/create route; the caller keeps the implementation stop boundary. |
| Plan and disclosure | R8-R11: record problem, outcome, scope, criteria, plan, and validation in the issue body or linked issue comment; keep confidential detail in the approved restricted record linked to the sanitized issue, with public records sanitized and disclosure-approved. | Adapter gate plus the skill's private/public capability check; local disclosure policy and private-record owner remain authoritative. |
| Milestones and preservation | R12-R14: keep issue state current, record completed/remaining work, decisions, blockers, validation, and PR links, and preserve content and discussion. | Adapter gate plus the skill's read-before-update route; caller activity logs and canonical ledgers remain local. |
| Saved and PR references | R15: saved implementation plans and builder handoffs include the verified repository issue URL. R16: every PR includes a relevant repository issue number or URL, including automated and stacked PRs. | Adapter gate plus the skill's reference verification; PR description and stack owners retain their roles. |
| Unattended triage | R17: complete retrospective intake before further implementation or review approval for pre-existing or unattended changes; do not claim intake preceded generation. | Adapter gate plus the skill's triage exception; automation permissions remain with their existing owner. |
| Partial and complete links | R18-R19: use non-closing references for partial delivery or a stack layer that leaves issue scope unfinished; close only complete acceptance under local policy and host semantics; external or child records do not replace local tracking. | Adapter gate plus the skill's relationship check; branch/stack operations remain with `gh-stack`. |
| Readiness verification | R20: verify references resolve and issue status, remaining work, and validation match the current PR before ready-to-merge; recheck after a base change and verify actual issue state after merge. | Adapter gate plus the skill's final evidence report; review and merge gates remain with their current owners. |

All 21 source requirements remain represented by these gates and the routed
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

The [evaluation cases](issue-tracking-skill-cases.json) define 12 scenarios for
assessment-only work, reuse versus creation, identity/plan mismatch, unavailable
tools, confidential disclosure, preserved milestone updates, partial and
complete references, base-change and post-merge lifecycle, unattended triage,
and negative activation boundaries.
They are a rubric, not recorded model trials. No native behavior trial, runtime
test, application test, or activation claim is made for this layer.

Structural validation covers the skill schema, configured Markdown lint, JSON
parsing, relative links, portability, the eight-gate/source-rule map, and
caller-scope comparisons showing that only the intended issue-bookkeeping
sections changed. Any unavailable host capability remains a reported gap rather
than an inferred success.

### Supplied-record assessment evidence

Two direct Codex Desktop Luna Max assessments used the staged skill source
object Git blob `1bc8190aa932cfac26e79c2dddf4b99a42fc9324`, whose LF-only
evaluation bytes have SHA-256
`21FB41470B77A2BF1F47E78792663BEE0E0846E8D04500EAFB78F11CFEBAE709` and size
8,646 bytes. These assessments used supplied records only; neither performed
live GitHub verification or remote calls.

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

Measurements use raw committed bytes from `git cat-file blob` via subprocess for
the parent adapter and LF-normalized candidate bytes for new content. UTF-8 text
is counted with `str.split()` words and `str.splitlines()` physical lines; raw
UTF-8 byte length and UTF-16-LE byte length divided by two are reported. No
PowerShell `Out-String` or worktree line-ending assumption is used. The adapter,
skill, discovery metadata, and audit metadata are measured separately.

| Content | Source | Words | Lines | UTF-8 bytes | UTF-16 units |
| --- | --- | ---: | ---: | ---: | ---: |
| Adapter before | parent commit `877f562a2f82bdf78f849475642f26cf6157b3e6` | 1,352 | 75 | 9,515 | 9,515 |
| Adapter candidate | ignored LF copy SHA-256 `22577716F2CDCD7C5AAE98D837E8A43D3A42211B1554A276FCB10B79A0989C95` | 696 | 42 | 5,407 | 5,407 |
| Skill full file | staged Git blob `1bc8190aa932cfac26e79c2dddf4b99a42fc9324`; LF copy SHA-256 `21FB41470B77A2BF1F47E78792663BEE0E0846E8D04500EAFB78F11CFEBAE709` | 1,226 | 143 | 8,646 | 8,646 |
| Skill body after complete front matter | same staged Git blob and LF copy | 1,167 | 139 | 8,177 | 8,177 |
| Discovery metadata values | same staged Git blob and LF copy; name and description joined with one space | 55 | 1 | 439 | 439 |

The adapter reduction is 656 words, 33 lines, and 4,108 UTF-8 bytes. The skill
body count removes the complete four-line YAML front matter before applying the
same `splitlines()` method. These are static context figures, not runtime or
startup savings claims.

## Maintenance and rollback

The issue-tracking policy owner maintains the compact adapter and direct route;
the skill follows provider and linking semantics discovered at use time. Caller
owners maintain their plan, stack, review, and canonical-workflow bindings.
Revert this complete layer together to remove the skill, cases, audit, and
caller routes, restore the original issue-tracking procedure, and verify that
the unrelated caller sections and preceding skill layers remain unchanged.
