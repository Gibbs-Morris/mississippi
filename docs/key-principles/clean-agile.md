# Clean Agile: Back to Basics

Robert C. Martin (Uncle Bob) published *Clean Agile: Back to Basics* in 2019 to
reclaim the original intent of the Agile Manifesto — a small, disciplined set of
practices designed to help small teams deliver software well. Over the two
decades since the Manifesto's signing in 2001, "Agile" had been diluted by
certification industries, process frameworks, and management consultancies into
something the original authors would barely recognise. Clean Agile is Martin's
attempt to articulate what Agile was always meant to be.

**This document is written in Minto Pyramid format. It is written with the
future use case in mind: serving as a research base when designing what a good
software team process should look like. Accordingly, it covers the human and
organisational dimensions of Agile, not merely the technical or coding
dimension.**

---

## Governing Thought

Clean Agile, as articulated by Robert C. Martin, is a return to the original
values and practices of the Agile Manifesto: a small set of disciplines that
help small teams produce working software in short cycles, with continuous
feedback, sustainable pace, and shared ownership — treating Agile as a
human-centred discipline for managing uncertainty, not a process framework to be
scaled or certified.

---

## Situation

In February 2001, seventeen software practitioners met at the Snowbird ski
resort in Utah and produced the **Manifesto for Agile Software Development**.
The four values and twelve principles they articulated were a reaction against
heavyweight, document-driven processes (particularly the Rational Unified Process
and waterfall methodologies) that had dominated the industry.

The original signatories included Kent Beck, Martin Fowler, Ron Jeffries, Ward
Cunningham, Alistair Cockburn, Robert C. Martin, and eleven others. What they
agreed on was surprisingly modest:

> "We are uncovering better ways of developing software by doing it and helping
> others do it. Through this work we have come to value:
>
> - **Individuals and interactions** over processes and tools
> - **Working software** over comprehensive documentation
> - **Customer collaboration** over contract negotiation
> - **Responding to change** over following a plan
>
> That is, while there is value in the items on the right, we value the items
> on the left more."
>
> — Manifesto for Agile Software Development (2001)

## Complication

By the time Martin wrote Clean Agile in 2019, the word "Agile" had been
captured by a certification and consultancy industry that bore little
resemblance to the Snowbird intent:

- **SAFe (Scaled Agile Framework)** and similar scaling frameworks had turned
  Agile into a management control structure with dozens of roles, ceremonies,
  and artefacts.
- **Certification programmes** (CSM, SAFe Agilist, etc.) had commoditised Agile
  into a two-day course and a multiple-choice exam.
- **"Agile" became a brand**, applied to everything from project management
  methodologies to HR practices, diluting its meaning to near-zero.
- **The technical disciplines** — TDD, refactoring, pair programming, simple
  design — had been quietly dropped by organisations that wanted the label
  without the practices.
- **Management adopted the ceremonies** (standups, sprints, retrospectives)
  without the underlying values, creating what Martin calls "Flaccid Agile" —
  process without substance.

Martin's central argument: Agile is not a process framework. It is not
something you buy, certify, or scale. It is a small set of disciplines for
small teams, and if you do not practice the technical disciplines, you are not
doing Agile no matter how many standups you hold.

## Question

What are the original values, practices, and disciplines of Agile as the
Manifesto authors intended them — and how should they be applied to build a
good software team process that respects both the human and technical
dimensions?

---

## Key-Line 1: The Circle of Life — The Iron Cross

### The Fundamental Constraint

Every software project operates under four constraints:

1. **Scope** — How much will be built?
2. **Schedule** — How much time is available?
3. **Quality** — How good must it be?
4. **Staff** — How many people are available?

Martin calls this the **Iron Cross of project management**. The constraints
are interdependent: changing one affects the others.

### The Waterfall Failure Mode

In waterfall projects, all four constraints are fixed at the start. When
reality diverges from the plan (as it always does), the only variable that
yields is **quality** — teams cut corners, skip tests, accumulate technical
debt, and ship defects. This is not a conscious decision; it is the
inevitable consequence of fixing three constraints and pretending the fourth
is also fixed.

### The Agile Response

Agile makes **scope** the variable. Schedule, quality, and staff are held
steady. As the team learns more about the problem and the technology, scope is
adjusted — features are added, removed, re-prioritised, or re-sized based on
real data from completed iterations.

> "Agile is about knowing, as early as possible, just how screwed we are."
>
> — Robert C. Martin, *Clean Agile* (2019)

This is not defeatism. It is the recognition that **software projects are
exercises in managing uncertainty**, and the only honest approach is to measure
reality frequently and adjust scope accordingly.

### The Circle of Life

Martin introduces the **Circle of Life** as the visualisation of healthy
Agile:

