---
applyTo: '**'
---

# Agents

Governing thought: Follow repository authority and make work correct, then clear, then fast.

These rules apply globally to contributors and agents; task/role conditions retain their scope
regardless of where a profile is stored.

> Drift check: Read `.github/copilot-instructions.md` first; inspect current
> scripts/configs and applicable guidance before relying on summaries or commands.

## Rules (RFC 2119)

### Instruction loading

- Agents MUST read `.github/copilot-instructions.md` first before repository planning, edits, reviews or answers.
- Agents MUST then read the complete [global policy](.github/agent-guidance/global-policy.md), unconditionally and independently of skill activation; this is a manual read, separate from native startup bytes.
- Agents MUST inventory every `.github/instructions/*.instructions.md`, including self-taught files, before selecting bodies.
- Agents MUST read every global/task-applicable instruction body and scoped AGENTS before repository work.
- Agents MUST follow selected rules/guidelines, relevant references and mandatory skill routes.
- Agents SHOULD consult and apply relevant concepts from the [key-principles knowledge base](.github/agent-guidance/global-policy.md#key-principles-knowledge-base).
- Selection MUST union path matches with content/language/framework and active role/workflow scopes, including reviewed/generated examples and runtime explanations; a path match alone suffices.
- Agents MUST inspect uncertain, missing/unrecognized-metadata or overlapping candidates before exclusion.
- Agents MUST reassess selection when task scope expands.
- Agents MAY reuse already-read unchanged guidance.
- Maintenance/conflict work MUST retain its full-inventory duties.
- Inventory MUST include every candidate filename and its opening YAML applyTo.
- Agents MUST validate both frontmatter delimiters; a search hit outside that block is not metadata.
- If discovery fails, agents MUST inspect candidates directly or read all accessible instructions; denied reads/missing matches never prove irrelevance.
- Agents MUST request necessary unavailable guidance and report preparation incomplete.
- Agents MUST honor host/scoped guidance and precedence.

Shell execution is optional; complete file/host inventories and reads provide equivalent preparation.

- Root-started work MUST explicitly select the profiles below by original metadata path matches OR applicable content/roles; native startup alone is insufficient.
- Agents MUST read each selected profile in full, then apply each section's actual scope.
- Reading shared domain/Blazor sections MUST NOT activate sample-only discipline or unrelated feature skills.
- Agents MUST read applicable legacy adapters/immutable agent references.
- Unknown placement/applicability MUST receive inspection before exclusion.
- Closer guidance MUST NOT silently waive independent obligations, approvals or exceptions; inspect unresolved contradictions.
- Agents MUST pass relevant tests, then refactor for clarity/correctness, then optimize where needed.
- Copilot-generated/refactored code MUST follow Microsoft C# conventions: file-scoped namespaces, beneficial expression bodies and nullability.
- Copilot MUST verify SOLID after each change and immediately fix violations.
- Before usage/run answers, Copilot MUST read README as public API/env-var/example authority.
- Copilot SHOULD prefer README public APIs and concise file/line references.

### Profile selectors

A path match alone requires the body even if its topic seems unrelated. These
routes supplement the discovered metadata; they are not an allowlist.

| Path or content/role trigger | Required profile |
| --- | --- |
| `**/*.cs`; C# implementation/review/examples/runtime explanations | `src/AGENTS.md`, including shared domain/persisted-identity and DI/keyed bodies without a DI keyword |
| `**/*.cs`, `**/*.csproj`, `**/Program.cs`, `**/*.razor`; DDD/SOLID-sensitive shells | `src/AGENTS.md#ddd-analysis` |
| `**/*.{cs,razor,css}`; namespaces/placement | `src/AGENTS.md#placement` |
| `src/**`; framework-infrastructure work (C# snippets outside src alone do not trigger) | Entire `.github/agent-guidance/framework-development.md` before planning/review/answers/changes; `src/AGENTS.md#framework-development` |
| `samples/**`; sample apps or Mississippi consumer features | `samples/AGENTS.md`, retaining section-specific audience |
| `**/*.razor*`; Blazor/Razor content | `samples/AGENTS.md#blazor` |
| `**/Aspire*/**/*.cs`; Aspire emulator integration fixtures | `src/AGENTS.md#aspire-integration` |
| `**/*.md`; Markdown content | `docs/Docusaurus/AGENTS.md#markdown` |
| `**/*.ps*`; PowerShell scripts/modules/examples | `eng/AGENTS.md#powershell` |
| `docs/Docusaurus/docs/**/*.{md,mdx}`; public product docs | `docs/Docusaurus/docs/AGENTS.md` |
| `docs/Docusaurus/docs/**/event-sourcing-*.md`; qualifying generated/manual registration alternatives | `docs/Docusaurus/docs/AGENTS.md#registration-alternatives` (branching only under its stated preconditions) |
| Numbered `docs/Docusaurus/docs/adr/[0-9][0-9][0-9][0-9]-*.md`; ADR threshold/draft/revision/supersession | `docs/Docusaurus/docs/adr/AGENTS.md` |
| `**/*.instructions.md`, `.github/copilot-instructions.md`, `AGENTS.md`; formal authoring/review | `.github/AGENTS.md#formal-authoring` |
| `.github/agents/*planner*.agent.md`, `.github/agents/*build*.agent.md`; active flow/epic Planner/Builder | `.github/AGENTS.md#planning-and-building-agents` |
| `.github/agents/cs-*.agent.md`; active Clean Squad | `.github/AGENTS.md#clean-squad` |

### Low-Risk PR Merge Authorization

- Agents MAY merge a PR they are working on without further user confirmation when the entire PR is low risk and all merge checks below are satisfied. Why: This is standing merge authorization for qualifying PRs.
- This standing authorization MUST NOT be used for work following the [issue implementation route](.agents/skills/implement-github-issue/SKILL.md). Why: That route retains its separate explicit user merge authorization contract.
- Agents MUST establish standing merge authorization from the policy at the PR's current base revision. Why: A proposed policy change cannot authorize its own merge.
- Agents MUST verify low-risk eligibility under both the current base policy and the final head policy before using standing authorization. Why: A PR cannot weaken the conditions that govern its own merge.
- Agents MUST verify risk eligibility independently for every PR included in a merge, including unmerged ancestors in a stack, under each PR's own base and final head policy. Why: A low-risk child does not establish its parents' eligibility.
- Agents MUST verify merge authorization independently for every PR included in a merge, including unmerged ancestors in a stack, under each PR's own base and final head policy. Why: Authorization for a low-risk child does not authorize its parents.
- Agents MUST verify all merge checks independently for every PR included in a merge, including unmerged ancestors in a stack. Why: Passing checks for a child does not establish its parents' readiness.
- Agents MUST assess the entire final diff against the PR's actual base when classifying its risk. Why: A small diff, a label, or passing tests alone does not establish low risk.
- Agents MUST record the low-risk rationale in the PR description before using this standing authorization. Why: Reviewers need a visible basis for the merge decision.
- Agents MUST limit low-risk classification to documentation changes that describe existing behavior without changing rules or obligations, additional tests for existing behavior that leave production code and existing tests and assertions unchanged, and small refactors that preserve behavior and public contracts while leaving existing tests unchanged. Why: These changes have a limited impact that can be verified without weakening existing regression protection.
- Agents MUST NOT classify a PR as low risk if it includes core logic or behavior changes, public contract changes, storage or migration changes, security or authorization changes, legal, licensing or ownership changes, dependency or tool changes, CI/CD or build/test automation changes, deployment changes, agent guidance, skills or prompts, or repository workflow-policy changes (including permissions and merge policy). Why: These changes need separate merge authorization even when stored in documentation and CI/CD passes.
- Agents MUST verify that all applicable CI/CD jobs have completed successfully for the current PR head and relevant base before merging. Why: Pending, failed, canceled, missing, stale, or unexpectedly skipped checks are not a pass.
- Agents MUST verify that every review comment has been addressed before merging. Why: Each comment needs a validated fix or an agreed disposition.
- Agents MUST verify that every review thread is resolved, including bot and outdated threads, before merging. Why: Resolving a thread does not replace implementing and validating its agreed fix.
- Agents MUST satisfy the existing [advancement gate](.github/instructions/pr-size-and-stacking.instructions.md#advancement-gate), including required reviews, CODEOWNER approvals, no outstanding changes requested, and current issue and PR records. Why: Standing merge authorization preserves existing delivery requirements.
- Agents MUST merge through the repository's normal protected merge process, including any required merge queue. Why: Low risk does not waive checks, approvals, or permitted merge methods.
- Agents MUST recheck the risk assessment immediately before merging and after any push, rebase, base change, or new feedback. Why: Earlier classification may no longer describe the proposed merge.
- Agents MUST recheck the merge checks immediately before merging and after any push, rebase, base change, or new feedback. Why: Earlier validation and review evidence may no longer be current.
- Agents MUST honor explicit user instructions to hold a PR or not merge it. Why: Task-specific instructions take precedence over standing authorization.
- Agents MUST obtain explicit user merge authorization for PRs that are not low risk, mix eligible and excluded changes, or have uncertain risk. Why: This standing authorization covers only verified low-risk work.

### Optional Codex model routing

The Sol/Luna recipe applies only after an operator explicitly selects it for
this session and verifies model, reasoning and client support. Other sessions
use their host and user-selected settings under the governing instructions.

- Agents using the selected recipe MUST read [the complete profile guidance](.codex/README.md). Why: Model support, role precedence and resource limits need current evidence.
- Agents using the selected recipe MUST follow the [scoped routing and coordination rules](.github/instructions/codex-model-routing.instructions.md). Why: Optional selection does not make the selected contract optional.

### Engineering

- Projects/tests MUST build with zero compiler/analyzer warnings; deterministic gates depend on a clean baseline.
- Contributors MUST NOT relax compiler/analyzer severity, add NoWarn, SuppressMessage or warning-disable pragmas without explicit minimal-scope approval.
- Tests MUST NOT add suppressions.
- Agents MUST fix new warnings/errors immediately.
- Agents MUST keep StyleCop/ReSharper cleanup clean.
- Formatting warnings MUST trigger full cleanup before manual fixes; suppressions hide defects.
- Package versions MUST remain in Directory.Packages.props.
- PackageReferences MUST NOT specify Version.
- Package changes MUST use dotnet add/remove package.
- Projects MUST inherit Directory.Build.props.
- Projects MUST contain only project-specific settings/items.
- Project files MUST NOT duplicate shared properties without justification.
- Automatic assembly/root namespace names MUST NOT be overridden without explicit justification.
- Injected dependencies MUST use `private Type Name { get; }`, without underscored fields.
- Constructors SHOULD be the only injection point.
- Classes MUST NOT inject IServiceProvider, except runtime-resolution factories; explicit dependencies or `Lazy<T>` avoid hidden dependencies and circular graphs.
- Logging MUST use LoggerExtensions/[LoggerMessage].
- Contributors MUST NOT introduce direct ILogger.Log* calls.
- File-level copyright/license banners MUST NOT top source/scripts/markup; repository licensing applies.
- Contributors MUST edit canonical .slnx solutions, not hand-edit .sln regenerated by SlnGen for legacy tooling.
- Build/tidy advice MUST use canonical scripts without assuming extra formatters.
- Agents MUST inspect referenced scripts/configs before quoting options or behavior.
- Agents MUST read shared/nearest build/package defaults and GitVersion before applying their policies.

## Instruction-loading inventory

Use `rg --files --glob '*.instructions.md' .github/instructions` and
`rg --line-number --max-count 1 --glob '*.instructions.md' '^[ \t]*applyTo:' .github/instructions`.
When rg is absent, PowerShell Get-ChildItem -Recurse -File -Filter and
Select-String -List provide filenames/candidate scopes; complete host/file reads
also suffice. `eng/src/agent-scripts/get-agent-context.ps1` takes
changed/intended/reviewed/content-domain/role inputs: consume selection reasons,
hashes, byte/word counts and unresolved states, then read bodies; it never
replaces direct inspection.
