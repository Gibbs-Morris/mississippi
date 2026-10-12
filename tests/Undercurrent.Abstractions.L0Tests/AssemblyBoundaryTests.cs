using System;
using System.Reflection;
using System.Runtime.CompilerServices;


namespace MississippiTests.Undercurrent.Abstractions.L0Tests;

/// <summary>
///     Verifies the business contract assembly boundaries and test access.
/// </summary>
public sealed class AssemblyBoundaryTests
{
    /// <summary>
    ///     Verifies that public contracts do not depend on Undercurrent implementations.
    /// </summary>
    [Fact]
    public void ContractAssemblyDoesNotDependOnImplementations()
    {
        Assembly assembly = Assembly.Load("Mississippi.Undercurrent.Abstractions");
        Assert.DoesNotContain(
            assembly.GetReferencedAssemblies(),
            reference =>
                (reference.Name?.StartsWith("Mississippi.Undercurrent.Domain", StringComparison.Ordinal) == true) ||
                (reference.Name?.StartsWith("Mississippi.Undercurrent.Client", StringComparison.Ordinal) == true));
    }

    /// <summary>
    ///     Verifies that the matching L0 assembly can test internal contracts.
    /// </summary>
    [Fact]
    public void L0TestAssemblyHasFriendAccess()
    {
        Assembly assembly = Assembly.Load("Mississippi.Undercurrent.Abstractions");
        string? testAssemblyName = typeof(AssemblyBoundaryTests).Assembly.GetName().Name;
        Assert.Contains(
            assembly.GetCustomAttributes<InternalsVisibleToAttribute>(),
            attribute => attribute.AssemblyName == testAssemblyName);
    }
}