```text
        Analyse
       ╱       ╲
      ╱         ╲
  Specify     Design
      ╲         ╱
       ╲       ╱
     Implement & Test
```

All four activities happen simultaneously, continuously, within each iteration.
There is no "analysis phase" followed by a "design phase". In every
iteration, the team analyses, specifies, designs, implements, and tests.

---

## Key-Line 2: The Practices — What Teams Actually Do

### Iteration (The Heartbeat)

The fundamental practice is the **iteration** (typically one or two weeks):

- At the start, the team selects work from the backlog.
- During the iteration, the team analyses, designs, implements, and tests.
- At the end, the team demonstrates **working software**.
- Stakeholders provide feedback.
- The team adjusts plans for the next iteration.

The iteration is the **heartbeat** of the project. It provides regular
measurement points. After a few iterations, the team knows its **velocity** —
how much work it can complete per iteration. This data replaces guessing.

### Planning

**Iteration Planning Meeting (IPM)**:

- The team estimates stories using relative sizing (story points).
- Stories are selected based on priority and team velocity.
- The team does not commit to more than velocity indicates they can deliver.

**The key insight about estimation**: Estimates are not promises. They are
probabilistic forecasts based on historical data. Martin emphasises that good
estimation requires:

1. Breaking stories into small, similarly-sized pieces.
2. Using the team's measured velocity as the primary planning input.
3. Accepting that estimates will be wrong and planning for that uncertainty.

### Acceptance Tests

**Acceptance tests define "done"**. They are:

- Written before or alongside development (not after).
- Expressed in business terms (not implementation terms).
- Automated wherever possible.
- The **unambiguous specification** of the story.

> "The purpose of acceptance tests is to formally define the requirements of
> the system in terms that are actionable and verifiable."
>
> — Robert C. Martin, *Clean Agile* (2019)

Martin argues that acceptance tests are not QA's job — they are a
collaboration between the customer (Product Owner) and the team. The tests
**are** the requirements, expressed in a form that can be run.

### Whole Team

The "whole team" practice means:

- Developers, testers, and business people sit together (physically or
  virtually).
- There is no handoff between "dev" and "QA" — the team owns quality
  collectively.
- Business people are available for questions throughout the iteration, not
  just at the start and end.
- Everyone participates in planning, estimation, and retrospectives.

### Continuous Integration

Martin defines continuous integration strictly:

- Developers integrate their work multiple times per day.
- Every integration triggers an automated build and test run.
- A broken build is the team's highest-priority issue — it is fixed before
  any new work begins.
- The integration branch (main, trunk) is always in a releasable state.

This is not "merge to main once a sprint". It is continuous, daily, multiple
times per day.

### Sustainable Pace

> "Software development is a marathon, not a sprint."
>
> — Robert C. Martin, *Clean Agile* (2019)

The team works at a pace they can sustain indefinitely. Overtime is a sign of
poor planning, not dedication. Martin argues that sustainable pace is not a
luxury — it is a prerequisite for quality. Tired developers make mistakes,
and those mistakes compound.

---

## Key-Line 3: The Technical Practices — Non-Negotiable

Martin is unequivocal: **without the technical practices, Agile does not
work**. The process practices (iterations, planning, retrospectives) are
necessary but not sufficient.

### Test-Driven Development (TDD)

The discipline of writing a failing test before writing the code that makes it
pass:

1. **Red**: Write a test that fails (for the behaviour you are about to
   implement).
2. **Green**: Write the minimum code to make the test pass.
3. **Refactor**: Clean up the code while keeping the tests green.

Martin's argument for TDD is not about testing per se — it is about **design**.
Code written to be testable is necessarily modular, decoupled, and
well-structured. TDD produces better design as a side effect of better
discipline.

> "The act of writing a test first is an act of design, not an act of
> testing."
>
> — Robert C. Martin, *Clean Agile* (2019)

### Refactoring

Refactoring is the practice of continuously improving the structure of the code
without changing its behaviour. Martin argues that refactoring is not optional
and not a separate task — it is part of every development cycle:

- After making a test pass, refactor before moving on.
- The codebase should be cleaner at the end of each iteration than at the
  start.
- Technical debt is repaid continuously, not in dedicated "tech debt sprints".

### Simple Design

Kent Beck's four rules of simple design (in priority order):

1. **Passes all the tests**
2. **Reveals intention** (clear, readable)
3. **No duplication** (DRY)
4. **Fewest elements** (no unnecessary complexity)

Martin endorses these rules as the design standard for Agile teams. The goal
is the simplest design that works, not the most elegant or the most
future-proof.

### Pair Programming

Two developers working at one workstation:

- **Driver**: Types the code.
- **Navigator**: Reviews, thinks ahead, catches errors.
- Partners switch roles frequently.

