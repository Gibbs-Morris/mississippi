---
name: rewrite-plain-english
description: Rewrite supplied text in clear, plain English while preserving meaning, technical details, and uncertainty. Use for simplifying messages, pull request comments, review replies, or documentation. Not for verifying facts, changing requirements, or publishing messages.
---

# Rewrite plain English

Rewrite the supplied text for its intended reader. Use the user's audience,
tone, length, and spelling preferences when provided; otherwise preserve the
source's conventions and use a direct, professional tone.

## Preserve the meaning

- Identify whether each passage is ordinary prose, a formal requirement, or a
  quotation. Keep RFC 2119 keywords and obligation strength in formal
  requirements. Use natural wording in ordinary conversations and comments.
- Preserve who does what, conditions, exceptions, negation, quantities, dates,
  sequence, and requested actions. Keep code, commands, paths, identifiers,
  links, and exact quotations unchanged.
- Retain uncertainty and the distinction between planned, completed, verified,
  failed, pending, and unrun work. Do not add test results, commit references,
  guarantees, or facts absent from the source.
- If the source has an ambiguity that changes its meaning, ask only for the
  missing detail or flag it separately. Do not silently choose an interpretation.

## Make the text easier to read

Put the main point first. Replace unnecessary jargon, inflated phrases, and
abbreviations with familiar words. Explain a necessary technical term briefly
when the reader needs it. Prefer direct verbs and short sentences, with one
main idea in each sentence.

Keep the detail needed to understand the decision. Use lists when they make
steps or several points easier to follow. Preserve required template sections,
review labels, and structured fields.

For a review comment, make the concern, effect, and requested change clear.
For a reply, make the decision, reason, and available evidence clear. Include
only information present in the source.

## Check and return the rewrite

Compare the rewrite with the source for changed meaning, missing qualifications,
and invented facts. Return the rewritten text first. Add a brief note only when
an unresolved ambiguity or meaningful editorial choice needs the user's attention.

Rewriting text does not authorize posting a comment, sending a message, or
editing a remote record. Use existing task authorization for any requested
publication step.

## Examples

Ordinary reply:

> The fix has been implemented; CI validation remains pending.

Plain English:

> The fix is in place. The automated checks have not finished.

Formal requirement:

> The caller MUST NOT retry after cancellation.

Keep this requirement's prohibition and RFC 2119 wording intact.

## References

- [Plain English Campaign guides](https://www.plainenglish.co.uk/free-guides)
- [GOV.UK Functional Standards writing style guide](https://www.gov.uk/government/publications/handbook-for-standard-managers/functional-standards-writing-style-guide)

Use these sources for clarity, not to replace the project's formal requirement
conventions. This skill does not claim ASD-STE100 compliance.
