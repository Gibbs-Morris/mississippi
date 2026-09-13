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

The committed cases in
[`build-remediation-skill-cases.json`](build-remediation-skill-cases.json)
cover explicit repair, assessment-only scope, environment and tooling
blockers, dependency cascades, empty execution evidence, authorized overlap
with user changes, safe retry requirements, approved narrow suppression,
repeated no-evidence reassessment, and negative review/feature/incident
boundaries. They are a reusable rubric, not a record of model trials.

The structural checks produced these results: the bundled skill-creator
validator passed with an isolated PyYAML 6.0.3 dependency; manual front matter
checks for required fields, name format, directory match, length, and unfinished
placeholders also passed. Configured Markdown lint passed with
markdownlint-cli2 0.23.2/markdownlint 0.41.1 and zero findings; the cases JSON
parsed; relative links resolved; the six retained rule bullets matched the base
exactly; changed-file whitespace checks passed; and the portability scan found
no repository names, fixed paths, local script names, or links in the skill.
The temporary validator dependency is outside tracked artifacts.

The canonical `pwsh ./go.ps1` run completed both builds, tests, cleanup passes,
and final builds with zero warnings or errors. Its cleanup generated 81
unrelated source/test paths; those outputs were saved and restored and are
excluded from this layer. Forty-three project TRX files contain 3,117 executed
and passed tests with zero failures; three documented SDK facade projects have
empty reports and are intentionally excluded from nonempty execution by
repository policy, as recorded by `RepositoryAutomation.psm1` and the agent
script README. All other selected project reports executed nonempty. The local
result does not establish exact published-head GitHub CI readiness.

Host evidence remains bounded. An independent Codex Desktop collaboration
Luna Max fixture used the final skill SHA-256
`49F165124D5E4B2CFD00FC30AF85D95929F7CF97ABE6C26F49AC4C8412F71F1D` in a
standalone fixture with its own Git root. After setup access was isolated with
fixture-local package configuration and application data, the checker reached
the supplied source failure (`CS0103` for `BuildMarkerr`) and exited 1; the
repair changed one expression in `Program.cs`, then the same checker exited 0
with `TEST_EXECUTED`. The 106-byte user-notes hash
`1703DFFF3847CAC0DC54204055D8013B8F10E4B7D120F7CB6E0B191AFD907761` and
checker/project policy were unchanged; an independent rerun produced the same
hash and exit 0. This is one application-host repair trial, not general skill
activation or cross-host conformance. Codex CLI 0.154.0-alpha.6.2 stopped
before model output on TLS `UnknownIssuer`; Copilot CLI 1.0.83-5 established
copied-fixture discovery but rejected max effort, so no Copilot behavior trial
is claimed. The cases remain a rubric, not recorded model trials.

No application, test-project, package, or workflow-configuration files are part
of this layer.

Corpus accounting uses the base adapter from `git show <base>:` and the final
adapter, counting whitespace-delimited tokens with `\S+`, physical lines, and
UTF-16 characters with PowerShell. The adapter is measured separately from the
added skill and audit metadata so discovery text is not presented as corpus
savings. Before: 342 words, 47 lines, and 2,618 characters. Current adapter:
244 words, 35 lines, and 1,970 characters, a reduction of 98 words, 12 lines,
and 648 characters. Added skill content: 1,216 words and 150 lines. Its
discovery metadata adds 56 words (the name is 1 word and the description is 55
words; the discovered path is `.agents/skills/repair-build-failures/SKILL.md`).
The full skill file is 1,276 words and 152 lines. Added cases and this record
are metadata, not savings.

## Rollback

Revert this complete layer together to remove the skill and restore the legacy
adapter's Quick-Start, Core Principles, and Procedure. Verify that the six
Rules bullets, local references, and all unrelated migrations remain intact;
do not revert the parent stack or other skills.
