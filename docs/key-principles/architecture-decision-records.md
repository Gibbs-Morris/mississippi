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

Architecture Decision Records (ADRs) are short, structured documents - stored
as Markdown files alongside the code they govern - that capture the context,
decision, and consequences of significant architectural choices. In
Mississippi, published ADRs use immutable frontmatter identity, explicit
human-readable slugs, deterministic ordering metadata, final-at-merge status
values, and explicit supersession relationships so teams preserve institutional
knowledge without merge-order choreography or post-merge status edits.

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

## Mississippi Publication Contract

Mississippi publishes ADRs from `docs/Docusaurus/docs/adr/` under an explicit
governance contract:

| Field or Rule | Mississippi Contract |
|---------------|----------------------|
| Canonical identity | Frontmatter `id` is canonical and immutable after merge |
| New filename pattern | `YYYY-MM-DD-title-slug--HHmmssSSS[-NN].md` |
| Published route | Frontmatter `slug` is `/adr/<filename-stem>` |
| Ordering | `sidebar_position = unixEpochMilliseconds(created_at_utc) * 100 + disambiguator` |
| Published statuses | `accepted`, `rejected`, `deprecated` |
| Supersession | Reciprocal `supersedes` and `superseded_by` metadata |
| Legacy alias bridge | `legacy_refs` preserves historical `ADR-NNNN` references when legacy ADRs are backfilled |

The filename is never the canonical identifier. Human prose references should
prefer linked `ADR YYYY-MM-DD: Title` text. If a decision changes later, a new
ADR supersedes the earlier one and the older ADR receives only the bounded
metadata update needed to reflect that relationship.

Published routes are also explicit. New Mississippi ADRs set frontmatter
`slug` to `/adr/<filename-stem>`, which keeps the public URL human-readable and
prevents Docusaurus from falling back to the canonical `id` in the route.

That contract is the live Mississippi authoring model for new ADRs. The
historical Nygard and adr-tools examples later in this document are background
context for the broader ADR ecosystem, not alternate Mississippi instructions.

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

### The Nygard Template (Minimal Historical Baseline)

Michael Nygard's original template is deliberately minimal:

The example below is a historical baseline used by older ADR toolchains. In
Mississippi, this numbered heading and inline lifecycle text are legacy
compatibility context, not the live authoring contract for new ADRs.

```markdown
# Title of Decision

## Status

Accepted | Proposed | Deprecated | Superseded by another ADR

## Context

What is the issue that we're seeing that is motivating this decision or change?

## Decision

What is the change that we're proposing and/or doing?

## Consequences

What becomes easier or more difficult to do because of this change?
```

This four-section format - Status, Context, Decision, Consequences - is the
most widely adopted because it is simple enough that teams actually write ADRs,
and structured enough that the records are useful.

Mississippi treats this as background context, not as the exact repository
publication contract. New Mississippi ADRs are published with frontmatter
identity and final-at-merge statuses instead of a mutable in-body status flow.

### The MADR Template (Markdown Any Decision Records)

**MADR** (Markdown Any Decision Records), created by **Oliver Kopp** and
maintained as an open-source project, extends Nygard's template with
additional sections for teams that want more rigour:

This generic example is useful for understanding the wider MADR ecosystem, but
Mississippi does not treat the numbered heading or inline mutable status text
below as the repository's current publication model.

