# Clean Squad: End-to-End SDLC Workflow

The Clean Squad is a family of 29 GitHub Copilot agents that takes an idea from
initial request through to a merge-ready pull request. There is exactly one
entry point — the **cs Product Owner** — who orchestrates all work by delegating
to specialist sub-agents. Every agent applies first-principles thinking and
chain-of-verification to every task.

## Foundational Principles (Embedded in Every Agent)

### Mandatory for All Agents (No Exceptions)

- **First Principles Thinking**: decompose problems to irreducible truths; never
  accept convention without examination; always ask why the question is being
  asked before solving it.
- **Chain of Verification (CoVe)**: every non-trivial claim follows the
  four-step process — draft, plan verification questions, answer independently,
  revise. Evidence over assumption.

### Applied Where Relevant

- **Clean Code**: meaningful names, small functions, single responsibility,
  DRY, command-query separation.
- **Clean Agile**: scope is the variable; continuous testing; technical
  disciplines are non-negotiable; sustainable pace.
- **Minto Pyramid**: answer first, then supporting arguments; SCQA framing
  for all significant communications.
- **Pull Request Best Practices**: single-responsibility PRs, self-review
  before submission, commit hygiene.
- **Architecture Decision Records**: immutable records of significant decisions
  with context, rationale, and consequences.
- **Mermaid Diagrams**: diagrams as code, version-controlled, diffable.
- **RFC 2119**: MUST, SHOULD, MAY for unambiguous obligations.

## Team Name

**Clean Squad** — reflecting Clean Agile and Clean Code principles.

## Entry Point

`@cs Product Owner` is the **only** agent the human user invokes. All other
agents are sub-agents invoked programmatically by the Product Owner or by
other sub-agents via `runSubagent`. The user never needs to invoke any other
agent directly.

## Shared State: The `.thinking` Folder

All agents share state through a filesystem folder:

```text
.thinking/
  <YYYY-MM-DD>-<task-slug>/        # One subfolder per task
    state.json                      # Workflow state (current phase, status)
    00-intake.md                    # Initial request & context
    01-discovery/
      questions-round-01.md         # First group of 5 questions + answers
      questions-round-02.md         # Next group + answers (adaptive)
      ...
      requirements-synthesis.md     # Synthesized requirements
    02-three-amigos/
      business-perspective.md       # Business Analyst output
      technical-perspective.md      # Tech Lead output
      qa-perspective.md             # QA Analyst output
      synthesis.md                  # Unified requirements
    03-architecture/
      solution-design.md            # Solution Architecture
      c4-context.md                 # C4 Context diagram
      c4-container.md               # C4 Container diagram
      c4-component.md               # C4 Component diagram (if needed)
      adrs/
        adr-001-<slug>.md           # Architecture Decision Records
    04-planning/
      draft-plan-v1.md              # Initial plan
      review-cycle-01/
        review-<persona>.md         # Each reviewer's feedback
        synthesis.md                # Synthesized feedback
      review-cycle-02/              # Second review cycle
        ...
      final-plan.md                 # Finalized plan
    05-implementation/
      increment-01/
        changes.md                  # What was implemented
        commit-review.md            # Commit-level review results
        test-results.md             # Test execution results
      increment-02/
        ...
    06-code-review/
      review-pedantic.md            # Pedantic reviewer output
      review-strategic.md           # Strategic reviewer output
      review-security.md            # Security reviewer output
      review-dx.md                  # DX reviewer output
      review-performance.md         # Performance reviewer output
      domain-experts/
        review-<domain>.md          # Domain expert reviews
      synthesis.md                  # Synthesized review findings
      remediation-log.md            # Fix tracking
    07-qa/
      test-strategy-review.md       # QA Lead review
      exploratory-findings.md       # Exploratory testing
      coverage-report.md            # Coverage analysis
      mutation-report.md            # Mutation testing results
    08-pr-merge/
      pr-description.md             # PR description draft
      thread-log.md                 # Review thread tracking
      merge-readiness.md            # Merge readiness checklist
      timing-log.md                 # Review timing rule log
    handover-log.md                 # All agent handovers
    decision-log.md                 # All decisions with reasoning
```

### State File (`state.json`)

```json
{
  "task": "<task-slug>",
  "created": "<ISO-8601 UTC>",
  "currentPhase": "discovery|three-amigos|architecture|planning|implementation|code-review|qa|pr-merge",
  "status": "in-progress|blocked|complete",
  "branch": "<branch-name>",
  "prNumber": null,
  "lastCommitSha": null,
  "lastCommitTime": null,
  "discoveryRound": 0,
  "planReviewCycle": 0,
  "implementationIncrement": 0,
  "adrCount": 0
}
```

