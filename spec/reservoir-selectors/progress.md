# Progress Log

## 2026-01-31

### 10:00 UTC — Spec scaffolded

- Created branch `feature/reservoir-selectors`
- Created spec folder with README, learned.md, rfc.md, verification.md, implementation-plan.md
- Reviewed existing Reservoir API surface and patterns
- Verified no existing selector mechanism exists

### 10:30 UTC — RFC complete (initial)

- Documented current state vs proposed design with Mermaid diagrams
- Defined key rules (MUST/SHOULD/MAY)
- Designed API for extension methods and StoreComponent integration
- Considered alternatives and rejected them with rationale
- Outlined 4-phase rollout plan

### 11:00 UTC — Scope refined

- Clarified two-source architecture: Manual client selectors + Future Domain→Client generation
- Scoped this PR to **client-side infrastructure only**
- Updated RFC with architecture diagram showing both sources
- Updated implementation plan to mark Phases 2-4 as future
- Confirmed client can still build its own selectors for client-only or cross-state logic

### 11:30 UTC — Scope expanded

- User requested memoization be included in this PR
- User requested sample app (Spring.Client) be updated to demonstrate selectors
- Moved memoization from Phase 4 → Step 1.4
- Added Steps 1.5-1.6 for sample selector files and Index.razor.cs refactoring
- Updated test plan to include MemoizeTests
- Updated files changed summary with all new files

### 17:50 UTC — Implementation complete

- Created SelectorExtensions.cs with 1/2/3 state overloads
- Created Memoize.cs with reference-equality caching
- Updated StoreComponent.cs with Select methods
- Created SignalRConnectionSelectors.cs (framework-level selectors)
- Created EntitySelectionSelectors.cs (sample app selectors)
- Updated Index.razor.cs to use selectors
- Created SelectorExtensionsTests.cs (13 tests)
- Created MemoizeTests.cs
- Fixed CA1062 null validation in selector classes
- All builds pass with zero warnings
- All tests pass

### Status

- [x] Spec scaffolded
- [x] Learned.md populated
- [x] RFC written
- [x] Verification complete
- [x] Implementation plan detailed
- [x] Scope refined to Phase 1 + memoization + sample app
- [x] Implementation complete
- [ ] Documentation pending
- [ ] Final cleanup pending
