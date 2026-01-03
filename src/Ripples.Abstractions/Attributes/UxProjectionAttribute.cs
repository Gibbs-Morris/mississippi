namespace Mississippi.Ripples.Abstractions;

using System;

/// <summary>
/// Marks a record as a UX projection that should be exposed via HTTP/SignalR.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class UxProjectionAttribute : Attribute
{
    /// <summary>
    /// Gets the HTTP route for this projection.
    /// </summary>
    public string Route { get; }

    /// <summary>
    /// Gets or sets the brook name to read from (defaults to projection type name).
    /// </summary>
    public string? BrookName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable batch endpoints.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool EnableBatch { get; set; } = true;

    /// <summary>
    /// Gets or sets the authorization policy name.
    /// </summary>
    public string? Authorize { get; set; }

    /// <summary>
    /// Gets or sets the OpenAPI tags for grouping.
    /// </summary>
    public string[]? Tags { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UxProjectionAttribute"/> class.
    /// </summary>
    /// <param name="route">The HTTP route.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="route"/> is null.</exception>
    public UxProjectionAttribute(string route)
    {
        ArgumentNullException.ThrowIfNull(route);
        Route = route;
    }
}
