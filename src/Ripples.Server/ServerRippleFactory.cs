namespace Mississippi.Ripples.Server;

using System;
using Microsoft.Extensions.Logging;
using Mississippi.EventSourcing.UxProjections.Abstractions;
using Mississippi.Ripples.Abstractions;

/// <summary>
/// Server-side factory for creating ripple instances.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ServerRippleFactory{TProjection}"/> creates <see cref="ServerRipple{TProjection}"/>
/// instances that access Orleans grains directly and subscribe to in-process notifications
/// for real-time updates.
/// </para>
/// </remarks>
/// <typeparam name="TProjection">The projection type.</typeparam>
public sealed class ServerRippleFactory<TProjection> : IRippleFactory<TProjection>
    where TProjection : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerRippleFactory{TProjection}"/> class.
    /// </summary>
    /// <param name="projectionGrainFactory">Factory for resolving UX projection grains.</param>
    /// <param name="signalRNotifier">Notifier for projection updates.</param>
    /// <param name="loggerFactory">Factory for creating loggers.</param>
    public ServerRippleFactory(
        IUxProjectionGrainFactory projectionGrainFactory,
        IProjectionUpdateNotifier signalRNotifier,
        ILoggerFactory loggerFactory)
    {
        ProjectionGrainFactory = projectionGrainFactory ??
            throw new ArgumentNullException(nameof(projectionGrainFactory));
        SignalRNotifier = signalRNotifier ??
            throw new ArgumentNullException(nameof(signalRNotifier));
        LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    private IUxProjectionGrainFactory ProjectionGrainFactory { get; }

    private IProjectionUpdateNotifier SignalRNotifier { get; }

    private ILoggerFactory LoggerFactory { get; }

    /// <inheritdoc/>
    public IRipple<TProjection> Create()
    {
        ILogger<ServerRipple<TProjection>> logger =
            LoggerFactory.CreateLogger<ServerRipple<TProjection>>();

        return new ServerRipple<TProjection>(
            ProjectionGrainFactory,
            SignalRNotifier,
            logger);
    }
}
