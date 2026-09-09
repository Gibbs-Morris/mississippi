# Scoped startup guidance

This integration replaces the blanket instruction-body preload with mandatory
global and task-applicable selection. It follows the four skill migrations in
native stack #619 and contributes to
[#532](https://github.com/Gibbs-Morris/mississippi/issues/532).
It is bootstrap policy, not another skill: the loading contract applies before
specialized work is selected.

Initial parent: [PR #622](https://github.com/Gibbs-Morris/mississippi/pull/622),
head `19fc6214aba699a40dc31e1d4a3c383884df57ad`, on main
`4aed4bcf9fab3200ec8c55b39e131576cf780bf8`. Before this layer started, the parent
passed 30 checks, completed Codex code/security reviews, received a positive
Copilot review with no findings, had no unresolved threads, and reported CLEAN.

## Preservation boundary

All 44 instruction bodies and all skill packages remain unchanged. The
entrypoints change only how relevant bodies are selected and add conservative
safeguards. Existing substantive entrypoint rules, Spring validation, build/test
commands, quality gates, and specialized full-inventory duties are preserved.

Selection always includes global scopes and considers reviewed/generated content,
languages, frameworks, and active workflows, not only edited filenames. Unknown
or missing metadata requires direct inspection. Scope expansion requires a new
selection pass. Relevant policy references, explicit skill routes, host-provided
instructions, and scoped AGENTS files remain authoritative.

No hard-coded filename allowlist, new resolver dependency, user configuration,
context-limit increase, or security/quality-gate relaxation is introduced.
The file inventory is separate from the scope search, so a missing scope line
does not silently remove a file. Only opening YAML frontmatter defines scope;
an example in a document body does not.

## Verification

- Both documented metadata-discovery methods enumerate all 44 current files and
  return their first scope line; opening frontmatter parses for every file.
- All 17 global files remain in every static selection case. Cases cover agent
  Markdown, core/sample C#, Razor, PowerShell, product docs, ADRs, C# snippets,
  role workflows, runtime questions, uncertainty, and global rule maintenance.
- Global rule maintenance retains all 44 files. Semantic additions, generated
  content, and uncertainty cases are explicit test inputs, not proof that a
  model automatically inferred them.
- A malformed-metadata fixture keeps all four candidate names discoverable;
  missing and invalid headers require inspection, and a body-level example is
  not accepted as metadata.
- Source comparisons verify unchanged instruction/skill bodies and preserved
  substantive entrypoint rules and validation sections. Markdown lint and
  relative-link checks pass.

These are static contract/metadata checks. Fresh host startup-behavior trials
remain unverified; the scenario table does not claim runtime token savings or
guarantee every model's instruction selection.

## Context accounting

At the assessed parent, the instruction bodies contain 29,398 whitespace-delimited
words. The two entrypoints add 1,408, giving 30,806 words under the previous
blanket preparation rule. The revised entrypoints contain 1,868 words and the
tested rg command pair adds 132. Supporting task references are additional.

| Static scenario | Selected instruction bodies | Total with entrypoints and metadata |
| --- | ---: | ---: |
| Agent Markdown | 11,592 | 13,592 |
| Core C# | 17,063 | 19,063 |
| Sample C# | 21,143 | 23,143 |
| PowerShell | 11,329 | 13,329 |
| Product documentation | 13,000 | 15,000 |
| ADR | 13,746 | 15,746 |
| C# snippet in Markdown | 16,310 | 18,310 |
| Global rule maintenance | 29,398 | 31,398 |

The savings depend on the task. Full-inventory work intentionally keeps the full
corpus and pays the added routing overhead. Unknown scopes may also require
broader reads. Counts describe the mandatory preparation text, not Codex's native
AGENTS concatenation limit or actual token/latency measurements.

## Research and rollback

[OpenAI's AGENTS guidance](https://learn.chatgpt.com/docs/agent-configuration/agents-md)
documents layered discovery.
[OpenAI's best practices](https://learn.chatgpt.com/guides/best-practices)
recommends concise entrypoints and task-specific referenced Markdown when
guidance grows. Checked September 9, 2026. The repository's selection contract
and quality requirements remain local policy decisions.

Revert this layer to restore the blanket preload procedure without changing any
retained policy or earlier skill migration. Verify the entrypoint references and
original preparation rule are restored together.
