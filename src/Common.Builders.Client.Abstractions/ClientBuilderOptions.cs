namespace Mississippi.Common.Builders.Client.Abstractions;

/// <summary>
///     Options for client host builder configuration.
/// </summary>
public sealed class ClientBuilderOptions
{
    /// <summary>
    ///     Gets or sets an optional route prefix used by client endpoints.
    /// </summary>
    public string? RoutePrefix { get; set; }
}