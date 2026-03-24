# Pull Request Best Practices

A pull request is not merely a mechanism for merging code. It is a structured
conversation between the author and the reviewers about a proposed change —
its intent, its correctness, its risks, and its design. The quality of that
conversation determines whether the team catches defects early, shares
knowledge effectively, and maintains a codebase that everyone trusts.

**This document is written in Minto Pyramid format.**

---

## Governing Thought

Pull request best practices centre on two goals: making it easy for reviewers
to understand and verify a change, and making the review process itself a
high-value activity that catches defects, shares knowledge, and improves design.
Both the author and the reviewer have distinct responsibilities, and excellence
in pull requests requires discipline from both sides.

---

## Situation

Modern software teams use pull requests (also called merge requests) as the
primary mechanism for proposing, reviewing, and integrating code changes. Code
review through pull requests is now the industry standard for distributed teams.
Research from **Microsoft** (Bosu et al., 2015; Bacchelli and Bird, 2013)
confirms that code review is practised by virtually all professional software
teams and is considered one of the most effective quality practices available.

## Complication

Despite their ubiquity, pull requests are frequently a source of friction:

- **Large PRs** overwhelm reviewers, who skim rather than review.
- **Unclear descriptions** force reviewers to reverse-engineer intent from the
  diff.
- **Slow reviews** block progress and create context-switching costs.
- **Superficial reviews** (rubber-stamping) detect cosmetic issues but miss
  logic errors and design problems.
- **Adversarial reviews** create defensive authors who minimise future
  submissions rather than welcoming feedback.

Research by **Czerwonka et al.** (Microsoft, 2015) found that code review
effectiveness drops significantly as change size increases. Studies at **Google**
(Sadowski et al., 2018) found that review quality is highest when changes are
small, focused, and well-described.

## Question

How should authors prepare pull requests and reviewers conduct reviews to
maximise the quality of the review, minimise friction, and make the process a
net positive for both parties and the codebase?

---

## Key-Line 1: Author Responsibilities — Before the Review

### The Single Responsibility Rule for PRs

> "A PR should do one thing. If it does more than one thing, split it."

A pull request should have **one logical change**. Examples of single-purpose
PRs:

- Add a new feature
- Fix a specific bug
- Refactor a specific area
- Update dependencies
- Add or update tests for an existing feature

Examples of PRs that should be split:

- Fix a bug AND refactor the surrounding code
- Add a feature AND update unrelated formatting
- Database migration AND business logic change

### Size Matters

| PR Size (lines changed) | Review Quality | Reviewer Experience |
|---|---|---|
| 1–50 | Thorough, high-quality review | Reviewers engage deeply |
| 50–200 | Good review for focused changes | Reviewers can maintain concentration |
| 200–400 | Review quality starts to decline | Reviewers begin to skim |
| 400–600 | Significant portions are skimmed | Reviewers focus on obvious issues only |
| 600+ | Rubber-stamp risk is high | Reviewers give up and approve |

Research from **SmartBear** (Cohen, 2006) and **Microsoft** (Czerwonka et al.,
2015) consistently shows that review effectiveness decreases as change size
increases. The optimal range is **200–400 lines** of meaningful change.

### The PR Description

The description is the author's primary tool for enabling a good review. A
well-structured description:

1. **States what changed** — A concise summary of the modification.
2. **Explains why** — The business value or technical motivation. This is the
   most important and most frequently omitted element.
3. **Describes how** — A brief architectural overview for non-trivial changes.
4. **Lists what to focus on** — What areas the author is least confident about
   and wants careful review.
5. **Notes breaking changes** — With before/after code examples.
6. **Lists testing evidence** — What was tested, how, and with what results.

### Self-Review Before Submission

Before requesting review, the author should:

| Step | Purpose |
|---|---|
| **Read the entire diff** | Catch unintended changes, debug statements, commented-out code |
| **Run all tests** | Confirm the change does not break anything |
| **Review commit messages** | Ensure they are clear and properly scoped |
| **Check for scope creep** | Remove any changes unrelated to the PR's purpose |
| **Add inline comments** | Annotate complex or non-obvious changes for the reviewer |
| **Verify CI passes** | Do not submit a PR with a failing build |

