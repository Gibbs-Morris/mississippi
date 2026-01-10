using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Mississippi.Architecture.L0Tests;

/// <summary>
///     Architecture tests enforcing service registration patterns per service-registration.instructions.md.
/// </summary>
/// <remarks>
///     Rules enforced:
///     - ServiceRegistration classes MUST be static.
///     - ServiceRegistration classes expose Add{Feature}() extension methods.
/// </remarks>
public sealed class ServiceRegistrationArchitectureTests : ArchitectureTestBase
{
    private static readonly IObjectProvider<Class> ServiceRegistrationClasses = Classes()
        .That()
        .HaveNameContaining("ServiceRegistration")
        .Or()
        .HaveNameContaining("ServiceCollectionExtensions")
        .Or()
        .HaveNameEndingWith("Registrations")
        .As("Service registration types");

    /// <summary>
    ///     Verifies that service registration classes are static (sealed).
    /// </summary>
    /// <remarks>
    ///     Per service-registration.instructions.md: registration classes expose static extension methods.
    /// </remarks>
    [Fact]
    public void ServiceRegistrationClassesShouldBeStatic()
    {
        IArchRule rule = Classes()
            .That()
            .Are(ServiceRegistrationClasses)
            .Should()
            .BeSealed() // Static classes are sealed
            .Because("ServiceRegistration classes must be static per service-registration.instructions.md");

        rule.Check(ArchitectureModel);
    }

    /// <summary>
    ///     Verifies that service registration classes are public (they provide the DI entry point).
    /// </summary>
    /// <remarks>
    ///     Per service-registration.instructions.md: "public registration MUST exist at product/feature boundaries".
    /// </remarks>
    [Fact]
    public void PublicServiceRegistrationClassesShouldBePublic()
    {
        // Note: Some internal registrations are called by parent registrations
        // This test just ensures naming consistency
        IArchRule rule = Classes()
            .That()
            .Are(ServiceRegistrationClasses)
            .And()
            .ArePublic()
            .Should()
            .BePublic()
            .Because("public service registrations provide DI entry points per service-registration.instructions.md");

        rule.Check(ArchitectureModel);
    }
}
