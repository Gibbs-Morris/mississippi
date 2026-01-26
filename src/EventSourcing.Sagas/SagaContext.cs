using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Sagas.Abstractions;

using Orleans;


namespace Mississippi.EventSourcing.Sagas;

/// <summary>
///     Default saga context implementation.
/// </summary>
internal sealed class SagaContext : ISagaContext
{
    /// <inheritdoc />
    public required Guid SagaId { get; init; }

    /// <inheritdoc />
    public required string CorrelationId { get; init; }

    /// <inheritdoc />
    public required string SagaName { get; init; }

    /// <inheritdoc />
    public int Attempt { get; init; } = 1;

    /// <inheritdoc />
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
}