## Phase 1: Intake & Discovery

**Owner**: cs Product Owner (drives conversation with human user)
**Sub-agents**: cs Requirements Analyst

### Process

1. User describes their idea to the Product Owner.
2. Product Owner creates `.thinking/<date>-<slug>/` and writes `00-intake.md`.
3. Product Owner asks the first group of **5 questions** (using built-in
   persona knowledge — business, technical, QA perspectives).
4. After the user answers, Product Owner records answers and invokes
   **cs Requirements Analyst** to analyze gaps and suggest the next 5 questions.
5. Product Owner asks the next 5 questions to the user.
6. Repeat until requirements are sufficiently clear (Product Owner decides,
   typically 3-6 rounds = 15-30 questions).
7. Product Owner writes `requirements-synthesis.md`.

### Adaptive Questioning Rules

- Questions **MUST** be grouped in sets of exactly 5.
- After each set, the next 5 **MUST** be informed by answers already received.
- Questions **SHOULD** start broad (business value, user needs) and progressively
  narrow (technical constraints, edge cases, quality expectations).
- If the user is highly technical and mentions code quality, subsequent questions
  **MUST** reflect that (architecture patterns, testing strategy, naming).
- If the user is non-technical, questions **MUST** use plain language and focus
  on outcomes rather than implementation.
- Each question **MUST** include ranked options (A, B, C...) plus
  **(X) I don't care — pick the best default**.

## Phase 2: Three Amigos

**Owner**: cs Product Owner
**Sub-agents**: cs Business Analyst, cs Tech Lead, cs QA Analyst

### Process

1. Product Owner invokes each Three Amigos sub-agent with the requirements
   synthesis, one at a time.
2. Each sub-agent reads requirements and produces their perspective document.
3. Product Owner writes `02-three-amigos/synthesis.md` combining all three.
4. If any sub-agent identifies critical gaps, Product Owner asks the user
   additional questions before proceeding.

### Three Amigos Outputs

| Perspective | Agent | Focus |
|-------------|-------|-------|
| Product | cs Business Analyst | User value, acceptance criteria, business rules |
| Development | cs Tech Lead | Technical feasibility, risks, architecture constraints |
| Quality | cs QA Analyst | Test strategy, edge cases, failure scenarios |

## Phase 3: Architecture & Design

**Owner**: cs Product Owner
**Sub-agents**: cs Solution Architect, cs C4 Diagrammer, cs ADR Keeper

### Process

1. Product Owner invokes **cs Solution Architect** with synthesized requirements.
2. Solution Architect produces `solution-design.md` with technology choices,
   component design, and integration points.
3. Product Owner invokes **cs C4 Diagrammer** to produce C4 model diagrams
   (Context, Container, Component as appropriate).
4. For each significant architectural decision, Product Owner invokes
   **cs ADR Keeper** to produce ADRs in `adrs/`.
5. Product Owner may invoke domain experts (distributed systems, cloud,
   serialization) for specialist input on architecture.

### ADR Protocol

- Every significant decision **MUST** be recorded as an ADR.
- ADRs use the Nygard template: Status, Context, Decision, Consequences.
- ADRs are immutable — superseded decisions get a new ADR referencing the old.
- ADRs in `.thinking/` are working copies; final ADRs **SHOULD** be extracted
  to a permanent location (e.g., `docs/adrs/`) when the PR is created.
- ADRs **MUST** be consulted on subsequent changes to verify directional
  alignment.

## Phase 4: Planning & Review Cycles

**Owner**: cs Product Owner
**Sub-agents**: cs Plan Synthesizer, multiple review personas

### Process

1. Product Owner combines architecture, requirements, and Three Amigos output
   into `draft-plan-v1.md`.
