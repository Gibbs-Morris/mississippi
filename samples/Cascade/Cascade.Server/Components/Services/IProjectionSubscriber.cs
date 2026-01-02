// <copyright file="IProjectionSubscriber.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;


namespace Cascade.Server.Components.Services;

/// <summary>
///     Provides auto-updating access to a UX projection via SignalR notifications and HTTP fetch.
/// </summary>
/// <typeparam name="T">The projection type.</typeparam>
/// <remarks>
///     <para>
///         This service manages the subscription lifecycle for a single projection entity.
///         It handles SignalR connection management, version tracking, and efficient
///         data refreshing using HTTP ETags.
///     </para>
///     <para>
///         When a projection's version changes on the server, this service receives
///         a notification via SignalR and fetches the updated data only if needed.
///     </para>
/// </remarks>
internal interface IProjectionSubscriber<T> : IAsyncDisposable
    where T : class
{
    /// <summary>
    ///     Occurs when the <see cref="Current" /> value changes.
    /// </summary>
    event EventHandler? OnChanged;

    /// <summary>
    ///     Occurs when an error occurs during subscription or refresh.
    /// </summary>
    event EventHandler<ProjectionErrorEventArgs>? OnError;

    /// <summary>
    ///     Gets the current projection value.
    /// </summary>
    /// <value>The current projection, or <c>null</c> before initial load.</value>
    T? Current { get; }

    /// <summary>
    ///     Gets a value indicating whether the SignalR connection is active.
    /// </summary>
    /// <value><c>true</c> if connected; otherwise, <c>false</c>.</value>
    bool IsConnected { get; }

    /// <summary>
    ///     Gets a value indicating whether initial data has been loaded.
    /// </summary>
    /// <value><c>true</c> if data has been loaded; otherwise, <c>false</c>.</value>
    bool IsLoaded { get; }

    /// <summary>
    ///     Gets a value indicating whether the service is currently loading data.
    /// </summary>
    /// <value><c>true</c> if loading; otherwise, <c>false</c>.</value>
    bool IsLoading { get; }

    /// <summary>
    ///     Gets the last error that occurred, if any.
    /// </summary>
    /// <value>The last error, or <c>null</c> if no error has occurred.</value>
    Exception? LastError { get; }

    /// <summary>
    ///     Gets the current version of the projection.
    /// </summary>
    /// <value>The version number, or <c>null</c> before initial load.</value>
    long? Version { get; }

    /// <summary>
    ///     Refreshes the projection data manually.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RefreshAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Subscribes to projection updates for the specified entity.
    /// </summary>
    /// <param name="entityId">The entity identifier to subscribe to.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    ///     This method connects to SignalR (if not already connected), subscribes to
    ///     the projection's update stream, and fetches the initial data.
    /// </remarks>
    Task SubscribeAsync(
        string entityId,
        CancellationToken cancellationToken = default
    );
}