using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Mississippi.Architecture.L0Tests;

/// <summary>
///     Architecture tests enforcing access control patterns per csharp.instructions.md.
/// </summary>
/// <remarks>
///     Rules enforced:
///     - Implementation types SHOULD default to internal.
///     - Only contracts/API types should be public.
///     These tests have extensive exclusions for known patterns that must be public.
///     New violations will be caught and should be evaluated case-by-case.
/// </remarks>
public sealed class AccessControlArchitectureTests : ArchitectureTestBase
{
    /// <summary>
    ///     Verifies that non-abstract classes in implementation namespaces are internal.
    /// </summary>
    /// <remarks>
    ///     Per csharp.instructions.md: "types MUST default to internal".
    ///     Many patterns legitimately need public visibility (Options, Extensions, Controllers, etc.).
    ///     This test catches new public types that don't match known patterns.
    ///     Private nested classes are also flagged but are acceptable in some patterns.
    /// </remarks>
    [Fact]
    public void ImplementationClassesShouldBeInternal()
    {
        IArchRule rule = Classes()
            .That()
            .ResideInNamespaceMatching(@"Mississippi\..*")
            .And()
            .DoNotResideInNamespaceMatching(@"Mississippi\..*\.Abstractions.*")
            .And()
            .DoNotResideInNamespaceMatching(@"OrleansCodeGen\..*") // Exclude Orleans generated code
            .And()
            .AreNotAbstract()
            .And()
            .AreNotNested() // Exclude nested/private classes - acceptable pattern
            .And()
            .DoNotHaveNameEndingWith("Options") // Options are public for configuration
            .And()
            .DoNotHaveNameContaining("Registration") // Service registrations are public
            .And()
            .DoNotHaveNameContaining("Extensions") // Extension methods are public
            .And()
            .DoNotHaveNameEndingWith("Factory") // Factories may need to be public
            .And()
            .DoNotHaveNameEndingWith("Controller") // ASP.NET controllers
            .And()
            .DoNotHaveNameEndingWith("Hub") // SignalR hubs
            .And()
            .DoNotHaveNameEndingWith("Observer") // Orleans observers
            .And()
            .DoNotHaveNameEndingWith("Component") // Blazor components
            .And()
            .DoNotHaveNameEndingWith("Exception") // Exceptions are public
            .And()
            .DoNotHaveNameEndingWith("State") // Orleans grain state must be public
            .And()
            .DoNotHaveNameContaining("Reducer") // Reducers are public API
            .And()
            .DoNotHaveNameContaining("Store") // Stores are public API
            .And()
            .DoNotHaveNameContaining("Handler") // Handlers may be public API
            .And()
            .DoNotHaveNameContaining("Manager") // Managers may be public API
            .And()
            .DoNotHaveNameContaining("Registry") // Registries are public API
            .And()
            .DoNotHaveNameContaining("Codec") // Orleans serialization codecs
            .And()
            .DoNotHaveNameContaining("Copier") // Orleans serialization copiers
            .And()
            .DoNotHaveNameContaining("Invokable") // Orleans grain invokables
            .And()
            .DoNotHaveNameContaining("Activator") // Orleans grain activators
            .And()
            .DoNotHaveNameContaining("MethodInvoker") // Orleans method invokers
            .And()
            .DoNotHaveNameContaining("_") // Orleans generated types with underscores
            .And()
            .DoNotHaveNameContaining("Serializer") // Serializers are public API
            .And()
            .DoNotHaveNameContaining("Writer") // Writers may be public API
            .And()
            .DoNotHaveNameContaining("Reader") // Readers may be public API
            .And()
            .DoNotHaveNameContaining("Provider") // Providers are public API
            .And()
            .DoNotHaveNameContaining("Aggregate") // Aggregates are public API
            .And()
            .DoNotHaveNameContaining("Event") // Domain events are public
            .And()
            .DoNotHaveNameContaining("Command") // Commands are public
            .And()
            .DoNotHaveNameContaining("Query") // Queries are public
            .And()
            .DoNotHaveNameContaining("Result") // Results are public
            .And()
            .DoNotHaveNameContaining("Dispatcher") // Dispatchers are public API
            .And()
            .DoNotHaveNameContaining("Builder") // Builders are public API
            .And()
            .DoNotHaveNameContaining("Client") // Clients are public API
            .And()
            .DoNotHaveNameContaining("Context") // Contexts are public API
            .And()
            .DoNotHaveNameContaining("Metadata") // Metadata types are public
            .And()
            .DoNotHaveNameContaining("Projection") // Projections are public
            .And()
            .DoNotHaveNameContaining("Policy") // Policies are public API
            .And()
            .DoNotHaveNameContaining("Info") // Info types may be public
            .Should()
            .BeInternal()
            .Because("implementation types should default to internal per csharp.instructions.md");

        rule.Check(ArchitectureModel);
    }

    /// <summary>
    ///     Verifies that interfaces in Abstractions namespaces are public.
    /// </summary>
    /// <remarks>
    ///     Per abstractions-projects.instructions.md: abstractions expose public contracts.
    ///     Internal interfaces for implementation details should be in non-Abstractions namespaces.
    /// </remarks>
    [Fact]
    public void AbstractionsInterfacesShouldBePublic()
    {
        IArchRule rule = Interfaces()
            .That()
            .ResideInNamespaceMatching(@"Mississippi\..*\.Abstractions.*")
            .Should()
            .BePublic()
            .Because("abstractions interfaces should be public per abstractions-projects.instructions.md");

        rule.Check(ArchitectureModel);
    }
}
