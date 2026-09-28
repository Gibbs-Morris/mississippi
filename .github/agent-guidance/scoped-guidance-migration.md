# Scoped guidance migration

This is the separate final consolidation layer of native stack #680, following
[#807](https://github.com/Gibbs-Morris/mississippi/pull/807) and contributing to
[#561](https://github.com/Gibbs-Morris/mississippi/issues/561) and
[#532](https://github.com/Gibbs-Morris/mississippi/issues/532).

## Loading and retained authority

Root guidance requires Copilot-first reading, the complete unconditional global
policy, dynamic instruction inventory, all global adapters and conservatively
selected profiles. Selection includes paths, content and workflow roles. Root
started work explicitly reads relevant nested profiles; native ancestor loading
alone does not select guidance in other directories. Skill activation does not
make mandatory policy optional.

The 47 instruction files remain compatibility adapters because existing custom
agents reference them. All 85 custom-agent files are unchanged. The original
frontmatter scopes, independent mandatory duties, exceptions, precedence and
read/follow obligations are retained. Clean Squad still reads its complete
unchanged master; relocation does not make that context optional or resolve
pre-existing disagreements by silently choosing a weaker rule.

| Policy destination | Source responsibilities |
| --- | --- |
| [Root](../../AGENTS.md) and [global policy](global-policy.md) | Conservative loading; global engineering, verification, issue, review, stack and learning duties. |
| [Framework profile](../../src/AGENTS.md) | C#, projects, architecture, names/placement, Orleans, logging, domain/serialization/storage, DI/keyed services and Aspire contracts, with original section scopes. |
| [Framework reference](framework-development.md) | The complete infrastructure-specific framework-development policy; stronger root duties still apply. |
| [Sample profile](../../samples/AGENTS.md) | Sample framework/discipline and Blazor contracts; section audiences remain distinct. |
| [GitHub profile](../AGENTS.md) | Formal instruction authoring, Planner/Builder roles and Clean Squad process; explicit authoritative-source/master routes. |
| [PowerShell profile](../../eng/AGENTS.md) | PowerShell scripts, modules and examples. |
| [Markdown profile](../../docs/Docusaurus/AGENTS.md) | Markdown anywhere, selected by its original content/path scope. |
| [Product docs profile](../../docs/Docusaurus/docs/AGENTS.md) | Documentation type, focus, feature structure, registration alternatives and completion contracts. |
| [ADR profile](../../docs/Docusaurus/docs/adr/AGENTS.md) | ADR threshold, MADR shape, lifecycle and accepted-history rules. |

The adapter-to-destination table below makes each retained entrypoint reviewable.
Compare each adapter with the immediately preceding layer when checking the
source duties; rule identifiers in destination profiles preserve that mapping.

| Retained adapter | Direct policy destinations |
| --- | --- |
| [abstractions-projects.instructions.md](../instructions/abstractions-projects.instructions.md) | [the complete global policy](global-policy.md); [root instruction loading](../../AGENTS.md#instruction-loading) |
| [adr.instructions.md](../instructions/adr.instructions.md) | [ADR lifecycle](../../docs/Docusaurus/docs/adr/AGENTS.md#adr-lifecycle) |
| [agent-efficiency.instructions.md](../instructions/agent-efficiency.instructions.md) | [the complete global policy](global-policy.md); [root instruction loading](../../AGENTS.md#instruction-loading) |
| [agent-planning-methodology.instructions.md](../instructions/agent-planning-methodology.instructions.md) | [planner and builder contracts](../AGENTS.md#planning-and-building-agents) |
| [agent-scratchpad.instructions.md](../instructions/agent-scratchpad.instructions.md) | [the complete global policy](global-policy.md); [root instruction loading](../../AGENTS.md#instruction-loading) |
| [aspire.instructions.md](../instructions/aspire.instructions.md) | [Aspire integration contracts](../../src/AGENTS.md#aspire-integration) |
| [authoring.instructions.md](../instructions/authoring.instructions.md) | [formal-authoring contracts](../AGENTS.md#formal-authoring) |
| [backwards-compatibility.instructions.md](../instructions/backwards-compatibility.instructions.md) | [the complete global policy](global-policy.md); [root instruction loading](../../AGENTS.md#instruction-loading) |
| [benchmarks.instructions.md](../instructions/benchmarks.instructions.md) | [the complete global policy](global-policy.md); [root instruction loading](../../AGENTS.md#instruction-loading) |
| [blazor-ux-guidelines.instructions.md](../instructions/blazor-ux-guidelines.instructions.md) | [Blazor contracts](../../samples/AGENTS.md#blazor) |
| [build-issue-remediation.instructions.md](../instructions/build-issue-remediation.instructions.md) | [the complete global policy](global-policy.md); [root instruction loading](../../AGENTS.md#instruction-loading) |
| [build-rules.instructions.md](../instructions/build-rules.instructions.md) | [the complete global policy](global-policy.md); [root instruction loading](../../AGENTS.md#instruction-loading) |
| [clean-squad.instructions.md](../instructions/clean-squad.instructions.md) | [Clean Squad contracts](../AGENTS.md#clean-squad) |
| [coding-discipline.instructions.md](../instructions/coding-discipline.instructions.md) | [sample discipline](../../samples/AGENTS.md#sample-discipline); [event-sourced domain contracts](../../src/AGENTS.md#event-sourced-domain) |
| [csharp.instructions.md](../instructions/csharp.instructions.md) | [C# contracts](../../src/AGENTS.md#c); [root engineering rules](../../AGENTS.md#engineering); [Orleans contracts](../../src/AGENTS.md#orleans) |
| [documentation-authoring.instructions.md](../instructions/documentation-authoring.instructions.md) | [public-documentation contracts](../../docs/Docusaurus/docs/AGENTS.md#public-documentation); [page-contract table](../../docs/Docusaurus/docs/AGENTS.md#selected-page-contracts) |
| [documentation-page-focus.instructions.md](../instructions/documentation-page-focus.instructions.md) | [public-documentation contracts](../../docs/Docusaurus/docs/AGENTS.md#public-documentation); [page-contract table](../../docs/Docusaurus/docs/AGENTS.md#selected-page-contracts) |
| [domain-modeling.instructions.md](../instructions/domain-modeling.instructions.md) | [event-sourced domain contracts](../../src/AGENTS.md#event-sourced-domain); [the domain registration contract](../../src/AGENTS.md#event-sourced-domain) |
| [dotnet-architecture-good-practices.instructions.md](../instructions/dotnet-architecture-good-practices.instructions.md) | [DDD analysis contracts](../../src/AGENTS.md#ddd-analysis) |
| [feature-docs-pattern.instructions.md](../instructions/feature-docs-pattern.instructions.md) | [registration alternatives](../../docs/Docusaurus/docs/AGENTS.md#registration-alternatives); [page-contract table](../../docs/Docusaurus/docs/AGENTS.md#selected-page-contracts) |
| [feature-documentation-structure.instructions.md](../instructions/feature-documentation-structure.instructions.md) | [public-documentation contracts](../../docs/Docusaurus/docs/AGENTS.md#public-documentation); [page-contract table](../../docs/Docusaurus/docs/AGENTS.md#selected-page-contracts) |
| [framework-patterns.instructions.md](../instructions/framework-patterns.instructions.md) | [complete framework-development policy](framework-development.md#framework-development) |
| [issue-tracking.instructions.md](../instructions/issue-tracking.instructions.md) | [the complete global policy](global-policy.md); [root instruction loading](../../AGENTS.md#instruction-loading) |
| [keyed-services.instructions.md](../instructions/keyed-services.instructions.md) | [registration and keyed-service contracts](../../src/AGENTS.md#registration-and-keyed-services) |
| [logging-rules.instructions.md](../instructions/logging-rules.instructions.md) | [logging contracts](../../src/AGENTS.md#logging); [root engineering rules](../../AGENTS.md#engineering) |
| [markdown.instructions.md](../instructions/markdown.instructions.md) | [Markdown contracts](../../docs/Docusaurus/AGENTS.md#markdown) |
| [mississippi-framework.instructions.md](../instructions/mississippi-framework.instructions.md) | [framework usage](../../samples/AGENTS.md#framework-usage); [event-sourced domain contracts](../../src/AGENTS.md#event-sourced-domain) |
| [mutation-testing.instructions.md](../instructions/mutation-testing.instructions.md) | [the complete global policy](global-policy.md); [root instruction loading](../../AGENTS.md#instruction-loading) |
| [namespace-folder-placement.instructions.md](../instructions/namespace-folder-placement.instructions.md) | [placement contracts](../../src/AGENTS.md#placement) |
| [naming.instructions.md](../instructions/naming.instructions.md) | [naming contracts](../../src/AGENTS.md#naming) |
| [orleans-serialization.instructions.md](../instructions/orleans-serialization.instructions.md) | [Orleans contracts](../../src/AGENTS.md#orleans) |
| [orleans.instructions.md](../instructions/orleans.instructions.md) | [Orleans contracts](../../src/AGENTS.md#orleans) |
| [plain-english.instructions.md](../instructions/plain-english.instructions.md) | [the complete global policy](global-policy.md); [root instruction loading](../../AGENTS.md#instruction-loading) |
| [powershell.instructions.md](../instructions/powershell.instructions.md) | [PowerShell contracts](../../eng/AGENTS.md#powershell) |
| [pr-description.instructions.md](../instructions/pr-description.instructions.md) | [the complete global policy](global-policy.md); [root instruction loading](../../AGENTS.md#instruction-loading) |
| [pr-review-polling.instructions.md](../instructions/pr-review-polling.instructions.md) | [the complete global policy](global-policy.md); [root instruction loading](../../AGENTS.md#instruction-loading) |
| [pr-size-and-stacking.instructions.md](../instructions/pr-size-and-stacking.instructions.md) | [the complete global policy](global-policy.md); [root instruction loading](../../AGENTS.md#instruction-loading); [the complete size and planning policy](global-policy.md#traceability-and-delivery); [the complete advancement gate](global-policy.md#traceability-and-delivery); [the mandatory stack lifecycle policy](global-policy.md#traceability-and-delivery) |
| [projects.instructions.md](../instructions/projects.instructions.md) | [the complete global policy](global-policy.md); [root instruction loading](../../AGENTS.md#instruction-loading) |
| [pull-request-reviews.instructions.md](../instructions/pull-request-reviews.instructions.md) | [the complete global policy](global-policy.md); [root instruction loading](../../AGENTS.md#instruction-loading) |
| [rfc2119.instructions.md](../instructions/rfc2119.instructions.md) | [formal-authoring contracts](../AGENTS.md#formal-authoring) |
| [self-improvement.instructions.md](../instructions/self-improvement.instructions.md) | [the complete global policy](global-policy.md); [root instruction loading](../../AGENTS.md#instruction-loading); [the lesson-admission and conflict policy](global-policy.md#learning-efficiency-and-communication) |
| [service-registration.instructions.md](../instructions/service-registration.instructions.md) | [registration and keyed-service contracts](../../src/AGENTS.md#registration-and-keyed-services); [root engineering rules](../../AGENTS.md#engineering) |
| [shared-policies.instructions.md](../instructions/shared-policies.instructions.md) | [the complete global policy](global-policy.md); [root instruction loading](../../AGENTS.md#instruction-loading) |
| [storage-type-naming.instructions.md](../instructions/storage-type-naming.instructions.md) | [persisted-identity contracts](../../src/AGENTS.md#persisted-identity) |
| [test-improvement.instructions.md](../instructions/test-improvement.instructions.md) | [the complete global policy](global-policy.md); [root instruction loading](../../AGENTS.md#instruction-loading) |
| [testing.instructions.md](../instructions/testing.instructions.md) | [the complete global policy](global-policy.md); [root instruction loading](../../AGENTS.md#instruction-loading) |
| [ux-validation.instructions.md](../instructions/ux-validation.instructions.md) | [the complete global policy](global-policy.md); [root instruction loading](../../AGENTS.md#instruction-loading) |

## September 30 main preservation

The September 30 rebase onto main `92a7a2b9f3c7eff10ef1eb971210f313f3f71693`
retains the upstream domain visibility and command-input changes. The scoped
framework profile preserves internal-by-default records, public API/discovery
exceptions, generator/registration verification, optional command defaults and
runtime validation. Events retain their separate internal/required contract.
The domain adapter retains the incoming visibility anchor, and sample guidance
continues to require the complete domain contract.

At the September 30 snapshot, normalized LF counts were 3,171 instruction-directory lines on main,
2,452 before this final layer and 911 afterward. The full policy corpus is
3,395, 2,644 and 2,178 lines respectively. The final directory decrease of 1,541
includes relocation; the final net policy decrease is 466. Across the stack,
the corresponding decreases are 2,260 directory lines and 1,217 policy lines.

## September 30 execution evidence

The rebased source completed both solutions' canonical build, full cleanup,
L0/L1 test, core coverage-summary and final warning-free build stages:
3,442 tests executed and passed across all 45 selected module TRX files.
Forty-two contain tests; the three named SDK facades remain empty exemptions
and supply no executed coverage. Core line coverage is 80.47% and
branch coverage is 68.81%. Tracked inputs remained unchanged.

The initial `go.ps1` attempt failed on a zero-filled generated Debug reference
assembly; the canonical Debug build repaired it without source edits. The next
whole `go.ps1` process was interrupted during sample cleanup. Completed stages
were preserved and the remaining canonical wrappers ran on identical tracked
inputs. A sample MSBuild worker crash before Crescent execution cleared on one
process-local no-reuse retry. Failed/interrupted logs remain separate from
successful stage receipts. No single completed `go.ps1` invocation is claimed
for this rebased tree.

All 11 canonical PowerShell runners passed: 503 Pester cases
passed and five Unix/case-sensitive-path cases were inapplicable on Windows.
Docs install/typecheck/build/tests passed.
The first browser run stopped before tests because port 3000 was occupied;
the unchanged command passed after that port became free, without stopping or
reusing another process. The install reported 59 dependency advisories:
3 low, 36 moderate, 18 high and 2 critical. These are npm summary counts on the
unchanged dependency graph; exploitability was not independently audited and no
automatic dependency fix was applied.
Docs reports six browser behaviors plus one existing placeholder. Production
navigation preserves 126 ordinary document entries, excludes both policy
routes with HTTP 404 and has no observed page errors. Six desktop/mobile
screenshots from Chromium 145.0.7632.6 were visually inspected; served homepage
bytes and build hashes matched the production output. The GitHub connector
cannot upload images, so local screenshots are not PR attachments.
This evidence prose was added afterward and checked separately.

Two additional native domain assessments were interrupted without final
answers. Effective Astra/max settings and all 63 recorded source/policy hashes
were verified; account memory was active and the ambient configuration hash
changed with unknown timing or cause. These cases are incomplete, not behavior
passes. Changed domain profiles retain independent source review; historical
native results below retain their original inputs. Copilot, the separately
denied Cosmos fixture and broader host/runtime acceptance remain open.

## Historical size and context accounting

The following comparison is the September 28 snapshot: main
`6002ab05a918c1e0c7391a7417db23a9f132d399`, parent
`27772cf5b2f6f6a41d2b58ca609f87d5e18feb46` and final
`46d985fc49adf6f3c8e2c2f4ed0d4f785dd587bd`.

Counts use normalized committed LF, with instruction-directory relocation kept
separate from net policy text reduction. Validation reports and this audit are
outside the policy corpus and are not hidden startup policy.

| Measure | Main | Before final layer | Final policy candidate |
| --- | ---: | ---: | ---: |
| Instruction directory lines | 3,164 | 2,445 | 907 |
| Entry points, instructions and new policy destinations | 3,388 | 2,637 | 2,165 |
| Same policy corpus bytes | 269,523 | 228,409 | 159,730 |
| Repository skills | 7 | 16 | 16 |

The final layer reduces the instruction directory by 1,538 lines, including
relocation, and the full policy corpus by 472 lines. Across the complete stack,
the corresponding decreases are 2,257 directory lines and 1,223 policy lines.
The three Docusaurus changes add four lines. New skills contain procedures that
are loaded when selected; their bodies are not included in the policy totals.

These six declared context packages include selected policy bodies and all
repository skill name/description metadata. Clean Squad includes its unchanged
full master on every side. Values use `o200k_base` as a static comparison proxy.

| Declared package | Main | Before final layer | Final policy candidate |
| --- | ---: | ---: | ---: |
| PowerShell | 25,828 | 22,155 | 14,214 |
| Framework C# | 36,430 | 31,084 | 22,205 |
| Public documentation | 28,607 | 24,686 | 16,604 |
| Sample feature with Markdown | 44,188 | 36,296 | 25,847 |
| All-guidance maintenance | 57,278 | 48,966 | 36,127 |
| Clean Squad including full master | 45,961 | 42,336 | 35,117 |

Selected skill bodies and their additional references, host framing, account
memory, tools, prompts and outputs are excluded. These are neither complete
native contexts nor billed-token or latency measurements. At that revision, policy and metadata inputs matched the recorded preparation;
the two lower-layer binding corrections were outside these declared packages.
The updated domain policy changes packages that select it. The historical token
counts and native assessments are not measurements of the rebased policy.

## Validation boundaries

The evidence below identifies the original September 28 candidate. The rebase
also brings upstream documentation, Spring tests and a validation-script fix.
The new stage evidence above covers applicable build, test and Docs validation;
the original results below remain historical and do not certify changed inputs.

Scoped source mapping and independent review cover 241 source units and 1,481
source-line dispositions. Configured Markdown checks bind to all 59 policy bodies. Adapter
references and unchanged custom-agent consumers were checked separately;
pre-existing missing reference targets were retained and recorded rather than
counted as repaired. The final layer introduces no adapter retirement.

Docusaurus retains the public default exclusions and additionally excludes
`**/AGENTS.md`. The direct utils dependency is the existing exact version 3.9.2;
the lock change is one reviewed root metadata line. The prepared prototype
passed typecheck/build and seven reported tests: six browser behaviors and one
existing placeholder. Output checks preserve 115 ordinary docs and exclude both
policy routes; six desktop/mobile screenshots were inspected. These results
are identified by their original inputs, including raw CRLF versus committed LF.

The assembled final candidate passed `pwsh ./go.ps1` on September 28, 2026:
3,434 tests executed and passed across 45 TRX files, with zero failures and
zero warnings/errors in both final builds. Forty-two TRX files contain tests;
three SDK files contain zero tests and are not counted as executed coverage.
Both cleanup stages passed. All 62 assembled input hashes remained unchanged.
This report was added afterward and receives its own Markdown/link checks; it
does not change build, test or policy inputs. No mutation run or score is claimed.

Fresh native Codex CLI sessions used the same `gpt-6-astra` model and `max`
reasoning setting as the migration review. Original native records establish
metadata discovery, whole selected skill-body reads and actual effective
settings. Across 89 portable cases, 85 passed their bounded rubrics, three
remain qualified by account-memory reads, and one original stack case retains
an ambiguous command-constraint limitation. Normal account guidance and memory
were present; these are not isolated causal or statistical reliability claims.

Lower-layer cases assess build repair, lesson admission, tracking and mutation
boundaries without live publication or mutation execution. Middle-layer native
outputs include a withdrawal example passing 24 assertions, shipping tests
passing 59 tests and detecting four injected faults, and clock tests passing
three tests and detecting a boundary fault. Corrected Spring binding assessment
is source evidence, not a Spring runtime run. Documentation selection exercised
six page-type contracts; three unchanged contracts retain source review only.
Upper-layer registration, Cosmos, PR and stack cases establish bounded selection
and assessment; historical registration runtime checks retain their own inputs.

Six additional read-only cases ran against the actual final repository candidate
for persistence, architecture, ADRs, readiness, PowerShell and Clean Squad.
Independent review confirms complete global policy and selected profile reads,
including all 1,450 lines of the unchanged Clean Squad master. One supplemental
Product Owner agent read omitted a line whose obligation was also present in
the fully read policy/master; no complete read of that supplemental file is
claimed. These cases assess decisions and do not execute the proposed work.
All six actively read account memory, so their results do not isolate the new
policy from that ambient context.
The first four-case wrapper failed its final configuration-hash guard;
per-session original settings and unchanged candidate hashes were verified,
but configuration-file stability and the cause of that failure remain unverified.
Raw native traces remain local
because they include account context. Public summaries retain these limits.

Copilot remains deferred and unverified until verified capacity returns. Cosmos
test authoring/runtime validation remains subject to the separate outstanding
fixture authorization. Native selection, read-only decisions, generated output,
externally executed fixture tests and full host conformance are separate evidence
categories. Current-head/base CI and complete feedback disposition remain live
delivery requirements. No merge or auto-merge is authorized.

## Review and rollback

This exceeds the 600-line target. Root/global authority, scoped destinations and
legacy adapters form one loading transition; the public-source policy files
need the Docs exclusion in the same layer. The user requested this as a separate
last PR. All changed lines, including the generated lock line, are counted.
An introduce-first split would require a separately reviewed interim loading
design. The coherent exception is subject to reviewer assessment.

Review root/global policy first, then scoped destinations and adapter duties,
then Docs exclusion and evidence limits. Reverting the actual final layer must
restore its 52 prior bodies and remove only its introduced files, leaving all
lower skills and custom agents intact. Verify the inverse diff and applicable
checks before calling rollback successful; no rollback execution is claimed.

## 4 October synchronization and current limits

Synchronization base `c8da151e607bcc8f3b253519317a8e7418d26261` is integrated
through all thirteen retained layers. Root preserves the complete current-main
low-risk merge authorization and optional model-routing contract. The campaign
has no merge authorization and does not select the optional Sol/Luna recipe.
The scoped Aspire profile retains current-main Aspire 13.6 and endpoint rules.
The sample profile retains all eight NotificationPulse disclosure/source bullets
and the current-main Blazor component ownership, test placement and CSS/token
policy route. New current-main instruction files remain discoverable.

All earlier counts, hashes, inventories, native assessments, build and browser
results above retain their stated historical inputs. They are not fresh passes
for this synchronized tree. Current cleanup/build/test, docs/browser and remote
checks require their own receipts. Independent CODEOWNER approval and the
unverified native Copilot/Codex matrix and consuming Cosmos fixture remain
open; no layer is merge-ready on historical evidence alone.
