---
name: clean-squad-synthesis
description: 'Synthesis guidance for Clean Squad. Use when deduplicating multiple specialist outputs, preserving conflicts, or producing one deterministic fan-in artifact.'
user-invocable: false
---

# Clean Squad Synthesis

Use this skill whenever a Clean Squad agent joins multiple specialist outputs.

## Procedure

1. Read every input artifact before writing conclusions.
2. Preserve real disagreements; do not flatten them into false consensus.
3. Deduplicate overlapping findings aggressively.
4. Keep fan-in ordering deterministic by declared worker roster.
5. Separate blockers from lower-signal improvements.
6. Produce one synthesis artifact that downstream steps can consume directly.

## Output checklist

- Governing thought
- Shared conclusions
- Material risks or blockers
- Conflicts requiring River or maintainer attention
- Recommended next action

## Anti-drift rule

Do not invent new evidence, new requirements, or new findings that are absent from the joined input set.
