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

The 16 evaluation cases in
[`build-remediation-skill-cases.json`](build-remediation-skill-cases.json)
cover explicit repair, assessment-only scope, environment and tooling
blockers, dependency cascades, expected-behavior contract checks, unknown and
invalid original commands, empty execution evidence, authorized overlap with
user changes, safe retry requirements, approved narrow suppression, repeated
no-evidence reassessment, and negative review/feature/incident boundaries. They
are a reusable rubric, not a record of model trials. The assessment-only case
supplies a clear command that writes restore artifacts while authority covers
only read-only diagnostics; its expected outcome leaves that command unrun.

The structural checks produced these results: the bundled skill-creator
validator passed with an isolated PyYAML 6.0.3 dependency; manual front matter
checks for required fields, name format, directory match, length, and unfinished
placeholders also passed. Configured Markdown lint passed with
markdownlint-cli2 0.23.2/markdownlint 0.41.1 and zero findings; the cases JSON
parsed; relative links resolved; the six retained rule bullets matched the base
exactly; changed-file whitespace checks passed; and the portability scan found
no repository names, fixed paths, local script names, or links in the skill.
The temporary validator dependency is outside tracked artifacts.

The historical `pwsh ./go.ps1` run completed both builds, tests, cleanup
passes, and final builds with zero warnings or errors at the original tree/base
`567313a0714c2fc45160345b7e0f9d32f54aee05`. Its cleanup generated 81 unrelated
source/test paths; those outputs were saved and restored and are excluded from
this layer. Forty-three project TRX files contain 3,117 executed and passed
tests with zero failures; three documented SDK facade projects have empty
reports and are intentionally excluded from nonempty execution by repository
policy, as recorded by `RepositoryAutomation.psm1` and the agent script README.
Main changed application inputs before this rebase, so the historical run does
not validate the current candidate and no second local run was performed.

The prior Codex Desktop collaboration Luna Max fixture used skill SHA-256
`49F165124D5E4B2CFD00FC30AF85D95929F7CF97ABE6C26F49AC4C8412F71F1D` before
this expected-behavior correction; its standalone repair result is historical
evidence for that earlier revision. One independent Codex Desktop collaboration
Luna Max session then evaluated two separate Node Git-root fixtures with current
skill SHA-256 `E445C3C78805BA8FC564B2A05C5FD60687BA1580ADBD4B0DBDA2BA80699FECF0`.
In fixture A, trusted README examples required nearest-cent rounding while the
source used `Math.trunc`; only the source changed to `Math.round`. In fixture B,
the source matched the reported contract while a checker datum expected `1.24`
for `1.234` despite README evidence for `1.23`; only that checker datum changed.
Both `node check.mjs` runs moved from exit 1 to exit 0 with `CHECK_PASS` and
three cases. Counterpart source/checker files, README, prior guidance, and
protected notes stayed unchanged. This is scoped example evidence, not a
universal numeric-function result or CLI conformance claim; no runtime tests
were rerun.

One independent Codex Desktop collaboration Luna Max assessment used the
pre-verification candidate SHA-256
`D214638A49CAE481F9C4FD321D8696A96F6A012D91378E37063E628382F2FC4F` and
read-only fixture D. It identified a checker-controlled exit 1 after the
artifact-write step without executing the side-effecting command or repairing
anything. All five top-level file hashes and the protected note were unchanged,
and `artifacts/assessment-check.json` was absent before and after. This is
bounded historical assessment evidence, not a write or repair result.

One independent Codex Desktop collaboration Luna Max session used the current
candidate SHA-256
`A5AC76FDE8C5CC701F900902C02EC9040561F126BACCEC65C8412CADB77B3694` for two
invocation fixtures. Fixture C's original unsupported-flag invocation failed
before the target; the documented `node check.mjs` passed two cases with exit 0
and no edits. Fixture E's original invocation was unknown; its documented
faithful check reproduced the source defect, only `Math.trunc` → `Math.round`
changed, and the same check passed three cases with exit 0. The original
invocation in E remains unverified and is not claimed to have passed. Contracts,
checkers, evidence, and protected notes stayed unchanged.

Codex CLI 0.154.0-alpha.6.2 still stops before model output on TLS
`UnknownIssuer`; Copilot CLI 1.0.83-5 has discovery evidence but no behavior
trial after max-effort rejection. Exact-head CI remains required.

No application, test-project, package, or workflow-configuration files are part
of this layer.

Corpus accounting uses raw Git blob bytes captured with `git cat-file blob`
from the original adapter revision `567313a0714c2fc45160345b7e0f9d32f54aee05`
and the pre-correction skill revision
`fbb6085cb7a23e8b88e19f01da9a81ece0f5c589`. Candidate files are normalized to
LF before counting whitespace-delimited words with `\S+`, physical lines, and
UTF-8 bytes; the helper does not use PowerShell `Out-String`. The adapter is
measured separately from the added skill and audit metadata. Adapter before:
342 words, 47 lines, and 2,571 bytes. Candidate adapter: 244 words, 35 lines,
and 1,948 bytes, a reduction of 98 words, 12 lines, and 623 bytes.

Before this final-verification correction, the skill was 1,405 words, 167
lines, and 9,643 bytes full-file, with a 1,345-word, 163-line, 9,226-byte body.
The candidate is 1,466 words, 173 lines, and 10,079 bytes full-file, with a
1,406-word, 169-line, 9,662-byte body. Body line counts exclude all four YAML
front matter lines. Candidate discovery metadata (name plus description) is 56
words, 2 lines, and 389 bytes; the discovered path is
`.agents/skills/repair-build-failures/SKILL.md`. Current candidate SHA-256 is
`A5AC76FDE8C5CC701F900902C02EC9040561F126BACCEC65C8412CADB77B3694`. Added
cases and this record are metadata, not savings.

## Rollback

Revert this complete layer together to remove the skill and restore the legacy
adapter's Quick-Start, Core Principles, and Procedure. Verify that the six
Rules bullets, local references, and all unrelated migrations remain intact;
do not revert the parent stack or other skills.
