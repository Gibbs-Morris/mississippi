# Source-generated logging skill pilot

Issue: [#583](https://github.com/Gibbs-Morris/mississippi/issues/583). Programme: [#532](https://github.com/Gibbs-Morris/mississippi/issues/532).

Base: `a04c8dc99f27477c0d30e78f279ecd274cdb0392`. The user selected logging as the first pilot on September 5, 2026 (Europe/London), with one complete PR containing the skill and removal of its replaced procedure. This leaf does not complete the broader foundation or authorize another migration.

## Why this is a skill

The task is adding or converting selected C# logging calls to source-generated helpers. The reusable procedure covers event identity, typed template mapping, exception and dynamic-level handling, call-site argument evaluation, and captured-log verification. These details recur across the repository's LoggerExtensions implementations and its duplicated authoring example.

A checklist is sufficient for mandatory logging policy, so that policy stays in its existing instruction file. The skill has one bounded workflow, standard name/description metadata, no additional tool permissions, and no scripts, templates, new packages, or Mississippi-specific path dependencies. It uses the consuming repository's commands, accessibility, DI convention, and logging policy.

## Source-to-destination map

| Source | Disposition |
| --- | --- |
| Logging instruction frontmatter, scope, and eight Rules bullets | Retained; Rules text unchanged from the base |
| Logging quick-start and repeated Core Principles | Replaced by the skill procedure and retained rules |
| Lead Developer Logging code example | Replaced by links to the skill and mandatory policy |
| Lead Developer role, tools, Hard Rules, and workflow | Unchanged |
| Shared logging policy and other inbound instruction references | Unchanged; policy path still exists |

The removed quick-start's unconditional public helper example is not a new accessibility requirement: C# policy already defaults types to internal, and the Lead Developer example and repository helpers are internal. The skill reads local accessibility conventions instead of choosing a universal visibility.

## Research decisions

Shared-root placement and skill format were checked September 8, 2026 against
the GitHub, Codex, and Agent Skills sources linked below. This is dated
verification evidence, not a guarantee that host behavior remains unchanged.

- Use a specific procedure only when it adds value; avoid a skill per policy file. [Agent Skills authoring guidance](https://agentskills.io/skill-creation/best-practices)
- Keep mandatory standards in custom instructions; load detailed task procedure on demand. [GitHub skill guidance](https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/add-skills)
- Use the single shared `.agents/skills` root, supported by [Copilot](https://docs.github.com/en/copilot/concepts/agents/about-agent-skills) and [Codex](https://learn.chatgpt.com/docs/build-skills).
- Use standard name/description metadata, with optional resources only when needed. [Agent Skills specification](https://agentskills.io/specification)
- Check parameter-name binding, exception parameters, dynamic levels, and compiler diagnostics. [Microsoft source-generation guidance](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/source-generation)
- A generated enabled check cannot prevent expensive argument evaluation at the caller. [Microsoft library logging guidance](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/library-guidance)

## Evaluation protocol

[Evaluation cases](logging-skill-cases.json) hold the exact task inputs. Run each case in five fresh sessions on each primary host; do not resume earlier conversations. Return proposed code or next actions, allowing read-only guidance inspection but no edits, builds, network tools, or publication by the evaluating agent. Run compilation separately against inspected output.

Prepare separate temporary Git repositories:

- `legacy`: the base logging instruction as guidance, with no candidate skill.
- `candidate`: the retained eight policy rules and only this repository skill; exclude the removed quick-start and agent example.
- `portable`: copy the skill unchanged, with this independent policy: Cedar is a .NET 10 library with source under `lib/`, private readonly logger fields, internal static partial helpers named `Telemetry`, and instrumentation only for requested operations. Build with `dotnet build Cedar.csproj -warnaserror`; do not change providers or packages.
- `instance`: copy the skill unchanged into a .NET 10 fixture using instance LoggerMessage methods on partial Worker classes with private readonly `ILogger<Worker>` fields. Retain that pattern, introduce no static helper classes, and preserve event identity and structured-field enumeration. Build/test tools are unavailable in this advisory task.

Keep the host's installed skill catalogue visible for collision testing. Copilot evaluations request `view`, `glob`, `grep`, and `skill`, deny write/shell tools, disable built-in MCP servers and auto-update, and run without asking the user. The installed CLI warns that `grep` is unknown; the effective available inspection tools are `view`, `glob`, and `skill`, with `rg` unavailable. Codex evaluations use ephemeral read-only sessions. Neither route grants additional file or publication permissions to the skill.

Assess outcomes separately from load traces:

- Explicit/direct/paraphrased conversion preserves level, event ID/name, template, typed property values and enumeration order, exception, and caller category. Output is compilable when wrapped with required imports and caller context. Where the prescribed signature conflicts with original field order, an accurate conflict report and scoped alternative is also a successful outcome; silently claiming equivalence is not.
- Unrelated/incidental cases answer the actual question without invoking this skill.
- Missing scope requests a component or operation without inferring a target or editing/converting files. Record unnecessary discovery separately from that scope gate.
- Missing tools reports unverified compilation and flags generated default event identity instead of claiming equivalence to `EventId(0, null)`.
- Portable output follows Cedar's readonly-field/Telemetry convention and guards expensive logging-only preparation at the dynamic level without changing business work.
- Instance output retains the repository's instance pattern and either preserves the ordered event contract or identifies the fixed-signature conflict accurately.
- Any success statement accurately distinguishes proposed code from executed checks. Investigate failures; do not average away lost requirements.

## Verification record

Evaluation date: September 5, 2026 (Europe/London). Accepted skill SHA-256: `839A52D655E8C3B576C31D3CD32A041B82995842F776327ACEE05E58445A6503`.

Hosts: Copilot CLI 1.0.80 using its configured default, reported as `gpt-5.4` in captured `session.tools_updated` events; Codex CLI 0.153.0 with a per-run `gpt-5.5` / `high` override and user config excluded. The installed Codex CLI could not run the configured `gpt-6-astra`; no global configuration was changed. Results are limited to these host configurations.

| Accepted advisory scenario | Copilot | Codex |
| --- | --- | --- |
| Explicit conversion, including conflicting signature | 5/5 | 5/5 |
| Direct implicit conversion | 5/5 | 5/5 |
| Paraphrased conversion | 5/5 | 5/5 |
| Unrelated question, no logging-skill activation | 5/5 | 5/5 |
| Incidental keyword, no logging-skill activation | 5/5 | 5/5 |
| Missing scope, request target without edits | 5/5 | 5/5 |
| Missing tools and null event-name uncertainty | 5/5 | 5/5 |
| Cedar conventions and expensive dynamic-level argument | 5/5 | 5/5 |
| Instance pattern and exact message literal | 5/5 | 5/5 |

Independent reviewers inspected the actual responses and relevant tool traces, not just successful process exits. The 70 accepted positive/incomplete-input cases record the skill hash above. The 20 negative cases are reused from the initial batch: discovery metadata was unchanged, the skill was not loaded, and neither correction affects their answers. Ten legacy-only comparison trials also completed; these simple supplied-code comparisons do not establish superiority over baseline guidance.

One of five revised Copilot missing-scope trials unnecessarily ran recursive filename searches before asking for scope. It did not infer a target, edit, or convert anything. The initial no-scanning rubric overstated the skill's actual before-editing scope gate; retain the observed 4/5 avoidance of recursive discovery as an efficiency limitation rather than claiming flawless routing.

Two findings changed the skill before acceptance: its initial static-only prescription became repository-governed static/instance support, and a compiled output that changed field order led to explicit ordered-state verification. Both revised explicit outputs then preserved full captured-record equivalence. Separately, the entire earlier instance batch was superseded because its unquoted template made a final period ambiguous; both hosts were rerun five times with a quoted exact literal. No ambiguous trial enters the table.

Structural checks passed: the bundled skill validator with PyYAML 6.0.3; direct markdownlint 0.41.1 checks of all four changed Markdown files with the repository configuration; JSON parsing; relative-reference resolution; byte-equivalent normalized Rules-section comparison (eight bullets); Git whitespace checks; and reverse-apply validation of the complete patch. Copilot listed the enabled project skill in the actual worktree; a Codex read-only worktree smoke test read the skill and verified both final consumer routes and retained policy.

The separately executed package-free .NET 10 capture fixture is a one-time local experiment, not a test project committed by this PR. Its 16 checks cover named fixed/dynamic events, enabled/disabled filtering, typed structured state, exceptions, and guarded expensive argument evaluation. SDK 10.0.111 and the installed framework supplied the logging generator. It does not establish equivalence for all conversions, null event names, scopes, or application control flow.

Actual inspected host output was also compiled: revised explicit output from each host passed two enabled/disabled captured-record checks, original Codex implicit output passed two, and original portable output from each host passed 24 dynamic-level/call-site checks. Builds used Release and `-warnaserror`, with zero warnings/errors. Helper declarations and replacement call sites were not silently fixed; required imports, caller context, and separately documented counted probes were supplied around advisory snippets. Not every full response in the table was compiled.

For these local fixtures, verification used `dotnet restore <fixture.csproj> --configfile <isolated NuGet.Config>`, `dotnet build <fixture.csproj> -c Release --no-restore -warnaserror`, and `dotnet run --project <fixture.csproj> -c Release --no-build --no-restore`. These are records of a local experiment, not runnable commands against a fresh repository checkout. The committed cases and rubric support future re-evaluation; model responses remain nondeterministic.

The two existing guidance files remove 21 lines and add two routing lines (476 fewer normalized characters). The new skill adds approximately 750 whitespace-delimited words including discovery metadata. This is a modest baseline-guidance reduction and substantial task-specific verification guidance, not a claim of overall token savings. No application code, runtime contract, project dependencies, global instruction loading, or other skills changed.

## Review and rollback

Two independent reviewers inspected the diff: one for preservation and consumer coverage, one for skill necessity and portability. They verified retained policy, the corrected skill, and the bounded advisory outcomes; neither role grants universal equivalence or broader migration approval. Relevant content changes require rechecking their findings.

Revert this single skill PR to restore the previous quick-start and agent example and remove the skill/routes together. Validate that both original guidance files and their references are restored; do not revert unrelated migrations.

## September 8 continuation

The migration resumed on September 8, 2026, under the user's instruction to
deliver one complete skill per PR using stacks. PR #585 remains the first layer;
later dependent skills wait for its current CI and review advancement gate.
The September 5 evaluation record above is historical evidence, not a new run.

The first review correction aligns the skill builder's write scope, discovery,
examples, and metadata with `.agents/skills`. The Rules Manager placement guide
and agent-extensibility reference use the same root. Other supported Copilot
roots remain visible for overlap checks, with no duplicate skill introduced.
The authoring agent retains its skills-only role and restrictions on writes
outside that root. Its portable template no longer specifies this product as
the skill owner.

The shared root and progressive loading were rechecked against the official
Codex, Copilot, and Agent Skills pages linked above. New skills discover local
project conventions rather than prescribing this repository's names or paths.
The current logging skill and all eight retained policy rules remain unchanged
by this authoring-root correction.

The second review correction explicitly matches the call-site enabled check to
the attribute's fixed level or to the dynamic argument passed to the generated
method, using the same logger. The new `portable-fixed-level` case also keeps
required business work outside the logging guard. The discovery description is
unchanged; the September 5 advisory trials were not rerun for this wording fix.

Current targeted verification:

- Codex CLI 0.153.4 `skills/list` returned this enabled repository skill once,
  at `.agents/skills`, with no discovery errors. No model turn was started.
- Copilot CLI 1.0.83-5 `skill list --json` returned the enabled project skill
  from the same directory. Discovery is distinct from behavioral evaluation.
- A separate package-free .NET 10 fixture built in Release with SDK 10.0.400,
  `-warnaserror`, zero warnings/errors, and passed 28 checks covering fixed and
  dynamic levels, enabled/disabled preparation, required business work, event
  identity, template, formatted output, ordered typed fields, and exception.
  This is a focused local experiment for the corrected guard guidance, not a
  new application test or a claim that the advisory case was run by both hosts.
- The skill validator, JSON parsing, unchanged eight-rule comparison, consumer
  link resolution, authoring metadata, and portability checks passed again.
  Configured Markdown lint passed. Direct lint of the excluded skill-builder
  agent has eight baseline findings and no new findings; the other changed
  agents have none. No lint settings were changed.

The runtime fixture initially produced analyzer findings; the corrected fixture
computes detail into a local variable inside each guard, allocates its expected
field-name array once, and uses a specific assertion exception. No analyzer was
disabled. The historical skill hash above identifies only the September 5
evaluation; the revised skill has changed.
