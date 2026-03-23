# Clean Squad: End-to-End SDLC Workflow

The Clean Squad is a family of 32 GitHub Copilot agents that takes an idea from
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
agents are sub-agents invoked programmatically via `runSubagent`. All Clean
Squad delegation **MUST** target only approved Clean Squad agents named in the
`Agent Roster` section of this workflow. The user never needs to invoke any
other agent directly.

If no approved Clean Squad agent fits a task, the Product Owner **MUST** stop,
record the blocker, and ask the user to either choose the nearest approved
Clean Squad agent, approve a roster or workflow change first, or explicitly
leave Clean Squad orchestration for that task.

## Shared State: The `.thinking` Folder

All agents share state through a filesystem folder:

```text
.thinking/
  <YYYY-MM-DD>-<task-slug>/        # One subfolder per task
    state.json                      # Workflow state (current phase, status)
    activity-log.md                 # Start/progress/blocker/completion log
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
      adoption-perspective.md       # Developer Evangelist output
      synthesis.md                  # Unified requirements
    03-architecture/
      solution-design.md            # Solution Architecture
      c4-context.md                 # C4 Context diagram
      c4-container.md               # C4 Container diagram
      c4-component.md               # C4 Component diagram (if needed)
      c4-component-omitted.md       # Explicit rationale when a Level 3 view is not needed
      adr-notes.md                  # ADR candidate notes and rationale drafts
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
    08-documentation/
      scope-assessment.md           # Branch diff analysis for doc needs
      page-plan.md                  # Planned pages with types and paths
      evidence-map.md               # Evidence map for technical claims
      drafts/
        <page-name>.md              # Draft pages before publishing
      review-cycle-01/
        doc-review.md               # Doc Reviewer findings
        doc-story-review.md         # Developer Evangelist story-value findings
        remediation-log.md          # Fix tracking
      review-cycle-02/              # Second review cycle (if needed)
        ...
      publication-report.md         # Final pages published with verification
    09-pr-merge/
      pr-description.md             # PR description draft
      thread-log.md                 # Review thread tracking
      merge-readiness.md            # Merge readiness checklist
      polling-log.md                # Review polling rule log
    handover-log.md                 # All agent handovers
    decision-log.md                 # All decisions with reasoning
```

Published ADRs live outside `.thinking/` under `docs/Docusaurus/docs/adr/` so
they remain part of the long-term documentation set after the task folder is
retired.

### Operational Logging Protocol

- Every Clean Squad agent MUST append an entry to `.thinking/<task>/activity-log.md` before substantive work starts.
- Every Clean Squad agent MUST append another entry after each material decision, delegation, blocker, or phase transition.
- Every Clean Squad agent MUST append a final entry before returning control, capturing outputs produced, status, blockers, and next action.
- The Product Owner MUST treat this log as mandatory operational telemetry, not an optional summary.
- Activity log entries SHOULD use a consistent structure: UTC timestamp, actor, phase, action, artifacts updated, blockers, and next action.

## Product Owner Execution Boundary

The Product Owner is an orchestrator, not an implementation agent.

- The Product Owner MUST ask the user questions, sequence the workflow, update shared state, synthesize sub-agent outputs, and enforce quality gates.
- The Product Owner MUST use `runSubagent` for specialist work including analysis, design, coding, testing, code review, QA validation, documentation, and PR management.
- Before every `runSubagent` call, the Product Owner MUST validate that the selected agent is explicitly named in the `Agent Roster` section of this workflow.
- Generic categories such as review personas and domain experts MUST resolve only to named agents in the `Agent Roster` section of this workflow.
- If no approved Clean Squad agent clearly fits, the Product Owner MUST stop, record the blocker, and ask the user to either choose the nearest approved Clean Squad agent, approve a roster or workflow change first, or explicitly leave Clean Squad orchestration for that task.
- The Product Owner MUST NOT bypass a specialist sub-agent by performing that specialist work directly.

### State File (`state.json`)

