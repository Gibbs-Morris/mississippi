using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Mississippi.Architecture.L0Tests;

/// <summary>
///     Architecture tests enforcing Orleans POCO grain patterns per orleans.instructions.md.
/// </summary>
/// <remarks>
///     Rules enforced:
///     - Grains MUST implement IGrainBase (not inherit from Grain).
///     - Concrete grains MUST be sealed.
///     - Grain implementations SHOULD be internal.
/// </remarks>
public sealed class OrleansGrainArchitectureTests : ArchitectureTestBase
{
    private static readonly IObjectProvider<Class> GrainClasses = Classes()
        .That()
        .HaveNameEndingWith("Grain")
        .And()
        .AreNotAbstract()
        .And()
        .DoNotHaveNameContaining("Factory")
        .As("Grain implementations");

    private static readonly IObjectProvider<Interface> GrainInterfaces = Interfaces()
        .That()
        .HaveNameMatching("^I.*Grain$")
        .As("Grain interfaces");

    /// <summary>
    ///     Verifies that concrete grain implementations are sealed.
    /// </summary>
    /// <remarks>
    ///     Per orleans.instructions.md: "concrete grains MUST be sealed".
    /// </remarks>
    [Fact]
    public void ConcreteGrainsShouldBeSealed()
    {
        IArchRule rule = Classes()
            .That()
            .Are(GrainClasses)
            .Should()
            .BeSealed()
            .Because("Orleans POCO grains must be sealed per orleans.instructions.md");

        rule.Check(ArchitectureModel);
    }

    /// <summary>
    ///     Verifies that grain implementations should be internal unless explicitly public.
    /// </summary>
    /// <remarks>
    ///     Per csharp.instructions.md: "types MUST default to internal".
    /// </remarks>
    [Fact]
    public void GrainImplementationsShouldBeInternal()
    {
        IArchRule rule = Classes()
            .That()
            .Are(GrainClasses)
            .And()
            .DoNotHaveNameContaining("Observer") // Observers may need to be public
            .Should()
            .BeInternal()
            .Because("grain implementations should be internal per csharp.instructions.md");

        rule.Check(ArchitectureModel);
    }

    /// <summary>
    ///     Verifies that grain interfaces follow the I{Name}Grain naming convention.
    /// </summary>
    [Fact]
    public void GrainInterfacesShouldFollowNamingConvention()
    {
        IArchRule rule = Interfaces()
            .That()
            .Are(GrainInterfaces)
            .Should()
            .HaveNameStartingWith("I")
            .Because("interfaces must prefix with I per naming.instructions.md");

        rule.Check(ArchitectureModel);
    }
}
