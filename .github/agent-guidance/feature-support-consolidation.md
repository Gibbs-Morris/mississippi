# Feature support guidance consolidation

This focused continuation contributes to [#795](https://github.com/Gibbs-Morris/mississippi/issues/795)
and [#532](https://github.com/Gibbs-Morris/mississippi/issues/532). Its
[six-field scope extension](https://github.com/Gibbs-Morris/mississippi/issues/795#issuecomment-5849010950)
was verified before implementation. The historical preparation baseline for the source map and context counts is
`23cf0ba2e89d56f76d4fd358a5260b639099ed4f`.
The [existing feature skill](../../.agents/skills/implement-event-sourced-feature/SKILL.md)
and every other existing skill remain unchanged. General C# work gains no feature-skill route.

## Source map

Line ranges refer to the baseline Git blobs. Each removed bullet or table row
has its exact source line and preserved target in the [audit](feature-support-consolidation-audit.json).
`R1`, `R2`, and similar identifiers mean the original file's ordered Rules bullets;
named cross-file targets identify their own retained Rules or binding example.
Removed headings and whitespace belong to the corresponding supporting block.

| Source and original lines | Supporting content and preservation |
| --- | --- |
| [C#](../instructions/csharp.instructions.md), 35-50 | Six quick-start and four principle summaries remain covered by Rules 1-17. The pre-1.0 qualifier remains available through the added compatibility reference. |
| [Naming](../instructions/naming.instructions.md), 25-49 | Four quick-start and three principle summaries are covered by Rules 2-5. All seven suffix rows are copied exactly into the [binding examples](event-sourced-feature-bindings.md#domain-type-suffix-examples); the original suffix heading remains as a narrow entrypoint. |
| [Serialization](../instructions/orleans-serialization.instructions.md), 25-35 | Three quick-start and two principle summaries are covered by Rules 1-6. The attribute shape remains in the [binding](event-sourced-feature-bindings.md#serialization-and-storage-examples). ID stability and additive changes remain conditional on the retained pre/post-1.0 Rules. |
| [Storage names](../instructions/storage-type-naming.instructions.md), 23-33 | Three quick-start and two principle summaries are covered by Rules 1-4. The explicit-version attribute example remains in the binding. Real-store identity and version evolution stay distinct from CLR naming. |
| [Compatibility](../instructions/backwards-compatibility.instructions.md), 24-37 | Four quick-start and four principle summaries are covered by all six Rules. Pre-release API freedom does not remove the separate real-store naming invariant or complete consumer updates. |
| [Architecture](../instructions/dotnet-architecture-good-practices.instructions.md), 25-37 | Four quick-start and three principle summaries remain covered by Rules 1-6 and the unchanged C#, registration, testing, and logging references. Existing verification policy owns the canonical gates. |
| [Blazor](../instructions/blazor-ux-guidelines.instructions.md), 28-40 | Four quick-start and three principle summaries remain covered by Rules 2-10, including presentation, callbacks, selectors, keyboard/ARIA, tests, and WebAssembly boundaries. |
| [Abstractions](../instructions/abstractions-projects.instructions.md), 24-35 | Three quick-start and three principle summaries are covered by Rules 1-3; contract triggers, optional triggers, exceptions, and dependency direction remain unchanged. |

The eight complete Rules sections (64 bullets), prefixes/frontmatter, scopes,
audience sections, and original references are preserved exactly. The naming
suffix heading remains reachable; no incoming link to a removed summary anchor
was found. Existing AGENTS, custom agents, application, scripts, workflows,
packages, and build inputs are unchanged. No policy exception is inferred from
illustrative names, sample visibility, generated output, or the portable skill.
Mississippi sample generation rules stay local; another consumer's permitted
manual path is governed by its own policy.

## Accounting and validation

Committed LF instruction blobs total 356 lines before and 248 afterward: 108
fewer lines and 5,022 fewer bytes. Useful examples add 27 binding lines outside
startup instruction bodies. Evidence records add review content separately.
The audit includes raw hashes, unchanged metadata, explicit o200k_base context
profiles, configured lint, schema/shape checks, and link/anchor results. These
are source and context measurements, not measured activation, billing, latency,
or behavioral improvements. No content is moved into AGENTS or a new skill.

The [four fresh cases](feature-support-consolidation-cases.json) are definitions,
not passing trials. Two supplied-catalog read-only assessments now have fresh inherited-model output
and independent protected-file/read-trace inspection, as recorded below. Historical
feature-skill trials do not establish behavior for the changed instruction and
binding inputs; CRLF fixture hashes are not presented as raw LF hashes.
Cross-model/native discovery and Copilot remain unverified; Copilot stays deferred.
No application feature, canonical cleanup/build/unit test, browser journey,
Docusaurus build, or mutation result is claimed by this preparation. Root owns
current-head/base CI, applicable completion gates, and complete review disposition.

## Rollback and handoff

Revert this complete layer to restore the eight supporting blocks and remove
the binding examples and three focused records together. Preserve the existing
feature skill and prior migration layers. Root owns serial native-stack draft
integration and tracking updates; this preparation makes no remote publication,
stack mutation, review resolution, or merge.

## Fresh bounded assessments

Two inherited-chat-model supplied-catalog assessments excluded the feature skill
for general C# and preserved `ACME.BILLING.RECORDED.V1` during a proposed pre-release
API/CLR rename. Root independently inspected both outputs and all nine protected
input hashes. These are read-only recommendations, not runtime or full native
discovery/model-matrix passes; the audit records their exact evidence hashes.