### Commit Hygiene

| Practice | Guidance |
|---|---|
| **Logical commits** | Each commit should represent one logical step |
| **Commit messages** | Start with a concise summary line; add detail in the body if needed |
| **One concern per commit** | Do not mix refactoring and behaviour changes in the same commit |
| **Compiles at every commit** | The build should pass at each commit point (reviewers may review commit-by-commit) |

---

## Key-Line 2: Reviewer Responsibilities — Conducting the Review

### What Reviewers Should Evaluate

Code review should evaluate multiple dimensions, roughly in this priority
order:

| Priority | Dimension | What to Check |
|---|---|---|
| 1 | **Correctness** | Does the code do what the description says? Are there logic errors, off-by-one errors, race conditions, or missing edge cases? |
| 2 | **Design** | Does the change fit the existing architecture? Are abstractions appropriate? Is the dependency direction correct? |
| 3 | **Security** | Are there injection risks, authentication gaps, broken access control, or data exposure? |
| 4 | **Tests** | Are there sufficient tests? Do they test the right things? Are they deterministic? |
| 5 | **Readability** | Can a future maintainer understand this code without the PR description? Are names meaningful? |
| 6 | **Performance** | Are there obvious performance problems (N+1 queries, unnecessary allocations, blocking calls in hot paths)? |
| 7 | **Style** | Does the code follow the team's conventions? (This should largely be automated.) |

### Reviewer Mindset

| Do | Do Not |
|---|---|
| Assume the author is competent and acted in good faith | Assume the author is careless |
| Ask questions when intent is unclear | Make assumptions about intent |
| Suggest alternatives, not just objections | Say "this is wrong" without offering a path forward |
| Distinguish critical issues from preferences | Block a PR over style preferences |
| Acknowledge good work | Focus exclusively on problems |
| Review promptly (within hours, not days) | Let PRs languish in the queue |

### The Feedback Spectrum

Not all comments are equal. Reviewers should **label** their feedback to help
the author prioritise:

| Label | Meaning | Author Response |
|---|---|---|
| **Blocker** / **Must fix** | The PR cannot be merged with this issue | Must address before merging |
| **Suggestion** | An improvement the reviewer recommends | Should be considered; can disagree with rationale |
| **Nit** / **Minor** | A cosmetic or preference-based comment | Nice to address; not required |
| **Question** | The reviewer does not understand something | Requires a response (explanation or code change) |
| **Praise** | Something the reviewer thinks is well done | No response needed; builds trust |

### The Review Process

1. **Read the description first** — Understand the intent before reading code.
2. **Review the tests first** — Tests reveal what the change is supposed to do.
   If the tests are clear, the production code review becomes easier.
3. **Review the diff file by file** — Start with the most important files
   (indicated by the description or by the nature of the change).
4. **Leave comments inline** — Anchor feedback to specific lines.
5. **Summarise** — At the end, leave a summary comment with the overall
   assessment: approve, request changes, or comment.

---

## Key-Line 3: The Review Conversation

### Commenting Best Practices

| Practice | Guidance |
|---|---|
| **Be specific** | "This null check is missing for the `userId` parameter" is actionable. "Handle errors better" is not. |
| **Explain the why** | "This should use `StringBuilder` because the loop creates O(n) string allocations" teaches. "Use StringBuilder" does not. |
| **Ask rather than tell** | "What happens if `count` is zero here?" is more collaborative than "This will throw a DivideByZeroException." (The reviewer may be wrong; a question opens dialogue.) |
| **Offer code suggestions** | When possible, provide the specific code you would write, not just a description of what you want. |
| **Scope comments appropriately** | Do not review code that was not changed in this PR unless the change interacts with it. |
| **Keep it professional** | Review the code, not the person. "This function has too many responsibilities" — not "You wrote this wrong." |

### Author Response Best Practices

| Practice | Guidance |
|---|---|
| **Respond to every comment** | Even if just to acknowledge. Silence is ambiguous. |
| **Thank reviewers for catching issues** | Positive reinforcement encourages thorough reviews. |
| **Disagree with evidence, not emotion** | "I benchmarked both approaches and the current one is 3x faster in our context" is persuasive. "I prefer it this way" is not. |
| **Do not take feedback personally** | The review is about the code, not about you. |
| **Make changes in dedicated commits** | So reviewers can see what changed in response to feedback. |
| **Resolve threads explicitly** | When a comment is addressed, reply with what was done and the commit reference. |