```json
{
  "task": "<task-slug>",
  "created": "<ISO-8601 UTC>",
  "currentPhase": "discovery|three-amigos|architecture|planning|implementation|code-review|qa|documentation|pr-merge",
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

## Phase 2: Three Amigos + Adoption

**Owner**: cs Product Owner
**Sub-agents**: cs Business Analyst, cs Tech Lead, cs QA Analyst, cs Developer Evangelist

### Process

1. Product Owner invokes each perspective sub-agent with the requirements
   synthesis, one at a time.
2. Each sub-agent reads requirements and produces their perspective document.
3. Product Owner writes `02-three-amigos/synthesis.md` combining all four.
4. If any sub-agent identifies critical gaps, Product Owner asks the user
   additional questions before proceeding.

### Perspective Outputs

| Perspective | Agent | Focus |
|-------------|-------|-------|
| Product | cs Business Analyst | User value, acceptance criteria, business rules |
| Development | cs Tech Lead | Technical feasibility, risks, architecture constraints |
| Quality | cs QA Analyst | Test strategy, edge cases, failure scenarios |
| Adoption | cs Developer Evangelist | Demo-ability, competitive positioning, conference potential, real-world relevance |

## Phase 3: Architecture & Design

**Owner**: cs Product Owner
**Sub-agents**: cs Solution Architect, cs C4 Diagrammer, cs ADR Keeper

### Process

1. Product Owner invokes **cs Solution Architect** with synthesized requirements.
2. Solution Architect produces `solution-design.md` with technology choices,
   component design, and integration points.
3. Product Owner invokes **cs C4 Diagrammer** to produce C4 model diagrams
  (Context and Container always; Component when a container has meaningful internal structure, otherwise an explicit omission rationale).
4. For each significant architectural decision, Product Owner invokes
  **cs ADR Keeper** to produce ADRs in `docs/Docusaurus/docs/adr/`.
5. Product Owner may invoke approved domain experts from the `Agent Roster`
   section (for example cs Expert Distributed, cs Expert Cloud, and
   cs Expert Serialization) for specialist input on architecture.

### ADR Protocol

- Every significant decision **MUST** be recorded as an ADR.
- ADRs **MUST** use the MADR 4.0.0 template defined in `.github/instructions/adr.instructions.md`.
- ADRs **MUST** be published to `docs/Docusaurus/docs/adr/` using the filename pattern `NNNN-title-with-dashes.md`.
- When a feature branch adds ADRs, the branch owner **MUST** treat those numbers as provisional and perform a final renumbering pass against the latest `main` during merge preparation, updating filenames, `ADR-NNNN` titles, `sidebar_position`, and relative ADR links for ADRs introduced by that branch.
- ADRs are immutable — superseded decisions get a new ADR referencing the old.
- ADRs **MUST** be consulted on subsequent changes to verify directional
  alignment.

## Phase 4: Planning & Review Cycles

**Owner**: cs Product Owner
**Sub-agents**: cs Plan Synthesizer, approved review personas from the Agent Roster

### Process

1. Product Owner combines architecture, requirements, and Three Amigos output
   into `draft-plan-v1.md`.
2. Product Owner runs **review cycle 1** by invoking each approved review
  persona from the `Agent Roster` section as a sub-agent.
3. Each reviewer reads the plan and produces feedback.
4. Product Owner invokes **cs Plan Synthesizer** to deduplicate and categorize
  feedback (Must / Should / Could / Won't).
5. Product Owner revises the plan.
6. Repeat for **3-5 review cycles** total.
7. After final cycle, Product Owner writes `final-plan.md`.

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
| Adoption | cs Developer Evangelist | Demo-ability, competitive positioning, content hooks |

## Phase 5: Implementation

**Owner**: cs Product Owner
**Sub-agents**: cs Lead Developer, cs Test Engineer, cs Commit Guardian

### Process

1. Product Owner creates a feature branch from `main`.
2. For each increment:
   a. Product Owner invokes **cs Lead Developer** with the next slice of work
      from the plan.
   b. Lead Developer writes production code (small, focused increment).
  c. Lead Developer performs semantic consistency review for touched code
    elements (types or members),
    updates stale comments or XML documentation when needed, and records the
    reviewed-member evidence in `changes.md`.
  d. Product Owner invokes **cs Test Engineer** to write/validate tests and
    independently verify semantic consistency for touched code elements
    against the changed behavior, recording the result in `test-results.md`.
  e. Build is run and verified clean (zero warnings).
  f. Tests are run and verified passing.
  g. Product Owner invokes **cs Commit Guardian** to review the final staged
    diff, validate the semantic consistency evidence chain, and emit a
    `PASS`, `WARNING`, or `BLOCKER` verdict in `commit-review.md`.
  h. If semantic drift or missing semantic-review evidence is found, Lead
    Developer fixes it and the semantic consistency gate is rerun against the
    final staged diff.
  i. Commit is made with a properly scoped message.
3. Repeat until all plan items are implemented.

Touched semantic-review scope includes any touched code element (type or
member) whose behavior, signature, or name changes, and any touched code
element that already has comments or XML documentation. Semantic drift on a
touched code element is a must-fix blocker before commit. Missing comments or
XML documentation are not universally required, but when a touched code element
clearly needs explanation or follows an established documented-public-API
pattern, the absence must be flagged as `BLOCKER` if it would leave a
materially false, incomplete, or undocumented contract; otherwise it must be
flagged as `WARNING`. This semantic consistency review is a Phase 5
code-comment/XML-doc gate, not Phase 8 product documentation.

Evidence of semantic consistency review must be recorded in `changes.md`,
`test-results.md`, and `commit-review.md`. Each artifact must identify the
reviewed touched code elements or explicitly state why no touched code
elements were in semantic-review scope.

### Incremental Discipline Rules

- Each increment **MUST** be small enough to review as if it were its own PR.
- Each increment **MUST** include relevant tests.
- The build **MUST** be clean after each increment.
- All tests **MUST** pass after each increment.
- Semantic consistency review evidence **MUST** be current for the final staged
  diff before commit approval.
- Any code change after semantic consistency review evidence is recorded
  **MUST** invalidate prior approval and **MUST** rerun the gate against the
  final staged diff before commit.
- Each commit **MUST** represent one logical step.
- The Commit Guardian reviews each commit before the next begins.

### Implementation Order

1. Write a failing test (TDD where practical).
2. Write the minimal production code to pass it.
3. Refactor if needed.
4. Run build + tests.
5. Review the final staged diff, including the semantic consistency gate.
6. Commit.
7. Next increment.

## Phase 6: Comprehensive Code Review

**Owner**: cs Product Owner
**Sub-agents**: Approved review personas and approved domain experts from the Agent Roster

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
   | 6 | cs Developer Evangelist | Demo-ability, shareability, competitive positioning (public API changes) |

3. Product Owner invokes relevant approved domain experts from the `Agent Roster` section based on the change type.
4. Product Owner synthesizes all review output.
5. For each finding: fix it or document why it was declined.
6. Iterate until all reviewers are satisfied.

The phrases review personas and domain experts in this workflow refer only to
the named agents in the `Agent Roster` section below. They do not authorize
delegation to other repo agents.

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

## Phase 8: Documentation

**Owner**: cs Product Owner
**Sub-agents**: cs Technical Writer, cs Doc Reviewer, cs Developer Evangelist

### Purpose

Ensure that every user-facing change is accompanied by accurate, evidence-backed
Docusaurus documentation before the PR is created. Documentation is a first-class
deliverable, not an afterthought.

### Process

1. Product Owner assesses documentation scope:
   - Run `git diff --name-status --find-renames main...HEAD` to identify all
     changed source files.
   - Identify new public APIs, changed behavior, new concepts, and affected
     existing doc pages.
   - If no user-facing changes exist (pure refactors, internal-only changes),
     record the skip reason in `.thinking/<task>/08-documentation/scope-assessment.md`
     and proceed to Phase 9.

2. Product Owner invokes **cs Technical Writer** to create/update documentation:
   - The writer reads all `.thinking/<task>/` artifacts and the branch diff.
   - The writer builds an evidence map, classifies page types, and drafts pages.
   - Draft pages are written to `.thinking/<task>/08-documentation/drafts/`.
   - Verified pages are published to `docs/Docusaurus/docs/`.

3. Product Owner runs a **documentation review cycle** (repeat 1-3 times):
   a. Invoke **cs Doc Reviewer** to independently review every new or updated
      doc page against source code and tests.
   b. Doc Reviewer writes findings to
      `.thinking/<task>/08-documentation/review-cycle-NN/doc-review.md`.
   c. Invoke **cs Developer Evangelist** to review documentation for story
      value, content potential, and adoption narrative.
   d. Developer Evangelist writes findings to
      `.thinking/<task>/08-documentation/review-cycle-NN/doc-story-review.md`.
   e. For each Must Fix or Should Fix finding:
      - Re-invoke **cs Technical Writer** with the specific finding to fix.
      - Record the fix in the remediation log.
   f. Repeat until the Doc Reviewer returns no Must Fix findings.

4. Product Owner validates documentation quality gates:
   - [ ] All new public APIs have documentation
   - [ ] All changed behaviors reflected in existing docs
   - [ ] Page types are correct
   - [ ] Frontmatter is complete
   - [ ] Internal links resolve
   - [ ] Code examples are verified
   - [ ] Claims are evidence-backed
   - [ ] Adjacent pages are cross-linked
   - [ ] No Must Fix findings remain from Doc Reviewer

5. Update `.thinking/<task>/activity-log.md` with documentation outcomes.

### Documentation Skip Criteria

Documentation may be skipped **only** when ALL of these are true:

- No new public types, methods, or extension points were introduced
- No existing public behavior was changed
- No new configuration options were added
- No existing documentation is invalidated by the change

The skip reason **MUST** be recorded in `scope-assessment.md` with evidence.

## Phase 9: PR Creation & Merge Readiness

**Owner**: cs Product Owner
**Sub-agents**: cs PR Manager, cs Scribe

### Process

1. Product Owner invokes **cs Scribe** to compile all decisions, thinking, and
   reasoning into a coherent narrative.
2. Product Owner invokes **cs PR Manager** to:
   a. Create the PR with a complete description (business value, how it works,
      files changed, testing evidence, breaking changes).
   b. Monitor CI pipelines.
  c. Handle review threads using the repository PR polling protocol.
3. Review thread handling:
   - For each human review comment: read it, decide scope-appropriateness,
     fix it or push back with reasoning. Reply to every thread. Resolve it.
4. Merge readiness is confirmed when:

- [ ] PR exists
- [ ] All CI pipelines are green
- [ ] No unresolved review comments
- [ ] No open review threads
- [ ] Review polling rule satisfied

### Review Polling Rule

A pushed PR enters a **300-second poll loop** for review comments. A single
quiet interval is not enough; the loop ends only when a poll returns no new
unaddressed comments or the configured iteration cap is reached.

Protocol:

1. After pushing to an open PR, wait 300 seconds.
2. Poll for unresolved review comments.
3. If new comments exist: address them one-at-a-time, push the fixes, then
   restart the 300-second wait.
4. If a poll returns zero new unaddressed comments: merge readiness is confirmed.
5. If the iteration cap is reached: stop and report the remaining unresolved
  threads for human review.

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

## Agent Roster (32 Agents)

This section is the single authoritative roster of approved Clean Squad
delegation targets. Any delegation term in this workflow, the shared Clean
Squad instruction, or the Product Owner prompt must resolve only to the named
agents in this roster. If no listed agent fits, the Product Owner must stop,
record the blocker, and ask the user to either choose the nearest approved
Clean Squad agent, approve a roster or workflow change first, or explicitly
leave Clean Squad orchestration for that task.

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

### Adoption (1)

| Agent | Role |
|-------|------|
| cs Developer Evangelist | Conference talks, competitive positioning, demo-ability, real-world adoption |

### QA (2)

| Agent | Role |
|-------|------|
| cs QA Lead | QA strategy, coverage analysis, shift-left advocacy |
| cs QA Exploratory | Exploratory testing, creative scenario discovery |

### DevOps (1)

| Agent | Role |
|-------|------|
| cs DevOps Engineer | CI/CD, pipelines, deployment, observability |

### Documentation (2)

| Agent | Role |
|-------|------|
| cs Technical Writer | Docusaurus docs authoring, evidence-backed pages |
| cs Doc Reviewer | Documentation accuracy, completeness, and navigation review |

### PR & Records (2)

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
