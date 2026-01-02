// <copyright file="ProjectionErrorEventArgs.cs" company="Gibbs-Morris">
// Copyright (c) Gibbs-Morris. All rights reserved.
// </copyright>

using System;


namespace Cascade.Server.Components.Services;

/// <summary>
///     Event args for projection errors.
/// </summary>
internal sealed class ProjectionErrorEventArgs : EventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionErrorEventArgs" /> class.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    public ProjectionErrorEventArgs(
        Exception exception
    ) =>
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));

    /// <summary>
    ///     Gets the exception that occurred.
    /// </summary>
    public Exception Exception { get; }
}