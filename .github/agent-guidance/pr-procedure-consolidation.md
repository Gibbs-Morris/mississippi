# PR procedure guidance consolidation

This focused continuation contributes to [#549](https://github.com/Gibbs-Morris/mississippi/issues/549).
The [six-field plan](https://github.com/Gibbs-Morris/mississippi/issues/549#issuecomment-5849329033)
was recorded before implementation. Source ranges and measurements use the pinned
preparation base `cbc7735777bea703fc2db78a29a68ba972a95590`; root may integrate onto
a later parent. No new skill is introduced. The installed gh-stack package and
[write-pull-request-description](../../.agents/skills/write-pull-request-description/SKILL.md)
remain unchanged.

## Source map

Ranges refer to the original LF Git blob of
[PR size and stacked delivery](../instructions/pr-size-and-stacking.instructions.md).
`R1` through `R18` name its ordered Rules bullets. Every removed support item has
a preserved target in the [audit](pr-procedure-consolidation-audit.json).
The installed gh-stack package is identified by raw file hashes there; upstream
links below also provide the existing fallback when the installation is unavailable.

| Original lines | Supporting content and preserved target |
| --- | --- |
| 38 | One-sentence outcome and unrelated-work separation: R1; [description drafting](../../.agents/skills/write-pull-request-description/SKILL.md#draft-the-requested-content). |
| 39 | Bottom-to-top planning with tests/docs: R11; [local planning](../instructions/pr-size-and-stacking.instructions.md#planning-and-size-judgment); [stack design](https://github.com/github/gh-stack/blob/main/skills/gh-stack/references/stack-design.md#plan-the-layers-before-writing-code). |
| 40 | Current layer and immediate-parent diff: R15; [gate](../instructions/pr-size-and-stacking.instructions.md#advancement-gate); [description evidence](../../.agents/skills/write-pull-request-description/SKILL.md#establish-the-change-and-its-evidence); gh-stack branch placement. |
| 41-42 | Gate before next layer and rechecking grouped landing: R15-R16, R18; complete local gate and planning landing intent. |
| 46-48 | Reviewability, complete prerequisites/proof, one active dependent layer: R1-R2, R6, R9, R15; unchanged evidence paragraph, planning and gate. |
| 78 | Extension setup, separate skill installation, layer design, branch names, explicit remote: [gh-stack setup/non-interactive use](https://github.com/github/gh-stack/blob/main/skills/gh-stack/SKILL.md#setup), [stack naming](https://github.com/github/gh-stack/blob/main/skills/gh-stack/references/stack-design.md#branch-naming), retained R14 and direct-read fallback. |
| 80-92 | Init/stage/commit/submit/view/add example: [core loop](https://github.com/github/gh-stack/blob/main/skills/gh-stack/SKILL.md#core-loop), non-interactive flags and [deliberate staging](https://github.com/github/gh-stack/blob/main/skills/gh-stack/references/stack-design.md#staging-changes-deliberately). Title/body updates use the existing description skill and template; local gate overrides illustrative ungated progression. |
| 94 | Draft/open submission and explicit state inspection: gh-stack core loop/non-interactive use/reading state. Membership verification and unavailable-native fallback remain local. |
| 96 | Authorized merge target/method, ancestor versus whole-stack scope and queue grouping: [merging](https://github.com/github/gh-stack/blob/main/skills/gh-stack/SKILL.md#merging), [command guarantees](https://github.com/github/gh-stack/blob/main/skills/gh-stack/references/commands.md#merge); explicit local target/method confirmation, authorization and full gate remain. |

Removed section headings and whitespace belong to those supporting blocks.
The complete opening prefix/frontmatter, 18 Rules bullets, scope/audience,
planning and size examples, full lockfile provenance procedure, complete
Advancement Gate including lower-layer corrections, and Evidence and References
table are byte-identical. The original hosting/protection paragraph and exact
unavailable-native fallback stay local because their complete constraints are
not duplicates of the installed skill. No incoming link to either removed
summary anchor was found. No content moves to AGENTS.

## Accounting and validation

The owned instruction changes from 109 to 83 LF lines: 26 fewer lines.
The audit records committed-source directory totals, hashes, unchanged discovery
metadata, and explicitly partial unloaded/loaded context proxies. Loading the
stack or description procedure adds context; a source-line reduction alone is
not an activation, billing, latency, or behavioral result. Installed package
hashes describe the verified host files, not a repository-pinned upstream version.

Configured Markdown lint, exact preserved-block comparisons, JSON shape and
unique identifiers, local links/anchors, source mapping, whitespace, and
protected-path checks cover this guidance change. The [six cases](pr-procedure-consolidation-cases.json)
are authored definitions, not passing trials. Fresh inherited-model behavioral
assessment, broader native discovery/model trials and Copilot remain unverified;
Copilot stays deferred. No app build, canonical cleanup/build/unit gate, mutation
result, or remote CI/review readiness is claimed here. Root owns those applicable
delivery gates and serial stack integration.

## Rollback and handoff

Revert this complete layer to restore the supporting summaries and lifecycle
example and remove these three focused records together. Preserve the existing
skills and other migration layers. This preparation makes no remote publication,
stack mutation, review reply/resolution, issue closure, or merge.
