---
title: Reference Pages
description: Document exact Mississippi contracts, options, defaults, constraints, and compatibility notes without tutorial narrative.
sidebar_label: Reference
sidebar_position: 6
---

# Reference Pages

Use a reference page when the reader needs exact, neutral, complete, and scannable facts.

Reference pages should mirror the system they describe. They are not the place for long rationale or guided walkthroughs.

## Use This Page Type When

- the reader needs an API contract
- the reader needs configuration details
- the reader needs exact defaults, allowed values, constraints, or compatibility notes

## Reference Template

Use the relevant sections from this template:

1. one-sentence summary
2. `## Applies to`
3. `## Syntax`, `## Contract`, or `## Configuration`
4. `## Parameters`, `## Properties`, or `## Options`
5. `## Defaults`
6. `## Allowed values` or `## Constraints`
7. `## Behavior`
8. `## Errors`, `## Exceptions`, or `## Failure behavior`
9. `## Compatibility` or `## Version notes`
10. `## Example`
11. `## Related content`

## Content Rules

- Stay factual and concise.
- Document side effects, cancellation behavior, ordering implications, and idempotency expectations when relevant.
- Every example must be verified.
- Use comments, not ellipses, if you intentionally omit code.

## Minimal Frontmatter Example

```yaml
---
title: Brook Storage Configuration Reference
description: Reference the supported configuration keys, defaults, scopes, and constraints for Mississippi brook storage providers.
sidebar_position: 50
sidebar_label: Brook Storage Config
---
```

## Page Skeleton Example

```md
# Brook Storage Configuration Reference

This page documents the exact configuration contract for brook storage providers.

## Applies to

## Configuration

## Options

## Defaults

## Constraints

## Behavior

## Failure behavior

## Related content
```

## Related Content

- [Documentation Guide](./documentation-guide.md)
- [Concept Pages](./documentation-concepts.md)
- [How-To Guides](./documentation-how-to.md)
