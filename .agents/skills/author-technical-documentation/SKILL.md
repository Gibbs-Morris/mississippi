---
name: author-technical-documentation
description: Create, revise, or validate engineering documentation pages in Markdown or MDX from evidence and reader intent. Use for getting-started pages, tutorials, how-to guides, concepts, reference, operations, troubleshooting, migration guides, or release notes. Not for ADRs, PR descriptions, or answering questions without a documentation task.
---

# Author technical documentation

Produce only the requested page, focused update, or validation findings. A
review-only request does not authorize edits; a narrow edit does not require a
whole-page rewrite. Publishing, changing code or configuration, and creating
additional artifacts require the user's request or the caller's authority.

## Select the page contract

Identify the reader, primary question, requested scope, and page type before
drafting or reviewing. Honor an explicitly requested type and the project's
classification rules. If the topic needs multiple intents, split or cross-link
within the authorized scope; do not silently drop requested content or create
extra pages to evade a constraint. Resolve a material scope conflict before
claiming the task is complete.

Read the reference for the selected type, not every reference in this table:

| Reader intent | Page contract |
| --- | --- |
| Reach one first successful result | [Getting started](references/getting-started.md) |
| Learn through a guided sequence | [Tutorials](references/tutorials.md) |
| Complete a specific task | [How-to guides](references/how-to.md) |
| Understand a model and its trade-offs | [Concepts](references/concepts.md) |
| Look up exact supported facts | [Reference](references/reference.md) |
| Run a system safely | [Operations](references/operations.md) |
| Diagnose and resolve a symptom | [Troubleshooting](references/troubleshooting.md) |
| Move between defined versions | [Migration](references/migration.md) |
| Understand a release and required action | [Release notes](references/release-notes.md) |

These contracts provide portable engineering guidance. Read any applicable
project policy, template, and local authoring guide as well; honor explicit user
or project overrides rather than silently replacing an established format.
ADRs and PR descriptions use their own contracts instead of these page types.

## Establish the evidence and local bindings

For repository changes, discover the actual documentation location, required
metadata, terminology, navigation conventions, supported markup, and validation
commands. Do not assume a site generator, folder layout, shell, or metadata
schema. Use sufficient supplied evidence for a draft without demanding access
that the requested output does not need.

Identify the source of each material technical claim: code, tests, verified
examples, configuration, decision records, or observed behavior. For a change
summary, inspect the selected diff against its actual base, not an unrelated
trunk comparison. Distinguish guaranteed, default, typical, internal,
unsupported, and intended future behavior. A fluent draft or old result is not
evidence; do not invent APIs, defaults, error messages, benchmarks, or guarantees.

When runtime behavior is involved, apply the project's relevant checks for
lifecycle, concurrency, ordering, retries/timeouts, durability, recovery,
serialization/versioning, deployment assumptions, telemetry, and security.
Keep the scope of each claim no broader than the evidence supporting it.

## Write or review within scope

Follow the selected contract and local format. State the page's answer or scope
early, keep prerequisites explicit, and link deeper explanation or exact
reference material where it belongs. Use the project's verified examples;
clearly distinguish complete runnable examples from deliberate partial excerpts.

For each factual claim in a draft, formulate a verification question and answer
it from evidence rather than from the draft itself. Correct or remove unsupported
claims, or label the unfinished draft and its missing evidence. Do not publish
an unverified assumption as a supported fact. Preserve still-valid content and
avoid unrelated rewrites when correcting a page.

Use diagrams, tabs, and callouts only where they clarify a real distinction or
change reader behavior, following the project's rendering and accessibility
rules. Keep diagrams aligned with the authoritative prose and separate genuine
variants from unnecessary branching in a guided path.

For validation-only work, inspect the claims and links independently, report
evidenced findings and unverified areas, and retain the caller's severity and
output format. Do not turn a review into editing or publication.

## Verify and report

Check the selected type, required content, metadata, links, examples, terminology,
navigation, and final diff. Run the applicable authorized validation, including
the project's lint, document build/render checks, and executable-example checks
when required. If a tool or evidence is unavailable, distinguish a prepared
draft from a validated page and state the remaining limitation.

Return the requested output and relevant results. Report only checks actually
run and publication actually confirmed. Creating a page does not itself prove
that its described behavior, deployment, or operational procedure works.
