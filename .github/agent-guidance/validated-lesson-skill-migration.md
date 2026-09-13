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

The [evaluation cases](validated-lesson-skill-cases.json) define 15 scenarios
covering validated admission, assessment-only and unrun evidence, negative
routing, duplicate and higher-policy conflict, denied scope, authorized
overlap, untrusted injection, promotion and retirement controls, existing
approval, peer contradiction, and bounded no-write outcomes. They are a reusable
rubric rather than recorded model trials; no runtime savings, activation rate, or
latency claim is made. The documented-silent case distinguishes missing required
execution evidence from a verified silent success under the consuming contract.

Structural checks passed: the bundled skill-creator validator; configured
Markdown lint; JSON parsing; relative-link resolution; exact comparison of the
11 retained Rules bullets; exact comparison of the moved format sections;
comparison showing only the Scribe Learning Capture block changed; portability
scanning of the skill; and whitespace checks. The Scribe file was linted with
the current repository configuration, and no pre-existing findings were
encountered.

### Current direct fixture evidence

One direct Codex Desktop collaboration-host Luna Max session used the LF-only
copy exported directly from commit
`7fc373ea63b42cc20aef292240e06a1124681a04`. That blob remains identical at
measured content revision `9c37990cf97263edf10b49d7291cf6d3698dae62`; the input
is 5,949 bytes with SHA-256
`154AD3CA0F0E014509005210A1E889023EFE60117271C81BB59D2E5AD42D6695`.

| Fixture | Recorded result |
| --- | --- |
| New | Added one bounded record at `records/field-notes.md` (output hash `6DBC54864AFF58F272628F1AD18EB295B74E48BA45239F7D5D78B8CA754B873A`). |
| Duplicate | Classified as Duplicate; no write occurred. |
| Higher-policy conflict | Classified as Conflict; no write occurred. |
| Peer conflict | Classified as Conflict; no write occurred. |

All baseline files and protected notes retained their hashes. The positive record
was read and accepted for scope and evidence. No runtime tests, native
inference, external actions, or main-repository edits were performed.

### Historical evaluation inputs

Earlier trials used raw/host input labels
`97F4B5C9CD60257F0501EBAE9E2CC5B44FA1E1A8FFB6A22DB9AB7E1268D22856` and
`299B0ADA675BBE35082896C5BFB5B08DFEC1D1E98F1C9FB74B07AEDA84060C28`. These
are historical evaluation inputs, not the current committed skill bytes, and
are retained only to identify prior trial boundaries.

Native CLI checks established packaging and discovery only: Codex CLI
`debug prompt-input` exited 0 without diagnostics in both Git roots, and Copilot
CLI `skill list` exited 0 and reported a Project skill in both roots. These
checks do not establish general activation or cross-host behavior; Codex TLS and
Copilot max-effort support remain host gaps, with no lower-effort fallback
authorized.

The historical `go.ps1` run at base
`567313a0714c2fc45160345b7e0f9d32f54aee05` predates application changes on
`main` and does not validate the current application tree. No second local run
was performed; exact-head CI is authoritative and mutation testing was not run.

## Corpus accounting

All measurements use raw bytes from `git cat-file blob` invoked through a
subprocess. The algorithm UTF-8-decodes each blob, counts `str.split()` words
and `str.splitlines()` lines, records raw byte length, and computes UTF-16 code
units as the UTF-16-LE byte length divided by two; no PowerShell `Out-String` or
worktree line-ending assumption is involved.
Discovery metadata counts use the `name` and `description` values joined with
one space.

| Content | Measured content revision | Words | Lines | UTF-8 bytes | UTF-16 units |
| --- | --- | ---: | ---: | ---: | ---: |
| Adapter before | `cbe8fc71a8aa62a24b507295af82b6d12c491b29` | 941 | 103 | 7,055 | 7,047 |
| Adapter candidate | `9c37990cf97263edf10b49d7291cf6d3698dae62` | 536 | 42 | 4,133 | 4,133 |
| Skill full file | `9c37990cf97263edf10b49d7291cf6d3698dae62` | 844 | 102 | 5,949 | 5,941 |
| Skill body after complete front matter | `9c37990cf97263edf10b49d7291cf6d3698dae62` | 789 | 98 | 5,538 | 5,530 |
| Discovery metadata values | `9c37990cf97263edf10b49d7291cf6d3698dae62` | 51 | 1 | 383 | 383 |

The adapter reduction is 405 words, 61 lines, 2,922 UTF-8 bytes, and 2,914
UTF-16 code units. The skill body count excludes all four YAML front matter
lines. Cases and this audit are evidence metadata; no runtime or startup
savings claim is made, and no application, package, workflow, or runtime files
are part of this layer.

## Rollback

Revert this complete layer together to remove the portable skill, local format
reference, audit, and cases; restore the original self-improvement procedure
sections and the original Scribe Learning Capture block. Recheck all 11 Rules,
the local format content, and the unchanged Scribe sections. Preserve the
preceding build-remediation skill and unrelated repository work.
