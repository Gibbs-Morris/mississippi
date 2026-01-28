using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;

using static ArchUnitNET.Fluent.ArchRuleDefinition;


namespace Mississippi.Architecture.L0Tests;

/// <summary>
///     Architecture tests enforcing logging patterns per logging-rules.instructions.md.
/// </summary>
/// <remarks>
///     Rules enforced:
///     - LoggerExtensions classes MUST be named *LoggerExtensions.
///     - LoggerExtensions classes MUST be static and partial.
///     - LoggerExtensions classes SHOULD be internal.
///     - Injected ILogger MUST follow get-only property pattern (no underscore-prefixed fields).
/// </remarks>
public sealed class LoggingArchitectureTests : ArchitectureTestBase
{
    private static readonly IObjectProvider<Class> LoggerExtensionClasses = Classes()
        .That()
        .HaveNameEndingWith("LoggerExtensions")
        .As("LoggerExtensions classes");

    /// <summary>
    ///     Verifies that LoggerExtensions classes are internal.
    /// </summary>
    /// <remarks>
    ///     Per logging-rules.instructions.md and csharp.instructions.md: types should default to internal.
    /// </remarks>
    [Fact]
    public void LoggerExtensionsShouldBeInternal()
    {
        IArchRule rule = Classes()
            .That()
            .Are(LoggerExtensionClasses)
            .Should()
            .BeInternal()
            .Because("LoggerExtensions should be internal per logging-rules.instructions.md");
        rule.Check(ArchitectureModel);
    }

    /// <summary>
    ///     Verifies that LoggerExtensions classes are sealed (static classes are sealed).
    /// </summary>
    [Fact]
    public void LoggerExtensionsShouldBeSealed()
    {
        IArchRule rule = Classes()
            .That()
            .Are(LoggerExtensionClasses)
            .Should()
            .BeSealed()
            .Because("LoggerExtensions must be static (sealed) per logging-rules.instructions.md");
        rule.Check(ArchitectureModel);
    }

    /// <summary>
    ///     Verifies that LoggerExtensions classes follow the naming convention.
    /// </summary>
    [Fact]
    public void LoggerExtensionsShouldFollowNamingConvention()
    {
        IArchRule rule = Classes()
            .That()
            .Are(LoggerExtensionClasses)
            .Should()
            .HaveNameEndingWith("LoggerExtensions")
            .Because("logging classes must be suffixed LoggerExtensions per logging-rules.instructions.md");
        rule.Check(ArchitectureModel);
    }

    /// <summary>
    ///     Verifies that logger fields do not use underscore prefix (should be get-only properties).
    /// </summary>
    /// <remarks>
    ///     Per logging-rules.instructions.md: "Injected ILogger must follow the get-only property pattern."
    ///     Per csharp.instructions.md: "private fields/locals MUST be camelCase with no underscore."
    ///     This test detects underscore-prefixed logger fields which violate both rules.
    /// </remarks>
    [Fact]
    public void LoggerFieldsShouldNotHaveUnderscorePrefix()
    {
        // Check for underscore-prefixed private fields that might be loggers
        // Pattern: _logger, _log, etc.
        IArchRule rule = FieldMembers()
            .That()
            .HaveNameStartingWith("_")
            .And()
            .HaveNameMatching(@"_[Ll]og.*")
            .Should()
            .NotExist()
            .Because(
                "ILogger<T> MUST use get-only property pattern, not underscore-prefixed fields per logging-rules.instructions.md")
            .WithoutRequiringPositiveResults();
        rule.Check(ArchitectureModel);
    }
}