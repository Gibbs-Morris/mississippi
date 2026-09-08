# PR description skill migration

This layer extracts title/body drafting into
[write-pull-request-description](../../.agents/skills/write-pull-request-description/SKILL.md).
It contributes to [#549](https://github.com/Gibbs-Morris/mississippi/issues/549)
and [#532](https://github.com/Gibbs-Morris/mississippi/issues/532), using the
user's current one-complete-PR-per-skill delivery instruction.

Parent: [PR #585](https://github.com/Gibbs-Morris/mississippi/pull/585), head
`a03316d401e46b0ce1579ce4424213bd1e7c8dc3`, checked September 8, 2026. Its 30
applicable GitHub checks passed, reviews completed, all inline threads were
resolved, and GitHub reported `CLEAN` before this layer started. Pages deployment
was intentionally skipped because its workflow deploys only pushes to main.

## Why this is a skill

A template supplies headings but cannot determine the actual comparison base,
exclude ancestor changes, reconcile a stale description, or distinguish current
verification from old green results. Those repeated drafting decisions form a
bounded workflow. The skill leaves title conventions, required sections, quality
gates, and permission boundaries with the consuming project and caller.

The skill has a precise name/description and a single self-contained procedure.
It has no scripts, dependencies, product names, repository paths, fixed shell
commands, or additional tool permissions. It accepts local templates and audit
requirements instead of embedding copies. This follows the documented shared
root and progressive loading model in
[Codex](https://learn.chatgpt.com/docs/build-skills),
[Copilot](https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/add-skills),
and the [Agent Skills specification](https://agentskills.io/specification).

## Source-to-destination map

| Source | Disposition |
| --- | --- |
| PR-authoring instruction's 15 normative bullets, semver table, title examples, scope, and references | Retained verbatim |
| Instruction Quick-Start, initial/update drafting procedures, repeated template structure, explanatory examples, and Core Principles | Replaced by the skill's evidence, drafting, and revision procedure |
| PR Manager title examples and duplicated description heading list | Replaced by skill, policy, and template links |
| PR Manager delegation, hard rules, audit summary/provenance, freshness loop, thread handling, and merge readiness | Unchanged |
| Epic Builder body heading list | Replaced by the same drafting route; sub-plan contribution and master-plan/dependency links explicitly retained |
| Epic Builder publication, title, base selection, completion marker, and advancement workflow | Unchanged |
| Repository PR template | Unchanged; still the local output contract |

## Verification and limits

The bundled skill validator passed. All 15 policy bullets and the complete
Rules section match the parent exactly. Prefix/suffix comparisons verify that
the affected agents retain their non-drafting workflows and authority boundaries.
All three consumer links resolve to the skill, the template is unchanged, the
skill contains no repository bindings, and the eight evaluation cases parse.
Markdown lint passed for the skill, policy, both changed agents, and this record.

Codex CLI 0.153.4 `skills/list` found the enabled repository skill once with no
discovery errors. Copilot CLI 1.0.83-5 `skill list --json` found the enabled
project skill in the same `.agents/skills` directory. These checks establish
discovery, not reliable implicit selection or behavior on every supported host.

The drafting procedure is used for this layer's own PR description against its
immediate parent. The committed
[evaluation cases](pr-description-skill-cases.json) cover explicit, implicit,
paraphrased, breaking-change, incomplete, unrelated, governed, and uncommitted
inputs. They are a reusable rubric, not a record of fresh model trials. Full
cross-agent behavioral conformance and app/IDE smoke tests remain unverified;
this PR does not claim to complete every evaluation checkbox in #549.

A manual walkthrough of the supplied-diff case found an overly strict initial
requirement for base/head metadata. The skill now accepts sufficient supplied
change evidence for a draft without demanding repository access or commit IDs,
while retaining actual-base checks for live PR/branch comparisons and avoiding
claims of independent inspection when files were not accessible.

The globally applied instruction shrinks from 1,002 to 605 whitespace-delimited
words, a reduction of 397 words and 59 lines. The two agent procedures remove
another 101 words and 17 lines; those agents are loaded only when selected.
Skill discovery metadata adds its own startup cost. These are text-corpus
measurements, not measured token counts or latency improvements. No application,
test-project, package, or workflow configuration changes are included.

## Rollback

Revert this single layer to remove the skill and restore its three original
consumer procedures together. Verify the policy and template remain unchanged
and the restored references resolve. Do not revert the logging parent or another
independent migration.
