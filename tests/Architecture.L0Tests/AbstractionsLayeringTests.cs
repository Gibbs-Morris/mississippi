using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Mississippi.Architecture.L0Tests;

/// <summary>
///     Architecture tests enforcing abstractions project layering per abstractions-projects.instructions.md.
/// </summary>
/// <remarks>
///     Rules enforced:
///     - Abstractions projects MUST NOT depend on implementation projects.
///     - Abstractions projects MUST contain only contracts (interfaces, DTOs, exceptions).
///     - Abstractions MUST NOT contain infrastructure/DI/persistence/hosting code.
///     - Abstractions naming SHOULD follow {Vendor}.{Area}[.{Feature}].Abstractions.
///     - Abstract base classes MUST end with Base.
/// </remarks>
public sealed class AbstractionsLayeringTests : ArchitectureTestBase
{
    private static readonly IObjectProvider<IType> AbstractionsTypes = Types()
        .That()
        .ResideInNamespaceMatching(@"Mississippi\..*\.Abstractions.*")
        .As("Abstractions types");

    private static readonly IObjectProvider<IType> ImplementationTypes = Types()
        .That()
        .ResideInNamespaceMatching(@"Mississippi\..*")
        .And()
        .DoNotResideInNamespaceMatching(@"Mississippi\..*\.Abstractions.*")
        .And()
        .DoNotResideInNamespaceMatching(@"OrleansCodeGen\..*") // Exclude Orleans generated code
        .As("Implementation types");

    /// <summary>
    ///     Verifies that abstractions namespaces do not depend on implementation namespaces.
    /// </summary>
    /// <remarks>
    ///     Per abstractions-projects.instructions.md: "abstractions MUST NOT depend on implementations".
    ///     Known violations documented in <see cref="KnownViolations.AbstractionImplementationDependencies"/>.
    /// </remarks>
    [Fact]
    public void AbstractionsShouldNotDependOnImplementations()
    {
        // Note: ICosmosRepository and ISnapshotContainerOperations are known violations - see KnownViolations
        // The rule is still enforced for all new code
        IArchRule rule = Types()
            .That()
            .Are(AbstractionsTypes)
            .And()
            .DoNotHaveNameContaining("ICosmosRepository") // Known violation - see KnownViolations
            .And()
            .DoNotHaveNameContaining("ISnapshotContainerOperations") // Known violation - see KnownViolations
            .Should()
            .NotDependOnAny(ImplementationTypes)
            .Because("abstractions MUST NOT depend on implementations per abstractions-projects.instructions.md");

        rule.Check(ArchitectureModel);
    }

    /// <summary>
    ///     Verifies that types in Abstractions namespaces are interfaces, abstract classes, or simple DTOs.
    /// </summary>
    /// <remarks>
    ///     Per abstractions-projects.instructions.md: "*.Abstractions projects MUST contain only public contracts
    ///     (interfaces, abstract bases, DTOs, domain exceptions, CQRS requests)".
    /// </remarks>
    [Fact]
    public void AbstractionsTypesShouldBeContractsOnly()
    {
        // Types in Abstractions should be interfaces, abstract, records, or exceptions
        IArchRule rule = Classes()
            .That()
            .ResideInNamespaceMatching(@"Mississippi\..*\.Abstractions.*")
            .And()
            .AreNotAbstract()
            .And()
            .DoNotHaveNameEndingWith("Exception")
            .And()
            .DoNotHaveNameEndingWith("Options") // Options are allowed
            .And()
            .DoNotHaveNameEndingWith("Extensions") // Extension methods are allowed
            .And()
            .DoNotHaveNameContaining("Registration") // Service registration is allowed
            .And()
            .DoNotHaveNameEndingWith("Event") // Domain events are allowed
            .Should()
            .BeSealed() // DTOs/records should be sealed or record
            .OrShould()
            .BeAbstract()
            .Because("abstractions should contain only contracts per abstractions-projects.instructions.md");

        rule.Check(ArchitectureModel);
    }

