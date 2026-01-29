# Learned

## Repository facts
- `.github/copilot-instructions.md` not found in repo root (searched by name).
- Docs under `docs/` do not mention `Add{App}Inlet`, `GenerateInletClientComposite`, `AddInletBlazorSignalR`, or projection DTO registration (search returned no matches).
- `InletClientCompositeGenerator` now emits an overload with `Action<InletBlazorSignalRBuilder>? configureSignalR` and invokes it after defaults.
- Default `Add{App}Inlet()` forwards to the overload with `null`.

## Code touchpoints (verified)
- `src/Inlet.Client.Generators/InletClientCompositeGenerator.cs`
- `src/Inlet.Client/InletBlazorSignalRBuilder.cs`
- `tests/Inlet.Client.Generators.L0Tests/InletClientCompositeGeneratorTests.cs`
- `samples/Spring/Spring.Client/Program.cs`
