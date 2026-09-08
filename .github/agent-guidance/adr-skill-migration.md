# Architecture-decision skill migration

This layer extracts decision-record authoring and the shared body template into
[author-architecture-decision](../../.agents/skills/author-architecture-decision/SKILL.md).
It contributes to [#553](https://github.com/Gibbs-Morris/mississippi/issues/553)
and [#532](https://github.com/Gibbs-Morris/mississippi/issues/532), using one
complete PR for the skill, direct consumer routes, and replaced procedure.

Initial parent: [PR #618](https://github.com/Gibbs-Morris/mississippi/pull/618), head
`2f54a5f19004cb3de05ba6c531521db1e19b2bbe`. Before this layer started on September
8, 2026, the parent passed 29 GitHub checks; its logging ancestor passed 30
applicable checks with an intentional Pages deployment skip. Both had completed
Codex code/security reviews, positive Copilot reviews, resolved inline threads,
and `CLEAN` GitHub status against main
`cc31f1a9c061f9ea83ef0c9eadbabc5b397cf9c6`.

Before submission, the stack was rebased onto the subsequently merged assertion
migration in main `e5b5054f4253046c2f16989eb417683ed9747734`. Current-head gates
are re-established after that rebase; the initial readiness evidence above does
not prove the rebased revisions are ready.

## Portable boundary

The workflow records an evidenced decision, handles narrow updates and
superseding, and checks local lifecycle and numbering conventions. It does not
design a system, approve a decision, publish a document, or assume workflow
ownership. The body template is separate from local publishing metadata so the
folder can be copied to another project without its source repository.

The body structure follows the
[released MADR 4.0.0 template](https://github.com/adr/madr/blob/4.0.0/template/adr-template.md),
whose [release license](https://github.com/adr/madr/blob/4.0.0/LICENSE) lists
`MIT OR CC0-1.0`. The asset adapts the existing repository template with generic
placeholders and no site-specific frontmatter. The released format was checked
instead of silently using the development template.

## Source-to-destination map

| Source | Disposition |
| --- | --- |
| ADR instruction's 15 rules | Retained; only the first rule's template pointer changes to the skill/body plus required local frontmatter |
| Local title/H1, date, and initial-status bindings | Retained explicitly in the adapter from the former template and Quick-Start |
| ADR Quick-Start, repeated diagram test, body template, and Core Principles | Replaced by the skill procedure and generic body, with diagram obligations retained in the rules |
| ADR Keeper significance/numbering procedure and body template | Replaced by the shared authoring route |
| ADR Keeper role, hard rules, publishing boundary, and CoV verification | Retained; template reference alone is redirected |
| Product Owner ADR delegation prompt | Template reference updated; delegation, output paths, notes, and other workflow behavior unchanged |
| Clean Squad instructions and workflow ADR protocol | Wording changes from template "defined in" the policy to "specified by" the policy; authority and lifecycle requirements unchanged |

## Verification and limitations

The skill validator and Markdown lint passed for the skill, asset, and changed
guidance. Comparing the complete ADR Rules section with the parent, allowing
only the explicit template-pointer replacement, preserves all 15 rules. The
required local metadata remains present. Equivalent comparisons preserve the
Keeper's hard rules/verification and verify that the Product Owner, shared
instructions, and workflow have only their documented reference changes.

The asset contains the MADR required and optional section structure. A manually
populated synthetic example has no remaining placeholders, parses its local
YAML metadata, keeps the filename ordinal aligned with ordering metadata, and
passes Markdown lint. This is a template exercise, not a real architectural
decision or a fresh agent-generated behavioral trial.

Codex CLI 0.153.4 and Copilot CLI 1.0.83-5 discover the enabled skill in
`.agents/skills`; Codex reports no discovery errors. The
[ten evaluation cases](adr-skill-cases.json) cover drafting, narrow updates,
superseding, collisions, different project conventions, missing information,
and negative activation boundaries. They are an evaluation rubric, not recorded
passing model trials. Full cross-agent behavioral conformance and app/IDE smoke
tests remain unverified, so this PR does not close every evaluation item in #553.

The ADR instruction shrinks from 1,069 to 746 whitespace-delimited words,
removing 323 words and 82 lines from the currently mandated instruction reads.
The two agent changes remove a net 272 words and 92 lines. Discovery metadata
adds its own startup cost; these are corpus measurements, not measured token
counts or latency savings. No actual ADR, application, test project, package, or
CI configuration is changed; workflow-document edits only clarify the template reference.

## Rollback

Revert this single layer to restore the inline body templates and authoring
procedures with their original references. Verify the retained local policy,
metadata, and authority boundaries; do not revert earlier skill migrations.