    /// <summary>
    ///     Verifies that abstractions do not contain infrastructure types (DI, persistence, hosting).
    /// </summary>
    /// <remarks>
    ///     Per abstractions-projects.instructions.md: "no infrastructure/DI/persistence/hosting code".
    ///     Service registration extension methods are explicitly allowed.
    /// </remarks>
    [Fact]
    public void AbstractionsShouldNotContainInfrastructureTypes()
    {
        // Abstractions should not contain types that indicate infrastructure concerns
        IArchRule rule = Classes()
            .That()
            .ResideInNamespaceMatching(@"Mississippi\..*\.Abstractions.*")
            .And()
            .DoNotHaveNameContaining("Registration") // Service registrations are allowed (extension methods)
            .And()
            .DoNotHaveNameContaining("Extensions") // Extension methods are allowed
            .Should()
            .NotHaveNameContaining("Repository") // Implementation pattern
            .AndShould()
            .NotHaveNameContaining("DbContext") // EF Core
            .AndShould()
            .NotHaveNameContaining("HostBuilder") // Hosting
            .AndShould()
            .NotHaveNameContaining("Startup") // ASP.NET startup
            .AndShould()
            .NotHaveNameContaining("Middleware") // HTTP pipeline
            .Because("abstractions MUST NOT contain infrastructure/DI/persistence/hosting code per abstractions-projects.instructions.md");

        rule.Check(ArchitectureModel);
    }

    /// <summary>
    ///     Verifies that abstractions namespace follows the naming convention.
    /// </summary>
    /// <remarks>
    ///     Per abstractions-projects.instructions.md: "naming SHOULD follow {Vendor}.{Area}[.{Feature}].Abstractions".
    ///     For Mississippi: Mississippi.{Area}[.{Feature}].Abstractions.
    ///     Excludes Orleans-generated types (OrleansCodeGen namespace) which are compiler-generated.
    /// </remarks>
    [Fact]
    public void AbstractionsNamespacesShouldFollowNamingConvention()
    {
        // Verify abstractions namespaces match the expected pattern
        // Exclude Orleans-generated types (OrleansCodeGen) as they are compiler-generated
        IArchRule rule = Types()
            .That()
            .ResideInNamespaceMatching(@"Mississippi\..*\.Abstractions.*")
            .And()
            .DoNotResideInNamespaceMatching(@"OrleansCodeGen\..*")
            .Should()
            .ResideInNamespaceMatching(@"^Mississippi\.[A-Z][a-zA-Z0-9]+(\.[A-Z][a-zA-Z0-9]+)*\.Abstractions(\.[A-Z][a-zA-Z0-9]+)*$")
            .Because("abstractions namespace naming SHOULD follow {Vendor}.{Area}[.{Feature}].Abstractions per abstractions-projects.instructions.md");

        rule.Check(ArchitectureModel);
    }

    /// <summary>
    ///     Verifies that abstract base classes in abstractions end with Base.
    /// </summary>
    /// <remarks>
    ///     Per abstractions-projects.instructions.md: "Abstract base classes intended for external inheritance
    ///     MUST end with Base and document justification".
    /// </remarks>
    [Fact]
    public void AbstractBaseClassesInAbstractionsShouldEndWithBase()
    {
        // WithoutRequiringPositiveResults() allows the rule to pass when no abstract classes exist.
        // Use regex pattern to handle generic types (e.g., ReducerBase`2 ends with "Base`2").
        IArchRule rule = Classes()
            .That()
            .ResideInNamespaceMatching(@"Mississippi\..*\.Abstractions.*")
            .And()
            .AreAbstract()
            .And()
            .AreNotSealed()
            .Should()
            .HaveNameMatching(@".*Base(`\d+)?$")
            .Because("abstract base classes in abstractions MUST end with Base per abstractions-projects.instructions.md")
            .WithoutRequiringPositiveResults();

        rule.Check(ArchitectureModel);
    }
}
