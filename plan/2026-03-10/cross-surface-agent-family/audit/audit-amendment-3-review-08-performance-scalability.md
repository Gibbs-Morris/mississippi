# Amendment 3 Review — Performance & Scalability Engineer

## Persona

Performance & Scalability Engineer — hot-path allocation budgets, throughput, latency, backpressure, memory pressure, benchmark-ability.

## Findings

### 1. FLAW — Prompt size budget is unaddressed

- **Issue**: The plan requires embedding three Mermaid diagrams in entry-agent prompts, plus the full 8-section prompt template (Mission, Read first, Scope, Out of scope, Workflow, Evidence to gather, Output contract, Guardrails), plus working-directory instructions, plus subagent guardrails, plus family-specific workflow details. Each entry agent's prompt body could easily exceed 3000-4000 words. Combined with the system prompt, conversation history, and tool schemas, this pushes against context-window limits.
- **Why it matters**: Long prompts reduce the effective context window available for actual work. On GitHub.com with potentially smaller context windows, this could cause instruction truncation.
- **Proposed change**: Add a "Prompt Budget" note: "Entry-agent prompt bodies should target ≤3000 words. If embedding all three diagrams exceeds this, embed the end-to-end workflow diagram and reference the manifest for the others. Specialist prompts should target ≤1500 words."
- **Evidence**: Prompt-Embedding Priority section says "in compact form whenever prompt length allows" and "if compression is needed, preserve these three" but doesn't set a concrete budget.
- **Confidence**: High.

### 2. GAP — No guidance on specialist invocation cost vs. value

- **Issue**: The conditional invocation guidance exists but is purely domain-based ("invoke distributed-systems when concurrency risks are present"). It doesn't consider cost-value: invoking 12 specialists for a 20-line config change is wasteful.
- **Why it matters**: Token cost and latency scale linearly with specialist count. For small tasks, the overhead may exceed the value.
- **Proposed change**: Add a cost-value heuristic: "For tasks affecting fewer than 5 files or fewer than 100 lines of change, the entry agent should narrow the specialist set to at most 5 relevant specialists. For larger tasks, the full routing set is appropriate."
- **Evidence**: Conditional invocation guidance section and Default Entry-to-Specialist Routing.
- **Confidence**: Medium — the exact numbers are debatable, but the principle of proportional specialist invocation is sound.

### 3. MINOR — 18 specialist agents means 18 `.agent.md` files loaded into context

- **Issue**: VS Code loads all workspace agents. With 18 specialist files plus 3 entry files plus an existing set of agents, the tool and agent discovery overhead grows.
- **Why it matters**: More agents in the workspace means more metadata in tool schemas and potentially slower agent discovery.
- **Proposed change**: None for v1 — this is an inherent tradeoff of the family-based architecture and the 128-tool cap note already addresses the worst case. Monitor after implementation.
- **Evidence**: Tools note mentions the 128-tool cap.
- **Confidence**: Low.
