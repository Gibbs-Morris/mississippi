---
id: documentation-troubleshooting
title: Troubleshooting Pages
description: Diagnose Mississippi failures from symptoms and evidence, then guide the reader through confirmation, resolution, and prevention.
sidebar_label: Troubleshooting
sidebar_position: 8
---

# Troubleshooting Pages

## Overview

Use a troubleshooting page when the reader starts from a symptom and needs to diagnose and resolve a problem from evidence.

Organize the page by symptom, not by subsystem alone.

The primary reader question is: "Given this symptom, how do I confirm the cause, fix it, and verify recovery?"

## Use This Page Type When

- the reader has a failure, timeout, deserialization problem, or runtime symptom
- the page can provide real evidence patterns, not speculative guesses
- the page needs confirmation steps and fix verification

## Required Structure

1. one-sentence symptom statement
2. `## Symptoms`
3. `## What this usually means`
4. `## Probable causes`
5. `## How to confirm`
6. `## Resolution`
7. `## Verify the fix`
8. `## Prevention`
9. `## Summary`
10. `## Next Steps`

## Content Rules

- Use real error messages, metrics, or log signatures when available.
- Do not fabricate stack traces.
- Do not list speculative causes unless you also explain how to confirm or rule them out.
- State whether the fix is safe live and whether restart, rollout, or state repair is required.

## Minimal Frontmatter Example

```yaml
---
title: State Deserialization Fails After Upgrade
description: Diagnose and resolve Mississippi state deserialization failures after an upgrade using evidence, compatibility checks, and repair guidance.
sidebar_position: 70
sidebar_label: Deserialization Failures
---
```

## Page Skeleton Example

```md
# State Deserialization Fails After Upgrade

Use this guide when Mississippi fails to load persisted state after a version change.

## Symptoms

## What this usually means

## Probable causes

## How to confirm

## Resolution

## Verify the fix

## Prevention

## Summary

## Next Steps
```

## Summary

- troubleshooting pages start from observable symptoms, not subsystem tours
- they should pair each probable cause with confirmation steps and concrete resolution guidance
- speculative causes without a confirmation path do not belong in this page type

## Next Steps

- [Documentation Guide](./documentation-guide.md)
- [Concept Pages](./documentation-concepts.md)
- [Migration Pages](./documentation-migration.md)
