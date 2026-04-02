# Clean Squad Platform Feature Coverage

This matrix records how Clean Squad uses current VS Code Copilot customization features.

| Feature | Classification | Clean Squad usage |
|---|---|---|
| Custom agents | Core | Every `cs-*.agent.md` file |
| `user-invocable` / `disable-model-invocation` | Core | Public boundary hardening and protected workers |
| `agents:` allowlists | Core | River and approved phase coordinators |
| Handoffs | Core | One-way `cs Entrepreneur` -> `cs River Orchestrator` handoff |
| Skills | Core | Repeated procedures moved into `.github/skills/` |
| Prompt files | Core | Bounded operator launchers under `.github/prompts/` |
| Tool sets | Optional ergonomic layer | Current platform uses profile-level `.toolsets.jsonc`; repo ships a template and guidance |
| Hooks | Preview pilot | Non-destructive `PostToolUse` reminder only |
| Agent-scoped hooks | Out of scope for this slice | Avoided to keep the pilot narrow |
| MCP servers / plugins | Optional extension | Documented as compatible but not required |
| Recursive coordinators | Out of scope for this slice | No Clean Squad role currently uses self-recursive `agents:` |

## Notes

- `WORKFLOW.md` remains the only authoritative roster and workflow contract.
- `customization-manifest.json` is subordinate parity enforcement.
- Optional richer environments should improve ergonomics without becoming a mandatory dependency.
