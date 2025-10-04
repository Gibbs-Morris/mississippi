We follow the Microsoft C# coding conventions.
When generating or refactoring code, always match those conventions—including file‑scoped namespaces, expression‑bodied members where beneficial, and the latest nullable‑reference guidelines.

Before answering questions about how to run or use this project, read **README.md** at the repository root.
Assume that usage examples, environment variables, and public API surfaces documented there are the single source of truth.

---

### Build & tidy

*To build, restore dependencies, and run all tests, call **`./build.ps1`** from the repo root.  
* To format / tidy code you generate or modify, finish with **`./cleanup.ps1`**.  
  – This script applies only the repository’s agreed housekeeping steps; do **not** assume extra formatters unless they’re explicitly referenced here.

---

### Dependency / project‑file rules

*Repository‑wide MSBuild settings live in **`Directory.Build.props`**; NuGet package versions live in **`Directory.Packages.props`**. Always inspect these files before touching any `.csproj`. :contentReference[oaicite:3]{index=3}  
* When adding or removing NuGet packages, **use the `dotnet add|remove package` CLI**. With Central Package Management enabled, the command updates `Directory.Packages.props` for versions and adds versionless `<PackageReference>` items to the project file automatically. :contentReference[oaicite:4]{index=4}  
* If a package version already exists in `Directory.Packages.props`, **never** repeat the `Version` attribute in a project’s `<PackageReference>` element—let CPM supply it (avoids NU1008/NU1010 warnings). :contentReference[oaicite:5]{index=5}

---

### Architectural preferences

***Cloud‑native** by default – 12‑Factor, container‑first, stateless processes, health probes, structured logging, metrics & traces.  
* Embrace **CQRS** combined with an **actor‑model** or other message‑driven runtime for complex or high‑concurrency domains.  
*Prefer **immutable‑by‑design** types (records, readonly properties/collections, value objects, functional helpers).  
* The primary persistence layer is **NoSQL** (document or key‑value) unless a relational model is explicitly required.  
* All code must honour **SOLID** principles (SRP, OCP, LSP, ISP, DIP).

---

### Testing expectations

*Unit tests **must use xUnit** and FluentAssertions.  
* Target **≥ 80 % line / branch coverage** across all production assemblies (use Coverlet or equivalent).  
*Run **Stryker.NET** mutation testing and achieve **≥ 80 % mutation score** (kill ≥ 80 % of mutants). Surface the Stryker report in build output.  
* Place tests under `/tests` and name files `<TypeUnderTest>.Tests.cs`, covering both “happy path” and edge cases.

---

### Copilot Chat / Search behaviour

* When suggesting code or docs, respect every rule above: architectural, dependency, and testing.  
* When choosing symbols to show, prioritise public APIs surfaced in README.md; avoid private helpers unless explicitly requested.  
* Respond concisely, but when referencing repository code, include file paths and line numbers for clarity.
* When a request spans many small, independent fixes, stage work via `.scratchpad/tasks` and claim tasks using atomic moves as defined in `.github/instructions/agent-scratchpad.instructions.md`. Do not reference `.scratchpad/` from source or tests; it is ephemeral and ignored by Git.