```markdown
# Title in the Form of a Short Noun Phrase

## Status

Proposed | Accepted | Deprecated | Superseded by another ADR

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

Mississippi uses MADR 4.0.0 as the body template while layering repository
specific frontmatter and lifecycle rules on top of it. The required sections
remain `Context and Problem Statement`, `Considered Options`, and
`Decision Outcome`, but the merged record carries its publication contract in
frontmatter rather than in a mutable `Status` section.

### Choosing a Template

| Factor | Nygard (Minimal) | MADR (Extended) |
|--------|-----------------|-----------------|
| **Adoption friction** | Very low — four sections | Moderate — more sections to fill |
| **Alternatives documentation** | Implicit in Context | Explicit section with pros/cons |
| **Decision drivers** | Implicit | Explicit enumerated list |
| **Team maturity** | Good for teams starting out | Good for teams with ADR practice |
| **Traceability** | Basic status field | Links and cross-references |

The best template is the one the team will actually use. For a greenfield
repository adopting ADRs from scratch, Nygard's minimal format is a reasonable
starting point. In Mississippi, the live repository contract is MADR 4.0.0 body
structure plus repository-specific frontmatter in `docs/Docusaurus/docs/adr/`.
A short ADR that exists is infinitely more valuable than a comprehensive
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

Mississippi publishes ADRs under `docs/Docusaurus/docs/adr/`. The repository
now keeps a mixed corpus during migration: legacy ADRs may still use sequential
filenames, while new ADRs use a merge-safe timestamped filename and immutable
frontmatter identity.

### Naming Convention in Mississippi

New Mississippi ADRs use a human-readable filename plus a canonical frontmatter
identifier:

```text
YYYY-MM-DD-short-descriptive-title--HHmmssSSS[-NN].md
```

- **YYYY-MM-DD**: UTC calendar date for human discovery.
- **short-descriptive-title**: Lowercase, hyphen-separated summary of the
  decision.
- **HHmmssSSS[-NN]**: UTC time plus an optional disambiguator for collisions.

The filename is not the canonical identity. Mississippi stores that identity in
frontmatter `id`, using the format `adr-YYYYMMDDTHHmmssSSSZ-NN`. This keeps
cross-reference stability separate from filenames and avoids renumbering work
when multiple ADR pull requests merge in parallel.

Mississippi also stores the published route in frontmatter `slug`, derived from
the filename stem. For a file named
`2026-03-25-redesign-adr-governance-publication-model--215831956.md`, the
published route is `/adr/2026-03-25-redesign-adr-governance-publication-model--215831956`.
This keeps routes readable and deterministic without making them canonical.

### Linking Between ADRs

ADRs frequently reference each other. Use relative Markdown links. In
Mississippi prose, prefer linked title-and-date references:

```markdown
[ADR 2026-03-25: Redesign ADR Governance Publication Model](./2026-03-25-redesign-adr-governance-publication-model--215831956.md)
```

When an ADR supersedes another in Mississippi, both records should be updated
through metadata, not by rewriting historical body content:

- The **new** ADR adds a `supersedes` entry with the target ADR `id` and
  relative `path`.
- The **original** ADR gains the matching `superseded_by` entry while keeping
  its original final `status`.
- Historical `ADR-NNNN` references may remain inside untouched legacy records
  until a deliberate metadata backfill touches them.

---

## Key-Line 4: The ADR Lifecycle

### Published Status Values

ADRs have a defined lifecycle, but Mississippi publishes only final states to
`main`:

```text
Pull request discussion and draft iteration
          |
          v
Merged ADR publishes one final status:
- accepted
- rejected
- deprecated

