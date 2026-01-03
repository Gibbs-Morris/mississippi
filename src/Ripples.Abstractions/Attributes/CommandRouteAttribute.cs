namespace Mississippi.Ripples.Abstractions;

using System;

/// <summary>
/// Specifies the HTTP route for an aggregate command method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class CommandRouteAttribute : Attribute
{
    /// <summary>
    /// Gets the HTTP route for this command.
    /// </summary>
    public string Route { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandRouteAttribute"/> class.
    /// </summary>
    /// <param name="route">The HTTP route.</param>
    public CommandRouteAttribute(string route)
    {
        Route = route;
    }
}
