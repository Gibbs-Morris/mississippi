# Build-remediation skill migration

This layer extracts the observed build-failure diagnosis and repair workflow
into [repair-build-failures](../../.agents/skills/repair-build-failures/SKILL.md).
It contributes to [#544](https://github.com/Gibbs-Morris/mississippi/issues/544)
and [#532](https://github.com/Gibbs-Morris/mississippi/issues/532). The current
authorized scope is one cohesive PR containing the skill, its adapter route,
the duplicate procedure removal, and evidence. The implementation starts from
base `567313a0714c2fc45160345b7e0f9d32f54aee05` on September 13, 2026; issue
planning was recorded in the [verified plan comment](https://github.com/Gibbs-Morris/mississippi/issues/544#issuecomment-5649615890),
and GitHub delivery remains tracked by the parent workflow.

## Why this is a skill

The former instruction mixed six repository rules with a short local procedure.
The procedure was useful across repositories because it must locate the first
actionable diagnostic, separate environment/tooling/dependency/source causes,
preserve caller changes, and verify execution evidence. The new skill owns that
portable workflow. The repository adapter retains its rules and routes to the
skill, including a direct-read fallback when automatic discovery is unavailable.

The corrected workflow establishes intended behavior from the accepted contract
and trustworthy evidence before choosing a production, assertion, or fixture
change. It changes tests or fixtures only when evidence shows they are wrong and
never weakens an assertion to accept a production regression.

The skill is instruction-only: one `SKILL.md`, no scripts, optional resources,
tool pre-approvals, repository names, solution names, fixed paths, or local
command names. It discovers the consuming project's commands and policies at
use time. Its description excludes general reviews, feature work, and runtime
incident-log analysis unless an observed failing quality check is the requested
issue.

## Source-to-target mapping

| Source content | Disposition |
| --- | --- |
| The six bullets in the legacy Rules section | Retained exactly in the repository adapter. |
| Legacy front matter, governing thought, drift check, scope, and references | Retained; the drift check and local policy links remain available before skill selection. |
| Legacy Quick-Start commands and four-step Procedure | Removed from the adapter as duplicated workflow; consuming-project commands are selected from local evidence by the skill. |
| Legacy Core Principles | Replaced by the skill's evidence, scope, preservation, retry, and verification guidance. |
| Repository build/test/cleanup bindings in other instructions | Unchanged; the skill defers to the consuming project's canonical commands and gates. |
| New evaluation cases and this migration record | Added as audit metadata; they do not change runtime or repository quality policy. |

The retained six rules are: per-issue attempt limit; consistent deferred-task
recording; narrow scope; no unapproved gate/analyzer/generated-code
workarounds; centralized package versions; and repository formatting/build
conventions. They remain effective when skill discovery is disabled or absent.

## Research and placement rationale

Research was checked on September 13, 2026. [ChatGPT build-skills guidance](https://learn.chatgpt.com/docs/build-skills)
places repository skills under `.agents/skills` and describes progressive
loading from name and description to the full `SKILL.md`. [GitHub Copilot's
skill guidance](https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/add-skills)
supports the same repository directory and required file shape.

The [Agent Skills specification](https://agentskills.io/specification) requires
the lowercase directory-matching `name` and a discriminating `description` in
YAML front matter, with optional resources kept separate. [Skill-creation best
practices](https://agentskills.io/skill-creation/best-practices) favor focused
triggers, concise instructions, and progressive disclosure. A single
instruction-only package therefore fits this workflow without adding scripts,
permissions, or a second overlapping skill.

## Validation record

The 22 evaluation cases in
[`build-remediation-skill-cases.json`](build-remediation-skill-cases.json)
cover triage, contract selection, supplied-command validation,
faithful-command substitution, silent and required execution evidence,
redaction, suppression authority, and negative boundaries. They are a rubric,
not recorded model trials. The assessment-only case keeps a side-effecting
command unrun when only read-only authority exists.

### Recorded fixture evidence

| Evidence | Result |
| --- | --- |
| Recorded canonical input | The fixed content revision `61056402410f13b890ac9ba6c5c4494ff3b90eeb` was exported directly to the ignored LF-only evaluation copy `.scratchpad/build-canonical-eval/skill-input/SKILL.md` (11,907 bytes; SHA-256 `AAE00AE9DCC487A1A9B2E0058B689F472628CDCA6DCB4B993C75FD7DC20858AC`). The results below are recorded for that revision, not this command-validation candidate. |
| Fixtures A, B, and E | Accepted contract/README evidence led A and E to `Math.trunc` → `Math.round`; B changed only its stale checker datum. Faithful checks moved from exit 1 to exit 0 with `CHECK_PASS`; E's original invocation remains unverified. |
| Fixtures C and D | C kept its invalid original unrun and verified the documented canonical command; D kept its side-effecting assessment command unrun. |
| Preservation | Only authorized A, B, and E files changed. All five protected notes and files outside those repairs stayed unchanged; no repository application tests or native inference were performed. |
| Structural checks | Skill validation, configured Markdown lint, JSON parsing, links, exact six-rule preservation, whitespace, and portability checks passed. No application or repository evidence files were modified. |

The command-validation correction requires independently matching a supplied
command's shape, target, flags, stages, and side effects to inspected project
documentation, CI, or configuration before execution. A mismatch or unavailable
validation leaves the supplied form unrun or uses an authorized faithful
invocation; a clear target or repair authorization alone is insufficient.

The revised command-validation candidate is exported as the LF-only evaluation
copy `.scratchpad/repair-build-command-validation/SKILL.md` (12,522 bytes;
SHA-256 `5A35BF5851CAB65869321DD433CD2B576600E66134A3555DDC15E7C69942A654`).
It is a new input for validation; no new model trial result is claimed.

### Historical evidence

| Input | Result and limit |
| --- | --- |
| Redaction trial, canonical copy `FC3EA72057CC92C4B60EA3E50C98EC54DC444C489E6112512A4DDB5F2E2D8DF9` | Direct Codex Desktop evaluation took no actions or edits and preserved all three fixture hashes. |
| Quiet trial, canonical input `E363F4F3C04508967CB9F6B09C87AE908220C34AE89E373E1F50F5AF8C9F3CD9` | Codex Desktop Luna Max trial: `node validate.mjs` moved exit 1 → 0 with stdout/stderr empty; only `trim` → `trim.toLowerCase` changed and the protected note stayed unchanged. This is historical input evidence and does not validate the current skill. |
| Original local pipeline | `pwsh ./go.ps1` passed at base `567313a0714c2fc45160345b7e0f9d32f54aee05`, but `main` changed application inputs afterward, so it does not validate the current application tree. |
| Superseded hash labels | Earlier mixed-EOL worktree and candidate pairings are superseded input provenance; they are not committed-blob identities. Current canonical bytes are identified above and in the corpus table. |

Current CI is authoritative; native CLI evidence is discovery-only with prior
Codex TLS and Copilot max-effort gaps. Mutation testing was not run.

## Corpus accounting

The measurements below use raw committed blobs from `git cat-file blob` via
subprocess, UTF-8 decoding, `str.split()` word counts, `str.splitlines()` line
counts, raw UTF-8 byte length, and UTF-16-LE byte length divided by two. They
do not use `Out-String` or worktree line endings.

| Content | Measured content revision | Words | Lines | UTF-8 bytes | UTF-16 units |
| --- | --- | ---: | ---: | ---: | ---: |
| Adapter before | `567313a0714c2fc45160345b7e0f9d32f54aee05` | 342 | 47 | 2,571 | 2,571 |
| Adapter candidate | `61056402410f13b890ac9ba6c5c4494ff3b90eeb` | 244 | 35 | 1,948 | 1,948 |
| Skill full file | `61056402410f13b890ac9ba6c5c4494ff3b90eeb` | 1,725 | 199 | 11,907 | 11,897 |
| Skill body after complete front matter | `61056402410f13b890ac9ba6c5c4494ff3b90eeb` | 1,665 | 195 | 11,490 | 11,480 |
| Discovery name plus description values | `61056402410f13b890ac9ba6c5c4494ff3b90eeb` | 56 | 2 | 389 | 389 |

The body count excludes all four YAML front matter lines. The canonical skill
input and audit are evidence artifacts. The adapter reduction is 98 words, 12
lines, and 623 UTF-8 bytes; discovery metadata is 56 words. These are static
corpus figures for the measured content revision and do not identify the revised
skill; use its candidate hash above for this correction. They are not runtime or
startup savings claims.

## Rollback

Revert this complete layer together to remove the skill and restore the legacy
adapter's Quick-Start, Core Principles, and Procedure. Verify that the six
Rules bullets, local references, and all unrelated migrations remain intact;
do not revert the parent stack or other skills.