Later change in direction:
- publish a new ADR
- add reciprocal supersession metadata
```

| Status | Meaning |
|--------|---------|
| **Accepted** | The merged ADR records the adopted decision and remains the historical record for that choice. |
| **Rejected** | The merged ADR records an option the team intentionally declined and kept for future context. |
| **Deprecated** | The merged ADR is published as historical guidance that is no longer the recommended active decision. |

`Superseded` is not a published status value in Mississippi. A later ADR can
supersede an earlier one, but that relationship is expressed through
`supersedes` and `superseded_by` metadata rather than by teaching a separate
status transition model.

### The Immutability Rule

Mississippi does not publish `proposed` ADRs to `main`; discussion happens in
the pull request, and the merged ADR already carries its final status. Once an
ADR reaches `main`, its `status` is already final for that record, and its
Context and Problem Statement plus Decision Outcome sections are not modified.
If understanding improves or circumstances change:

1. **Minor clarifications** (typos, formatting) are acceptable.
2. **Substantive changes** to the decision require a new ADR that supersedes
  the original.
3. **Supersession updates** on the older ADR are metadata-only changes such as
  `superseded_by`; they do not rewrite the historical body.

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
ADRs.

The example below reflects the older sequential-number ADR ecosystem. It is
useful background if you maintain repositories that still follow that model,
but it is not Mississippi's current publication contract for new ADRs:

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

Mississippi does not currently use `adr init docs/decisions`, repository-wide
sequential numbering, or `adr new -s 3` as its live governance workflow. New
Mississippi ADRs are authored in `docs/Docusaurus/docs/adr/` with timestamped
filenames, immutable frontmatter `id`, explicit frontmatter `slug`, and
reciprocal supersession metadata.

### Log4brains

**Log4brains** is a more modern tool that generates a searchable, static
website from ADR Markdown files:

- Supports MADR template out of the box.
- Generates a browsable knowledge base with timeline and search.
- Integrates with CI/CD to publish the ADR site automatically.

Repository: <https://github.com/thomvaill/log4brains>

### CI Integration

ADRs can be validated in CI pipelines once a repository chooses to wire those
checks into its enforcement boundary. For Mississippi, the list below describes
the target validation surface for the redesigned governance model rather than a
claim that every check has already landed in every branch:

| Check | Purpose |
|-------|---------|
| **Canonical identity** | Every ADR has the correct immutable `id` contract |
| **Published route** | Every new ADR `slug` matches the filename stem instead of defaulting to the canonical `id` |
| **Derived ordering** | `sidebar_position` matches `created_at_utc` and the disambiguator |
| **Supersession integrity** | Reciprocal metadata points to valid ADR targets without cycles |
| **Legacy backfill boundary** | Legacy governance edits stay metadata-only and within the allow-list |
| **Markdown lint** | ADR files pass the same lint rules as other docs |
| **Template compliance** | Required sections are present |

### GitHub Pull Request Workflow

The recommended workflow for ADR adoption:

1. Author creates the ADR as a Markdown file in a feature branch.
2. The PR description summarises the decision and links to the ADR file.
3. Team members review the ADR in the PR — comments, alternatives, and
   consequences are discussed inline.
4. Once approved, the ADR is merged with its final publication status already
  set to `accepted`, `rejected`, or `deprecated`.
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

For teams adopting ADRs generically, start with:

1. **One template** — Nygard's four-section format.
2. **One directory** — `docs/decisions/` in the main repository.
3. **One rule** — Any decision that changes system structure or is hard to
   reverse gets an ADR.
4. **One workflow** — ADRs are created in PRs and reviewed like code.

For Mississippi contributors, do not apply that generic baseline literally.
Start with the published Mississippi contract instead:

1. **One template** — MADR 4.0.0 body sections plus Mississippi frontmatter.
2. **One directory** — `docs/Docusaurus/docs/adr/`.
3. **One example** — the published governance ADR linked from the ADR overview.
4. **One workflow** — create the ADR in a PR and choose its final merged status before review closes.

### Building the Habit

| Practice | Effect |
|----------|--------|
| **Write retrospective ADRs** for existing decisions | Builds the initial corpus and demonstrates value |
| **Reference ADRs in PRs** | "This implements ADR 2026-03-25: Title" connects code to decisions without relying on sequence allocation |
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
- **Canonical identity lives in frontmatter** - filenames stay readable, but
  `id` owns machine-readable identity.
- **Published routes stay readable** - `slug` mirrors the filename stem so the
  public URL does not degrade to the canonical `id`.
- **Merged ADRs are final** - changes produce new ADRs that supersede the
  original instead of post-merge status flips.
- **Short and honest** — 1–2 pages with explicit trade-offs and negative
  consequences.
- **Use one clear authoring contract** — in Mississippi, MADR 4.0.0 plus
  repository frontmatter in `docs/Docusaurus/docs/adr/`; in other repositories,
  Nygard's minimal template remains a reasonable baseline.
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
