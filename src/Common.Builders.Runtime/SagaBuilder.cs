using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Builders.Runtime.Abstractions;


namespace Mississippi.Common.Builders.Runtime;

/// <summary>
///     Runtime saga sub-builder implementation.
/// </summary>
/// <typeparam name="TSagaState">Saga state type.</typeparam>
public sealed class SagaBuilder<TSagaState> : ISagaBuilder<TSagaState>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SagaBuilder{TSagaState}" /> class.
    /// </summary>
    /// <param name="services">Service collection.</param>
    public SagaBuilder(
        IServiceCollection services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
    }
}