### Resolving Disagreements

When the author and reviewer disagree:

1. **Prefer data over opinion** — benchmarks, tests, documentation.
2. **Seek a third opinion** if stalled — do not block indefinitely.
3. **The reviewer approves or escalates** — the author does not self-approve
   to bypass a disagreement.
4. **Document the decision** — if an unconventional approach is chosen, add a
   brief explanation to the code or the PR thread.

---

## Key-Line 4: Review Speed and Throughput

### Why Speed Matters

| Metric | Impact |
|---|---|
| **Time to first review** | If review takes days, the author has context-switched. Feedback is less effective because the code is no longer in working memory. |
| **Total review cycle time** | Long cycles encourage large PRs (to amortise the wait), which are harder to review, creating a vicious cycle. |
| **Reviewer availability** | Teams should treat PR review as a high-priority interrupt, not a background task. |

### Google's Guidance

Research at **Google** (Sadowski et al., 2018) found:

- The median time to first review comment is about **4 hours**.
- Reviews completed within one business day have the highest satisfaction
  and quality ratings.
- Faster review turnaround encourages smaller, more frequent PRs.

### Practical Speed Guidelines

| Guideline | Rationale |
|---|---|
| **Review within 4 hours during working hours** | Keeps the author in context |
| **No PR should wait more than 1 business day** | Prevents queue stagnation |
| **Block time for reviews** | Dedicated slots (e.g., first thing in the morning) prevent reviews from being perpetually deferred |
| **Use "review roulette" or rotation** | Distributes review load and prevents bottlenecks on senior engineers |

---

## Key-Line 5: Automated Checks — The First Reviewer

### What Should Be Automated

Every pull request should pass automated checks before a human reviewer looks at
it:

| Check | Purpose |
|---|---|
| **Build** | The code compiles |
| **Unit tests** | Existing tests still pass |
| **Linting / formatting** | Style rules are satisfied |
| **Static analysis** | Analysers detect common defects, security issues, and code smells |
| **Coverage** | Test coverage has not regressed |
| **Dependency audit** | No known vulnerable dependencies introduced |
| **Commit message format** | Messages follow the team's convention |

### Why Automation Matters

Automated checks free the human reviewer to focus on what machines cannot judge:
design, intent, correctness under complex conditions, and alignment with
architectural direction. A reviewer who spends their time pointing out
formatting issues is wasting their expertise.

### The Green Build Prerequisite

A PR with a failing build should not be reviewed. The author should fix the
build first. Reviewing against a broken build is unreliable because the reviewer
cannot distinguish between intentional changes and collateral breakage.

---

## Key-Line 6: Advanced Practices

### Stacked PRs

For large features, use **stacked PRs** (also called PR chains):

```text
main ← PR 1 (data model) ← PR 2 (business logic) ← PR 3 (API layer)
```

Each PR is small and reviewable. Each builds on the previous one. Reviewers
review and merge from the bottom up. This avoids the problem of a single
600-line PR that nobody can review effectively.

### Draft PRs

Open a **draft PR early** for:

- Getting directional feedback before the implementation is complete.
- Making work visible to the team.
- Enabling early architectural review.

Draft PRs should be clearly marked and should not be reviewed as if they were
final submissions.

### Post-Merge Review

Some teams practise **post-merge review** for low-risk changes:

- The author merges directly (after automated checks pass).
- Reviews happen after the fact.
- Issues found in post-merge review are addressed in follow-up PRs.

This is appropriate for teams with high trust and strong automated checks. It
is not appropriate for teams with quality problems or for high-risk changes.

### Review Checklists

A lightweight checklist can improve review consistency:

- [ ] Description explains the "why"
- [ ] Change is single-purpose
- [ ] Tests cover the changed behaviour
- [ ] No unintended scope creep
- [ ] Security implications considered
- [ ] Breaking changes documented
- [ ] CI passes

---

## Key-Line 7: What Good Looks Like — A Reference PR

