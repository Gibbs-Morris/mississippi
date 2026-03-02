using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Builders.Runtime.Abstractions;


namespace Mississippi.Common.Builders.Runtime;

/// <summary>
///     Runtime UX projection sub-builder implementation.
/// </summary>
/// <typeparam name="TProjectionState">Projection state type.</typeparam>
public sealed class UxProjectionBuilder<TProjectionState> : IUxProjectionBuilder<TProjectionState>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UxProjectionBuilder{TProjectionState}" /> class.
    /// </summary>
    /// <param name="services">Service collection.</param>
    public UxProjectionBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
    }
}