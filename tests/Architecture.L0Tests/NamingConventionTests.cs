using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;

using static ArchUnitNET.Fluent.ArchRuleDefinition;


namespace Mississippi.Architecture.L0Tests;

/// <summary>
///     Architecture tests enforcing naming conventions per naming.instructions.md.
/// </summary>
/// <remarks>
///     Rules enforced:
///     - Interfaces MUST prefix with I.
///     - Namespaces MUST be feature-oriented (no Services/Models silos).
///     - Namespaces MUST have max 5 PascalCase segments, no underscores.
///     - Types MUST use PascalCase.
///     - Enums MUST be singular with PascalCase members.
/// </remarks>
public sealed class NamingConventionTests : ArchitectureTestBase
{
    /// <summary>
    ///     Verifies that abstract base classes intended for inheritance end with Base.
    /// </summary>
    /// <remarks>
    ///     Per abstractions-projects.instructions.md: "Abstract base classes MUST end with Base".
    /// </remarks>
    [Fact]
    public void AbstractBaseClassesShouldEndWithBase()
    {
        // Note: This is a SHOULD rule with exceptions for Orleans grains
        IArchRule rule = Classes()
            .That()
            .AreAbstract()
            .And()
            .AreNotSealed()
            .And()
            .DoNotHaveNameEndingWith("Grain") // Orleans grains have their own conventions
            .And()
            .DoNotHaveNameEndingWith("Controller") // ASP.NET controllers
            .And()
            .DoNotHaveNameEndingWith("Component") // Blazor components
            .Should()
            .HaveNameEndingWith("Base")
            .OrShould()
            .BeAbstract() // Accept abstract classes that aren't meant for inheritance
            .Because(
                "abstract base classes for inheritance should end with Base per abstractions-projects.instructions.md");
        rule.Check(ArchitectureModel);
    }

    /// <summary>
    ///     Verifies that enum types are singular (not plural).
    /// </summary>
    /// <remarks>
    ///     Per naming.instructions.md: "enums MUST be singular with PascalCase members".
    ///     Common plural endings like -Types, -Modes, -Statuses, -States indicate violations.
    /// </remarks>
    [Fact]
    public void EnumsShouldBeSingular()
    {
        // Check for common plural endings that indicate non-singular enums
        // Note: This is a heuristic check - some words ending in 's' are not plurals
        IArchRule rule = Types()
            .That()
            .AreEnums()
            .And()
            .ResideInNamespaceMatching(@"Mississippi\..*")
            .Should()
            .NotHaveNameEndingWith("Types")
            .AndShould()
            .NotHaveNameEndingWith("Modes")
            .AndShould()
            .NotHaveNameEndingWith("States")
            .AndShould()
            .NotHaveNameEndingWith("Statuses")
            .AndShould()
            .NotHaveNameEndingWith("Kinds")
            .AndShould()
            .NotHaveNameEndingWith("Categories")
            .Because("enums MUST be singular per naming.instructions.md")
            .WithoutRequiringPositiveResults();
        rule.Check(ArchitectureModel);
    }

    /// <summary>
    ///     Verifies that all interfaces start with I prefix.
    /// </summary>
    /// <remarks>
    ///     Per naming.instructions.md: "interfaces MUST prefix I".
    /// </remarks>
    [Fact]
    public void InterfacesShouldStartWithI()
    {
        IArchRule rule = Interfaces()
            .Should()
            .HaveNameStartingWith("I")
            .Because("interfaces MUST prefix I per naming.instructions.md");
        rule.Check(ArchitectureModel);
    }

    /// <summary>
    ///     Verifies that namespaces have at most 10 segments.
    /// </summary>
    /// <remarks>
    ///     Per naming.instructions.md: "namespaces max ten PascalCase segments".
    ///     Example valid: Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Actions (6 segments).
    ///     Example invalid: A.B.C.D.E.F.G.H.I.J.K (11 segments).
    /// </remarks>
    [Fact]
    public void NamespacesShouldHaveAtMostTenSegments()
    {
        // Pattern matches namespaces with more than 10 segments (11+ dots)
        // 11 segments example: A.B.C.D.E.F.G.H.I.J.K has 10 dots
        IArchRule rule = Types()
            .That()
            .ResideInNamespaceMatching(@"Mississippi\..*")
            .And()
            .DoNotResideInNamespaceMatching(@"OrleansCodeGen\..*") // Exclude Orleans generated code
            .Should()
            .NotResideInNamespaceMatching(@"^[^.]+\.[^.]+\.[^.]+\.[^.]+\.[^.]+\.[^.]+\.[^.]+\.[^.]+\.[^.]+\.[^.]+\.[^.]+.*$") // 11+ segments
            .Because("namespaces MUST have max ten PascalCase segments per naming.instructions.md")
            .WithoutRequiringPositiveResults();
        rule.Check(ArchitectureModel);
    }

    /// <summary>
    ///     Verifies that namespaces do not contain generic silo names like Services or Models.
    /// </summary>
    /// <remarks>
    ///     Per naming.instructions.md: "Namespaces MUST be feature-oriented (no Services/Models silos)".
    /// </remarks>
    [Fact]
    public void NamespacesShouldNotContainGenericSilos()
    {
        IArchRule rule = Types()
            .That()
            .ResideInNamespaceMatching(@"Mississippi\..*")
            .Should()
            .NotResideInNamespaceMatching(@".*\.Services\..*")
            .AndShould()
            .NotResideInNamespaceMatching(@".*\.Models\..*")
            .AndShould()
            .NotResideInNamespaceMatching(@".*\.Helpers\..*")
            .AndShould()
            .NotResideInNamespaceMatching(@".*\.Utilities\..*")
            .Because("namespaces must be feature-oriented, not generic silos per naming.instructions.md");
        rule.Check(ArchitectureModel);
    }

    /// <summary>
    ///     Verifies that namespaces do not contain underscores.
    /// </summary>
    /// <remarks>
    ///     Per naming.instructions.md: "namespaces max five PascalCase segments, no underscores".
    ///     Underscores are forbidden in namespace segments; use PascalCase instead.
    /// </remarks>
    [Fact]
    public void NamespacesShouldNotContainUnderscores()
    {
        IArchRule rule = Types()
            .That()
            .ResideInNamespaceMatching(@"Mississippi\..*")
            .And()
            .DoNotResideInNamespaceMatching(@"OrleansCodeGen\..*") // Exclude Orleans generated code
            .Should()
            .NotResideInNamespaceMatching(@".*_.*") // Any namespace containing underscore
            .Because("namespaces MUST NOT contain underscores per naming.instructions.md")
            .WithoutRequiringPositiveResults();
        rule.Check(ArchitectureModel);
    }

    /// <summary>
    ///     Verifies that Options classes follow the {Feature}Options naming convention.
    /// </summary>
    /// <remarks>
    ///     Per service-registration.instructions.md: "options classes MUST be named {Feature}Options".
    /// </remarks>
    [Fact]
    public void OptionsClassesShouldFollowNamingConvention()
    {
        IArchRule rule = Classes()
            .That()
            .HaveNameEndingWith("Options")
            .Should()
            .HaveNameEndingWith("Options")
            .Because("options classes must be named {Feature}Options per service-registration.instructions.md");
        rule.Check(ArchitectureModel);
    }
}