### The PR Description

```markdown
## What

Add rate limiting to the `/api/orders` endpoint using a sliding
window algorithm.

## Why

Production monitoring shows that 3% of traffic to this endpoint comes
from a single client exceeding 10,000 requests per minute, causing
latency spikes for all users.

## How

- Added `SlidingWindowRateLimiter` using `System.Threading.RateLimiting`
- Configured at 1,000 requests per minute per client IP
- Returns HTTP 429 with `Retry-After` header when exceeded
- Added unit tests for the limiter and integration test for the middleware

## What to focus on

- Is the 1,000/minute limit appropriate? (Product decision)
- Is the sliding window the right algorithm vs. token bucket?

## Testing

- 12 unit tests covering normal flow, limit exceeded, window reset
- 1 integration test hitting the endpoint in a test host
- All existing tests pass
- Zero build warnings
```

### The Review

```markdown
## Summary

Looks good overall. Clean implementation of rate limiting. Two items:

**Blocker**: The `Retry-After` header value is in milliseconds but
RFC 7231 specifies seconds. See line 47.

**Suggestion**: Consider extracting the rate limit configuration (1000,
60s window) into `appsettings.json` for runtime adjustment.

**Praise**: Good test coverage, especially the window-reset edge case.
```

---

## Common Pitfalls

| Pitfall | Description | Prevention |
|---|---|---|
| **Large PRs** | Reviewers skim or rubber-stamp | Target 200–400 lines; use stacked PRs for large features |
| **Missing "why"** | Reviewers cannot evaluate design without understanding intent | Require business context in every PR description |
| **Rubber-stamp reviews** | Approving without reading | Require at least one substantive comment per review |
| **Style wars** | Blocking PRs over formatting preferences | Automate style checks; human reviews focus on logic and design |
| **Review hoarding** | One senior reviewer becomes the bottleneck | Use rotation; encourage everyone to review |
| **Silent disagreement** | Author ignores comments without responding | Require explicit response to every thread |
| **Stale PRs** | PRs open for days or weeks, accumulating merge conflicts | Set a team SLA (e.g., 1 business day for first review) |
| **Reviewing unrelated code** | Reviewer critiques code not changed in this PR | Stay within the scope of the diff |

---

## Authoritative Sources

| Source | Reference |
|---|---|
| **Bacchelli, A. and Bird, C.** | *Expectations, Outcomes, and Challenges of Modern Code Review* (2013). ICSE '13. Microsoft Research. Foundational empirical study on code review motivations and outcomes. |
| **Czerwonka, J., Greiler, M., and Tilford, J.** | *Code Reviews Do Not Find Bugs: How the Current Code Review Best Practice Slows Us Down* (2015). ICSE '15. Microsoft. Study on review effectiveness vs. change size. |
| **Sadowski, C. et al.** | *Modern Code Review: A Case Study at Google* (2018). ICSE-SEIP '18. Google's code review practices and metrics. |
| **Cohen, J.** | *Best Kept Secrets of Peer Code Review* (2006). SmartBear. Practical guidelines and empirical data on review effectiveness. |
| **Bosu, A. et al.** | *Process Aspects and Social Dynamics of Contemporary Code Review* (2015). IEEE TSE. Microsoft Research. |
| **Google Engineering Practices** | *How to do a code review*. Google Engineering. <https://google.github.io/eng-practices/review/reviewer/> |
| **Fowler, M.** | *Refactoring: Improving the Design of Existing Code* (2nd ed., 2018). Addison-Wesley. Context for review of refactoring changes. |

---

## Summary

Pull request best practices serve two goals: enabling effective review and
making review a high-value activity. Authors should prepare small,
single-purpose PRs with clear descriptions that explain the "why", self-review
before submission, and maintain clean commit history. Reviewers should evaluate
correctness, design, security, and tests (in that priority order), label
feedback by severity, and review promptly — ideally within four hours.
Automated checks should handle style and build verification so human reviewers
can focus on what machines cannot judge. The review conversation should be
specific, evidence-based, and professional, with both parties responding
explicitly to every comment. Speed matters: fast review cycles encourage small
PRs, which are easier to review, creating a virtuous cycle.
