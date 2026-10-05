# Architecture Decision Records (ADRs)

Software teams make dozens of architectural decisions every month — which
database to use, how to structure authentication, whether to adopt a new
framework, how to handle eventual consistency. These decisions are rarely
documented. When they are, the documentation lives in wiki pages that rot,
Confluence articles that nobody can find, or Slack threads that scroll into
oblivion. Six months later, a new team member asks "why did we choose Cosmos
over Postgres?" and nobody remembers the reasoning. The decision gets
re-litigated, reversed without understanding the original constraints, or
blindly preserved as cargo cult.

**This document is written in Minto Pyramid format.**

---

## Governing Thought

Architecture Decision Records (ADRs) are short, structured documents — stored
as Markdown files alongside the code they govern — that capture the context,
decision, and consequences of significant architectural choices. By treating
decisions as immutable, version-controlled artefacts with a defined lifecycle
(proposed, accepted, deprecated, superseded), teams preserve institutional
knowledge, prevent costly re-litigation, and enable new members to understand
not just what was decided but why.

---

## Situation

Every software project accumulates architectural decisions over its lifetime.
Early decisions — language choice, persistence strategy, messaging patterns,
deployment model — shape the system for years. Later decisions — adopting a new
library, splitting a monolith, changing serialization formats — carry migration
costs and compatibility implications. These decisions are made by people who
understand the current context: the constraints, trade-offs, alternatives
considered, and reasons one option was chosen over another.

## Complication

People leave. Context evaporates. The developer who chose event sourcing over
CRUD joins another company. The architect who selected Orleans over Akka retires.
The tech lead who rejected GraphQL in favour of REST moves to a different team.
Without written records, the reasoning behind decisions is lost. New team members
face a binary choice: accept the existing architecture on faith (cargo cult) or
challenge it without understanding the original constraints (uninformed
reversal). Both outcomes waste time and introduce risk.

Worse, when decisions are documented informally — in meeting notes, chat
messages, or email threads — the documentation is unsearchable, unversioned,
disconnected from the code, and impossible to maintain. There is no standard
format, no lifecycle, and no way to trace which decisions are still active
versus which have been superseded.

## Question

How should a team capture, organise, and maintain architectural decisions so
that the reasoning is preserved, discoverable, version-controlled, and useful
to both current and future team members?

---

## Key-Line 1: What an ADR Is

### Definition

An Architecture Decision Record is a short document that captures a single
architectural decision and its context. The term and practice were formalised by
**Michael Nygard** in a November 2011 blog post:

> "An architecture decision record is a short text file in a format similar to
> an Alexandrian pattern ... that describes a set of forces and a single
> decision in response to those forces."
>
> — Michael Nygard, "Documenting Architecture Decisions" (2011)

### What Constitutes an Architectural Decision

Not every technical choice warrants an ADR. An architectural decision is one
that:

- **Affects structure** — Changes the system's component boundaries, data flow,
  or deployment topology.
- **Is hard to reverse** — Carries significant migration cost if changed later.
- **Affects multiple teams or components** — Crosses boundaries beyond a single
  module or service.
- **Involves significant trade-offs** — Multiple viable alternatives exist, each
  with different consequences.
- **Sets a precedent** — Establishes a pattern that future decisions will follow.

Examples of decisions that warrant ADRs:

| Decision | Why It Qualifies |
|----------|-----------------|
| Adopt event sourcing over CRUD | Fundamental persistence paradigm; hard to reverse |
| Use Orleans for actor model | Framework choice affecting all runtime code |
| Store events in Cosmos DB | Infrastructure commitment with vendor implications |
| Use JSON over Protobuf for serialization | Affects wire format, versioning, and tooling |
| Adopt CQRS pattern | Structural pattern affecting all command/query paths |
| Choose .NET 9 as minimum target | Platform constraint affecting all projects |

Examples of decisions that typically do not warrant ADRs:

- Choosing between two equivalent NuGet packages for JSON parsing
- Naming a variable or method
- Picking a specific test assertion library
- CSS framework selection for an internal tool

