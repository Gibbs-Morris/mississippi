---
name: clean-squad-skills-map
description: 'Consumer map for the Clean Squad skill pack. Use when maintaining Clean Squad skill ownership, consumer links, or procedural reuse boundaries.'
user-invocable: false
---

# Clean Squad Skills Consumer Map

These skills are reusable procedure packs for Clean Squad agents. They are **not** the authority surface for governance; `WORKFLOW.md`, `clean-squad.instructions.md`, and the agent contracts remain authoritative.

| Skill | Purpose | Primary consumers |
|---|---|---|
| `clean-squad-delegation` | File-first delegation, bounded output paths, status-envelope rules, and artifact validation | River, Scribe, PR Manager, every specialist that writes `.thinking/` artifacts |
| `clean-squad-subagent-orchestration` | `agents:` allowlists, deterministic nested batches, coordinator boundaries, and disabled-mode fallback | River, Three Amigos Synthesizer, Code Review Synthesizer, QA Synthesizer, Documentation Scope Synthesizer |
| `clean-squad-discovery` | Five-question batching, first-principles discovery, and CoV-backed intake loops | Entrepreneur, River, Requirements Analyst, Discovery Synthesizer, Three Amigos participants |
| `clean-squad-synthesis` | Deterministic fan-in, deduplication, conflict preservation, and synthesis artifact shaping | Plan Synthesizer, Three Amigos Synthesizer, Code Review Synthesizer, QA Synthesizer, Scribe, Merge Readiness Evaluator |
| `clean-squad-delivery-quality` | Increment discipline, validation expectations, review and QA loops, and commit hygiene | Lead Developer, Test Engineer, Commit Guardian, QA Lead, QA Exploratory, DevOps Engineer |
| `clean-squad-documentation-governance` | Documentation scope rules, ADR/C4 interplay, doc review expectations, and skip criteria | Documentation Scope Synthesizer, Technical Writer, Doc Reviewer, ADR Keeper, C4 Diagrammer |

## Maintenance rules

1. Keep skill names lowercase kebab-case and aligned to their folder names.
2. Link each consuming agent to its relevant skill files directly from the agent body.
3. Move repeated procedures into skills, but keep authority, ownership, and public-boundary rules in `WORKFLOW.md` and agent contracts.
4. Keep skill content secret-free and portable across VS Code, Copilot CLI, and Copilot coding agent.
