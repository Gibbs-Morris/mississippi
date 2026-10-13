using System;
using System.Reflection;
using System.Runtime.CompilerServices;


namespace MississippiTests.Undercurrent.Domain.L0Tests;

/// <summary>
///     Verifies the enterprise domain assembly boundaries and test access.
/// </summary>
public sealed class AssemblyBoundaryTests
{
    /// <summary>
    ///     Verifies that server-side business behavior does not depend on client packages.
    /// </summary>
    [Fact]
    public void DomainAssemblyDoesNotDependOnClientPackages()
    {
        Assembly assembly = Assembly.Load("Mississippi.Undercurrent.Domain");
        Assert.DoesNotContain(
            assembly.GetReferencedAssemblies(),
            reference =>
                (reference.Name?.StartsWith("Mississippi.Undercurrent.Client", StringComparison.Ordinal) == true) ||
                (reference.Name?.StartsWith("Mississippi.Refraction.Client", StringComparison.Ordinal) == true) ||
                (reference.Name?.StartsWith("Mississippi.Reservoir", StringComparison.Ordinal) == true));
    }

    /// <summary>
    ///     Verifies that the matching L0 assembly can test internal domain behavior.
    /// </summary>
    [Fact]
    public void L0TestAssemblyHasFriendAccess()
    {
        Assembly assembly = Assembly.Load("Mississippi.Undercurrent.Domain");
        string? testAssemblyName = typeof(AssemblyBoundaryTests).Assembly.GetName().Name;
        Assert.Contains(
            assembly.GetCustomAttributes<InternalsVisibleToAttribute>(),
            attribute => attribute.AssemblyName == testAssemblyName);
    }
}