# Validated-lesson skill migration

This layer extracts the evidence, admission, conflict, and bounded-write
workflow for recording empirical lessons into
[capture-validated-lesson](../../.agents/skills/capture-validated-lesson/SKILL.md).
It contributes to the open [#532](https://github.com/Gibbs-Morris/mississippi/issues/532)
skills-migration epic under the verified [implementation plan](https://github.com/Gibbs-Morris/mississippi/issues/532#issuecomment-5649871064).
The current user-authorized scope is one complete PR containing the portable
skill, its local adapter routes, the moved repository format reference, and
evidence. No new self-taught lesson is added by this migration.
The September 26 instruction-only scope excludes custom agents; Scribe and
Rules Manager remain byte-identical to the current parent.

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
| All 12 bullets in `self-improvement.instructions.md` Rules | Retained exactly; they remain effective whether skill discovery works or not. |
| Governing thought, drift check, global scope, and existing references | Retained; the route and local format links are added without changing authority. |
| Quick-Start, conflict-detection procedure, lesson lifecycle, and Core Principles | Replaced by a concise skill route with a direct-read fallback. |
| Domain Categories and quoted Self-Taught File Template | Moved verbatim to `self-taught-format.md`, with its repository-specific paths and domains intentionally kept local. |
| Existing `cs-scribe` Learning Capture consumer | Agent file remains unchanged; the adapter retains its named Conflict Detection Protocol entrypoint and routes to the skill and local format without repeating the procedure. |
| Clean Squad Scribe route | Trigger, Scribe ownership, **SHOULD**, lesson destination, and rationale remain; the stale protocol name now routes directly to the skill, self-improvement policy, and local format. |
| Remaining Scribe role, permissions, hard rules, ledger/audit duties, and output structures | Unchanged. Rules Manager remains unchanged and retains broader user-rule intake. |

The adapter retains the mandatory 12-rule policy even when automatic skill
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

The [evaluation cases](validated-lesson-skill-cases.json) define 17 scenarios
covering validated admission, assessment-only and unrun evidence, negative
routing, duplicate and higher-policy conflict, denied scope, authorized
overlap, untrusted injection, promotion and retirement controls, existing
approval, mixed duplicate/conflict precedence, peer contradiction, and bounded
no-write outcomes, and a consuming hierarchy that ranks captured guidance above
an older hand-written source. They are a reusable rubric rather than recorded model trials;
no runtime savings, activation rate, or latency claim is made. The
documented-silent case distinguishes missing required execution evidence from a
verified silent success under the consuming contract.

Structural checks passed: the bundled skill-creator validator; configured
Markdown lint; JSON parsing; relative-link resolution; exact comparison of the
12 retained Rules bullets; exact comparison of the moved format sections;
comparison showing Scribe is byte-identical to the current parent; portability
scanning of the skill; and whitespace checks. The compatibility entrypoint and
its skill/local-format references remain present; no agent migration is included.

The retained-rule comparison was refreshed September 26 against the current
parent: all 12 bullets match exactly. Other historical checks retain their
recorded revision and do not establish current native behavior.

### September 26 authority correction

The portable skill now uses the governing hierarchy and the consuming project's
declared authority model; hand-written/captured provenance establishes no rank.
The unchanged local adapter retains this repository's hand-authored precedence.
Candidate Git blob `e6179bb5b86817b707fb6c367075089e505f7159` has UTF-8/LF
SHA-256 `1260e379c5c52737e0477b2f610d5d143792fc924f8c11a25ce9794c207a9fca`:
6,312 bytes, 893 words, 107 lines. Earlier corpus rows and Desktop trials below
describe historical inputs, not this corrected candidate.

One fresh [native Codex record](validated-lesson-native-trial.json) used
CLI 0.142.5 / GPT-5.5 medium in a read-only synthetic fixture. It read the skill,
ranked the captured source above the handwritten source under the declared
project policy, returned Conflict before Duplicate without an authorized
resolution, and distinguished supplied evidence from live verification. Every
fixture file hash remained unchanged; no checker, write, or publication ran.
This is one authority case, not all 17 rubric cases or Copilot conformance.
Copilot behavior remains unavailable from the observed account usage limit.
Skill schema, JSON, configured Markdown lint, and whitespace checks pass.

### Historical fixed-input fixture evidence

One direct Codex Desktop collaboration-host Luna Max session recorded results
from the LF-only
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
inference, external actions, or main-repository edits were performed. These are
recorded fixed-input results, not a trial of the precedence correction.

### Historical precedence correction trial

One direct Codex Desktop collaboration-host Luna Max assessment used the staged
skill input at `.scratchpad/lesson-conflict-precedence-eval/skill-input/SKILL.md`
(6,232 LF-only bytes; SHA-256
`DB54316F1B1DB4A50A40F61B7B07FBE3872A38A18EF7D89BFC2A875EA4C589F1`). It
classified the overall result as **Conflict** after finding one matching and
one contradictory equal-authority source; the duplicate match was reported but
did not authorize a write. The target remained absent, and all seven file hashes
including the protected note stayed unchanged. No runtime checks, native
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
| Skill full file (staged Git blob) | `933e8102e16cf0940824db43829c0a79044da9bd` | 882 | 106 | 6,232 | 6,224 |
| Skill body after complete front matter (staged Git blob) | `933e8102e16cf0940824db43829c0a79044da9bd` | 827 | 102 | 5,821 | 5,813 |
| Discovery metadata values (staged Git blob) | `933e8102e16cf0940824db43829c0a79044da9bd` | 51 | 1 | 383 | 383 |

The adapter reduction is 405 words, 61 lines, 2,922 UTF-8 bytes, and 2,914
UTF-16 code units. The skill body count excludes all four YAML front matter
lines. Git blob ID `933e8102e16cf0940824db43829c0a79044da9bd` identifies the
staged skill source object, while SHA-256
`DB54316F1B1DB4A50A40F61B7B07FBE3872A38A18EF7D89BFC2A875EA4C589F1` identifies
the raw LF evaluation bytes. Cases and this audit are evidence metadata; no
runtime or startup savings claim is made, and no application, package,
workflow, or runtime files are part of this layer.

## Rollback

Revert this complete layer together to remove the portable skill, local format
reference, audit, and cases; restore the original self-improvement procedure
sections. Recheck all 12 Rules, the local format content, and the unchanged
Scribe file. Preserve the
preceding build-remediation skill and unrelated repository work.
