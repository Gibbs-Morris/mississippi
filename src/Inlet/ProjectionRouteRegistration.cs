using Mississippi.Inlet.Abstractions;


namespace Mississippi.Inlet;

/// <summary>
///     Implementation of <see cref="IConfigureProjectionRegistry" /> for registering projection routes.
/// </summary>
/// <typeparam name="T">The projection type being registered.</typeparam>
internal sealed class ProjectionRouteRegistration<T> : IConfigureProjectionRegistry
    where T : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionRouteRegistration{T}" /> class.
    /// </summary>
    /// <param name="route">The route path for the projection.</param>
    public ProjectionRouteRegistration(
        string route
    ) =>
        Route = route;

    private string Route { get; }

    /// <inheritdoc />
    public void Configure(
        IProjectionRegistry registry
    )
    {
        registry.Register<T>(Route);
    }
}