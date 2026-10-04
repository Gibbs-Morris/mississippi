---
applyTo: '**'
---

# Plain English Communication

Governing thought: Write so people can understand the message on first reading, while keeping formal requirements precise.

> Drift check: Keep this policy aligned with [RFC 2119 usage](rfc2119.instructions.md) and [instruction authoring](authoring.instructions.md).

## Rules (RFC 2119)

- Agents **MUST** use clear, plain English for text intended for people, including conversations, progress updates, reviews, and pull request comments and replies. Why: Readers need to understand the message without decoding unnecessary jargon.
- Agents **SHOULD** state the main point first, followed by the reason, evidence, and next action when relevant.
- Agents **SHOULD** use familiar words, short sentences, and active voice suited to the reader.
- Agents **MUST** explain unfamiliar technical terms and abbreviations when the reader needs them to understand the message.
- Ordinary conversations and comments **MUST NOT** use RFC 2119 capitalization unless quoting a rule or explicitly writing a formal requirement. Why: Everyday messages need natural wording.
- Instruction rules and formal requirements **MUST** retain RFC 2119 keywords and their obligation strength under the [RFC policy](rfc2119.instructions.md).
- Rewrites **MUST** preserve technical meaning, evidence, uncertainty, and exact identifiers or quoted text. Why: Simpler wording must not change facts, weaken requirements, or imply unverified success.
- Review comments and replies **SHOULD** explain the concern or decision, its reason, and the relevant action or evidence.
- Agents **MUST** apply this communication policy ahead of conflicting persona styles or shorthand preferences. Why: A specialized role does not excuse unclear writing.

## Scope and Audience

All repository agents writing messages or prose for people. This includes issue and pull request text, documentation, and code comments. Formal requirements can appear within those formats; their requirement wording stays intact. Existing spelling conventions and required document structures still apply.

## At-a-Glance Quick-Start

Lead with the point. Explain necessary terms. Check that the reader can understand the outcome, reason, and any next step.

For a requested rewrite, use [Rewrite plain English](../../.agents/skills/rewrite-plain-english/SKILL.md). Routine messages follow this policy without needing a separate skill invocation.

## Core Principles

Clarity and accuracy belong together. Plain English does not require formal ASD-STE100 compliance.

## References

- [Plain English Campaign guides](https://www.plainenglish.co.uk/free-guides)
- [GOV.UK Functional Standards writing style guide](https://www.gov.uk/government/publications/handbook-for-standard-managers/functional-standards-writing-style-guide)

These sources inform clarity. Repository RFC 2119 conventions govern formal requirements.