### The Key Principle: Decisions Are Immutable Records

An accepted ADR is never edited to change the decision. If a decision is
reversed or superseded, a new ADR is created that references the original. The
original remains in the repository as a historical record. This immutability
is fundamental — it preserves the reasoning at the time the decision was made,
even when circumstances change later.

---

## Key-Line 2: ADR Format and Structure

### The Nygard Template (Minimal)

Michael Nygard's original template is deliberately minimal:

```markdown
# ADR-NNNN: Title of Decision

## Status

Accepted | Proposed | Deprecated | Superseded by ADR-XXXX

## Context

What is the issue that we're seeing that is motivating this decision or change?

## Decision

What is the change that we're proposing and/or doing?

## Consequences

What becomes easier or more difficult to do because of this change?
```

This four-section format — Status, Context, Decision, Consequences — is the
most widely adopted because it is simple enough that teams actually write ADRs,
and structured enough that the records are useful.

### The MADR Template (Markdown Any Decision Records)

**MADR** (Markdown Any Decision Records), created by **Oliver Kopp** and
maintained as an open-source project, extends Nygard's template with
additional sections for teams that want more rigour:

```markdown
# ADR-NNNN: Title in the Form of a Short Noun Phrase

## Status

Proposed | Accepted | Deprecated | Superseded by [ADR-XXXX](XXXX-title.md)

## Context and Problem Statement

Describe the context and problem statement, e.g., in free form using two to
three sentences or in the form of an illustrative story. You may want to
articulate the problem in the form of a question.

## Decision Drivers

- Driver 1 (e.g., performance requirement)
- Driver 2 (e.g., team expertise)
- Driver 3 (e.g., cost constraint)

## Considered Alternatives

### Alternative 1: Name

Description, pros, cons.

### Alternative 2: Name

Description, pros, cons.

### Alternative 3: Name

Description, pros, cons.

## Decision Outcome

Chosen alternative: "Alternative N", because [justification].

### Positive Consequences

- Consequence 1
- Consequence 2

### Negative Consequences

- Consequence 1
- Consequence 2

## Links

- [Related ADR](NNNN-related-decision.md)
- [RFC or design document](link)
```

MADR is available at <https://adr.github.io/madr/> and provides a GitHub
template repository for quick adoption.

### Choosing a Template

| Factor | Nygard (Minimal) | MADR (Extended) |
|--------|-----------------|-----------------|
| **Adoption friction** | Very low — four sections | Moderate — more sections to fill |
| **Alternatives documentation** | Implicit in Context | Explicit section with pros/cons |
| **Decision drivers** | Implicit | Explicit enumerated list |
| **Team maturity** | Good for teams starting out | Good for teams with ADR practice |
| **Traceability** | Basic status field | Links and cross-references |

The best template is the one the team will actually use. Start with Nygard's
minimal format. If the team finds they consistently need more structure, adopt
MADR. A short ADR that exists is infinitely more valuable than a comprehensive
template that nobody fills out.

---

## Key-Line 3: ADRs as Markdown Files in Version Control

### Why Markdown in the Repository

ADRs belong in the same repository as the code they govern, stored as Markdown
files. This is a deliberate choice with specific advantages:

| Property | In-Repo Markdown | Wiki / Confluence |
|----------|-----------------|-------------------|
| **Version controlled** | Git history tracks every change | Edit history exists but is separate from code |
| **Code review** | ADRs go through PR review | No review workflow |
| **Co-located** | Decisions live next to the code | Decisions live in a separate system |
| **Searchable** | `grep`, IDE search, GitHub search | Separate search interface |
| **Offline access** | Available with any `git clone` | Requires network access |
| **Tooling** | Linters, CI checks, link validation | Platform-dependent |
| **Ownership** | Same access controls as code | Separate permissions model |
| **Longevity** | Survives as long as the repo | Survives as long as the wiki platform |

### Directory Convention

