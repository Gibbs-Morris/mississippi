# Agile Software Development Life Cycle

Traditional software development life cycles treat quality assurance as a
late-stage gate — code is written, then tested, then fixed, then retested. This
sequential handoff model creates long feedback loops, late-discovered defects,
and adversarial relationships between developers and testers. An agile SDLC
restructures the entire flow so that quality is built in from the start, QA
participates from day one, and feedback loops are measured in hours, not weeks.

**This document is written in Minto Pyramid format.**

---

## Governing Thought

An agile software development life cycle integrates quality assurance into every
phase of development — from story refinement through deployment — rather than
treating it as a late-stage inspection. This produces shorter feedback loops,
fewer defects in production, and a shared ownership model where the whole team
is responsible for quality.

---

## Situation

Software teams need a repeatable process for turning requirements into working,
tested, deployable software. The traditional waterfall SDLC defines sequential
phases: requirements, design, implementation, testing, deployment, and
maintenance. Each phase completes before the next begins.

## Complication

Sequential handoffs create problems that compound over time:

- **Late defect discovery**: Bugs found in the testing phase are expensive to
  fix because the code has moved far from the developer's working memory.
- **QA as adversary**: When testers are brought in late, they become the
  bearers of bad news. This creates friction rather than collaboration.
- **Requirements drift**: By the time testing begins, requirements may have
  changed, but the code reflects the original understanding.
- **Big-batch risk**: Large increments of work are integrated and tested
  together, making it difficult to isolate which change introduced a defect.

## Question

How should a modern agile squad structure its development life cycle so that
quality is a continuous activity, QA participates from the beginning, and
feedback loops remain short enough to catch problems before they compound?

---

## Key-Line 1: The Three Amigos — QA at the Beginning

### What the Three Amigos Practice Is

Before any story enters development, three perspectives meet to discuss it:

1. **Product** (Product Owner, Business Analyst) — What does the user need?
   What is the acceptance criteria?
2. **Development** (Developer) — How will this be built? What are the technical
   risks? What are the edge cases?
3. **Quality** (QA Engineer, Tester) — How will this be tested? What could go
   wrong? What are the boundary conditions?

This meeting is called a **Three Amigos session** (sometimes called a
**specification workshop** or **example mapping session**).

### Why QA Must Be Present from the Start

| Without Early QA | With Early QA |
|---|---|
| Test cases written after code is done | Test scenarios defined before coding starts |
| Edge cases discovered during testing | Edge cases discovered during refinement |
| Developers build "happy path" and hope | Developers build with failure modes in mind |
| Rework after testing | Prevention before coding |
| QA feels excluded and adversarial | QA is a collaborative partner |

### What the Three Amigos Session Produces

- **Refined acceptance criteria** with explicit examples (Given-When-Then or
  equivalent)
- **Identified edge cases and error scenarios**
- **Agreed test strategy** (what will be automated, what needs manual
  exploration, what needs infrastructure)
- **Shared understanding** — all three perspectives agree on what "done" means

### Example Mapping (Concrete Technique)

**Example Mapping**, created by Matt Wynne of Cucumber, is a structured format
for Three Amigos sessions:

```text
┌─────────────────────────────────────────────────┐
│  STORY (yellow card)                            │
│  "As a user, I can reset my password"           │
├─────────────────────────────────────────────────┤
│  RULE (blue card)                RULE           │
│  "Must use existing email"       "Link expires  │
│                                   in 24 hours"  │
├─────────────────────────────────────────────────┤
│  EXAMPLE      EXAMPLE           EXAMPLE         │
│  (green)      (green)           (green)         │
│  "Valid email" "Unknown email"  "Expired link"  │
├─────────────────────────────────────────────────┤
│  QUESTION (red card)                            │
│  "What happens if user has multiple emails?"    │
└─────────────────────────────────────────────────┘
```

- **Yellow**: The story
- **Blue**: Business rules
- **Green**: Concrete examples (become test cases)
- **Red**: Open questions (must be resolved before development starts)

---

## Key-Line 2: The Test Pyramid in Agile SDLC

### Mike Cohn's Test Pyramid

The **test pyramid**, introduced by **Mike Cohn** in *Succeeding with Agile*
(2009), defines the ratio and speed of different test types:

```text
        ╱╲
       ╱  ╲        UI / E2E Tests
      ╱    ╲       (few, slow, brittle)
     ╱──────╲
    ╱        ╲     Integration Tests
   ╱          ╲    (moderate number, medium speed)
  ╱────────────╲
 ╱              ╲   Unit Tests
╱                ╲  (many, fast, stable)
──────────────────
```

| Level | Quantity | Speed | Stability | Feedback Loop |
|---|---|---|---|---|
| Unit tests | Many (hundreds–thousands) | Milliseconds | Very stable | Seconds |
| Integration tests | Moderate (tens–hundreds) | Seconds | Moderately stable | Minutes |
| UI / E2E tests | Few (tens) | Minutes | Brittle | Minutes–hours |

