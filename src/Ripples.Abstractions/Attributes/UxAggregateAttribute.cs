namespace Mississippi.Ripples.Abstractions;

using System;

/// <summary>
/// Marks an interface as a UX aggregate, enabling command routing via HTTP.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class UxAggregateAttribute : Attribute
{
    /// <summary>
    /// Gets the base HTTP route for this aggregate's commands.
    /// </summary>
    public string Route { get; }

    /// <summary>
    /// Gets or sets the authorization policy name.
    /// </summary>
    public string? Authorize { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UxAggregateAttribute"/> class.
    /// </summary>
    /// <param name="route">The HTTP route.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="route"/> is null.</exception>
    public UxAggregateAttribute(string route)
    {
        ArgumentNullException.ThrowIfNull(route);
        Route = route;
    }
}