The most common convention is a dedicated `docs/decisions/` or `docs/adr/`
directory:

```text
docs/
└── decisions/
    ├── 0001-use-event-sourcing.md
    ├── 0002-adopt-orleans-actor-model.md
    ├── 0003-cosmos-db-for-event-store.md
    ├── 0004-json-serialization-format.md
    └── 0005-cqrs-command-query-separation.md
```

### Naming Convention

ADR files follow a sequential numbering scheme:

```text
NNNN-short-descriptive-title.md
```

- **NNNN**: Zero-padded sequential number (0001, 0002, ...).
- **short-descriptive-title**: Lowercase, hyphen-separated summary of the
  decision.

The number provides a stable, unambiguous identifier for cross-referencing.
The title provides human-readable context in directory listings.

### Linking Between ADRs

ADRs frequently reference each other. Use relative Markdown links:

```markdown
Superseded by [ADR-0012: Migrate from JSON to Protobuf](0012-migrate-to-protobuf.md).
```

When an ADR supersedes another, both records should be updated:

- The **original** ADR's status changes to "Superseded by ADR-NNNN" with a
  link.
- The **new** ADR's context references the original: "This supersedes
  [ADR-0004](0004-json-serialization-format.md)."

---

## Key-Line 4: The ADR Lifecycle

### Status Values

ADRs have a defined lifecycle expressed through their status field:

```text
┌──────────┐     ┌──────────┐
│ Proposed  │────▶│ Accepted │
└──────────┘     └─────┬────┘
                       │
              ┌────────┴────────┐
              ▼                 ▼
       ┌────────────┐   ┌─────────────────┐
       │ Deprecated │   │ Superseded by   │
       │            │   │ ADR-NNNN        │
       └────────────┘   └─────────────────┘
```

| Status | Meaning |
|--------|---------|
| **Proposed** | The decision is under discussion. The ADR is in a PR awaiting review and approval. |
| **Accepted** | The decision has been approved and is in effect. This is the steady state for active decisions. |
| **Deprecated** | The decision is no longer relevant (e.g., the feature was removed). The record remains for historical context. |
| **Superseded** | A newer decision replaces this one. The status includes a link to the replacement ADR. |

### The Immutability Rule

Once an ADR reaches "Accepted" status, its Context and Decision sections are
not modified. If understanding improves or circumstances change:

1. **Minor clarifications** (typos, formatting) are acceptable.
2. **Substantive changes** to the decision require a new ADR that supersedes
   the original.

This rule exists because the original ADR captured the reasoning at a specific
point in time. Editing it retroactively destroys that historical record. A
superseding ADR, by contrast, explicitly documents what changed and why.

### When to Write an ADR

Write an ADR when:

- The team is about to make a decision that meets the criteria in Key-Line 1.
- A decision has already been made but was never documented (retrospective ADR).
- An existing decision needs to be reversed or significantly modified.

Write the ADR **before or during** the decision, not after. The act of writing
forces clarity: if you cannot articulate the context, alternatives, and
consequences in a short document, the decision may not be well understood.

---

## Key-Line 5: Writing Effective ADRs

### The Context Section

The context section is the most important part of the ADR. It must answer:

- **What problem are we solving?** — State the concrete issue, not an abstract
  goal.
- **What constraints exist?** — Budget, timeline, team expertise, existing
  infrastructure, compliance requirements.
- **What has changed?** — If this revisits a previous decision, what new
  information or circumstances prompted reconsideration?

Write context in the present tense as of the decision date. Include specific,
concrete details rather than generalities:

**Weak context:**

> We need to choose a database.

**Strong context:**

> Our event store currently uses Azure Table Storage, which limits query
> flexibility and does not support transactions across partition keys. The
> team has identified three pain points: (1) projections require full table
> scans for cross-aggregate queries, adding 2–5 seconds to dashboard loads;
> (2) lack of change feed support prevents real-time projections; (3) the
> 1 MB entity size limit constrains snapshot storage for large aggregates.
> The team has production experience with Cosmos DB and PostgreSQL but not
> DynamoDB.

