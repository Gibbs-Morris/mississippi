---
id: documentation-operations
title: Operations Pages
description: Help engineers run Mississippi safely in production with explicit guidance on validation, failure modes, telemetry, and rollback.
sidebar_label: Operations
sidebar_position: 7
---

# Operations Pages

## Overview

Use an operations page when the reader is responsible for reliability, throughput, security, cost, or recovery in a live system.

Operational guidance must be concrete. Replace vague advice with signals, thresholds, dashboards, commands, and validation steps whenever the evidence exists.

The primary reader question is: "How do I run or change this safely in a live system and prove the outcome?"

## Use This Page Type When

- the topic is about production behavior or rollout safety
- the page needs deployment, restart, scaling, or fault-tolerance guidance
- the reader must know what to watch and how to validate a live change

## Required Structure

1. direct operational goal or concern
2. `## When this matters`
3. `## Prerequisites and assumptions`
4. `## Recommended baseline`
5. `## Procedure` or `## Operational guidance`
6. `## Validation`
7. `## Failure modes and rollback`
8. `## Telemetry to watch`
9. `## Summary`
10. `## Next Steps`

## Content Rules

- State what is safe live and what requires a maintenance window.
- State blast radius when a setting can affect the whole cluster.
- Cover capacity limits, scaling triggers, deployment order, mixed-version behavior, disaster recovery, observability, security, and cost when relevant.
- Do not write vague advice such as "scale as needed" without operational criteria.

## Minimal Frontmatter Example

```yaml
---
title: Operate Mississippi Cluster Rollouts
description: Safely roll out Mississippi updates with explicit validation, rollback, and telemetry guidance.
sidebar_position: 60
sidebar_label: Cluster Rollouts
---
```

## Page Skeleton Example

```md
# Operate Mississippi Cluster Rollouts

This page explains how to roll out Mississippi updates safely in production.

## When this matters

## Prerequisites and assumptions

## Recommended baseline

## Operational guidance

## Validation

## Failure modes and rollback

## Telemetry to watch

## Summary

## Next Steps
```

## Summary

- operations pages are for live-system safety, validation, and rollback
- they must replace vague advice with concrete signals, procedures, and telemetry
- they should state what is safe live, what is not, and how to prove the change succeeded

## Next Steps

- [Documentation Guide](./documentation-guide.md)
- [Migration Pages](./documentation-migration.md)
- [Troubleshooting Pages](./documentation-troubleshooting.md)
