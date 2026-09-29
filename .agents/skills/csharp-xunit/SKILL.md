---
name: csharp-xunit
description: Write or review C# xUnit v3 tests, including facts, theories, assertions, and fixtures. Use for xUnit test work; skip for other test frameworks.
---

# C# xUnit tests

Use xUnit to test observable behavior with focused, independent cases. Before editing,
read the repository's testing and C# guidance, then inspect nearby tests and the
target project's test runner. Those sources govern project names, package choices,
test levels, and commands.

## Shape the test

- Put the test in the level that matches its dependencies. In Mississippi, new
  pure tests belong in `<ProductionProjectName>.L0Tests` (for example,
  `Widget.Abstractions` maps to `Widget.Abstractions.L0Tests`); do not insert
  the class name into the project name. Infrastructure contracts belong in L2,
  and browser journeys belong in L3. Smoke is a suite within a level.
- Use `[Fact]` for one case and `[Theory]` with `[InlineData]` or `[MemberData]`
  for meaningful variants. Reach for a custom data source only when simpler
  forms make the test harder to understand.
- Arrange the needed state, act once, and assert the behavior. Cover success,
  failure, and boundary cases that matter to the contract. Keep cases isolated
  and avoid shared mutable state, real network access in L0, and sleeps.
- Use the owning project and its actual API contract. When asked for a test
  example without the parser grammar, result type, or expected values, give a
  test outline with named placeholders such as `KnownValidInput` and
  `ExpectedResult`; do not invent compiling literals, result members, or
  exception-message checks. Request the missing details before editing code.
  Assert exception messages only when they are part of the contract.
- Use xUnit `Assert` methods that express the contract, such as `Equal`,
  `Same`, `Contains`, `Throws`, and `ThrowsAsync`. Await asynchronous work and
  verify the specific exception or outcome. `Assert.Throws` already verifies
  an exception was raised, so do not add a redundant nonnull assertion. Do not
  add FluentAssertions or switch frameworks to follow a generic example.
- Use `IClassFixture<T>` or collection fixtures when expensive shared setup
  is necessary; keep mutable test state per case. Use `IAsyncLifetime` for
  asynchronous setup and teardown. Inject `FakeTimeProvider` when production
  code uses `TimeProvider`.

## Verify the result

Use this repository's canonical test script and its nonempty execution and TRX
checks. For a focused Mississippi test project, run
`pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject <Name> -SkipMutation`.
For Spring integration or browser tests, follow `samples/Spring/TESTING.md` and
`test-spring.ps1`; browser journeys belong in `Spring.L3Tests`. Report what ran
and any missing coverage; a compiled test is not evidence that it executed.

Adapted from the [awesome-copilot csharp-xunit skill](https://github.com/github/awesome-copilot/blob/997e95a6e42869c350f8ca6ec4c066287697c9f0/skills/csharp-xunit/SKILL.md)
(MIT license). Repository instructions take precedence over upstream examples.
