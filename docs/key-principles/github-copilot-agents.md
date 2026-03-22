# GitHub Copilot Custom Agents, Sub-Agents, and Extensibility

When an AI coding assistant operates inside an IDE, its effectiveness is bounded
by context: what it knows about the codebase, the team's conventions, and the
task at hand. GitHub Copilot's agent mode, combined with custom agents,
sub-agents, instructions, and skills, allows teams to extend that context
boundary — building purpose-built AI workflows that carry domain knowledge, use
tools, and delegate to specialised sub-agents.

**This document is written in Minto Pyramid format.**

---

## Governing Thought

GitHub Copilot's extensibility model — custom agents, sub-agents, instruction
files, and skills — enables teams to build domain-specific AI workflows inside
VS Code that operate autonomously, delegate tasks, use tools, and follow project
conventions. This document provides sufficient detail to build custom agents from
scratch.

---

## Situation

GitHub Copilot in VS Code provides agent mode (agentic chat) that can
autonomously plan, execute terminal commands, edit files, and iterate on
problems. Out of the box, it uses built-in tools (file editing, terminal, search)
and follows a general-purpose system prompt.

## Complication

General-purpose behaviour is insufficient for teams with specific conventions,
multi-step workflows, specialised domains, or the need to orchestrate multiple
AI personas for different parts of a task. Without customisation, every
conversation starts from zero context about the team's rules, patterns, and
preferred approaches.

## Question

How can a team extend Copilot to carry persistent domain knowledge, follow
project-specific rules, use custom tools, delegate to specialised sub-agents,
and operate as a purpose-built workflow engine?

---

## Key-Line 1: The Extensibility Architecture

GitHub Copilot's customisation in VS Code is layered. Each layer adds more
specificity and capability:

```text
┌──────────────────────────────────────────────────┐
│  Layer 4: Custom Agents (.agent.md)              │
│  Purpose-built personas with tools and handoffs  │
├──────────────────────────────────────────────────┤
│  Layer 3: Skills (SKILL.md + tools)              │
│  Reusable tool bundles agents can reference      │
├──────────────────────────────────────────────────┤
│  Layer 2: Instruction Files (.instructions.md)   │
│  Scoped rules auto-attached by glob pattern      │
├──────────────────────────────────────────────────┤
│  Layer 1: Copilot Instructions                   │
│  Global repo/org-level context                   │
└──────────────────────────────────────────────────┘
```

### Layer 1 — Copilot Instructions (Global Context)

A file at `.github/copilot-instructions.md` provides global context that is
automatically included in every Copilot chat interaction in the repository.

- **Location**: `.github/copilot-instructions.md`
- **Scope**: All Copilot interactions in the repository
- **Format**: Markdown
- **Purpose**: High-level project context, coding standards, architectural
  overview, tech stack description

This is the simplest form of customisation. It requires no special syntax —
just Markdown content describing how the project works and what conventions to
follow.

### Layer 2 — Instruction Files (Scoped Rules)

Instruction files are Markdown files with YAML frontmatter that are
**automatically attached** to Copilot's context when the user is working on
files matching a glob pattern.

- **Location**: `.github/instructions/` directory
- **Extension**: `.instructions.md`
- **Frontmatter**: YAML with `applyTo` glob pattern

#### File Structure

```markdown
---
applyTo: "**/*.cs"
---

# C# Coding Standards

- Use `private Type Name { get; }` for injected dependencies.
- Never inject `IServiceProvider` directly.
- Use LoggerExtensions for all logging.
```

#### The `applyTo` Glob

The `applyTo` field determines when the instruction file is included in context:

| Pattern | Effect |
|---|---|
| `**` | Applies to all files (global rule) |
| `**/*.cs` | Applies when editing C# files |
| `src/Brooks/**` | Applies when editing files under src/Brooks |
| `tests/**` | Applies when editing test files |

