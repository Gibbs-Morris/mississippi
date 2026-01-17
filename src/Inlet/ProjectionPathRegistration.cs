using Mississippi.Inlet.Abstractions;


namespace Mississippi.Inlet;

/// <summary>
///     Implementation of <see cref="IConfigureProjectionRegistry" /> for registering projection paths.
/// </summary>
/// <typeparam name="T">The projection type being registered.</typeparam>
internal sealed class ProjectionPathRegistration<T> : IConfigureProjectionRegistry
    where T : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionPathRegistration{T}" /> class.
    /// </summary>
    /// <param name="path">The path for the projection.</param>
    public ProjectionPathRegistration(
        string path
    ) =>
        Path = path;

    private string Path { get; }

    /// <inheritdoc />
    public void Configure(
        IProjectionRegistry registry
    )
    {
        registry.Register<T>(Path);
    }
}