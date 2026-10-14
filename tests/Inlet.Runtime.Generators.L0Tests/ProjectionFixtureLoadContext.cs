using System.Reflection;
using System.Runtime.Loader;


namespace Mississippi.Inlet.Runtime.Generators.L0Tests;

/// <summary>
///     Loads each compiled projection fixture while sharing its real framework dependencies.
/// </summary>
internal sealed class ProjectionFixtureLoadContext : AssemblyLoadContext
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionFixtureLoadContext" /> class.
    /// </summary>
    public ProjectionFixtureLoadContext()
        : base(true)
    {
    }

    /// <inheritdoc />
    protected override Assembly? Load(
        AssemblyName assemblyName
    ) =>
        Default.LoadFromAssemblyName(assemblyName);
}