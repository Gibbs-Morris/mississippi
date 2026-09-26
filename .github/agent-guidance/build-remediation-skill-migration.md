# Build-remediation skill migration

This instruction-only layer contributes to [#544](https://github.com/Gibbs-Morris/mississippi/issues/544)
and [#532](https://github.com/Gibbs-Morris/mississippi/issues/532).
The [September 26 resume plan](https://github.com/Gibbs-Morris/mississippi/issues/544#issuecomment-5846443812)
precedes the current correction. It replaces repeated build-repair procedure
text with [repair-build-failures](../../.agents/skills/repair-build-failures/SKILL.md),
while retaining mandatory repository behavior independently of skill selection.
Custom agents, applications, packages, and lockfiles are outside this layer.

## Source-to-target mapping

| Source content | Destination and reason |
| --- | --- |
| Six legacy remediation rules | Retained byte-for-byte in the instruction adapter: attempt cap, consistent deferred task, narrow edits, no unapproved suppressions/generated edits, CPM, formatting/build conventions. |
| Legacy Quick-Start and Procedure | Shared skill: inspect evidence, diagnose the causal failure, establish the contract, reproduce within authority, repair, reassess, verify. |
| Repeated legacy rationale | Removed; the skill expresses the corresponding decision constraints once. |
| Local commands and required gates | Existing repository policies/scripts remain authoritative; the generic skill discovers local bindings. |
| Skill unavailable or not automatically selected | Adapter retains mandatory rules and an explicit direct-file fallback. |
| Evaluation and progress records | On-demand audit files; none are startup instruction bodies. |

The skill is one portable instruction-only capability. It contains no repository
names, fixed paths, tool pre-approvals, scripts, or universal retry count. Its
trigger centers on an observed failing quality check and excludes unrelated
review, feature work, and runtime incidents. The shorter September 26 candidate
preserves all prior review corrections: validate supplied commands, respect
assessment-only side effects, establish accepted behavior before changing tests,
redact shared evidence, respect suppression authority, accept documented silent
success, and distinguish an unavailable original command from a faithful rerun.

## Discovery and loading evidence

Official documentation was refreshed September 26, 2026:

- [Codex skills](https://learn.chatgpt.com/docs/build-skills) describes repository `.agents/skills` discovery and metadata-first loading. [AGENTS discovery](https://learn.chatgpt.com/docs/agent-configuration/agents-md) describes layered filenames, precedence, and the default 32 KiB concatenation limit. This is distinct from files subsequently read by the agent.
- [Copilot skills](https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/add-skills) supports the shared repository directory and description-based selection. [Copilot surface support](https://docs.github.com/en/copilot/reference/custom-instructions-support) varies by environment; AGENTS support is not universal.
- The [Agent Skills specification](https://agentskills.io/specification) defines directory-matching name/description frontmatter and progressive resource loading. The shared package uses only required fields.

Observed on this host, not inferred from documentation:

| Check | Evidence and limit |
| --- | --- |
| Copilot CLI `skill list --json`, version 1.0.83-5 | Candidate is an enabled project skill in `.agents/skills`; seven repository skills are discovered at this layer. |
| Codex CLI `debug prompt-input`, version 0.142.5 | Startup includes candidate metadata and root AGENTS, excludes candidate body and the Copilot entrypoint body. The latter is read because repository policy requires it, not because native Codex automatically loads every Copilot instruction. |
| Configured Codex model | Saved GPT-6 Sol/max was rejected by this CLI account. Live `debug models` advertised GPT-5.5; trials used GPT-5.5/medium through invocation-only overrides. Saved settings were not changed. |
| Copilot behavior trial | Parser-compatible invocation reached GPT-5.4 and failed before model output with HTTP 402 additional usage limit. No fixture file changed; behavior is unverified. |
| Other Copilot surfaces | No app, VS Code, cloud-agent, or code-review activation smoke trial was executed. Documentation is a support claim, not runtime proof. |

No settings, trust limits, TLS verification, mandatory checks, or custom agents
were changed to obtain these results. An automatic approval rejection based on
assumed private repository content was reconciled with live public visibility
and inspected synthetic inputs before the native trial was approved.

## Current-candidate validation

Immutable skill content revision: `8c72aba1126468c06042ba5af806db6df3e83005`.
Committed UTF-8/LF SHA-256:
`eea91fb6176b24159a765001b3e746105131574df3249ba356b4d354b4e6a912`.
The trial inputs match those bytes after documented CRLF-to-LF normalization.

[Native trial records](build-remediation-native-trials.json) bind four fresh
Codex runs to that candidate. All repair results were independently rerun, and
all existing fixture file hashes were compared before/after:

| Trial | Observed result |
| --- | --- |
| Explicit skill request | Loaded the skill; repaired only `value.mjs`; canonical checker passed all three cases. |
| Implicit failure-repair request | Loaded the skill; repaired only `value.mjs`; canonical checker passed all three cases. |
| Unrelated rewrite with incidental build-repair words | Did not read the skill or use tools; all fixture files unchanged. |
| Assessment-only request with a side-effecting proposed command | Loaded the skill; inspected without editing or running that command; all fixture files unchanged and no artifact created. |

This is synthetic Node fixture evidence with one run per case. It does not prove
all 22 [rubric cases](build-remediation-skill-cases.json), arbitrary project
behavior, or Copilot conformance. In particular, redaction, stale-test repair,
suppression exceptions, and unobservable-operation handling have no fresh
native trial at this candidate. Earlier Desktop fixture trials are historical,
not current-candidate proof.

Skill-creator structural validation, configured Markdown lint, JSON parsing,
exact six-rule comparison, portability, and whitespace checks pass. Current
pipeline and GitHub gate state are maintained in the
[resumable progress record](skills-migration-progress.md), PR, and issue.
Mutation testing was not run; no mutation score is claimed.

## Context accounting

Every figure and before/after total in this section is a historical measurement,
using main `30b306c076bd198329ae5c2f6e6e17fa3a0801e7`,
previous PR head `50760d308aa13b2fc75643e26fe62469cfe3d667`, and skill candidate
`8c72aba1126468c06042ba5af806db6df3e83005`. They have not been recomputed
for the reviewed PR base `6002ab05a918c1e0c7391a7417db23a9f132d399`,
verified on 2026-09-26: 47 instructions, 20 global instructions and seven
repository skills.
The native trials above retain their separate candidate and input attribution.

Raw committed blobs from historical main `30b306c076bd198329ae5c2f6e6e17fa3a0801e7`
provide the historical baseline: 46 instructions, 19 global instructions, six repository
skills. The instruction corpus is 247,476 UTF-8 bytes / 31,310 words / 52,262
`o200k_base` tokens. Global bodies are 96,904 bytes / 12,845 words / 20,436 tokens.
The two entrypoints together are 18,364 bytes / 2,295 words / 3,906 tokens;
existing repository skill name/description values add 1,785 bytes / 235 words /
328 tokens. Counts are independent of worktree line endings.

| Component | UTF-8 bytes | Words | `o200k_base` tokens |
| --- | ---: | ---: | ---: |
| Adapter before | 2,571 | 342 | 592 |
| Adapter after | 1,948 | 244 | 435 |
| Previous PR skill body | 12,104 | 1,751 | 2,312 |
| Measured candidate skill body | 6,459 | 849 | 1,183 |
| Previous PR name/description | 389 | 56 | 74 |
| Measured candidate name/description | 295 | 40 | 58 |

The previous PR input is immutable head `50760d308aa13b2fc75643e26fe62469cfe3d667`.
Extraction parses YAML and counts name plus newline plus description; the body
starts after complete frontmatter. Measurements use Python 3.13, PyYAML 6.0.3,
tiktoken 0.14.0, UTF-8 blob decoding, `str.split()`, and `o200k_base.encode()`.

These representative static packages sum both entrypoints, all global bodies,
matching scopes below, and every repository skill's name/description. References,
discovery/search output, personal/plugin skills, framing, and tool results are
additional. Scope matching below is an explicit measurement input, not a claim
that a model inferred every relevant domain.

| Static task | Additional scopes | Before | After, body unloaded | After, repair body loaded |
| --- | --- | ---: | ---: | ---: |
| PowerShell | `**/*.ps*` | 25,103 | 25,004 | 26,187 |
| Core C# | `**/*.cs`, `src/**`, `**/*.{cs,razor,css}`, architecture multi-pattern scope | 35,705 | 35,606 | 36,789 |
| Product docs | `**/*.md`, `docs/Docusaurus/docs/**/*.{md,mdx}` | 27,882 | 27,783 | 28,966 |
| Instruction maintenance | All instruction bodies | 56,496 | 56,397 | 57,580 |

Net unloaded reduction is 99 proxy tokens, including metadata. The active body
is 1,129 proxy tokens smaller than the previous PR, but still adds cost relative
to the former short procedure; it is not an active-task saving against main.
These are reproducible text-cost estimates for the repository-imposed loading
contract shared by both tools, not host billing or native before/after savings.
Actual Codex multi-request trial usage is recorded separately in the JSON and
includes cached, personal/plugin, tool, and repeated-turn context; it is not
comparable to these static packages. Copilot runtime usage is unavailable.

## Rollback

Revert this complete layer to restore the legacy adapter procedure and remove
its skill and evaluation artifacts. Verify the six rules and local references
remain intact. Preserve unrelated migrations and custom agents. The progress
record is a checkpoint, not a replacement for live PR heads and gates.