When a user opens or edits a file matching the glob, the instruction content is
automatically injected into Copilot's context. Multiple instruction files can
apply simultaneously.

#### Design Guidance for Instructions

- Keep each file focused on one concern (logging, testing, serialization).
- Use RFC 2119 keywords (MUST, SHOULD, MAY) for clarity of obligation.
- Include "Why" rationale so the AI can make correct judgement calls in
  ambiguous situations.
- Reference other instruction files rather than duplicating content.

### Layer 3 — Skills (Reusable Tool Bundles)

Skills are directories containing a `SKILL.md` file that describes a reusable
capability — typically wrapping one or more tools (MCP servers, VS Code
commands, terminal operations) into a coherent, documented action that agents
can invoke.

- **Location**: `.github/skills/<skill-name>/SKILL.md`
- **Format**: Markdown with structured sections

#### Skill File Structure

```markdown
# Skill Name

Description of what this skill does.

## When to Use

Conditions under which this skill should be invoked.

## Tools Required

- `mcp_github_create_pull_request` — Creates a PR
- `run_in_terminal` — Executes build commands

## Procedure

1. Step one
2. Step two
3. Step three

## Inputs

| Parameter | Required | Description |
|-----------|----------|-------------|
| branch    | Yes      | Branch name |

## Outputs

Description of what the skill produces.
```

Skills are referenced by agents in their `tools` or description sections, and
Copilot resolves them at invocation time.

### Layer 4 — Custom Agents (Purpose-Built Personas)

Custom agents are Markdown files that define a complete AI persona with a
specific mission, tools, instructions, and optionally, the ability to hand off
work to other agents (sub-agents).

- **Location**: `.github/agents/<agent-name>.agent.md`
- **Extension**: `.agent.md`
- **Invocation**: `@<agent-name>` in Copilot Chat

---

## Key-Line 2: Building a Custom Agent

### Agent File Anatomy

An agent file has two parts: **YAML frontmatter** (metadata) and **Markdown
body** (the system prompt).

```markdown
---
name: "My Agent Name"
description: "One-line description shown in the agent picker."
---

# My Agent Name

You are a [role description]. Your job is to [mission].

## Rules

- Rule one
- Rule two

## Workflow

1. Step one
2. Step two
```

### YAML Frontmatter Fields

| Field | Required | Type | Description |
|---|---|---|---|
| `name` | Yes | string | Display name of the agent |
| `description` | Yes | string | One-line description shown in the picker and used for routing |
| `metadata` | No | object | Arbitrary key-value pairs for organisational use |
| `handoffs` | No | array | Sub-agent delegation definitions (see Key-Line 3) |

### The Markdown Body (System Prompt)

The Markdown body **is** the system prompt. Everything in the body is sent to the
language model as instructions when the agent is invoked. This means:

- The writing style, tone, and specificity of the Markdown directly controls
  the agent's behaviour.
- Headers, lists, and formatting are preserved and influence how the model
  interprets the instructions.
- The body can reference tools, instruction files, and skills.

### Agent Design Principles

| Principle | Description |
|---|---|
| **Single responsibility** | Each agent should do one thing well. A coding agent codes. A review agent reviews. A documentation agent writes docs. |
| **Explicit mission** | State the agent's purpose in the first paragraph. Do not rely on the model inferring intent. |
| **Bounded scope** | Define what the agent may and may not modify (e.g., "only files under `tests/`"). |
| **Evidence-based** | Instruct the agent to verify claims against code, tests, or command output rather than assuming. |
| **Workflow-driven** | Define a numbered workflow the agent must follow. This prevents skipping steps. |
| **Tool-aware** | Reference specific tools (terminal, MCP, file edit) the agent should use. |

### Example: A Minimal Custom Agent