Martin acknowledges that pair programming is the most culturally controversial
practice. His position: it is not mandatory for all work, but it is the most
effective way to share knowledge, catch defects early, and maintain collective
code ownership. Teams should try it before dismissing it.

---

## Key-Line 4: The Human and Organisational Dimension

### The Role of the Customer (Product Owner)

The customer is not someone who writes requirements and disappears. In Clean
Agile, the customer:

- **Prioritises the backlog** — deciding what is most valuable to build next.
- **Is available** during the iteration for questions and clarifications.
- **Provides feedback** at the end of each iteration.
- **Adjusts scope** based on the team's velocity and changing business needs.

The customer-developer relationship is **collaborative, not contractual**.
Martin emphasises that this requires trust on both sides: the customer trusts
the team's estimates and velocity data, and the team trusts the customer's
priorities.

### The Role of Management

Martin's view of management in Agile is specific:

- **Managers do not assign tasks** — the team self-organises.
- **Managers do not dictate process** — the team chooses its practices.
- **Managers provide resources, remove obstacles, and set direction** — the
  "what" and "why", not the "how".
- **Managers protect sustainable pace** — they do not demand overtime.

> "Management's job is to manage the project, not manage the programmers."
>
> — Robert C. Martin, *Clean Agile* (2019)

This is a significant cultural shift. In traditional organisations, management
controls the process and measures individuals. In Agile, management provides
context and measures outcomes.

### Collective Ownership

No individual owns a module, service, or layer. The whole team owns the whole
codebase:

- **Any developer can modify any code** (with appropriate review).
- **Knowledge silos are actively broken** through pair programming and
  rotation.
- **Bus factor** (the number of people who, if hit by a bus, would stall the
  project) should be as high as possible.

### Small Teams

Martin is explicit that Agile was designed for small teams:

- **5–12 people** is the ideal range.
- Communication overhead grows quadratically with team size.
- If the project needs more people, the answer is multiple small teams with
  clear interfaces, not one large team.

This has direct implications for organisational design: scaling is achieved
through team topology, not by adding bodies to a single team.

### Trust and Courage

Two of the less-discussed Extreme Programming values that Martin highlights:

- **Courage**: The willingness to do the right thing even when it is hard —
  refactoring messy code, pushing back on unrealistic deadlines, admitting
  when an approach is not working.
- **Trust**: Management trusts the team's estimates. The team trusts
  management's priorities. Both sides trust the data (velocity, defect rates,
  cycle time) over opinions.

Without trust and courage, the other practices collapse:

- Without courage, teams accept technical debt rather than refactoring.
- Without trust, management adds process overhead rather than letting teams
  self-organise.
- Without both, estimates become promises and sustainable pace becomes
  permanent overtime.

---

## Key-Line 5: What Agile Is Not

Martin devotes significant space to debunking distortions of Agile:

### Not a Process Framework

Agile is not SAFe, LeSS, Nexus, or any other scaling framework. These
frameworks may contain useful ideas, but they are not Agile. Agile is a set of
values and practices for small teams.

### Not a Certification

No two-day course makes someone "Agile". Agile is learned by practice, not by
examination. Martin is openly critical of the certification industry.

### Not About Velocity Metrics

Velocity is a **planning tool**, not a **performance metric**:

- Velocity measures how much the team can do per iteration.
- It is used to plan future iterations.
- It is **not** used to compare teams, evaluate individuals, or justify
  layoffs.
- When velocity becomes a target, it ceases to be a useful measure (Goodhart's
  Law).

### Not "No Documentation"

The Manifesto says "working software over comprehensive documentation". It does
not say "no documentation". Martin argues for just enough documentation:

- Acceptance tests are the primary specification.
- Architecture decisions should be documented.
- Operational runbooks are essential.
- The documentation that is written should be maintained, not abandoned.

### Not "No Planning"

The Manifesto says "responding to change over following a plan". It does not
say "no plan". Martin argues for continuous planning:

- Plan the next iteration in detail.
- Plan the next few iterations at a higher level.
- Plan the release at a strategic level.
- Replan continuously based on velocity and feedback.

---

## Key-Line 6: Designing a Good Team Process — Synthesis

This section synthesises Martin's principles into design criteria for a good
software team process.

### The Non-Negotiable Elements

Any good team process must include:

| Element | Why |
|---|---|
| **Short iterations** (1–2 weeks) | Frequent measurement and feedback |
| **Working software at the end of each iteration** | The only honest progress measure |
| **Automated tests at all levels** | Quality built in, not inspected in |
| **TDD or test-first discipline** | Design quality, not just test coverage |
| **Continuous integration** | Always releasable; broken builds fixed immediately |
| **Collaborative planning with the customer** | Scope is adjustable; priorities are real |
| **Sustainable pace** | Quality degrades under fatigue |
| **Retrospectives** | The process itself improves iteratively |

