---
id: documentation-section-entry-pages
title: Section Entry Pages
sidebar_label: Section Entry Pages
sidebar_position: 11
description: Write section landing pages that orient readers, establish scope, and send them to the right next page without sounding like placeholders.
---

# Section Entry Pages

## Overview

Section entry pages are landing pages.

They are not status notes, migration notices, or apologies for missing content. Their job is to give a strong first impression, define the section clearly, and direct readers to the right next page.

## What A Good Section Entry Page Must Do

A good section landing page answers these questions in order:

1. what is this area
2. why would I use it
3. what belongs in this section
4. when should I start here
5. what should I read next

If the area is independently adoptable, the page should also name representative package entry points.

## Recommended Structure

Use this structure for section entry pages unless there is a strong reason to narrow it.

1. `## Overview`
2. `## Why This Area Exists`
3. `## Representative Packages` when package choice matters
4. `## What This Area Owns`
5. `## Use This Section`
6. `## How It Fits Mississippi`
7. `## Current Coverage`
8. `## Learn More`

Not every page needs every section, but the page must still answer the underlying questions.

## Strong Patterns

- Lead with reader value, not documentation process.
- Use concrete language about behavior, responsibilities, and boundaries.
- Name the major contracts, runtimes, or packages that define the area.
- Explain how the area composes with adjacent Mississippi subsystems.
- Describe current coverage as available material, not as an apology.

## Weak Patterns To Avoid

Do not write section entry pages like this:

- "This is a holding page"
- "The docs are being rebuilt"
- "This docs reset organizes"
- generic ownership lists with no reason to care
- file-system language presented as user guidance

Those phrases explain the authoring process instead of helping the reader choose the section.

## Current Coverage Guidance

Use `## Current Coverage` to tell the reader what is available now.

Good examples:

- "Use the archived Reservoir material for detailed reference and testing guidance while the new section is expanded."
- "This section currently gives the architectural boundary and package entry points; deeper operational and reference pages are still to be added."

Bad examples:

- "This is a placeholder page."
- "More docs coming soon."

## Package Guidance For Entry Pages

If the section represents an independently adoptable area, include representative package IDs rather than every possible package.

Choose the smallest set that helps a reader understand the adoption surface:

- contracts or abstractions package
- primary runtime package
- primary UI, gateway, or integration package when applicable

## Summary

- section entry pages are landing pages, not placeholder notices
- they should define the area, justify it, bound it, and route the reader onward
- package cues matter when the area can be adopted independently
- current coverage should be honest without sounding apologetic

## Next Steps

- [Documentation Guide](./documentation-guide.md)
- [Concept Pages](./documentation-concepts.md)
- [Reference Pages](./documentation-reference.md)
- [Tutorial Pages](./documentation-tutorials.md)