2. Product Owner runs **review cycle 1**:
   - Invokes each review persona (see roster below) as a sub-agent.
   - Each reviewer reads the plan and produces feedback.
   - Product Owner invokes **cs Plan Synthesizer** to deduplicate and categorize
     feedback (Must / Should / Could / Won't).
   - Product Owner revises the plan.
3. Repeat for **3-5 review cycles** total.
4. After final cycle, Product Owner writes `final-plan.md`.

### Review Personas for Planning

Each review cycle invokes these personas (subset varies by task complexity):

| Persona | Agent | Focus |
|---------|-------|-------|
| Technical Architect | cs Solution Architect | Architecture soundness |
| Tech Lead | cs Tech Lead | Feasibility, risks |
| Security | cs Reviewer Security | Security implications |
| DX | cs Reviewer DX | Developer experience |
| QA | cs QA Lead | Test strategy adequacy |
| Cloud | cs Expert Cloud | Infrastructure implications |
| Distributed Systems | cs Expert Distributed | Distributed concerns |
| Performance | cs Reviewer Performance | Performance implications |

## Phase 5: Implementation

**Owner**: cs Product Owner
**Sub-agents**: cs Lead Developer, cs Test Engineer, cs Commit Guardian

### Process

1. Product Owner creates a feature branch from `main`.
2. For each increment:
   a. Product Owner invokes **cs Lead Developer** with the next slice of work
      from the plan.
   b. Lead Developer writes production code (small, focused increment).
   c. Product Owner invokes **cs Test Engineer** to write/validate tests.
   d. Build is run and verified clean (zero warnings).
   e. Tests are run and verified passing.
   f. Product Owner invokes **cs Commit Guardian** to review the increment.
   g. If issues found, Lead Developer fixes them.
   h. Commit is made with a properly scoped message.
3. Repeat until all plan items are implemented.

### Incremental Discipline Rules

- Each increment **MUST** be small enough to review as if it were its own PR.
- Each increment **MUST** include relevant tests.
- The build **MUST** be clean after each increment.
- All tests **MUST** pass after each increment.
- Each commit **MUST** represent one logical step.
- The Commit Guardian reviews each commit before the next begins.

### Implementation Order

1. Write a failing test (TDD where practical).
2. Write the minimal production code to pass it.
3. Refactor if needed.
4. Run build + tests.
5. Commit.
6. Review.
7. Next increment.

## Phase 6: Comprehensive Code Review

**Owner**: cs Product Owner
**Sub-agents**: All review personas, domain experts

### Process

1. Product Owner uses `git diff main...HEAD` to identify all changed files.
2. Product Owner invokes review personas in sequence:

   | Priority | Agent | Style |
   |----------|-------|-------|
   | 1 | cs Reviewer Pedantic | Line-by-line, naming, every detail |
   | 2 | cs Reviewer Strategic | Architecture, design patterns, big picture |
   | 3 | cs Reviewer Security | OWASP, attack surface, trust boundaries |
   | 4 | cs Reviewer DX | API ergonomics, discoverability, pit of success |
   | 5 | cs Reviewer Performance | Allocations, complexity, hot paths |

3. Product Owner invokes relevant domain experts based on the change type.
4. Product Owner synthesizes all review output.
5. For each finding: fix it or document why it was declined.
6. Iterate until all reviewers are satisfied.

### Review Coverage Rule

Every changed file **MUST** be reviewed by at least the Pedantic and Strategic
reviewers. Domain experts review files within their expertise.

## Phase 7: QA Validation

**Owner**: cs Product Owner
**Sub-agents**: cs QA Lead, cs QA Exploratory, cs Test Engineer

### Process

1. Product Owner invokes **cs QA Lead** to review test strategy and coverage.
2. Product Owner invokes **cs QA Exploratory** to apply exploratory testing
   perspective.
3. Product Owner invokes **cs Test Engineer** for mutation testing (Mississippi
   projects only).
4. Any gaps identified are fed back to implementation.

## Phase 8: PR Creation & Merge Readiness

**Owner**: cs Product Owner
**Sub-agents**: cs PR Manager, cs Scribe

### Process

1. Product Owner invokes **cs Scribe** to compile all decisions, thinking, and
   reasoning into a coherent narrative.
2. Product Owner invokes **cs PR Manager** to:
   a. Create the PR with a complete description (business value, how it works,
      files changed, testing evidence, breaking changes).
   b. Monitor CI pipelines.
   c. Handle review threads.
3. Review thread handling:
   - For each human review comment: read it, decide scope-appropriateness,
     fix it or push back with reasoning. Reply to every thread. Resolve it.
4. Merge readiness is confirmed when:
   - [ ] PR exists
   - [ ] All CI pipelines are green
   - [ ] No unresolved review comments
   - [ ] No open review threads
   - [ ] Review timing rule satisfied

### Review Timing Rule

A review comment may be added at any point within **10 minutes** of the last
commit. After that 10-minute window, no further checking is required.

Protocol:
1. After the last commit, start a 10-minute timer.
2. Poll for new review comments.
3. If a new comment appears within the window: address it, commit the fix,
   and restart the 10-minute timer from the new commit time.
4. If no new comments appear after 10 minutes from the last commit: merge
   readiness is confirmed.

### Review Thread Handling

- Use GitHub MCP or GitHub CLI to read, reply to, and resolve threads.
- For each comment:
  - Read and understand it.
  - Determine if it is in scope.
  - If in scope: fix, commit, push, reply with evidence, resolve.
  - If out of scope: reply with reasoned explanation, leave open for reviewer.
- Resolving threads is **critical** — the PR cannot merge with open threads.
- One comment = one commit = one reply = one resolution.

## Handover Protocol

Every handover between agents **MUST** include:

1. **Context**: what has been done so far (file paths in `.thinking/`)
2. **Objective**: what the receiving agent must do
3. **Constraints**: what the receiving agent must not do
4. **Evidence**: relevant file paths, code references, test results
5. **Expected Output**: what the receiving agent must produce and where

Handovers are logged in `.thinking/<task>/handover-log.md`:

```markdown
## Handover: <from-agent> → <to-agent>
- **Time**: <ISO-8601 UTC>
- **Phase**: <current phase>
- **Context**: <summary + file paths>
- **Objective**: <what to do>
- **Output**: <expected result and location>
```

## Agent Roster (29 Agents)

### Entry Point (1)

| Agent | Role |
|-------|------|
| cs Product Owner | Sole human interface; orchestrates the entire SDLC |

### Discovery & Requirements (3)

| Agent | Role |
|-------|------|
| cs Requirements Analyst | Deep requirements analysis, gap identification |
| cs Business Analyst | Business value, user needs, acceptance criteria |
| cs QA Analyst | Testability, edge cases, quality scenarios |

### Architecture (4)

| Agent | Role |
|-------|------|
| cs Tech Lead | Technical feasibility, risk assessment, architecture review |
| cs Solution Architect | Solution design, technology choices, component design |
| cs C4 Diagrammer | C4 architecture diagrams (Context, Container, Component) |
| cs ADR Keeper | Architecture Decision Records management |

### Planning (1)

| Agent | Role |
|-------|------|
| cs Plan Synthesizer | Synthesizes multi-persona review feedback |

### Implementation (3)

| Agent | Role |
|-------|------|
| cs Lead Developer | Production code, clean code, incremental implementation |
| cs Test Engineer | Tests, coverage, mutation testing |
| cs Commit Guardian | Commit-level review, enforces discipline |

### Code Review (5)

| Agent | Role |
|-------|------|
| cs Reviewer Pedantic | Line-by-line, naming, every minor detail |
| cs Reviewer Strategic | Architecture, design patterns, big-picture risks |
| cs Reviewer Security | OWASP, attack surface, trust boundaries |
| cs Reviewer DX | API ergonomics, developer experience |
| cs Reviewer Performance | Allocations, complexity, hot paths |

### Domain Experts (7)

| Agent | Role |
|-------|------|
| cs Expert CSharp | C# language idioms, runtime, type system |
| cs Expert Python | Python perspective, cross-ecosystem analysis |
| cs Expert Java | Java/enterprise patterns, type-safety perspective |
| cs Expert Serialization | JSON, wire formats, versioning, transport |
| cs Expert Cloud | Azure, AWS, cloud infrastructure, cost |
| cs Expert Distributed | Distributed systems, consensus, CAP theorem |
| cs Expert UX | User experience, accessibility, interaction design |

### QA (2)

| Agent | Role |
|-------|------|
| cs QA Lead | QA strategy, coverage analysis, shift-left advocacy |
| cs QA Exploratory | Exploratory testing, creative scenario discovery |

### DevOps (1)

| Agent | Role |
|-------|------|
| cs DevOps Engineer | CI/CD, pipelines, deployment, observability |

### Documentation & PR (2)

| Agent | Role |
|-------|------|
| cs Scribe | Records thinking, decisions, reasoning, handovers |
| cs PR Manager | PR lifecycle, thread management, merge readiness |

## Quality Bar

This system builds mission-critical applications to the highest enterprise
standard:

- No shortcuts.
- No quality compromises.
- Naming conventions matter.
- Developer experience matters.
- The "easier" approach is not chosen unless it is also the correct approach.
- Every decision is documented with reasoning.
- Every conclusion is verified through CoV.
- The goal is **exceptional, error-free output** — consistently.