```markdown
---
name: "Test Writer"
description: "Writes unit tests for the Mississippi framework."
---

# Test Writer

You are a test-writing specialist for the Mississippi framework.

## Mission

Write comprehensive L0 unit tests for production code changes.

## Constraints

- Only create or modify files under `tests/`.
- Do not modify production code.
- All tests must be deterministic (no sleeps, no real network).
- Use FakeTimeProvider for time-dependent tests.

## Workflow

1. Read the production code that needs testing.
2. Identify all branches, edge cases, and error paths.
3. Write tests covering each path.
4. Run the tests: `dotnet test`.
5. Verify zero warnings in the test build.
```

---

## Key-Line 3: Sub-Agents and Handoffs

### What Sub-Agents Are

Sub-agents are other custom agents that an agent can **delegate work to**. This
enables multi-agent workflows where a coordinator agent breaks a task into parts
and hands each part to a specialist.

### Defining Handoffs

Handoffs are defined in the YAML frontmatter of the delegating agent:

```yaml
---
name: "Coordinator"
description: "Coordinates multi-step implementation tasks."
handoffs:
  - label: "Write Tests"
    agent: test-writer
    prompt: "Write tests for the following changes: "
    send: false
  - label: "Write Docs"
    agent: technical-writer
    prompt: "Document the following new feature: "
    send: false
---
```

### Handoff Fields

| Field | Required | Type | Description |
|---|---|---|---|
| `label` | Yes | string | Button label shown to the user |
| `agent` | Yes | string | Name of the target agent file (without `.agent.md`) |
| `prompt` | No | string | Pre-filled prompt text for the handoff |
| `send` | No | boolean | If `true`, the handoff is sent automatically. If `false` (default), the user can review and edit before sending. |

### How Handoffs Work at Runtime

1. The delegating agent decides a sub-task should be handed off.
2. Copilot presents the handoff as a clickable action in the chat.
3. If `send: false`, the user sees the pre-filled prompt and can edit it.
4. If `send: true`, the handoff is executed immediately.
5. The target agent receives the prompt and executes in its own context with its
   own system prompt.
6. The target agent's response is returned to the conversation.

### Designing Multi-Agent Workflows

```text
┌─────────────────────────┐
│    Coordinator Agent     │
│  (plans, delegates)     │
└──┬──────────┬───────────┘
   │          │
   ▼          ▼
┌────────┐ ┌──────────┐
│ Coder  │ │ Reviewer  │
│ Agent  │ │ Agent     │
└────────┘ └──────────┘
```

Design rules for multi-agent workflows:

- **Keep handoff prompts specific**: Include all context the sub-agent needs.
  Do not assume the sub-agent has access to the parent conversation.
- **Each sub-agent is stateless**: A sub-agent invocation starts fresh. It does
  not inherit the parent agent's conversation history.
- **Prefer `send: false`** for high-stakes operations so the user can review.
- **Use `send: true`** only for low-risk, well-understood delegations.

---

## Key-Line 4: The Chain of Verification Pattern in Agents

A particularly effective agent design pattern is the **Chain of Verification
(CoVe)**, derived from Meta AI's research (arXiv:2309.11495). This pattern
structures the agent's workflow to systematically verify its own output before
finalising.

### The Pattern Applied to Agents

```markdown
## Mandatory Workflow

1. **Initial Draft** — Produce a plan or implementation.
2. **Verification Questions** — Generate 5–10 questions that would
   expose errors in the draft.
3. **Independent Answers** — Answer each question using repository
   evidence (code, tests, commands), NOT the draft itself.
4. **Final Revised Output** — Revise the draft based on verified answers.
```

### Why It Works in Agent Context

- Agents hallucinate less when forced to verify against evidence.
- The "independent answers" step breaks the self-reinforcement loop where the
  model trusts its own draft.
- Verification questions can be answered by running commands, reading files, or
  checking test output — all tools available to agents.

### Real-World Example

The Mississippi repository uses this pattern in its `CoV-mississippi-coding`
agent:

```text
1) Initial draft — Restate requirements, propose plan, list claims
2) Verification questions (5–10) — Questions answerable via repo evidence
3) Independent answers — Re-derive from evidence, cite file paths
4) Final revised plan — Highlight changes from initial draft
5) Implementation — Only after verification is complete
```

---

## Key-Line 5: Instruction Files in Depth

### Automatic Context Injection

Instruction files under `.github/instructions/` are not passive documentation.
They are **actively injected** into the model's context when:

- The user is editing a file matching the `applyTo` glob.
- An agent is working on files matching the glob.

This means instruction files function as **persistent, scoped rules** that the
AI follows without the user needing to repeat them.

### Organisation Strategies

| Strategy | Description |
|---|---|
| **By concern** | `logging-rules.instructions.md`, `testing.instructions.md`, `serialization.instructions.md` |
| **By area** | `brooks.instructions.md`, `inlet.instructions.md` |
| **By audience** | `shared-policies.instructions.md` (everyone), `mutation-testing.instructions.md` (agents running mutation) |

### Composability

Instruction files compose naturally:

- A file with `applyTo: "**"` applies to everything (shared guardrails).
- A file with `applyTo: "tests/**"` adds testing-specific rules.
- A file with `applyTo: "src/Brooks/**"` adds Brooks-specific rules.

When a user edits `tests/Brooks.Runtime.L0Tests/SomeTest.cs`, all three
instruction files apply simultaneously. The model sees the union of all
matching instructions.

### Writing Effective Instructions

1. **State the governing thought** — What is the one principle this file
   enforces?
2. **Use RFC 2119 keywords** — MUST, SHOULD, MAY, MUST NOT, SHOULD NOT. These
   are unambiguous to both humans and language models.
3. **Include rationale** — Every rule should have a "Why:" explanation. Without
   rationale, the model cannot make correct judgement calls in edge cases.
4. **Reference, do not duplicate** — If a rule exists in `shared-policies`,
   reference it rather than restating it.
5. **Keep files focused** — One concern per file. A 50-line focused instruction
   file is more effective than a 500-line omnibus.

---

## Key-Line 6: Practical Setup Guide

### Directory Structure

```text
.github/
├── copilot-instructions.md          # Global repo context
├── instructions/
│   ├── shared-policies.instructions.md
│   ├── testing.instructions.md
│   ├── logging-rules.instructions.md
│   └── ...
├── agents/
│   ├── dev.agent.md
│   ├── technical-writer.agent.md
│   ├── CoV-coding.agent.md
│   └── ...
└── skills/
    └── build-and-test/
        └── SKILL.md
```

### Step-by-Step: Creating Your First Agent

1. **Create the file**: `.github/agents/my-agent.agent.md`
2. **Add frontmatter**:

   ```yaml
   ---
   name: "My Agent"
   description: "What this agent does in one line."
   ---
   ```

3. **Write the system prompt** as Markdown body:
   - State the role and mission
   - Define constraints and scope
   - Specify the workflow (numbered steps)
   - Reference tools and instruction files
4. **Test it**: Open Copilot Chat, type `@my-agent` followed by a task.
5. **Iterate**: Refine the prompt based on observed behaviour.

### Step-by-Step: Creating an Instruction File

1. **Create the file**: `.github/instructions/my-rules.instructions.md`
2. **Add frontmatter with glob**:

   ```yaml
   ---
   applyTo: "src/**/*.cs"
   ---
   ```

3. **Write the rules** as Markdown body.
4. **Test it**: Edit a file matching the glob and observe whether Copilot
   follows the rules.

### Step-by-Step: Adding a Sub-Agent Handoff

1. **Ensure the target agent exists** (e.g., `.github/agents/reviewer.agent.md`).
2. **Add `handoffs` to the parent agent's frontmatter**:

   ```yaml
   handoffs:
     - label: "Review Changes"
       agent: reviewer
       prompt: "Review the following changes for correctness: "
       send: false
   ```

