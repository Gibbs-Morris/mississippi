# Copilot Instructions

Agents MUST read the relevant `.github/instructions/*.instructions.md` files before working, treat those documents as the single source of truth for repository rules, and avoid duplicating their content elsewhere in order to prevent drift. Before declaring any task complete (including answering a question or finalizing code edits), Agents MUST re-review the applicable instruction files and explicitly ensure their work complies with every rule—use this as a mandatory fail-safe verification step for every response.

Agents MUST follow the Microsoft C# coding conventions.
When generating or refactoring code, Agents MUST match those conventions—including
file‑scoped namespaces, expression‑bodied members where beneficial, and the
latest nullable‑reference guidelines.
Why: Ensures generated code integrates cleanly and remains consistent with repository standards.

Before answering questions about how to run or use this project, Agents MUST read
**README.md** at the repository root, and MUST treat usage examples, environment variables,
and public API surfaces documented there as the single source of truth.
Why: Prevents drift and ensures guidance reflects the authoritative documentation.

---

## Build & tidy

* To build, restore dependencies, and run all tests, Agents MUST call **`pwsh ./go.ps1`**
  from the repo root.  
  Why: This orchestrates build/test gates and catches regressions consistently.
* To format / tidy code you generate or modify, Agents MUST finish with
  **`pwsh ./clean-up.ps1`**.  
  Why: Applies the repository's canonical housekeeping steps for consistent results.
  – Agents MUST NOT assume extra formatters unless they are explicitly referenced here.
  Why: Avoids unintended formatting changes and keeps tooling deterministic.

---

## Dependency / project‑file rules

* Repository‑wide MSBuild settings live in **`Directory.Build.props`**;
  NuGet package versions live in **`Directory.Packages.props`**.
  Agents MUST inspect these files before touching any `.csproj`.  
  Why: Central Package Management (CPM) and shared props centralize configuration and prevent duplication.
* When adding or removing NuGet packages, Agents MUST use the
  `dotnet add|remove package` CLI. With Central Package Management enabled,
  the command updates `Directory.Packages.props` for versions and adds
  versionless `<PackageReference>` items to the project file automatically.  
  Why: Ensures versions are centralized and project files remain versionless.
* If a package version already exists in `Directory.Packages.props`, Agents MUST NOT
  repeat the `Version` attribute in a project's `<PackageReference>`
  element—let CPM supply it.  
  Why: Avoids NU1008/NU1010 warnings and guarantees a single source of truth for versions.

---

## Architectural preferences

* **Cloud‑native** by default – 12‑Factor, container‑first, stateless processes,
  health probes, structured logging, metrics & traces.
* Embrace **CQRS** combined with an **actor‑model** or other message‑driven
  runtime for complex or high‑concurrency domains.
* Prefer **immutable‑by‑design** types (records, readonly properties/collections,
  value objects, functional helpers).
* The primary persistence layer is **NoSQL** (document or key‑value) unless
  a relational model is explicitly required.
* All code MUST honour **SOLID** principles (SRP, OCP, LSP, ISP, DIP).  
  Why: Upholds maintainability and design quality across the codebase.

---

## Testing expectations

* Unit tests MUST use xUnit and FluentAssertions.  
  Why: Standardizes test frameworks and assertions for reliable tooling.
* Agents SHOULD target **≥ 80 % line / branch coverage** across all production assemblies
  (use Coverlet or equivalent).  
  Why: Encourages meaningful coverage without over-constraining contributors.
* Agents SHOULD run **Stryker.NET** mutation testing and achieve **≥ 80 % mutation score**
  (kill ≥ 80 % of mutants). Surface the Stryker report in build output.  
  Why: Validates test quality by ensuring assertions catch injected faults.
* Agents MUST place tests under `/tests` and name files `<TypeUnderTest>.Tests.cs`,
  covering both "happy path" and edge cases.  
  Why: Improves discoverability and consistency across solutions.

---

## Copilot Chat / Search behaviour

* When suggesting code or docs, Agents MUST respect every rule above: architectural,
  dependency, and testing.  
  Why: Keeps AI suggestions aligned with repository policies.
* When choosing symbols to show, Agents SHOULD prioritise public APIs surfaced in README.md;
  avoid private helpers unless explicitly requested.  
  Why: Highlights stable, user-facing contracts first.
* Agents SHOULD respond concisely, and when referencing repository code, include file paths
  and line numbers for clarity.  
  Why: Improves traceability and reviewability of suggestions.
* When a request spans many small, independent fixes, Agents SHOULD stage work via
  `.scratchpad/tasks` and claim tasks using atomic moves as defined in
  `.github/instructions/agent-scratchpad.instructions.md`.  
  Why: Enables incremental, reviewable progress.
* Agents MUST NOT reference `.scratchpad/` from source or tests; it is ephemeral
  and ignored by Git.  
  Why: Prevents accidental dependencies on temporary workspace artifacts.
