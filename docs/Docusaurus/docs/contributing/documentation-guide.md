---
id: documentation-guide
title: Documentation Guide
sidebar_label: Documentation
description: Comprehensive guide for writing and maintaining Mississippi Framework documentation
keywords:
  - documentation
  - writing
  - contributing
  - style guide
  - best practices
---

# Documentation Guide

This guide establishes a consistent structure and tone for all Mississippi Framework documentation. Follow these principles to create content that is useful, maintainable, and easy to navigate.

## Tone and Language

### Purpose and Audience

Before writing a page, define its goal and identify who will read it: administrator, developer, architect, or executive. Documentation should exist only to enable decisions or actions.

### Write for Your Readers

Adjust terminology, depth, and examples to match reader expertise and time. Provide executive summaries for leaders, thorough explanations for implementers, and appendices for specialists.

### Use Concise, Factual Language

- Prefer active voice and present tense
- Avoid marketing language and jargon
- Give one idea per sentence
- Explain abbreviations on first use

### Follow RFC 2119 Where Appropriate

When stating requirements or recommendations, use "MUST," "SHOULD," or "MAY" consistently. Reserve these keywords for rules sections.

### Be Human and Helpful

Use a friendly tone; anticipate readers' questions and link to background material rather than repeating it. Code examples should be minimal but complete.

## Page Structure

Every documentation page should follow a predictable layout so that readers can orient themselves quickly.

### 1. Front Matter and Metadata

Include a YAML header with `id`, `title`, `sidebar_label`, and optional `description`. Set `keywords` to improve search. Use kebab-case filenames.

```yaml
---
id: my-topic
title: My Topic
sidebar_label: My Topic
description: Brief description of this page
keywords:
  - keyword1
  - keyword2
---
```

### 2. Title and Summary (H1)

Start with a clear, descriptive title and one-sentence summary explaining what the page covers.

### 3. On-Page Table of Contents

Provide a TOC component so users see the scope of information without scrolling. Limit heading levels to H3.

### 4. Body Sections (H2/H3)

Break content into logical sections. Use progressive disclosure: start with an overview, then deepen detail in later sections. If a section grows beyond 2–3 pages of text, split it into its own topic and cross-link.

### 5. Related Links and Navigation

At the end of each page, include "See also" links to related topics. Whenever you mention a concept, feature, or command, link to its description.

### 6. References and Footnotes

Place additional resources, specification references, or external links in a "References" section. Provide alt text for images and diagrams.

### 7. Last Updated and Ownership

Note the author and last updated date. Assign a maintainer responsible for updates.

## Topic Scope

### One Cohesive Topic Per Page

A page should answer a single question or cover a single concept or procedure. If you find yourself adding multiple rule sets or divergent tasks, split the content.

### Right-Size Topics

Aim for 5–10 minutes of reading time. Balance depth and brevity; provide enough detail to understand and act, but avoid overwhelming the reader.

### Avoid Deep Hierarchies

The Docusaurus sidebar should not nest beyond two levels; deeper nesting makes it hard for users to grasp the whole. Group related pages into sections and use cross-links to connect concepts.

### Use Multi-Level Documentation

Provide different layers:

- **High-level overviews**: Portal or section homepages with executive summaries
- **Task-oriented guides**: Step-by-step instructions for common workflows
- **In-depth references**: API documentation and detailed specifications

## Navigation and Information Architecture

### Doc Portal (Level 1)

The home page lists major documentation areas such as Introduction, Core Concepts, Guides, Reference, and Contribution. Use cards or a scannable list to orient users.

### Section Pages (Level 2)

Each major area has its own landing page summarizing its purpose and listing child pages. Keep the sidebar shallow: one or two sub-levels at most.

### Individual Pages (Level 3/4)

Pages contain the actual instructional content. Provide an on-page TOC and embed inline links to other topics. Readers often navigate via inline links rather than the sidebar.

### Related Resources

Include a "Related resources" section on section pages to point users to other relevant doc sets.

### Surface Popular Topics

Use analytics or feedback to identify frequently accessed pages and feature them prominently in the section or portal home.

## Visuals and Examples

### Use Visuals Strategically

Diagrams, flowcharts, and simple figures improve comprehension of:

- System architecture
- Process flows
- Data relationships
- UI elements

Keep visuals clear and uncluttered, use consistent symbols and colors, and provide alt text for accessibility.

### Code Samples and Snippets

Provide working examples in code fences with language identifiers. Keep them short; link to repositories or sample projects for full implementations. Use comments to explain non-obvious steps.

```csharp
// Example: Creating an aggregate
public class BankAccountAggregate : AggregateRoot<BankAccountState>
{
    // Aggregate implementation
}
```

### Tables and Lists

- Use tables for concise data (keywords, options, parameters)
- Avoid long sentences in tables
- Use lists for procedures or grouped ideas

## Maintenance and Versioning

### Living Documentation

Treat docs as evolving artifacts; use version control, assign owners, and review schedules. Update docs when features change, performance characteristics shift, or new components are added.

### Naming and Findability

Use clear, descriptive file names and titles; include keywords to aid search. Avoid duplication; link to the single source of truth.

### Templates and Consistency

Follow common templates for similar page types (concept, tutorial, reference). Reuse components such as admonitions, callouts, and code patterns.

## Writing Checklist

Before publishing a page, ensure:

- [ ] Purpose and audience are clearly defined and addressed
- [ ] The page adheres to the structure above (title, summary, TOC, sections, references)
- [ ] Language is concise and uses consistent terminology
- [ ] Inline links connect concepts and tasks
- [ ] Visuals and examples are accurate and accessible
- [ ] Metadata (keywords, author, last updated) is present
- [ ] The page is placed in the correct section, and the sidebar remains shallow
