# Scoped startup guidance

This integration replaces the blanket instruction-body preload with mandatory
global and task-applicable selection. It follows the four skill migrations in
native stack #619 and contributes to
[#532](https://github.com/Gibbs-Morris/mississippi/issues/532).
It is bootstrap policy, not another skill: the loading contract applies before
specialized work is selected.

Preimplementation snapshot: [PR #622](https://github.com/Gibbs-Morris/mississippi/pull/622),
head `19fc6214aba699a40dc31e1d4a3c383884df57ad`, on main
`4aed4bcf9fab3200ec8c55b39e131576cf780bf8`. Before this layer started, the parent
passed 30 checks, completed Codex code/security reviews, received a positive
Copilot review with no findings, had no unresolved threads, and reported CLEAN.

These revisions record the gate before implementation began. The stack was
subsequently rebased on September 9 onto main
`f5bb7c0c2bd843deb27483e001a3350e106e83cd` after PR #621 merged. That later rebase
does not replace the historical gate evidence above. GitHub's PR refs and checks
identify the revisions to use for each new advancement audit.

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

- The loading trigger explicitly covers planning, edits, reviews, and repository
  questions, so advisory tasks also require instruction selection.
- Hosts without shell access can use file tools or complete supplied context;
  fallback full reads retain uncertain guidance, and inaccessible files remain
  explicit preparation gaps.
- Both documented metadata-discovery methods enumerate all 44 current files and
  return their first scope line; opening frontmatter parses for every file.
- All 17 global files remain in every static selection case. Cases cover agent
  Markdown, core/sample C#, Razor, PowerShell, product docs, ADRs, C# snippets,
  role workflows, runtime questions, uncertainty, and global rule maintenance.
- Global rule maintenance retains all 44 files. Semantic additions, generated
  content, and uncertainty cases are explicit test inputs, not proof that a
  model automatically inferred them.
- A six-file metadata fixture keeps all candidate names discoverable. Both
  discovery methods find space- and tab-indented scope candidates; valid
  space-indented frontmatter parses, while invalid tab indentation, missing or
  malformed headers, and body-level examples require direct inspection.
- Source comparisons verify unchanged instruction/skill bodies and preserved
  substantive entrypoint rules and validation sections. Markdown lint and
  relative-link checks pass.

These are static contract/metadata checks. Fresh host startup-behavior trials
remain unverified; the scenario table does not claim runtime token savings or
guarantee every model's instruction selection.

## Context accounting

At parent snapshot `32703e87af5c11e37ffcd64236b99cec87380377`, the instruction bodies contain
29,477 whitespace-delimited words. The two original entrypoints add 1,408,
giving 30,885 words under the previous blanket preparation rule.
The revised entrypoints contain 1,987 words and the tested rg command pair
adds 132. Supporting task references are additional.

| Static scenario | Selected instruction bodies | Total with entrypoints and metadata |
| --- | ---: | ---: |
| Agent Markdown | 11,659 | 13,778 |
| Core C# | 17,131 | 19,250 |
| Sample C# | 21,211 | 23,330 |
| PowerShell | 11,396 | 13,515 |
| Product documentation | 13,067 | 15,186 |
| ADR | 13,824 | 15,943 |
| C# snippet in Markdown | 16,378 | 18,497 |
| Global rule maintenance | 29,477 | 31,596 |

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
