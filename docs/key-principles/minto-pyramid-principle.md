# The Minto Pyramid Principle

**This document explains the Minto Pyramid format and is intentionally not
written in that format itself, since it serves as the meta-reference that
defines the structure all other documents follow.**

## What It Is

The Minto Pyramid Principle is a structured communication framework developed by
**Barbara Minto** at McKinsey & Company in the late 1960s and formally published
in her book *The Minto Pyramid Principle: Logic in Writing, Thinking and Problem
Solving* (first edition 1987, revised editions 1996 and 2010).

The framework provides a method for organising ideas hierarchically so that the
most important conclusion or recommendation is presented first, supported by
progressively detailed arguments beneath it. It is the dominant standard for
structuring business writing, consulting presentations, and executive
communication.

> "The Pyramid Principle says that ideas in writing should always form a pyramid
> under a single thought. The single thought is the answer to the reader's
> question. Beneath it are the arguments that support or explain that thought,
> and beneath each argument are the detailed data or reasoning that supports
> it."
>
> — Barbara Minto, *The Minto Pyramid Principle* (2010 edition)

## Why It Matters

Most people write in the order they think: background first, analysis second,
conclusion last. This forces the reader to hold all the details in mind before
understanding the point. The Pyramid Principle reverses this. By leading with
the answer, the reader immediately grasps the purpose of the communication and
can then choose how deeply to read.

This is particularly valuable when:

- The audience is time-constrained (executives, reviewers, stakeholders).
- The material is complex and requires the reader to hold multiple threads.
- The document will be used as a reference that people return to repeatedly.
- Multiple authors contribute and need a shared structural discipline.

## The Core Structure: SCQA

The Pyramid Principle uses the **SCQA** framework to introduce and frame a
communication:

| Element | Purpose | Example |
|---|---|---|
| **Situation** | Establish the shared, uncontroversial context the reader already knows. | "Our team ships a .NET event-sourcing framework." |
| **Complication** | Introduce the problem, change, or tension that disrupts the situation. | "New contributors frequently misapply the CQRS pattern, causing broken builds." |
| **Question** | State (explicitly or implicitly) the question the reader now has in mind. | "How do we prevent these errors and reduce onboarding time?" |
| **Answer** | Deliver the recommendation or conclusion — the governing thought at the top of the pyramid. | "We should create a set of key-principles reference documents for agent and human use." |

The answer then becomes the apex of the pyramid. Everything that follows
supports, explains, or provides evidence for that answer.

## Building the Pyramid

### The Governing Thought

Every pyramid has exactly one governing thought at the top. This is the single
most important thing the audience must take away. If the reader reads nothing
else, they should read this.

> "If you find you cannot state a single governing thought, you do not yet know
> what you are trying to say."
>
> — Barbara Minto

### Key-Line Arguments

Directly beneath the governing thought sit the **key-line arguments** — usually
between two and five. These are the main pillars that support the conclusion.
Each key-line argument must:

1. Directly support or prove the governing thought.
2. Be mutually exclusive (no overlap between arguments).
3. Be collectively exhaustive (together they fully support the conclusion).

This property is known as **MECE** — Mutually Exclusive, Collectively
Exhaustive.

### Supporting Detail

Beneath each key-line argument are the facts, data, examples, or reasoning that
substantiate it. This is where analysis, evidence, and worked examples live.

```text
                    ┌──────────────────┐
                    │ Governing Thought│
                    │  (The Answer)    │
                    └────────┬─────────┘
               ┌─────────────┼─────────────┐
               ▼             ▼             ▼
        ┌─────────┐   ┌─────────┐   ┌─────────┐
        │Key-Line │   │Key-Line │   │Key-Line │
        │   A     │   │   B     │   │   C     │
        └────┬────┘   └────┬────┘   └────┬────┘
          ┌──┴──┐       ┌──┴──┐       ┌──┴──┐
          ▼     ▼       ▼     ▼       ▼     ▼
        Detail Detail Detail Detail Detail Detail
```

## Logical Ordering Within the Pyramid

Minto identifies three ways to order the key-line arguments or supporting
detail beneath any point in the pyramid:

1. **Deductive order** — A logical chain where each point follows necessarily
   from the previous one (major premise → minor premise → conclusion).
2. **Chronological order** — Steps in a process or events in time sequence
   (first → then → finally).
3. **Structural order** — A breakdown of a whole into its parts, ordered by
   some principle such as importance, geography, or category (component A →
   component B → component C).

The choice of ordering depends on what makes the argument clearest to the
reader.

## The Vertical and Horizontal Rules

Minto defines two rules that govern relationships within the pyramid:

### Vertical Rule

> Every point at any level of the pyramid must be a summary of the points
> grouped below it.

This means a reader can stop at any level and have a complete (if less
detailed) understanding of the argument.

### Horizontal Rule

> Points in a group must be logically alike and must together constitute a
> complete set — they must be MECE.

This prevents gaps in reasoning and eliminates redundancy.

## Applying the Pyramid Principle in Practice

### For Written Documents

1. **Start with the answer.** Open with the recommendation, conclusion, or
   finding. Do not build up to it.
2. **Follow with key-line arguments.** Each section heading should state the
   point of that section, not merely label its topic.
3. **Support each argument.** Provide evidence, data, or reasoning beneath each
   key-line point.
4. **Use SCQA for the introduction.** Frame context, complication, question, and
   answer before the body begins.

### For Presentations

1. **Slide titles are assertions, not labels.** Each slide title should state
   the point the slide makes (e.g., "Revenue grew 12% in Q3" rather than
   "Q3 Revenue").
2. **The executive summary slide mirrors the pyramid apex.** It delivers the
   governing thought and key-line arguments.
3. **Detail slides support key-lines.** Each section of the presentation maps
   to a key-line argument.

### For Day-to-Day Communication

- Emails: State the ask or conclusion in the first sentence.
- Pull request descriptions: Lead with what changed and why, then provide
  supporting detail.
- Status updates: Lead with the outcome, then explain how you got there.

## Common Mistakes

| Mistake | Why it fails | Correction |
|---|---|---|
| Leading with background | The reader does not know why they are reading it. | Move background into the Situation of an SCQA introduction. |
| Listing activities instead of conclusions | "We did X, Y, Z" gives no answer. | State the answer first; activities become supporting detail. |
| Overlapping arguments | Violates MECE; the reader cannot tell the arguments apart. | Ensure each key-line covers a distinct, non-overlapping aspect. |
| Too many key-lines | More than five overloads working memory. | Group sub-arguments under a smaller number of key-lines. |
| Topic-label headings | "Background", "Analysis", "Recommendations" tell the reader nothing. | Use assertion-form headings that state the point. |

## Authoritative Sources

| Source | Reference |
|---|---|
| **Primary text** | Minto, B. (2010). *The Minto Pyramid Principle: Logic in Writing, Thinking and Problem Solving*. Pearson Education. ISBN 978-0-273-71051-6. |
| **McKinsey origin** | The framework was developed while Barbara Minto was the first female MBA hire at McKinsey & Company, where it became the firm's standard for written communication. It has since been adopted globally across consulting, finance, technology, and government. |
| **Harvard Business Review** | The Pyramid Principle is referenced extensively in HBR's guidance on executive communication and structured thinking. |

## Summary

The Minto Pyramid Principle is a top-down communication framework: answer first,
key-line arguments second, supporting detail third. It uses the SCQA
introduction pattern to frame the reader's context. All arguments must be MECE.
Every level of the pyramid summarises what sits beneath it. Mastering this
structure ensures that any document, presentation, or communication is clear,
efficient, and immediately actionable by its audience.
