# Validated-lesson skill migration

This layer extracts the evidence, admission, conflict, and bounded-write
workflow for recording empirical lessons into
[capture-validated-lesson](../../.agents/skills/capture-validated-lesson/SKILL.md).
It contributes to the open [#532](https://github.com/Gibbs-Morris/mississippi/issues/532)
skills-migration epic under the verified [implementation plan](https://github.com/Gibbs-Morris/mississippi/issues/532#issuecomment-5649871064).
The current user-authorized scope is one complete PR containing the portable
skill, its local adapter routes, the moved repository format reference, and
evidence. No new self-taught lesson is added by this migration.

## Parent gate and boundary

Before implementation, the first layer's [PR #676](https://github.com/Gibbs-Morris/mississippi/pull/676)
gate was recorded at head `de3c12dc88db9169817b42d07b6deb30958894ef` against
base `567313a0714c2fc45160345b7e0f9d32f54aee05`: 29 checks passed, including
all 11 required checks, one metadata `label-by-files` skip was intentional for
an `edited` event, GitHub reported `CLEAN`, and there were no review requests
or unresolved threads. The Codex code-review bot completed without findings;
this historical gate does not prove the current layer's eventual head.

The portable skill records only a lesson grounded in an actual failure, retry,
or non-obvious workaround whose correction was validated. It discovers the
consuming project's authority, format, location, scope, and review controls at
use time. It deliberately returns no-write outcomes for insufficient evidence,
duplicates, conflicts, or unauthorized promotion. It contains no product or
repository names, fixed locations, domain list, file template, item threshold,
or mandatory host tool. The local format reference carries repository-specific
categories and template text outside the portable package.

## Source-to-target mapping

| Source content | Disposition |
| --- | --- |
| All 11 bullets in `self-improvement.instructions.md` Rules | Retained exactly; they remain effective whether skill discovery works or not. |
| Governing thought, drift check, global scope, and existing references | Retained; the route and local format links are added without changing authority. |
| Quick-Start, conflict-detection procedure, lesson lifecycle, and Core Principles | Replaced by a concise skill route with a direct-read fallback. |
| Domain Categories and quoted Self-Taught File Template | Moved verbatim to `self-taught-format.md`, with its repository-specific paths and domains intentionally kept local. |
| `cs-scribe` Learning Capture trigger, conflict recording, and activity-log duty | Trigger and the exact conflict/activity lines remain; duplicated identification, domain, and write guidance becomes skill/policy/format routing. |
| Remaining Scribe role, permissions, hard rules, ledger/audit duties, and output structures | Unchanged. Rules Manager remains unchanged and retains broader user-rule intake. |

The adapter retains the mandatory 11-rule policy even when automatic skill
selection is unavailable. The direct route reads both the portable procedure
and the local format reference before a lesson is recorded; the skill itself
does not depend on that repository-only file when copied elsewhere.

## Research and placement rationale

Research was checked September 13, 2026. [ChatGPT build-skills guidance](https://learn.chatgpt.com/docs/build-skills)
places repository skills under `.agents/skills` and describes progressive
loading from name and description to the full `SKILL.md`. [GitHub Copilot's
skill guidance](https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/add-skills)
supports the same repository root and `SKILL.md` shape.

The [Agent Skills specification](https://agentskills.io/specification) defines
the required lowercase directory-matching `name` and non-empty `description`,
with optional resources kept separate. [Skill-creation best practices](https://agentskills.io/skill-creation/best-practices)
favor discriminating triggers, concise instructions, and progressive disclosure.
One instruction-only package therefore fits lesson capture without adding
scripts, pre-approved tools, a second rule-manager workflow, or a repository
specific dependency.

## Validation and evaluation limits

The [evaluation cases](validated-lesson-skill-cases.json) define 14 scenarios
covering validated admission, assessment-only and unrun evidence, negative
routing, duplicate and higher-policy conflict, denied scope, authorized
overlap, untrusted injection, promotion and retirement controls, existing
approval, peer contradiction, and bounded no-write outcomes. They are a reusable
rubric rather than recorded model trials; no runtime savings, activation rate, or
latency claim is made.

The conflict correction treats every applicable guidance source, including an
equal-authority peer captured lesson, as a possible contradiction. It requires
reconciliation before writing, preserves approved valid resolutions without a
second approval, and removes only a new change if final verification exposes a
contradiction; unresolved peer conflicts remain no-write outcomes.

Structural checks passed: the bundled skill-creator validator; configured
Markdown lint; JSON parsing; relative-link resolution; exact comparison of the
11 retained Rules bullets; exact comparison of the moved format sections;
comparison showing only the Scribe Learning Capture block changed; portability
scanning of the skill; and whitespace checks. The Scribe file was linted with
the current repository configuration, and no pre-existing findings were
encountered.

One independent Codex Desktop collaboration-host Luna Max session evaluated
three separate Git-root fixtures using the earlier skill SHA-256
`97F4B5C9CD60257F0501EBAE9E2CC5B44FA1E1A8FFB6A22DB9AB7E1268D22856` and real
recorded Pester events. The first admitted one bounded record at
`records/field-notes.md` (output hash
`6DBC54864AFF58F272628F1AD18EB295B74E48BA45239F7D5D78B8CA754B873A`); the
second identified duplicate guidance and wrote nothing; the third identified a
higher-policy conflict and wrote nothing. Inventories were 4→5, 5→5, and 4→4;
baseline policies, evidence, and protected user-note hashes were unchanged.
The resulting record was independently read and accepted for scope and
evidence; no runtime tests were rerun. Those three outcomes are historical for
the earlier skill. One independent Codex Desktop collaboration-host Luna
Max session then evaluated two separate Git-root fixtures with current skill
SHA-256 `299B0ADA675BBE35082896C5BFB5B08DFEC1D1E98F1C9FB74B07AEDA84060C28`:
an equal-authority contradictory captured lesson was classified as Conflict
with no write, and an existing matching lesson was classified as Duplicate with
no write. Both authorized target files stayed absent, both five-file inventories
remained unchanged, and every baseline hash, including protected notes, stayed
unchanged. No runtime tests or native inference were performed. This is bounded
current peer-conflict evidence, not a claim of general activation or host
conformance.

Native discovery was checked separately: Codex CLI 0.154.0-alpha.6.2
`debug prompt-input` exited 0 without diagnostics in both the current worktree
and a separately initialized lesson-skill-discovery Git root, returning the
skill from each corresponding `.agents/skills` location. Copilot CLI 1.0.83-5
`skill list` exited 0 in both roots and reported a Project skill; its text did
not expose a source path, but separate Git roots and source/copy hashes matching
the historical hash above were verified. These checks establish packaging/discovery, not general
activation or cross-host behavior. Codex TLS and Copilot max-effort support
remain host gaps; no lower-effort fallback was authorized.

No second local `go.ps1` run was performed; the preceding layer's full local
pipeline covers unchanged application inputs. Exact-head CI remains required
for this layer, and mutation testing was not run.

## Corpus accounting

All measurements use raw bytes from `git cat-file blob` invoked through a
subprocess. The algorithm UTF-8-decodes each blob, counts `str.split()` words
and `str.splitlines()` lines, records raw byte length, and computes UTF-16 code
units as the UTF-16-LE byte length divided by two; no PowerShell `Out-String` or
worktree line-ending assumption is involved. For parent content revision
`1f15295894db0eeee621d65397ea8a765fbc6d13` and measured content revision
`4a26d019d011fc6c0ef8ba61afa0e41f72018f62`, the self-improvement adapter is
941 words, 103 lines, 7,055 UTF-8 bytes, and 7,047 UTF-16 code units before,
then 536 words, 42 lines, 4,133 bytes, and 4,133 code units after: a reduction
of 405 words, 61 lines, 2,922 UTF-8 bytes, and 2,914 code units. The moved
local format is 205 words, 40 lines, 1,550 UTF-8 bytes, and 1,550 code units.

The final skill is 815 words, 99 lines, 5,732 UTF-8 bytes, and 5,724 UTF-16
code units full-file. After removing the complete four-line YAML front matter,
its body is 760 words, 95 lines, 5,321 UTF-8 bytes, and 5,313 UTF-16 code units.
Discovery metadata values (name plus description, joined with one space) are 51
words, 383 UTF-8 bytes, and 383 code units; the skill path is separate. The
cases and this audit are evidence metadata, not claimed startup savings. No
application, package, workflow, or runtime files are part of this layer.

## Rollback

Revert this complete layer together to remove the portable skill, local format
reference, audit, and cases; restore the original self-improvement procedure
sections and the original Scribe Learning Capture block. Recheck all 11 Rules,
the local format content, and the unchanged Scribe sections. Preserve the
preceding build-remediation skill and unrelated repository work.
