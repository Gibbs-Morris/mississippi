# Copilot Instructions

- For `.Abstractions` suffixes, follow `abstractions-projects.instructions.md`—create the abstractions project when the required conditions apply.
- Agents **MUST** follow Microsoft C# conventions (file-scoped namespaces, expression-bodied members when beneficial, nullable guidance).
- Before answering usage/run questions, Agents **MUST** read `README.md` and treat it as the authoritative source.

## Build & Tidy

- **MUST** build/test with `pwsh ./go.ps1`.
- **MUST** finish with `pwsh ./clean-up.ps1`; **MUST NOT** assume additional formatters beyond those scripts.

## Dependency / Project Rules

- Repository-wide MSBuild settings live in `Directory.Build.props`; package versions live in `Directory.Packages.props`—Agents **MUST** review these before editing any `.csproj`.
- Use `dotnet add|remove package`; with CPM, **MUST NOT** specify `Version` attributes in project files when a version exists in `Directory.Packages.props`.

## Architectural Preferences

- Default to cloud-native, 12-factor, container-first designs with structured logging/metrics.
- Prefer CQRS + actor/message-driven runtimes for complex domains; favor immutable-by-design types (records, readonly members).
- Primary persistence is NoSQL unless a relational model is explicitly required.
- All code **MUST** honor SOLID principles.

## Testing Expectations

- Unit tests **MUST** use xUnit and FluentAssertions; target ≥80% line/branch coverage, covering happy and edge paths. Tests belong under `/tests` and **SHOULD** be named `<TypeUnderTest>.Tests.cs`.
- Agents **SHOULD** run Stryker.NET and aim for ≥80% mutation score.

## C# Code Quality & SOLID Verification

- After each C# change, Agents **MUST** verify SRP/OCP/LSP/ISP/DIP and fix violations immediately, adjusting design before proceeding.
- Review responsibilities, dependencies, and interfaces to ensure separation of concerns.

## Copilot Chat / Search Behaviour

- Responses **MUST** respect all rules above (architecture, dependency, testing). Prefer public APIs documented in `README.md` when showing symbols.
- Respond concisely with file paths/line references; for many small fixes, stage work via `.scratchpad/tasks` per `agent-scratchpad.instructions.md`.
- **MUST NOT** reference `.scratchpad/` from source or tests.
