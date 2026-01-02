using System;

using Microsoft.Extensions.DependencyInjection;


namespace Cascade.Server.Components.Services;

/// <summary>
///     Default implementation of <see cref="IProjectionSubscriberFactory" />.
/// </summary>
internal sealed class ProjectionSubscriberFactory : IProjectionSubscriberFactory
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionSubscriberFactory" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    public ProjectionSubscriberFactory(
        IServiceProvider serviceProvider
    ) =>
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    private IServiceProvider ServiceProvider { get; }

    /// <inheritdoc />
    public IProjectionSubscriber<T> Create<T>()
        where T : class =>
        ServiceProvider.GetRequiredService<IProjectionSubscriber<T>>();
}