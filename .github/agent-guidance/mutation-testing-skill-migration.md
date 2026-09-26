# Mutation-testing skill migration

This layer extracts the explicit mutation-testing workflow into
[run-mutation-testing](../../.agents/skills/run-mutation-testing/SKILL.md). It
contributes to the open [#548](https://github.com/Gibbs-Morris/mississippi/issues/548)
work item under the verified
[implementation plan](https://github.com/Gibbs-Morris/mississippi/issues/548#issuecomment-5652236218)
and the [#532](https://github.com/Gibbs-Morris/mississippi/issues/532)
skills-migration epic. The migration keeps mutation optional, evidence-based,
and owned by the existing test and quality workflows.
The September 26 scope excludes custom agents; Test Engineer remains unchanged.

## Parent gate and boundary

Before implementation, parent PR684 head `e933c33084d406c00c754aa8741d3c36e8f59b19`
against `877f562a2f82bdf78f849475642f26cf6157b3e6` passed its 08:29 UTC gate as
`CLEAN` and `MERGEABLE`, with 30 successful checks, all 11 required checks
passing, and 7 of 7 review threads resolved. The same-head Sonar rerun passed
after the public service recovered, and the gate was reverified at 08:58 UTC.
This is historical parent advancement evidence; this layer needs its own
exact-head CI and review gate.

The portable skill handles report assessment, authorized focused execution,
proportionate survivor analysis, and mutation evidence reporting. It does not
own ordinary test strategy, legacy test implementation, build-failure repair,
QA approval, PR or branch operations, or workflow audit state.

## Why this is a skill

The mutation policy and testing policy repeated an optional route and detailed
execution decisions in startup context. The adapter now keeps compact atomic
invariants and a mandatory skill/direct-file route. Repository commands,
targets, Stryker configuration, report locations, and the sample-solution
exception remain in a local binding that the portable package does not require.

## Source-to-target map

| Source requirement | Destination and preservation |
| --- | --- |
| Mutation rule 1 | The adapter's additional-signal rule and the skill's mode/acceptance step preserve mutation as optional unless the caller makes it part of acceptance. |
| Mutation rule 2 | The adapter's separate threshold and maintain-or-raise rules, plus the skill's interpretation step, reject both gates when inferred from tooling rather than declared policy or acceptance. |
| Mutation rule 3 | The adapter's priority rule and the skill's interpretation step preserve requested correctness, maintainability, and conventional tests. |
| Mutation rule 4 | The skill asks for meaningful mutation-resistant tests on new behavior when straightforward and proportionate, without making that a completion gate. |
| Mutation rule 5 | The adapter's effort rule and the skill's survivor step bound work and avoid repeated gap chasing unless explicitly requested. |
| Mutation rule 6 | The skill's survivor and report steps defer costly or historical gaps to dedicated follow-up when appropriate. |
| Mutation rule 7 | The local binding retains the routine `-SkipMutation` test-and-coverage route; the testing adapter routes explicit mutation work to the skill. |
| Mutation rule 8 | Adapter pre-run rules and the skill's preparation step require tool restore and the locally required clean build before an authorized run. |
| Mutation rule 9 | The adapter's focused-effort rule and the skill's preparation step preserve focused target selection and a proportionate effort bound. |
| Mutation rule 10 | The adapter's reporting rule and the skill's report step retain execution status, scores, report paths, and significant gaps. |
| Mutation rule 11 | The adapter and skill reject skipped, failed, interrupted, or incomplete mutation runs as passing evidence. |
| Mutation rule 12 | The adapter and skill limit score claims to targets and revisions supported by valid reports, including incomplete coverage. |
| Mutation rule 13 | The adapter and skill distinguish mutation-tool or threshold results from build/test failures and task acceptance. |
| Mutation rule 14 | The adapter and skill preserve the justified, caller-authorized production-edit exception for a survivor proven unkillable by appropriate tests. |
| Mutation rule 15 | The adapter's preflight-stop rule and the skill's preparation step keep mutation stopped while build warnings or conventional-test failures invalidate preflight; the skill requires a repaired, passing preflight before resuming. |
| Mutation rule 16 | The adapter and skill prefer valid existing reports before another run; the local summarizer remains authoritative for report reuse. |
| Testing policy optional mutation route | Replaced by a direct route to the skill and local bindings; xUnit/MTP, levels, coverage, determinism, warnings, CPM, and legacy-test rules remain in testing policy. |
| Existing Test Engineer mutation responsibility | Agent file remains unchanged; existing mutation/testing policy references resolve to the retained adapter and its shared procedure/local bindings. |

No source requirement is dropped. The adapter retains the mandatory workflow and
fail-closed outcomes; the skill provides the detailed sequence; local bindings
provide this repository's commands and report contract.

## Local bindings and ownership

`.github/agent-guidance/mutation-testing-bindings.md` records the supported
solution and focused commands, tool restore/build preflight, report manifests,
configured reporters and thresholds, and the current sample-solution exception.
Those details remain local and are not portable skill triggers.

The mutation skill owns mutation execution evidence and survivor analysis. The
Test Engineer owns test implementation and semantic validation. The repair skill
owns restore, build, and test failures. The mutation and testing policies,
quality scripts, report summarizer, and Stryker configuration remain authoritative
for their behavior.

## Research and placement rationale

Shared skill placement and progressive loading were previously verified against
[ChatGPT build-skills guidance](https://learn.chatgpt.com/docs/build-skills),
[GitHub Copilot skill guidance](https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/add-skills),
the [Agent Skills specification](https://agentskills.io/specification), and
[skill-creation best practices](https://agentskills.io/skill-creation/best-practices).
Their focused-trigger and progressive-disclosure guidance supports one
instruction-only mutation package with local supporting bindings.

## Validation and limits

The [evaluation cases](mutation-testing-skill-cases.json) cover report-only
assessment, authorized focused execution, preflight failure, report reuse,
threshold/tool distinctions, optional manifests, declared gates, incomplete
scope, sample or ordinary-test routing, proportionate survivors, and the
justified production exception. They are a rubric; the recorded assessment
below is separate from the current native trial. The 17 cases are a rubric;
neither the historical assessment nor the native trial is a mutation run.

One direct Luna Max collaboration assessment used the canonical LF input at
SHA-256 `2D25A268D9AFF060C150ABF040196E2EDB360BADF73E8A35CDC17DEF8A2C30CE`
(7,018 bytes). In the supplied threshold report, the focused `0.82` evidence
was usable for interpretation but exit code `1` remained a configured-tool
threshold failure; no invocation or gate was invented. In the stale-target
report, the matching `0.91` alpha evidence was retained while the beta report
from an older revision made the selected aggregate incomplete; no score was
combined or rerun was authorized. Both fixture roots stayed clean and all
baseline/protected hashes were unchanged. These are bounded report assessments,
not mutation execution or native CLI behavior evidence.

A discovery-only check resolved this package from the migration checkout and a
separate Git root in both Codex CLI `0.154.0-alpha.6.2` and GitHub Copilot CLI
`1.0.83-5`; each command exited `0` and identified the project-local skill path.
The copied input matched SHA-256 `2D25A268D9AFF060C150ABF040196E2EDB360BADF73E8A35CDC17DEF8A2C30CE`
and Git blob `131a8f79720649d9b10d79d9ee9f6df7724149f6`. This establishes
packaging and discovery only; no model inference, authentication, install,
global configuration, remote mutation, or native behavior equivalence was
tested.

Structural validation covers the skill schema, configured Markdown lint, JSON
parsing, relative links, portability, source-map coverage, local-binding links,
and raw-parent comparison confirming the unchanged Test Engineer file. This repository's configured thresholds are
reported as tooling behavior and are not treated as acceptance gates; declared
target-specific policy or caller acceptance remains applicable where present.

Because the local summarizer writes enriched and summary artifacts even with
`-SkipMutationRun`, report-only validation uses raw reports unless those writes
are authorized.

### September 26 review corrections and native evidence

The portable trigger includes PR mutation-evidence assessment, and warning
preflight follows the consuming project's policy. Mississippi's focused binding
now names the canonical clean warn-as-error build, existing report-bound source
provenance, the solution provenance gap, and ambiguous focused failure statuses.
Native Codex CLI `0.158.0-alpha.2.1`, GPT-5.5 medium, assessed six supplied-record
cases at skill blob `4d842febfec30c2573297a0a871fda3d337ea896`, LF SHA-256
`066ec00cee9c86b99ee43023f1386dfb3d00033f34fa3fae29f61a43f656b222`.
It reused valid focused companion provenance, rejected current-revision claims
for unbound solution reports, left generic focused failure causes unverified,
distinguished a diagnosed advisory threshold exit, respected another consumer's
allowed warnings, and selected Mississippi's required build preflight.
All five fixture file hashes stayed unchanged. Commands read the candidate,
local binding and host guidance/memory; no checker, mutation run or write occurred.
This proves bounded decisions from synthetic supplied evidence, not all 17 cases,
exclusive skill influence, current application validation, or native savings.
Copilot remains unverified and deferred by the user while capacity is unavailable.

## Historical corpus accounting

Counts use raw Git blobs or LF-normalized candidate copies. UTF-8 text uses
`str.split()` words and `str.splitlines()` physical lines; raw UTF-8 bytes and
UTF-16-LE bytes divided by two are reported. Complete four-line skill front
matter is excluded from body counts. No PowerShell `Out-String` or worktree
line-ending assumption is used.

| Content | Source | Words | Lines | UTF-8 bytes | UTF-16 units |
| --- | --- | ---: | ---: | ---: | ---: |
| Mutation policy before | parent commit `e933c33084d406c00c754aa8741d3c36e8f59b19` | 827 | 63 | 6,347 | 6,347 |
| Mutation policy candidate | LF candidate SHA-256 `97CAF5D5EB6708AE5A77A002F528AC47F9402F0181F3972DF26D77214E0F46B9` | 512 | 47 | 4,155 | 4,155 |
| Testing policy before | parent commit `e933c33084d406c00c754aa8741d3c36e8f59b19` | 1,120 | 100 | 8,511 | 8,509 |
| Testing policy candidate | LF candidate SHA-256 `542FB14DCADF5E82293381EB013ED489C684181E6321104FF283D971C4BA5D9C` | 1,112 | 103 | 8,511 | 8,509 |
| Skill full file | staged Git blob `131a8f79720649d9b10d79d9ee9f6df7724149f6`; LF SHA-256 `2D25A268D9AFF060C150ABF040196E2EDB360BADF73E8A35CDC17DEF8A2C30CE` | 984 | 119 | 7,018 | 7,018 |
| Skill body after complete front matter | same staged Git blob and LF copy | 931 | 115 | 6,586 | 6,586 |
| Discovery metadata values | same staged Git blob; name and description joined with one space | 51 | 1 | 423 | 423 |
| Local mutation binding | LF candidate SHA-256 `D9BE40DDDFA86C844C2DCAB5FB34EDB801F5766DFEC827E7432C323C68339DE6` | 508 | 68 | 3,907 | 3,907 |

The movable mutation source slice was 28 lines, 294 words, and about 2,330
bytes before this migration. These measurements are static context figures,
not startup or runtime savings claims.

## Rollback

Revert this complete layer together to remove the portable skill, local binding,
cases, audit, adapter route, and testing route. Preserve the unchanged Test Engineer file and restore the
original mutation policy and testing optional-mutation section, then verify that
the legacy-test workflow, application files, and preceding skills remain intact.