### The Decision Section

State the decision clearly and concisely. Use active voice:

**Weak decision:**

> It was decided that Cosmos DB would be used.

**Strong decision:**

> We will use Azure Cosmos DB with the NoSQL API as the primary event store.
> Events will be partitioned by stream ID. We will use the change feed for
> real-time projection updates.

### The Consequences Section

Document both positive and negative consequences. Be honest about trade-offs:

**Weak consequences:**

> This will be better for the project.

**Strong consequences:**

> **Positive:**
>
> - Change feed enables real-time projections without polling.
> - Cosmos DB's document model removes the 1 MB entity size limit.
> - Cross-partition queries support flexible projection rebuilds.
> - Team has existing Cosmos DB expertise from the Reservoir project.
>
> **Negative:**
>
> - Cosmos DB costs are usage-based (RU/s); unexpected traffic spikes could
>   increase costs significantly.
> - Local development requires the Cosmos DB emulator, which is Windows-only
>   and resource-intensive.
> - Vendor lock-in increases; migrating away from Cosmos DB would require
>   rewriting the storage layer.

### Common Writing Mistakes

| Mistake | Problem | Fix |
|---------|---------|-----|
| **No alternatives discussed** | Reader cannot tell if other options were considered | Add a "Considered Alternatives" section or mention them in Context |
| **Vague context** | "We need to modernise" tells the reader nothing | Include specific problems, metrics, and constraints |
| **Missing constraints** | Decision seems arbitrary without understanding the boundaries | List budget, timeline, team skills, and technical constraints |
| **No negative consequences** | Looks like the author did not think critically | Every decision has trade-offs; document them honestly |
| **Written after the fact without context** | Retrospective ADRs often lack the "why" | Interview the original decision-makers; reconstruct constraints |
| **Too long** | Nobody reads a 10-page ADR | Target 1–2 pages; if longer, the decision may need splitting |

---

## Key-Line 6: Tooling and Automation

### adr-tools (Nygard's CLI)

**adr-tools**, created by Nat Pryce, is a command-line toolkit for managing
ADRs:

```bash
# Initialise the ADR directory
adr init docs/decisions

# Create a new ADR
adr new "Use event sourcing for persistence"

# Record that ADR 5 supersedes ADR 3
adr new -s 3 "Migrate from JSON to Protobuf serialization"

# Generate a table of contents
adr generate toc

# Generate a graph of ADR relationships
adr generate graph | dot -Tpng -o adr-graph.png
```

Repository: <https://github.com/npryce/adr-tools>

### Log4brains

**Log4brains** is a more modern tool that generates a searchable, static
website from ADR Markdown files:

- Supports MADR template out of the box.
- Generates a browsable knowledge base with timeline and search.
- Integrates with CI/CD to publish the ADR site automatically.

Repository: <https://github.com/thomvaill/log4brains>

### CI Integration

ADRs can be validated in CI pipelines:

| Check | Purpose |
|-------|---------|
| **Status field present** | Every ADR has a valid status |
| **Sequential numbering** | No gaps or duplicates in ADR numbers |
| **Cross-reference integrity** | Superseded ADRs link to valid replacements |
| **Markdown lint** | ADR files pass the same lint rules as other docs |
| **Template compliance** | Required sections are present |

### GitHub Pull Request Workflow

The recommended workflow for ADR adoption:

1. Author creates the ADR as a Markdown file in a feature branch.
2. The PR description summarises the decision and links to the ADR file.
3. Team members review the ADR in the PR — comments, alternatives, and
   consequences are discussed inline.
4. Once approved, the ADR is merged with status "Accepted".
5. The PR serves as the discussion record; the ADR serves as the decision
   record.

This workflow makes ADR creation a natural part of the development process
rather than a separate documentation chore.

---

## Key-Line 7: Organisational Adoption

### Starting Small

The most common failure mode for ADR adoption is ambition. Teams create
elaborate templates, governance processes, and review boards — and then nobody
writes ADRs because the overhead is too high.