### Why the Pyramid Shape Matters

- **Fast feedback at the base**: Unit tests run in seconds and catch logic
  errors immediately. Developers get feedback while the code is still in
  working memory.
- **Confidence at the middle**: Integration tests verify that components work
  together correctly. They catch wiring and contract errors.
- **Validation at the top**: UI and E2E tests verify that the system behaves
  correctly from the user's perspective. They are expensive and brittle, so
  there should be few.

### The Anti-Pattern: The Ice-Cream Cone

The inverse of the pyramid — many E2E tests, few unit tests — is called the
**ice-cream cone** anti-pattern. It produces:

- Slow feedback (minutes to hours per test run)
- Flaky tests (UI tests break when CSS changes)
- Developer avoidance (nobody wants to fix flaky E2E tests)
- False confidence (passing E2E tests do not mean the logic is correct)

---

## Key-Line 3: The Agile SDLC Phases (Iterative, Not Sequential)

### Phase 1 — Backlog Refinement (Continuous)

- Product Owner maintains a prioritised backlog.
- Stories are refined in Three Amigos sessions before sprint planning.
- Each story has acceptance criteria with concrete examples.
- QA identifies test strategy during refinement, not after development.

### Phase 2 — Sprint Planning

- Team selects stories for the sprint based on priority and capacity.
- For each selected story, the team confirms:
  - Acceptance criteria are clear and testable.
  - Test strategy is agreed (unit, integration, manual exploration).
  - Technical approach is understood, including risks.
- QA begins writing test scenarios (automated and manual) immediately.

### Phase 3 — Development (with Continuous Testing)

Development and testing happen concurrently, not sequentially:

```text
┌────────────────────────────────────────────────────┐
│  Developer                    QA Engineer           │
│  ─────────                    ───────────           │
│  Write failing test           Write acceptance tests│
│  Write implementation         Exploratory testing   │
│  Pass unit tests              Pair with developer   │
│  Code review                  Review test coverage  │
│  Integration tests            Automation            │
│  ◄────── Daily pairing and feedback ──────►        │
└────────────────────────────────────────────────────┘
```

Key practices during development:

- **Test-Driven Development (TDD)**: Write the test first, then the code, then
  refactor. The test defines the expected behaviour before implementation.
- **Pair programming / mob programming**: QA pairs with developers on complex
  stories to catch issues in real time.
- **Continuous integration**: Every commit triggers an automated build and test
  run. Failures are fixed immediately.
- **Definition of Done includes testing**: A story is not "dev complete" until
  tests pass. There is no handoff to QA — QA has been involved throughout.

### Phase 4 — Review and Demo

- The team demonstrates working software to stakeholders.
- QA presents test coverage and any exploratory testing findings.
- Feedback is incorporated into the backlog for future sprints.

### Phase 5 — Retrospective

- The team reflects on what worked and what did not.
- Testing process is included in the retrospective: Was the test strategy
  effective? Were defects found early enough? Was QA involved early enough?

### Phase 6 — Release and Deploy

- Deployment is automated (CI/CD pipeline).
- Release decisions are decoupled from deployment (feature flags, canary
  releases).
- Monitoring and alerting detect issues in production.
- Post-deployment smoke tests verify critical paths.

---

## Key-Line 4: Concrete Testing Practices by Phase

### During Refinement

| Practice | Who | Output |
|---|---|---|
| Example Mapping | Product + Dev + QA | Concrete examples, rules, questions |
| Risk Assessment | QA + Dev | List of high-risk areas needing deeper testing |
| Testability Review | Dev + QA | Identified areas needing test hooks or infrastructure |

### During Development

| Practice | Who | Output |
|---|---|---|
| TDD (Test-Driven Development) | Dev | Unit tests before code |
| BDD (Behaviour-Driven Development) | QA + Dev | Executable specifications (Given-When-Then) |
| Code Review with Test Review | Dev + Dev/QA | Verified test coverage and quality |
| Pair Testing | Dev + QA | Real-time defect discovery during development |

### During Integration

| Practice | Who | Output |
|---|---|---|
| CI Pipeline | Automated | Build + unit + integration tests on every commit |
| Contract Testing | Dev + QA | Verified API contracts between services |
| Performance Testing | QA + Dev | Baseline performance metrics and regression detection |

### Before Release

| Practice | Who | Output |
|---|---|---|
| Exploratory Testing | QA | Unscripted investigation of areas not covered by automation |
| Regression Suite | Automated | Full automated regression pass |
| Smoke Tests | Automated | Critical-path verification post-deployment |

---

## Key-Line 5: Shift-Left Testing

### What Shift-Left Means

**Shift-left testing** means moving testing activities earlier in the
development life cycle. The term was coined by **Larry Smith** in 2001:

```text
Traditional:  Requirements → Design → Code → ██ TEST ██ → Deploy
Shift-left:   Requirements ██ TEST ██ Design ██ TEST ██ Code ██ TEST ██ Deploy
```

