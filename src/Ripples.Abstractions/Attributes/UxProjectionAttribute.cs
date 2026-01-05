using System;


namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     Marks a record as a UX projection that should be exposed via HTTP/SignalR.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class UxProjectionAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionAttribute" /> class.
    /// </summary>
    /// <param name="route">The HTTP route.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="route" /> is null.</exception>
    public UxProjectionAttribute(
        string route
    )
    {
        ArgumentNullException.ThrowIfNull(route);
        Route = route;
    }

    /// <summary>
    ///     Gets or sets the authorization policy name.
    /// </summary>
    public string? Authorize { get; set; }

    /// <summary>
    ///     Gets or sets the brook name to read from (defaults to projection type name).
    /// </summary>
    public string? BrookName { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether batch endpoints are enabled.
    ///     Defaults to <c>true</c>.
    /// </summary>
    public bool IsBatchEnabled { get; set; } = true;

    /// <summary>
    ///     Gets the HTTP route for this projection.
    /// </summary>
    public string Route { get; }

    /// <summary>
    ///     Gets or sets the OpenAPI tags for grouping.
    /// </summary>
    public string[]? Tags { get; set; }
}