Start with:

1. **One template** — Nygard's four-section format.
2. **One directory** — `docs/decisions/` in the main repository.
3. **One rule** — Any decision that changes system structure or is hard to
   reverse gets an ADR.
4. **One workflow** — ADRs are created in PRs and reviewed like code.

### Building the Habit

| Practice | Effect |
|----------|--------|
| **Write retrospective ADRs** for existing decisions | Builds the initial corpus and demonstrates value |
| **Reference ADRs in PRs** | "This implements ADR-0003" connects code to decisions |
| **Review ADRs in onboarding** | New members read the decision log as part of joining |
| **Include ADR check in PR review** | "Does this change warrant an ADR?" becomes a review habit |
| **Keep ADRs short** | 1–2 pages maximum; brevity encourages writing |

### Scaling Across Teams

For organisations with multiple repositories or teams:

- **Repository-scoped ADRs** document decisions specific to one codebase.
- **Organisation-wide ADRs** (in a central repository) document cross-cutting
  decisions: authentication standards, API conventions, infrastructure choices.
- **Cross-references** link repository ADRs to organisation ADRs when a local
  decision implements or extends an organisational standard.

### When ADRs Fail

| Failure Mode | Cause | Remedy |
|-------------|-------|--------|
| **Nobody writes them** | Template too heavy, process too formal | Simplify template; remove governance overhead |
| **ADRs rot** | No lifecycle management; superseded ADRs not updated | Add status checks to CI; review ADRs quarterly |
| **ADRs are too vague** | Authors skip context and consequences | Provide concrete examples; review ADRs in PRs |
| **ADRs are ignored** | Not referenced in code or PRs | Add ADR references to PR templates and code comments |
| **Decision re-litigation** | Team does not trust or read ADRs | Reference the ADR in discussions; make reading ADRs part of onboarding |

---

## Summary

Architecture Decision Records solve the problem of lost institutional
knowledge by capturing decisions as structured, immutable Markdown files stored
alongside the code. The practice requires minimal tooling — a directory, a
simple template, and a PR workflow — but delivers outsized value: preserved
context, reduced re-litigation, faster onboarding, and honest documentation of
trade-offs.

The essentials:

- **One decision per ADR**, structured as Context → Decision → Consequences.
- **Stored as Markdown** in the repository, version-controlled and reviewed in
  PRs.
- **Immutable once accepted** — changes produce new ADRs that supersede the
  original.
- **Short and honest** — 1–2 pages with explicit trade-offs and negative
  consequences.
- **Start simple** — Nygard's four-section template, one directory, one rule.
- **Build the habit** — retrospective ADRs, PR references, onboarding reading.

The decision to adopt ADRs is itself worth an ADR.

---

## References and Further Reading

- Nygard, Michael. "Documenting Architecture Decisions." Blog post, November
  2011. <https://www.cognitect.com/blog/2011/11/15/documenting-architecture-decisions>
- Kopp, Oliver et al. "MADR — Markdown Any Decision Records." Open-source
  project. <https://adr.github.io/madr/>
- Pryce, Nat. "adr-tools." Command-line toolkit for ADR management.
  <https://github.com/npryce/adr-tools>
- Keeling, Michael. "Architecture Decision Records in Action." Saturn 2017
  conference talk. <https://resources.sei.cmu.edu/library/asset-view.cfm?assetid=497744>
- Zimmermann, Olaf et al. "Architectural Decision Guidance Across Projects."
  WICSA 2015. (Academic treatment of ADR practices and patterns.)
- ThoughtWorks Technology Radar. "Lightweight Architecture Decision Records."
  Adopt recommendation since 2016.
  <https://www.thoughtworks.com/radar/techniques/lightweight-architecture-decision-records>
- Tyree, Jeff and Akerman, Art. "Architecture Decisions: Demystifying
  Architecture." IEEE Software, Vol. 22, No. 2, March/April 2005. (Early
  academic formalisation of architecture decision documentation.)
