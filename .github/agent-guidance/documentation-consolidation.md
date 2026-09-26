# Documentation guidance consolidation

This is a focused continuation of the one-skill, nine-contract boundary from
[merged PR #622](https://github.com/Gibbs-Morris/mississippi/pull/622).
The existing [skill](../../.agents/skills/author-technical-documentation/SKILL.md)
and all nine references remain unchanged. It contributes to #555 and #556
and the #532 programme without completing their remaining evaluation criteria.

The six-field plan was recorded before implementation in
[#556](https://github.com/Gibbs-Morris/mississippi/issues/556#issuecomment-5848675525)
and [#555](https://github.com/Gibbs-Morris/mississippi/issues/555#issuecomment-5848675670).
The source baseline is `3d258825642463390c356abdda0b35c92cbdcad7`.
Initial integration used parent `4f54b7e368d81a814bd777f63f4ada56b50180cd`;
the instruction directory changed from 2,732 to 2,680 LF lines. That pinned
accounting snapshot is historical after restacking; use the live PR base and
latest #532 checkpoint for current ancestry and CI.
This source/contract consolidation does not claim a newly approved taxonomy,
measured activation quality, or evaluation sign-off.

## Source map

Line ranges refer to the committed baseline, not the shortened files.
Every removed supporting item remains available through the targets below.

| Source | Removed supporting content | Preserved target |
| --- | --- | --- |
| [Page focus](../instructions/documentation-page-focus.instructions.md), 24-30 | Four quick-start items | [Skill selection](../../.agents/skills/author-technical-documentation/SKILL.md#select-the-page-contract), [scoped rules](../instructions/documentation-page-focus.instructions.md#rules-rfc-2119), and the skill's scoped writing procedure |
| Page focus, 31-44 | Nine classification questions | All nine reader-intent rows in [skill selection](../../.agents/skills/author-technical-documentation/SKILL.md#select-the-page-contract) |
| [Feature structure](../instructions/feature-documentation-structure.instructions.md), 26-32 | Four quick-start items | [Scoped rules](../instructions/feature-documentation-structure.instructions.md#rules-rfc-2119) retain folder stability, page splitting, `_category_.yml`, and orientation pages |
| Feature structure, 33-41 | Four transition restatements | [Scoped rules](../instructions/feature-documentation-structure.instructions.md#rules-rfc-2119), [navigation](../../docs/Docusaurus/docs/contributing/documentation-guide.md#file-and-navigation-rules), and [migration stance](../../docs/Docusaurus/docs/contributing/documentation-guide.md#migration-stance) |
| [Feature pattern](../instructions/feature-docs-pattern.instructions.md), 38-44 | Four quick-start items | [Scoped rules](../instructions/feature-docs-pattern.instructions.md#rules-rfc-2119) retain page type, shared setup, generated-first ordering, and the manual-path rationale |
| [Authoring](../instructions/documentation-authoring.instructions.md), 57-70 | Runtime-checklist introduction and eleven topics | Exact unchanged [local checklist](../../docs/Docusaurus/docs/contributing/documentation-guide.md#distributed-systems-checklist), reached directly from the retained instruction heading |

The four files retain their frontmatter and 38 rule bullets byte-for-byte.
The nine-row contract/local-guide table, fallback procedure, and ADR route are
unchanged. The runtime checklist's eleven bullets match the public guide
exactly; the retained rule still requires the applicable topics. Metadata,
relative links, Mermaid layout, rendering/build completion, generated/manual
semantics, applicability examples, and core principles remain local.
No ADR or C4 guidance, Markdown/RFC policy, custom agent, AGENTS, public page,
application, package, or workflow is changed.

## Measured scope

Committed LF source blobs total 237 instruction lines before and 185 afterward:
52 fewer lines, including the replacement routes. This is an instruction-text
measurement, not a measured token, latency, or behavioral improvement.
The [audit](documentation-consolidation-audit.json) records raw-blob hashes and
per-file counts. Evidence records add review content separately; no content is
moved into AGENTS to produce this reduction.

## Validation and remaining work

Exact source comparisons cover every retained rule, frontmatter block, and
contract-table row. Link and anchor checks cover the modified instructions,
unchanged skill package, and this record. JSON/schema and configured Markdown
lint results are recorded in the audit. The
[focused cases](documentation-consolidation-cases.json) are definitions, not a
passing native-results table.

The authored consuming fixture produced a how-to page, a linked reference page,
and command evidence following its local Overview/Steps/Validation contract.
Root inspected those outputs, the unchanged sample hash, and the actual scoped
Git status (`?? sample.txt`); configured lint passed. This establishes only the
inspected page output, structure, and link evidence. It is not a blind activation
trial, a Copilot trial, native CLI conformance, or a separate model behavior trial. Fresh
activation/collision/output trials, the complete cross-model matrix, and
Copilot app/IDE discovery remain unverified. Copilot trials are deferred until
capacity is verified. No expensive documentation/application builds or
mutation runs are claimed; this layer changes guidance only. Root delivery
still requires current head/base CI and complete review disposition.

## Rollback

Revert this complete layer to restore the four supporting blocks and remove
its audit/case records together. Confirm the original rule/table equality,
runtime-checklist availability, and instruction links. Leave PR #622's skill,
nine contracts, and all earlier migration layers intact. Root owns serial
native-stack #680 integration and publication. User authorization allows
independent parallel draft publication while CI runs; final readiness still
requires current checks and feedback, with no merge or auto-merge authority.
