# Legacy test improvement skill migration

This preparation contributes one coherent capability to
[#547](https://github.com/Gibbs-Morris/mississippi/issues/547) under the recorded
[September 26 plan](https://github.com/Gibbs-Morris/mississippi/issues/547#issuecomment-5848635646).
It starts from published snapshot `3d258825642463390c356abdda0b35c92cbdcad7`
on `codex/skills/improve-legacy-tests-prep`. Integration and publication belong
to the root workflow for native stack #680. The user's parallel preparation
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

Same-model native evaluation is coordinated by the root workflow. No worker
selects another model or starts a CLI trial here. Copilot behavior and review
remain deferred until verified capacity returns. Schema, lint and preservation
checks establish structure only; no historical application pass is presented
as validation of this skill or current CI.

## Validation and measurements

Skill schema, configured Markdown lint (four files, zero issues), JSON and
unique-case validation, 27 relative links, whitespace, focused scope and all six
original Rules/frontmatter preservation pass. No protected agent or runtime
path changes. Eleven evaluation cases are prepared and zero are natively
executed; every observed status is `NOT_RUN`. Application builds and cleanup are
outside this authorized instruction-only preparation. Final integrated-head
CI and review remain required; no mutation execution or merge readiness is
claimed.

Raw committed-LF instruction lines fall from 2,804 to 2,792: 12 fewer. The source
file falls from 41 to 29 lines; both entrypoints and every other instruction
remain unchanged. The representative C# legacy-test package contains both
entrypoints, all global instructions, scopes matching
`tests/Legacy.L0Tests/ExampleTests.cs`, and all skill names/descriptions. Using
canonical path/newline/blob/newline framing and o200k_base, it changes from
33,097 to 33,072 proxy tokens (156,123 to 156,185 bytes); loading the skill and
binding totals 34,868 tokens. All-instruction maintenance changes from 53,305 to
53,280 tokens. These are content proxies, not native billing savings; host
framing, plugins, other references and tool outputs are excluded.

| Content | Git blob | LF SHA-256 |
| --- | --- | --- |
| Skill | `6c4f48e11bd9bafda63cdc15c7c25fcfa697e51c` | `8d7833bf1a5bc39eefb4d912c2d558992144b4a361169c193b4b31a55732c063` |
| Source policy | `f1a3860af03a2985e3c1ca269b054c458c420328` | `c5a0d07144a3ccd7ffabf7207ecf78cdf86ea1ef01955da378d46f63dcea2718` |
| Local binding | `fe850d2e800d7b9a65537a7d46d4fbacce0d89fc` | `9c0f226308763e8b194aa17fbabad8b784db71f0cf2112b8fabd0de743b81626` |
| Cases | `78b207aa340d8a93d45a5ab3e849f024053c40d9` | `81365542d781eaf2719098381cdc4e5088ceb490390cb59afa41e0c5a643a741` |

Representative package LF SHA-256 before/after/loaded:
`ee13644c00e99e5eae8652f786adf8e674eb1393311bc0862a834e628321074e`,
`2fbcc29805e6c3968553b3684d96b954550bd51c7890fb12a1095675c3af0821`,
`6e05a994d0f0480edf27e714054aaad99cf66c38996dcb54baea46a7ef20200c`.
All-instruction package before/after:
`d4fb5729072556cdd3f9aca8c50fe668cf33178f8375880a3336c8bca9f586d1`,
`473c4b57f5d1c8c7b508d6022b77bc28cdaafa86f2971818c09c6115ed1925ea`.

## Rollback

Revert the complete owning stack layer to restore the removed summaries and
remove the skill, added route, binding and evaluation records together. Preserve
ancestor layers and unrelated work. If a later layer consumes this capability,
inspect that dependency before rollback; removing only the skill would leave a
broken mandatory route. This slice changes no application behavior or agent file.
