using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Common.Builders.Abstractions;

/// <summary>
///     Shared root contract for all Mississippi host-surface builders.
/// </summary>
public interface IMississippiBuilder
{
    /// <summary>
    ///     Gets the service collection used for immediate registration delegation.
    /// </summary>
    IServiceCollection Services { get; }
}