3. **Test it**: Invoke the parent agent and trigger the workflow path that
   leads to delegation.

---

## Key-Line 7: Advanced Patterns

### Pattern: Workflow Agent with Multiple Handoffs

An agent that orchestrates a complete development workflow:

```yaml
---
name: "Flow Builder"
description: "Orchestrates feature implementation end-to-end."
handoffs:
  - label: "Implement Code"
    agent: dev
    prompt: "Implement the following specification: "
    send: false
  - label: "Write Tests"
    agent: test-writer
    prompt: "Write tests for: "
    send: false
  - label: "Write Documentation"
    agent: technical-writer
    prompt: "Document the following feature: "
    send: false
  - label: "Review PR"
    agent: reviewer
    prompt: "Review this PR for: "
    send: false
---
```

### Pattern: Evidence-Gated Agent

An agent that refuses to act without evidence:

```markdown
## Hard Rule

Before implementing any change, you MUST:

1. Read the relevant source files.
2. Read the relevant test files.
3. Run the existing tests to confirm they pass.

If any test fails before your change, STOP and report the failure.
Do not make changes on top of a broken baseline.
```

### Pattern: Metadata-Driven Agent Families

Using the `metadata` field to create agent families:

```yaml
metadata:
  family: epic
  role: builder
  workflow: plan-driven-execution
  pair: "epic Planner"
```

This enables tooling to discover and organise related agents programmatically.

---

## Common Pitfalls

| Pitfall | Consequence | Prevention |
|---|---|---|
| Overly broad agent scope | Agent attempts tasks outside its competence | Define explicit scope boundaries in the system prompt |
| Missing workflow steps | Agent skips verification or testing | Use numbered mandatory workflows with "do not skip" language |
| Vague instructions | Agent makes inconsistent decisions | Use RFC 2119 keywords and concrete examples |
| Sub-agent without context | Sub-agent lacks information to complete the task | Include all necessary context in the handoff prompt |
| Duplicated rules across files | Rules drift out of sync | Use a shared-policies file and reference it |
| Instruction file too large | Model struggles to prioritise | Keep each file under 100 lines, focused on one concern |

---

## Authoritative Sources

| Source | Reference |
|---|---|
| **VS Code Custom Agents** | *Custom chat agents*. VS Code documentation. <https://code.visualstudio.com/docs/copilot/copilot-extensibility-overview> |
| **VS Code Custom Instructions** | *Custom instructions*. VS Code documentation. <https://code.visualstudio.com/docs/copilot/copilot-customization> |
| **VS Code Sub-Agents** | *Sub-agents*. VS Code documentation. <https://code.visualstudio.com/docs/copilot/copilot-extensibility-overview#_subagents> |
| **GitHub Copilot Customization** | *Customizing Copilot*. GitHub documentation. <https://docs.github.com/en/copilot/customizing-copilot> |
| **Chain of Verification** | Dhuliawala, S. et al. (2023). *Chain-of-Verification Reduces Hallucination in Large Language Models*. arXiv:2309.11495 |
| **Mississippi Repository** | Real-world examples under `.github/agents/` and `.github/instructions/` in this repository |

---

## Summary

GitHub Copilot's extensibility model consists of four layers: global copilot
instructions, scoped instruction files with glob-based auto-injection, reusable
skills, and custom agents with optional sub-agent handoffs. Agents are Markdown
files with YAML frontmatter that define a persona, mission, constraints, and
workflow. Sub-agents enable multi-agent orchestration through handoff
definitions. Instruction files provide persistent, scoped rules that are
automatically injected based on which files the user is editing. The Chain of
Verification pattern, when embedded in agent workflows, reduces hallucination by
forcing evidence-based self-verification. Together, these mechanisms allow teams
to build domain-specific AI workflows that carry project knowledge, enforce
conventions, and delegate to specialised sub-agents.