### The Human Non-Negotiables

| Element | Why |
|---|---|
| **Small teams** (5–12) | Communication overhead is manageable |
| **Whole team** (dev, QA, product sit together) | No handoffs; shared context |
| **Collective ownership** | No knowledge silos; high bus factor |
| **Management provides context, not control** | Self-organising teams make better local decisions |
| **Trust-based relationships** | Data over politics; honesty over optimism |
| **Courage to do the right thing** | Refactor, push back, admit mistakes |

### The Warning Signs

If a team process has these characteristics, it is not Agile regardless of what
it is called:

| Warning Sign | What It Means |
|---|---|
| Iterations without working software | Process theatre |
| Velocity used as a performance metric | Management co-opting a planning tool |
| QA as a separate phase after development | Waterfall in Agile clothing |
| Technical practices dropped | "Flaccid Agile" — ceremonies without substance |
| No retrospectives or retrospectives without action | The process cannot improve |
| Managers assigning tasks to individuals | Command-and-control, not self-organisation |
| Permanent overtime | Unsustainable; quality will degrade |
| "Agile transformation" led by consultants without practitioner involvement | Process-driven, not value-driven |

---

## Key-Line 7: The Twelve Principles — Annotated

The Agile Manifesto's twelve principles, with Martin's interpretation:

| # | Principle | Martin's Commentary |
|---|---|---|
| 1 | Satisfy the customer through early and continuous delivery | Deliver working software every iteration; do not wait for "the big release" |
| 2 | Welcome changing requirements, even late | Scope is the variable; the team adjusts |
| 3 | Deliver working software frequently (weeks, not months) | The iteration is the heartbeat |
| 4 | Business people and developers work together daily | The customer is part of the team, not a stakeholder at arm's length |
| 5 | Build projects around motivated individuals; trust them | Self-organisation; management provides context, not control |
| 6 | Face-to-face conversation is the most efficient communication | Co-location or high-bandwidth virtual presence |
| 7 | Working software is the primary measure of progress | Not plans, not documents, not story points — working software |
| 8 | Sustainable pace | No overtime; fatigue destroys quality |
| 9 | Continuous attention to technical excellence | TDD, refactoring, simple design — the technical practices are non-negotiable |
| 10 | Simplicity — maximise the work not done | Do less; build only what is needed |
| 11 | Self-organising teams produce the best results | The team decides how to do the work |
| 12 | Reflect and adjust at regular intervals | Retrospectives with real action items |

---

## Common Pitfalls

| Pitfall | Description |
|---|---|
| **Flaccid Agile** | Adopting ceremonies (standups, sprints) without technical practices (TDD, CI, refactoring) |
| **Certification over practice** | Believing that a CSM or SAFe certificate means someone understands Agile |
| **Velocity as target** | Using velocity to measure performance rather than to plan iterations |
| **Scaling before mastering** | Adopting a scaling framework before a single team can deliver well |
| **Big-bang transformation** | Attempting to make the entire organisation "Agile" overnight |
| **Dropping the customer** | Product Owner writes stories and disappears; team works in isolation |
| **Process without trust** | Adding more process to compensate for lack of trust between management and teams |

---

## Authoritative Sources

| Source | Reference |
|---|---|
| **Martin, R.C.** | *Clean Agile: Back to Basics* (2019). Prentice Hall. The primary source for this document. |
| **Beck, K. et al.** | *Manifesto for Agile Software Development* (2001). <https://agilemanifesto.org/> |
| **Beck, K.** | *Extreme Programming Explained: Embrace Change* (1999). Addison-Wesley. The origin of XP practices. |
| **Beck, K.** | *Test Driven Development: By Example* (2002). Addison-Wesley. TDD discipline. |
| **Schwaber, K. and Sutherland, J.** | *The Scrum Guide* (2020). <https://scrumguides.org/> |
| **Martin, R.C.** | *The Clean Coder: A Code of Conduct for Professional Programmers* (2011). Prentice Hall. Professional discipline and estimates. |
| **Fowler, M.** | *Refactoring: Improving the Design of Existing Code* (1999; 2nd ed. 2018). Addison-Wesley. |

---

## Summary

Clean Agile, as articulated by Robert C. Martin, reclaims the original intent
of the 2001 Agile Manifesto. It is a small set of disciplines for small teams:
short iterations producing working software, collaborative planning with the
customer, automated testing at all levels, TDD as a design discipline,
continuous integration, sustainable pace, and regular retrospectives. The human
dimension is equally essential: small teams with collective ownership,
management that provides context rather than control, trust-based relationships,
and the courage to do the right thing. Martin explicitly rejects the
certification industry, scaling frameworks, and the practice of adopting
ceremonies without technical disciplines ("Flaccid Agile"). A good team process
must include both the human and technical elements — neither alone is
sufficient.
