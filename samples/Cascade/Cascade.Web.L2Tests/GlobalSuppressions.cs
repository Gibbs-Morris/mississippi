using System.Diagnostics.CodeAnalysis;


// xUnit requires test-related types to be public: test base classes, collection definitions,
// and fixtures must be accessible for the test runner. Page objects are used by public tests.
// Suppress CA1515 for the entire test assembly since test infrastructure requires public types.
[assembly:
    SuppressMessage(
        "Performance",
        "CA1515:Consider making public types internal",
        Justification = "xUnit requires public test base classes, collection definitions, and fixtures.",
        Scope = "namespaceanddescendants",
        Target = "~N:Cascade.Web.L2Tests")]

// BaseUrl is kept as string for simpler test usage with Playwright's GotoAsync.
// Converting to Uri adds ceremony without benefit in test code.
[assembly:
    SuppressMessage(
        "Design",
        "CA1056:URI properties should not be strings",
        Justification = "Simpler for Playwright test usage - GotoAsync accepts strings.",
        Scope = "member",
        Target = "~P:Cascade.Web.L2Tests.PlaywrightFixture.BaseUrl")]

// PlaywrightFixture manages Playwright/Browser/App lifetime via IAsyncLifetime.DisposeAsync.
// The analyzer cannot see the async disposal pattern correctly.
[assembly:
    SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP002:Dispose member",
        Justification = "Disposed in DisposeAsync - analyzer does not recognize IAsyncLifetime pattern.",
        Scope = "type",
        Target = "~T:Cascade.Web.L2Tests.PlaywrightFixture")]

// Fields are null initially and only assigned once in InitializeAsync.
[assembly:
    SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP003:Dispose previous before re-assigning",
        Justification = "Fields are null initially and assigned once in InitializeAsync.",
        Scope = "type",
        Target = "~T:Cascade.Web.L2Tests.PlaywrightFixture")]

// DistributedApplicationTestingBuilder.CreateAsync returns an object whose lifetime is managed
// by the BuildAsync result (DistributedApplication). The builder itself does not need disposal.
[assembly:
    SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Builder lifetime managed by BuildAsync result - builder pattern, not disposal pattern.",
        Scope = "member",
        Target = "~M:Cascade.Web.L2Tests.PlaywrightFixture.InitializeAsync~System.Threading.Tasks.Task")]