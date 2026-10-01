# Legacy test improvement skill migration

This preparation contributes one coherent capability to
[#547](https://github.com/Gibbs-Morris/mississippi/issues/547) under the recorded
[September 26 plan](https://github.com/Gibbs-Morris/mississippi/issues/547#issuecomment-5848635646).
Its isolated preparation started at published snapshot
`3d258825642463390c356abdda0b35c92cbdcad7`; initial accounting used base
`23cf0ba2e89d56f76d4fd358a5260b639099ed4f` in native stack #680. That pinned
snapshot is historical after restacking; live PR ancestry and current gates
come from the latest #532 checkpoint. The user's parallel preparation
authority overrides the old issue's introduce/reduce two-PR pattern; this is
one skill, its route, binding, reduction, and evidence in one proposed layer.

## Source map and authority

| Original content | Destination or retained authority |
| --- | --- |
| All six original Rules and `applyTo` frontmatter | Retained exactly in [test-improvement policy](../instructions/test-improvement.instructions.md); one mandatory skill and direct-file binding route is added. |
| Governing thought, drift check, scope, and existing references | Retained; source rules stay effective independently of discovery. |
| Tool restore, fast tests/coverage, no-build iteration, warning-as-error build | [Local binding](legacy-test-improvement-bindings.md) links verified command/configuration sources; portable procedure preserves required gates. |
| Optional mutation command and existing-report task refresh | Existing mutation workflow retains execution/report ownership; local binding preserves `-SkipMutationRun` and warns that summarizers can write. |
| Repeated Core Principles | Determinism, tests-only authority, bounded iteration and report-based gap work remain in rules, binding and [portable skill](../../.agents/skills/improve-legacy-tests/SKILL.md). |
| Characterization, contract uncertainty, meaningful assertions, safe flaky-test handling | Portable procedure distinguishes observed behavior from intended behavior and does not authorize production changes. |
| Test levels, naming, assertions, packages, warnings, coverage and scripts | Discovered through authoritative local sources; no Mississippi convention is imported into the portable skill. |

The skill owns strengthening tests for existing behavior. Build failure repair,
new feature implementation, ordinary test strategy, explicit mutation outcomes,
issue/PR tracking, and final release approval remain separate workflows. All
other instructions, AGENTS, custom agents, production/tests, runtime, scripts,
workflows and packages are unchanged by this slice.

## Evaluation boundary

[Evaluation cases](legacy-test-improvement-skill-cases.json) include raw
portable JavaScript artifacts, concrete expectations, positive explicit and
paraphrased requests, unrelated/incidental negatives, incomplete target,
partial/missing evidence, flaky time handling, contract disagreement, and
neighboring-workflow collisions. Expected outcomes are a rubric, not passed
native evidence. A checker should run authored tests against the protected
implementation and disposable faulty variants, check meaningful boundary
sensitivity, and compare every protected artifact hash; wording matches alone
do not prove behavior.

A fresh worker inheriting this chat's exact model/settings selected the skill
from a supplied file catalog and strengthened the consuming shipping tests from
one truthiness assertion to 34 tests with 75 assertions. The executed suite
passed without skips or todos. All 19 protected production/catalog/checker
inputs stayed unchanged; only the permitted test file changed. Root reran the
34-test suite and checked four disposable production faults: threshold,
membership conjunction, wrong fee, and omitted validation. Each fault was
detected by failing assertions (1, 2, 1, and 31 tests respectively), while the
original production file remained unchanged. The skill LF hash below matches
the trial catalog input.

This bounded supplied-catalog task is not native CLI/app/IDE discovery or the
complete eleven-case matrix. Its old coverage record lacked source/branch and
assertion-strength evidence, so it was not promoted to current coverage proof.
No Stryker run or repository mutation score is claimed. Copilot remains deferred
and unverified until verified capacity returns under the user's instruction.

## Validation and measurements

Skill schema, configured Markdown lint, JSON/unique-case validation, relative
links, whitespace, focused scope, and all six original Rules/frontmatter/audience
checks passed during preparation and are rechecked after integration. No
protected agent, application, build or package input changed. Current-head/base
CI and review remain required before draft readiness.

Raw LF instruction-directory lines change from 2,744 to 2,732: 12 fewer.
The source policy changes from 41 to 29 lines; both entrypoints and all other
instruction files are unchanged. Static context measurements are proxies that
include discovery metadata and distinguish activation cost; they do not measure
native billing savings. Whole-stack accounting is kept in the issue checkpoint.

| Content | LF SHA-256 |
| --- | --- |
| Skill | `8d7833bf1a5bc39eefb4d912c2d558992144b4a361169c193b4b31a55732c063` |
| Current local binding | `9d5bf412405bc127feddd06ec4b44dbda588d44f3b393d2d266bc2ef39e47aeb` |

## Rollback

Revert the complete owning stack layer to restore the removed summaries and
remove the skill, added route, binding and evaluation records together. Preserve
ancestor layers and unrelated work. If a later layer consumes this capability,
inspect that dependency before rollback; removing only the skill would leave a
broken mandatory route. This slice changes no application behavior or agent file.