Every phase has testing activities. Testing is not a phase — it is a continuous
discipline.

### Shift-Left Practices

| Phase | Traditional Testing Activity | Shift-Left Testing Activity |
|---|---|---|
| Requirements | None | Review requirements for testability; write examples |
| Design | None | Review design for testability; identify integration points |
| Coding | None (code first, test later) | TDD — write test first, then code |
| Integration | Manual testing begins | Automated tests run on every commit |
| Pre-release | Full regression pass | Continuous regression; exploratory testing ongoing |

### The Economics of Shift-Left

Research by **Barry Boehm** (documented in *Software Engineering Economics*,
1981, and confirmed in subsequent studies) established that the cost of fixing a
defect increases exponentially the later it is found:

| Phase Found | Relative Cost to Fix |
|---|---|
| Requirements | 1x |
| Design | 3–6x |
| Coding | 10x |
| Testing | 15–40x |
| Production | 30–100x |

Shift-left testing is fundamentally an economic argument: find defects when they
are cheapest to fix.

---

## Key-Line 6: Definition of Done

### Why Definition of Done Matters

The **Definition of Done (DoD)** is a shared checklist that defines when a
piece of work is truly complete. Without it:

- "Done" means different things to different people.
- Testing, documentation, and code review are treated as optional extras.
- Technical debt accumulates because shortcuts are invisible.

### A Comprehensive Definition of Done

A well-structured DoD for an agile squad:

| Category | Criterion |
|---|---|
| **Code** | Code is written, reviewed, and merged to the main branch |
| **Unit Tests** | Unit tests written, passing, and covering the change |
| **Integration Tests** | Integration tests written for cross-component behaviour |
| **Acceptance Criteria** | All acceptance criteria from the story are met |
| **Exploratory Testing** | QA has performed exploratory testing |
| **Documentation** | Relevant documentation updated |
| **Build** | CI build passes with zero warnings |
| **Performance** | No performance regressions introduced |
| **Security** | No new security vulnerabilities introduced |
| **Accessibility** | Accessibility requirements met (if applicable) |
| **Release Notes** | Change documented for release notes (if user-facing) |

### The Key Insight

The DoD makes quality non-negotiable. A story cannot be moved to "done" until
every criterion is satisfied. This prevents the common failure mode where
stories are marked "done" with testing deferred to a later sprint.

---

## Common Pitfalls

| Pitfall | Description | Prevention |
|---|---|---|
| **QA as gatekeeper** | QA only sees code after development is "done" | Three Amigos from the start; QA pairs during development |
| **Testing as a phase** | Testing happens in a separate sprint or at the end | Shift-left; concurrent development and testing |
| **Ice-cream cone** | Mostly E2E tests, few unit tests | Enforce the test pyramid; measure test type ratios |
| **Flaky tests ignored** | Intermittent test failures are accepted as normal | Fix or remove flaky tests immediately; never normalise red builds |
| **DoD erosion** | Team relaxes the DoD under pressure | DoD is a team agreement; revisit in retrospectives but do not weaken |
| **Skipping refinement** | Stories enter development without clear acceptance criteria | No story enters a sprint without a Three Amigos session |

---

## Authoritative Sources

| Source | Reference |
|---|---|
| **Cohn, M.** | *Succeeding with Agile: Software Development Using Scrum* (2009). Addison-Wesley. The test pyramid. |
| **Wynne, M.** | *Introducing Example Mapping*. Cucumber Blog. <https://cucumber.io/blog/bdd/example-mapping-introduction/> |
| **Beck, K.** | *Test Driven Development: By Example* (2002). Addison-Wesley. The TDD discipline. |
| **Boehm, B.** | *Software Engineering Economics* (1981). Prentice-Hall. Cost-of-defect curves. |
| **Smith, L.** | *Shift-Left Testing* (2001). Dr. Dobb's Journal. The origin of the shift-left concept. |
| **Humble, J. and Farley, D.** | *Continuous Delivery: Reliable Software Releases through Build, Test, and Deployment Automation* (2010). Addison-Wesley. CI/CD pipeline practices. |
| **Marick, B.** | *Agile Testing Quadrants*. Referenced in Crispin, L. and Gregory, J., *Agile Testing* (2009). Addison-Wesley. |

---

## Summary

An agile SDLC integrates quality assurance into every development phase rather
than treating it as a late-stage gate. The Three Amigos practice brings QA into
story refinement from the start. The test pyramid structures automated testing
for fast feedback (many unit tests, fewer integration tests, very few E2E
tests). Shift-left testing moves testing activities earlier, exploiting the
economic reality that early defect detection is orders of magnitude cheaper than
late discovery. The Definition of Done makes quality non-negotiable by requiring
all testing, review, and documentation criteria to be satisfied before work is
considered complete. The result is shorter feedback loops, fewer production
defects, and a collaborative quality culture where the whole team owns quality.
