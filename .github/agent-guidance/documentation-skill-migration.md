# Technical-documentation skill migration

This layer introduces
[author-technical-documentation](../../.agents/skills/author-technical-documentation/SKILL.md)
with one reference per page type. It contributes to
[#555](https://github.com/Gibbs-Morris/mississippi/issues/555),
[#556](https://github.com/Gibbs-Morris/mississippi/issues/556), and
[#532](https://github.com/Gibbs-Morris/mississippi/issues/532).

Initial parent: [PR #620](https://github.com/Gibbs-Morris/mississippi/pull/620)
at `0b412b85ece94bcde4f2cfe406fb4caffe4392cf`, on main
`4aed4bcf9fab3200ec8c55b39e131576cf780bf8`. Before this layer started, the
parent had 29 passing checks, completed Codex reviews, a positive Copilot review,
resolved threads, and `CLEAN` status. Ancestor required checks and reviews also
passed. The intermediate PR's aggregate merge flag remained `BLOCKED` without
a failed check or unresolved review requirement identified; that flag is retained
for the eventual merge audit rather than represented as merge readiness.

## Boundary and alternatives

The outcome is one authored, revised, or validated engineering page. The shared
procedure selects reader intent, collects evidence, applies the selected format,
and verifies the requested output. Page-type detail belongs in separate
references so it need not all load for every task. Review-only callers keep their
authority and output contracts; ADR and PR-writing workflows remain separate.

One skill per existing filename would duplicate this procedure and enlarge the
discovery catalog. Keeping the nine full instruction files alongside the skill
would preserve the startup load and create another maintained copy. The chosen
boundary therefore uses one workflow with nine conditional contracts. This is
an evidence-backed source/contract design, not a claim that competing portfolios
have passed fresh model-activation trials or that #555 is fully complete.

## Mandatory routing and preservation

The broad documentation-authoring instruction retains all 16 original rules
and its runtime checklist. One additional rule requires authors and reviewers to
read and follow the selected portable contract and local guide before drafting
or validation. Direct-file reading remains available if skill discovery is not.
This explicit route replaces the deleted page-type instruction files; required
behavior does not depend solely on implicit skill selection.

| Page type | Original rule count | Replacement |
| --- | ---: | --- |
| Getting started | 6 | [Contract](../../.agents/skills/author-technical-documentation/references/getting-started.md) |
| Tutorials | 6 | [Contract](../../.agents/skills/author-technical-documentation/references/tutorials.md) |
| How-to | 6 | [Contract](../../.agents/skills/author-technical-documentation/references/how-to.md) |
| Concepts | 5 | [Contract](../../.agents/skills/author-technical-documentation/references/concepts.md) |
| Reference | 6 | [Contract](../../.agents/skills/author-technical-documentation/references/reference.md) |
| Operations | 6 | [Contract](../../.agents/skills/author-technical-documentation/references/operations.md) |
| Troubleshooting | 6 | [Contract](../../.agents/skills/author-technical-documentation/references/troubleshooting.md) |
| Migration | 6 | [Contract](../../.agents/skills/author-technical-documentation/references/migration.md) |
| Release notes | 6 | [Contract](../../.agents/skills/author-technical-documentation/references/release-notes.md) |

All 53 rules retain their obligation levels. Portability edits change "this file"
to "this reference", generalize the concept rationale/comparison example, and
replace the getting-started public-guide pointer with the same structure below
the rules. Required structures are copied from the corresponding local guides;
reference documentation retains its flexible relevant-sections template.

Local guides, page-focus policy, metadata, paths, build gates, diagram limits,
and publication requirements remain local. The Technical Writer's obsolete
inline example layouts are replaced by the authoritative contracts. Its scope,
evidence workflow, and report format remain intact. The Doc Reviewer and shared
Clean Squad rule change only their instruction routes. ADRs retain their
specialized policy/template rather than being forced into product-page layouts.

## Validation and limits

Schema and Markdown lint passed. Normalized source comparisons preserve every
conditional rule, all guide structures, all 16 core rules, the runtime checklist,
page-focus policy, and caller authority/output. All local guides are unchanged.
All skill and adapter links resolve; a repository search found no remaining
references to the nine deleted instruction paths or old page-type instruction
wording. The portable package contains no source-repository bindings.

Both installed CLIs discover the enabled skill, including a complete copy in a
separate temporary Git repository. A manually authored how-to fixture follows
the selected structure, uses valid local metadata and links, and passes lint.
Its `git status --short -- sample.txt` command was actually executed in the
fixture and returned the documented untracked-file result. This is scoped
template/output evidence, not a fresh cross-agent model trial.

[Ten cases](documentation-skill-cases.json) define positive, partial-update,
review-only, missing-evidence, local-format, and negative scenarios. Fresh
activation/collision/output trials and app/IDE smoke tests remain unverified;
the cases are a rubric, not a passing-results table.

The nine type files and broad authoring instruction total 2,743 words before
the change and 809 afterward. The shared instruction route adds seven words,
giving a net reduction of 1,927 words across mandatory instruction reads. Agent
changes remove another net 132 words. Nine instruction files are retired.
Discovery metadata adds its own cost; no measured token or latency claim is made.

## Review size and rollback

The PR exceeds the 600-line target because moving the nine contracts counts both
their deletion and addition. One complete router/contract migration is safer to
review than a partially connected skill or nine duplicate skills created only
to satisfy a line count. Review the mandatory route first, then each table row's
normalized rule/structure comparison, then caller boundaries and validation.

Revert this layer to restore all nine instruction files and original routes
together. Verify the guide links and caller contracts; leave earlier skill
migrations intact. No actual public documentation page, application code,
package, or CI configuration changes are included.
